using AxXMMTLib;            //XMMR.ocx support for MMTTY
using Constants;            //see XmmrOcxInteropConstants.cs - various constants/enum etc for XMMR/MMTTY
using IniParser;            //ini file support
using NAudio.Wave;          //audio device support
using NmmrContract;         //see XmmrOcxInteropConstants.cs
using System.Runtime.InteropServices;

namespace XMMTwrapper1
{
    public partial class Form1 : Form
    {

        // --- XMMR Data Structures and Indexes ---
        // See XmmrOcxInteropConstants.cs for XMMR.OCX defines and further details of data

        // VB6: Private m_nmmr(63) As Long
        // C#: A private array of 64 integers (0 to 63) to store the MMTTY engine status block.
        private readonly object _nmmrLock = new();
        private Nmmr _currentNmmr;   // last known full snapshot from OCX. NMMR[] contains MMTTY status/settings

        private readonly System.Windows.Forms.Timer _specTimer = new() { Interval = 50 }; // 20 fps
        private XmmrRawEventHook? _hook;                //COMconnectionPointHost callbacks

        private MmttySwitchButtonGroup? _MmttyButtons;  //MMTTY standard button group

        private string lastChar = "";                   //remember last char from RTTY decoder                                                       //to sort out errors in cr/lf

        private NamedPipeClient _NamedPipeClient = new();       //coms with TXLink for tx commands and status (eg current rx freq)
        private SerialMessageHandler? _serial = new();          //COM with keyer for CW tx commands

        private CWDecoderEngine _decoder;               //CW decoder module

        private int CWinputSensitivity = 200;        // controlled by slider bar
        private double SignalStrength = 0;

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

            _decoder = new CWDecoderEngine();

            // Subscribe to the engine's events
            _decoder.OnMagnitudeUpdated = (mag) =>
            {
                this.BeginInvoke(new Action(() =>
                {
                    // Update a signal strength bar
                    //bar matches axxmmrLevel1 bar. control goes from 0 to 1000
                    //  represents -50 to +30dB. The mag from decoder gives this range
                    double magdB = 20 * Math.Log10(mag);
                    //scaled value
                    double val = Math.Max(0, (magdB + 50) / (30 + 50));         //clamp to above zero
                    SignalStrength = Math.Min(1000, (int)(val * 1000));         //max 1000, used to drive axxMMRlevel control
                }));
            };

            _decoder.OnSNratioUpdated = (SN) =>
            {
                this.BeginInvoke(new Action(() =>
                {
                    // Update a SN ratio text box
                    tbSNratio.Text = SN.ToString("F1");
                }));
            };

            _decoder.OnToneDetected = (isToning) =>
            {
                this.BeginInvoke(new Action(() =>
                {
                    // Change a "Light" color to indicate Key Down
                    pnlKeyIndicator.BackColor = isToning ? Color.Red : Color.Gray;
                }));
            };

            _decoder.OnCharacterDecoded = (c) =>
            {
                if (btnRTTY.Checked) return;

                this.BeginInvoke(new Action(() =>
                {
                    DisplayNextChar(c.ToString());
                }));
            };

            _decoder.OnSequenceChanged = (seq) =>
            {
                this.BeginInvoke(new Action(() =>
                {
                    lblCurrentSequence.Text = seq + " "; // Shows ".-" etc.
                }));
            };

            _decoder.OnWpmChanged = (wpm) =>
            {
                this.BeginInvoke(new Action(() =>
                {
                    tbWPM.Text = wpm.ToString(); // Shows current WPM
                }));
            };
            PopulateAudioMenu();

            // Optional: Auto-select the first device (usually Default Mapper)
            if (audioSelectToolStripMenuItem.DropDownItems.Count > 0)
            {
                DeviceItem_Click(audioSelectToolStripMenuItem.DropDownItems[0], null);
            }

            // Populate the send button list
            _sendButtonList.Add(btnSend1);
            _sendButtonList.Add(btnSend2);
            _sendButtonList.Add(btnSend3);
            _sendButtonList.Add(btnSend4);
            _sendButtonList.Add(btnSend5);
            _sendButtonList.Add(btnSend6);

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
                    tbCallsign.Clear();
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
            else
                btnRTTY.Checked = true;

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
            _ini.SetValue("General", "Mode", btnCW.Checked ? "CW" : "RTTY");
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _hook?.Dispose();
            base.OnFormClosed(e);
        }

        private void BuildMmttyButtons()
        {
            //create MMTTY buttons in Group1 container
            _MmttyButtons = new MmttySwitchButtonGroup(
                btnTemplate,
                parent: gbRTTYbuttons,  // or a Panel/GroupBox
                left: 10,
                top: 10,
                columns: 5);

            _MmttyButtons.SwitchChangedByUser += (flags, btn) =>
            {
                // Send the updated mask back to mmTTY via OCX
                if (flags != 0)
                {
                    axXMMR1.SetMmttySwitch(unchecked((int)(uint)flags));
                }
                else
                {
                    //must be an extra button in group (eg Ham)
                    axXMMR1.PostMmttyMessage((int)RxmMessage.SetHam, 1);
                    btn.CtlState = false;
                }
            };
        }

        // When you receive NMMR from OnNotifyNMMR:
        private void OnNmmrUpdated(uint codeswitch)
        {
            _MmttyButtons?.SetFromMmtty(codeswitch);

            //also the Discriminator filter select
            uint filter = codeswitch & 0x03;

            DiscrimBtn0.Checked = (filter == 0);
            DiscrimBtn1.Checked = (filter == 1);
            DiscrimBtn2.Checked = (filter == 2);
            DiscrimBtn3.Checked = (filter == 3);
        }

        private Nmmr GetCurrentNmmr()
        {
            lock (_nmmrLock) return _currentNmmr;
        }


        void StartMmttyEngine()
        {
            Log.LogThreadDetails("StartMmttyEngine()", axXMMR1);
            axXMMR1.InvokeCommand = "C:\\Ham\\MMTTY\\MMTTY.exe -m";

            // Connect to the MMTTY engine
            axXMMR1.bActive = true;

            // Request notification events for the FFT/Spectra data
            axXMMR1.bNotifyFFT = true;

            //Log.Mark("MMTTY engine invoke , axXMMR1 sent");
            Log.LogThreadDetails("MMTTY engine invoke instruction sent", axXMMR1);

            //ShowStatus();
        }


        //Event message callback when OCX NotifyXY event is intercepted
        //Receives XY data array from XmmrEventSink via Form event message
        // [id(0x00000007)]  XY.Draw(long* pXY);
        private void OnNotifyGetXY(int[] XY)
        {
            IntPtr pXY = AllocXYBuffer(XY);
            try
            {
                //emulate VB6: Call XYscope.Draw(pXY)
                //Draw XY data in XY control
                XMMSpecComMsgDispatch.InvokeSendIntArray(axxmmxy1.GetOcx(), pXY, 0x00000007 /* XY.Draw */);
            }
            finally
            {
                Marshal.FreeHGlobal(pXY);
            }

            return;
        }



        //Event message callback when OCX NotifyNMMR event is intercepted
        private void OnNotifyNMMR(int[] NMMRarray)
        {
            //Log.LogThreadDetails("[[OnNotifyNMMR no action]]");

            IntPtr pNMMR = AllocNmmrBuffer(NMMRarray);

            try
            {
                // Map the incoming array memory to a managed structure snapshot
                Nmmr nmmr = Marshal.PtrToStructure<Nmmr>(pNMMR);
                //modify it
                if (btnCW.Checked)
                {
                    nmmr.SigLevel = (uint)SignalStrength;          //replace MMRRTY level with CW engine
                    SendSquelchFromXMMLevel((int) nmmr.SqLevel);   //send squelch level to CW engine
                }
                // Copy the modified managed structure BACK into the unmanaged buffer
                Marshal.StructureToPtr(nmmr, pNMMR, false);

                //emulate VB6: Call Level.UpdateByNMMR(pNMMR)
                //Data Array contains various data to update controls (see XmmrNmmrContract.cs)
                //Send level to draw on level control
                XMMSpecComMsgDispatch.InvokeSendIntArray(axXMMLevel1.GetOcx(), pNMMR, XMMLvlContract.DISPID_DrawByNMMR);

                //emulate VB6: Call Spectrum.UpdateByNMMR(pNMMR)
                //Send settings for spectrum control
                XMMSpecComMsgDispatch.InvokeSendIntArray(axXMMSpec1.GetOcx(), pNMMR, XMMSpecContract.DISPID_UpdateByNMMR);

                //Create a local copy for use in Form1 that uses a few of the data values, eg CodeSwitch
                // and needs to update and return the entire array. The lock ensures array integrity.
                //Nmmr nmmr = Marshal.PtrToStructure<Nmmr>(pNMMR);
                lock (_nmmrLock)
                {
                    _currentNmmr = nmmr;
                }

                //Update buttons as recieved from MMTTY settings
                OnNmmrUpdated(nmmr.CodeSwitch);
            }
            finally
            {
                Marshal.FreeHGlobal(pNMMR);
            }
        }

        private static IntPtr AllocXYBuffer(int[] xy1024)
        {
            if (xy1024 is null) throw new ArgumentNullException(nameof(xy1024));
            if (xy1024.Length != 1024) throw new ArgumentException("Must be 1024 elements.");

            IntPtr p = Marshal.AllocHGlobal(1024 * sizeof(int));
            Marshal.Copy(xy1024, 0, p, 1024);
            return p;
        }

        private static IntPtr AllocNmmrBuffer(int[] Nmmr64)
        {
            int elements = Marshal.SizeOf<Nmmr>() / sizeof(int);
            if (Nmmr64 is null) throw new ArgumentNullException(nameof(Nmmr64));
            if (Nmmr64.Length != elements) throw new ArgumentException("Must be 64 elements.");

            IntPtr p = Marshal.AllocHGlobal(elements * sizeof(int));
            Marshal.Copy(Nmmr64, 0, p, elements);
            return p;
        }


        //Event message callback when OCX NotifySpec event is intercepted
        // This is called from the XmmrEventSink via a Form event message
        private void OnNotifyFftRaw(short[] fftRaw, short size, short sampfreq)
        {
            if (fftRaw == null || fftRaw.Length == 0) return;
            if (size <= 0) return;

            // Clamp to available data (defensive)
            int n = Math.Min(size, (short)fftRaw.Length);

            // Pin the managed array so we can pass a stable pointer into COM
            var handle = GCHandle.Alloc(fftRaw, GCHandleType.Pinned);
            try
            {
                //emulate VB6: Call Spectram.Draw(pFFT, size, sampfreq)

                IntPtr pFirst = handle.AddrOfPinnedObject(); // points to fftRaw[0] as short*
                //send FFT data to Spectrum control
                XMMSpecComMsgDispatch.InvokeSpecDraw(
                    axXMMSpec1.GetOcx(),   // you can also try axXMMSpec1 itself if needed
                    pFirst,
                    (short)n,
                    sampfreq,
                    XMMSpecComMsgDispatch.DISPID_SpecDraw
                );

                //emulate VB6: Call WaterFall.Draw(pFFT, size, sampfreq)

                //send FFT data waterfall
                XMMSpecComMsgDispatch.InvokeSpecDraw(
                    axxmmWater1.GetOcx(),   // you can also try axxmmWater1 itself if needed
                    pFirst,
                    (short)n,
                    sampfreq,
                    XMMSpecComMsgDispatch.DISPID_WaterfallDraw
                );

            }
            finally
            {
                handle.Free();
            }
        }


        // C# method equivalent to your VB6 ShowStatus() Sub
        private void ShowStatus()
        {
            // VB6: If XMMR.Item(1).bActive Then ...
            if (axXMMR1.bActive)
            {
                // axXMMR1.verMMTTY will provide the version string
                lblMMTTYstatus.Text = $"Connected to MMTTY {axXMMR1.verMMTTY}";
            }
            else if (axXMMR1.bInvoking)
            {
                lblMMTTYstatus.Text = "Invoking...";
            }
            else
            {
                lblMMTTYstatus.Text = "Disconnected";
            }
        }

        // Equivalent to VB6: Private Sub XMMR_OnConnected(Index As Integer)
        private void axXMMR1_OnConnected_1(object sender, EventArgs e)
        {
            // VB6: XYscope.DemSampleFreq = XMMR.Item(1).smpDemFreq
            // Set the sample frequency property on your visual display control
            // axXYScope1.DemSampleFreq = axXMMR1.smpDemFreq; 


            Log.Mark("MMTTY engine started and in event axXMMR1_OnConnected_1");
            ShowStatus();

            Log.Mark("[UI] Engine connected — enabling FFT");
            _hook.AcceptFft = true;


            // Call UpdateMenus if you implement it (to enable/disable profile menu items)
            // UpdateMenus(); 
        }

        // Equivalent to VB6: Private Sub XMMR_OnDisconnected(Index As Integer, ByVal status As Integer
        private void axXMMR1_OnDisconnected(object sender, _DXMMREvents_OnDisconnectedEvent e)
        {
            switch (e.status)
            {
                case 0:
                    lblMMTTYstatus.Text = "MMTTY closing failed.";
                    break;
                case 2:
                    lblMMTTYstatus.Text = "MMTTY invoking failed.";
                    // If invoking failed, prompt the user for the path as in VB6
                    string prompt = "Probably, MMTTY was not invoked.\r\n\r\n" +
                                    "Enter full path name.\r\n" +
                                    "Or copy mmtty.exe and userpara.ini to the current folder.";

                    // This calls the method to prompt for a new invoke command
                    //InputInvokeCommand(prompt);
                    break;
                default:
                    lblMMTTYstatus.Text = "Disconnected (Status: " + e.status + ")";
                    break;
            }
        }

        // following mouse events set Squelch level in level control
        // following mouse events set SQL level in level control
        private void axXMMLevel1_OnLMouseMove(object sender, _DXMMLvlEvents_OnLMouseMoveEvent e)
        {
            axXMMLevel1_UseLevel((int)RxmMessage.SetSqLevel, e.level);
        }

        private void axXMMLevel1_OnLMouseDown(object sender, _DXMMLvlEvents_OnLMouseDownEvent e)
        {
            axXMMLevel1_UseLevel((int)RxmMessage.SetSqLevel, e.level);
        }

        private void axXMMLevel1_OnLMouseUp(object sender, _DXMMLvlEvents_OnLMouseUpEvent e)
        {
            axXMMLevel1_UseLevel((int)RxmMessage.SetSqLevel, e.level);
        }

        private void axXMMLevel1_UseLevel(object sender, int eLevel)
        {
            //set the bar
            axXMMR1.PostMmttyMessage((int)RxmMessage.SetSqLevel, eLevel);

            //set the CW engine
            if (btnCW.Checked)
            {
                SendSquelchFromXMMLevel(eLevel);
            }
        }

        private void SendSquelchFromXMMLevel(int eLevel)
        {
            //XMMLevel comes back 0 - 1000. For CW decoder set is linear volts
            // use a log scale mid point = 0dB
            // decode mag valuse rannges from ~ -50 to +30dB
            //decoder mag and squelch level on same scaling.
            double dBmin = -50;          //bottom of control gives  0 = dBMin
            double dBmax = 30;          //top of control gives 1000 = dBMax
            double dB = (dBmax - dBmin) / 1000.0 * eLevel + dBmin + 3;      //rescale user control set value (eLevel) to dB
            double level = Math.Pow(10, dB / 20);                       //calculate linear value for decoder
            _decoder.SetSquelchLevel(level);
        }


        //Left button on Spectrum control moves the mark/space freq's
        private void axXMMSpec1_OnLMouseDown_1(object sender, _DXMMSpecEvents_OnLMouseDownEvent e)
        {
            Spec_OnMouseDown(e.freq);
        }

        private void axXMMSpec1_OnLMouseMove(object sender, _DXMMSpecEvents_OnLMouseMoveEvent e)
        {
            Spec_OnMouseDown(e.freq);
        }

        private void axXMMSpec1_OnLMouseUp(object sender, _DXMMSpecEvents_OnLMouseUpEvent e)
        {
            Spec_OnMouseDown(e.freq);
        }

        //helper to set mark-space markers on Spectrum control
        public void Spec_OnMouseDown(int freq /* VB6 Integer */)
        {
            // Move the mark/space withing the current bandwidth
            // Like RIT without moving receiver frequency

            if (freq < 300) freq = 300;

            // Spacefreq = m_nmmr(xr_spacefreq) - m_nmmr(xr_markfreq) + Freq
            // Interpret as keeping the current shift (space - mark) and applying it to the clicked mark freq.
            Nmmr CurrentNmmr = GetCurrentNmmr();
            int currentShift = (int)CurrentNmmr.SpaceFreq - (int)CurrentNmmr.MarkFreq;       //int currentShift = 170;     // 
            int spaceFreq = currentShift + freq;

            // If Spacefreq > 2700 Then offset = Spacefreq - 2700 ... subtract offset from both
            const int MaxSpace = 2700;
            if (spaceFreq > MaxSpace)
            {
                int offset = spaceFreq - MaxSpace;
                spaceFreq -= offset;
                freq -= offset;
            }

            axXMMR1.PostMmttyMessage((int)RxmMessage.SetMark, freq);
            axXMMR1.PostMmttyMessage((int)RxmMessage.SetSpace, spaceFreq);
        }

        private void setupEngineToolStripMenuItem_Click(object sender, EventArgs e)
        {
            axXMMR1.PostMmttyMessage((int)RxmMessage.ShowSetup, 1);
        }


        private void OnDiscrimBtnChanged(object sender, EventArgs e)
        {
            // We only act when a button becomes CHECKED
            if (sender is not RadioButton rb || !rb.Checked)
                return;

            //radio button.tag holds CodeSwitch value of its setting
            uint filter = Convert.ToUInt32(rb.Tag);

            //bits 0-1 of CodeSwitch hold the 4 settings value
            const uint DecodeMask = 0x00000003; // bits 0-1

            //get the current setting
            uint m_codeswitch = GetCurrentNmmr().CodeSwitch;

            //add the change (bottom 2 bits)
            m_codeswitch = (m_codeswitch & ~DecodeMask) | filter;

            //send back to MMTTY
            axXMMR1.SetMmttySwitch((int)m_codeswitch);
        }

        private string CurrentCWfilterName = "FIR Bandpass";
        private void OnCwFilterChanged(object sender, EventArgs e)
        {
            // We only act when a button becomes CHECKED
            if (sender is not RadioButton rb || !rb.Checked)
                return;

            string filterName = (string)rb.Tag;
            if (filterName != "")
            {
                CurrentCWfilterName = filterName;
                _decoder.ChangeFilter(CurrentCWfilterName);
                _decoder.SetInputScale(CWinputSensitivity);

            }
        }

        private void axXMMR1_OnCharRcvd(object sender, _DXMMREvents_OnCharRcvdEvent e)
        {
            if (btnCW.Checked) return;      //Only display RTTY here

            string s = ((char)e.bChar).ToString();
            DisplayNextChar(s);
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
            if (sender is TextBoxBase tb) TextBoxWordSelect.SelectWordAtClick(tb, e);
            tbCallsign.Text = tbTextReceived.SelectedText;
        }

        private void PipeConnectTimer_Tick(object sender, EventArgs e)
        {
            if (!_NamedPipeClient.TryConnect())
            {
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
                gbCWcontrols.Left = -500;
                gbRTTYbuttons.Left = 12;
                gbRTTYbuttons.Visible = true;
            }
            UpdateButtonMacros();
        }

        private void PopulateAudioMenu()
        {
            // Clear existing items (except maybe a 'Refresh' button if you have one)
            audioSelectToolStripMenuItem.DropDownItems.Clear();

            for (int i = 0; i < WaveInEvent.DeviceCount; i++)
            {
                var caps = WaveInEvent.GetCapabilities(i);

                // Create a new menu item for the device
                ToolStripMenuItem item = new ToolStripMenuItem(caps.ProductName);
                item.Tag = i; // Store the device index in the Tag property
                item.Click += DeviceItem_Click;

                audioSelectToolStripMenuItem.DropDownItems.Add(item);
            }
        }

        private int _selectedDeviceIndex = -1;

        //select Audio device from enum list in menu
        private void DeviceItem_Click(object sender, EventArgs e)
        {
            var clickedItem = (ToolStripMenuItem)sender;

            // 1. Uncheck all other items in this menu
            foreach (ToolStripMenuItem item in audioSelectToolStripMenuItem.DropDownItems)
            {
                item.Checked = false;
            }

            // 2. Check the selected one and store the index
            clickedItem.Checked = true;
            _selectedDeviceIndex = (int)clickedItem.Tag;

            // 3. Restart the engine if it was already running
            StopAndRestartEngine(_selectedDeviceIndex);
        }

        private void StopAndRestartEngine(int index)
        {
            _decoder.Stop();
            _decoder.Start(index);
        }

        private int TextToInt(string text)
        {
            if (int.TryParse(text, out int Value))
            {
                // Success: wpmValue is now an integer
                return Value;
            }
            else
            {
                // Failure: the text wasn't a valid number
                MessageBox.Show("Please enter a valid number for WPM.");
            }
            return -1;
        }

        private void sbAudioLevel_Scroll(object sender, EventArgs e)
        {
            double linearlevel = Math.Pow(10, ((double)sbAudioLevel.Value / 22 / 10.0));
            CWinputSensitivity = (int)linearlevel;                 //int
            _decoder.SetInputScale(CWinputSensitivity);
        }

        //diagnostic param logger
        private void btnStartLog_Click(object sender, EventArgs e)
        {
            _decoder.StartLogging();
        }

        //diagnostic param logger
        private void btnStopLogging_Click(object sender, EventArgs e)
        {
            _decoder.StopLogging();
        }

        //change filter bandwidth
        private void tbBW_ValueChanged(object sender, EventArgs e)
        {
            _decoder.FIRbw = (double)tbBW.Value;
            _decoder.ChangeFilter(CurrentCWfilterName);     //need to update instance

        }

        //clear text boxes containing received text
        private void btnClearText_Click(object sender, EventArgs e)
        {
            tbTextReceived.Clear();
            tbCallSignList.Clear();
        }

        private void tbToneFreq_TextChanged(object sender, EventArgs e)
        {
            _decoder.TargetFrequency = (TextToInt(tbToneFreq.Text));
            _decoder.ChangeFilter(CurrentCWfilterName);
        }

        //double click on WPM control resets WPM in decoder
        private void tbWPM_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            //reset WPM to default
            _decoder.SetWPM(25);            //TODO make value a user setting
        }

        //double click in suggested callsign list copies entry to DXcall text box
        private void tbCallSignList_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            //transfer callsign to active callsign to call or log
            tbCallsign.Text = tbCallSignList.SelectedText;
        }

        private void btnSerialNumInc_Click(object sender, EventArgs e)
        {
            tbSerialNumOut.Increment();
        }

        private void btnSerialNumDec_Click(object sender, EventArgs e)
        {
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

    }
}
