
using System.Diagnostics;
using System.Text;
using XMMTwrapper1;

public class QsoEntry
{
    public string Callsign { get; set; } = "";
    public string FrequencyMHz { get; set; }        //to kHz res eg 28.180
    public DateTime Timestamp { get; set; }
    public string Date { get; set; }                //these are created internally from TimeStamp
    public string Time { get; set; }
    public string Mode { get; set; } = "CW";
    public string RstSent { get; set; } = "";
    public string RstRcvd { get; set; } = "";
    public string Comment { get; set; } = "";
    public string Remarks { get; set; } = "";
    public string Power { get; set; } = "100";
}

public class AdifLogger
{
    private readonly string _fileName;
    private readonly string _filePath;
    private readonly string _fileFolder;
    private List<QsoEntry> _loggedEntries = new();

    public AdifLogger(string fileName = "CW-RTTY.ADI")
    {
        // Equivalent to App.Path in VB6
        _fileFolder = AppDomain.CurrentDomain.BaseDirectory;
        _fileName = fileName;
        _filePath = Path.Combine(_fileFolder, fileName);
    }

    public void Initialize()
    {
        CheckAndCreateLogFile();
        LoadLogFile();
    }

    private void CheckAndCreateLogFile()
    {
        if (!File.Exists(_filePath))
        {
            var header = new StringBuilder();
            header.AppendLine("CW - RTTY Controller for TXLink");
            header.AppendLine("ID_G4AHN");
            header.AppendLine("<PROGRAMID:13>CW-RTTY-G4AHN");
            header.AppendLine("<PROGRAMVERSION:3>1.0");
            header.AppendLine("CW-RTTY DECODER + LOGGER BY G4AHN");
            header.AppendLine("CALL      G4AHN");
            header.AppendLine("<EOH>\r\n");

            File.WriteAllText(_filePath, header.ToString());
        }
    }

    public bool LogQso(QsoEntry entry)
    {
        // Client provides Timestamp
        entry.Date = entry.Timestamp.ToString("yyyyMMdd");
        entry.Time = entry.Timestamp.ToString("HHmm");

        // Client must format frequency to kHz resolution (3 decimal places) eg 28.180
        string freqStr = entry.FrequencyMHz;

        var sb = new StringBuilder();
        sb.Append(FormatAdifTag("CALL", entry.Callsign));
        sb.Append(FormatAdifTag("QSO_DATE", entry.Date));
        sb.Append(FormatAdifTag("QSO_TIME", entry.Time));
        sb.Append(FormatAdifTag("FREQ", freqStr));              //pre formatted by client eg "28.180"
        sb.Append(FormatAdifTag("MODE", entry.Mode));
        sb.Append(FormatAdifTag("RST_SENT", entry.RstSent));
        sb.Append(FormatAdifTag("RST_RCVD", entry.RstRcvd));
        sb.Append(FormatAdifTag("TX_PWR", entry.Power));
        sb.Append(FormatAdifTag("COMMENT", entry.Comment));
        sb.Append(FormatAdifTag("APP_WL32_REMARKS", entry.Remarks));
        sb.Append("<EOR>\r\n");

        //check for sensible entry
        if (entry.Callsign == "" || entry.FrequencyMHz == "") return false;

        //log to file
        File.AppendAllText(_filePath, sb.ToString());
        // Update in-memory list for dupe checking
        _loggedEntries.Add(entry);
        return true;
    }

    private string FormatAdifTag(string field, string value, string type = "")
    {
        if (string.IsNullOrEmpty(value)) return "";
        string typeStr = string.IsNullOrEmpty(type) ? "" : $":{type}";
        return $"<{field}:{value.Length}{typeStr}>{value.ToUpper()}\t";
    }

    public void LoadLogFile()
    {
        _loggedEntries.Clear();
        if (!File.Exists(_filePath)) return;

        foreach (var line in File.ReadLines(_filePath))
        {
            if (!line.Contains("<EOR>")) continue;

            var entry = new QsoEntry
            {
                Callsign = GetAdifElement(line, "<CALL:"),
                Date = GetAdifElement(line, "<QSO_DATE:"),
                Time = GetAdifElement(line, "<QSO_TIME:"),
                FrequencyMHz = GetAdifElement(line, "<FREQ:"),
                Mode = GetAdifElement(line, "<MODE:"),
                RstSent = GetAdifElement(line, "<RST_SENT:"),
                RstRcvd = GetAdifElement(line, "<RST_RCVD:")
            };

            if (!string.IsNullOrEmpty(entry.Callsign))
                _loggedEntries.Add(entry);
        }
    }

    private string GetAdifElement(string line, string tag)
    {
        int sPos = line.IndexOf(tag, StringComparison.OrdinalIgnoreCase);
        if (sPos == -1) return "";

        sPos += tag.Length;
        int ePos = line.IndexOf('>', sPos);
        if (ePos == -1) return "";

        // Get length from tag (e.g., <CALL:5>)
        string lenStr = line.Substring(sPos, ePos - sPos);
        // Handle type markers like :N
        if (lenStr.Contains(":")) lenStr = lenStr.Split(':')[0];

        if (int.TryParse(lenStr, out int length))
        {
            return line.Substring(ePos + 1, length).Trim();
        }
        return "";
    }

    public int GetDupeIndex(string callsign, string freqMhz)
    {
        // Get the part before the dot (e.g., "28")
        string bandMhz = freqMhz.Split('.')[0];

        return _loggedEntries.FindIndex(e =>
            e.Callsign.Equals(callsign, StringComparison.OrdinalIgnoreCase) &&
            e.FrequencyMHz.Split('.')[0] == bandMhz);
    }

    public int TotalLogged => _loggedEntries.Count;

    public string GetLogFilePath()
    {
        return _filePath;
    }

    public void ExportToCabrillo(string fileName, CabrilloHeader header)
    {
        StringBuilder sb = new StringBuilder();

        // 1. Header (Standard Cabrillo v3.0)
        sb.AppendLine("START-OF-LOG: 3.0");
        sb.AppendLine($"CONTEST: {header.Contest}");
        sb.AppendLine($"CALLSIGN: {header.Callsign}");
        sb.AppendLine($"OPERATORS: {header.Operators}");
        sb.AppendLine($"CATEGORY-OPERATOR: {header.CategoryOp}");
        sb.AppendLine($"CATEGORY-TRANSMITTER: {header.CategoryTx}");
        sb.AppendLine($"CATEGORY-OPERATOR: {header.CategoryPower}");
        sb.AppendLine($"CLAMED-SCORE: {header.ClaimedScore}");
        sb.AppendLine($"SOAPBOX: {header.Soapbox}");
        sb.AppendLine("CREATED-BY: MyG4AHNLogger v1.0");

        // 2. QSO Data - Column alignment is critical here
        // Format: FREQ MOD DATE       TIME  SENT-CALL SENT-RST SENT-NUM  RCVD-CALL RCVD-RST RCVD-NUM
        foreach (var qso in _loggedEntries)
        {
            // Split "599 001" into parts
            string[] rstSentParts = qso.RstSent.Split(' ');
            string myRst = rstSentParts.Length > 0 ? rstSentParts[0] : "599";
            string mySerial = rstSentParts.Length > 1 ? rstSentParts[1] : "001";

            string[] rstRecParts = qso.RstRcvd.Split(' ');
            string dxRst = rstRecParts.Length > 0 ? rstRecParts[0] : "599";
            string dxSerial = rstRecParts.Length > 1 ? rstRecParts[1] : "001";

            string mode = qso.Mode;
            if (mode == "RTTY") mode = "RY";

            // Build the line with separate columns
            // The widths below are standard for most HF contests
            string line = string.Format("QSO: {0} {1} {2} {3} {4} {5} {6} {7} {8} {9}",
                qso.FrequencyMHz.Replace(".", "").PadLeft(5), // Freq
                mode,                                         // Mode (e.g., RTTY)
                qso.Date,                                     // Date
                qso.Time,                                     // Time
                header.Callsign.PadRight(13),                // Sent Call
                myRst.PadRight(3),                           // Sent RST
                mySerial.PadLeft(4),                         // Sent Serial (often 4-6 chars)
                qso.Callsign.PadRight(13),                   // Received Call
                dxRst.PadRight(3),                           // Received RST
                dxSerial.PadLeft(4)                          // Received Serial
            );

            sb.AppendLine(line);
        }

        sb.AppendLine("END-OF-LOG:");

        // 3. Write to file
        File.WriteAllText(fileName, sb.ToString());
    }

    public void OpenLogDirectory()
    {
        if (!File.Exists(_filePath))
        {
            // Fallback: just open the directory if the file is missing
            string dir = Path.GetDirectoryName(_filePath);
            if (Directory.Exists(dir)) Process.Start("explorer.exe", $"\"{dir}\"");
            return;
        }

        // The /select argument is the secret sauce
        string argument = $"/select, \"{_filePath}\"";
        Process.Start("explorer.exe", argument);
    }
}