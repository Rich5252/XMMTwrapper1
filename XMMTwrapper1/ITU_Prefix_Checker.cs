
public static partial class ITU_Prefix_Checker
{
    // A HashSet of 1, 2 and 3 character prefixes for rapid lookup
    //see ITU_PrefixTable.cs for the full list of prefixes and their corresponding country entities

    public static string Entity { get; set; } = "";        //country entity for last search
                                                           // null unless last search returned true

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
            else if (IsTerminator(c) && c != ' ')                   // if callsign sent with spaces keep it
            {                                                       // otherwise a terminator cancels this word   
                string ret = strWordReceived;
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
        if (string.IsNullOrWhiteSpace(word) || word.Length < 3)
            return false;

        // Rule 1: Must contain at least one digit (The Anchor)
        bool hasDigit = false;
        for (int i = 0; i < word.Length; i++)
        {
            if (char.IsDigit(word[i])) { hasDigit = true; break; }
        }
        if (!hasDigit) return false;

        string prefix = "";         //the prefix found
        Entity = "";                // reset country entity
        bool bS1, bS2, bS3;
        bS1 = bS2 = bS3 = false;

        //try 3 char first
        string S3 = word.Length >= 3 ? word.Substring(0, 3).ToUpper() : "";
        bS3 = PrefixTable.ContainsKey(S3);
        if (bS3)
        {
            //if a 3 char prefix then 4th must be letter
            bool b3rdCharIsLetter = word.Length >= 3 && char.IsLetter(word[2]);
            bool b4thCharIsLetter = word.Length >= 4 && char.IsLetter(word[3]);
            if (word.Length > 3 && (b3rdCharIsLetter == b4thCharIsLetter)) return false;        //3rd & 4th char must be different type
            prefix = S3;
        }
        else
        {
            // then 2 char if 3 failed
            string S2 = word.Length >= 2 ? word.Substring(0, 2).ToUpper() : "";
            bS2 = PrefixTable.ContainsKey(S2);
            if (bS2)
            {
                //if first 2 are lettes then third should be number
                if ((char.IsLetter(word[0]) && char.IsLetter(word[1])) && !char.IsDigit(word[2])) return false;
                prefix = S2;
            }
            else
            {
                // then 1 char if 3 & 2 failed
                string S1 = word.Substring(0, 1).ToUpper();
                bS1 = PrefixTable.ContainsKey(S1);                
                if (bS1)
                {
                    //if first char is number then second must be letter and third a number
                    if (char.IsDigit(word[0]) && !(char.IsLetter(word[1]) && char.IsDigit(word[2]))) return false;

                    //if first single letter then 2nd or 3rd should be a number
                    if (char.IsLetter(word[0]) && !(char.IsDigit(word[1]) || char.IsDigit(word[2]))) return false;
                    prefix += S1;
                }
            }
        }
        if (bS1 || bS2 || bS3)
        {
            //if we get here then we've found something
            //last char in any callsign should be a letter
            if (!char.IsLetter(word[word.Length - 1])) return false;

            Entity = PrefixTable[prefix];
            return true;
        }
        //no prefix found, keep adding to word and looking
        return false;
    }

    //find country entity from callsign
    public static string GetEntity(string callsign)
    {
        // Normalize and iterate backwards from length 3 down to 1
        for (int len = 3; len >= 1; len--)
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