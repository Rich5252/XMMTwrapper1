// This file contains all the Form GUI stuff associated with the JTTY mode
//
// Receive: WS decodes JTTY and sends the text to us over its MMTTY-style TCP link
//          (JttyLink). Start WS with  --mode jtty -p <port>  using the port in settings.ini
//          [JTTY] Port. WS takes its audio from SDRuno, as MMTTY does.
// Transmit: WS receives our text over the same TCP link and keys the transmitter via its
//          TXLink interface.
// Startup:  WS must be started with  -p <port> --n1mm-rxtext-info as it only looks for tcp
//          connection at startup. Hence this app normally starts WS.
// TX state: TXLink reports {TXS} when it keys up and {TXE} when its queue has run empty.
//          While transmitting (plus a short holdoff) decoded text from WS is suppressed,
//          because the SDRuno receiver hears our own signal.
//
// settings.ini, section [JTTY]:
//   Port=52001            TCP port WS connects to (must match WS -p)
//   SuppressOwnTx=True    hide decodes of our own transmission
//   RxHoldoffMs=0         optional: keep suppressing RX this long after {TXE} (0 = off)

namespace XMMTwrapper1
{
    public partial class Form1
    {
        private readonly JttyLink _jttyLink = new();
        private bool _jttyTxActive = false;                 // TXLink has reported {TXS}
        private DateTime _jttyTxStartedUtc = DateTime.MinValue;
        private DateTime _jttyRxHoldoffUntilUtc = DateTime.MinValue;
        private bool _jttyLastWasCR = false;                // for CRLF folding across fragments

        private bool _jttySuppressOwnTx = true;
        private int _jttyRxHoldoffMs = 0;

        // A lost {TXE} must not mute the receiver for ever
        private static readonly TimeSpan JttyTxWatchdog = TimeSpan.FromSeconds(120);

        // Current operating mode as a string: "CW", "RTTY" or "JTTY".
        // Used for the ini mode setting and the macro context.
        private string CurrentModeName =>
            btnCW.Checked ? "CW" : (btnJTTY != null && btnJTTY.Checked) ? "JTTY" : "RTTY";

        // Called from the Form1 constructor, after InitializeComponent()
        // Adds the JTTY mode button to the CW/RTTY group box.
        // (Can be moved into the designer later - just delete this block then.)
        private void InitializeJttyUi()
        {
            lblWSjttyStatus.Text = "WS not connected";
        }

        // Called from Form1_Shown, so events are raised on the UI thread
        private void StartJttyLink()
        {
            int port = TextToInt(_ini.GetValue("JTTY", "Port", "52001"));
            _jttySuppressOwnTx = _ini.GetValue("JTTY", "SuppressOwnTx", "True")
                .Equals("True", StringComparison.OrdinalIgnoreCase);
            _jttyRxHoldoffMs = TextToInt(_ini.GetValue("JTTY", "RxHoldoffMs", "0"));

            _jttyLink.RxText += OnJttyRxText;
            _jttyLink.TagReceived += OnJttyTagReceived;
            _jttyLink.ConnectionChanged += OnJttyConnectionChanged;

            try
            {
                _jttyLink.Start(port);      //start looking before WS starts, so we don't miss its connect
                lblWSjttyStatus.Text = $"Waiting for WS on port {port}";
                LaunchWsIfNeeded(port);         
            }
            catch (Exception ex)
            {
                // Most likely the port is in use
                Log.Mark($"JTTY link failed to start on port {port}: {ex.Message}");
                lblWSjttyStatus.Text = $"Could not listen on port {port}: {ex.Message}";
                btnJTTY.ForeColor = Color.Red;
            }
        }

        private void StopJttyLink()
        {
            _jttyLink.Dispose();
        }

        private void btnJTTY_CheckedChanged(object? sender, EventArgs e)
        {
            _jttyTxLineOpen = false;                    // SelectMode clears the text box
            _jttySkipRxNewline = false;
            SelectMode();
        }

        // ---------------------------------------------------------------- receive

        private void OnJttyConnectionChanged(bool connected)
        {
            btnJTTY.ForeColor = connected ? Color.DarkGreen : SystemColors.GrayText;
            lblWSjttyStatus.Text = connected
                    ? $"WS connected on port {_jttyLink.Port}"
                    : $"Waiting for WS on port {_jttyLink.Port}";
        }

        private void OnJttyRxText(string text)
        {
            if (!btnJTTY.Checked) return;

            // {TXE} lost for too long - stop treating text as our own TX
            if (_jttyTxActive && DateTime.UtcNow - _jttyTxStartedUtc >= JttyTxWatchdog)
            {
                _jttyTxActive = false;
                Log.Mark("JTTY: TX watchdog expired without {TXE}");
            }

            // Between our TXTEXT and OUTPUTCOMPLETE, WS is echoing our own text
            if (_jttyTxActive)
            {
                ShowTxEcho(text);
                return;
            }
            CloseTxLine();                              // finish any open yellow line

            if (IsJttyRxSuppressed()) return;

            // Feed one character at a time so DisplayNextChar's CR/LF handling and the
            // callsign checker behave exactly as they do for RTTY.
            // WS puts \r\n before each new message - fold CRLF (or a lone LF) to a single CR.
            foreach (char c in text)
            {
                // we are already at the start of a line after our TX line: skip WS's leading return
                if ((c == '\r' || c == '\n') && _jttySkipRxNewline)
                {
                    _jttyLastWasCR = (c == '\r');
                    continue;
                }
                _jttySkipRxNewline = false;

                if (c == '\n')
                {
                    if (_jttyLastWasCR) { _jttyLastWasCR = false; continue; }
                    DisplayNextChar("\r");
                    continue;
                }
                _jttyLastWasCR = (c == '\r');
                DisplayNextChar(c.ToString());
            }
        }

        private bool IsJttyRxSuppressed()
        {
            if (!_jttySuppressOwnTx) return false;

            DateTime now = DateTime.UtcNow;
            if (_jttyTxActive)
            {
                if (now - _jttyTxStartedUtc < JttyTxWatchdog) return true;
                _jttyTxActive = false;                  // {TXE} lost - give up suppressing
                Log.Mark("JTTY: TX watchdog expired without {TXE}");
            }
            return now < _jttyRxHoldoffUntilUtc;
        }

        // Tags other than RXTEXT. In receive-only use we don't expect much here,
        // but log them so anything WS sends is visible while testing.
        private void OnJttyTagReceived(string tag, string data)
        {
            Log.Mark($"JTTY link tag <{tag}> {data}");
            lblWSjttyStatus.Text = $"WS: <{tag}>{(data == "" ? "" : " " + data)}  {DateTime.Now:HH:mm:ss}";

            if (tag == "OUTPUTCOMPLETE")
            {
                TryHandleJttyPipeMessage("{TXE}");      // end TX (and optional RX holdoff)
                CloseTxLine();                          // WS echoes all of our TX before this tag
            }
        }

        // ---------------------------------------------------------------- TX echo (yellow)

        private bool _jttyTxLineOpen = false;       // a yellow TX line is currently being built
        private bool _jttyTxPendingSpace = false;   // WS sent a return between two TXTEXTs
        private bool _jttySkipRxNewline = false;    // swallow WS's leading return after a TX line

        // WS echoes each TXTEXT with a leading return. Join all the TXTEXTs of one
        // transmission onto a single highlighted line, space separated.
        // Bypasses DisplayNextChar so our own calls don't reach the callsign checker.
        private void ShowTxEcho(string text)
        {
            foreach (char c in text)
            {
                if (c == '\r' || c == '\n') { _jttyTxPendingSpace = _jttyTxLineOpen; continue; }

                if (!_jttyTxLineOpen)
                {
                    // start our TX on a fresh line
                    if (tbTextReceived.TextLength > 0 && lastChar != Environment.NewLine)
                        tbTextReceived.AppendText(Environment.NewLine);
                    _jttyTxLineOpen = true;
                }
                if (_jttyTxPendingSpace) { AppendTx(" "); _jttyTxPendingSpace = false; }
                AppendTx(c.ToString());
            }
            tbTextReceived.ScrollToCaret();
        }

        private void AppendTx(string s)
        {
            tbTextReceived.SelectionStart = tbTextReceived.TextLength;
            tbTextReceived.SelectionLength = 0;
            tbTextReceived.SelectionBackColor = Color.Yellow;
            tbTextReceived.AppendText(s);
            tbTextReceived.SelectionBackColor = tbTextReceived.BackColor;   // so later RX text isn't yellow
            lastChar = s;
        }

        private void CloseTxLine()
        {
            if (!_jttyTxLineOpen) return;
            tbTextReceived.AppendText(Environment.NewLine);
            lastChar = Environment.NewLine;
            _jttyTxLineOpen = false;
            _jttyTxPendingSpace = false;
            _jttySkipRxNewline = true;              // next RX message starts on this fresh line
        }

        // ---------------------------------------------------------------- transmit

        // Called from MacroButton_Click in JTTY mode, after UpdateButtonMacroInserts()
        private void SendJttyMacro(string template)
        {
            string cmd = _macroProvider.ParseJTTY(template);
            if (cmd == "") return;                      // nothing left to send
            _NamedPipeClient.SendCommand(cmd);
        }

        private void StopJttyTx()
        {
            _NamedPipeClient.SendCommand("JTTY:{X0}");
        }

        // TXLink -> logger status messages. Returns true if the message was a JTTY one.
        // Called from PipeConnectTimer_Tick before the frequency handling.
        private bool TryHandleJttyPipeMessage(string msg)
        {
            switch (msg)
            {
                case "{TXS}":
                    _jttyTxActive = true;
                    _jttyTxStartedUtc = DateTime.UtcNow;
                    btnStopTX.BackColor = Color.Red;
                    return true;

                case "{TXE}":
                    _jttyTxActive = false;
                    _jttyRxHoldoffUntilUtc = DateTime.UtcNow.AddMilliseconds(_jttyRxHoldoffMs);
                    btnStopTX.BackColor = SystemColors.Control;
                    btnStopTX.UseVisualStyleBackColor = true;
                    return true;
            }
            return false;
        }

        // If the pipe drops while transmitting we will never see {TXE}
        private void ResetJttyTxState()
        {
            if (!_jttyTxActive) return;
            _jttyTxActive = false;
            btnStopTX.BackColor = SystemColors.Control;
            btnStopTX.UseVisualStyleBackColor = true;
        }

        // settings.ini [JTTY]
        //   WsPath=C:\path\to\ws.exe     (blank = don't launch)
        //   AutoStartWs=True
        private void LaunchWsIfNeeded(int port)
        {
            if (!_ini.GetValue("JTTY", "AutoStartWs", "True")
                     .Equals("True", StringComparison.OrdinalIgnoreCase)) return;

            string wsPath = _ini.GetValue("JTTY", "WsPath", "");
            if (wsPath == "" || !File.Exists(wsPath))
            {
                lblWSjttyStatus.Text = "WS path not set - start WS manually";
                return;
            }

            // Already running? It won't connect to us, so tell the user rather than start a second copy
            string exeName = Path.GetFileNameWithoutExtension(wsPath);
            if (System.Diagnostics.Process.GetProcessesByName(exeName).Length > 0)
            {
                lblWSjttyStatus.Text = "WS already running - restart it to connect";
                return;
            }

            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = wsPath,
                    Arguments = $"-p {port} --n1mm-rxtext-info",
                    WorkingDirectory = Path.GetDirectoryName(wsPath)!,
                    UseShellExecute = true,
                });
                lblWSjttyStatus.Text = $"Starting WS on port {port}...";
            }
            catch (Exception ex)
            {
                lblWSjttyStatus.Text = $"Could not start WS: {ex.Message}";
            }
        }
    }
}
