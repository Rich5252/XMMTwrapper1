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
            tbOpNames.Text = ini.GetValue("Station", "Operators", "Richard Lax");
            tbClub.Text = ini.GetValue("Station", "Club", "");
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            // Capture the UI data into a simple class
            HeaderData = new CabrilloHeader
            {
                Contest = tbContest.Text,
                Callsign = tbCallsign.Text,
                CategoryOp = tbCatOp.Text,
                CategoryTx = tbCatTx.Text,
                CategoryPower = tbCatPwr.Text,
                Operators = tbOpNames.Text,
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
    }

    public class CabrilloHeader
    {
        public string Contest { get; set; }
        public string Callsign { get; set; }
        public string CategoryOp { get; set; }
        public string CategoryTx { get; set; }
        public string CategoryPower { get; set; }
        public string Operators { get; set; }
        public string ClaimedScore { get; set; }
        public string Soapbox { get; set; }
    }
}
