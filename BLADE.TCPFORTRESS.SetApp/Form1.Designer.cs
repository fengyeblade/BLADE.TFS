namespace BLADE.TCPFORTRESS.SetApp
{
    partial class Form1
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.textXML = new System.Windows.Forms.TextBox();
            this.butLoadXML = new System.Windows.Forms.Button();
            this.butExpto = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.checkDebug = new System.Windows.Forms.CheckBox();
            this.label2 = new System.Windows.Forms.Label();
            this.textDBname = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.textDBstr = new System.Windows.Forms.TextBox();
            this.checkLonglock = new System.Windows.Forms.CheckBox();
            this.label4 = new System.Windows.Forms.Label();
            this.checkautoblack = new System.Windows.Forms.CheckBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.ListWhite = new System.Windows.Forms.RadioButton();
            this.listGray = new System.Windows.Forms.RadioButton();
            this.listBlack = new System.Windows.Forms.RadioButton();
            this.label7 = new System.Windows.Forms.Label();
            this.textLockTime = new System.Windows.Forms.TextBox();
            this.textCountTime = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.textCount = new System.Windows.Forms.TextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.textLogPath = new System.Windows.Forms.TextBox();
            this.textAppCurPath = new System.Windows.Forms.TextBox();
            this.label11 = new System.Windows.Forms.Label();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.butAddTun = new System.Windows.Forms.Button();
            this.label12 = new System.Windows.Forms.Label();
            this.buttoXML = new System.Windows.Forms.Button();
            this.butsaveXML = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.butStop = new System.Windows.Forms.Button();
            this.button4 = new System.Windows.Forms.Button();
            this.serviceStatus = new System.Windows.Forms.Label();
            this.serviceController1 = new System.ServiceProcess.ServiceController();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.SuspendLayout();
            // 
            // textXML
            // 
            this.textXML.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(24)))), ((int)(((byte)(36)))));
            this.textXML.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textXML.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(10)))), ((int)(((byte)(120)))), ((int)(((byte)(220)))));
            this.textXML.Location = new System.Drawing.Point(12, 56);
            this.textXML.Multiline = true;
            this.textXML.Name = "textXML";
            this.textXML.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textXML.Size = new System.Drawing.Size(412, 593);
            this.textXML.TabIndex = 1;
            this.textXML.Text = "xml file text ";
            // 
            // butLoadXML
            // 
            this.butLoadXML.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(32)))), ((int)(((byte)(64)))));
            this.butLoadXML.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.butLoadXML.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.butLoadXML.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.butLoadXML.Location = new System.Drawing.Point(12, 12);
            this.butLoadXML.Name = "butLoadXML";
            this.butLoadXML.Size = new System.Drawing.Size(134, 30);
            this.butLoadXML.TabIndex = 2;
            this.butLoadXML.Text = "Load Xml File";
            this.butLoadXML.UseVisualStyleBackColor = false;
            this.butLoadXML.Click += new System.EventHandler(this.butLoadXML_Click);
            // 
            // butExpto
            // 
            this.butExpto.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(32)))), ((int)(((byte)(64)))));
            this.butExpto.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.butExpto.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.butExpto.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(255)))), ((int)(((byte)(128)))));
            this.butExpto.Location = new System.Drawing.Point(282, 12);
            this.butExpto.Name = "butExpto";
            this.butExpto.Size = new System.Drawing.Size(142, 30);
            this.butExpto.TabIndex = 3;
            this.butExpto.Text = "Explor to >>";
            this.butExpto.UseVisualStyleBackColor = false;
            this.butExpto.Click += new System.EventHandler(this.butExpto_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label1.Location = new System.Drawing.Point(465, 60);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(61, 16);
            this.label1.TabIndex = 4;
            this.label1.Text = "Debug:";
            // 
            // checkDebug
            // 
            this.checkDebug.AutoSize = true;
            this.checkDebug.Checked = true;
            this.checkDebug.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkDebug.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(255)))), ((int)(((byte)(128)))));
            this.checkDebug.Location = new System.Drawing.Point(561, 59);
            this.checkDebug.Name = "checkDebug";
            this.checkDebug.Size = new System.Drawing.Size(74, 20);
            this.checkDebug.TabIndex = 5;
            this.checkDebug.Text = "Enable";
            this.checkDebug.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label2.Location = new System.Drawing.Point(465, 131);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(79, 16);
            this.label2.TabIndex = 6;
            this.label2.Text = "DB Name:";
            // 
            // textDBname
            // 
            this.textDBname.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(32)))), ((int)(((byte)(64)))));
            this.textDBname.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(255)))), ((int)(((byte)(128)))));
            this.textDBname.Location = new System.Drawing.Point(561, 128);
            this.textDBname.Name = "textDBname";
            this.textDBname.Size = new System.Drawing.Size(102, 26);
            this.textDBname.TabIndex = 7;
            this.textDBname.Text = "TFS";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label3.Location = new System.Drawing.Point(465, 99);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(70, 16);
            this.label3.TabIndex = 8;
            this.label3.Text = "DB Str:";
            // 
            // textDBstr
            // 
            this.textDBstr.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(32)))), ((int)(((byte)(64)))));
            this.textDBstr.Font = new System.Drawing.Font("宋体", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.textDBstr.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(255)))), ((int)(((byte)(128)))));
            this.textDBstr.Location = new System.Drawing.Point(561, 96);
            this.textDBstr.Name = "textDBstr";
            this.textDBstr.Size = new System.Drawing.Size(548, 23);
            this.textDBstr.TabIndex = 9;
            this.textDBstr.Text = "Data Source=127.0.0.1,22233; Initial Catalog=TFS;User ID=TFS;Password =pass;";
            // 
            // checkLonglock
            // 
            this.checkLonglock.AutoSize = true;
            this.checkLonglock.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(255)))), ((int)(((byte)(128)))));
            this.checkLonglock.Location = new System.Drawing.Point(797, 59);
            this.checkLonglock.Name = "checkLonglock";
            this.checkLonglock.Size = new System.Drawing.Size(98, 20);
            this.checkLonglock.TabIndex = 11;
            this.checkLonglock.Text = "Long Lock";
            this.checkLonglock.UseVisualStyleBackColor = true;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label4.Location = new System.Drawing.Point(690, 61);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(88, 16);
            this.label4.TabIndex = 10;
            this.label4.Text = "LockGray:";
            this.label4.Click += new System.EventHandler(this.label4_Click);
            // 
            // checkautoblack
            // 
            this.checkautoblack.AutoSize = true;
            this.checkautoblack.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(255)))), ((int)(((byte)(128)))));
            this.checkautoblack.Location = new System.Drawing.Point(1041, 59);
            this.checkautoblack.Name = "checkautoblack";
            this.checkautoblack.Size = new System.Drawing.Size(74, 20);
            this.checkautoblack.TabIndex = 13;
            this.checkautoblack.Text = "Enable";
            this.checkautoblack.UseVisualStyleBackColor = true;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label5.Location = new System.Drawing.Point(934, 61);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(97, 16);
            this.label5.TabIndex = 12;
            this.label5.Text = "AutoBlack:";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label6.Location = new System.Drawing.Point(768, 131);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(88, 16);
            this.label6.TabIndex = 14;
            this.label6.Text = "WOB List:";
            // 
            // ListWhite
            // 
            this.ListWhite.AutoSize = true;
            this.ListWhite.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(255)))), ((int)(((byte)(128)))));
            this.ListWhite.Location = new System.Drawing.Point(875, 131);
            this.ListWhite.Name = "ListWhite";
            this.ListWhite.Size = new System.Drawing.Size(65, 20);
            this.ListWhite.TabIndex = 15;
            this.ListWhite.Text = "White";
            this.ListWhite.UseVisualStyleBackColor = true;
            this.ListWhite.CheckedChanged += new System.EventHandler(this.ListWhite_CheckedChanged);
            // 
            // listGray
            // 
            this.listGray.AutoSize = true;
            this.listGray.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(255)))), ((int)(((byte)(128)))));
            this.listGray.Location = new System.Drawing.Point(960, 131);
            this.listGray.Name = "listGray";
            this.listGray.Size = new System.Drawing.Size(57, 20);
            this.listGray.TabIndex = 16;
            this.listGray.Text = "Gray";
            this.listGray.UseVisualStyleBackColor = true;
            this.listGray.CheckedChanged += new System.EventHandler(this.listGray_CheckedChanged);
            // 
            // listBlack
            // 
            this.listBlack.AutoSize = true;
            this.listBlack.Checked = true;
            this.listBlack.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(255)))), ((int)(((byte)(128)))));
            this.listBlack.Location = new System.Drawing.Point(1044, 131);
            this.listBlack.Name = "listBlack";
            this.listBlack.Size = new System.Drawing.Size(65, 20);
            this.listBlack.TabIndex = 17;
            this.listBlack.TabStop = true;
            this.listBlack.Text = "Black";
            this.listBlack.UseVisualStyleBackColor = true;
            this.listBlack.CheckedChanged += new System.EventHandler(this.listBlack_CheckedChanged);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label7.Location = new System.Drawing.Point(465, 168);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(115, 16);
            this.label7.TabIndex = 18;
            this.label7.Text = "Lock Second:";
            // 
            // textLockTime
            // 
            this.textLockTime.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(32)))), ((int)(((byte)(64)))));
            this.textLockTime.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(255)))), ((int)(((byte)(128)))));
            this.textLockTime.Location = new System.Drawing.Point(586, 163);
            this.textLockTime.Name = "textLockTime";
            this.textLockTime.Size = new System.Drawing.Size(77, 26);
            this.textLockTime.TabIndex = 19;
            this.textLockTime.Text = "600";
            // 
            // textCountTime
            // 
            this.textCountTime.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(32)))), ((int)(((byte)(64)))));
            this.textCountTime.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(255)))), ((int)(((byte)(128)))));
            this.textCountTime.Location = new System.Drawing.Point(837, 163);
            this.textCountTime.Name = "textCountTime";
            this.textCountTime.Size = new System.Drawing.Size(70, 26);
            this.textCountTime.TabIndex = 20;
            this.textCountTime.Text = "180";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label8.Location = new System.Drawing.Point(719, 168);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(97, 16);
            this.label8.TabIndex = 21;
            this.label8.Text = "CountTime:";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label9.Location = new System.Drawing.Point(957, 168);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(61, 16);
            this.label9.TabIndex = 22;
            this.label9.Text = "Count:";
            // 
            // textCount
            // 
            this.textCount.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(32)))), ((int)(((byte)(64)))));
            this.textCount.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(255)))), ((int)(((byte)(128)))));
            this.textCount.Location = new System.Drawing.Point(1039, 163);
            this.textCount.Name = "textCount";
            this.textCount.Size = new System.Drawing.Size(70, 26);
            this.textCount.TabIndex = 23;
            this.textCount.Text = "6";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label10.Location = new System.Drawing.Point(465, 203);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(79, 16);
            this.label10.TabIndex = 24;
            this.label10.Text = "LogPath:";
            // 
            // textLogPath
            // 
            this.textLogPath.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(32)))), ((int)(((byte)(64)))));
            this.textLogPath.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(255)))), ((int)(((byte)(128)))));
            this.textLogPath.Location = new System.Drawing.Point(561, 200);
            this.textLogPath.Name = "textLogPath";
            this.textLogPath.Size = new System.Drawing.Size(102, 26);
            this.textLogPath.TabIndex = 25;
            this.textLogPath.Text = "\\logs";
            // 
            // textAppCurPath
            // 
            this.textAppCurPath.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(32)))), ((int)(((byte)(64)))));
            this.textAppCurPath.ForeColor = System.Drawing.Color.Blue;
            this.textAppCurPath.Location = new System.Drawing.Point(837, 200);
            this.textAppCurPath.Name = "textAppCurPath";
            this.textAppCurPath.ReadOnly = true;
            this.textAppCurPath.Size = new System.Drawing.Size(272, 26);
            this.textAppCurPath.TabIndex = 26;
            this.textAppCurPath.Text = "TFS";
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label11.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(90)))), ((int)(((byte)(110)))), ((int)(((byte)(100)))));
            this.label11.Location = new System.Drawing.Point(719, 203);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(106, 16);
            this.label11.TabIndex = 27;
            this.label11.Text = "AppCurPath:";
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.AutoScroll = true;
            this.flowLayoutPanel1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowLayoutPanel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(468, 299);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(641, 350);
            this.flowLayoutPanel1.TabIndex = 28;
            // 
            // butAddTun
            // 
            this.butAddTun.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(32)))), ((int)(((byte)(64)))));
            this.butAddTun.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.butAddTun.Font = new System.Drawing.Font("宋体", 11F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.butAddTun.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(0)))));
            this.butAddTun.Location = new System.Drawing.Point(1011, 263);
            this.butAddTun.Name = "butAddTun";
            this.butAddTun.Size = new System.Drawing.Size(98, 30);
            this.butAddTun.TabIndex = 29;
            this.butAddTun.Text = "Add Tun";
            this.butAddTun.UseVisualStyleBackColor = false;
            this.butAddTun.Click += new System.EventHandler(this.button3_Click);
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label12.Location = new System.Drawing.Point(465, 270);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(88, 16);
            this.label12.TabIndex = 30;
            this.label12.Text = "Tun List:";
            // 
            // buttoXML
            // 
            this.buttoXML.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(32)))), ((int)(((byte)(64)))));
            this.buttoXML.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.buttoXML.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.buttoXML.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(255)))), ((int)(((byte)(128)))));
            this.buttoXML.Location = new System.Drawing.Point(468, 12);
            this.buttoXML.Name = "buttoXML";
            this.buttoXML.Size = new System.Drawing.Size(142, 30);
            this.buttoXML.TabIndex = 31;
            this.buttoXML.Text = "<< To XML";
            this.buttoXML.UseVisualStyleBackColor = false;
            this.buttoXML.Click += new System.EventHandler(this.buttoXML_Click);
            // 
            // butsaveXML
            // 
            this.butsaveXML.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(32)))), ((int)(((byte)(64)))));
            this.butsaveXML.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.butsaveXML.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.butsaveXML.ForeColor = System.Drawing.Color.Red;
            this.butsaveXML.Location = new System.Drawing.Point(12, 655);
            this.butsaveXML.Name = "butsaveXML";
            this.butsaveXML.Size = new System.Drawing.Size(134, 30);
            this.butsaveXML.TabIndex = 32;
            this.butsaveXML.Text = "Save Xml File";
            this.butsaveXML.UseVisualStyleBackColor = false;
            this.butsaveXML.Click += new System.EventHandler(this.butsaveXML_Click);
            // 
            // button2
            // 
            this.button2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(32)))), ((int)(((byte)(64)))));
            this.button2.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.button2.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.button2.ForeColor = System.Drawing.Color.GreenYellow;
            this.button2.Location = new System.Drawing.Point(975, 662);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(134, 30);
            this.button2.TabIndex = 33;
            this.button2.Text = "Edit List";
            this.button2.UseVisualStyleBackColor = false;
            this.button2.Visible = false;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // butStop
            // 
            this.butStop.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(32)))), ((int)(((byte)(64)))));
            this.butStop.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.butStop.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.butStop.ForeColor = System.Drawing.Color.LightSeaGreen;
            this.butStop.Location = new System.Drawing.Point(640, 662);
            this.butStop.Name = "butStop";
            this.butStop.Size = new System.Drawing.Size(76, 30);
            this.butStop.TabIndex = 34;
            this.butStop.Text = "Stop";
            this.butStop.UseVisualStyleBackColor = false;
            this.butStop.Click += new System.EventHandler(this.butStop_Click);
            // 
            // button4
            // 
            this.button4.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(32)))), ((int)(((byte)(64)))));
            this.button4.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.button4.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.button4.ForeColor = System.Drawing.Color.Lime;
            this.button4.Location = new System.Drawing.Point(722, 662);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(113, 30);
            this.button4.TabIndex = 35;
            this.button4.Text = "ReStart";
            this.button4.UseVisualStyleBackColor = false;
            this.button4.Click += new System.EventHandler(this.button4_Click);
            // 
            // serviceStatus
            // 
            this.serviceStatus.AutoSize = true;
            this.serviceStatus.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.serviceStatus.Location = new System.Drawing.Point(465, 669);
            this.serviceStatus.Name = "serviceStatus";
            this.serviceStatus.Size = new System.Drawing.Size(142, 16);
            this.serviceStatus.TabIndex = 36;
            this.serviceStatus.Text = "Service Running";
            // 
            // serviceController1
            // 
            this.serviceController1.ServiceName = "TFService";
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            this.openFileDialog1.Filter = "(*.cfg)|*.cfg|(*.xml)|*.xml";
            this.openFileDialog1.Title = "Open Settings";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(20)))), ((int)(((byte)(30)))));
            this.ClientSize = new System.Drawing.Size(1136, 708);
            this.Controls.Add(this.serviceStatus);
            this.Controls.Add(this.button4);
            this.Controls.Add(this.butStop);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.butsaveXML);
            this.Controls.Add(this.buttoXML);
            this.Controls.Add(this.label12);
            this.Controls.Add(this.butAddTun);
            this.Controls.Add(this.flowLayoutPanel1);
            this.Controls.Add(this.label11);
            this.Controls.Add(this.textAppCurPath);
            this.Controls.Add(this.textLogPath);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.textCount);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.textCountTime);
            this.Controls.Add(this.textLockTime);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.listBlack);
            this.Controls.Add(this.listGray);
            this.Controls.Add(this.ListWhite);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.checkautoblack);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.checkLonglock);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.textDBstr);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.textDBname);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.checkDebug);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.butExpto);
            this.Controls.Add(this.butLoadXML);
            this.Controls.Add(this.textXML);
            this.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MaximizeBox = false;
            this.Name = "Form1";
            this.Text = "TUN FORTRESS SETUP";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textXML;
        private System.Windows.Forms.Button butLoadXML;
        private System.Windows.Forms.Button butExpto;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.CheckBox checkDebug;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textDBname;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox textDBstr;
        private System.Windows.Forms.CheckBox checkLonglock;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.CheckBox checkautoblack;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.RadioButton ListWhite;
        private System.Windows.Forms.RadioButton listGray;
        private System.Windows.Forms.RadioButton listBlack;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox textLockTime;
        private System.Windows.Forms.TextBox textCountTime;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.TextBox textCount;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.TextBox textLogPath;
        private System.Windows.Forms.TextBox textAppCurPath;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.Button butAddTun;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Button buttoXML;
        private System.Windows.Forms.Button butsaveXML;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button butStop;
        private System.Windows.Forms.Button button4;
        private System.Windows.Forms.Label serviceStatus;
        private System.ServiceProcess.ServiceController serviceController1;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
    }
}

