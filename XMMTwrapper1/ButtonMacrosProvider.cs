public class MacroMessage
{
    public string Id { get; set; }        // e.g., "F1", "F2"
    public string Label { get; set; }     // e.g., "CQ", "Exchange"
    public string Template { get; set; }  // e.g., "CQ TEST DE %myCall"
}

public class MacroSet
{
    public string SetName { get; set; }   // e.g., "WPX_Contest"
    public List<MacroMessage> Messages { get; set; } = new List<MacroMessage>();
}

public class MacroProvider
{
    private IniManager _ini;
    private Dictionary<string, string> _substitutions = new Dictionary<string, string>();

    public MacroProvider(IniManager ini)
    {
        _ini = ini;
    }

    // Example context: "RTTY_WPX" or "CW_Default"
    public List<MacroMessage> LoadMacroSet(string context, int buttonCount)
    {
        var macroSet = new List<MacroMessage>();

        for (int i = 1; i <= buttonCount; i++)
        {
            string btnId = $"btnF{i}";

            // Read from INI: Section is the context, Key is the button ID
            // We provide a generic default if the key doesn't exist
            string template = _ini.GetValue(context, btnId, "%myCall %DXcall 5NN");
            string label = _ini.GetValue(context, btnId + "_Label", $"F{i}");

            macroSet.Add(new MacroMessage
            {
                Id = btnId,
                Template = template,
                Label = label
            });
        }

        return macroSet;
    }


    //Save button macros to ini file
    public void SaveMacroToIni(string context, MacroMessage msg)
    {
        _ini.SetValue(context, msg.Id, msg.Template);
        _ini.SetValue(context, msg.Id + "_Label", msg.Label);
    }


    //These are the substitution strings in button macros eg %myCall
    // stored in their own dictionary
    // Call this to setup the dictionary before calling the maro parsers
    public void AddTagSubstitutions(string Key, string Value)
    {
        _substitutions[Key] = Value.ToUpper();
    }

    //Parse button macro and do substitutions in RTTY context
    //return command to send to message pipe for TXLink
    public string ParseRTTY(string template)
    {
        string result = template;
        foreach (var var in _substitutions)
        {
            result = result.Replace(var.Key, var.Value);
        }
        return $"RTTY:{{M1}}{{Z{result}}}{{X1}}";
    }

    //Parse button macro and do substitutions in CW context
    //return command to send to keyer
    public string ParseCW(string template)
    {
        string result = template;
        foreach (var var in _substitutions)
        {
            string val = var.Value;
            if (var.Key.StartsWith("%RST") || var.Key.StartsWith("%Num"))
            {
                //replace 0 and 9 with shortforms
                val = val.Replace("0", "T");
                val = val.Replace("9", "N");
            }
            result = result.Replace(var.Key, val);
        }
        result = result.Replace("%r%", "\r\n");
        return $"{result}";
    }
}