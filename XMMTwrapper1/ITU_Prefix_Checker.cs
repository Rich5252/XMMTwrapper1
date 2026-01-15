using System.Collections.Generic;

public static class ITU_Prefix_Checker
{
    // A HashSet of 1, 2 and 3 character prefixes for rapid lookup
    private static readonly Dictionary<string, string> PrefixTable = new Dictionary<string, string>
{
    { "1A0", "Malta" }, { "1C", "Chechnya" }, { "1P", "Seborga" }, { "1S", "Spratly" },
    { "2D", "Isle of Man" }, { "2E", "England" }, { "2I", "No. Ireland" }, { "2J", "Jersey" },
    { "2M", "Scotland" }, { "2U", "Guernsey" }, { "2W", "Wales" }, { "3A", "Monaco" },
    { "3B6", "Agalega" }, { "3B7", "Cargados" }, { "3B8", "Mauritius" }, { "3B9", "Rodriguez" },
    { "3C", "Eq. Guinea" }, { "3C0", "Annobon" }, { "3D2", "Fiji" }, { "3DA", "Eswatini" },
    { "3V", "Tunisia" }, { "3W", "Vietnam" }, { "3X", "Guinea" }, { "3Y", "Bouvet" },
    { "3Z", "Poland" }, { "4A", "Mexico" }, { "4B", "Mexico" }, { "4C", "Mexico" },
    { "4D", "Philippines" }, { "4E", "Philippines" }, { "4F", "Philippines" }, { "4G", "Philippines" },
    { "4H", "Philippines" }, { "4I", "Philippines" }, { "4J", "Azerbaijan" }, { "4K", "Azerbaijan" },
    { "4L", "Georgia" }, { "4M", "Venezuela" }, { "4O", "Montenegro" }, { "4S", "Sri Lanka" },
    { "4T", "Peru" }, { "4U", "UN" }, { "4V", "Haiti" }, { "4W", "East Timor" },
    { "4X", "Israel" }, { "4Z", "Israel" }, { "5A", "Libya" }, { "5B", "Cyprus" },
    { "5C", "Morocco" }, { "5H", "Tanzania" }, { "5J", "Colombia" }, { "5K", "Colombia" },
    { "5L", "Liberia" }, { "5N", "Nigeria" }, { "5P", "Denmark" }, { "5R", "Madagascar" },
    { "5T", "Mauritania" }, { "5U", "Niger" }, { "5V", "Togo" }, { "5W", "Samoa" },
    { "5X", "Uganda" }, { "5Y", "Kenya" }, { "5Z", "Kenya" }, { "6C", "Syria" },
    { "6D", "Mexico" }, { "6E", "Mexico" }, { "6F", "Mexico" }, { "6G", "Mexico" },
    { "6H", "Mexico" }, { "6I", "Mexico" }, { "6J", "Mexico" }, { "6K", "S. Korea" },
    { "6L", "S. Korea" }, { "6O", "Somalia" }, { "6P", "Pakistan" }, { "6T", "Sudan" },
    { "6U", "Sudan" }, { "6V", "Senegal" }, { "6W", "Senegal" }, { "6Y", "Jamaica" },
    { "7J", "Japan" }, { "7K", "Japan" }, { "7L", "Japan" }, { "7M", "Japan" },
    { "7N", "Japan" }, { "7O", "Yemen" }, { "7P", "Lesotho" }, { "7Q", "Malawi" },
    { "7S", "Sweden" }, { "7W", "Algeria" }, { "7X", "Algeria" }, { "7Z", "Saudi Arabia" },
    { "8A", "Indonesia" }, { "8B", "Indonesia" }, { "8E", "Indonesia" }, { "8I", "Indonesia" },
    { "8J", "Japan" }, { "8O", "Botswana" }, { "8P", "Barbados" }, { "8Q", "Maldives" },
    { "8R", "Guyana" }, { "8S", "Sweden" }, { "9A", "Croatia" }, { "9G", "Ghana" },
    { "9H", "Malta" }, { "9I", "Zambia" }, { "9J", "Zambia" }, { "9K", "Kuwait" },
    { "9L", "Sierra Leone" }, { "9M", "Malaysia" }, { "9N", "Nepal" }, { "9Q", "Congo" },
    { "9R", "Congo" }, { "9U", "Burundi" }, { "9V", "Singapore" }, { "9W", "Malaysia" },
    { "9X", "Rwanda" }, { "9Y", "Trinidad" }, { "9Z", "Trinidad" }, { "A2", "Botswana" },
    { "A3", "Tonga" }, { "A4", "Oman" }, { "A5", "Bhutan" }, { "A6", "UAE" },
    { "A7", "Qatar" }, { "A8", "Liberia" }, { "A9", "Bahrain" }, { "AA", "USA" },
    { "AB", "USA" }, { "AC", "USA" }, { "AD", "USA" }, { "AE", "USA" },
    { "AF", "USA" }, { "AG", "USA" }, { "AH", "USA Pacific" }, { "AI", "USA" },
    { "AJ", "USA" }, { "AK", "USA" }, { "AL", "Alaska" }, { "AM", "Spain" },
    { "AN", "Spain" }, { "AO", "Spain" }, { "AP", "Pakistan" }, { "AQ", "Pakistan" },
    { "AR", "Pakistan" }, { "AT", "India" }, { "AX", "Australia" }, { "AY", "Argentina" },
    { "AZ", "Argentina" }, { "BO", "Quemoy" }, { "BS", "Scarborough" }, { "BV", "Taiwan" },
    { "BV9", "Spratly" }, { "B", "China" }, { "C2", "Nauru" }, { "C3", "Andorra" },
    { "C4", "Cyprus" }, { "C5", "Gambia" }, { "C6", "Bahamas" }, { "C8", "Mozambique" },
    { "C9", "Mozambique" }, { "CE", "Chile" }, { "CE0", "Easter Is" }, { "CN", "Morocco" },
    { "CO", "Cuba" }, { "CP", "Bolivia" }, { "CT", "Portugal" }, { "CU", "Azores" },
    { "CX", "Uruguay" }, { "CY", "Canada" }, { "CY0", "Sable I" }, { "CY9", "St Paul I" },
    { "CZ", "Canada" }, 
    { "D2", "Angola" }, { "D3", "Angola" }, { "D4", "Cape Verde" }, { "D5", "Liberia" }, 
    { "D6", "Comoros" }, { "D7", "S. Korea" }, { "D8", "S. Korea" }, { "D9", "S. Korea" },
    { "D", "Germany" }, { "Y", "Germany" },                                         //exceptions are double chars
    { "DS", "S. Korea" }, { "DT", "S. Korea" },
    { "DU", "Philippines" }, { "DV", "Philippines" },{ "DW", "Philippines" },{ "DX", "Philippines" },
    { "DY", "Philippines" },{ "DZ", "Philippines" },
    { "E2", "Thailand" }, { "E3", "Eritrea" }, { "E4", "Palestine" },
    { "E5", "Cook Is" }, { "E7", "Bosnia" }, { "EA", "Spain" }, { "EA6", "Balearic Is" },
    { "EA8", "Canary Is" }, { "EA9", "Ceuta" }, { "EI", "Ireland" }, { "EK", "Armenia" },
    { "EL", "Liberia" }, { "EM", "Ukraine" }, { "EP", "Iran" }, { "ER", "Moldova" },
    { "ES", "Estonia" }, { "ET", "Ethiopia" }, { "EU", "Belarus" }, { "EX", "Kyrghyzstan" },
    { "EY", "Tajikistan" }, { "EZ", "Turkmenistan" }, { "F", "France" }, { "FG", "Guadeloupe" },
    { "FH", "Mayotte" }, { "FJ", "St Barthelemy" }, { "FK", "New Caledonia" }, { "FM", "Martinique" },
    { "FO", "French Polynesia" }, { "FP", "St Pierre" }, { "FR", "Reunion" }, { "FS", "St Martin" },
    { "FT", "Crozet/Kerguelen" }, { "FW", "Wallis" }, { "FY", "Guiana" }, { "G", "England" },
    { "GB", "United Kingdom" }, { "GD", "Isle of Man" }, { "GI", "No. Ireland" }, { "GJ", "Jersey" },
    { "GM", "Scotland" }, { "GU", "Guernsey" }, { "GW", "Wales" }, { "H2", "Cyprus" },
    { "H3", "Panama" }, { "H4", "Solomon Is" }, { "H5", "Bophuthatswana" }, { "H6", "Nicaragua" },
    { "HA", "Hungary" }, { "HB", "Switzerland" }, { "HB0", "Liechtenstein" }, { "HC", "Ecuador" },
    { "HD", "Ecuador" }, { "HE", "Switzerland" }, { "HF", "Poland" }, { "HG", "Hungary" },
    { "HH", "Haiti" }, { "HI", "Dominican Rep" }, { "HK", "Colombia" }, { "HL", "S. Korea" },
    { "HP", "Panama" }, { "HR", "Honduras" }, { "HS", "Thailand" }, { "HT", "Nicaragua" },
    { "HU", "El Salvador" }, { "HV", "Vatican City" }, { "HZ", "Saudi Arabia" }, { "I", "Italy" },
    { "IS0", "Sardinia" }, { "J2", "Djibouti" }, { "J3", "Grenada" }, { "J4", "Greece" },
    { "J5", "Guinea-Bissau" }, { "J6", "St Lucia" }, { "J7", "Dominica" }, { "J8", "St Vincent" },
    { "JA", "Japan" }, { "JD", "Ogasawara" }, { "JT", "Mongolia" }, { "JW", "Svalbard" },
    { "JX", "Jan Mayen" }, { "JY", "Jordan" }, { "K", "USA" }, { "KC6", "Palau" },
    { "KG4", "Guantanamo" }, { "KG6", "Guam" }, { "KH1", "Baker I" }, { "KH2", "Guam" },
    { "KH3", "Johnston I" }, { "KH4", "Midway Is" }, { "KH5", "Jarvis I" }, { "KH6", "Hawaii" },
    { "KH8", "Samoa" }, { "KH9", "Wake I" }, { "KH0", "Mariana Is" }, { "KL", "Alaska" },
    { "KP1", "Navassa I" }, { "KP2", "Virgin Is" }, { "KP3", "Puerto Rico" }, { "KP5", "Desecheo I" },
    { "L2", "Argentina" }, { "L3", "Argentina" }, { "L4", "Argentina" }, { "L5", "Argentina" },
    { "L6", "Argentina" }, { "L7", "Argentina" }, { "L8", "Argentina" }, { "L9", "Argentina" },
    { "LA", "Norway" }, { "LU", "Argentina" }, { "LX", "Luxembourg" }, { "LY", "Lithuania" },
    { "LZ", "Bulgaria" }, { "M", "England" }, { "MD", "Isle of Man" }, { "MI", "No. Ireland" },
    { "MJ", "Jersey" }, { "MM", "Scotland" }, { "MU", "Guernsey" }, { "MW", "Wales" },
    { "N", "USA" }, { "NH", "USA Pacific" }, { "NL", "Alaska" }, { "NP", "USA Caribbean" },
    { "OA", "Peru" }, { "OD", "Lebanon" }, { "OE", "Austria" }, { "OH", "Finland" },
    { "OH0", "Aland Is" }, { "OJ0", "Market Reef" }, { "OK", "Czech Rep" }, { "OM", "Slovak Rep" },
    { "ON", "Belgium" }, { "OX", "Greenland" }, { "OY", "Faroe Is" }, { "OZ", "Denmark" },
    { "P2", "Papua New Guinea" }, { "P3", "Cyprus" }, { "P4", "Aruba" }, { "P5", "N. Korea" },
    { "PA", "Netherlands" }, { "PJ", "Antilles" }, 
    { "PP", "Brazil" }, { "PQ", "Brazil" },{ "PR", "Brazil" },{ "PS", "Brazil" },{ "PT", "Brazil" },
    { "PU", "Brazil" },{ "PV", "Brazil" },{ "PW", "Brazil" },{ "PX", "Brazil" },{ "PY", "Brazil" },
    { "PZ", "Suriname" },
    { "R", "Russia" }, { "S2", "Bangladesh" }, { "S4", "Ciskei" }, { "S5", "Slovenia" },
    { "S6", "Singapore" }, { "S7", "Seychelles" }, { "S8", "Transkei" }, { "S9", "Sao Tome" },
    { "S0", "W. Sahara" }, { "SM", "Sweden" }, { "SN", "Sweden" }, { "SP", "Poland" }, { "ST", "Sudan" },
    { "SU", "Egypt" }, { "SV", "Greece" }, { "SV5", "Dodecanese Is" }, { "SV9", "Crete" },
    { "T2", "Tuvalu" }, { "T3", "Kiribati" }, { "T4", "Cuba" }, { "T5", "Somalia" },
    { "T6", "Afghanistan" }, { "T7", "San Marino" }, { "T9", "Bosnia" }, { "TA", "Turkey" },
    { "TD", "Guatemala" }, { "TE", "Costa Rica" }, { "TF", "Iceland" }, { "TG", "Guatemala" },
    { "TI", "Costa Rica" }, { "TJ", "Cameroon" }, { "TK", "Corsica" }, { "TL", "Central Africa" },
    { "TM", "France" }, { "TN", "Congo" }, { "TO", "France" }, { "TR", "Gabon" },
    { "TT", "Chad" }, { "TU", "Ivory Coast" }, { "TX", "France" }, { "TY", "Benin" },
    { "TZ", "Mali" }, { "UA", "Russia" }, { "UA2", "Kaliningrad" }, { "UK", "Uzbekistan" },
    { "UN", "Kazakhstan" }, { "UR", "Ukraine" }, { "V2", "Antigua" }, { "V3", "Belize" },
    { "V4", "St Kitts" }, { "V5", "Namibia" }, { "V6", "Micronesia" }, { "V7", "Marshall Is" },
    { "V8", "Brunei" }, { "V9", "Vendaland" }, { "VE", "Canada" }, { "VK", "Australia" },
    { "VK9", "Cocos/Norfolk" }, { "VK0", "Heard/Macquarie" }, { "VO", "Newfoundland" }, { "VP2", "Anguilla" },
    { "VP5", "Turks" }, { "VP6", "Pitcairn" }, { "VP8", "Antarctica" }, { "VP9", "Bermuda" },
    { "VQ9", "Chagos" }, { "VR2", "Hong Kong" },
    { "VT", "India" }, { "VU", "India" }, { "VV", "India" }, { "VW", "India" }, 
    { "VX", "Canada" }, { "VY", "Canada" }, 
    { "W", "USA" }, { "WH", "USA Pacific" }, { "WL", "Alaska" }, { "WP", "USA Caribbean" },
    { "XE", "Mexico" }, { "XF4", "Revilla Gigedo" }, { "XT", "Burkina Faso" },
    { "XU", "Cambodia" }, { "XV", "Vietnam" }, { "XW", "Laos" }, { "XX", "China" },
    { "XY", "Myanmar" }, { "YA", "Afghanistan" }, { "YI", "Iraq" }, { "YJ", "Vanuatu" },
    { "YK", "Syria" }, { "YL", "Latvia" }, { "YM", "Turkey" }, { "YN", "Nicaragua" },
    { "YO", "Romania" }, { "YS", "El Salvador" }, { "YU", "Serbia" }, { "YV", "Venezuela" },
    { "YZ", "Yugoslavia" }, { "Z2", "Zimbabwe" }, { "Z3", "N. Macedonia" }, { "Z6", "Kosovo" },
    { "Z8", "S. Sudan" }, { "ZA", "Albania" }, { "ZB", "Gibraltar" }, { "ZC", "Cyprus" },
    { "ZD", "St Helena" }, { "ZF", "Cayman Is" }, { "ZK1", "Cook Is" }, { "ZK2", "Niue" },
    { "ZK3", "Tokelau" }, { "ZL", "New Zealand" }, { "ZP", "Paraguay" }, 
    { "ZR", "So. Africa" }, { "ZS", "So. Africa" },{ "ZT", "So. Africa" },{ "ZU", "So. Africa" },
    { "ZV", "Brazil" }
};

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
            if (!char.IsLetter(word[3])) return false;
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