using AxXMMTLib;
using Constants;
using MmttySwitchFlagsConstants;
using stdole;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing;
using System.Windows.Forms;
// Define specific aliases to resolve the ambiguity
using NetFont = System.Drawing.Font;
using OleFont = stdole.Font;

namespace XMMTwrapper1
{
    /// Refactored button group to handle legacy ActiveX timing issues.
    /// Uses BeginInvoke to ensure properties like Caption and Font are set 
    /// only after the OCX is fully initialized.
    /// NOTE ALL ax buttons have to be dynamically created to avoid font/render problems
    internal sealed class MmttySwitchButtonGroup
    {
        private bool _updatingUi;
        private readonly List<(AxXMMTLib.AxXMMBtn Btn, BtnSetup Setup)> _buttons = new();
        private readonly AxXMMTLib.AxXMMBtn _XmmBtnTemplate;
        private readonly Control _parent;

        private readonly int _left, _top, _spacingX, _spacingY;

        public MmttySwitchFlags Current { get; private set; }
        public event Action<MmttySwitchFlags, AxXMMBtn>? SwitchChangedByUser;

        private sealed class BtnSetup
        {
            public string Text = "";
            public int Col;
            public int Row;
            public MmttySwitchFlags Flag;
        }

        public MmttySwitchButtonGroup(
            AxXMMTLib.AxXMMBtn XmmBtnTemplate,
            Control parent,
            int left,
            int top,
            int spacingX = 38,
            int spacingY = 38,
            int columns = 5)
        {
            _XmmBtnTemplate = XmmBtnTemplate;
            _parent = parent;
            _left = left;
            _top = top;
            _spacingX = spacingX;
            _spacingY = spacingY;

            //This version includes user defined row/col positions
            var defs = new (string Text, MmttySwitchFlags Flag, int row, int col)[]
            {
                ("AFC", MmttySwitchFlags.AFC, 0, 0),
                ("NET", MmttySwitchFlags.NET, 0, 1),
                ("ATC", MmttySwitchFlags.ATC, 0, 2),
                ("BPF", MmttySwitchFlags.BPF, 0, 3),
                ("LMS", MmttySwitchFlags.LMS, 0, 4),
                ("SQ",  MmttySwitchFlags.SQ, 1, 0),
                ("REV", MmttySwitchFlags.REV, 1, 1),
                ("UOS", MmttySwitchFlags.UOS, 1, 2),
                ("NOT", MmttySwitchFlags.NOT, 1, 3),
                ("TWO", MmttySwitchFlags.TWO, 1, 4),
                ("Ham", MmttySwitchFlags.None, 3, 2)
            };

            int i = 0;
            foreach (var (text, flag, dRow, dCol) in defs)
            {
                //use following to auto allocate row/col
                int col = i % columns;
                int row = i / columns;

                var xmmBtn = new AxXMMTLib.AxXMMBtn();
                ((System.ComponentModel.ISupportInitialize)xmmBtn).BeginInit();

                // Only copy OcxState if the template is actually created/valid
                if (_XmmBtnTemplate.Created)
                {
                    xmmBtn.OcxState = _XmmBtnTemplate.OcxState;
                }

                ((System.ComponentModel.ISupportInitialize)xmmBtn).EndInit();

                xmmBtn.Parent = _parent;
                xmmBtn.Tag = flag;
                xmmBtn.OnLButtonClick += OnLButtonClick;

                _parent.Controls.Add(xmmBtn);

                _buttons.Add((xmmBtn, new BtnSetup
                {
                    Text = text,
                    Col = dCol,
                    Row = dRow,
                    Flag = flag
                }));
                i++;
            }
            Log.LogThreadDetails("MmttySwitchButtonGroup: Buttons initialized");
        }

        /// Call this in Form_Shown to finalize layout and rendering.
        public void ApplyMmttyBtnSetup()
        {
            if (!_parent.IsHandleCreated) _parent.CreateControl();
            _parent.SuspendLayout();

            foreach (var (btn, setup) in _buttons)
            {
                // 1. Set WinForms layout properties immediately
                btn.Width = 35;
                btn.Height = 35;
                btn.Left = _left + setup.Col * _spacingX;
                btn.Top = _top + setup.Row * _spacingY;
                if (setup.Row == 3) btn.Top = 95;               //align with radio btns

                    // 2. Use BeginInvoke to defer ActiveX-specific COM property assignments.
                    // This allows the current UI message loop to process the handle creation 
                    // and OLE activation before we try to set the Caption or Font.
                    ApplyBtnFont(btn, setup.Text);
            }
            _parent.ResumeLayout(true);
            Log.LogThreadDetails("ApplyMmttyBtnSetup: Layout applied and property updates queued.");
        }

        private void ApplyBtnFont(AxXMMBtn btn, string txt)
        {
            btn.BeginInvoke(new Action(() =>
            {
            try
            {
                // Ensure the button is visible before rendering text
                btn.Visible = true;

                // Create a UNIQUE font instance for every button.
                // Sharing the template font object via pointer can cause COM corruption.
                btn.Font = new System.Drawing.Font("Verdana", 8f, System.Drawing.FontStyle.Bold);

                btn.Caption = txt;
                btn.Refresh();
            }
            catch (Exception ex)
            {
                Log.LogThreadDetails($"Button Render Error ({txt}): {ex.Message}");
            }
            }));
         }

        public void SetFromMmtty(uint codeswitch) => SetFromMmtty((MmttySwitchFlags)codeswitch);

        public void SetFromMmtty(MmttySwitchFlags flags)
        {
            _updatingUi = true;
            try
            {
                Current = flags;
                foreach (var (btn, setup) in _buttons)
                {
                    // Update state safely on UI thread
                    if (btn.InvokeRequired)
                    {
                        btn.BeginInvoke(new Action(() => btn.CtlState = (Current & setup.Flag) != 0));
                    }
                    else
                    {
                        btn.CtlState = (Current & setup.Flag) != 0;
                    }
                }
            }
            finally
            {
                _updatingUi = false;
            }
        }

        private void OnLButtonClick(object? sender, EventArgs e)
        {
            if (_updatingUi || sender is not AxXMMTLib.AxXMMBtn xmmBtn) return;

            var flag = (MmttySwitchFlags)xmmBtn.Tag;
            if (flag != MmttySwitchFlags.None)
            {
                var newFlags = xmmBtn.CtlState ? (Current | flag) : (Current & ~flag);
                Current = newFlags;
                SwitchChangedByUser?.Invoke(Current, xmmBtn);
            }
            else
            {
                //additional buttons eg Ham select
                //add Tag in future to identify more than one button
                SwitchChangedByUser?.Invoke(MmttySwitchFlags.None, xmmBtn);
            }

                   
        }
    }
}