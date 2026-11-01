/*
 // DONE
 //     A few prefixes cover several entities and need the digit, or even the suffix, to tell them apart:

    KG4 is Guantanamo only with a 2-letter suffix.
    VK9 covers several entities: Cocos (VK9C), Christmas Island (VK9X), Norfolk (VK9N), Lord Howe (VK9L) and Willis (VK9W).
    PJ2/4/5/7 are separate entities.
    UA9/UA0 are Asiatic Russia.
*/


public static partial class ITU_Prefix_Checker
{
    // A HashSet of 1, 2 and 3 character prefixes for rapid lookup

    private static readonly Dictionary<string, string> PrefixTable = new Dictionary<string, string>
    {
        // ---------- Digit-first prefixes ----------
        { "1A0", "SMOM" }, { "1C", "Chechnya" }, { "1P", "Seborga" }, { "1S", "Spratly" },
        { "2D", "Isle of Man" }, { "2E", "England" }, { "2I", "No. Ireland" }, { "2J", "Jersey" },
        { "2M", "Scotland" }, { "2U", "Guernsey" }, { "2W", "Wales" },
        { "3A", "Monaco" }, { "3B6", "Agalega" }, { "3B7", "Cargados" }, { "3B8", "Mauritius" }, { "3B9", "Rodriguez" },
        { "3C", "Eq. Guinea" }, { "3C0", "Annobon" }, { "3D2", "Fiji" }, { "3DA", "Eswatini" },
        { "3E", "Panama" }, { "3F", "Panama" }, { "3G", "Chile" },
        { "3H", "China" }, { "3I", "China" }, { "3J", "China" }, { "3K", "China" }, { "3L", "China" },
        { "3M", "China" }, { "3N", "China" }, { "3O", "China" }, { "3P", "China" }, { "3Q", "China" },
        { "3R", "China" }, { "3S", "China" }, { "3T", "China" }, { "3U", "China" },
        { "3V", "Tunisia" }, { "3W", "Vietnam" }, { "3X", "Guinea" }, { "3Y", "Bouvet" }, { "3Z", "Poland" },
        { "4A", "Mexico" }, { "4B", "Mexico" }, { "4C", "Mexico" },
        { "4D", "Philippines" }, { "4E", "Philippines" }, { "4F", "Philippines" }, { "4G", "Philippines" },
        { "4H", "Philippines" }, { "4I", "Philippines" }, { "4J", "Azerbaijan" }, { "4K", "Azerbaijan" },
        { "4L", "Georgia" }, { "4M", "Venezuela" }, { "4O", "Montenegro" },
        { "4P", "Sri Lanka" }, { "4Q", "Sri Lanka" }, { "4R", "Sri Lanka" }, { "4S", "Sri Lanka" },
        { "4T", "Peru" }, { "4U", "UN" }, { "4V", "Haiti" }, { "4W", "East Timor" },
        { "4X", "Israel" }, { "4Z", "Israel" },
        { "5A", "Libya" }, { "5B", "Cyprus" },
        { "5C", "Morocco" }, { "5D", "Morocco" }, { "5E", "Morocco" }, { "5F", "Morocco" }, { "5G", "Morocco" },
        { "5H", "Tanzania" }, { "5I", "Tanzania" }, { "5J", "Colombia" }, { "5K", "Colombia" },
        { "5L", "Liberia" }, { "5M", "Liberia" }, { "5N", "Nigeria" }, { "5O", "Nigeria" },
        { "5P", "Denmark" }, { "5Q", "Denmark" }, { "5R", "Madagascar" }, { "5S", "Madagascar" },
        { "5T", "Mauritania" }, { "5U", "Niger" }, { "5V", "Togo" }, { "5W", "Samoa" },
        { "5X", "Uganda" }, { "5Y", "Kenya" }, { "5Z", "Kenya" },
        { "6A", "Egypt" }, { "6B", "Egypt" }, { "6C", "Syria" },
        { "6D", "Mexico" }, { "6E", "Mexico" }, { "6F", "Mexico" }, { "6G", "Mexico" },
        { "6H", "Mexico" }, { "6I", "Mexico" }, { "6J", "Mexico" },
        { "6K", "S. Korea" }, { "6L", "S. Korea" }, { "6M", "S. Korea" }, { "6N", "S. Korea" },
        { "6O", "Somalia" }, { "6P", "Pakistan" }, { "6Q", "Pakistan" }, { "6R", "Pakistan" }, { "6S", "Pakistan" },
        { "6T", "Sudan" }, { "6U", "Sudan" }, { "6V", "Senegal" }, { "6W", "Senegal" },
        { "6X", "Madagascar" }, { "6Y", "Jamaica" }, { "6Z", "Liberia" },
        { "7A", "Indonesia" }, { "7B", "Indonesia" }, { "7C", "Indonesia" }, { "7D", "Indonesia" }, { "7E", "Indonesia" },
        { "7F", "Indonesia" }, { "7G", "Indonesia" }, { "7H", "Indonesia" }, { "7I", "Indonesia" },
        { "7J", "Japan" }, { "7K", "Japan" }, { "7L", "Japan" }, { "7M", "Japan" }, { "7N", "Japan" },
        { "7O", "Yemen" }, { "7P", "Lesotho" }, { "7Q", "Malawi" }, { "7R", "Algeria" }, { "7S", "Sweden" },
        { "7T", "Algeria" }, { "7U", "Algeria" }, { "7V", "Algeria" }, { "7W", "Algeria" }, { "7X", "Algeria" },
        { "7Y", "Algeria" }, { "7Z", "Saudi Arabia" },
        { "8A", "Indonesia" }, { "8B", "Indonesia" }, { "8C", "Indonesia" }, { "8D", "Indonesia" }, { "8E", "Indonesia" },
        { "8F", "Indonesia" }, { "8G", "Indonesia" }, { "8H", "Indonesia" }, { "8I", "Indonesia" },
        { "8J", "Japan" }, { "8K", "Japan" }, { "8L", "Japan" }, { "8M", "Japan" }, { "8N", "Japan" },
        { "8O", "Botswana" }, { "8P", "Barbados" }, { "8Q", "Maldives" }, { "8R", "Guyana" }, { "8S", "Sweden" },
        { "8T", "India" }, { "8U", "India" }, { "8V", "India" }, { "8W", "India" }, { "8X", "India" }, { "8Y", "India" },
        { "8Z", "Saudi Arabia" },
        { "9A", "Croatia" }, { "9B", "Iran" }, { "9C", "Iran" }, { "9D", "Iran" }, { "9E", "Ethiopia" }, { "9F", "Ethiopia" },
        { "9G", "Ghana" }, { "9H", "Malta" }, { "9I", "Zambia" }, { "9J", "Zambia" }, { "9K", "Kuwait" },
        { "9L", "Sierra Leone" }, { "9M", "Malaysia" }, { "9N", "Nepal" },
        { "9O", "Congo" }, { "9P", "Congo" }, { "9Q", "Congo" }, { "9R", "Congo" }, { "9S", "Congo" }, { "9T", "Congo" },
        { "9U", "Burundi" }, { "9V", "Singapore" }, { "9W", "Malaysia" }, { "9X", "Rwanda" },
        { "9Y", "Trinidad" }, { "9Z", "Trinidad" },

        // ---------- A ----------
        { "A2", "Botswana" }, { "A3", "Tonga" }, { "A4", "Oman" }, { "A5", "Bhutan" },
        { "A6", "UAE" }, { "A7", "Qatar" }, { "A8", "Liberia" }, { "A9", "Bahrain" },
        { "AA", "USA" }, { "AB", "USA" }, { "AC", "USA" }, { "AD", "USA" }, { "AE", "USA" },
        { "AF", "USA" }, { "AG", "USA" }, { "AH", "USA Pacific" }, { "AI", "USA" },
        { "AJ", "USA" }, { "AK", "USA" }, { "AL", "Alaska" },
        { "AM", "Spain" }, { "AN", "Spain" }, { "AO", "Spain" },
        { "AP", "Pakistan" }, { "AQ", "Pakistan" }, { "AR", "Pakistan" }, { "AS", "Pakistan" },
        { "AT", "India" }, { "AU", "India" }, { "AV", "India" }, { "AW", "India" },
        { "AX", "Australia" }, { "AY", "Argentina" }, { "AZ", "Argentina" },

        // ---------- B ----------
        { "B", "China" }, { "BO", "Quemoy" }, { "BS", "Scarborough" }, { "BV", "Taiwan" }, { "BV9", "Spratly" },

        // ---------- C ----------
        { "C2", "Nauru" }, { "C3", "Andorra" }, { "C4", "Cyprus" }, { "C5", "Gambia" }, { "C6", "Bahamas" },
        { "C8", "Mozambique" }, { "C9", "Mozambique" },
        { "CA", "Chile" }, { "CB", "Chile" }, { "CC", "Chile" }, { "CD", "Chile" }, { "CE", "Chile" }, { "CE0", "Easter Is" },
        { "CF", "Canada" }, { "CG", "Canada" }, { "CH", "Canada" }, { "CI", "Canada" }, { "CJ", "Canada" }, { "CK", "Canada" },
        { "CL", "Cuba" }, { "CM", "Cuba" }, { "CN", "Morocco" }, { "CO", "Cuba" }, { "CP", "Bolivia" },
        { "CQ", "Portugal" }, { "CR", "Portugal" }, { "CS", "Portugal" }, { "CT", "Portugal" },
        { "CQ3", "Madeira" }, { "CR3", "Madeira" }, { "CT3", "Madeira" }, { "CU", "Azores" },
        { "CV", "Uruguay" }, { "CW", "Uruguay" }, { "CX", "Uruguay" },
        { "CY", "Canada" }, { "CY0", "Sable I" }, { "CY9", "St Paul I" }, { "CZ", "Canada" },

        // ---------- D ----------
        { "D2", "Angola" }, { "D3", "Angola" }, { "D4", "Cape Verde" }, { "D5", "Liberia" },
        { "D6", "Comoros" }, { "D7", "S. Korea" }, { "D8", "S. Korea" }, { "D9", "S. Korea" },
        { "D", "Germany" }, { "Y", "Germany" },                                         //exceptions are double chars
        { "DS", "S. Korea" }, { "DT", "S. Korea" },
        { "DU", "Philippines" }, { "DV", "Philippines" }, { "DW", "Philippines" }, { "DX", "Philippines" },
        { "DY", "Philippines" }, { "DZ", "Philippines" },

        // ---------- E ----------
        { "E2", "Thailand" }, { "E3", "Eritrea" }, { "E4", "Palestine" }, { "E5", "Cook Is" }, { "E6", "Niue" },
        { "E7", "Bosnia" },
        { "EA", "Spain" }, { "EB", "Spain" }, { "EC", "Spain" }, { "ED", "Spain" }, { "EE", "Spain" },
        { "EF", "Spain" }, { "EG", "Spain" }, { "EH", "Spain" },
        { "EA6", "Balearic Is" }, { "EA8", "Canary Is" }, { "EA9", "Ceuta" },
        { "EI", "Ireland" }, { "EJ", "Ireland" }, { "EK", "Armenia" }, { "EL", "Liberia" },
        { "EM", "Ukraine" }, { "EN", "Ukraine" }, { "EO", "Ukraine" }, { "EP", "Iran" }, { "EQ", "Iran" },
        { "ER", "Moldova" }, { "ES", "Estonia" }, { "ET", "Ethiopia" },
        { "EU", "Belarus" }, { "EV", "Belarus" }, { "EW", "Belarus" },
        { "EX", "Kyrghyzstan" }, { "EY", "Tajikistan" }, { "EZ", "Turkmenistan" },

        // ---------- F ----------
        { "F", "France" }, { "FG", "Guadeloupe" }, { "FH", "Mayotte" }, { "FJ", "St Barthelemy" },
        { "FK", "New Caledonia" }, { "FM", "Martinique" }, { "FO", "French Polynesia" }, { "FP", "St Pierre" },
        { "FR", "Reunion" }, { "FS", "St Martin" }, { "FT", "Crozet/Kerguelen" }, { "FW", "Wallis" }, { "FY", "Guiana" },

        // ---------- G ----------
        { "G", "England" }, { "GB", "United Kingdom" }, { "GC", "Wales" }, { "GD", "Isle of Man" },
        { "GH", "Jersey" }, { "GI", "No. Ireland" }, { "GJ", "Jersey" }, { "GM", "Scotland" },
        { "GN", "No. Ireland" }, { "GP", "Guernsey" }, { "GS", "Scotland" }, { "GT", "Isle of Man" },
        { "GU", "Guernsey" }, { "GW", "Wales" }, { "GX", "England" },

        // ---------- H ----------
        { "H2", "Cyprus" }, { "H3", "Panama" }, { "H4", "Solomon Is" }, { "H6", "Nicaragua" }, { "H7", "Nicaragua" },
        { "H8", "Panama" }, { "H9", "Panama" },
        { "HA", "Hungary" }, { "HB", "Switzerland" }, { "HB0", "Liechtenstein" },
        { "HC", "Ecuador" }, { "HD", "Ecuador" }, { "HC8", "Galapagos" }, { "HD8", "Galapagos" },
        { "HE", "Switzerland" }, { "HF", "Poland" }, { "HG", "Hungary" }, { "HH", "Haiti" },
        { "HI", "Dominican Rep" }, { "HJ", "Colombia" }, { "HK", "Colombia" }, { "HL", "S. Korea" },
        { "HM", "N. Korea" }, { "HN", "Iraq" }, { "HO", "Panama" }, { "HP", "Panama" },
        { "HQ", "Honduras" }, { "HR", "Honduras" }, { "HS", "Thailand" }, { "HT", "Nicaragua" },
        { "HU", "El Salvador" }, { "HV", "Vatican City" },
        { "HW", "France" }, { "HX", "France" }, { "HY", "France" }, { "HZ", "Saudi Arabia" },

        // ---------- I ----------
        { "I", "Italy" }, { "IM0", "Sardinia" }, { "IS0", "Sardinia" },

        // ---------- J ----------
        { "J2", "Djibouti" }, { "J3", "Grenada" }, { "J4", "Greece" }, { "J5", "Guinea-Bissau" },
        { "J6", "St Lucia" }, { "J7", "Dominica" }, { "J8", "St Vincent" },
        { "JA", "Japan" }, { "JD", "Ogasawara" },
        { "JE", "Japan" }, { "JF", "Japan" }, { "JG", "Japan" }, { "JH", "Japan" }, { "JI", "Japan" },
        { "JJ", "Japan" }, { "JK", "Japan" }, { "JL", "Japan" }, { "JM", "Japan" }, { "JN", "Japan" },
        { "JO", "Japan" }, { "JP", "Japan" }, { "JQ", "Japan" }, { "JR", "Japan" }, { "JS", "Japan" },
        { "JT", "Mongolia" }, { "JU", "Mongolia" }, { "JV", "Mongolia" },
        { "JW", "Svalbard" }, { "JX", "Jan Mayen" }, { "JY", "Jordan" }, { "JZ", "Indonesia" },

        // ---------- K ----------
        { "K", "USA" }, { "KC6", "Palau" },
        { "KG4", "Guantanamo" }, { "KG6", "Guam" }, { "KH1", "Baker I" }, { "KH2", "Guam" },
        { "KH3", "Johnston I" }, { "KH4", "Midway Is" }, { "KH5", "Jarvis I" }, { "KH6", "Hawaii" },
        { "KH8", "Samoa" }, { "KH9", "Wake I" }, { "KH0", "Mariana Is" }, { "KL", "Alaska" },
        { "KP1", "Navassa I" }, { "KP2", "Virgin Is" }, { "KP3", "Puerto Rico" }, { "KP4", "Puerto Rico" },
        { "KP5", "Desecheo I" },

        // ---------- L ----------
        { "L2", "Argentina" }, { "L3", "Argentina" }, { "L4", "Argentina" }, { "L5", "Argentina" },
        { "L6", "Argentina" }, { "L7", "Argentina" }, { "L8", "Argentina" }, { "L9", "Argentina" },
        { "LA", "Norway" }, { "LB", "Norway" }, { "LC", "Norway" }, { "LD", "Norway" }, { "LE", "Norway" },
        { "LF", "Norway" }, { "LG", "Norway" }, { "LH", "Norway" }, { "LI", "Norway" }, { "LJ", "Norway" },
        { "LK", "Norway" }, { "LL", "Norway" }, { "LM", "Norway" }, { "LN", "Norway" },
        { "LO", "Argentina" }, { "LP", "Argentina" }, { "LQ", "Argentina" }, { "LR", "Argentina" },
        { "LS", "Argentina" }, { "LT", "Argentina" }, { "LU", "Argentina" }, { "LV", "Argentina" }, { "LW", "Argentina" },
        { "LX", "Luxembourg" }, { "LY", "Lithuania" }, { "LZ", "Bulgaria" },

        // ---------- M ----------
        { "M", "England" }, { "MC", "Wales" }, { "MD", "Isle of Man" }, { "MH", "Jersey" },
        { "MI", "No. Ireland" }, { "MJ", "Jersey" }, { "MM", "Scotland" }, { "MN", "No. Ireland" },
        { "MP", "Guernsey" }, { "MS", "Scotland" }, { "MT", "Isle of Man" }, { "MU", "Guernsey" },
        { "MW", "Wales" }, { "MX", "England" },

        // ---------- N ----------
        { "N", "USA" }, { "NH", "USA Pacific" }, { "NL", "Alaska" }, { "NP", "USA Caribbean" },

        // ---------- O ----------
        { "OA", "Peru" }, { "OB", "Peru" }, { "OC", "Peru" }, { "OD", "Lebanon" }, { "OE", "Austria" },
        { "OF", "Finland" }, { "OG", "Finland" }, { "OH", "Finland" }, { "OI", "Finland" }, { "OJ", "Finland" },
        { "OH0", "Aland Is" }, { "OJ0", "Market Reef" },
        { "OK", "Czech Rep" }, { "OL", "Czech Rep" }, { "OM", "Slovak Rep" },
        { "ON", "Belgium" }, { "OO", "Belgium" }, { "OP", "Belgium" }, { "OQ", "Belgium" },
        { "OR", "Belgium" }, { "OS", "Belgium" }, { "OT", "Belgium" },
        { "OU", "Denmark" }, { "OV", "Denmark" }, { "OW", "Denmark" },
        { "OX", "Greenland" }, { "OY", "Faroe Is" }, { "OZ", "Denmark" },

        // ---------- P ----------
        { "P2", "Papua New Guinea" }, { "P3", "Cyprus" }, { "P4", "Aruba" },
        { "P5", "N. Korea" }, { "P6", "N. Korea" }, { "P7", "N. Korea" }, { "P8", "N. Korea" }, { "P9", "N. Korea" },
        { "PA", "Netherlands" }, { "PB", "Netherlands" }, { "PC", "Netherlands" }, { "PD", "Netherlands" },
        { "PE", "Netherlands" }, { "PF", "Netherlands" }, { "PG", "Netherlands" }, { "PH", "Netherlands" },
        { "PI", "Netherlands" }, { "PJ", "Antilles" },
        { "PK", "Indonesia" }, { "PL", "Indonesia" }, { "PM", "Indonesia" }, { "PN", "Indonesia" }, { "PO", "Indonesia" },
        { "PP", "Brazil" }, { "PQ", "Brazil" }, { "PR", "Brazil" }, { "PS", "Brazil" }, { "PT", "Brazil" },
        { "PU", "Brazil" }, { "PV", "Brazil" }, { "PW", "Brazil" }, { "PX", "Brazil" }, { "PY", "Brazil" },
        { "PZ", "Suriname" },
        { "PJ2", "Curacao" }, { "PJ4", "Bonaire" }, { "PJ5", "Saba & St Eustatius" },
        { "PJ6", "Saba & St Eustatius" }, { "PJ7", "Sint Maarten" },

        // ---------- R ----------
        { "R", "Russia" },

        // ---------- S ----------
        { "S2", "Bangladesh" }, { "S3", "Bangladesh" }, { "S5", "Slovenia" }, { "S6", "Singapore" },
        { "S7", "Seychelles" }, { "S8", "So. Africa" }, { "S9", "Sao Tome" }, { "S0", "W. Sahara" },
        { "SA", "Sweden" }, { "SB", "Sweden" }, { "SC", "Sweden" }, { "SD", "Sweden" }, { "SE", "Sweden" },
        { "SF", "Sweden" }, { "SG", "Sweden" }, { "SH", "Sweden" }, { "SI", "Sweden" }, { "SJ", "Sweden" },
        { "SK", "Sweden" }, { "SL", "Sweden" }, { "SM", "Sweden" },
        { "SN", "Poland" }, { "SO", "Poland" }, { "SP", "Poland" }, { "SQ", "Poland" }, { "SR", "Poland" },
        { "SS", "Egypt" }, { "ST", "Sudan" }, { "SU", "Egypt" },
        { "SV", "Greece" }, { "SW", "Greece" }, { "SX", "Greece" }, { "SY", "Greece" }, { "SZ", "Greece" },
        { "SV5", "Dodecanese Is" }, { "SV9", "Crete" },

        // ---------- T ----------
        { "T2", "Tuvalu" }, { "T3", "Kiribati" }, { "T4", "Cuba" }, { "T5", "Somalia" },
        { "T6", "Afghanistan" }, { "T7", "San Marino" }, { "T8", "Palau" }, { "T9", "Bosnia" },
        { "TA", "Turkey" }, { "TB", "Turkey" }, { "TC", "Turkey" },
        { "TD", "Guatemala" }, { "TE", "Costa Rica" }, { "TF", "Iceland" }, { "TG", "Guatemala" },
        { "TH", "France" }, { "TI", "Costa Rica" }, { "TJ", "Cameroon" }, { "TK", "Corsica" },
        { "TL", "Central Africa" }, { "TM", "France" }, { "TN", "Congo" }, { "TO", "France" },
        { "TP", "France" }, { "TQ", "France" }, { "TR", "Gabon" }, { "TS", "Tunisia" },
        { "TT", "Chad" }, { "TU", "Ivory Coast" }, { "TV", "France" }, { "TW", "France" },
        { "TX", "France" }, { "TY", "Benin" }, { "TZ", "Mali" },

        // ---------- U ----------
        { "UA", "Russia" }, { "UB", "Russia" }, { "UC", "Russia" }, { "UD", "Russia" }, { "UE", "Russia" },
        { "UF", "Russia" }, { "UG", "Russia" }, { "UH", "Russia" }, { "UI", "Russia" }, { "UA2", "Kaliningrad" },
        { "UJ", "Uzbekistan" }, { "UK", "Uzbekistan" }, { "UL", "Uzbekistan" }, { "UM", "Uzbekistan" },
        { "UN", "Kazakhstan" }, { "UO", "Kazakhstan" }, { "UP", "Kazakhstan" }, { "UQ", "Kazakhstan" },
        { "UR", "Ukraine" }, { "US", "Ukraine" }, { "UT", "Ukraine" }, { "UU", "Ukraine" }, { "UV", "Ukraine" },
        { "UW", "Ukraine" }, { "UX", "Ukraine" }, { "UY", "Ukraine" }, { "UZ", "Ukraine" },
        { "UA9", "Asiatic Russia" }, { "UA0", "Asiatic Russia" },

        // ---------- V ----------
        { "V2", "Antigua" }, { "V3", "Belize" }, { "V4", "St Kitts" }, { "V5", "Namibia" },
        { "V6", "Micronesia" }, { "V7", "Marshall Is" }, { "V8", "Brunei" },
        { "VA", "Canada" }, { "VB", "Canada" }, { "VC", "Canada" }, { "VD", "Canada" }, { "VE", "Canada" },
        { "VF", "Canada" }, { "VG", "Canada" },
        { "VH", "Australia" }, { "VI", "Australia" }, { "VJ", "Australia" }, { "VK", "Australia" },
        { "VL", "Australia" }, { "VM", "Australia" }, { "VN", "Australia" },
        { "VK9", "Cocos/Norfolk" }, { "VK0", "Heard/Macquarie" }, { "VO", "Newfoundland" },
        { "VP2", "Anguilla" }, { "VP5", "Turks" }, { "VP6", "Pitcairn" }, { "VP8", "Falkland Is" },
        { "VP9", "Bermuda" }, { "VQ9", "Chagos" }, { "VR2", "Hong Kong" },
        { "VT", "India" }, { "VU", "India" }, { "VV", "India" }, { "VW", "India" },
        { "VX", "Canada" }, { "VY", "Canada" }, { "VZ", "Australia" },
        { "VK9C", "Cocos (Keeling)" }, { "VK9X", "Christmas I" }, { "VK9N", "Norfolk I" },
        { "VK9L", "Lord Howe I" },     { "VK9W", "Willis I" },    { "VK9M", "Mellish Reef" },
        { "VK0H", "Heard I" },         { "VK0M", "Macquarie I" },
        // VP8 sub-entities removed: Diego Garcia is VQ9, Bouvet is 3Y, and Falklands calls such as
        // VP8DMH / VP8SDZ use the same first suffix letters, so VP8 stays "Falkland Is"

        // ---------- W ----------
        { "W", "USA" }, { "WH", "USA Pacific" }, { "WL", "Alaska" }, { "WP", "USA Caribbean" },

        // ---------- X ----------
        { "XA", "Mexico" }, { "XB", "Mexico" }, { "XC", "Mexico" }, { "XD", "Mexico" }, { "XE", "Mexico" },
        { "XF", "Mexico" }, { "XG", "Mexico" }, { "XH", "Mexico" }, { "XI", "Mexico" }, { "XF4", "Revilla Gigedo" },
        { "XJ", "Canada" }, { "XK", "Canada" }, { "XL", "Canada" }, { "XM", "Canada" }, { "XN", "Canada" }, { "XO", "Canada" },
        { "XP", "Denmark" }, { "XQ", "Chile" }, { "XR", "Chile" }, { "XS", "China" },
        { "XT", "Burkina Faso" }, { "XU", "Cambodia" }, { "XV", "Vietnam" }, { "XW", "Laos" },
        { "XX", "Macao" }, { "XY", "Myanmar" }, { "XZ", "Myanmar" },

        // ---------- Y ----------   ("Y" = Germany is in the D block above)
        { "YA", "Afghanistan" },
        { "YB", "Indonesia" }, { "YC", "Indonesia" }, { "YD", "Indonesia" }, { "YE", "Indonesia" },
        { "YF", "Indonesia" }, { "YG", "Indonesia" }, { "YH", "Indonesia" },
        { "YI", "Iraq" }, { "YJ", "Vanuatu" }, { "YK", "Syria" }, { "YL", "Latvia" }, { "YM", "Turkey" },
        { "YN", "Nicaragua" }, { "YO", "Romania" }, { "YP", "Romania" }, { "YQ", "Romania" }, { "YR", "Romania" },
        { "YS", "El Salvador" }, { "YT", "Serbia" }, { "YU", "Serbia" },
        { "YV", "Venezuela" }, { "YW", "Venezuela" }, { "YX", "Venezuela" }, { "YY", "Venezuela" },

        // ---------- Z ----------
        { "Z2", "Zimbabwe" }, { "Z3", "N. Macedonia" }, { "Z6", "Kosovo" }, { "Z8", "S. Sudan" },
        { "ZA", "Albania" }, { "ZB", "Gibraltar" }, { "ZC", "Cyprus" }, { "ZD", "St Helena" }, { "ZF", "Cayman Is" },
        { "ZK1", "Cook Is" }, { "ZK2", "Niue" }, { "ZK3", "Tokelau" },
        { "ZL", "New Zealand" }, { "ZM", "New Zealand" }, { "ZP", "Paraguay" },
        { "ZR", "So. Africa" }, { "ZS", "So. Africa" }, { "ZT", "So. Africa" }, { "ZU", "So. Africa" },
        { "ZV", "Brazil" }, { "ZW", "Brazil" }, { "ZX", "Brazil" }, { "ZY", "Brazil" }, { "ZZ", "Brazil" }
    };
}