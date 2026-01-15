// This file contains all the Form GUI stuff associated with the CW mode
// 


using NAudio.Wave;

namespace XMMTwrapper1
{
    public partial class Form1
    {
        private NamedPipeClient _NamedPipeClient = new();       //coms with TXLink for tx commands and status (eg current rx freq)
        private SerialMessageHandler? _serial = new();          //COM with keyer for CW tx commands

        private CWDecoderEngine _decoder;               //CW decoder module

        private int CWinputSensitivity = 200;        // controlled by slider bar
        private double SignalStrength = 0;

        private string CurrentCWfilterName = "FIR Bandpass";


        // Initialize the CW Decoder and subscribe to its events called from Form1 constructor
        private void InitializeCWDecoder()
        {
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

        }

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
    }

}