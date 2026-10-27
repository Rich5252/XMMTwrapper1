//Code for handlining the received text box.

namespace XMMTwrapper1
{
    public partial class Form1
    {
        private void tbTextReceived_MouseDown(object sender, MouseEventArgs e)
        {
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
                if (tbCallsign.Text != "")
                {
                    _pendingAction = () => ApplyCallsignSelection(selectedWord, LogRST, Hz);
                    cmsLogOverwriteConfirm.Text = $"Replace {tbCallsign.Text.Trim()} with {selectedWord}";
                    cmsLogOverwrite.Show(Cursor.Position);        //show context menu to confirm overwrite of callsign
                }
                else
                {
                    //no change, don't clear report in or serial in
                    ApplyCallsignSelection(selectedWord, LogRST, Hz);
                }



            }
        }

        private void ApplyCallsignSelection(string word, string? logRst, int? hz)
        {
            ClearDownUiAfterLog();              // clears tbReportOut if it was auto filled from received text
            tbCallsign.Text = word;

            if (btnJTTY.Checked && logRst != null && !(bool)tbReportOut.Tag)        // only fill if not fixed
                tbReportOut.Text = logRst.Length >= 3 ? logRst.Substring(0, 3) : logRst;

            if (btnJTTY.Checked && hz is int f && f >= 200 && f <= 5000)
            {
                _jttyLink.SendTag("RXFREQ", f.ToString());
                _jttyLink.SendTag("FTOL", "10");
            }
        }

        private void cmsLogOverwrite_Closed(object? sender, ToolStripDropDownClosedEventArgs e)
        {
            // Clicking away or pressing Esc closes the menu without an item click = cancel.
            if (e.CloseReason != ToolStripDropDownCloseReason.ItemClicked) _pendingAction = null;
        }

        private void cmsLogOverwriteConfirm_Click(object? sender, EventArgs e)
        {
            var a = _pendingAction;
            _pendingAction = null;
            a?.Invoke();
        }

        private void cmsLogOverwriteCancel_Click(object? sender, EventArgs e)
        {
            _pendingAction = null;
        }


    }
}