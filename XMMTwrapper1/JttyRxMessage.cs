// JttyRxMessage.cs
//
// One <RXMSG:len>id|hhmmss|freq|snr|flags|text frame from WS (needs the ws-rxmsg-tcp patch and
// WS started with --n1mm-rxmsg). Unlike RXTEXT, which carries only the characters added since
// the last update, RXMSG carries the FULL text so far of one message, tagged with its id.
// The receiver replaces the message with that id, so messages on different frequencies can
// never be interleaved or scrambled.
//
//   flags bit 0 (1): message complete (end-of-message decoded)
//   flags bit 1 (2): message is inside the QSO window (Rx freq +/- F Tol)

using System.Globalization;

namespace XMMTwrapper1
{
    public sealed record JttyRxMessage(
        long Id,
        string Time,          // hhmmss UTC of the message start
        int Freq,             // audio frequency, Hz
        int Snr,
        bool Complete,
        bool InQsoWindow,
        string Text)
    {
        /// Parses the data part of an RXMSG frame. Returns null if it is malformed.
        /// The text is the last field and may itself contain '|'.
        public static JttyRxMessage? TryParse(string? data)
        {
            if (string.IsNullOrEmpty(data)) return null;

            string[] f = data.Split('|', 6);
            if (f.Length < 6) return null;

            var inv = CultureInfo.InvariantCulture;
            if (!long.TryParse(f[0], NumberStyles.None, inv, out long id)) return null;
            if (!int.TryParse(f[2], NumberStyles.AllowLeadingSign, inv, out int freq)) return null;
            if (!int.TryParse(f[3], NumberStyles.AllowLeadingSign, inv, out int snr)) return null;
            if (!int.TryParse(f[4], NumberStyles.None, inv, out int flags)) return null;

            return new JttyRxMessage(id, f[1], freq, snr, (flags & 1) != 0, (flags & 2) != 0, f[5]);
        }
    }
}
