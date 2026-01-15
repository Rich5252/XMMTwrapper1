using System.Collections.Generic;

public static class ITU_Prefix_Checker
{
    // A HashSet of 1 and 2 character prefixes for rapid lookup
    private static readonly HashSet<string> _prefixes = new HashSet<string>
    {
        // Americas
        "W", "K", "N", "A", "VE", "VA", "XE", "PY", "LU", "CX", "OA", "YV",
        // Europe
        "G", "M", "2", "F", "DL", "DA", "DK", "EA", "EB", "I", "HB", "OE", "SB", "SM", "SK", "LA", "OH",
        "OZ", "PA", "ON", "SP", "SN", "OK", "OM", "OP", "HA", "YO", "LZ", "SV", "UR", "UA", "YU", 
        "E7", "T7", "9A", "R", "5B", "Z", 
        // Asia/Oceania
        "JA", "HL", "BY", "VR", "DU", "YB", "9V", "HS", "XV", "VK", "ZL", "VU", "4X",
        // Africa
        "ZS", "5Z", "6W", "CN", "SU", "EA8"
    };

    private static string strWordReceived = "";
    // add next char to running word and return "" or callsign suggestion
    public static string IsCallsign(string s)
    {
        char c = s[0];
        //append valid chars, all invalid char are ignored
        if ((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9')) strWordReceived = strWordReceived + s;

        //check for terminator or 6 char to check for callsign
        if (s == " " || s == Environment.NewLine || strWordReceived.Length >= 6)
        {
            //if terminator or 6 chars check for callsign
            if (IsPotentialCallsign(strWordReceived))
            {
                string ret = strWordReceived;
                strWordReceived = "";
                return ret;
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

        // Rule 2: Check Prefix (1, 2 or 3 characters)
        string S1 = word.Substring(0, 1).ToUpper();
        string S2 = word.Length >= 2 ? word.Substring(0, 2).ToUpper() : "";
        string S3 = word.Length >= 3 ? word.Substring(0, 3).ToUpper() : "";

        bool bS1 = _prefixes.Contains(S1);
        bool bS2 = _prefixes.Contains(S2);
        bool bS3 = _prefixes.Contains(S3);

        if (bS1 || bS2 || bS3)
        {
            // Rule 3: Exclude common "False Positives" found in contests
            if (word == "5NN" || word == "73" || word == "RST") return false;

            //if first char is number then second must be letter and third a number
            if (bS1)
            if (char.IsDigit(word[0]) && !(char.IsAscii(word[1]) && char.IsDigit(word[2]))) return false;

            //if first single letter then 2nd or 3rd should be a number
            if (bS1)
            if (char.IsAscii(word[0]) && !(char.IsDigit(word[1]) || char.IsDigit(word[2]))) return false;

            
            //if first 2 are lettes then third should be number
            if (bS2)
            if ((char.IsAscii(word[0]) && char.IsAscii(word[1])) && !char.IsDigit(word[2]))  return false;

            //if a 3 char prefix then 4th must be letter
            if (bS3)
            if (!char.IsAscii(word[3])) return false;

            //last char should be a letter
            if (!char.IsAscii(word[word.Length -1])) return false;

            return true;
        }

        return false;
    }
}