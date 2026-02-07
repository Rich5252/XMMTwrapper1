// This file contains all the Form GUI stuff associated with the QSO logging activity
// 


namespace XMMTwrapper1
{
    public partial class Form1
    {
        private AdifLogger _QSOlogger = new AdifLogger("CW-RTTY.ADI");      //file in app folder

        private void InitQsoLogger()
        {
            tbPwr.Text = _ini.GetValue("FixedSubs", "myPower", "100");
            _QSOlogger.Initialize();        //creates file (if needed) with header & then loads it
            tbNumberQSOs.Text = _QSOlogger.TotalLogged.ToString();
        }

        private void btnLogQSO_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                //refresh list from file
                _QSOlogger.LoadLogFile();
                tbNumberQSOs.Text = _QSOlogger.TotalLogged.ToString();
                return;
            }

            // otherwise log a QSO
            QsoEntry newEntry = new QsoEntry();

            newEntry.Callsign = tbCallsign.Text;
            string freq = tbRxFrequency.Text;
            if (freq.Length > 4) freq = freq.Substring(0, freq.Length-4);       //28.189.000 in UI, needs 28.180 in log
            newEntry.FrequencyMHz = freq;                                       //in log kHz res format eg 28.180
            newEntry.Timestamp = DateTime.UtcNow;
            newEntry.Mode = btnCW.Checked ? "CW" : "RTTY";

            //combine report & serial num
            string str = tbReportOut.Text;
            if (tbSerialNumOut.Text != "") str = str + " " + tbSerialNumOut.Text;
            newEntry.RstSent = str;
            str = tbReportIn.Text;
            if (tbSerialNumIn.Text != "") str = str + " " + tbSerialNumIn.Text;
            newEntry.RstRcvd = str;

            newEntry.Comment = tbLocator.Text;
            newEntry.Remarks = tbComment.Text;
            newEntry.Power = tbPwr.Text;

            if (!_QSOlogger.LogQso(newEntry))
            {
                MessageBox.Show("Error: valid Callsign & Frequency not specified");
                return;
            }

            tbCallsign.Text = "";
            tbNumberQSOs.Text = _QSOlogger.TotalLogged.ToString();
            if (cbSerialNumAutoInc.Checked) { tbSerialNumOut.Increment(); }
        }

        //this is also called when tbFreq changes
        private void tbCallsign_TextChanged(object sender, EventArgs e)
        {
            string freq = tbRxFrequency.Text;
            if (freq.Length > 4) freq = freq.Substring (0, freq.Length-4);
            int dupeIndex = _QSOlogger.GetDupeIndex(tbCallsign.Text, freq);
            if (dupeIndex == -1) {lblDUPE.Visible = false;}
            else {lblDUPE.Visible = true;}
        }


        private void GenerateCabrillo()
        {
            using (var headerForm = new CabrilloHeaderForm(_ini))
            {
                if (headerForm.ShowDialog() == DialogResult.OK)
                {
                    SaveFileDialog sfd = new SaveFileDialog { Filter = "Cabrillo Log|*.cbr" };
                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        headerForm.ExportToCabrillo(sfd.FileName, headerForm.HeaderData, _QSOlogger._loggedEntries);
                        MessageBox.Show("Cabrillo log created successfully!");
                    }
                }
            }
        }
    }
}