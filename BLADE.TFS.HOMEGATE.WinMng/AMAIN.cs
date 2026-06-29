using BLADE.TOOLS.NET;
using BLADE.TOOLS.NET.IPGATE_SqlBase;
using System.ComponentModel;

namespace BLADE.TFS.HOMEGATE.WinMng
{
    public partial class AMAIN : Form
    {

        protected string logtext = string.Empty;
        private Lock _lll = new Lock();
        public void AddLog(string text)
        {
            lock (_lll)
            {
                logtext = BLADE.TimeProvider.LocalNow.ToString("HH:mm:ss") + " 【" + text + "】" + Environment.NewLine + logtext;
                if (logtext.Length > 1100) { logtext = logtext.Substring(0, 900) + "..."; }
            }
        }
        private System.Threading.Timer? _timer;
        public AMAIN()
        {
            InitializeComponent();
            WorkClass.MainForm = this;
        }

        private void AMAIN_Load(object sender, EventArgs e)
        {
            _timer = new(timeCall, null, 700, 700);
            richTextBox1.Text = "";
            logtext = "App Started.";
            // dataGridView1.Get_DG_Extend().WBG = CurWBG.White;
            //dataGridView1.Get_DG_Extend().IPFM = CurIPFM.NONE;
            //dataGridView1.Get_DG_Extend().Loaded = false;
            textBox2.Text = AppCenter.DBTYPE.Trim();
            textBox1.Text = AppCenter.DBCONNSTR.Trim();
            listBox1.Items.Clear();
        }

        protected void timeCall(object? state)
        {
            if (this.IsHandleCreated)
            {
                this.Invoke(new Action(() =>
                {
                    lock (_lll)
                    {
                        richTextBox1.Text = logtext;
                        label1.Text = BLADE.TimeProvider.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                        label2.Text = BLADE.TimeProvider.LocalNow.ToString("yyyy-MM-dd HH:mm:ss");
                    }
                }));
            }
        }

        private void AMAIN_FormClosing(object sender, FormClosingEventArgs e)
        {
            _timer?.Dispose();
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void textBox2_Leave(object sender, EventArgs e)
        {
            textBox2.Text = textBox2.Text.Trim().ToUpper();
            if (textBox2.Text != "SQLSERVER" && textBox2.Text != "MYSQL" && textBox2.Text != "POSTGRESQL")
            {
                MessageBox.Show("DBTYPE must be SQLSERVER, MYSQL, or POSTGRESQL.");
                textBox2.Text = AppCenter.DBTYPE.Trim();
                return;
            }
            AppCenter.DBTYPE = textBox2.Text;
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            var j = await WorkClass.OpenIDB(textBox2.Text, textBox1.Text, true);
            if (j.suc)
            {
                textBox3.Text = j.msg;
                if (j.msg == "Not inited.")
                {
                    var dr = MessageBox.Show("DB not inited. Do you want to build Tables ?", "Bulie Tab ?", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (dr == DialogResult.Yes && AppCenter.IDB != null)
                    {
                        var jj = await AppCenter.IDB.InitDatabase();
                        if (jj.StartsWith("OK"))
                        {
                            MessageBox.Show(jj, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            textBox3.Text = "DB " + jj;
                        }
                        else
                        {
                            MessageBox.Show("Failed to initialize database: " + jj, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
                AddLog("Open DB " + j.suc + "  " + j.msg);
            }
            else
            {
                MessageBox.Show(j.msg, "Not Open DB.");
                AppCenter.IDB = null;
                textBox3.Text = "DB Not Ready";
                AddLog("Open DB " + j.suc + ", Now Interface_IPGATE_DB is null");
            }
        }
        private bool dbisok()
        {
            if (AppCenter.IDB == null)
            {
                MessageBox.Show("DB Not Ready. Please open DB first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); return false;
            }
            return true;
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            if (dbisok())
            {
                AppCenter.UpdateWorkList(NameListType.White, CurIPFM.IPV4, null);
                var ls = await AppCenter.IDB.Get_WBG_V4List(wbgtype: 10, maxrow: 0);
                if (ls.Count < 1)
                {
                    MessageBox.Show("Load IPV4 WhiteList failed. No data found.");
                }
                else
                {
                    var j = WorkClass.FormatAndSortList(ls);
                    AddLog(j.info);
                    AppCenter.UpdateWorkList(NameListType.White, CurIPFM.IPV4, j.lst);
                }
            }
        }

        private void dataGridView1_RowPrePaint(object sender, DataGridViewRowPrePaintEventArgs e)
        {
            // 跳过表头行
            if (e.RowIndex < 0 || e.RowIndex >= dataGridView1.Rows.Count)
                return;

            // 获取当前行绑定的实体对象
            if (dataGridView1.Rows[e.RowIndex].DataBoundItem is WBGshow item)
            {
                var rowStyle = dataGridView1.Rows[e.RowIndex].DefaultCellStyle;

                // 原始 backcolor (0,240,250) 问题 backcolor(redlevel,240,250-redlevel)
                // 原始 选中背景 (240,150,210)  
                rowStyle.BackColor = Color.FromArgb(item.RedLevel, 240, 250 - item.RedLevel);
                rowStyle.SelectionBackColor = Color.FromArgb(240, 150, 210 - item.RedLevel);

            }
        }

        private async void button3_Click(object sender, EventArgs e)
        {
            if (dbisok())
            {
                AppCenter.UpdateWorkList(NameListType.White, CurIPFM.IPV6, null);
                var ls = await AppCenter.IDB.Get_WBG_V6List(wbgtype: 10, maxrow: 0);
                if (ls.Count < 1)
                {
                    MessageBox.Show("Load IPV6 WhiteList failed. No data found.");
                }
                else
                {
                    var j = WorkClass.FormatAndSortList(ls);
                    AddLog(j.info);
                    AppCenter.UpdateWorkList(NameListType.White, CurIPFM.IPV6, j.lst);
                }
            }
        }
    }
}
