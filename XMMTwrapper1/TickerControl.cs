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

        /// Timeline view: a second title row shows a UTC time ruler and every message is drawn as a
        /// block placed at its start time (and as wide as it lasted), so frames on different lanes
        /// line up in time. Off = the plain right-aligned scrolling text.
        [Category("Appearance"), DefaultValue(false)]
        public bool TimelineMode
        {
            get => _timeline;
            set
            {
                if (_timeline == value) return;
                _timeline = value;
                _follow = true;
                _now = DateTime.UtcNow;
                if (!DesignMode) { if (value) _tickTimer.Start(); else _tickTimer.Stop(); }
                RelayoutAndRepaint();
            }
        }

        /// Timeline scale in pixels per second (8..400). Ctrl+Wheel zooms. About 50 shows RTTY at
        /// roughly its real speed; smaller squeezes more time on screen.
        [Category("Appearance"), DefaultValue(50)]
        public int TimelinePixelsPerSecond
        {
            get => _pps;
            set { _pps = Math.Max(4, Math.Min(400, value)); _now = DateTime.UtcNow; RelayoutAndRepaint(); }
        }

        /// Faint vertical lines on the lanes at the ruler's tick marks (timeline mode).
        [Category("Appearance"), DefaultValue(true)]
        public bool ShowTimelineGrid { get => _grid; set { _grid = value; Invalidate(); } }

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

        // ---- Clipboard ---------------------------------------------------------------

        /// One lane as a text line: "freq  snr  call  text" (tab separated, so it pastes
        /// into Excel as columns). Call is blank if not set.
        public static string LaneToText(TickerLane lane) =>
            $"{lane.Freq}\t{lane.Snr}\t{lane.Call}\t{lane.FullText}";

        /// All lanes (in the order shown), one per line, with a header row if asked.
        public string AllLanesToText(bool header = false)
        {
            var sb = new System.Text.StringBuilder();
            if (header) sb.AppendLine("Freq\tSNR\tCall\tText");
            foreach (var l in Model.Lanes) sb.AppendLine(LaneToText(l));
            return sb.ToString();
        }

        /// Copies one lane to the clipboard. Returns false if the clipboard was busy.
        public bool CopyLane(TickerLane lane) => SetClipboard(LaneToText(lane));

        /// Copies the selected lane (last clicked). False if none selected.
        public bool CopySelectedLane() => SelectedLane != null && CopyLane(SelectedLane);

        /// Copies only the text of a lane (no freq/snr/call columns).
        public bool CopyLaneTextOnly(TickerLane lane) => SetClipboard(lane.FullText);

        /// Copies every lane to the clipboard. False if there are none or clipboard was busy.
        public bool CopyAllLanes(bool header = false) =>
            Model.Lanes.Count > 0 && SetClipboard(AllLanesToText(header));

        private static bool SetClipboard(string text)
        {
            try { Clipboard.SetText(text); return true; }
            catch (System.Runtime.InteropServices.ExternalException) { return false; } // clipboard locked
            catch (System.Threading.ThreadStateException) { return false; }
        }

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
        private bool _showId = true, _follow = true, _updating, _timeline, _grid = true;
        private int _pps = 50;                  // timeline pixels per second
        private DateTime _now = DateTime.UtcNow;   // refreshed at each entry point so a paint is consistent
        private DateTime _anchor = DateTime.UtcNow; // time at the right edge while scrolled back
        private Color _qsoBack = Color.LemonChiffon, _partial = Color.Gray;
        private float _cw = 8f;                 // pixel width of one character (monospace)
        private int _rowH = 18;
        private int HeaderH => _rowH * (_timeline ? 2 : 1);   // timeline mode has a second title row
        private readonly VScrollBar _vbar = new() { Visible = false };
        private readonly HScrollBar _hbar = new();
        private readonly System.Windows.Forms.Timer _pruneTimer = new() { Interval = 30_000 };
        private readonly System.Windows.Forms.Timer _settleTimer = new() { Interval = 5_000 };
        private readonly System.Windows.Forms.Timer _tickTimer = new() { Interval = 200 };   // timeline moves with the clock

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
            _hbar.Scroll += (s, e) => { if (!_updating) SetHScroll(_hbar.Value); };

            Model.Changed += () => { UpdateScrollBars(); Invalidate(); };
            _pruneTimer.Tick += (s, e) => Model.Prune();
            _settleTimer.Tick += (s, e) => Model.Settle();
            _tickTimer.Tick += (s, e) => { if (_timeline) { UpdateScrollBars(); if (_follow) Invalidate(); } };   // paused view does not move
            if (!DesignMode && LicenseManager.UsageMode != LicenseUsageMode.Designtime) { _pruneTimer.Start(); _settleTimer.Start(); }

            MeasureFont();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { _pruneTimer.Dispose(); _settleTimer.Dispose(); _tickTimer.Dispose(); }
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
        }

        private void RelayoutAndRepaint() { UpdateScrollBars(); Invalidate(); }

        // columns (pixels)
        private int FreqW => (int)(5 * _cw) + PadX;
        private int SnrW => (int)(4 * _cw) + PadX;
        private int IdW => _showId ? (int)(6 * _cw) + PadX : 0;
        private int CallW => (int)(10 * _cw) + PadX;
        private int TextLeft => FreqW + SnrW + IdW + CallW + PadX;

        private int ViewW => Math.Max(1, ClientSize.Width - (_vbar.Visible ? _vbar.Width : 0) - TextLeft - PadX);
        private int VisibleRows => Math.Max(1, (ClientSize.Height - HeaderH - _hbar.Height) / _rowH);

        private int MaxChars() => Model.Lanes.Count == 0 ? 0 : Model.Lanes.Max(l => l.FullText.Length);
        private int ContentW()
        {
            if (!_timeline) return (int)Math.Ceiling(MaxChars() * _cw);
            DateTime start = Model.EarliestStart() ?? _now;      // from the oldest message to now
            return (int)Math.Ceiling(Math.Max(0, (_now - start).TotalSeconds) * _pps);
        }
        private int MaxScroll() => Math.Max(0, ContentW() - ViewW);

        // Timeline: time shown at the right edge of the view (now while following live).
        private DateTime RightEdge => _follow ? _now : _anchor;
        private float TimeX(DateTime t) => TickerTimeline.TimeToX(t, RightEdge, TextLeft + ViewW, _pps);

        /// Applies a horizontal scroll position (from the scroll bar or the wheel).
        private void SetHScroll(int value)
        {
            _now = DateTime.UtcNow;
            int max = MaxScroll();
            value = Math.Max(0, Math.Min(max, value));
            _follow = value >= max;
            if (_timeline && !_follow) _anchor = _now - TimeSpan.FromSeconds((max - value) / (double)_pps);
            _updating = true;
            try { _hbar.Value = value; } finally { _updating = false; }
            Invalidate();
        }

        private void UpdateScrollBars()
        {
            if (IsDisposed) return;
            _now = DateTime.UtcNow;
            _updating = true;
            try
            {
                int bw = SystemInformation.VerticalScrollBarWidth, bh = SystemInformation.HorizontalScrollBarHeight;

                // vertical: only when more lanes than rows
                bool needV = Model.Lanes.Count > Math.Max(1, (ClientSize.Height - HeaderH - bh) / _rowH);
                _vbar.Visible = needV;
                _vbar.SetBounds(ClientSize.Width - bw, HeaderH, bw, Math.Max(1, ClientSize.Height - HeaderH - bh));
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
                int val;
                if (_follow) val = max;
                else if (_timeline) val = (int)Math.Max(0, Math.Min(max, max - (_now - _anchor).TotalSeconds * _pps));
                else val = Math.Min(_hbar.Value, max);
                _hbar.Value = val;
            }
            finally { _updating = false; }
        }

        private int OffsetFromTail() => MaxScroll() - Math.Min(_hbar.Value, MaxScroll());   // character mode

        // x of the first character of a lane's text (character mode)
        private float LaneTextX(TickerLane lane, int len) =>
            TextLeft + ViewW - len * _cw + OffsetFromTail();

        // One message of a lane laid out on the timeline.
        private readonly record struct Placed(TickerSegment Seg, float X0, float X1, string Shown);

        /// Timeline layout of a lane: each message starts at its start time and is as wide as it
        /// lasted - but never narrower than its text, and never running into the next message.
        private List<Placed> LayoutTimeline(TickerLane lane)
        {
            var segs = lane.Segments.OrderBy(sg => sg.Start).ToList();
            var list = new List<Placed>(segs.Count);
            for (int i = 0; i < segs.Count; i++)
            {
                var sg = segs[i];
                float x0 = TimeX(sg.Start);
                float xTime = TimeX(sg.LastUpdate > sg.Start ? sg.LastUpdate : sg.Start);
                float limit = i + 1 < segs.Count ? TimeX(segs[i + 1].Start) - 2 : float.MaxValue;
                float x1 = Math.Max(xTime, Math.Min(x0 + sg.Text.Length * _cw + 4, limit));
                x1 = Math.Max(x1, x0 + 3);

                string shown = sg.Text;
                int fit = (int)((x1 - x0 - 4) / _cw);
                if (fit < shown.Length)
                    shown = fit >= 2 ? shown.Substring(0, fit - 1) + "\u2026" : "";
                list.Add(new Placed(sg, x0, x1, shown));
            }
            return list;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(BackColor);
            _now = DateTime.UtcNow;

            const TextFormatFlags tf = TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix |
                                       TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter |
                                       TextFormatFlags.Left;
            Color dim = Color.FromArgb(120, ForeColor);

            int x1 = 0, x2 = FreqW, x3 = x2 + SnrW, x4 = x3 + IdW, x5 = x4 + CallW;
            int viewRight = ClientSize.Width - (_vbar.Visible ? _vbar.Width : 0);

            // time ruler ticks (timeline mode) - drawn in the 2nd title row and as faint grid lines
            var ticks = new List<(float X, string Label)>();
            if (_timeline)
            {
                int step = TickerTimeline.ChooseStep(_pps, (int)(8 * _cw) + 30);
                double span = (viewRight - TextLeft) / (double)_pps;
                DateTime right = RightEdge;
                foreach (var t in TickerTimeline.Ticks(right.AddSeconds(-span - step), right.AddSeconds(step), step))
                {
                    float x = TimeX(t);
                    if (x >= x5 && x <= viewRight) ticks.Add((x, TickerTimeline.Label(t, step)));
                }
            }

            // title row(s): the scrolling parts first, the fixed column titles on top afterwards
            if (_timeline)
            {
                string title = $"Timeline  UTC   {_pps} px/s  (Ctrl+wheel zooms)" + (_follow ? "" : "   [paused - scroll right for live]");
                TextRenderer.DrawText(g, title, Font, new Rectangle(x5 + PadX / 2, 0, ViewW, _rowH), dim, tf);
                using (var pen = new Pen(Color.FromArgb(120, ForeColor)))
                    foreach (var (tx, label) in ticks)
                    {
                        g.DrawLine(pen, tx, _rowH + _rowH / 2, tx, HeaderH - 2);
                        TextRenderer.DrawText(g, label, Font, new Rectangle((int)tx + 2, _rowH, (int)(10 * _cw), _rowH), ForeColor, tf);
                    }
                using (var b = new SolidBrush(BackColor)) g.FillRectangle(b, new Rectangle(0, 0, x5, HeaderH));
                TextRenderer.DrawText(g, "UTC", Font, new Rectangle(x1 + PadX / 2, _rowH, FreqW, _rowH), dim, tf);
            }
            else
            {
                TextRenderer.DrawText(g, "Text", Font, new Rectangle(x5 + PadX / 2, 0, ViewW, _rowH), dim, tf);
            }
            TextRenderer.DrawText(g, "Freq", Font, new Rectangle(x1 + PadX / 2, 0, FreqW, _rowH), dim, tf);
            TextRenderer.DrawText(g, "SNR", Font, new Rectangle(x2 + PadX / 2, 0, SnrW, _rowH), dim, tf);
            if (_showId) TextRenderer.DrawText(g, "ID", Font, new Rectangle(x3 + PadX / 2, 0, IdW, _rowH), dim, tf);
            TextRenderer.DrawText(g, "Call", Font, new Rectangle(x4 + PadX / 2, 0, CallW, _rowH), dim, tf);
            using (var pen = new Pen(Color.FromArgb(80, ForeColor)))
                g.DrawLine(pen, 0, HeaderH - 1, ClientSize.Width, HeaderH - 1);

            var lanes = Model.Lanes;
            int first = _vbar.Visible ? _vbar.Value : 0;
            int rows = VisibleRows;

            for (int r = 0; r < rows && first + r < lanes.Count; r++)
            {
                TickerLane lane = lanes[first + r];
                int y = HeaderH + r * _rowH;
                var row = new Rectangle(0, y, viewRight, _rowH);

                Color rowBack = lane.InQsoWindow ? _qsoBack : BackColor;
                using (var b = new SolidBrush(rowBack)) g.FillRectangle(b, row);

                // 1) the scrolling part first. GDI text (TextRenderer) does not reliably honour the
                //    Graphics clip region, so this may spill left over the fixed columns...
                if (_timeline)
                {
                    if (_grid)
                        using (var pen = new Pen(Color.FromArgb(28, ForeColor)))
                            foreach (var (tx, _) in ticks) g.DrawLine(pen, tx, y, tx, y + _rowH - 1);

                    foreach (var pl in LayoutTimeline(lane))
                    {
                        if (pl.X1 < x5 || pl.X0 > viewRight) continue;
                        Color tint = pl.Seg.Complete ? ForeColor : _partial;
                        using (var b = new SolidBrush(Color.FromArgb(34, tint)))
                            g.FillRectangle(b, pl.X0, y + 1, pl.X1 - pl.X0, _rowH - 2);
                        using (var pen = new Pen(Color.FromArgb(150, tint)))
                            g.DrawLine(pen, pl.X0, y + 1, pl.X0, y + _rowH - 2);      // start marker
                        if (pl.Shown.Length > 0)
                            TextRenderer.DrawText(g, pl.Shown, Font,
                                                  new Point((int)pl.X0 + 3, y + (_rowH - Font.Height) / 2), tint,
                                                  TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.Left);
                    }
                }
                else
                {
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
            int rowIndex = (e.Y - HeaderH) / _rowH;
            int idx = (_vbar.Visible ? _vbar.Value : 0) + rowIndex;
            if (e.Y < HeaderH || rowIndex < 0 || rowIndex >= VisibleRows || idx >= Model.Lanes.Count)
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
                if (_timeline)
                {
                    _now = DateTime.UtcNow;
                    foreach (var pl in LayoutTimeline(lane))
                    {
                        if (e.X < pl.X0 || e.X >= pl.X1) continue;
                        int cj = (int)Math.Floor((e.X - pl.X0 - 3) / _cw);
                        if (cj >= 0 && cj < pl.Shown.Length && pl.Shown[cj] != '\u2026')
                            ClickedWord = WordAt(pl.Seg.Text, cj);
                    }
                }
                else
                {
                    string full = lane.FullText;
                    int ci = (int)Math.Floor((e.X - LaneTextX(lane, full.Length)) / _cw);
                    ClickedWord = WordAt(full, ci);
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

        /// The space-delimited word containing character index `ci`, or null.
        private static string? WordAt(string text, int ci)
        {
            if (ci < 0 || ci >= text.Length || text[ci] == ' ') return null;
            int a = ci, b = ci;
            while (a > 0 && text[a - 1] != ' ') a--;
            while (b < text.Length - 1 && text[b + 1] != ' ') b++;
            return text.Substring(a, b - a + 1);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (_timeline && (ModifierKeys & Keys.Control) == Keys.Control)
            {
                // zoom; the time at the right edge stays put
                TimelinePixelsPerSecond = e.Delta > 0 ? (int)Math.Ceiling(_pps * 1.25) : (int)(_pps / 1.25);
            }
            else if ((ModifierKeys & Keys.Shift) == Keys.Shift)
            {
                if (!_hbar.Enabled) return;
                int step = (int)(_cw * 8) * Math.Sign(e.Delta);          // wheel up = back in time
                SetHScroll(_hbar.Value - step);
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
