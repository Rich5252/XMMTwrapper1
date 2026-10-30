// TickerModel.cs
//
// Lane bookkeeping for the JTTY "ticker tape" display: one lane per signal (frequency),
// each lane a scrolling stream of the messages decoded there.
//
// A message is identified by its WS message id and arrives repeatedly with growing text
// (JttyRxMessage). New messages are assigned to a lane by FREQUENCY: the nearest lane whose
// last frequency is within FreqTolerance Hz, else a new lane. The lane's frequency follows
// the latest message, so a slowly drifting station stays on one lane. Pure logic - no WinForms.

namespace XMMTwrapper1
{
    public sealed class TickerSegment
    {
        public long MessageId { get; internal set; }
        public string Text { get; internal set; } = "";
        public bool Complete { get; internal set; }
    }

    public sealed class TickerLane
    {
        private readonly List<TickerSegment> _segments = new();

        public int Number { get; internal set; }                 // stable lane number (1, 2, 3 ...)
        public int Freq { get; internal set; }                   // Hz, of the latest message
        public int Snr { get; internal set; }
        public long LastMessageId { get; internal set; }
        public string Time { get; internal set; } = "";          // start time of the latest message
        public bool InQsoWindow { get; internal set; }
        public DateTime LastActivity { get; internal set; }

        /// Callsign shown for the lane. Set by the user clicking a word (CallIsManual) or guessed.
        public string Call { get; set; } = "";
        public bool CallIsManual { get; set; }

        public IReadOnlyList<TickerSegment> Segments => _segments;
        internal List<TickerSegment> SegmentList => _segments;

        internal const string Separator = "  ";

        /// The lane's whole text: the messages in order, separated by two spaces.
        public string FullText => string.Join(Separator, _segments.Select(s => s.Text));
    }

    public sealed class TickerModel
    {
        /// A new message within this many Hz (+/-) of a lane's last frequency joins that lane;
        /// otherwise it starts a new lane. Small values separate crowded stations; too small a
        /// value splits one drifting station across lanes.
        public int FreqTolerance { get; set; } = 2;

        /// Idle lanes (no update for this long) are dropped by Prune(), unless pinned by a manual call.
        public TimeSpan LaneTimeout { get; set; } = TimeSpan.FromMinutes(10);

        public int MaxLanes { get; set; } = 40;
        public int MaxSegmentsPerLane { get; set; } = 60;

        /// Optional: returns a callsign for a word, or "" if the word is not one
        /// (e.g. w => ITU_Prefix_Checker.IsCallsign(w)).
        public Func<string, string?>? CallFinder { get; set; }

        /// Raised after any change to the lanes.
        public event Action? Changed;

        private readonly List<TickerLane> _lanes = new();                       // sorted by Freq
        private readonly Dictionary<long, TickerLane> _laneByMessage = new();
        private int _nextLaneNumber = 1;

        public IReadOnlyList<TickerLane> Lanes => _lanes;

        public void Clear()
        {
            _lanes.Clear();
            _laneByMessage.Clear();
            _nextLaneNumber = 1;
            Changed?.Invoke();
        }

        /// Applies one RXMSG update. Returns the lane it landed on.
        public TickerLane Update(JttyRxMessage m, DateTime? now = null)
        {
            DateTime t = now ?? DateTime.UtcNow;

            TickerLane? lane = null;
            TickerSegment? seg = null;

            if (_laneByMessage.TryGetValue(m.Id, out lane))
            {
                seg = lane.SegmentList.FirstOrDefault(s => s.MessageId == m.Id);
                // Same id but far from its lane (WS restarted its ids?) - treat as a new message.
                if (seg == null || Math.Abs(lane.Freq - m.Freq) > Math.Max(50, 5 * FreqTolerance))
                {
                    _laneByMessage.Remove(m.Id);
                    lane = null;
                    seg = null;
                }
            }

            if (lane == null)
            {
                lane = FindLane(m.Freq) ?? NewLane(m.Freq, t);
                seg = new TickerSegment { MessageId = m.Id };
                lane.SegmentList.Add(seg);
                if (lane.SegmentList.Count > MaxSegmentsPerLane)
                {
                    foreach (var old in lane.SegmentList.Take(lane.SegmentList.Count - MaxSegmentsPerLane))
                        _laneByMessage.Remove(old.MessageId);
                    lane.SegmentList.RemoveRange(0, lane.SegmentList.Count - MaxSegmentsPerLane);
                }
                _laneByMessage[m.Id] = lane;
            }

            seg!.Text = m.Text;
            seg.Complete = m.Complete;

            lane.Freq = m.Freq;
            lane.Snr = m.Snr;
            lane.Time = m.Time;
            lane.LastMessageId = m.Id;
            lane.InQsoWindow = m.InQsoWindow;
            lane.LastActivity = t;

            if (!lane.CallIsManual) GuessCall(lane, m.Text);

            _lanes.Sort((a, b) => a.Freq != b.Freq ? a.Freq.CompareTo(b.Freq) : a.Number.CompareTo(b.Number));
            Prune(t, raise: false);
            Changed?.Invoke();
            return lane;
        }

        /// Deletes a lane and its text. If a message that was still being received on it sends
        /// another update, it reappears as a new lane (with the full text so far).
        public bool RemoveLane(TickerLane lane)
        {
            if (!_lanes.Remove(lane)) return false;
            ForgetMessages(lane);
            Changed?.Invoke();
            return true;
        }

        /// Drops idle lanes. Lanes with a manually set call stay.
        public void Prune(DateTime? now = null, bool raise = true)
        {
            DateTime t = now ?? DateTime.UtcNow;
            int removed = _lanes.RemoveAll(l =>
            {
                bool drop = !l.CallIsManual && t - l.LastActivity > LaneTimeout;
                if (drop) ForgetMessages(l);
                return drop;
            });

            // Over the limit: drop the least recently active lanes (never the one just updated).
            while (_lanes.Count > MaxLanes)
            {
                TickerLane oldest = _lanes.OrderBy(l => l.LastActivity).First();
                _lanes.Remove(oldest);
                ForgetMessages(oldest);
                removed++;
            }
            if (raise && removed > 0) Changed?.Invoke();
        }

        private void ForgetMessages(TickerLane l)
        {
            foreach (var s in l.SegmentList) _laneByMessage.Remove(s.MessageId);
        }

        private TickerLane? FindLane(int freq)
        {
            TickerLane? best = null;
            int bestDist = int.MaxValue;
            foreach (var l in _lanes)
            {
                int d = Math.Abs(l.Freq - freq);
                if (d <= FreqTolerance && d < bestDist) { best = l; bestDist = d; }
            }
            return best;
        }

        private TickerLane NewLane(int freq, DateTime t)
        {
            var l = new TickerLane { Number = _nextLaneNumber++, Freq = freq, LastActivity = t };
            _lanes.Add(l);
            return l;
        }

        // First callsign-like word of the message; the word after "DE" wins (the sender).
        private void GuessCall(TickerLane lane, string text)
        {
            if (CallFinder == null) return;
            string[] words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            int de = Array.FindIndex(words, w => w == "DE");
            if (de >= 0 && de + 1 < words.Length)
            {
                string c = CallFinder(words[de + 1]) ?? "";
                if (c.Length > 0) { lane.Call = c; return; }
            }
            foreach (string w in words)
            {
                string c = CallFinder(w) ?? "";
                if (c.Length > 0) { lane.Call = c; return; }
            }
        }
    }
}
