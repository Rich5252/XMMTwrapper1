using MmttySwitchFlagsConstants;
using stdole;

namespace XMMTwrapper1
{
    //Buttin array used to indicate and change MMTTY settings
    //Received settings are stored in the CodeSwitch int in the NMMR array.
    //Updates are sent via axXMMR1.SetMmttySwitch(new) call
    internal sealed class MmttySwitchButtonGroup
    {
        //flag to stop reentrant
        private bool _updatingUi;

        //button array with controls and individual setups (deferred until Form1.Shown)
        private readonly List<(AxXMMTLib.AxXMMBtn Btn, BtnSetup Setup)> _buttons = new();

        public MmttySwitchFlags Current { get; private set; }

        public event Action<MmttySwitchFlags>? SwitchChangedByUser;

        //private static IFontDisp CreateOleFont();

        //Button setup happens after controls created so store required setup values for later
        private sealed class BtnSetup
        {
            public string Text = "";
            public int Col;
            public int Row;
            public MmttySwitchFlags Flag;
        }

        private readonly AxXMMTLib.AxXMMBtn _XmmBtnTemplate;
        public MmttySwitchButtonGroup(
            AxXMMTLib.AxXMMBtn XmmBtnTemplate,          //template is dummy Form button used to set default (ocx)
                                                        //state of dynamic ones. Trying to solve bad Caption render                                                         
            Control parent,                             //container for the button array   
            int left,
            int top,
            int spacingX = 38,
            int spacingY = 38,
            int columns = 3)
        {
            
            _XmmBtnTemplate = XmmBtnTemplate;
            _parent = parent;

            //standard MMTTY buttons - see CodeSwitch Settings.md for full descriptions
            IEnumerable<(string Text, MmttySwitchFlags Flag)> defs;
            defs = new (string, MmttySwitchFlags)[]
            {
                ("AFC", MmttySwitchFlags.AFC),          // Auto Freq Control
                ("NET", MmttySwitchFlags.NET),          // Tx/Rx Netted (ie Tx AFSK tracks Rx mark/space)
                ("ATC", MmttySwitchFlags.ATC),          // Auto Threshold Control
                ("BPF", MmttySwitchFlags.BPF),          // BandPass Filter
                ("LMS", MmttySwitchFlags.LMS),          // Least Mean Square Filter (enable for Notch)
                ("SQ",  MmttySwitchFlags.SQ),           // Squelch on/off
                ("REV", MmttySwitchFlags.REV),          // Reverse mark/space (normal on for Ham)
                ("UOS", MmttySwitchFlags.UOS),          // Unshift On Space (helps on noisy signals)
                ("NOT", MmttySwitchFlags.NOT),          // Notch enable (requires LMS enabled)
                ("TWO", MmttySwitchFlags.TWO),          // Two notch filters
            };

            int i = 0;
            foreach (var (text, flag) in defs)
            {
                int col = i % columns;
                int row = i / columns;

                //Create each button in control group
                var XmmBtn = new AxXMMTLib.AxXMMBtn();
                ((System.ComponentModel.ISupportInitialize)XmmBtn).BeginInit();
                XmmBtn.OcxState = _XmmBtnTemplate.OcxState;
                ((System.ComponentModel.ISupportInitialize)XmmBtn).EndInit();

                XmmBtn.Parent = _parent;

                XmmBtn.Tag = flag;                          //marks each button ID
                XmmBtn.OnLButtonClick += OnLButtonClick;    //add to consolotated click event
                XmmBtn.Font = XmmBtnTemplate.Font;
                parent.Controls.Add(XmmBtn);                //add to parent Group control
                //((System.ComponentModel.ISupportInitialize)XmmBtn).EndInit();

                //and save default setup for later
                _buttons.Add((XmmBtn, new BtnSetup
                {
                    Text = text,
                    Col = col,
                    Row = row,
                    Flag = flag
                }));

                i++;
            }

            // store layout constants for later default setup
            _parent = parent;
            _left = left;
            _top = top;
            _spacingX = spacingX;
            _spacingY = spacingY;

            Log.LogThreadDetails("MmttySwitchButtonGroup Buttons created");
        }

        private int _left, _top, _spacingX, _spacingY;
        private Control _parent;

        public void ApplyMmttyBtnSetup()
        {
            // Ensure parent has a handle
            _parent.CreateControl();
            _parent.SuspendLayout();

            foreach (var (btn, setup) in _buttons)
            {
                btn.CreateControl();
                btn.AutoSize = false;
                btn.Width = 35;
                btn.Height = 35;
                btn.Left = _left + setup.Col * _spacingX;
                btn.Top = _top + setup.Row * _spacingY;
                // Prefer copying the template font (it already matches what the OCX expects)
                btn.Font = _XmmBtnTemplate.Font;
                btn.Caption = setup.Text;
                btn.Refresh();

                /*
                btn.Font = btn.Font = new System.Drawing.Font(
                            "Verdana", // or "Segoe UI"
                            8f,
                            FontStyle.Bold,
                            GraphicsUnit.Point);
                */
                
            }
            _parent.ResumeLayout(true);
            Log.LogThreadDetails("ApplyMmttyBtnSetup Buttons setup done");
        }

        private static IFontDisp CreateOleFont(
            string name = "MS Sans Serif",
            float size = 8.25f,
            bool bold = false)
        {
            var f = new StdFont
            {
                Name = name,
                Size = (decimal) size,
                Bold = bold
            };
            return (IFontDisp)f;
        }


        // Call this whenever you receive a fresh codeswitch from the OCX (NMMR update)
        public void SetFromMmtty(uint codeswitch)
            => SetFromMmtty((MmttySwitchFlags)codeswitch);

        public void SetFromMmtty(MmttySwitchFlags flags)
        {
            _updatingUi = true;
            try
            {
                Current = flags;

                foreach (var (btn, setup) in _buttons)
                {
                    var f = (MmttySwitchFlags)setup.Flag;
                    btn.CtlState = (Current & f) != 0;
                }
            }
            finally
            {
                _updatingUi = false;
            }
        }

        private void OnLButtonClick(object? sender, EventArgs e)
        {
            if (_updatingUi) return;
            if (sender is not AxXMMTLib.AxXMMBtn XmmBtn) return;

            var flag = (MmttySwitchFlags)XmmBtn.Tag;

            var newFlags = XmmBtn.CtlState ? (Current | flag) : (Current & ~flag);
            Current = newFlags;

            SwitchChangedByUser?.Invoke(Current);
        }
    }
}
