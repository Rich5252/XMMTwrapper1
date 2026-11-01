
public static partial class ITU_Prefix_Checker
{
    // A Dictionary of 1, 2, 3 and 4 character prefixes for rapid lookup
    //see ITU_PrefixTable.cs for the full list of prefixes and their corresponding country entities

    public static string Entity { get; set; } = "";        //country entity for last search
                                                           // "" unless last search returned true

    // 3-char prefixes that only apply with a 2-letter suffix (KG4AB = Guantanamo, KG4ABC = USA)
    private static readonly HashSet<string> TwoLetterSuffixOnly = new HashSet<string> { "KG4", "KC6" };

    private static string strWordReceived = "";
    private static bool WaitForSpace = false;
    // add next char to running word and return "" or callsign suggestion
    public static string IsCallsign(string s)
    {
        if (s == "") return "";
        s = s.ToUpper();
        Log.Perf($"Next char \"{s}\"");
        char c = s[0];
        //if last char detected callsign wait for a space before next check
        if (WaitForSpace && c != ' ') return "";        // a space is a clean start
        WaitForSpace = false;

        //append valid chars, all invalid char are ignored in word build
        if ((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9')) strWordReceived = strWordReceived + s;


        //check for terminator or 6 char to check for callsign
        if (IsTerminator(c) || strWordReceived.Length >= 6)     //includes space terminator here so that "call call call" works.
        {
            //if terminator or 6 chars check for callsign
            if (IsPotentialCallsign(strWordReceived))
            {
                Log.Perf($"Next word \"{strWordReceived}\"");
                string ret = strWordReceived;
                strWordReceived = "";
                if (!IsTerminator(c)) WaitForSpace = true;          //must get a terminator before next search
                return ret;                                         // ***** SUCCESS  *******
            }
            else if (IsTerminator(c))                               // FIX: any terminator (incl. space) ends the word,
            {                                                       // otherwise "DE G4AHN" is read as "DEG4AHN"
                strWordReceived = "";
                return "";
            }
            //wasn't a callsign yet so roll through valid chars
            //terminators and invalid char ignored while doing scrolling check
            else if (strWordReceived.Length >= 6)
            {
                //max callsign is 6 chars so drop first and try again with next char
                strWordReceived = strWordReceived.Substring(strWordReceived.Length - 5);
            }
        }

        //callsign not found
        return "";
    }

    public static string strIsPotentialCallsign(string word)
    {
        if (IsPotentialCallsign(word))
            return word;
        else
            return "";
    }

    public static bool IsPotentialCallsign(string word)
    {
        Entity = "";                // FIX: reset first, so a failed search never leaves a stale entity

        if (string.IsNullOrWhiteSpace(word) || word.Length < 3)
            return false;
        word = word.ToUpper();

        // Rule 1: Must contain at least one digit (The Anchor)
        bool hasDigit = false;
        for (int i = 0; i < word.Length; i++)
        {
            if (char.IsDigit(word[i])) { hasDigit = true; break; }
        }
        if (!hasDigit) return false;

        // Rule 2: last char in any callsign should be a letter
        if (!char.IsLetter(word[word.Length - 1])) return false;

        // Try the longest prefix first; if its structure doesn't fit, fall back to a shorter one
        for (int len = 4; len >= 1; len--)
        {
            if (word.Length < len) continue;
            string prefix = word.Substring(0, len);
            if (!PrefixTable.ContainsKey(prefix)) continue;
            if (!StructureFits(word, len))
            {
                if (len == 4) return false;     // VK9X matched but no suffix: don't fall back to VK9
                continue;
            }
            if (TwoLetterSuffixOnly.Contains(prefix) && word.Length != 5) continue;   // KG4ABC -> falls back to "K" = USA

            Entity = PrefixTable[prefix];
            return true;                                    // ***** found *****
        }
        //no prefix found, keep adding to word and looking
        return false;
    }

    // Does the rest of the word fit the shape expected after a prefix of this length?
    private static bool StructureFits(string w, int len)
    {
        switch (len)
        {
            case 4:     // VK9X + suffix letters: VK9XX
                return w.Length >= 5 && char.IsLetter(w[4]);

            case 3:
                if (char.IsDigit(w[2]))     // EA8 / VK9 / 1A0 / PJ2 : numeral is in the prefix, suffix follows
                    return w.Length >= 4 && char.IsLetter(w[3]);
                else                        // 3DA : numeral follows, then suffix  (3DA0RU)
                    return w.Length >= 5 && char.IsDigit(w[3]);

            case 2:     // PA / 9A / A6 / 2E : always followed by a numeral, then suffix (PA3ABC, 9A1A, A61AB, 2E0ABC)
                return w.Length >= 4 && char.IsDigit(w[2]);

            case 1:     // G4AHN  or  DL1ABC / RA3ABC  (second letter of a 2-letter prefix not in the table)
                if (char.IsDigit(w[0])) return false;
                return char.IsDigit(w[1]) || (char.IsLetter(w[1]) && char.IsDigit(w[2]));
        }
        return false;
    }

    //find country entity from callsign
    public static string GetEntity(string callsign)
    {
        if (IsPotentialCallsign(callsign)) return Entity;        // same rules as detection

        // fallback: plain longest-prefix lookup, length 4 down to 1
        for (int len = 4; len >= 1; len--)
        {
            if (callsign.Length >= len)
            {
                string fragment = callsign.Substring(0, len).ToUpper();
                if (PrefixTable.ContainsKey(fragment))
                    return PrefixTable[fragment];
            }
        }
        return "";
    }

    public static bool IsTerminator(char c)
    {
        //bool ret = (c == ' ' || c == '\r' || c == '\n');      //limited terminators
        bool ret = !char.IsLetter(c) && !char.IsDigit(c);       // all chars other than valid callsign char
        return ret;
    }
}
