// This file contains all the Form GUI stuff associated with the QSO logging activity
// 


namespace XMMTwrapper1
{
    
    public partial class Form1
    {
        private AdifLogger _QSOlogger = new AdifLogger(_ini);      //file in app folder
        private QrzService _qrzService = new QrzService();

        private void InitQsoLogger()
        {
            _qrzService.SetCredentials();
            Log.Perf($"QRZ login: {_qrzService.EnsureLoggedInAsync()}");

            tbPwr.Text = _ini.GetValue("FixedSubs", "myPower", "100");
            _QSOlogger.Initialize();        //creates file (if needed) with header & then loads it
            tbNumberQSOs.Text = _QSOlogger.TotalLogged.ToString();
            lblLogFileName.Text = _QSOlogger.LogFileName;
        }

        private void btnLogQSO_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                //refresh list from file
                _QSOlogger.LoadLogFile();
                tbNumberQSOs.Text = _QSOlogger.TotalLogged.ToString();
                lblLogFileName.Text = _QSOlogger.LogFileName;
                return;
            }

            // otherwise log a QSO
            QsoEntry newEntry = new QsoEntry();

            newEntry.Callsign = tbCallsign.Text;                        
            newEntry.FrequencyMHz = NormalizeFreqKhz(tbRxFrequency.Text);       //28.189.000 in UI, needs 28.180 in log                                //in log kHz res format eg 28.180
            newEntry.Timestamp = DateTime.UtcNow;
            newEntry.Mode = btnCW.Checked ? "CW" : btnRTTY.Checked ? "RTTY" : "MFSK";
            if (btnJTTY.Checked) newEntry.SubMode = "JTTY";

            //<mode:4>MFSK <submode:3>FT4
            //combine report & serial num
            string str = tbReportOut.Text;
            if (tbSerialNumOut.Text != "") str = str + " " + tbSerialNumOut.Text;
            newEntry.RstSent = str;
            str = tbReportIn.Text;
            if (tbSerialNumIn.Text != "") str = str + " " + tbSerialNumIn.Text;
            newEntry.RstRcvd = str;

            newEntry.Comment = tbLocator.Text;
            newEntry.Remarks = tbQTH.Text + " " + tbComment.Text;
            newEntry.Power = tbPwr.Text;


            if (!_QSOlogger.LogQso(newEntry))
            {
                MessageBox.Show("Error: valid Callsign & Frequency not specified");
                return;
            }

            ClearDownUiAfterLog();
            tbNumberQSOs.Text = _QSOlogger.TotalLogged.ToString();
            lblLogFileName.Text = _QSOlogger.LogFileName;
            if (cbSerialNumAutoInc.Checked) { tbSerialNumOut.Increment(); }
        }

        /// Normalises a frequency entry to MHz.kHz:
        ///   "14" -> "14"   "14.09" -> "14.090"   "14.090" -> "14.090"   "14.090.000" -> "14.090"
        /// A 2nd group is the kHz part, right-padded to 3 digits ("09" = 090, as in a decimal "14.09").
        /// More than 3 digits there is truncated to kHz ("14.0905" -> "14.090"). A 3rd group (Hz) is dropped.
        /// Returns null if the entry isn't a number in one of these forms.
        public static string? NormalizeFreqKhz(string? entry)
        {
            if (string.IsNullOrWhiteSpace(entry)) return null;

            string[] p = entry.Trim().Split('.');
            if (p.Length > 3 || p.Any(g => !g.All(char.IsAsciiDigit))) return null;
            if (p[0].Length == 0) return null;

            string mhz = p[0].TrimStart('0');
            if (mhz.Length == 0) mhz = "0";
            if (p.Length == 1 || p[1].Length == 0) return mhz;

            string khz = p[1].Length > 3 ? p[1][..3] : p[1].PadRight(3, '0');
            return $"{mhz}.{khz}";
        }

        private void ClearDownUiAfterLog()
        {
            tbCallsign.Text = "";
            tbLocator.Text = "";
            tbQTH.Text = "";
            tbOpName.Text = "";
            tbComment.Text = "";
            ClearSerialNumOutTim();
            if (tbReportOut.Tag != null && !(bool)tbReportOut.Tag) { tbReportOut.Text = ""; }          //clear if not fixed
            if (tbReportIn.Tag != null && !(bool)tbReportIn.Tag) { tbReportIn.Text = ""; }            
            if (tbSerialNumIn.Tag != null && !(bool)tbSerialNumIn.Tag) { tbSerialNumIn.Text = ""; } 
            if (tbSerialNumOut.Tag != null && !(bool)tbSerialNumOut.Tag) { tbSerialNumOut.Text = ""; }
        }


        private void ClearSerialNumOutTim()
        {
            //special case BARTG contest using HHMM after serial, eg 001-1533
            //remove time when changing serial ready for next QSO
            if (tbSerialNumOut.Text.Contains("-"))
            {
                //remove -HHMM
                int index = tbSerialNumOut.Text.IndexOf("-");
                tbSerialNumOut.Text = tbSerialNumOut.Text.Substring(0, index);
            }
        }

        //this is also called when tbFreq changes
        private void tbCallsign_TextChanged(object sender, EventArgs e)
        {
            int dupeIndex = _QSOlogger.GetDupeIndex(tbCallsign.Text, NormalizeFreqKhz(tbRxFrequency.Text));
            if (dupeIndex == -1)
            {
                lblDUPE.Visible = false;
                lblDupeTimeDate.Visible = false;
                tbDupeTimeDate.Visible = false;
                tbDupeTimeDate.Text = "";
            }
            else 
            {
                lblDUPE.Visible = true; 
                lblDupeTimeDate.Visible = true;
                tbDupeTimeDate.Visible = true;
                tbDupeTimeDate.Text = _QSOlogger.GetQsoTimeText(dupeIndex);
            }
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

        private async void btnLookup_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                //reset login
                _qrzService.SetCredentials(true);
                return;
            }

            if (await _qrzService.GetCallsignDataAsync(tbCallsign.Text) is { } data)
            {
                // Inside this block, 'data' is guaranteed not to be null
                tbOpName.Text = data.Name;
                tbLocator.Text = data.Grid;
                tbQTH.Text = data.City;

                string USinfo = "";
                if (!string.IsNullOrEmpty(data.UScounty)) USinfo = data.UScounty.Trim() + " CTY";
                if (!string.IsNullOrEmpty(data.USstate)) USinfo = USinfo + " " + data.USstate.Trim();
                if (!string.IsNullOrEmpty(USinfo)) tbComment.Text = USinfo;
            }
            else
            {
                tbComment.Text = "Not found in QRZ";
            }  
        }


        //Context menu for report text boxes to set fix/auto mode.
        private void contextMenuStripReportFields_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var menu = (ContextMenuStrip)sender!;
            var tb = menu.SourceControl as TextBoxBase;
            fixToolStripMenuItem.Checked = tb?.Tag is bool isChecked && isChecked;
        }
        private void fixToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var tb = SourceBox(sender);
            if (tb == null) return;
            tb.Tag = fixToolStripMenuItem.Checked;
        }
        private void clearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var tb = SourceBox(sender);
            tb.Clear();
        }

        // Find the text box the menu was opened on, from a clicked menu item
        private static TextBoxBase? SourceBox(object? sender)
        {
            if (sender is ToolStripItem item && item.Owner is ContextMenuStrip menu)
                return menu.SourceControl as TextBoxBase;
            return null;
        }
    }
}