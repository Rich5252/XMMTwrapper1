// TickerControl.cs
//
// "Ticker tape" display for JTTY: one row (lane) per signal, sorted by frequency, each row showing
//     Freq | SNR | ID | Call | scrolling text
// The text of a lane is right-aligned so the newest characters are always at the right edge
// (like a CW skimmer). One horizontal scroll bar scrolls ALL lanes back through their history;
// scrolled fully right it follows live. A vertical scroll bar appears when there are more lanes
// than fit. Lane bookkeeping lives in TickerModel; feed it with Model.Update(JttyRxMessage).
//
// Click a word in a lane's text  -> WordClicked   (e.g. put it in the Callsign box)
// Click Freq / SNR / ID / Call   -> LaneClicked   (e.g. send <RXFREQ> to WS)
// Wheel scrolls lanes; Shift+Wheel scrolls the text back/forward.

using System.ComponentModel;

namespace XMMTwrapper1
{
    public class TickerControl : Control
    {
        public TickerModel Model { get; } = new();

        // ---- events -------------------------------------------------------------------------

        /// A word in a lane's text was clicked (word is delimited by spaces).
        public event Action<TickerLane, string, MouseEventArgs>? WordClicked;

        /// One of the lane's left-hand columns (Freq, SNR, ID, Call) was clicked.
        public event Action<TickerLane, MouseEventArgs>? LaneClicked;

        /// Right mouse button pressed on a lane (anywhere in the row). The word under the pointer
        /// is passed too, or null if the pointer is not on a word. The lane is also selected
        /// (SelectedLane) and the word kept in ClickedWord, so a ContextMenuStrip assigned to
        /// this control can use those in its item handlers. This fires before the menu opens,
        /// so it is the place to enable/disable items. Right-clicking empty space gives
        /// SelectedLane == null.
        public event Action<TickerLane, string?, MouseEventArgs>? LaneRightClicked;

        // ---- appearance ---------------------------------------------------------------------

        [Category("Appearance"), DefaultValue(true)]
        public bool ShowId { get => _showId; set { _showId = value; RelayoutAndRepaint(); } }

        /// Background of lanes that are inside the QSO window (Rx freq +/- F Tol).
        [Category("Appearance"), DefaultValue(typeof(Color), "LemonChiffon")]
        public Color QsoBackColor { get => _qsoBack; set { _qsoBack = value; Invalidate(); } }

        /// Colour of a message that is still being received (end of message not yet decoded).
        [Category("Appearance"), DefaultValue(typeof(Color), "Gray")]
        public Color PartialTextColor { get => _partial; set { _partial = value; Invalidate(); } }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public TickerLane? SelectedLane { get; private set; }

        /// The word under the pointer at the last mouse press (null if none).
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string? ClickedWord { get; private set; }

        /// Lane resolution: a new message joins a lane whose last frequency is within +/- this many Hz.
        [Category("Behavior"), DefaultValue(2)]
        public int LaneTolerance
        {
            get => Model.FreqTolerance;
            set => Model.FreqTolerance = Math.Max(0, value);
        }

        /// Deletes a lane (and its text).
        public bool RemoveLane(TickerLane lane)
        {
            if (ReferenceEquals(lane, SelectedLane)) SelectedLane = null;
            return Model.RemoveLane(lane);
        }

        /// Deletes the selected lane (the one last clicked), if any.
        public bool RemoveSelectedLane() => SelectedLane != null && RemoveLane(SelectedLane);

        /// Sets the lane's callsign by hand (it then stays until the lane is pruned/cleared).
        public void SetLaneCall(TickerLane lane, string call)
        {
            lane.Call = call;
            lane.CallIsManual = call.Length > 0;
            Invalidate();
        }

        /// Scrolls every lane back to the live (newest) end.
        public void ScrollToLive()
        {
            _follow = true;
            UpdateScrollBars();
            Invalidate();
        }

        // ---- implementation -----------------------------------------------------------------

        private const int PadX = 6;
        private bool _showId = true, _follow = true, _updating;
        private Color _qsoBack = Color.LemonChiffon, _partial = Color.Gray;
        private float _cw = 8f;                 // pixel width of one character (monospace)
        private int _rowH = 18, _headerH = 18;
        private readonly VScrollBar _vbar = new() { Visible = false };
        private readonly HScrollBar _hbar = new();
        private readonly System.Windows.Forms.Timer _pruneTimer = new() { Interval = 30_000 };

        public TickerControl()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.Selectable, true);
            BackColor = SystemColors.Window;
            ForeColor = SystemColors.WindowText;
            Font = new Font(FontFamily.Families.Any(f => f.Name == "Consolas") ? "Consolas" : "Courier New", 10f);

            Controls.Add(_vbar);
            Controls.Add(_hbar);
            _vbar.Scroll += (s, e) => Invalidate();
            _hbar.Scroll += (s, e) => { if (!_updating) { _follow = _hbar.Value >= MaxScroll(); Invalidate(); } };

            Model.Changed += () => { UpdateScrollBars(); Invalidate(); };
            _pruneTimer.Tick += (s, e) => Model.Prune();
            if (!DesignMode && LicenseManager.UsageMode != LicenseUsageMode.Designtime) _pruneTimer.Start();

            MeasureFont();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _pruneTimer.Dispose();
            base.Dispose(disposing);
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            MeasureFont();
            RelayoutAndRepaint();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateScrollBars();
        }

        private void MeasureFont()
        {
            const string probe = "0000000000";
            var sz = TextRenderer.MeasureText(probe, Font, new Size(int.MaxValue, int.MaxValue),
                                              TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
            _cw = Math.Max(1f, sz.Width / (float)probe.Length);
            _rowH = Font.Height + 4;
            _headerH = _rowH;
        }

        private void RelayoutAndRepaint() { UpdateScrollBars(); Invalidate(); }

        // columns (pixels)
        private int FreqW => (int)(5 * _cw) + PadX;
        private int SnrW => (int)(4 * _cw) + PadX;
        private int IdW => _showId ? (int)(6 * _cw) + PadX : 0;
        private int CallW => (int)(10 * _cw) + PadX;
        private int TextLeft => FreqW + SnrW + IdW + CallW + PadX;

        private int ViewW => Math.Max(1, ClientSize.Width - (_vbar.Visible ? _vbar.Width : 0) - TextLeft - PadX);
        private int VisibleRows => Math.Max(1, (ClientSize.Height - _headerH - _hbar.Height) / _rowH);

        private int MaxChars() => Model.Lanes.Count == 0 ? 0 : Model.Lanes.Max(l => l.FullText.Length);
        private int ContentW() => (int)Math.Ceiling(MaxChars() * _cw);
        private int MaxScroll() => Math.Max(0, ContentW() - ViewW);

        private void UpdateScrollBars()
        {
            if (IsDisposed) return;
            _updating = true;
            try
            {
                int bw = SystemInformation.VerticalScrollBarWidth, bh = SystemInformation.HorizontalScrollBarHeight;

                // vertical: only when more lanes than rows
                bool needV = Model.Lanes.Count > Math.Max(1, (ClientSize.Height - _headerH - bh) / _rowH);
                _vbar.Visible = needV;
                _vbar.SetBounds(ClientSize.Width - bw, _headerH, bw, Math.Max(1, ClientSize.Height - _headerH - bh));
                _hbar.SetBounds(0, ClientSize.Height - bh, Math.Max(1, ClientSize.Width - (needV ? bw : 0)), bh);

                if (needV)
                {
                    int rows = VisibleRows;
                    _vbar.Minimum = 0;
                    _vbar.LargeChange = rows;
                    _vbar.SmallChange = 1;
                    _vbar.Maximum = Math.Max(0, Model.Lanes.Count - 1);
                    int maxV = Math.Max(0, Model.Lanes.Count - rows);
                    _vbar.Value = Math.Min(_vbar.Value, maxV);
                }
                else _vbar.Value = 0;

                // horizontal: 0 .. MaxScroll, value MaxScroll = live (newest at the right edge)
                int max = MaxScroll();
                _hbar.Enabled = max > 0;
                _hbar.Minimum = 0;
                _hbar.LargeChange = Math.Max(1, ViewW);
                _hbar.SmallChange = Math.Max(1, (int)(_cw * 4));
                _hbar.Maximum = max + _hbar.LargeChange - 1;
                _hbar.Value = _follow ? max : Math.Min(_hbar.Value, max);
            }
            finally { _updating = false; }
        }

        private int OffsetFromTail() => MaxScroll() - Math.Min(_hbar.Value, MaxScroll());

        // x of the first character of a lane's text
        private float LaneTextX(TickerLane lane, int len) =>
            TextLeft + ViewW - len * _cw + OffsetFromTail();

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(BackColor);

            const TextFormatFlags tf = TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix |
                                       TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter |
                                       TextFormatFlags.Left;
            Color dim = Color.FromArgb(120, ForeColor);

            // header
            int x1 = 0, x2 = FreqW, x3 = x2 + SnrW, x4 = x3 + IdW, x5 = x4 + CallW;
            TextRenderer.DrawText(g, "Freq", Font, new Rectangle(x1 + PadX / 2, 0, FreqW, _headerH), dim, tf);
            TextRenderer.DrawText(g, "SNR", Font, new Rectangle(x2 + PadX / 2, 0, SnrW, _headerH), dim, tf);
            if (_showId) TextRenderer.DrawText(g, "ID", Font, new Rectangle(x3 + PadX / 2, 0, IdW, _headerH), dim, tf);
            TextRenderer.DrawText(g, "Call", Font, new Rectangle(x4 + PadX / 2, 0, CallW, _headerH), dim, tf);
            TextRenderer.DrawText(g, "Text", Font, new Rectangle(x5 + PadX / 2, 0, ViewW, _headerH), dim, tf);
            using (var pen = new Pen(Color.FromArgb(80, ForeColor)))
                g.DrawLine(pen, 0, _headerH - 1, ClientSize.Width, _headerH - 1);

            var lanes = Model.Lanes;
            int first = _vbar.Visible ? _vbar.Value : 0;
            int rows = VisibleRows;
            int viewRight = ClientSize.Width - (_vbar.Visible ? _vbar.Width : 0);

            for (int r = 0; r < rows && first + r < lanes.Count; r++)
            {
                TickerLane lane = lanes[first + r];
                int y = _headerH + r * _rowH;
                var row = new Rectangle(0, y, viewRight, _rowH);

                Color rowBack = lane.InQsoWindow ? _qsoBack : BackColor;
                using (var b = new SolidBrush(rowBack)) g.FillRectangle(b, row);

                // 1) the scrolling text first. GDI text (TextRenderer) does not reliably honour the
                //    Graphics clip region, so this may spill left over the fixed columns...
                string full = lane.FullText;
                float x = LaneTextX(lane, full.Length);
                bool firstSeg = true;
                foreach (var seg in lane.Segments)
                {
                    if (!firstSeg) x += TickerLane.Separator.Length * _cw;
                    firstSeg = false;
                    float w = seg.Text.Length * _cw;
                    if (x + w >= x5 && x <= viewRight)
                        TextRenderer.DrawText(g, seg.Text, Font, new Point((int)x, y + (_rowH - Font.Height) / 2),
                                              seg.Complete ? ForeColor : _partial,
                                              TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.Left);
                    x += w;
                }

                // 2) ...so repaint the fixed Freq/SNR/ID/Call columns on top of it.
                using (var b = new SolidBrush(rowBack)) g.FillRectangle(b, new Rectangle(0, y, x5, _rowH));
                TextRenderer.DrawText(g, lane.Freq.ToString(), Font, new Rectangle(x1 + PadX / 2, y, FreqW, _rowH), ForeColor, tf);
                TextRenderer.DrawText(g, lane.Snr.ToString("+0;-0;0"), Font, new Rectangle(x2 + PadX / 2, y, SnrW, _rowH), ForeColor, tf);
                if (_showId)
                    TextRenderer.DrawText(g, lane.LastMessageId.ToString(), Font, new Rectangle(x3 + PadX / 2, y, IdW, _rowH), dim, tf);
                TextRenderer.DrawText(g, lane.Call, Font, new Rectangle(x4 + PadX / 2, y, CallW, _rowH),
                                      lane.CallIsManual ? ForeColor : dim, tf);
                using (var pen = new Pen(Color.FromArgb(60, ForeColor)))
                    g.DrawLine(pen, x5 - PadX / 2, y, x5 - PadX / 2, y + _rowH - 1);

                if (ReferenceEquals(lane, SelectedLane))
                    using (var pen = new Pen(SystemColors.Highlight))
                        g.DrawRectangle(pen, row.X, row.Y, row.Width - 1, row.Height - 1);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();

            ClickedWord = null;
            int rowIndex = (e.Y - _headerH) / _rowH;
            int idx = (_vbar.Visible ? _vbar.Value : 0) + rowIndex;
            if (e.Y < _headerH || rowIndex < 0 || rowIndex >= VisibleRows || idx >= Model.Lanes.Count)
            {
                SelectedLane = null;                 // pressed on empty space
                Invalidate();
                return;
            }

            TickerLane lane = Model.Lanes[idx];
            SelectedLane = lane;
            Invalidate();

            bool inLeftColumns = e.X < TextLeft - PadX;
            if (!inLeftColumns)
            {
                string full = lane.FullText;
                int ci = (int)Math.Floor((e.X - LaneTextX(lane, full.Length)) / _cw);
                if (ci >= 0 && ci < full.Length && full[ci] != ' ')
                {
                    int a = ci, b = ci;
                    while (a > 0 && full[a - 1] != ' ') a--;
                    while (b < full.Length - 1 && full[b + 1] != ' ') b++;
                    ClickedWord = full.Substring(a, b - a + 1);
                }
            }

            if (e.Button == MouseButtons.Right)
            {
                LaneRightClicked?.Invoke(lane, ClickedWord, e);
                return;
            }

            if (inLeftColumns) LaneClicked?.Invoke(lane, e);
            else if (ClickedWord != null) WordClicked?.Invoke(lane, ClickedWord, e);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if ((ModifierKeys & Keys.Shift) == Keys.Shift)
            {
                if (!_hbar.Enabled) return;
                int step = (int)(_cw * 8) * Math.Sign(e.Delta);          // wheel up = back in time
                int max = MaxScroll();
                _hbar.Value = Math.Max(0, Math.Min(max, _hbar.Value - step));
                _follow = _hbar.Value >= max;
                Invalidate();
            }
            else if (_vbar.Visible)
            {
                int maxV = Math.Max(0, Model.Lanes.Count - VisibleRows);
                _vbar.Value = Math.Max(0, Math.Min(maxV, _vbar.Value - Math.Sign(e.Delta) * 2));
                Invalidate();
            }
        }
    }
}
