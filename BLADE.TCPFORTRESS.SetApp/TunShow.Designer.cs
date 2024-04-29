namespace BLADE.TCPFORTRESS.SetApp
{
    partial class TunShow
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

        #region 组件设计器生成的代码

        /// <summary> 
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.label1 = new System.Windows.Forms.Label();
            this.textName = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.textMTU = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.checkUserule = new System.Windows.Forms.CheckBox();
            this.label4 = new System.Windows.Forms.Label();
            this.textInAddr = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.textInport = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.textOutAddr = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.textOutport = new System.Windows.Forms.TextBox();
            this.butdelete = new System.Windows.Forms.Button();
            this.label8 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.textSpeed = new System.Windows.Forms.TextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.textLockCount = new System.Windows.Forms.TextBox();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.label11 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("宋体", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label1.Location = new System.Drawing.Point(44, 46);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(71, 14);
            this.label1.TabIndex = 0;
            this.label1.Text = "TunName:";
            // 
            // textName
            // 
            this.textName.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.textName.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(192)))), ((int)(((byte)(0)))));
            this.textName.Location = new System.Drawing.Point(160, 43);
            this.textName.Name = "textName";
            this.textName.Size = new System.Drawing.Size(120, 21);
            this.textName.TabIndex = 1;
            this.textName.Text = "Tun Name";
            this.textName.TextChanged += new System.EventHandler(this.textName_TextChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("宋体", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label2.Location = new System.Drawing.Point(410, 46);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(79, 14);
            this.label2.TabIndex = 2;
            this.label2.Text = "MTU size:";
            // 
            // textMTU
            // 
            this.textMTU.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.textMTU.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(192)))), ((int)(((byte)(0)))));
            this.textMTU.Location = new System.Drawing.Point(501, 43);
            this.textMTU.Name = "textMTU";
            this.textMTU.Size = new System.Drawing.Size(57, 21);
            this.textMTU.TabIndex = 3;
            this.textMTU.Text = "1400";
            this.textMTU.TextChanged += new System.EventHandler(this.textMTU_TextChanged);
            this.textMTU.Leave += new System.EventHandler(this.textMTU_Leave);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("宋体", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label3.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.label3.Location = new System.Drawing.Point(105, 13);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(71, 14);
            this.label3.TabIndex = 4;
            this.label3.Text = "UseRule:";
            // 
            // checkUserule
            // 
            this.checkUserule.AutoSize = true;
            this.checkUserule.Checked = true;
            this.checkUserule.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkUserule.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.checkUserule.Location = new System.Drawing.Point(190, 12);
            this.checkUserule.Name = "checkUserule";
            this.checkUserule.Size = new System.Drawing.Size(90, 16);
            this.checkUserule.TabIndex = 5;
            this.checkUserule.Text = "Enable Rule";
            this.checkUserule.UseVisualStyleBackColor = true;
            this.checkUserule.CheckedChanged += new System.EventHandler(this.checkUserule_CheckedChanged);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("宋体", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label4.Location = new System.Drawing.Point(44, 76);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(95, 14);
            this.label4.TabIndex = 6;
            this.label4.Text = "In Address:";
            // 
            // textInAddr
            // 
            this.textInAddr.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.textInAddr.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(192)))), ((int)(((byte)(0)))));
            this.textInAddr.Location = new System.Drawing.Point(160, 73);
            this.textInAddr.Name = "textInAddr";
            this.textInAddr.Size = new System.Drawing.Size(120, 21);
            this.textInAddr.TabIndex = 7;
            this.textInAddr.Text = "0.0.0.0";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("宋体", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label5.Location = new System.Drawing.Point(410, 76);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(71, 14);
            this.label5.TabIndex = 8;
            this.label5.Text = "In Port:";
            // 
            // textInport
            // 
            this.textInport.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.textInport.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(192)))), ((int)(((byte)(0)))));
            this.textInport.Location = new System.Drawing.Point(501, 73);
            this.textInport.Name = "textInport";
            this.textInport.Size = new System.Drawing.Size(57, 21);
            this.textInport.TabIndex = 9;
            this.textInport.Text = "2000";
            this.textInport.TextChanged += new System.EventHandler(this.textInport_TextChanged);
            this.textInport.Leave += new System.EventHandler(this.textInport_Leave);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("宋体", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label6.Location = new System.Drawing.Point(44, 104);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(103, 14);
            this.label6.TabIndex = 10;
            this.label6.Text = "Out Address:";
            // 
            // textOutAddr
            // 
            this.textOutAddr.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.textOutAddr.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(192)))), ((int)(((byte)(0)))));
            this.textOutAddr.Location = new System.Drawing.Point(160, 101);
            this.textOutAddr.Name = "textOutAddr";
            this.textOutAddr.Size = new System.Drawing.Size(120, 21);
            this.textOutAddr.TabIndex = 11;
            this.textOutAddr.Text = "110.110.119.201";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Font = new System.Drawing.Font("宋体", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label7.Location = new System.Drawing.Point(410, 104);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(79, 14);
            this.label7.TabIndex = 12;
            this.label7.Text = "Out Port:";
            // 
            // textOutport
            // 
            this.textOutport.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.textOutport.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(192)))), ((int)(((byte)(0)))));
            this.textOutport.Location = new System.Drawing.Point(501, 101);
            this.textOutport.Name = "textOutport";
            this.textOutport.Size = new System.Drawing.Size(57, 21);
            this.textOutport.TabIndex = 13;
            this.textOutport.Text = "2222";
            this.textOutport.TextChanged += new System.EventHandler(this.textOutport_TextChanged);
            this.textOutport.Leave += new System.EventHandler(this.textOutport_Leave);
            // 
            // butdelete
            // 
            this.butdelete.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(16)))), ((int)(((byte)(32)))), ((int)(((byte)(64)))));
            this.butdelete.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.butdelete.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(64)))), ((int)(((byte)(0)))));
            this.butdelete.Location = new System.Drawing.Point(413, 9);
            this.butdelete.Name = "butdelete";
            this.butdelete.Size = new System.Drawing.Size(145, 23);
            this.butdelete.TabIndex = 15;
            this.butdelete.Text = "Delete This";
            this.butdelete.UseVisualStyleBackColor = false;
            this.butdelete.Click += new System.EventHandler(this.butdelete_Click);
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label8.ForeColor = System.Drawing.Color.Red;
            this.label8.Location = new System.Drawing.Point(44, 12);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(43, 16);
            this.label8.TabIndex = 16;
            this.label8.Text = "1254";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Font = new System.Drawing.Font("宋体", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label9.Location = new System.Drawing.Point(410, 131);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(79, 14);
            this.label9.TabIndex = 17;
            this.label9.Text = "Speed KB:";
            // 
            // textSpeed
            // 
            this.textSpeed.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.textSpeed.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(192)))), ((int)(((byte)(0)))));
            this.textSpeed.Location = new System.Drawing.Point(501, 128);
            this.textSpeed.Name = "textSpeed";
            this.textSpeed.Size = new System.Drawing.Size(57, 21);
            this.textSpeed.TabIndex = 18;
            this.textSpeed.Text = "1024";
            this.textSpeed.TextChanged += new System.EventHandler(this.textSpeed_TextChanged);
            this.textSpeed.Leave += new System.EventHandler(this.textSpeed_Leave);
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Font = new System.Drawing.Font("宋体", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label10.Location = new System.Drawing.Point(411, 159);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(87, 14);
            this.label10.TabIndex = 19;
            this.label10.Text = "LockCount:";
            // 
            // textLockCount
            // 
            this.textLockCount.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.textLockCount.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(192)))), ((int)(((byte)(0)))));
            this.textLockCount.Location = new System.Drawing.Point(500, 156);
            this.textLockCount.Name = "textLockCount";
            this.textLockCount.Size = new System.Drawing.Size(58, 21);
            this.textLockCount.TabIndex = 20;
            this.textLockCount.Text = "9";
            this.textLockCount.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.textLockCount.Leave += new System.EventHandler(this.textLockCount_Leave);
            // 
            // textBox1
            // 
            this.textBox1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.textBox1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(192)))), ((int)(((byte)(0)))));
            this.textBox1.Location = new System.Drawing.Point(160, 156);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(120, 21);
            this.textBox1.TabIndex = 22;
            this.textBox1.Leave += new System.EventHandler(this.textBox1_Leave);
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Font = new System.Drawing.Font("宋体", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label11.Location = new System.Drawing.Point(44, 159);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(55, 14);
            this.label11.TabIndex = 21;
            this.label11.Text = "DName:";
            // 
            // TunShow
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(8)))), ((int)(((byte)(32)))), ((int)(((byte)(64)))));
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.label11);
            this.Controls.Add(this.textLockCount);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.textSpeed);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.butdelete);
            this.Controls.Add(this.textOutport);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.textOutAddr);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.textInport);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.textInAddr);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.checkUserule);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.textMTU);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.textName);
            this.Controls.Add(this.label1);
            this.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(255)))), ((int)(((byte)(128)))));
            this.Name = "TunShow";
            this.Size = new System.Drawing.Size(610, 200);
            this.Load += new System.EventHandler(this.TunShow_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        
        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textName;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textMTU;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.CheckBox checkUserule;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox textInAddr;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox textInport;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox textOutAddr;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox textOutport;
        private System.Windows.Forms.Button butdelete;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.TextBox textSpeed;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.TextBox textLockCount;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Label label11;
    }
}
