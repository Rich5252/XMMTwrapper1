
using System.Diagnostics.Eventing.Reader;
using System.Runtime.InteropServices;

namespace XMMTwrapper1
{
    public partial class Form1 : Form
    {
        private string lastChar = "";                   //remember last char from RTTY decoder                                                       //to sort out errors in cr/lf

        // This will automatically save to: C:\YourAppPath\settings.ini
        private static IniManager _ini = new IniManager("settings.ini");

        private List<Button> _sendButtonList = new List<Button>();      //send buttons collection
        private MacroProvider _macroProvider = new MacroProvider(_ini);

        // Place constants and DLL Imports at the top of the class
        private const int WM_HOTKEY = 0x0312;
        private const int MY_HOTKEY_ID = 1;

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        public Form1()
        {
            Log.Mark("Form1 before init");
            InitializeComponent();

            InitializeJttyUi();

            InitializeCWDecoder();

            // Populate the send button list
            _sendButtonList.Add(btnSend1);
            _sendButtonList.Add(btnSend2);
            _sendButtonList.Add(btnSend3);
            _sendButtonList.Add(btnSend4);
            _sendButtonList.Add(btnSend5);
            _sendButtonList.Add(btnSend6);
            _sendButtonList.Add(btnSend7);
            _sendButtonList.Add(btnSend8);
            _sendButtonList.Add(btnSend9);
            _sendButtonList.Add(btnSend10);
            _sendButtonList.Add(btnSend11);
            _sendButtonList.Add(btnSend12);
            _sendButtonList.Add(btnSend13);
            _sendButtonList.Add(btnSend14);

            // Now you can call your Load method
            UpdateButtonMacros();

            Log.Mark("Form1 after init");
            this.Shown += Form1_Shown;
        }

        //intercept F8 to put focus on Callsign entry text box for next DX callsign KB input
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            // Register F8 when the form is ready 0x7B (F12) to 0x7A (F11) or 0x78 (F9)
            RegisterHotKey(this.Handle, MY_HOTKEY_ID, 0, 0x77);
        }

        //action F8
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == MY_HOTKEY_ID)
            {
                IntPtr foregroundWindow = GetForegroundWindow();
                bool appWasAlreadyActive = (foregroundWindow == this.Handle);

                // 1. Always bring to front and restore
                if (this.WindowState == FormWindowState.Minimized)
                    this.WindowState = FormWindowState.Normal;

                this.Activate();

                // 2. The Logic: F8 clears the callsign box only if the app was already active
                //                  otherwise first click just focuses the box without clearing
                // ONLY clear if the app was already in front AND the box was already active
                if (appWasAlreadyActive && this.ActiveControl == tbCallsign)
                {
                    ClearDownUiAfterLog();
                }
                else
                {
                    // Just move the cursor there without clearing
                    tbCallsign.Focus();
                    //option to move caret to end of text
                    //tbCallsign.SelectionStart = tbCallsign.Text.Length;
                }
            }
            base.WndProc(ref m);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            BuildMmttyButtons();
            _serial.Connect("COM3", 115200);            //Keyer

            this.Left = TextToInt(_ini.GetValue("General", "Left", "100"));
            this.Top = TextToInt(_ini.GetValue("General", "Top", "100"));
            this.Height = TextToInt(_ini.GetValue("General", "Height", "572"));
            this.Width = TextToInt(_ini.GetValue("General", "Width", "820"));

            string mode = _ini.GetValue("General", "Mode", "CW");
            if (mode == "CW")
                btnCW.Checked = true;
            else if (mode == "RTTY")
                btnRTTY.Checked = true;
            else
                btnJTTY.Checked = true;     //default if not set or invalid value


            //restore contest settings
            tbReportOut.Text = _ini.GetValue("General", "ReportOut", "599");
            tbSerialNumOut.Text = _ini.GetValue("General", "SerialNumOut", "");
            tbReportIn.Text = _ini.GetValue("General", "ReportIn", "599");
            tbSerialNumIn.Text = _ini.GetValue("General", "SerialNumIn", "");
            string autoInc = _ini.GetValue("General", "AutoIncr", "false");
            cbSerialNumAutoInc.Checked = (autoInc == "True");

            InitQsoLogger();
        }

        private void Form1_Shown(object? sender, EventArgs e)
        {
            Log.Mark("Form1_Shown entered");
            Log.LogThreadDetails("Form1_Shown");

            //XmmrRawEventHook event message callbacks
            if (_hook != null)      //should not be set already here
            {
                Log.Mark("Form1_Shown _hook not null");
                return;
            }
            else
            {
                _hook = new XmmrRawEventHook(axXMMR1);
                _hook.OnNotifyFftRaw += OnNotifyFftRaw;
                _hook.OnNotifyGetXY += OnNotifyGetXY;
                _hook.OnNotifyNMMR += OnNotifyNMMR;
                _hook.AcceptFft = false;                    // ignore early noise
                _hook.Advise();
            }

            //setup button group defaults
            _MmttyButtons.ApplyMmttyBtnSetup();

            //start MMTTY app in background
            StartMmttyEngine();

            StartJttyLink();            //link to WS JTTY server for RTTY decode and TX

            SelectMode();           //init mode UI
            UpdateButtonMacros();   //init Send button macros & labels

            //diagnostic to explore ocx interface
            //DumpSpecDrawSignature();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (this.WindowState == FormWindowState.Normal)
            {
                _ini.SetValue("General", "Left", $"{this.Left}");
                _ini.SetValue("General", "Top", $"{this.Top}");
                _ini.SetValue("General", "Width", $"{this.Width}");
                _ini.SetValue("General", "Height", $"{this.Height}");
            }
            _ini.SetValue("FixedSubs", "myPower", tbPwr.Text);
            _ini.SetValue("General", "Mode", btnCW.Checked ? "CW" : btnRTTY.Checked ? "RTTY" : "JTTY");

            //save contest settings
            _ini.SetValue("General", "ReportOut", tbReportOut.Text);
            _ini.SetValue("General", "SerialNumOut", tbSerialNumOut.Text);
            _ini.SetValue("General", "ReportIn", tbReportIn.Text);
            _ini.SetValue("General", "SerialNumIn", tbSerialNumIn.Text);
            _ini.SetValue("General", "AutoIncr", $"{cbSerialNumAutoInc.Checked}");
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            StopJttyLink();
            _hook?.Dispose();
            base.OnFormClosed(e);
        }



        string strWordReceived = "";            //build word to see if its a callsign
        private void DisplayNextChar(string s)
        {
            //filter CR & LF so textbox shows CRLF correctly on either 
            //  received singley or once when both received together.
            if ((s == "\r" || s == "\n") && lastChar != "\r\n")
            {
                tbTextReceived.AppendText(Environment.NewLine);
                lastChar = Environment.NewLine;
            }
            else
            {
                tbTextReceived.AppendText(s);
                lastChar = s;
            }
            tbTextReceived.SelectionStart = tbTextReceived.Text.Length;
            tbTextReceived.SelectionLength = 0;
            tbTextReceived.ScrollToCaret();

            string strSuggestedCallsign = ITU_Prefix_Checker.IsCallsign(lastChar);
            if (strSuggestedCallsign != "")
            {
                tbCallSignList.AppendText(strSuggestedCallsign);
                tbCallSignList.AppendText(Environment.NewLine);
            }

        }

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

            if (isCtrlDown && !isShiftDown)
            {
                // If Ctrl is down, copy locator
                tbLocator.Text = selectedWord;
            }
            else if (isShiftDown && !isCtrlDown)        // If Shift is down, copy report in
            {
                if (tbReportIn.Text == "")
                {
                    tbReportIn.Text = selectedWord.Length >= 3 ? selectedWord.Substring(0, 3) : selectedWord;
                    tbSerialNumIn.Text = selectedWord.Length > 3 ? selectedWord.Substring(3) : "";
                }
                else
                {
                    // If Shift is down and report in already has value, copy to serial number in
                    tbSerialNumIn.Text = selectedWord;
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
                ClearDownUiAfterLog();
                tbCallsign.Text = selectedWord;
            }
        }

        private void PipeConnectTimer_Tick(object sender, EventArgs e)
        {
            if (!_NamedPipeClient.TryConnect())
            {
                lblPipeStatus.Text = "TXLink Message Pipe Trying To Connect";
                return;
            }
            else
            {
                lblPipeStatus.Text = "TXLink Message Pipe Connected";
            }

            string str = _NamedPipeClient.PollForMessage();
            if (!string.IsNullOrEmpty(str))
            {
                // Process the command message
                if (str.StartsWith("{F") && str.EndsWith("}") && str.Length > 3)
                {
                    str = str.Substring(2, str.Length - 3);
                    for (int i = str.Length - 3; i > 0; i -= 3)
                    {
                        str = str.Insert(i, ".");
                    }
                }
                tbRxFrequency.Text = str;
            }
        }

        private void btnCW_CheckedChanged(object sender, EventArgs e)
        {
            SelectMode();

        }

        private void btnRTTY_CheckedChanged(object sender, EventArgs e)
        {
            SelectMode();
        }

        //Setup selected CW or RTTY in GUI by moving required groupbox controls
        private void SelectMode()
        {
            if (btnCW.Checked == true)
            {
                tbTextReceived.Text = "";
                _decoder.Start();
                gbJTTYcontrols.Left = -500;
                gbRTTYbuttons.Left = -500;
                gbCWcontrols.Left = 12;
                gbCWcontrols.Visible = true;
                if (CWinputSensitivity > 0 && (_decoder != null))
                {
                    _decoder.SetInputScale((double)CWinputSensitivity);
                }

                //update Squelch to current axxMMLevel value, stored in _currentNmmr._currentNmmr
                SendSquelchFromXMMLevel((int)_currentNmmr.SqLevel);
            }

            if (btnRTTY.Checked == true)
            {
                tbTextReceived.Text = "";
                gbJTTYcontrols.Left = -500;
                gbCWcontrols.Left = -500;
                gbRTTYbuttons.Left = 12;
                gbRTTYbuttons.Visible = true;
            }

            if (btnJTTY.Checked == true)
            {
                tbTextReceived.Text = "";
                gbCWcontrols.Left = -500;
                gbRTTYbuttons.Left = -500;
                gbJTTYcontrols.Left = 12;
                gbJTTYcontrols.Visible = true;
            }
            UpdateButtonMacros();
        }


        //double click in suggested callsign list copies entry to DXcall text box
        private void tbCallSignList_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            //transfer callsign to active callsign to call or log
            ClearDownUiAfterLog();
            tbCallsign.Text = tbCallSignList.SelectedText;
        }

        private void btnSerialNumInc_Click(object sender, EventArgs e)
        {
            ClearSerialNumOutTim();
            tbSerialNumOut.Increment();
        }

        private void btnSerialNumDec_Click(object sender, EventArgs e)
        {
            ClearSerialNumOutTim();
            tbSerialNumOut.Decrement();
        }


        private void exportCabrilloToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GenerateCabrillo();
        }

        private void openLogDirectoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _QSOlogger.OpenLogDirectory();
        }

        private void createNewLogBookToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string fileName = InputBox.Show("Enter new logbook name (without extension)", "New Logbook", "MyContestLog");

            if (!string.IsNullOrEmpty(fileName))
            {
                _QSOlogger.CreateNewLogFile(fileName);  //can create new or open existing log with option to overwrite or append
                tbNumberQSOs.Text = _QSOlogger.TotalLogged.ToString();
                lblLogFileName.Text = _QSOlogger.LogFileName;
            }
        }

        private void tbFreeText_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13) // Enter key
            {
                e.Handled = true;
                SendOutputMacro(tbFreeText.Text);

                if (cbAutoClearFreeText.Checked)
                {
                    tbFreeText.Clear();
                }
            }
        }
    }
}
