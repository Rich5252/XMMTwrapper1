using System;
using System.Windows.Forms;

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


