//Code for handlining the received text box.

namespace XMMTwrapper1
{
    public partial class Form1
    {
        public void InitializeTextReceivedUi()
        {
            cmsLogInitActions();
        }

        private void tbTextReceived_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
            {
                cmsLogOverwrite.Tag = null;
                return; // Only handle left mouse button clicks)
            }
            cmsLogOverwrite.Tag = "Active";

            // Use the bitwise AND (&) to check for specific keys
            bool isCtrlDown = (Control.ModifierKeys & Keys.Control) == Keys.Control;
            bool isShiftDown = (Control.ModifierKeys & Keys.Shift) == Keys.Shift;
            bool isAltDown = (Control.ModifierKeys & Keys.Alt) == Keys.Alt;

            if (!(sender is TextBoxBase tb)) return;

            //select word at click position in received text box and copy to correct log entry
            TextBoxWordSelect.SelectWordAtClick(tb, e);
            string selectedWord = tb.SelectedText.Trim();
            if (string.IsNullOrEmpty(selectedWord)) return;
            string LogRST = TextBoxWordSelect.GetReportOnSelectedLine(tb, selectReport: true);
            int? Hz = TextBoxWordSelect.GetFreqOnSelectedLine(tb, selectFreq: true);

            if (isCtrlDown && !isShiftDown)
            {
                // If Ctrl is down, copy locator
                tbLocator.Text = selectedWord;
            }
            else if (isShiftDown && !isCtrlDown)        // If Shift is down, copy report in
            {
                string rpt = TextBoxWordSelect.NormalizeReportWord(selectedWord);   // "+8DB" -> "+08"   "R+00" -> "+00"   "R-12" -> "-12"   "R599" -> "599" 

                if (tbReportIn.Text == "")
                {
                    tbReportIn.Text = rpt.Length >= 3 ? rpt.Substring(0, 3) : rpt;
                    tbSerialNumIn.Text = rpt.Length > 3 ? rpt.Substring(3) : "";
                }
                else
                {
                    tbSerialNumIn.Text = rpt;
                }
            }
            else if (isCtrlDown && isShiftDown)
            {
                // If both Ctrl and Shift are down, copy to Name
                tbOpName.Text = selectedWord;
            }
            else if (isAltDown)
            {
                // If Alt is down, copy to QTH
                tbQTH.Text = selectedWord;
            }
            else
            {
                // No modifier keys, copy to callsign
                QuerySetNewDxCallsign(selectedWord, LogRST, Hz);
            }
        }

        private void QuerySetNewDxCallsign(string selectedWord, string LogRST, int? Hz)
        {
            if (tbCallsign.Text != "")
            {
                //There is an entry pending in Log entry table so ask what to do.
                _pendingAction = () => ApplyCallsignSelection(selectedWord, LogRST, Hz);
                cmsLogOverwriteConfirm.Text = $"Replace {tbCallsign.Text.Trim()} with {selectedWord}";
                cmsLogOverwrite.Show(Cursor.Position);        //show context menu to confirm overwrite of callsign
            }
            else
            {
                //No current DXcallsign in Log entry table so tidy up and add selection
                ApplyCallsignSelection(selectedWord, LogRST, Hz);
            }
        }

        private void ApplyCallsignSelection(string word, string? logRst, int? hz)
        {
            ClearDownUiAfterLog();              // clears tbReportOut if it was auto filled from received text
            tbCallsign.Text = word;

            if (btnJTTY.Checked && logRst != null && !(bool)tbReportOut.Tag)        // only fill if not fixed
                tbReportOut.Text = TextBoxWordSelect.NormalizeReportWord(logRst.Length >= 3 ? logRst.Substring(0, 3) : logRst);

            if (btnJTTY.Checked && hz is int f && f >= 200 && f <= 5000)
            {
                _jttyLink.SendTag("RXFREQ", f.ToString());
                if (cbAutoFtol.Checked)
                {
                    _jttyLink.SendTag("FTOL", "10");
                }
            }
        }


        private void cmsLogInitActions()
        {
            //Log context action menu
            cmsLogOverwrite.Opening += (s, e) =>
            {
                if (cmsLogOverwrite.Tag == null)
                {
                    // If the context menu was opened by a left click, cancel the opening to prevent it from showing.
                    e.Cancel = true;
                }
            };
            cmsLogOverwrite.Closed += (s, e) =>
            {
                // Clicking away or pressing Esc closes the menu without an item click = cancel.
                if (e.CloseReason != ToolStripDropDownCloseReason.ItemClicked) _pendingAction = null;
            };

            cmsLogOverwriteConfirm.Click += (s, e) =>
            {
                var a = _pendingAction;
                _pendingAction = null;
                a?.Invoke();
            };

            cmsLogOverwriteCancel.Click += (s, e) => { _pendingAction = null; };

            cmsLogQSOFirst.Click += (s, e) =>
            {
                bool success = LogQSO();      //returns true if QSO logged, false if not
                if (success)                //if logging was successful, then clear the pending action and invoke it
                {
                    var a = _pendingAction;
                    _pendingAction = null;
                    a?.Invoke();
                }
            };

            //Log save context menu actions
            cmsLogSaveMenuLogQSO.Click += (s, e) =>
            {
                LogQSO();
                _pendingAction = null;
            };

            cmsLogSaveMenuCancel.Click += (s, e) => { _pendingAction = null; };

            cmsLogSave.Closed += (s, e) =>
            {
                // Clicking away or pressing Esc closes the menu without an item click = cancel.
                if (e.CloseReason != ToolStripDropDownCloseReason.ItemClicked) _pendingAction = null;
            };

            cmsLogSaveMenuClearLog.Click += (s, e) =>
            {
                ClearDownUiAfterLog();
                _pendingAction = null;
            };

        }
    }
}