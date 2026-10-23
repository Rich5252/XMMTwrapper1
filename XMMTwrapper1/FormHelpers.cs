
using System.Globalization;
using System.Text.RegularExpressions;

public static class TextBoxWordSelect
{
    // Delimiters: space, tab, CR, LF. Add punctuation here if you want.
    private static bool IsDelimiter(char c) => c == ' ' || c == '\t' || c == '\r' || c == '\n';

    public static void SelectWordAtClick(TextBoxBase tb, MouseEventArgs e)
    {
        if (tb is null) return;
        if (tb.TextLength == 0) return;

        // Move caret to the click location
        int i = tb.GetCharIndexFromPosition(e.Location);
        if (i < 0) return;
        if (i >= tb.TextLength) i = tb.TextLength - 1;

        string text = tb.Text;

        // If you clicked on a delimiter, try to nudge left onto a word
        if (IsDelimiter(text[i]))
        {
            int j = i - 1;
            while (j >= 0 && IsDelimiter(text[j])) j--;
            if (j < 0) { tb.SelectionLength = 0; return; }
            i = j;
        }

        // Expand left
        int start = i;
        while (start > 0 && !IsDelimiter(text[start - 1]))
            start--;

        // Expand right
        int end = i;
        while (end < text.Length && !IsDelimiter(text[end]))
            end++;

        tb.SelectionStart = start;
        tb.SelectionLength = end - start;

        // Optional: keep caret visible and avoid weird scroll jumps
        tb.ScrollToCaret();
    }



// Start of a WS RXTEXT line when WS runs with --n1mm-rxtext-info:
//   "hhmmss freq snr  text"   e.g. "123456 1503 -12  W9XYZ 599 107"
private static readonly Regex RxTextPrefix = new Regex(
    @"^\s*(?<time>\d{6})\s+(?<freq>\d{1,4})\s+(?<snr>[+-]?\d{1,3})\s",
    RegexOptions.Compiled | RegexOptions.CultureInvariant);

private static readonly Regex ReportWord = new Regex(
    @"^[+-]?\d{1,3}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

/// <summary>
/// Report for the line holding the current selection (e.g. the callsign just
/// picked by SelectWordAtClick), from the WS RXTEXT "hhmmss freq snr" prefix,
/// formatted by FormatReport. Returns null if that line has no prefix.
/// If selectReport is true, the raw report word is selected in the textbox
/// (this replaces the current selection).
/// </summary>
public static string GetReportOnSelectedLine(TextBoxBase tb, bool selectReport = false)
{
    if (tb is null || tb.TextLength == 0) return null;

    string text = tb.Text;
    int pos = Math.Max(0, Math.Min(tb.SelectionStart, text.Length - 1));

    // Logical line (between '\n's), not the word-wrapped display line
    int lineStart = pos > 0 ? text.LastIndexOf('\n', pos - 1) + 1 : 0;
    int lineEnd = text.IndexOf('\n', pos);
    if (lineEnd < 0) lineEnd = text.Length;

    // TextBox uses "\r\n", RichTextBox uses "\n"
    string line = text.Substring(lineStart, lineEnd - lineStart).TrimEnd('\r');

    Match m = RxTextPrefix.Match(line);
    if (!m.Success) return null;

    Group g = m.Groups["snr"];
    if (selectReport)
    {
        tb.SelectionStart = lineStart + g.Index;
        tb.SelectionLength = g.Length;
        tb.ScrollToCaret();
    }
    return FormatReport(g.Value);
}

/// <summary>
/// Formats a report word as 3 characters, detecting the style:
///   signed, or unsigned 0..30   -> FT8 SNR "-06", "+05", "+00" (clamped -50..+49)
///   unsigned above 30, 3 digits -> CW RST as is: "599", "579"
///   unsigned above 30, 2 digits -> CW RS, tone 9 added: "58" -> "589"
/// Returns null if the word is not a 1-3 digit number with optional sign.
/// </summary>
public static string FormatReport(string word)
{
    if (string.IsNullOrWhiteSpace(word)) return null;
    string w = word.Trim();
    if (!ReportWord.IsMatch(w)) return null;

    bool signed = w[0] == '+' || w[0] == '-';
    int value = int.Parse(w, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);

    if (!signed && value > 30)                       // CW: RS or RST
    {
        string digits = value.ToString(CultureInfo.InvariantCulture);   // drops leading zeros
        if (digits.Length == 2) return digits + "9";                    // 58 -> 589
        if (digits.Length == 3) return digits;                          // 599
        return null;
    }

    value = Math.Max(-50, Math.Min(49, value));      // FT8 SNR
    return value.ToString("+00;-00;+00", CultureInfo.InvariantCulture);  // +05, -06, +00
}
}

public static class TextBoxExtensions
{
    // incr and decr number in text box preserving padding
    // if non-numeric do nothing
    // if empty set a default 001
    // 001 is the minimum value
    public static void Increment(this TextBox tb, int step = 1)
    {
        if (tb.Text == "") { tb.Text = "001"; return; } // init

        // TryParse returns true if it's a number, false if it's "ABC" or empty
        if (int.TryParse(tb.Text, out int current))
        {
             tb.Text = Math.Max(current + step, 1).ToString("D" + tb.Text.Length);
        }
        // If false, we do nothing - the original text stays exactly as it was
    }

    public static void Decrement(this TextBox tb, int step = -1)
    {
        if (tb.Text == "") { tb.Text = "001"; return; } // init

        // TryParse returns true if it's a number, false if it's "ABC" or empty
        if (int.TryParse(tb.Text, out int current))
        {
            tb.Text = Math.Max(current + step, 1).ToString("D" + tb.Text.Length);
        }
        // If false, we do nothing - the original text stays exactly as it was
    }
}


