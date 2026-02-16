/*
Mandatory Header Tags
Every Cabrillo 3.0 file must have these to be accepted:
Tag                     Expected Values / Format
START-OF-LOG:           3.0
CALLSIGN:               The callsign used during the contest.
CONTEST:                The specific name (e.g., CQ-WW-RTTY, ARRL-DX-CW).
CATEGORY-OPERATOR:      SINGLE-OP, MULTI-OP, or CHECKLOG.CATEGORY-ASSISTED:ASSISTED or NON-ASSISTED.
CATEGORY-BAND:          ALL, 160M, 80M, 40M, 20M, 15M, 10M.
CATEGORY-MODE:          CW, DIGI, FM, RTTY, SSB, or MIXED.
CATEGORY-POWER:         HIGH, LOW, or QRP.
CATEGORY-STATION:       FIXED, MOBILE, PORTABLE, ROVER, EXPEDITION.
CATEGORY-TRANSMITTER:   ONE, TWO, LIMITED, UNLIMITED, or SWL.
CLAIMED-SCORE:          Integer (no commas).
OPERATORS:              List of callsigns (comma or space separated).
NAME:                   Your full name.
ADDRESS:                Mailing address (multiple lines allowed).
END-OF-LOG:             Must be the very last line of the file.

Contest-Specific Tags Depending on the contest rules, you might also need:
LOCATION:               (Mainly for ARRL/RSGB) The State, Province, or Section (e.g., NH, DX, SCV).
CLUB:                   The name of your affiliated club for club competition.
GRID-LOCATOR:           Your 4 or 6-character Maidenhead grid (e.g., IO91oe).
SOAPBOX:                Any comments about the contest (multiple lines allowed).
*/


using System.Text;

namespace XMMTwrapper1
{
    public partial class CabrilloHeaderForm : Form
    {
        // Object to hold the results
        public CabrilloHeader HeaderData { get; private set; }

        public CabrilloHeaderForm(IniManager ini)
        {
            InitializeComponent();
            // Pre-fill from INI using your IniManager
            tbCallsign.Text = ini.GetValue("Station", "Callsign", "G4AHN");

            tbOpCalls.Text = ini.GetValue("Station", "Operators", "G4AHN");
            tbClub.Text = ini.GetValue("Station", "Club", "");
            tbCatOp.Text = ini.GetValue("Station", "CategoryOp", "SINGLE-OP");
            tbCatAssisted.Text = ini.GetValue("Station", "CategoryAssist", "NON-ASSISTED");
            tbCatOverlay.Text = ini.GetValue("Station", "CategoryOvl", "TB-WIRES");
            tbCatTx.Text = ini.GetValue("Station", "CategoryTx", "ONE");
            tbCatPwr.Text = ini.GetValue("Station", "CategoryPower", "LOW");
            tbCatBand.Text = ini.GetValue("Station", "CategoryBand", "ALL");
            tbCatMode.Text = ini.GetValue("Station", "CategoryMode", "RTTY");
            tbLocator.Text = ini.GetValue("Station", "GridLocator", "IO91OE");
            tbFullName.Text = ini.GetValue("Station", "FullName", "Richard Lax");
            tbAddress.Text = ini.GetValue("Station", "Address", "1 Gardeners Hill Rd|Farnham|GU10 4RL|England");
            tbEmail.Text = ini.GetValue("Station", "Email", "richard.lax7@gmail.com");
            tbCatStation.Text = ini.GetValue("Station", "CategoryStation", "FIXED");
            tbLocation.Text = ini.GetValue("Station", "Location", "DX");
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            // Capture the UI data into a simple class
            HeaderData = new CabrilloHeader
            {
                Contest = tbContest.Text,
                Callsign = tbCallsign.Text,
                FullName = tbFullName.Text,
                Email = tbEmail.Text,
                Address = tbAddress.Text,
                Location = tbLocation.Text,
                Locator = tbLocator.Text,
                CategoryStation = tbCatStation.Text,
                CategoryOp = tbCatOp.Text,
                CategoryAssist = tbCatAssisted.Text,
                CategoryBand = tbCatBand.Text,
                CategoryMode = tbCatMode.Text,
                CategoryOverlay = tbCatOverlay.Text,
                CategoryTx = tbCatTx.Text,
                CategoryPower = tbCatPwr.Text,
                Operators = tbOpCalls.Text,
                Club = tbClub.Text,
                Soapbox = tbSoap.Text,
                ClaimedScore = tbScore.Text
            };

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        public void ExportToCabrillo(string fileName, CabrilloHeader header, List<QsoEntry> _loggedEntries)
        {
            StringBuilder sb = new StringBuilder();

            // 1. Header (Standard Cabrillo v3.0)
            sb.AppendLine("START-OF-LOG: 3.0");
            sb.AppendLine($"CONTEST: {header.Contest}");
            sb.AppendLine($"CALLSIGN: {header.Callsign}");
            sb.AppendLine($"OPERATORS: {header.Operators}");
            sb.AppendLine($"CATEGORY-OPERATOR: {header.CategoryOp}");
            sb.AppendLine($"CATEGORY-ASSISTED: {header.CategoryAssist}");
            sb.AppendLine($"CATEGORY-BAND: {header.CategoryBand}");
            sb.AppendLine($"CATEGORY-TRANSMITTER: {header.CategoryTx}");
            sb.AppendLine($"CATEGORY-STATION: {header.CategoryStation}");
            sb.AppendLine($"CATEGORY-OVERLAY: {header.CategoryOverlay}");
            sb.AppendLine($"CATEGORY-MODE: {header.CategoryMode}");
            sb.AppendLine($"CATEGORY-POWER: {header.CategoryPower}");
            sb.AppendLine($"LOCATION: {header.Location}");
            if (header.Club != "") sb.AppendLine($"CLUB: {header.Club}");
            sb.AppendLine($"GRID-LOCATOR: {header.Locator}");
            sb.AppendLine($"NAME: {header.FullName}");

            // Split by pipe and add a line for each segment
            foreach (var line in header.Address.Split('|'))
            {
                sb.AppendLine($"ADDRESS: {line.Trim()}");
            }

            sb.AppendLine($"EMAIL: {header.Email}");

            if (header.ClaimedScore != "") sb.AppendLine($"CLAMED-SCORE: {header.ClaimedScore}");

            if (!string.IsNullOrWhiteSpace(header.Soapbox))
            {
                // Split by the standard Windows NewLine (\r\n) from multiline textbox
                string[] lines = header.Soapbox.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string line in lines)
                {
                    // Cabrillo spec prefers lines under 75 chars
                    sb.AppendLine($"SOAPBOX: {line.Trim()}");
                }
            }

            sb.AppendLine("CREATED-BY: MyG4AHNLogger v1.0");
            //sb.AppendLine("<EOH>");
            sb.AppendLine("");
            sb.AppendLine("");
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
                string formattedDate = DateTime.ParseExact(qso.Date, "yyyyMMdd", null).ToString("yyyy-MM-dd");

                string line = string.Format("QSO: {0} {1} {2} {3} {4} {5} {6} {7} {8} {9}",
                    qso.FrequencyMHz.Replace(".", "").PadLeft(5), // Freq
                    mode,                                         // Mode (e.g., RTTY)
                    formattedDate,                                // Date
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

        private void tbSoap_TextChanged(object sender, EventArgs e)
        {

        }
    }

    public class CabrilloHeader
    {
        public string Contest { get; set; }
        public string Callsign { get; set; }
        public string Club { get; set; }
        public string CategoryOp { get; set; }
        public string CategoryAssist { get; set; }
        public string CategoryOverlay { get; set; }
        public string CategoryBand { get; set; }
        public string CategoryTx { get; set; }
        public string CategoryPower { get; set; }
        public string CategoryStation { get; set; }
        public string CategoryMode { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Location { get; set; }
        public string Locator { get; set; }
        public string Address { get; set; }
        public string Operators { get; set; }
        public string ClaimedScore { get; set; }
        public string Soapbox { get; set; }
    }
}
