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
            cbAutoSquelch = new CheckBox();
            cbFixWpm = new CheckBox();
            tbConfidence = new TextBox();
            tbTolerance = new NumericUpDown();
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
            toolStripStatusLabel3 = new ToolStripStatusLabel();
            lblPipeStatus = new ToolStripStatusLabel();
            toolStripStatusLabel1 = new ToolStripStatusLabel();
            lblWSjttyStatus = new ToolStripStatusLabel();
            toolStripStatusLabel2 = new ToolStripStatusLabel();
            lblLogFileName = new ToolStripStatusLabel();
            menuStrip1 = new MenuStrip();
            OptionMenu = new ToolStripMenuItem();
            setupEngineToolStripMenuItem = new ToolStripMenuItem();
            audioSelectToolStripMenuItem = new ToolStripMenuItem();
            exportLogToolStripMenuItem = new ToolStripMenuItem();
            createNewLogBookToolStripMenuItem = new ToolStripMenuItem();
            exportCabrilloToolStripMenuItem = new ToolStripMenuItem();
            openLogDirectoryToolStripMenuItem = new ToolStripMenuItem();
            tbTextReceived = new RichTextBox();
            cmsLogOverwrite = new ContextMenuStrip(components);
            cmsLogOverwriteConfirm = new ToolStripMenuItem();
            cmsLogOverwriteCancel = new ToolStripMenuItem();
            toolStripSeparator1 = new ToolStripSeparator();
            cmsLogQSOFirst = new ToolStripMenuItem();
            tbCallsign = new TextBox();
            PipeConnectTimer = new System.Windows.Forms.Timer(components);
            tbReportOut = new TextBox();
            contextMenuStripReportFields = new ContextMenuStrip(components);
            fixToolStripMenuItem = new ToolStripMenuItem();
            clearToolStripMenuItem = new ToolStripMenuItem();
            btnTemplate = new AxXMMBtn();
            btnSend1 = new Button();
            gbRttyCW = new GroupBox();
            btnJTTY = new RadioButton();
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
            btnLookup = new Button();
            btnSend7 = new Button();
            btnStopTX = new Button();
            btnSend8 = new Button();
            btnSend9 = new Button();
            btnSend10 = new Button();
            btnSend11 = new Button();
            btnSend12 = new Button();
            btnSend13 = new Button();
            btnSend14 = new Button();
            tbFreeText = new TextBox();
            label16 = new Label();
            gbJTTYcontrols = new GroupBox();
            btnResetRxFreq = new Button();
            btnTune = new Button();
            btnTol500 = new Button();
            btnTol10 = new Button();
            cbAutoClearFreeText = new CheckBox();
            panelButtons = new Panel();
            panelLogging = new Panel();
            panelTextReceived = new Panel();
            RxTextScroller = new TickerControl();
            cmsScroller = new ContextMenuStrip(components);
            cmsScrollerMenuDeleteLane = new ToolStripMenuItem();
            cmsScrollerMenuDeleteAll = new ToolStripMenuItem();
            lblDupeTimeDate = new Label();
            panelLogging2 = new Panel();
            tbDupeTimeDate = new TextBox();
            cmsLogSave = new ContextMenuStrip(components);
            cmsLogSaveMenuLogQSO = new ToolStripMenuItem();
            cmsLogSaveMenuClearLog = new ToolStripMenuItem();
            cmsLogSaveMenuCancel = new ToolStripMenuItem();
            ((System.ComponentModel.ISupportInitialize)axxmmxy1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)axxmmWater1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)axXMMR1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)axXMMSpec1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)axXMMLevel1).BeginInit();
            gbRTTYbuttons.SuspendLayout();
            gbRttyFilters.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)tbBW).BeginInit();
            gbCWcontrols.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)tbTolerance).BeginInit();
            gbCwFilters.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)sbAudioLevel).BeginInit();
            statusStrip1.SuspendLayout();
            menuStrip1.SuspendLayout();
            cmsLogOverwrite.SuspendLayout();
            contextMenuStripReportFields.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)btnTemplate).BeginInit();
            gbRttyCW.SuspendLayout();
            gbJTTYcontrols.SuspendLayout();
            panelButtons.SuspendLayout();
            panelLogging.SuspendLayout();
            panelTextReceived.SuspendLayout();
            cmsScroller.SuspendLayout();
            panelLogging2.SuspendLayout();
            cmsLogSave.SuspendLayout();
            SuspendLayout();
            // 
            // axxmmxy1
            // 
            axxmmxy1.Enabled = true;
            axxmmxy1.Location = new Point(620, 37);
            axxmmxy1.Name = "axxmmxy1";
            axxmmxy1.OcxState = (AxHost.State)resources.GetObject("axxmmxy1.OcxState");
            axxmmxy1.Size = new Size(168, 177);
            axxmmxy1.TabIndex = 14;
            // 
            // axxmmWater1
            // 
            axxmmWater1.Enabled = true;
            axxmmWater1.Location = new Point(261, 125);
            axxmmWater1.Name = "axxmmWater1";
            axxmmWater1.OcxState = (AxHost.State)resources.GetObject("axxmmWater1.OcxState");
            axxmmWater1.Size = new Size(353, 89);
            axxmmWater1.TabIndex = 13;
            // 
            // axXMMR1
            // 
            axXMMR1.Enabled = true;
            axXMMR1.Location = new Point(201, 59);
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
            axXMMSpec1.Size = new Size(353, 82);
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
            axXMMLevel1.Size = new Size(26, 177);
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
            gbRTTYbuttons.Size = new Size(215, 187);
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
            tbBW.Location = new Point(158, 47);
            tbBW.Maximum = new decimal(new int[] { 999, 0, 0, 0 });
            tbBW.Name = "tbBW";
            tbBW.Size = new Size(40, 23);
            tbBW.TabIndex = 29;
            tbBW.Value = new decimal(new int[] { 40, 0, 0, 0 });
            tbBW.ValueChanged += tbBW_ValueChanged;
            tbBW.KeyUp += tbBW_ValueChanged;
            // 
            // gbCWcontrols
            // 
            gbCWcontrols.Controls.Add(cbAutoSquelch);
            gbCWcontrols.Controls.Add(cbFixWpm);
            gbCWcontrols.Controls.Add(tbConfidence);
            gbCWcontrols.Controls.Add(tbTolerance);
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
            gbCWcontrols.Location = new Point(-500, 27);
            gbCWcontrols.Name = "gbCWcontrols";
            gbCWcontrols.Size = new Size(215, 187);
            gbCWcontrols.TabIndex = 64;
            gbCWcontrols.TabStop = false;
            gbCWcontrols.Visible = false;
            // 
            // cbAutoSquelch
            // 
            cbAutoSquelch.AutoSize = true;
            cbAutoSquelch.CheckAlign = ContentAlignment.MiddleRight;
            cbAutoSquelch.Checked = true;
            cbAutoSquelch.CheckState = CheckState.Checked;
            cbAutoSquelch.Location = new Point(101, 10);
            cbAutoSquelch.Name = "cbAutoSquelch";
            cbAutoSquelch.Size = new Size(95, 19);
            cbAutoSquelch.TabIndex = 76;
            cbAutoSquelch.Text = "Squelch AGC";
            cbAutoSquelch.TextAlign = ContentAlignment.MiddleRight;
            cbAutoSquelch.UseVisualStyleBackColor = true;
            cbAutoSquelch.CheckedChanged += cbAutoSquelch_CheckedChanged;
            // 
            // cbFixWpm
            // 
            cbFixWpm.AutoSize = true;
            cbFixWpm.Location = new Point(92, 128);
            cbFixWpm.Name = "cbFixWpm";
            cbFixWpm.Size = new Size(15, 14);
            cbFixWpm.TabIndex = 75;
            cbFixWpm.UseVisualStyleBackColor = true;
            cbFixWpm.CheckedChanged += cbFixWpm_CheckedChanged;
            // 
            // tbConfidence
            // 
            tbConfidence.Location = new Point(65, 75);
            tbConfidence.Name = "tbConfidence";
            tbConfidence.Size = new Size(40, 23);
            tbConfidence.TabIndex = 74;
            // 
            // tbTolerance
            // 
            tbTolerance.Location = new Point(16, 76);
            tbTolerance.Name = "tbTolerance";
            tbTolerance.Size = new Size(42, 23);
            tbTolerance.TabIndex = 73;
            tbTolerance.Value = new decimal(new int[] { 20, 0, 0, 0 });
            tbTolerance.ValueChanged += tbTolerance_ValueChanged;
            tbTolerance.KeyUp += tbTolerance_ValueChanged;
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
            label12.Location = new Point(58, 128);
            label12.Name = "label12";
            label12.Size = new Size(36, 15);
            label12.TabIndex = 71;
            label12.Text = "WPM";
            // 
            // label11
            // 
            label11.AutoSize = true;
            label11.Location = new Point(158, 27);
            label11.Name = "label11";
            label11.Size = new Size(25, 15);
            label11.TabIndex = 70;
            label11.Text = "BW";
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Location = new Point(120, 28);
            label10.Name = "label10";
            label10.Size = new Size(32, 15);
            label10.TabIndex = 69;
            label10.Text = "Tone";
            // 
            // tbToneFreq
            // 
            tbToneFreq.Location = new Point(117, 46);
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
            sbAudioLevel.AutoSize = false;
            sbAudioLevel.Location = new Point(111, 72);
            sbAudioLevel.Maximum = 1000;
            sbAudioLevel.Name = "sbAudioLevel";
            sbAudioLevel.Size = new Size(104, 26);
            sbAudioLevel.TabIndex = 62;
            sbAudioLevel.TickStyle = TickStyle.None;
            sbAudioLevel.Value = 506;
            sbAudioLevel.Scroll += sbAudioLevel_Scroll;
            // 
            // tbWPM
            // 
            tbWPM.Location = new Point(58, 146);
            tbWPM.Name = "tbWPM";
            tbWPM.Size = new Size(46, 23);
            tbWPM.TabIndex = 59;
            tbWPM.Text = "25";
            tbWPM.TextAlign = HorizontalAlignment.Center;
            tbWPM.KeyPress += tbWPM_KeyPress;
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
            statusStrip1.Items.AddRange(new ToolStripItem[] { lblMMTTYstatus, toolStripStatusLabel3, lblPipeStatus, toolStripStatusLabel1, lblWSjttyStatus, toolStripStatusLabel2, lblLogFileName });
            statusStrip1.Location = new Point(0, 571);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new Size(804, 22);
            statusStrip1.TabIndex = 22;
            statusStrip1.Text = "statusStrip1";
            // 
            // lblMMTTYstatus
            // 
            lblMMTTYstatus.Name = "lblMMTTYstatus";
            lblMMTTYstatus.Size = new Size(82, 17);
            lblMMTTYstatus.Text = "MMTTY status";
            lblMMTTYstatus.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // toolStripStatusLabel3
            // 
            toolStripStatusLabel3.Name = "toolStripStatusLabel3";
            toolStripStatusLabel3.Size = new Size(16, 17);
            toolStripStatusLabel3.Text = " | ";
            // 
            // lblPipeStatus
            // 
            lblPipeStatus.Name = "lblPipeStatus";
            lblPipeStatus.Size = new Size(173, 17);
            lblPipeStatus.Text = "TxLink Pipe ... Trying to connect";
            // 
            // toolStripStatusLabel1
            // 
            toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            toolStripStatusLabel1.Size = new Size(16, 17);
            toolStripStatusLabel1.Text = " | ";
            // 
            // lblWSjttyStatus
            // 
            lblWSjttyStatus.Name = "lblWSjttyStatus";
            lblWSjttyStatus.Size = new Size(104, 17);
            lblWSjttyStatus.Text = "WS not connected";
            // 
            // toolStripStatusLabel2
            // 
            toolStripStatusLabel2.Name = "toolStripStatusLabel2";
            toolStripStatusLabel2.Size = new Size(16, 17);
            toolStripStatusLabel2.Text = " | ";
            // 
            // lblLogFileName
            // 
            lblLogFileName.ImageAlign = ContentAlignment.MiddleRight;
            lblLogFileName.Name = "lblLogFileName";
            lblLogFileName.Size = new Size(83, 17);
            lblLogFileName.Text = "Log File Name";
            lblLogFileName.TextAlign = ContentAlignment.MiddleRight;
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
            exportLogToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { createNewLogBookToolStripMenuItem, exportCabrilloToolStripMenuItem, openLogDirectoryToolStripMenuItem });
            exportLogToolStripMenuItem.Name = "exportLogToolStripMenuItem";
            exportLogToolStripMenuItem.Size = new Size(69, 20);
            exportLogToolStripMenuItem.Text = "Log Book";
            // 
            // createNewLogBookToolStripMenuItem
            // 
            createNewLogBookToolStripMenuItem.Name = "createNewLogBookToolStripMenuItem";
            createNewLogBookToolStripMenuItem.Size = new Size(183, 22);
            createNewLogBookToolStripMenuItem.Text = "Open New Log Book";
            createNewLogBookToolStripMenuItem.Click += createNewLogBookToolStripMenuItem_Click;
            // 
            // exportCabrilloToolStripMenuItem
            // 
            exportCabrilloToolStripMenuItem.Name = "exportCabrilloToolStripMenuItem";
            exportCabrilloToolStripMenuItem.Size = new Size(183, 22);
            exportCabrilloToolStripMenuItem.Text = "Export Cabrillo";
            exportCabrilloToolStripMenuItem.Click += exportCabrilloToolStripMenuItem_Click;
            // 
            // openLogDirectoryToolStripMenuItem
            // 
            openLogDirectoryToolStripMenuItem.Name = "openLogDirectoryToolStripMenuItem";
            openLogDirectoryToolStripMenuItem.Size = new Size(183, 22);
            openLogDirectoryToolStripMenuItem.Text = "Open log directory";
            openLogDirectoryToolStripMenuItem.Click += openLogDirectoryToolStripMenuItem_Click;
            // 
            // tbTextReceived
            // 
            tbTextReceived.AcceptsTab = true;
            tbTextReceived.ContextMenuStrip = cmsLogOverwrite;
            tbTextReceived.Font = new Font("Courier New", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            tbTextReceived.Location = new Point(0, 42);
            tbTextReceived.Name = "tbTextReceived";
            tbTextReceived.Size = new Size(402, 95);
            tbTextReceived.TabIndex = 24;
            tbTextReceived.Text = "";
            tbTextReceived.WordWrap = false;
            tbTextReceived.MouseDown += tbTextReceived_MouseDown;
            // 
            // cmsLogOverwrite
            // 
            cmsLogOverwrite.Items.AddRange(new ToolStripItem[] { cmsLogOverwriteConfirm, cmsLogOverwriteCancel, toolStripSeparator1, cmsLogQSOFirst });
            cmsLogOverwrite.Name = "cmsLogOverwtite";
            cmsLogOverwrite.Size = new Size(145, 76);
            cmsLogOverwrite.Text = "Log Overwrite";
            cmsLogOverwrite.Closed += cmsLogOverwrite_Closed;
            // 
            // cmsLogOverwriteConfirm
            // 
            cmsLogOverwriteConfirm.Name = "cmsLogOverwriteConfirm";
            cmsLogOverwriteConfirm.Size = new Size(144, 22);
            cmsLogOverwriteConfirm.Text = "Confirm";
            cmsLogOverwriteConfirm.Click += cmsLogOverwriteConfirm_Click;
            // 
            // cmsLogOverwriteCancel
            // 
            cmsLogOverwriteCancel.Name = "cmsLogOverwriteCancel";
            cmsLogOverwriteCancel.Size = new Size(144, 22);
            cmsLogOverwriteCancel.Text = "Cancel";
            cmsLogOverwriteCancel.Click += cmsLogOverwriteCancel_Click;
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(141, 6);
            // 
            // cmsLogQSOFirst
            // 
            cmsLogQSOFirst.Name = "cmsLogQSOFirst";
            cmsLogQSOFirst.Size = new Size(144, 22);
            cmsLogQSOFirst.Text = "Log QSO first";
            cmsLogQSOFirst.Click += cmsLogQSOFirst_Click;
            // 
            // tbCallsign
            // 
            tbCallsign.CharacterCasing = CharacterCasing.Upper;
            tbCallsign.Location = new Point(204, 8);
            tbCallsign.Name = "tbCallsign";
            tbCallsign.Size = new Size(108, 23);
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
            tbReportOut.ContextMenuStrip = contextMenuStripReportFields;
            tbReportOut.Location = new Point(204, 37);
            tbReportOut.Name = "tbReportOut";
            tbReportOut.Size = new Size(42, 23);
            tbReportOut.TabIndex = 26;
            tbReportOut.Tag = "false";
            tbReportOut.Text = "599";
            // 
            // contextMenuStripReportFields
            // 
            contextMenuStripReportFields.Items.AddRange(new ToolStripItem[] { fixToolStripMenuItem, clearToolStripMenuItem });
            contextMenuStripReportFields.Name = "contextMenuStripReportFields";
            contextMenuStripReportFields.Size = new Size(102, 48);
            contextMenuStripReportFields.Opening += contextMenuStripReportFields_Opening;
            // 
            // fixToolStripMenuItem
            // 
            fixToolStripMenuItem.CheckOnClick = true;
            fixToolStripMenuItem.Name = "fixToolStripMenuItem";
            fixToolStripMenuItem.Size = new Size(101, 22);
            fixToolStripMenuItem.Text = "Fix";
            fixToolStripMenuItem.Click += fixToolStripMenuItem_Click;
            // 
            // clearToolStripMenuItem
            // 
            clearToolStripMenuItem.Name = "clearToolStripMenuItem";
            clearToolStripMenuItem.Size = new Size(101, 22);
            clearToolStripMenuItem.Text = "Clear";
            clearToolStripMenuItem.Click += clearToolStripMenuItem_Click;
            // 
            // btnTemplate
            // 
            btnTemplate.Enabled = true;
            btnTemplate.Location = new Point(176, 59);
            btnTemplate.Name = "btnTemplate";
            btnTemplate.OcxState = (AxHost.State)resources.GetObject("btnTemplate.OcxState");
            btnTemplate.Size = new Size(100, 50);
            btnTemplate.TabIndex = 27;
            btnTemplate.Visible = false;
            // 
            // btnSend1
            // 
            btnSend1.Location = new Point(4, 3);
            btnSend1.Name = "btnSend1";
            btnSend1.Size = new Size(55, 23);
            btnSend1.TabIndex = 29;
            btnSend1.Text = "CQ";
            btnSend1.UseVisualStyleBackColor = true;
            btnSend1.Click += MacroButton_Click;
            // 
            // gbRttyCW
            // 
            gbRttyCW.Controls.Add(btnJTTY);
            gbRttyCW.Controls.Add(btnRTTY);
            gbRttyCW.Controls.Add(btnCW);
            gbRttyCW.Location = new Point(12, 148);
            gbRttyCW.Name = "gbRttyCW";
            gbRttyCW.Size = new Size(58, 66);
            gbRttyCW.TabIndex = 29;
            gbRttyCW.TabStop = false;
            // 
            // btnJTTY
            // 
            btnJTTY.AutoSize = true;
            btnJTTY.Location = new Point(5, 47);
            btnJTTY.Name = "btnJTTY";
            btnJTTY.Size = new Size(48, 19);
            btnJTTY.TabIndex = 2;
            btnJTTY.TabStop = true;
            btnJTTY.Text = "JTTY";
            btnJTTY.UseVisualStyleBackColor = true;
            btnJTTY.CheckedChanged += btnJTTY_CheckedChanged;
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
            tbComment.Location = new Point(204, 182);
            tbComment.Name = "tbComment";
            tbComment.Size = new Size(168, 23);
            tbComment.TabIndex = 30;
            // 
            // tbSerialNumOut
            // 
            tbSerialNumOut.CharacterCasing = CharacterCasing.Upper;
            tbSerialNumOut.ContextMenuStrip = contextMenuStripReportFields;
            tbSerialNumOut.Location = new Point(255, 37);
            tbSerialNumOut.Name = "tbSerialNumOut";
            tbSerialNumOut.Size = new Size(57, 23);
            tbSerialNumOut.TabIndex = 31;
            tbSerialNumOut.Tag = "false";
            // 
            // tbSerialNumIn
            // 
            tbSerialNumIn.CharacterCasing = CharacterCasing.Upper;
            tbSerialNumIn.ContextMenuStrip = contextMenuStripReportFields;
            tbSerialNumIn.Location = new Point(255, 66);
            tbSerialNumIn.Name = "tbSerialNumIn";
            tbSerialNumIn.Size = new Size(57, 23);
            tbSerialNumIn.TabIndex = 33;
            tbSerialNumIn.Tag = "false";
            // 
            // tbReportIn
            // 
            tbReportIn.CharacterCasing = CharacterCasing.Upper;
            tbReportIn.ContextMenuStrip = contextMenuStripReportFields;
            tbReportIn.Location = new Point(204, 66);
            tbReportIn.Name = "tbReportIn";
            tbReportIn.Size = new Size(42, 23);
            tbReportIn.TabIndex = 32;
            tbReportIn.Tag = "false";
            // 
            // tbQTH
            // 
            tbQTH.Location = new Point(204, 153);
            tbQTH.Name = "tbQTH";
            tbQTH.Size = new Size(108, 23);
            tbQTH.TabIndex = 34;
            // 
            // tbOpName
            // 
            tbOpName.Location = new Point(204, 124);
            tbOpName.Name = "tbOpName";
            tbOpName.Size = new Size(108, 23);
            tbOpName.TabIndex = 35;
            // 
            // tbLocator
            // 
            tbLocator.CharacterCasing = CharacterCasing.Upper;
            tbLocator.Location = new Point(204, 95);
            tbLocator.Name = "tbLocator";
            tbLocator.Size = new Size(108, 23);
            tbLocator.TabIndex = 36;
            // 
            // btnSerialNumInc
            // 
            btnSerialNumInc.Font = new Font("Segoe UI", 8.25F);
            btnSerialNumInc.Location = new Point(316, 29);
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
            btnSerialNumDec.Location = new Point(316, 50);
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
            cbSerialNumAutoInc.Location = new Point(357, 41);
            cbSerialNumAutoInc.Name = "cbSerialNumAutoInc";
            cbSerialNumAutoInc.Size = new Size(15, 14);
            cbSerialNumAutoInc.TabIndex = 39;
            cbSerialNumAutoInc.UseVisualStyleBackColor = true;
            // 
            // btnSend2
            // 
            btnSend2.Location = new Point(67, 3);
            btnSend2.Name = "btnSend2";
            btnSend2.Size = new Size(55, 23);
            btnSend2.TabIndex = 40;
            btnSend2.Text = "CQ";
            btnSend2.UseVisualStyleBackColor = true;
            btnSend2.Click += MacroButton_Click;
            // 
            // btnSend4
            // 
            btnSend4.Location = new Point(193, 3);
            btnSend4.Name = "btnSend4";
            btnSend4.Size = new Size(55, 23);
            btnSend4.TabIndex = 41;
            btnSend4.Text = "CQ";
            btnSend4.UseVisualStyleBackColor = true;
            btnSend4.Click += MacroButton_Click;
            // 
            // btnSend3
            // 
            btnSend3.Location = new Point(130, 3);
            btnSend3.Name = "btnSend3";
            btnSend3.Size = new Size(55, 23);
            btnSend3.TabIndex = 42;
            btnSend3.Text = "CQ";
            btnSend3.UseVisualStyleBackColor = true;
            btnSend3.Click += MacroButton_Click;
            // 
            // btnSend5
            // 
            btnSend5.Location = new Point(256, 3);
            btnSend5.Name = "btnSend5";
            btnSend5.Size = new Size(55, 23);
            btnSend5.TabIndex = 43;
            btnSend5.Text = "CQ";
            btnSend5.UseVisualStyleBackColor = true;
            btnSend5.Click += MacroButton_Click;
            // 
            // btnSend6
            // 
            btnSend6.Location = new Point(319, 3);
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
            cbEditButton.Location = new Point(457, 7);
            cbEditButton.Name = "cbEditButton";
            cbEditButton.Size = new Size(46, 19);
            cbEditButton.TabIndex = 45;
            cbEditButton.Text = "Edit";
            cbEditButton.UseVisualStyleBackColor = true;
            // 
            // btnLogQSO
            // 
            btnLogQSO.Location = new Point(204, 213);
            btnLogQSO.Name = "btnLogQSO";
            btnLogQSO.Size = new Size(75, 23);
            btnLogQSO.TabIndex = 46;
            btnLogQSO.Text = "Log QSO";
            btnLogQSO.UseVisualStyleBackColor = true;
            btnLogQSO.MouseDown += btnLogQSO_MouseDown;
            // 
            // tbNumberQSOs
            // 
            tbNumberQSOs.Location = new Point(306, 214);
            tbNumberQSOs.Name = "tbNumberQSOs";
            tbNumberQSOs.Size = new Size(66, 23);
            tbNumberQSOs.TabIndex = 47;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(286, 217);
            label1.Name = "label1";
            label1.Size = new Size(14, 15);
            label1.TabIndex = 48;
            label1.Text = "#";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(153, 11);
            label2.Name = "label2";
            label2.Size = new Size(45, 15);
            label2.TabIndex = 49;
            label2.Text = "DX Call";
            label2.TextAlign = ContentAlignment.MiddleRight;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(146, 41);
            label3.Name = "label3";
            label3.Size = new Size(52, 15);
            label3.TabIndex = 50;
            label3.Text = "Rprt Out";
            label3.TextAlign = ContentAlignment.MiddleRight;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(121, 70);
            label4.Name = "label4";
            label4.Size = new Size(77, 15);
            label4.TabIndex = 51;
            label4.Text = "Rprt In (Shift)";
            label4.TextAlign = ContentAlignment.MiddleRight;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(113, 98);
            label5.Name = "label5";
            label5.Size = new Size(85, 15);
            label5.TabIndex = 52;
            label5.Text = "Locator (CTRL)";
            label5.TextAlign = ContentAlignment.MiddleRight;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(103, 127);
            label6.Name = "label6";
            label6.Size = new Size(95, 15);
            label6.TabIndex = 53;
            label6.Text = "Name (Shft/Ctrl)";
            label6.TextAlign = ContentAlignment.MiddleRight;
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(142, 156);
            label7.Name = "label7";
            label7.Size = new Size(56, 15);
            label7.TabIndex = 54;
            label7.Text = "QTH (Alt)";
            label7.TextAlign = ContentAlignment.MiddleRight;
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(137, 185);
            label8.Name = "label8";
            label8.Size = new Size(61, 15);
            label8.TabIndex = 55;
            label8.Text = "Comment";
            label8.TextAlign = ContentAlignment.MiddleRight;
            // 
            // btnClearText
            // 
            btnClearText.Location = new Point(373, 13);
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
            label9.Location = new Point(7, 16);
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
            tbRxFrequency.Location = new Point(50, 10);
            tbRxFrequency.Name = "tbRxFrequency";
            tbRxFrequency.Size = new Size(119, 26);
            tbRxFrequency.TabIndex = 69;
            tbRxFrequency.TextAlign = HorizontalAlignment.Center;
            tbRxFrequency.TextChanged += tbCallsign_TextChanged;
            // 
            // tbCallSignList
            // 
            tbCallSignList.Location = new Point(2, 38);
            tbCallSignList.Multiline = true;
            tbCallSignList.Name = "tbCallSignList";
            tbCallSignList.ScrollBars = ScrollBars.Vertical;
            tbCallSignList.Size = new Size(100, 199);
            tbCallSignList.TabIndex = 70;
            tbCallSignList.MouseDoubleClick += tbCallSignList_MouseDoubleClick;
            // 
            // label14
            // 
            label14.AutoSize = true;
            label14.Location = new Point(17, 16);
            label14.Name = "label14";
            label14.Size = new Size(54, 15);
            label14.TabIndex = 71;
            label14.Text = "Callsigns";
            // 
            // tbPwr
            // 
            tbPwr.Location = new Point(345, 153);
            tbPwr.Name = "tbPwr";
            tbPwr.Size = new Size(27, 23);
            tbPwr.TabIndex = 72;
            tbPwr.Text = "100";
            // 
            // label15
            // 
            label15.AutoSize = true;
            label15.Location = new Point(314, 156);
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
            lblDUPE.Location = new Point(307, 1);
            lblDUPE.Name = "lblDUPE";
            lblDUPE.Size = new Size(67, 30);
            lblDUPE.TabIndex = 74;
            lblDUPE.Text = "DUPE";
            lblDUPE.Visible = false;
            // 
            // btnLookup
            // 
            btnLookup.Location = new Point(316, 94);
            btnLookup.Name = "btnLookup";
            btnLookup.Size = new Size(56, 23);
            btnLookup.TabIndex = 75;
            btnLookup.Text = "Lookup";
            btnLookup.UseVisualStyleBackColor = true;
            btnLookup.MouseDown += btnLookup_MouseDown;
            // 
            // btnSend7
            // 
            btnSend7.Location = new Point(382, 3);
            btnSend7.Name = "btnSend7";
            btnSend7.Size = new Size(55, 23);
            btnSend7.TabIndex = 76;
            btnSend7.Text = "CQ";
            btnSend7.UseVisualStyleBackColor = true;
            btnSend7.Click += MacroButton_Click;
            // 
            // btnStopTX
            // 
            btnStopTX.Location = new Point(4, 58);
            btnStopTX.Name = "btnStopTX";
            btnStopTX.Size = new Size(55, 23);
            btnStopTX.TabIndex = 77;
            btnStopTX.Text = "STOP";
            btnStopTX.UseVisualStyleBackColor = true;
            btnStopTX.Click += btnStopTX_Click;
            // 
            // btnSend8
            // 
            btnSend8.Location = new Point(4, 29);
            btnSend8.Name = "btnSend8";
            btnSend8.Size = new Size(55, 23);
            btnSend8.TabIndex = 78;
            btnSend8.Text = "tbd";
            btnSend8.UseVisualStyleBackColor = true;
            btnSend8.Click += MacroButton_Click;
            // 
            // btnSend9
            // 
            btnSend9.Location = new Point(67, 29);
            btnSend9.Name = "btnSend9";
            btnSend9.Size = new Size(55, 23);
            btnSend9.TabIndex = 79;
            btnSend9.Text = "tbd";
            btnSend9.UseVisualStyleBackColor = true;
            btnSend9.Click += MacroButton_Click;
            // 
            // btnSend10
            // 
            btnSend10.Location = new Point(130, 29);
            btnSend10.Name = "btnSend10";
            btnSend10.Size = new Size(55, 23);
            btnSend10.TabIndex = 80;
            btnSend10.Text = "tbd";
            btnSend10.UseVisualStyleBackColor = true;
            btnSend10.Click += MacroButton_Click;
            // 
            // btnSend11
            // 
            btnSend11.Location = new Point(193, 29);
            btnSend11.Name = "btnSend11";
            btnSend11.Size = new Size(55, 23);
            btnSend11.TabIndex = 81;
            btnSend11.Text = "tbd";
            btnSend11.UseVisualStyleBackColor = true;
            btnSend11.Click += MacroButton_Click;
            // 
            // btnSend12
            // 
            btnSend12.Location = new Point(256, 29);
            btnSend12.Name = "btnSend12";
            btnSend12.Size = new Size(55, 23);
            btnSend12.TabIndex = 82;
            btnSend12.Text = "tbd";
            btnSend12.UseVisualStyleBackColor = true;
            btnSend12.Click += MacroButton_Click;
            // 
            // btnSend13
            // 
            btnSend13.Location = new Point(319, 29);
            btnSend13.Name = "btnSend13";
            btnSend13.Size = new Size(55, 23);
            btnSend13.TabIndex = 83;
            btnSend13.Text = "tbd";
            btnSend13.UseVisualStyleBackColor = true;
            btnSend13.Click += MacroButton_Click;
            // 
            // btnSend14
            // 
            btnSend14.Location = new Point(382, 29);
            btnSend14.Name = "btnSend14";
            btnSend14.Size = new Size(55, 23);
            btnSend14.TabIndex = 84;
            btnSend14.Text = "tbd";
            btnSend14.UseVisualStyleBackColor = true;
            btnSend14.Click += MacroButton_Click;
            // 
            // tbFreeText
            // 
            tbFreeText.Location = new Point(134, 59);
            tbFreeText.Name = "tbFreeText";
            tbFreeText.Size = new Size(303, 23);
            tbFreeText.TabIndex = 85;
            tbFreeText.KeyPress += tbFreeText_KeyPress;
            // 
            // label16
            // 
            label16.AutoSize = true;
            label16.Location = new Point(78, 62);
            label16.Name = "label16";
            label16.Size = new Size(50, 15);
            label16.TabIndex = 86;
            label16.Text = "FreeText";
            // 
            // gbJTTYcontrols
            // 
            gbJTTYcontrols.Controls.Add(btnResetRxFreq);
            gbJTTYcontrols.Controls.Add(btnTune);
            gbJTTYcontrols.Controls.Add(btnTol500);
            gbJTTYcontrols.Controls.Add(btnTol10);
            gbJTTYcontrols.Location = new Point(-500, 27);
            gbJTTYcontrols.Name = "gbJTTYcontrols";
            gbJTTYcontrols.Size = new Size(211, 187);
            gbJTTYcontrols.TabIndex = 87;
            gbJTTYcontrols.TabStop = false;
            gbJTTYcontrols.Text = "JTTY";
            // 
            // btnResetRxFreq
            // 
            btnResetRxFreq.Location = new Point(8, 82);
            btnResetRxFreq.Name = "btnResetRxFreq";
            btnResetRxFreq.Size = new Size(51, 23);
            btnResetRxFreq.TabIndex = 71;
            btnResetRxFreq.Text = "Reset";
            btnResetRxFreq.UseVisualStyleBackColor = true;
            btnResetRxFreq.Click += btnResetRxFreq_Click;
            // 
            // btnTune
            // 
            btnTune.Location = new Point(154, 158);
            btnTune.Name = "btnTune";
            btnTune.Size = new Size(51, 23);
            btnTune.TabIndex = 70;
            btnTune.Text = "Tune";
            btnTune.UseVisualStyleBackColor = true;
            btnTune.Click += btnTune_Click;
            // 
            // btnTol500
            // 
            btnTol500.Location = new Point(8, 53);
            btnTol500.Name = "btnTol500";
            btnTol500.Size = new Size(51, 23);
            btnTol500.TabIndex = 1;
            btnTol500.Text = "Tol 500";
            btnTol500.UseVisualStyleBackColor = true;
            btnTol500.Click += btnTol500_Click;
            // 
            // btnTol10
            // 
            btnTol10.Location = new Point(8, 24);
            btnTol10.Name = "btnTol10";
            btnTol10.Size = new Size(51, 23);
            btnTol10.TabIndex = 0;
            btnTol10.Text = "Tol 10";
            btnTol10.UseVisualStyleBackColor = true;
            btnTol10.Click += btnTol10_Click;
            // 
            // cbAutoClearFreeText
            // 
            cbAutoClearFreeText.AutoSize = true;
            cbAutoClearFreeText.CheckAlign = ContentAlignment.MiddleRight;
            cbAutoClearFreeText.Location = new Point(450, 61);
            cbAutoClearFreeText.Name = "cbAutoClearFreeText";
            cbAutoClearFreeText.Size = new Size(53, 19);
            cbAutoClearFreeText.TabIndex = 88;
            cbAutoClearFreeText.Text = "Clear";
            cbAutoClearFreeText.UseVisualStyleBackColor = true;
            // 
            // panelButtons
            // 
            panelButtons.Controls.Add(btnSend4);
            panelButtons.Controls.Add(cbAutoClearFreeText);
            panelButtons.Controls.Add(btnSend1);
            panelButtons.Controls.Add(label16);
            panelButtons.Controls.Add(btnSend2);
            panelButtons.Controls.Add(tbFreeText);
            panelButtons.Controls.Add(btnSend3);
            panelButtons.Controls.Add(btnSend14);
            panelButtons.Controls.Add(btnSend5);
            panelButtons.Controls.Add(btnSend13);
            panelButtons.Controls.Add(btnSend6);
            panelButtons.Controls.Add(btnSend12);
            panelButtons.Controls.Add(cbEditButton);
            panelButtons.Controls.Add(btnSend11);
            panelButtons.Controls.Add(btnSend7);
            panelButtons.Controls.Add(btnSend10);
            panelButtons.Controls.Add(btnStopTX);
            panelButtons.Controls.Add(btnSend9);
            panelButtons.Controls.Add(btnSend8);
            panelButtons.Location = new Point(12, 476);
            panelButtons.Name = "panelButtons";
            panelButtons.Size = new Size(519, 88);
            panelButtons.TabIndex = 89;
            // 
            // panelLogging
            // 
            panelLogging.Controls.Add(tbOpName);
            panelLogging.Controls.Add(lblDUPE);
            panelLogging.Controls.Add(btnLookup);
            panelLogging.Controls.Add(tbCallsign);
            panelLogging.Controls.Add(label15);
            panelLogging.Controls.Add(tbReportOut);
            panelLogging.Controls.Add(tbPwr);
            panelLogging.Controls.Add(tbComment);
            panelLogging.Controls.Add(label14);
            panelLogging.Controls.Add(tbSerialNumOut);
            panelLogging.Controls.Add(tbCallSignList);
            panelLogging.Controls.Add(tbReportIn);
            panelLogging.Controls.Add(tbSerialNumIn);
            panelLogging.Controls.Add(tbQTH);
            panelLogging.Controls.Add(tbLocator);
            panelLogging.Controls.Add(btnSerialNumInc);
            panelLogging.Controls.Add(label8);
            panelLogging.Controls.Add(btnSerialNumDec);
            panelLogging.Controls.Add(label7);
            panelLogging.Controls.Add(cbSerialNumAutoInc);
            panelLogging.Controls.Add(label6);
            panelLogging.Controls.Add(btnLogQSO);
            panelLogging.Controls.Add(label5);
            panelLogging.Controls.Add(tbNumberQSOs);
            panelLogging.Controls.Add(label4);
            panelLogging.Controls.Add(label1);
            panelLogging.Controls.Add(label3);
            panelLogging.Controls.Add(label2);
            panelLogging.Location = new Point(414, 221);
            panelLogging.Name = "panelLogging";
            panelLogging.Size = new Size(374, 249);
            panelLogging.TabIndex = 90;
            // 
            // panelTextReceived
            // 
            panelTextReceived.Controls.Add(RxTextScroller);
            panelTextReceived.Controls.Add(tbTextReceived);
            panelTextReceived.Controls.Add(axXMMR1);
            panelTextReceived.Controls.Add(btnTemplate);
            panelTextReceived.Controls.Add(tbRxFrequency);
            panelTextReceived.Controls.Add(btnClearText);
            panelTextReceived.Controls.Add(label9);
            panelTextReceived.Location = new Point(12, 220);
            panelTextReceived.Name = "panelTextReceived";
            panelTextReceived.Size = new Size(405, 250);
            panelTextReceived.TabIndex = 91;
            // 
            // RxTextScroller
            // 
            RxTextScroller.BackColor = SystemColors.Window;
            RxTextScroller.ContextMenuStrip = cmsScroller;
            RxTextScroller.Font = new Font("Consolas", 10F);
            RxTextScroller.ForeColor = SystemColors.WindowText;
            RxTextScroller.Location = new Point(0, 143);
            RxTextScroller.Name = "RxTextScroller";
            RxTextScroller.Size = new Size(402, 107);
            RxTextScroller.TabIndex = 70;
            RxTextScroller.Text = "tickerControl1";
            // 
            // cmsScroller
            // 
            cmsScroller.Items.AddRange(new ToolStripItem[] { cmsScrollerMenuDeleteLane, cmsScrollerMenuDeleteAll });
            cmsScroller.Name = "cmsScroller";
            cmsScroller.Size = new Size(181, 70);
            // 
            // cmsScrollerMenuDeleteLane
            // 
            cmsScrollerMenuDeleteLane.Name = "cmsScrollerMenuDeleteLane";
            cmsScrollerMenuDeleteLane.Size = new Size(180, 22);
            cmsScrollerMenuDeleteLane.Text = "Delete Lane";
            cmsScrollerMenuDeleteLane.Click += cmsScrollerMenuDeleteLane_Click;
            // 
            // cmsScrollerMenuDeleteAll
            // 
            cmsScrollerMenuDeleteAll.Name = "cmsScrollerMenuDeleteAll";
            cmsScrollerMenuDeleteAll.Size = new Size(180, 22);
            cmsScrollerMenuDeleteAll.Text = "Delete All";
            cmsScrollerMenuDeleteAll.Click += cmsScrollerMenuDeleteAll_Click;
            // 
            // lblDupeTimeDate
            // 
            lblDupeTimeDate.AutoSize = true;
            lblDupeTimeDate.Font = new Font("Segoe UI", 15.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblDupeTimeDate.ForeColor = Color.Red;
            lblDupeTimeDate.Location = new Point(0, 1);
            lblDupeTimeDate.Name = "lblDupeTimeDate";
            lblDupeTimeDate.Size = new Size(67, 30);
            lblDupeTimeDate.TabIndex = 76;
            lblDupeTimeDate.Text = "DUPE";
            lblDupeTimeDate.Visible = false;
            // 
            // panelLogging2
            // 
            panelLogging2.Controls.Add(tbDupeTimeDate);
            panelLogging2.Controls.Add(lblDupeTimeDate);
            panelLogging2.Location = new Point(535, 475);
            panelLogging2.Name = "panelLogging2";
            panelLogging2.Size = new Size(253, 42);
            panelLogging2.TabIndex = 92;
            // 
            // tbDupeTimeDate
            // 
            tbDupeTimeDate.Location = new Point(64, 8);
            tbDupeTimeDate.Name = "tbDupeTimeDate";
            tbDupeTimeDate.Size = new Size(187, 23);
            tbDupeTimeDate.TabIndex = 77;
            tbDupeTimeDate.Visible = false;
            tbDupeTimeDate.WordWrap = false;
            // 
            // cmsLogSave
            // 
            cmsLogSave.Items.AddRange(new ToolStripItem[] { cmsLogSaveMenuLogQSO, cmsLogSaveMenuClearLog, cmsLogSaveMenuCancel });
            cmsLogSave.Name = "cmsLogSave";
            cmsLogSave.Size = new Size(129, 70);
            cmsLogSave.Closed += cmsLogSave_Closed;
            // 
            // cmsLogSaveMenuLogQSO
            // 
            cmsLogSaveMenuLogQSO.Name = "cmsLogSaveMenuLogQSO";
            cmsLogSaveMenuLogQSO.Size = new Size(128, 22);
            cmsLogSaveMenuLogQSO.Text = "Log QSO";
            cmsLogSaveMenuLogQSO.Click += cmsLogSaveMenuLogQSO_Click;
            // 
            // cmsLogSaveMenuClearLog
            // 
            cmsLogSaveMenuClearLog.Name = "cmsLogSaveMenuClearLog";
            cmsLogSaveMenuClearLog.Size = new Size(128, 22);
            cmsLogSaveMenuClearLog.Text = "Clear Log";
            cmsLogSaveMenuClearLog.Click += cmsLogSaveMenuClearLog_Click;
            // 
            // cmsLogSaveMenuCancel
            // 
            cmsLogSaveMenuCancel.Name = "cmsLogSaveMenuCancel";
            cmsLogSaveMenuCancel.Size = new Size(128, 22);
            cmsLogSaveMenuCancel.Text = "No Action";
            cmsLogSaveMenuCancel.Click += cmsLogSaveMenuCancel_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(804, 593);
            Controls.Add(panelLogging2);
            Controls.Add(panelTextReceived);
            Controls.Add(panelLogging);
            Controls.Add(panelButtons);
            Controls.Add(gbRttyCW);
            Controls.Add(statusStrip1);
            Controls.Add(menuStrip1);
            Controls.Add(axXMMSpec1);
            Controls.Add(axXMMLevel1);
            Controls.Add(axxmmWater1);
            Controls.Add(axxmmxy1);
            Controls.Add(gbRTTYbuttons);
            Controls.Add(gbCWcontrols);
            Controls.Add(gbJTTYcontrols);
            Icon = (Icon)resources.GetObject("$this.Icon");
            KeyPreview = true;
            MainMenuStrip = menuStrip1;
            Name = "Form1";
            Text = "RTTY/CW Controller";
            FormClosing += Form1_FormClosing;
            Load += Form1_Load;
            KeyDown += Form1_KeyDown;
            Resize += Form1_Resize;
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
            ((System.ComponentModel.ISupportInitialize)tbTolerance).EndInit();
            gbCwFilters.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)sbAudioLevel).EndInit();
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            cmsLogOverwrite.ResumeLayout(false);
            contextMenuStripReportFields.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)btnTemplate).EndInit();
            gbRttyCW.ResumeLayout(false);
            gbRttyCW.PerformLayout();
            gbJTTYcontrols.ResumeLayout(false);
            panelButtons.ResumeLayout(false);
            panelButtons.PerformLayout();
            panelLogging.ResumeLayout(false);
            panelLogging.PerformLayout();
            panelTextReceived.ResumeLayout(false);
            panelTextReceived.PerformLayout();
            cmsScroller.ResumeLayout(false);
            panelLogging2.ResumeLayout(false);
            panelLogging2.PerformLayout();
            cmsLogSave.ResumeLayout(false);
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
        private RichTextBox tbTextReceived;
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
        private NumericUpDown tbTolerance;
        private TextBox tbConfidence;
        private CheckBox cbFixWpm;
        private CheckBox cbAutoSquelch;
        private Button btnLookup;
        private ToolStripMenuItem createNewLogBookToolStripMenuItem;
        private ToolStripStatusLabel lblLogFileName;
        private Button btnSend7;
        private Button btnStopTX;
        private Button btnSend8;
        private Button btnSend9;
        private Button btnSend10;
        private Button btnSend11;
        private Button btnSend12;
        private Button btnSend13;
        private Button btnSend14;
        private RadioButton btnJTTY;
        private TextBox tbFreeText;
        private Label label16;
        private GroupBox gbJTTYcontrols;
        private ToolStripStatusLabel lblWSjttyStatus;
        private ToolStripStatusLabel toolStripStatusLabel1;
        private ToolStripStatusLabel toolStripStatusLabel2;
        private ToolStripStatusLabel toolStripStatusLabel3;
        private CheckBox cbAutoClearFreeText;
        private Panel panelButtons;
        private Panel panelLogging;
        private Panel panelTextReceived;
        private ContextMenuStrip contextMenuStripReportFields;
        private ToolStripMenuItem fixToolStripMenuItem;
        private ToolStripMenuItem clearToolStripMenuItem;
        private Label lblDupeTimeDate;
        private Panel panelLogging2;
        private TextBox tbDupeTimeDate;
        private Button btnTol500;
        private Button btnTol10;
        private Button btnTune;
        private Button btnResetRxFreq;
        private ContextMenuStrip cmsLogOverwrite;
        private ToolStripMenuItem cmsLogOverwriteConfirm;
        private ToolStripMenuItem cmsLogOverwriteCancel;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripMenuItem cmsLogQSOFirst;
        private ContextMenuStrip cmsLogSave;
        private ToolStripMenuItem cmsLogSaveMenuLogQSO;
        private ToolStripMenuItem cmsLogSaveMenuClearLog;
        private ToolStripMenuItem cmsLogSaveMenuCancel;
        private TickerControl RxTextScroller;
        private ContextMenuStrip cmsScroller;
        private ToolStripMenuItem cmsScrollerMenuDeleteLane;
        private ToolStripMenuItem cmsScrollerMenuDeleteAll;
    }
}
