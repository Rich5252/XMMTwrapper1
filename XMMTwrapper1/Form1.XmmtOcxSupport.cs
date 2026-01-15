using AxXMMTLib;            //XMMR.ocx support for MMTTY
using Constants;            //see XmmrOcxInteropConstants.cs - various constants/enum etc for XMMR/MMTTY
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
                    SendSquelchFromXMMLevel((int)nmmr.SqLevel);   //send squelch level to CW engine
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

        private void axXMMR1_OnCharRcvd(object sender, _DXMMREvents_OnCharRcvdEvent e)
        {
            if (btnCW.Checked) return;      //Only display RTTY here

            string s = ((char)e.bChar).ToString();
            DisplayNextChar(s);
        }


    }
}