namespace BLADE.TFS.HOMEGATE.WINAPP
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
            textBox1 = new TextBox();
            label1 = new Label();
            button1 = new Button();
            button2 = new Button();
            SuspendLayout();
            // 
            // textBox1
            // 
            textBox1.ForeColor = Color.Green;
            textBox1.Location = new Point(12, 124);
            textBox1.Multiline = true;
            textBox1.Name = "textBox1";
            textBox1.ScrollBars = ScrollBars.Vertical;
            textBox1.Size = new Size(701, 342);
            textBox1.TabIndex = 0;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold);
            label1.Location = new Point(12, 9);
            label1.Name = "label1";
            label1.Size = new Size(212, 26);
            label1.TabIndex = 1;
            label1.Text = "2026-05-05 12:33:30";
            // 
            // button1
            // 
            button1.FlatStyle = FlatStyle.Flat;
            button1.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
            button1.ForeColor = Color.FromArgb(0, 0, 192);
            button1.Location = new Point(12, 62);
            button1.Name = "button1";
            button1.Size = new Size(176, 39);
            button1.TabIndex = 2;
            button1.Text = "启动 Core";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // button2
            // 
            button2.Enabled = false;
            button2.FlatStyle = FlatStyle.Flat;
            button2.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
            button2.ForeColor = Color.FromArgb(0, 0, 192);
            button2.Location = new Point(236, 62);
            button2.Name = "button2";
            button2.Size = new Size(176, 39);
            button2.TabIndex = 3;
            button2.Text = "停止 Core";
            button2.UseVisualStyleBackColor = true;
            button2.Click += button2_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(9F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(725, 478);
            Controls.Add(button2);
            Controls.Add(button1);
            Controls.Add(label1);
            Controls.Add(textBox1);
            Name = "Form1";
            Text = "Form1";
            Load += Form1_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TextBox textBox1;
        private Label label1;
        private Button button1;
        private Button button2;
    }
}
