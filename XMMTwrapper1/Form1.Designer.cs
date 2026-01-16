using AxXMMTLib;

namespace XMMTwrapper1
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            axxmmxy1 = new AxXMMXY();
            axxmmWater1 = new AxXMMWater();
            axXMMR1 = new AxXMMR();
            axXMMSpec1 = new AxXMMSpec();
            axXMMLevel1 = new AxXMMLvl();
            gbRTTYbuttons = new GroupBox();
            gbRttyFilters = new GroupBox();
            DiscrimBtn0 = new RadioButton();
            DiscrimBtn1 = new RadioButton();
            DiscrimBtn2 = new RadioButton();
            DiscrimBtn3 = new RadioButton();
            tbBW = new NumericUpDown();
            gbCWcontrols = new GroupBox();
            label13 = new Label();
            label12 = new Label();
            label11 = new Label();
            label10 = new Label();
            tbToneFreq = new TextBox();
            lblCurrentSequence = new Label();
            btnStopLogging = new Button();
            tbSNratio = new TextBox();
            btnStartLog = new Button();
            gbCwFilters = new GroupBox();
            rbCwFilter1 = new RadioButton();
            rbCwFilter2 = new RadioButton();
            rbCwFilter3 = new RadioButton();
            rbCwFilter4 = new RadioButton();
            sbAudioLevel = new TrackBar();
            tbWPM = new TextBox();
            pnlKeyIndicator = new Panel();
            statusStrip1 = new StatusStrip();
            lblMMTTYstatus = new ToolStripStatusLabel();
            lblPipeStatus = new ToolStripStatusLabel();
            menuStrip1 = new MenuStrip();
            OptionMenu = new ToolStripMenuItem();
            setupEngineToolStripMenuItem = new ToolStripMenuItem();
            audioSelectToolStripMenuItem = new ToolStripMenuItem();
            exportLogToolStripMenuItem = new ToolStripMenuItem();
            exportCabrilloToolStripMenuItem = new ToolStripMenuItem();
            openLogDirectoryToolStripMenuItem = new ToolStripMenuItem();
            tbTextReceived = new TextBox();
            tbCallsign = new TextBox();
            PipeConnectTimer = new System.Windows.Forms.Timer(components);
            tbReportOut = new TextBox();
            btnTemplate = new AxXMMBtn();
            btnSend1 = new Button();
            gbRttyCW = new GroupBox();
            btnRTTY = new RadioButton();
            btnCW = new RadioButton();
            tbComment = new TextBox();
            tbSerialNumOut = new TextBox();
            tbSerialNumIn = new TextBox();
            tbReportIn = new TextBox();
            tbQTH = new TextBox();
            tbOpName = new TextBox();
            tbLocator = new TextBox();
            btnSerialNumInc = new Button();
            btnSerialNumDec = new Button();
            cbSerialNumAutoInc = new CheckBox();
            btnSend2 = new Button();
            btnSend4 = new Button();
            btnSend3 = new Button();
            btnSend5 = new Button();
            btnSend6 = new Button();
            cbEditButton = new CheckBox();
            btnLogQSO = new Button();
            tbNumberQSOs = new TextBox();
            label1 = new Label();
            label2 = new Label();
            label3 = new Label();
            label4 = new Label();
            label5 = new Label();
            label6 = new Label();
            label7 = new Label();
            label8 = new Label();
            btnClearText = new Button();
            label9 = new Label();
            tbRxFrequency = new TextBox();
            tbCallSignList = new TextBox();
            label14 = new Label();
            tbPwr = new TextBox();
            label15 = new Label();
            lblDUPE = new Label();
            ((System.ComponentModel.ISupportInitialize)axxmmxy1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)axxmmWater1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)axXMMR1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)axXMMSpec1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)axXMMLevel1).BeginInit();
            gbRTTYbuttons.SuspendLayout();
            gbRttyFilters.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)tbBW).BeginInit();
            gbCWcontrols.SuspendLayout();
            gbCwFilters.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)sbAudioLevel).BeginInit();
            statusStrip1.SuspendLayout();
            menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)btnTemplate).BeginInit();
            gbRttyCW.SuspendLayout();
            SuspendLayout();
            // 
            // axxmmxy1
            // 
            axxmmxy1.Enabled = true;
            axxmmxy1.Location = new Point(620, 37);
            axxmmxy1.Name = "axxmmxy1";
            axxmmxy1.OcxState = (AxHost.State)resources.GetObject("axxmmxy1.OcxState");
            axxmmxy1.Size = new Size(168, 161);
            axxmmxy1.TabIndex = 14;
            // 
            // axxmmWater1
            // 
            axxmmWater1.Enabled = true;
            axxmmWater1.Location = new Point(261, 119);
            axxmmWater1.Name = "axxmmWater1";
            axxmmWater1.OcxState = (AxHost.State)resources.GetObject("axxmmWater1.OcxState");
            axxmmWater1.Size = new Size(353, 79);
            axxmmWater1.TabIndex = 13;
            // 
            // axXMMR1
            // 
            axXMMR1.Enabled = true;
            axXMMR1.Location = new Point(210, 259);
            axXMMR1.Name = "axXMMR1";
            axXMMR1.OcxState = (AxHost.State)resources.GetObject("axXMMR1.OcxState");
            axXMMR1.Size = new Size(100, 50);
            axXMMR1.TabIndex = 11;
            axXMMR1.Visible = false;
            axXMMR1.OnConnected += axXMMR1_OnConnected_1;
            axXMMR1.OnCharRcvd += axXMMR1_OnCharRcvd;
            // 
            // axXMMSpec1
            // 
            axXMMSpec1.Enabled = true;
            axXMMSpec1.Location = new Point(261, 37);
            axXMMSpec1.Name = "axXMMSpec1";
            axXMMSpec1.OcxState = (AxHost.State)resources.GetObject("axXMMSpec1.OcxState");
            axXMMSpec1.Size = new Size(353, 76);
            axXMMSpec1.TabIndex = 10;
            axXMMSpec1.OnLMouseDown += axXMMSpec1_OnLMouseDown_1;
            axXMMSpec1.OnLMouseMove += axXMMSpec1_OnLMouseMove;
            axXMMSpec1.OnLMouseUp += axXMMSpec1_OnLMouseUp;
            // 
            // axXMMLevel1
            // 
            axXMMLevel1.Enabled = true;
            axXMMLevel1.Location = new Point(229, 37);
            axXMMLevel1.Name = "axXMMLevel1";
            axXMMLevel1.OcxState = (AxHost.State)resources.GetObject("axXMMLevel1.OcxState");
            axXMMLevel1.Size = new Size(26, 161);
            axXMMLevel1.TabIndex = 9;
            axXMMLevel1.OnLMouseDown += axXMMLevel1_OnLMouseDown;
            axXMMLevel1.OnLMouseUp += axXMMLevel1_OnLMouseUp;
            axXMMLevel1.OnLMouseMove += axXMMLevel1_OnLMouseMove;
            // 
            // gbRTTYbuttons
            // 
            gbRTTYbuttons.Controls.Add(gbRttyFilters);
            gbRTTYbuttons.Font = new Font("Verdana", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            gbRTTYbuttons.Location = new Point(-500, 27);
            gbRTTYbuttons.Name = "gbRTTYbuttons";
            gbRTTYbuttons.Size = new Size(215, 171);
            gbRTTYbuttons.TabIndex = 20;
            gbRTTYbuttons.TabStop = false;
            // 
            // gbRttyFilters
            // 
            gbRttyFilters.Controls.Add(DiscrimBtn0);
            gbRttyFilters.Controls.Add(DiscrimBtn1);
            gbRttyFilters.Controls.Add(DiscrimBtn2);
            gbRttyFilters.Controls.Add(DiscrimBtn3);
            gbRttyFilters.FlatStyle = FlatStyle.Flat;
            gbRttyFilters.Location = new Point(142, 92);
            gbRttyFilters.Name = "gbRttyFilters";
            gbRttyFilters.Size = new Size(73, 85);
            gbRttyFilters.TabIndex = 28;
            gbRttyFilters.TabStop = false;
            // 
            // DiscrimBtn0
            // 
            DiscrimBtn0.CheckAlign = ContentAlignment.MiddleRight;
            DiscrimBtn0.Checked = true;
            DiscrimBtn0.Font = new Font("Verdana", 9F, FontStyle.Bold);
            DiscrimBtn0.ImageAlign = ContentAlignment.MiddleRight;
            DiscrimBtn0.Location = new Point(6, 3);
            DiscrimBtn0.Name = "DiscrimBtn0";
            DiscrimBtn0.Size = new Size(50, 18);
            DiscrimBtn0.TabIndex = 24;
            DiscrimBtn0.TabStop = true;
            DiscrimBtn0.Tag = "0";
            DiscrimBtn0.Text = "IIR";
            DiscrimBtn0.TextAlign = ContentAlignment.MiddleRight;
            DiscrimBtn0.UseVisualStyleBackColor = true;
            DiscrimBtn0.CheckedChanged += OnDiscrimBtnChanged;
            // 
            // DiscrimBtn1
            // 
            DiscrimBtn1.CheckAlign = ContentAlignment.MiddleRight;
            DiscrimBtn1.Font = new Font("Verdana", 9F, FontStyle.Bold);
            DiscrimBtn1.ImageAlign = ContentAlignment.MiddleRight;
            DiscrimBtn1.Location = new Point(6, 22);
            DiscrimBtn1.Name = "DiscrimBtn1";
            DiscrimBtn1.Size = new Size(50, 18);
            DiscrimBtn1.TabIndex = 25;
            DiscrimBtn1.Tag = "1";
            DiscrimBtn1.Text = "FIR";
            DiscrimBtn1.TextAlign = ContentAlignment.MiddleRight;
            DiscrimBtn1.UseVisualStyleBackColor = true;
            DiscrimBtn1.CheckedChanged += OnDiscrimBtnChanged;
            // 
            // DiscrimBtn2
            // 
            DiscrimBtn2.CheckAlign = ContentAlignment.MiddleRight;
            DiscrimBtn2.Font = new Font("Verdana", 9F, FontStyle.Bold);
            DiscrimBtn2.ImageAlign = ContentAlignment.MiddleRight;
            DiscrimBtn2.Location = new Point(6, 41);
            DiscrimBtn2.Name = "DiscrimBtn2";
            DiscrimBtn2.Size = new Size(50, 18);
            DiscrimBtn2.TabIndex = 26;
            DiscrimBtn2.Tag = "2";
            DiscrimBtn2.Text = "PLL";
            DiscrimBtn2.TextAlign = ContentAlignment.MiddleRight;
            DiscrimBtn2.UseVisualStyleBackColor = true;
            DiscrimBtn2.CheckedChanged += OnDiscrimBtnChanged;
            // 
            // DiscrimBtn3
            // 
            DiscrimBtn3.CheckAlign = ContentAlignment.MiddleRight;
            DiscrimBtn3.Font = new Font("Verdana", 9F, FontStyle.Bold);
            DiscrimBtn3.ImageAlign = ContentAlignment.MiddleRight;
            DiscrimBtn3.Location = new Point(6, 60);
            DiscrimBtn3.Name = "DiscrimBtn3";
            DiscrimBtn3.Size = new Size(50, 18);
            DiscrimBtn3.TabIndex = 27;
            DiscrimBtn3.Tag = "3";
            DiscrimBtn3.Text = "FFT";
            DiscrimBtn3.TextAlign = ContentAlignment.MiddleRight;
            DiscrimBtn3.UseVisualStyleBackColor = true;
            DiscrimBtn3.CheckedChanged += OnDiscrimBtnChanged;
            // 
            // tbBW
            // 
            tbBW.Location = new Point(158, 38);
            tbBW.Maximum = new decimal(new int[] { 999, 0, 0, 0 });
            tbBW.Name = "tbBW";
            tbBW.Size = new Size(40, 23);
            tbBW.TabIndex = 29;
            tbBW.Value = new decimal(new int[] { 45, 0, 0, 0 });
            tbBW.ValueChanged += tbBW_ValueChanged;
            tbBW.KeyUp += tbBW_ValueChanged;
            // 
            // gbCWcontrols
            // 
            gbCWcontrols.Controls.Add(label13);
            gbCWcontrols.Controls.Add(label12);
            gbCWcontrols.Controls.Add(label11);
            gbCWcontrols.Controls.Add(label10);
            gbCWcontrols.Controls.Add(tbToneFreq);
            gbCWcontrols.Controls.Add(lblCurrentSequence);
            gbCWcontrols.Controls.Add(btnStopLogging);
            gbCWcontrols.Controls.Add(tbSNratio);
            gbCWcontrols.Controls.Add(btnStartLog);
            gbCWcontrols.Controls.Add(gbCwFilters);
            gbCWcontrols.Controls.Add(tbBW);
            gbCWcontrols.Controls.Add(sbAudioLevel);
            gbCWcontrols.Controls.Add(tbWPM);
            gbCWcontrols.Controls.Add(pnlKeyIndicator);
            gbCWcontrols.Location = new Point(12, 27);
            gbCWcontrols.Name = "gbCWcontrols";
            gbCWcontrols.Size = new Size(215, 171);
            gbCWcontrols.TabIndex = 64;
            gbCWcontrols.TabStop = false;
            gbCWcontrols.Visible = false;
            // 
            // label13
            // 
            label13.AutoSize = true;
            label13.Location = new Point(111, 128);
            label13.Name = "label13";
            label13.Size = new Size(27, 15);
            label13.TabIndex = 72;
            label13.Text = "S/N";
            // 
            // label12
            // 
            label12.AutoSize = true;
            label12.Location = new Point(66, 128);
            label12.Name = "label12";
            label12.Size = new Size(36, 15);
            label12.TabIndex = 71;
            label12.Text = "WPM";
            // 
            // label11
            // 
            label11.AutoSize = true;
            label11.Location = new Point(158, 18);
            label11.Name = "label11";
            label11.Size = new Size(25, 15);
            label11.TabIndex = 70;
            label11.Text = "BW";
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Location = new Point(120, 19);
            label10.Name = "label10";
            label10.Size = new Size(32, 15);
            label10.TabIndex = 69;
            label10.Text = "Tone";
            // 
            // tbToneFreq
            // 
            tbToneFreq.Location = new Point(117, 37);
            tbToneFreq.Name = "tbToneFreq";
            tbToneFreq.Size = new Size(35, 23);
            tbToneFreq.TabIndex = 68;
            tbToneFreq.Text = "1000";
            tbToneFreq.TextChanged += tbToneFreq_TextChanged;
            // 
            // lblCurrentSequence
            // 
            lblCurrentSequence.AutoSize = true;
            lblCurrentSequence.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblCurrentSequence.Location = new Point(7, 98);
            lblCurrentSequence.MaximumSize = new Size(120, 0);
            lblCurrentSequence.MinimumSize = new Size(12, 0);
            lblCurrentSequence.Name = "lblCurrentSequence";
            lblCurrentSequence.Size = new Size(12, 21);
            lblCurrentSequence.TabIndex = 58;
            // 
            // btnStopLogging
            // 
            btnStopLogging.Location = new Point(16, 47);
            btnStopLogging.Name = "btnStopLogging";
            btnStopLogging.Size = new Size(46, 23);
            btnStopLogging.TabIndex = 67;
            btnStopLogging.Text = "Stop";
            btnStopLogging.UseVisualStyleBackColor = true;
            btnStopLogging.Click += btnStopLogging_Click;
            // 
            // tbSNratio
            // 
            tbSNratio.Location = new Point(108, 146);
            tbSNratio.Name = "tbSNratio";
            tbSNratio.Size = new Size(34, 23);
            tbSNratio.TabIndex = 65;
            // 
            // btnStartLog
            // 
            btnStartLog.Location = new Point(16, 18);
            btnStartLog.Name = "btnStartLog";
            btnStartLog.Size = new Size(46, 23);
            btnStartLog.TabIndex = 66;
            btnStartLog.Text = "Start";
            btnStartLog.UseVisualStyleBackColor = true;
            btnStartLog.Click += btnStartLog_Click;
            // 
            // gbCwFilters
            // 
            gbCwFilters.Controls.Add(rbCwFilter1);
            gbCwFilters.Controls.Add(rbCwFilter2);
            gbCwFilters.Controls.Add(rbCwFilter3);
            gbCwFilters.Controls.Add(rbCwFilter4);
            gbCwFilters.FlatStyle = FlatStyle.Flat;
            gbCwFilters.Location = new Point(142, 92);
            gbCwFilters.Name = "gbCwFilters";
            gbCwFilters.Size = new Size(73, 85);
            gbCwFilters.TabIndex = 65;
            gbCwFilters.TabStop = false;
            // 
            // rbCwFilter1
            // 
            rbCwFilter1.CheckAlign = ContentAlignment.MiddleRight;
            rbCwFilter1.Font = new Font("Verdana", 9F, FontStyle.Bold);
            rbCwFilter1.ImageAlign = ContentAlignment.MiddleRight;
            rbCwFilter1.Location = new Point(6, 3);
            rbCwFilter1.Name = "rbCwFilter1";
            rbCwFilter1.Size = new Size(50, 18);
            rbCwFilter1.TabIndex = 24;
            rbCwFilter1.Tag = "Goertzel";
            rbCwFilter1.Text = "IIR";
            rbCwFilter1.TextAlign = ContentAlignment.MiddleRight;
            rbCwFilter1.UseVisualStyleBackColor = true;
            rbCwFilter1.CheckedChanged += OnCwFilterChanged;
            // 
            // rbCwFilter2
            // 
            rbCwFilter2.CheckAlign = ContentAlignment.MiddleRight;
            rbCwFilter2.Checked = true;
            rbCwFilter2.Font = new Font("Verdana", 9F, FontStyle.Bold);
            rbCwFilter2.ImageAlign = ContentAlignment.MiddleRight;
            rbCwFilter2.Location = new Point(6, 22);
            rbCwFilter2.Name = "rbCwFilter2";
            rbCwFilter2.Size = new Size(50, 18);
            rbCwFilter2.TabIndex = 25;
            rbCwFilter2.TabStop = true;
            rbCwFilter2.Tag = "FIR Bandpass";
            rbCwFilter2.Text = "FIR";
            rbCwFilter2.TextAlign = ContentAlignment.MiddleRight;
            rbCwFilter2.UseVisualStyleBackColor = true;
            rbCwFilter2.CheckedChanged += OnCwFilterChanged;
            // 
            // rbCwFilter3
            // 
            rbCwFilter3.CheckAlign = ContentAlignment.MiddleRight;
            rbCwFilter3.Font = new Font("Verdana", 9F, FontStyle.Bold);
            rbCwFilter3.ImageAlign = ContentAlignment.MiddleRight;
            rbCwFilter3.Location = new Point(0, 41);
            rbCwFilter3.Name = "rbCwFilter3";
            rbCwFilter3.Size = new Size(56, 18);
            rbCwFilter3.TabIndex = 26;
            rbCwFilter3.Tag = "Hilbert Analytic";
            rbCwFilter3.Text = "HBA";
            rbCwFilter3.TextAlign = ContentAlignment.MiddleRight;
            rbCwFilter3.UseVisualStyleBackColor = true;
            rbCwFilter3.CheckedChanged += OnCwFilterChanged;
            // 
            // rbCwFilter4
            // 
            rbCwFilter4.CheckAlign = ContentAlignment.MiddleRight;
            rbCwFilter4.Font = new Font("Verdana", 9F, FontStyle.Bold);
            rbCwFilter4.ImageAlign = ContentAlignment.MiddleRight;
            rbCwFilter4.Location = new Point(0, 60);
            rbCwFilter4.Name = "rbCwFilter4";
            rbCwFilter4.Size = new Size(56, 18);
            rbCwFilter4.TabIndex = 27;
            rbCwFilter4.Tag = "RMS";
            rbCwFilter4.Text = "RMS";
            rbCwFilter4.TextAlign = ContentAlignment.MiddleRight;
            rbCwFilter4.UseVisualStyleBackColor = true;
            rbCwFilter4.CheckedChanged += OnCwFilterChanged;
            // 
            // sbAudioLevel
            // 
            sbAudioLevel.Location = new Point(111, 63);
            sbAudioLevel.Maximum = 1000;
            sbAudioLevel.Name = "sbAudioLevel";
            sbAudioLevel.Size = new Size(104, 45);
            sbAudioLevel.TabIndex = 62;
            sbAudioLevel.Value = 506;
            sbAudioLevel.Scroll += sbAudioLevel_Scroll;
            // 
            // tbWPM
            // 
            tbWPM.Location = new Point(65, 146);
            tbWPM.Name = "tbWPM";
            tbWPM.Size = new Size(37, 23);
            tbWPM.TabIndex = 59;
            tbWPM.Text = "25";
            tbWPM.TextAlign = HorizontalAlignment.Center;
            tbWPM.MouseDoubleClick += tbWPM_MouseDoubleClick;
            // 
            // pnlKeyIndicator
            // 
            pnlKeyIndicator.Location = new Point(198, 10);
            pnlKeyIndicator.Name = "pnlKeyIndicator";
            pnlKeyIndicator.Size = new Size(17, 22);
            pnlKeyIndicator.TabIndex = 57;
            // 
            // statusStrip1
            // 
            statusStrip1.Items.AddRange(new ToolStripItem[] { lblMMTTYstatus, lblPipeStatus });
            statusStrip1.Location = new Point(0, 511);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new Size(804, 22);
            statusStrip1.TabIndex = 22;
            statusStrip1.Text = "statusStrip1";
            // 
            // lblMMTTYstatus
            // 
            lblMMTTYstatus.AutoSize = false;
            lblMMTTYstatus.Name = "lblMMTTYstatus";
            lblMMTTYstatus.Size = new Size(300, 17);
            lblMMTTYstatus.Text = "MMTTY status";
            lblMMTTYstatus.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblPipeStatus
            // 
            lblPipeStatus.Name = "lblPipeStatus";
            lblPipeStatus.Size = new Size(173, 17);
            lblPipeStatus.Text = "TxLink Pipe ... Trying to connect";
            // 
            // menuStrip1
            // 
            menuStrip1.Items.AddRange(new ToolStripItem[] { OptionMenu, audioSelectToolStripMenuItem, exportLogToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(804, 24);
            menuStrip1.TabIndex = 23;
            menuStrip1.Text = "menuStrip1";
            // 
            // OptionMenu
            // 
            OptionMenu.DropDownItems.AddRange(new ToolStripItem[] { setupEngineToolStripMenuItem });
            OptionMenu.Name = "OptionMenu";
            OptionMenu.Size = new Size(61, 20);
            OptionMenu.Text = "Options";
            // 
            // setupEngineToolStripMenuItem
            // 
            setupEngineToolStripMenuItem.Name = "setupEngineToolStripMenuItem";
            setupEngineToolStripMenuItem.Size = new Size(171, 22);
            setupEngineToolStripMenuItem.Text = "RTTY Setup Engine";
            setupEngineToolStripMenuItem.Click += setupEngineToolStripMenuItem_Click;
            // 
            // audioSelectToolStripMenuItem
            // 
            audioSelectToolStripMenuItem.Name = "audioSelectToolStripMenuItem";
            audioSelectToolStripMenuItem.Size = new Size(85, 20);
            audioSelectToolStripMenuItem.Text = "Audio Select";
            // 
            // exportLogToolStripMenuItem
            // 
            exportLogToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { exportCabrilloToolStripMenuItem, openLogDirectoryToolStripMenuItem });
            exportLogToolStripMenuItem.Name = "exportLogToolStripMenuItem";
            exportLogToolStripMenuItem.Size = new Size(69, 20);
            exportLogToolStripMenuItem.Text = "Log Book";
            // 
            // exportCabrilloToolStripMenuItem
            // 
            exportCabrilloToolStripMenuItem.Name = "exportCabrilloToolStripMenuItem";
            exportCabrilloToolStripMenuItem.Size = new Size(173, 22);
            exportCabrilloToolStripMenuItem.Text = "Export Cabrillo";
            exportCabrilloToolStripMenuItem.Click += exportCabrilloToolStripMenuItem_Click;
            // 
            // openLogDirectoryToolStripMenuItem
            // 
            openLogDirectoryToolStripMenuItem.Name = "openLogDirectoryToolStripMenuItem";
            openLogDirectoryToolStripMenuItem.Size = new Size(173, 22);
            openLogDirectoryToolStripMenuItem.Text = "Open log directory";
            openLogDirectoryToolStripMenuItem.Click += openLogDirectoryToolStripMenuItem_Click;
            // 
            // tbTextReceived
            // 
            tbTextReceived.AcceptsReturn = true;
            tbTextReceived.AcceptsTab = true;
            tbTextReceived.Font = new Font("Courier New", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            tbTextReceived.Location = new Point(12, 239);
            tbTextReceived.Multiline = true;
            tbTextReceived.Name = "tbTextReceived";
            tbTextReceived.ScrollBars = ScrollBars.Vertical;
            tbTextReceived.Size = new Size(393, 190);
            tbTextReceived.TabIndex = 24;
            tbTextReceived.MouseDown += tbTextReceived_MouseDown;
            // 
            // tbCallsign
            // 
            tbCallsign.CharacterCasing = CharacterCasing.Upper;
            tbCallsign.Location = new Point(620, 210);
            tbCallsign.Name = "tbCallsign";
            tbCallsign.Size = new Size(96, 23);
            tbCallsign.TabIndex = 25;
            tbCallsign.TextChanged += tbCallsign_TextChanged;
            // 
            // PipeConnectTimer
            // 
            PipeConnectTimer.Enabled = true;
            PipeConnectTimer.Interval = 50;
            PipeConnectTimer.Tick += PipeConnectTimer_Tick;
            // 
            // tbReportOut
            // 
            tbReportOut.CharacterCasing = CharacterCasing.Upper;
            tbReportOut.Location = new Point(620, 239);
            tbReportOut.Name = "tbReportOut";
            tbReportOut.Size = new Size(42, 23);
            tbReportOut.TabIndex = 26;
            tbReportOut.Text = "599";
            // 
            // btnTemplate
            // 
            btnTemplate.Enabled = true;
            btnTemplate.Location = new Point(185, 259);
            btnTemplate.Name = "btnTemplate";
            btnTemplate.OcxState = (AxHost.State)resources.GetObject("btnTemplate.OcxState");
            btnTemplate.Size = new Size(100, 50);
            btnTemplate.TabIndex = 27;
            btnTemplate.Visible = false;
            // 
            // btnSend1
            // 
            btnSend1.Location = new Point(19, 435);
            btnSend1.Name = "btnSend1";
            btnSend1.Size = new Size(55, 23);
            btnSend1.TabIndex = 29;
            btnSend1.Text = "CQ";
            btnSend1.UseVisualStyleBackColor = true;
            btnSend1.Click += MacroButton_Click;
            // 
            // gbRttyCW
            // 
            gbRttyCW.Controls.Add(btnRTTY);
            gbRttyCW.Controls.Add(btnCW);
            gbRttyCW.Location = new Point(12, 148);
            gbRttyCW.Name = "gbRttyCW";
            gbRttyCW.Size = new Size(62, 49);
            gbRttyCW.TabIndex = 29;
            gbRttyCW.TabStop = false;
            // 
            // btnRTTY
            // 
            btnRTTY.AutoSize = true;
            btnRTTY.Checked = true;
            btnRTTY.Font = new Font("Verdana", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            btnRTTY.Location = new Point(5, 30);
            btnRTTY.Name = "btnRTTY";
            btnRTTY.Size = new Size(54, 18);
            btnRTTY.TabIndex = 1;
            btnRTTY.TabStop = true;
            btnRTTY.Text = "RTTY";
            btnRTTY.UseVisualStyleBackColor = true;
            btnRTTY.CheckedChanged += btnRTTY_CheckedChanged;
            // 
            // btnCW
            // 
            btnCW.AutoSize = true;
            btnCW.Font = new Font("Verdana", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            btnCW.Location = new Point(5, 11);
            btnCW.Name = "btnCW";
            btnCW.Size = new Size(47, 18);
            btnCW.TabIndex = 0;
            btnCW.Text = "CW";
            btnCW.UseVisualStyleBackColor = true;
            btnCW.CheckedChanged += btnCW_CheckedChanged;
            // 
            // tbComment
            // 
            tbComment.Location = new Point(620, 384);
            tbComment.Name = "tbComment";
            tbComment.Size = new Size(168, 23);
            tbComment.TabIndex = 30;
            // 
            // tbSerialNumOut
            // 
            tbSerialNumOut.CharacterCasing = CharacterCasing.Upper;
            tbSerialNumOut.Location = new Point(671, 239);
            tbSerialNumOut.Name = "tbSerialNumOut";
            tbSerialNumOut.Size = new Size(45, 23);
            tbSerialNumOut.TabIndex = 31;
            // 
            // tbSerialNumIn
            // 
            tbSerialNumIn.CharacterCasing = CharacterCasing.Upper;
            tbSerialNumIn.Location = new Point(671, 268);
            tbSerialNumIn.Name = "tbSerialNumIn";
            tbSerialNumIn.Size = new Size(45, 23);
            tbSerialNumIn.TabIndex = 33;
            // 
            // tbReportIn
            // 
            tbReportIn.CharacterCasing = CharacterCasing.Upper;
            tbReportIn.Location = new Point(620, 268);
            tbReportIn.Name = "tbReportIn";
            tbReportIn.Size = new Size(42, 23);
            tbReportIn.TabIndex = 32;
            // 
            // tbQTH
            // 
            tbQTH.Location = new Point(620, 355);
            tbQTH.Name = "tbQTH";
            tbQTH.Size = new Size(96, 23);
            tbQTH.TabIndex = 34;
            // 
            // tbOpName
            // 
            tbOpName.Location = new Point(620, 326);
            tbOpName.Name = "tbOpName";
            tbOpName.Size = new Size(96, 23);
            tbOpName.TabIndex = 35;
            // 
            // tbLocator
            // 
            tbLocator.CharacterCasing = CharacterCasing.Upper;
            tbLocator.Location = new Point(620, 297);
            tbLocator.Name = "tbLocator";
            tbLocator.Size = new Size(96, 23);
            tbLocator.TabIndex = 36;
            // 
            // btnSerialNumInc
            // 
            btnSerialNumInc.Font = new Font("Segoe UI", 8.25F);
            btnSerialNumInc.Location = new Point(720, 231);
            btnSerialNumInc.Margin = new Padding(1);
            btnSerialNumInc.Name = "btnSerialNumInc";
            btnSerialNumInc.Size = new Size(35, 20);
            btnSerialNumInc.TabIndex = 37;
            btnSerialNumInc.Text = "Inc";
            btnSerialNumInc.TextAlign = ContentAlignment.TopCenter;
            btnSerialNumInc.UseVisualStyleBackColor = true;
            btnSerialNumInc.Click += btnSerialNumInc_Click;
            // 
            // btnSerialNumDec
            // 
            btnSerialNumDec.Font = new Font("Segoe UI", 8.25F);
            btnSerialNumDec.Location = new Point(720, 252);
            btnSerialNumDec.Name = "btnSerialNumDec";
            btnSerialNumDec.Size = new Size(35, 20);
            btnSerialNumDec.TabIndex = 38;
            btnSerialNumDec.Text = "dec";
            btnSerialNumDec.UseVisualStyleBackColor = true;
            btnSerialNumDec.Click += btnSerialNumDec_Click;
            // 
            // cbSerialNumAutoInc
            // 
            cbSerialNumAutoInc.AutoSize = true;
            cbSerialNumAutoInc.Location = new Point(761, 243);
            cbSerialNumAutoInc.Name = "cbSerialNumAutoInc";
            cbSerialNumAutoInc.Size = new Size(15, 14);
            cbSerialNumAutoInc.TabIndex = 39;
            cbSerialNumAutoInc.UseVisualStyleBackColor = true;
            // 
            // btnSend2
            // 
            btnSend2.Location = new Point(83, 435);
            btnSend2.Name = "btnSend2";
            btnSend2.Size = new Size(55, 23);
            btnSend2.TabIndex = 40;
            btnSend2.Text = "CQ";
            btnSend2.UseVisualStyleBackColor = true;
            btnSend2.Click += MacroButton_Click;
            // 
            // btnSend4
            // 
            btnSend4.Location = new Point(211, 435);
            btnSend4.Name = "btnSend4";
            btnSend4.Size = new Size(55, 23);
            btnSend4.TabIndex = 41;
            btnSend4.Text = "CQ";
            btnSend4.UseVisualStyleBackColor = true;
            btnSend4.Click += MacroButton_Click;
            // 
            // btnSend3
            // 
            btnSend3.Location = new Point(147, 435);
            btnSend3.Name = "btnSend3";
            btnSend3.Size = new Size(55, 23);
            btnSend3.TabIndex = 42;
            btnSend3.Text = "CQ";
            btnSend3.UseVisualStyleBackColor = true;
            btnSend3.Click += MacroButton_Click;
            // 
            // btnSend5
            // 
            btnSend5.Location = new Point(275, 435);
            btnSend5.Name = "btnSend5";
            btnSend5.Size = new Size(55, 23);
            btnSend5.TabIndex = 43;
            btnSend5.Text = "CQ";
            btnSend5.UseVisualStyleBackColor = true;
            btnSend5.Click += MacroButton_Click;
            // 
            // btnSend6
            // 
            btnSend6.Location = new Point(339, 435);
            btnSend6.Name = "btnSend6";
            btnSend6.Size = new Size(55, 23);
            btnSend6.TabIndex = 44;
            btnSend6.Text = "CQ";
            btnSend6.UseVisualStyleBackColor = true;
            btnSend6.Click += MacroButton_Click;
            // 
            // cbEditButton
            // 
            cbEditButton.AutoSize = true;
            cbEditButton.CheckAlign = ContentAlignment.MiddleRight;
            cbEditButton.Location = new Point(433, 439);
            cbEditButton.Name = "cbEditButton";
            cbEditButton.Size = new Size(85, 19);
            cbEditButton.TabIndex = 45;
            cbEditButton.Text = "Edit Button";
            cbEditButton.UseVisualStyleBackColor = true;
            // 
            // btnLogQSO
            // 
            btnLogQSO.Location = new Point(620, 415);
            btnLogQSO.Name = "btnLogQSO";
            btnLogQSO.Size = new Size(75, 23);
            btnLogQSO.TabIndex = 46;
            btnLogQSO.Text = "Log QSO";
            btnLogQSO.UseVisualStyleBackColor = true;
            btnLogQSO.Click += btnLogQSO_Click;
            // 
            // tbNumberQSOs
            // 
            tbNumberQSOs.Location = new Point(722, 416);
            tbNumberQSOs.Name = "tbNumberQSOs";
            tbNumberQSOs.Size = new Size(66, 23);
            tbNumberQSOs.TabIndex = 47;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(702, 419);
            label1.Name = "label1";
            label1.Size = new Size(14, 15);
            label1.TabIndex = 48;
            label1.Text = "#";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(569, 213);
            label2.Name = "label2";
            label2.Size = new Size(45, 15);
            label2.TabIndex = 49;
            label2.Text = "DX Call";
            label2.TextAlign = ContentAlignment.MiddleRight;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(562, 243);
            label3.Name = "label3";
            label3.Size = new Size(52, 15);
            label3.TabIndex = 50;
            label3.Text = "Rprt Out";
            label3.TextAlign = ContentAlignment.MiddleRight;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(537, 272);
            label4.Name = "label4";
            label4.Size = new Size(77, 15);
            label4.TabIndex = 51;
            label4.Text = "Rprt In (Shift)";
            label4.TextAlign = ContentAlignment.MiddleRight;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(529, 300);
            label5.Name = "label5";
            label5.Size = new Size(85, 15);
            label5.TabIndex = 52;
            label5.Text = "Locator (CTRL)";
            label5.TextAlign = ContentAlignment.MiddleRight;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(519, 329);
            label6.Name = "label6";
            label6.Size = new Size(95, 15);
            label6.TabIndex = 53;
            label6.Text = "Name (Shft/Ctrl)";
            label6.TextAlign = ContentAlignment.MiddleRight;
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(558, 358);
            label7.Name = "label7";
            label7.Size = new Size(56, 15);
            label7.TabIndex = 54;
            label7.Text = "QTH (Alt)";
            label7.TextAlign = ContentAlignment.MiddleRight;
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(553, 387);
            label8.Name = "label8";
            label8.Size = new Size(61, 15);
            label8.TabIndex = 55;
            label8.Text = "Comment";
            label8.TextAlign = ContentAlignment.MiddleRight;
            // 
            // btnClearText
            // 
            btnClearText.Location = new Point(382, 213);
            btnClearText.Name = "btnClearText";
            btnClearText.Size = new Size(23, 23);
            btnClearText.TabIndex = 66;
            btnClearText.Text = "X";
            btnClearText.UseVisualStyleBackColor = true;
            btnClearText.Click += btnClearText_Click;
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Font = new Font("Verdana", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label9.Location = new Point(16, 216);
            label9.Name = "label9";
            label9.Size = new Size(37, 14);
            label9.TabIndex = 68;
            label9.Text = "Freq";
            label9.TextAlign = ContentAlignment.MiddleRight;
            // 
            // tbRxFrequency
            // 
            tbRxFrequency.Font = new Font("Verdana", 11.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            tbRxFrequency.ForeColor = Color.Green;
            tbRxFrequency.Location = new Point(59, 210);
            tbRxFrequency.Name = "tbRxFrequency";
            tbRxFrequency.Size = new Size(119, 26);
            tbRxFrequency.TabIndex = 69;
            tbRxFrequency.TextAlign = HorizontalAlignment.Center;
            tbRxFrequency.TextChanged += tbCallsign_TextChanged;
            // 
            // tbCallSignList
            // 
            tbCallSignList.Location = new Point(418, 240);
            tbCallSignList.Multiline = true;
            tbCallSignList.Name = "tbCallSignList";
            tbCallSignList.ScrollBars = ScrollBars.Vertical;
            tbCallSignList.Size = new Size(100, 189);
            tbCallSignList.TabIndex = 70;
            tbCallSignList.MouseDoubleClick += tbCallSignList_MouseDoubleClick;
            // 
            // label14
            // 
            label14.AutoSize = true;
            label14.Location = new Point(433, 218);
            label14.Name = "label14";
            label14.Size = new Size(54, 15);
            label14.TabIndex = 71;
            label14.Text = "Callsigns";
            // 
            // tbPwr
            // 
            tbPwr.Location = new Point(761, 355);
            tbPwr.Name = "tbPwr";
            tbPwr.Size = new Size(27, 23);
            tbPwr.TabIndex = 72;
            tbPwr.Text = "100";
            // 
            // label15
            // 
            label15.AutoSize = true;
            label15.Location = new Point(730, 358);
            label15.Name = "label15";
            label15.Size = new Size(27, 15);
            label15.TabIndex = 73;
            label15.Text = "Pwr";
            // 
            // lblDUPE
            // 
            lblDUPE.AutoSize = true;
            lblDUPE.Font = new Font("Segoe UI", 15.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblDUPE.ForeColor = Color.Red;
            lblDUPE.Location = new Point(720, 205);
            lblDUPE.Name = "lblDUPE";
            lblDUPE.Size = new Size(67, 30);
            lblDUPE.TabIndex = 74;
            lblDUPE.Text = "DUPE";
            lblDUPE.Visible = false;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(804, 533);
            Controls.Add(label15);
            Controls.Add(tbPwr);
            Controls.Add(label14);
            Controls.Add(tbCallSignList);
            Controls.Add(tbRxFrequency);
            Controls.Add(label9);
            Controls.Add(btnClearText);
            Controls.Add(gbRttyCW);
            Controls.Add(label8);
            Controls.Add(label7);
            Controls.Add(label6);
            Controls.Add(label5);
            Controls.Add(label4);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(tbNumberQSOs);
            Controls.Add(btnLogQSO);
            Controls.Add(cbEditButton);
            Controls.Add(btnSend6);
            Controls.Add(btnSend5);
            Controls.Add(btnSend3);
            Controls.Add(btnSend4);
            Controls.Add(btnSend2);
            Controls.Add(cbSerialNumAutoInc);
            Controls.Add(btnSerialNumDec);
            Controls.Add(btnSerialNumInc);
            Controls.Add(tbLocator);
            Controls.Add(tbOpName);
            Controls.Add(tbQTH);
            Controls.Add(tbSerialNumIn);
            Controls.Add(tbReportIn);
            Controls.Add(tbSerialNumOut);
            Controls.Add(tbComment);
            Controls.Add(btnSend1);
            Controls.Add(btnTemplate);
            Controls.Add(tbReportOut);
            Controls.Add(tbCallsign);
            Controls.Add(tbTextReceived);
            Controls.Add(statusStrip1);
            Controls.Add(menuStrip1);
            Controls.Add(axXMMSpec1);
            Controls.Add(axXMMLevel1);
            Controls.Add(axXMMR1);
            Controls.Add(axxmmWater1);
            Controls.Add(axxmmxy1);
            Controls.Add(gbRTTYbuttons);
            Controls.Add(gbCWcontrols);
            Controls.Add(lblDUPE);
            Icon = (Icon)resources.GetObject("$this.Icon");
            KeyPreview = true;
            MainMenuStrip = menuStrip1;
            Name = "Form1";
            Text = "RTTY/CW Controller";
            FormClosing += Form1_FormClosing;
            Load += Form1_Load;
            KeyDown += Form1_KeyDown;
            ((System.ComponentModel.ISupportInitialize)axxmmxy1).EndInit();
            ((System.ComponentModel.ISupportInitialize)axxmmWater1).EndInit();
            ((System.ComponentModel.ISupportInitialize)axXMMR1).EndInit();
            ((System.ComponentModel.ISupportInitialize)axXMMSpec1).EndInit();
            ((System.ComponentModel.ISupportInitialize)axXMMLevel1).EndInit();
            gbRTTYbuttons.ResumeLayout(false);
            gbRttyFilters.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)tbBW).EndInit();
            gbCWcontrols.ResumeLayout(false);
            gbCWcontrols.PerformLayout();
            gbCwFilters.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)sbAudioLevel).EndInit();
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)btnTemplate).EndInit();
            gbRttyCW.ResumeLayout(false);
            gbRttyCW.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private AxXMMTLib.AxXMMXY axxmmxy1;
        private AxXMMTLib.AxXMMWater axxmmWater1;
        private AxXMMTLib.AxXMMR axXMMR1;
        private AxXMMTLib.AxXMMLvl axXMMLevel1;
        private AxXMMTLib.AxXMMT axxmmt1;
        private GroupBox gbRTTYbuttons;
        private AxXMMTLib.AxXMMSpec axXMMSpec1;
        private StatusStrip statusStrip1;
        private ToolStripStatusLabel lblMMTTYstatus;
        private MenuStrip menuStrip1;
        private ToolStripMenuItem OptionMenu;
        private ToolStripMenuItem setupEngineToolStripMenuItem;
        private RadioButton DiscrimBtn0;
        private RadioButton DiscrimBtn1;
        private RadioButton DiscrimBtn2;
        private RadioButton DiscrimBtn3;
        private GroupBox gbRttyFilters;
        private TextBox tbTextReceived;
        private TextBox tbCallsign;
        private System.Windows.Forms.Timer PipeConnectTimer;
        private ToolStripStatusLabel lblPipeStatus;
        private TextBox tbReportOut;
        private AxXMMBtn btnTemplate;
        private Button btnSend1;
        private GroupBox gbRttyCW;
        private RadioButton btnRTTY;
        private RadioButton btnCW;
        private TextBox tbComment;
        private TextBox tbSerialNumOut;
        private TextBox tbSerialNumIn;
        private TextBox tbReportIn;
        private TextBox tbQTH;
        private TextBox tbOpName;
        private TextBox tbLocator;
        private Button btnSerialNumInc;
        private Button btnSerialNumDec;
        private CheckBox cbSerialNumAutoInc;
        private Button btnSend2;
        private Button btnSend4;
        private Button btnSend3;
        private Button btnSend5;
        private Button btnSend6;
        private CheckBox cbEditButton;
        private Button btnLogQSO;
        private TextBox tbNumberQSOs;
        private Label label1;
        private Label label2;
        private Label label3;
        private Label label4;
        private Label label5;
        private Label label6;
        private Label label7;
        private Label label8;
        private Panel pnlKeyIndicator;
        private ToolStripMenuItem audioSelectToolStripMenuItem;
        private Label lblCurrentSequence;
        private TextBox tbWPM;
        private TrackBar sbAudioLevel;
        private GroupBox gbCWcontrols;
        private GroupBox gbCwFilters;
        private RadioButton rbCwFilter1;
        private RadioButton rbCwFilter2;
        private RadioButton rbCwFilter3;
        private RadioButton rbCwFilter4;
        private Button btnStopLogging;
        private Button btnStartLog;
        private NumericUpDown tbBW;
        private TextBox tbSNratio;
        private Button btnClearText;
        private TextBox tbToneFreq;
        private Label label9;
        private TextBox tbRxFrequency;
        private TextBox tbCallSignList;
        private Label label10;
        private Label label12;
        private Label label11;
        private Label label13;
        private Label label14;
        private TextBox tbPwr;
        private Label label15;
        private Label lblDUPE;
        private ToolStripMenuItem exportLogToolStripMenuItem;
        private ToolStripMenuItem exportCabrilloToolStripMenuItem;
        private ToolStripMenuItem openLogDirectoryToolStripMenuItem;
    }
}
