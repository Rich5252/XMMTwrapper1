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
        public DateTime Start { get; internal set; }             // UTC, message start (from WS hhmmss)
        public DateTime LastUpdate { get; internal set; }        // UTC, when the latest text arrived
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
                seg = new TickerSegment { MessageId = m.Id, Start = ParseMessageTime(m.Time, t) };
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
            seg.LastUpdate = t;

            lane.Freq = m.Freq;
            lane.Snr = m.Snr;
            lane.Time = m.Time;
            lane.LastMessageId = m.Id;
            lane.InQsoWindow = m.InQsoWindow;
            lane.LastActivity = t;

            if (!lane.CallIsManual)
            {
                // A message that has been followed by another on this lane is finished, so its last
                // word is whole even if it never got an end-of-message flag. Then the current one.
                int si = lane.SegmentList.IndexOf(seg);
                if (si > 0) GuessCall(lane, lane.SegmentList[si - 1].Text, true);
                GuessCall(lane, m.Text, m.Complete);
            }

            _lanes.Sort((a, b) => a.Freq != b.Freq ? a.Freq.CompareTo(b.Freq) : a.Number.CompareTo(b.Number));
            Prune(t, raise: false);
            Changed?.Invoke();
            return lane;
        }

        /// Treats the last message of any lane that has been quiet for `idle` (default 6 s) as
        /// finished, so its final word counts when guessing the call (a message often just stops
        /// without an end-of-message flag). Call it from a timer; returns true if a call changed.
        public bool Settle(DateTime? now = null, TimeSpan? idle = null)
        {
            DateTime t = now ?? DateTime.UtcNow;
            TimeSpan limit = idle ?? TimeSpan.FromSeconds(6);
            bool changed = false;
            foreach (var lane in _lanes)
            {
                if (lane.CallIsManual || lane.SegmentList.Count == 0) continue;
                var last = lane.SegmentList[lane.SegmentList.Count - 1];
                if (last.Complete || t - lane.LastActivity < limit) continue;
                string before = lane.Call;
                GuessCall(lane, last.Text, true);
                if (lane.Call != before) changed = true;
            }
            if (changed) Changed?.Invoke();
            return changed;
        }

        /// The start time of the oldest message still held (UTC), or null if there is none.
        public DateTime? EarliestStart()
        {
            DateTime? min = null;
            foreach (var l in _lanes)
                foreach (var sg in l.SegmentList)
                    if (min == null || sg.Start < min) min = sg.Start;
            return min;
        }

        /// WS sends the message start as hhmmss UTC. Converts it to a DateTime next to `nowUtc`:
        /// handles midnight rollover, never returns the future, and falls back to `nowUtc` if the
        /// text is bad or more than 30 minutes old (a live message cannot be that old - this
        /// happens when WS replays a recording).
        public static DateTime ParseMessageTime(string? hhmmss, DateTime nowUtc)
        {
            nowUtc = DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc);
            if (hhmmss == null || hhmmss.Length != 6 ||
                !int.TryParse(hhmmss.AsSpan(0, 2), out int h) ||
                !int.TryParse(hhmmss.AsSpan(2, 2), out int mi) ||
                !int.TryParse(hhmmss.AsSpan(4, 2), out int sec) ||
                h > 23 || mi > 59 || sec > 59) return nowUtc;
            DateTime d = nowUtc.Date.AddHours(h).AddMinutes(mi).AddSeconds(sec);
            if (d > nowUtc.AddMinutes(1)) d = d.AddDays(-1);        // just after midnight
            if (d > nowUtc) d = nowUtc;
            if (nowUtc - d > TimeSpan.FromMinutes(30)) d = nowUtc;
            return d;
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

        // Strict shape check: 1-3 char prefix (must start with a letter, or digit+letter), one digit,
        // then 1-3 letters, optional /suffix.  Rejects glued/garbled decodes such as F4JKYCQ or
        // G5BAQTU (suffix of 5 letters), and numbers/words such as 599, 40W, TS890, RST5.
        private static readonly System.Text.RegularExpressions.Regex CallShape = new(
            @"^(?:[A-Z][A-Z0-9]?[A-Z0-9]?|[0-9][A-Z][A-Z0-9]?)[0-9][A-Z]{1,3}(?:/[A-Z0-9]{1,4})?$",
            System.Text.RegularExpressions.RegexOptions.Compiled);

        public static bool LooksLikeCall(string word) => CallShape.IsMatch(word);

        /// Your own call(s). A message that is YOUR transmission (the decoder hears it too) must not
        /// set the lane's call, so these are never taken as the sender. Case-insensitive; a
        /// portable form (G4AHN/P) matches too.  e.g. Model.IgnoreCalls.Add("G4AHN");
        public ISet<string> IgnoreCalls { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // A call straight after one of these is who is being addressed, not the sender
        // ("TU NOW G4AHN 599 011" = next caller). Compared with the preceding word.
        private static readonly HashSet<string> NotSenderCues = new() { "NOW", "TO" };

        private bool IsIgnored(string w)
        {
            if (IgnoreCalls.Count == 0) return false;
            if (IgnoreCalls.Contains(w)) return true;
            int slash = w.IndexOf('/');
            return slash > 0 && IgnoreCalls.Contains(w.Substring(0, slash));
        }

        // A word counts as a call only if it has the right shape AND (when set) CallFinder accepts it.
        // CallFinder MUST be stateless (whole word in, bool-ish out). Do not wire in the char-stream
        // ITU_Prefix_Checker.IsCallsign here: it keeps a static buffer and glues words together.
        // The word itself is used, never CallFinder's return value.
        private bool IsCall(string w) =>
            LooksLikeCall(w) && (CallFinder == null || !string.IsNullOrEmpty(CallFinder(w)));

        // Whole words only. Rules, strongest first (the latest match wins):
        //   1. DE <call>              -> sender
        //   2. CQ [DX|TEST|..] <call> -> sender
        //   3. <call> CQ              -> sender
        //   4. <call> <call> ...      -> second is the sender (CALLED-STN SENDER 599 nnn)
        //   5. line is only a call (or the same call repeated)
        // Never the sender: your own calls (IgnoreCalls), a call right after NOW/TO, and any word
        // next to a "..." (the decoder lost characters there, so the word may be cut short:
        // "CQ DE F4MH ... K" is not F4MH).
        // Each line of the text (CR/LF separated) is judged on its own, in order. No match ->
        // the lane keeps its previous guess.  While a message is still being received its last
        // word may be unfinished, so it is ignored unless the message is complete.
        private void GuessCall(TickerLane lane, string text, bool complete)
        {
            string[] lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            bool unfinished = !complete && text.Length > 0 && !char.IsWhiteSpace(text[text.Length - 1]);
            for (int li = 0; li < lines.Length; li++)
            {
                string? call = GuessCallInLine(lines[li], unfinished && li == lines.Length - 1);
                if (call != null) lane.Call = call;
            }
        }

        private string? GuessCallInLine(string line, bool dropLast)
        {
            string[] tok = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            int n = tok.Length - (dropLast && tok.Length > 0 ? 1 : 0);      // words we may use
            if (n == 0) return null;

            string[] w = tok.Select(x => x.Trim(',', '.', ':', ';', '!', '?')).ToArray();
            bool[] gap = tok.Select(x => x.Contains("...")).ToArray();
            var valid = new bool[n];       // looks like a call
            var callish = new bool[n];     // ...and is whole (not next to a decoder gap)
            var cand = new bool[n];        // ...and can be the sender
            var mine = new bool[n];        // ...but is one of our own calls
            for (int i = 0; i < n; i++)
            {
                valid[i] = !gap[i] && IsCall(w[i]);
                callish[i] = valid[i] && !(i > 0 && gap[i - 1]) && !(i + 1 < tok.Length && gap[i + 1]);
                mine[i] = callish[i] && IsIgnored(w[i]);
                cand[i] = callish[i] && !mine[i] && !(i > 0 && NotSenderCues.Contains(w[i - 1]));
            }

            // 1. DE <call>
            for (int i = n - 2; i >= 0; i--)
                if (w[i] == "DE" && cand[i + 1]) return w[i + 1];

            // 2. CQ <call>  (up to two filler words between, e.g. CQ DX CALL, CQ TEST CALL)
            for (int i = n - 2; i >= 0; i--)
            {
                if (w[i] != "CQ") continue;
                for (int j = i + 1; j < n && j <= i + 3; j++)
                {
                    if (w[j] == "CQ" || w[j] == "DE") break;
                    if (valid[j]) { if (cand[j]) return w[j]; break; }
                }
            }

            // 3. <call> CQ
            for (int i = n - 1; i >= 1; i--)
                if (w[i] == "CQ" && cand[i - 1]) return w[i - 1];

            // 4. <call> <call>
            for (int i = n - 1; i >= 1; i--)
                if (valid[i - 1] && cand[i]) return w[i];     // first may be cut by a gap, the sender must be whole

            // 5. only a call (maybe repeated)
            if (cand[0] && Enumerable.Range(0, n).All(i => callish[i]) && w.Take(n).Distinct().Count() == 1)
                return w[0];

            return null;
        }
    }

    /// Pure helpers for the timeline view (kept out of the control so they can be tested).
    public static class TickerTimeline
    {
        private static readonly int[] Steps = { 1, 2, 5, 10, 15, 30, 60, 120, 300, 600, 900, 1800, 3600 };
        private static readonly DateTime Epoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// Smallest tick spacing in seconds that is at least `minPixels` wide at `pxPerSec`.
        public static int ChooseStep(double pxPerSec, int minPixels)
        {
            foreach (int st in Steps) if (st * pxPerSec >= minPixels) return st;
            return Steps[Steps.Length - 1];
        }

        /// Tick times in [from, to], aligned to multiples of `stepSec` of UTC time.
        public static IEnumerable<DateTime> Ticks(DateTime from, DateTime to, int stepSec)
        {
            if (stepSec <= 0 || to < from) yield break;
            long first = (long)Math.Ceiling((from - Epoch).TotalSeconds / stepSec) * stepSec;
            for (long sec = first; ; sec += stepSec)
            {
                DateTime t = Epoch.AddSeconds(sec);
                if (t > to) yield break;
                yield return t;
            }
        }

        public static string Label(DateTime t, int stepSec) =>
            t.ToString(stepSec < 60 ? "HH:mm:ss" : "HH:mm", System.Globalization.CultureInfo.InvariantCulture);

        /// x position of time `t` when the right edge of the view (x = rightX) shows `rightEdge`.
        public static float TimeToX(DateTime t, DateTime rightEdge, float rightX, double pxPerSec) =>
            rightX - (float)((rightEdge - t).TotalSeconds * pxPerSec);
    }
}
