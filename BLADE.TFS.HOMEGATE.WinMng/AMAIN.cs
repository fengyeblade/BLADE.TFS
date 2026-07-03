using BLADE.TOOLS.NET;
using BLADE.TOOLS.NET.HighConcurrency;
using BLADE.TOOLS.NET.IPGATE_SqlBase;
using Microsoft.Win32.SafeHandles;
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
                            AppCenter.DBOPENED = true;
                        }
                        else
                        {
                            MessageBox.Show("Failed to initialize database: " + jj, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            AppCenter.DBOPENED = false;

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
                AppCenter.DBOPENED = false;
            }
        }
        private bool dbisok()
        {
            if (AppCenter.IDB == null || !AppCenter.DBOPENED)
            {
                MessageBox.Show("DB Not Ready. Please open DB first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            return true;
        }

        /// <summary>
        /// 加载 white ipv4
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void button2_Click(object sender, EventArgs e)
        {
            await load_white_ipv4();
        }
        private async ValueTask load_white_ipv4()
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
        private async ValueTask load_GRAY_ipv4()
        {
            if (dbisok())
            {
                AppCenter.UpdateWorkList(NameListType.Gray, CurIPFM.IPV4, null);
                var ls = await AppCenter.IDB.Get_WBG_V4List(wbgtype: 30, maxrow: 0);
                if (ls.Count < 1)
                {
                    MessageBox.Show("Load IPV4 GrayList failed. No data found.");
                }
                else
                {
                    var j = WorkClass.FormatAndSortList(ls);
                    AddLog(j.info);
                    AppCenter.UpdateWorkList(NameListType.Gray, CurIPFM.IPV4, j.lst);
                }
            }
        }
        private async ValueTask load_GRAY_ipv6()
        {
            if (dbisok())
            {
                AppCenter.UpdateWorkList(NameListType.Gray, CurIPFM.IPV6, null);
                var ls = await AppCenter.IDB.Get_WBG_V6List(wbgtype: 30, maxrow: 0);
                if (ls.Count < 1)
                {
                    MessageBox.Show("Load IPV6 GrayList failed. No data found.");
                }
                else
                {
                    var j = WorkClass.FormatAndSortList(ls);
                    AddLog(j.info);
                    AppCenter.UpdateWorkList(NameListType.Gray, CurIPFM.IPV6, j.lst);
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

        /// <summary>
        /// 加载 white ipv6
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void button3_Click(object sender, EventArgs e)
        {
            await load_white_ipv6();
        }
        private async ValueTask load_white_ipv6()
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
        /// <summary>
        /// 加载 domain
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void button11_Click(object sender, EventArgs e)
        {
            //load domain
            await load_doms();
        }
        private async ValueTask load_doms()
        {
            if (dbisok())
            {
                AppCenter.UpdateWorkList(NameListType.ALL, CurIPFM.DOM, null);
                var ls = await AppCenter.IDB.Get_WBG_DOMList(maxrow: 0);
                if (ls.Count < 1)
                {
                    MessageBox.Show("Load DomainList failed. No data found.");
                }
                else
                {
                    var j = WorkClass.FormatAndSortList(ls);
                    AddLog(j.info);
                    AppCenter.UpdateWorkList(NameListType.ALL, CurIPFM.DOM, j.lst);
                }
            }
        }

        /// <summary>
        /// 加载 black ipv6
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void button8_Click(object sender, EventArgs e)
        {
            //load black ipv6
            await load_black_ipv6();
        }
        public async ValueTask load_black_ipv6()
        {
            if (dbisok())
            {
                AppCenter.UpdateWorkList(NameListType.Black, CurIPFM.IPV6, null);
                var ls = await AppCenter.IDB.Get_WBG_V6List(wbgtype: 20, maxrow: 0);
                if (ls.Count < 1)
                {
                    MessageBox.Show("Load IPV6 BlackList failed. No data found.");
                }
                else
                {
                    var j = WorkClass.FormatAndSortList(ls);
                    AddLog(j.info);
                    AppCenter.UpdateWorkList(NameListType.Black, CurIPFM.IPV6, j.lst);
                }
            }
        }
        public async ValueTask load_black_ipv4()
        {
            if (dbisok())
            {
                AppCenter.UpdateWorkList(NameListType.Black, CurIPFM.IPV4, null);
                var ls = await AppCenter.IDB.Get_WBG_V4List(wbgtype: 20, maxrow: 0);
                if (ls.Count < 1)
                {
                    MessageBox.Show("Load IPV4 BlackList failed. No data found.");
                }
                else
                {
                    var j = WorkClass.FormatAndSortList(ls);
                    AddLog(j.info);
                    AppCenter.UpdateWorkList(NameListType.Black, CurIPFM.IPV4, j.lst);
                }
            }
        }

        /// <summary>
        /// 加载 black ipv4
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void button10_Click(object sender, EventArgs e)
        {
            //load black ipv4
            await load_black_ipv4();
        }

        private async void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            var srs = dataGridView1.SelectedRows;
            if (srs == null || srs.Count < 1)
            {
                AppCenter.UpdateSELECTED(new List<selectedItem>());
            }
            else
            {
                List<selectedItem> sri = new List<selectedItem>();
                foreach (DataGridViewRow s in srs)
                {
                    if (s.DataBoundItem is WBGshow item)
                    {
                        sri.Add(new selectedItem(item.TID, item.IPADDRESS, 0));
                    }
                }
                AppCenter.UpdateSELECTED(sri);
            }
        }

        private void button9_Click(object sender, EventArgs e)
        {
            dataGridView1.ClearSelection();
            AppCenter.UpdateSELECTED(new List<selectedItem>());
            AddLog("Clear Selection");
        }

        private async void button4_Click(object sender, EventArgs e)
        {
            var srs = dataGridView1.SelectedRows;
            if (srs == null || srs.Count < 1)
            {
            }
            else
            {
                List<selectedItem> sri = new List<selectedItem>();
                string tids = "";
                List<long> lids = new List<long>();
                foreach (DataGridViewRow s in srs)
                {
                    if (s.DataBoundItem is WBGshow item)
                    {
                        sri.Add(new selectedItem(item.TID, item.IPADDRESS, 0));
                        tids += item.TID + ",";
                        lids.Add(item.TID);
                    }

                }

                DialogResult dy = MessageBox.Show("Confim to DELETE " + AppCenter.CWBG.ToString() + "." + AppCenter.CIPFM.ToString() + "\r\n" + tids + "\r\n  YES or NO?", "Confim Delete?", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (dy == DialogResult.Yes)
                {
                    await deleteSelects(lids);
                }
                else
                {
                    AddLog("Drop Delete ...");
                }
            }
        }

        public async ValueTask reloadTable()
        {
            if (AppCenter.CIPFM == CurIPFM.IPV4)
            {
                if (AppCenter.CWBG == NameListType.Black) { await load_black_ipv4(); }
                else if (AppCenter.CWBG == NameListType.White) { await load_white_ipv4(); }
                else if (AppCenter.CWBG == NameListType.Gray) { await load_GRAY_ipv4(); }
            }
            else if (AppCenter.CIPFM == CurIPFM.IPV6)
            {
                if (AppCenter.CWBG == NameListType.Black) { await load_black_ipv6(); }
                else if (AppCenter.CWBG == NameListType.White) { await load_white_ipv6(); }
                else if (AppCenter.CWBG == NameListType.Gray) { await load_GRAY_ipv6(); }
            }
            else if (AppCenter.CIPFM == CurIPFM.DOM)
            {
                await load_doms();
            }
        }
        private async ValueTask deleteSelects(List<long> lids)
        {
            try
            {
                var result = await WorkClass.DeleteSelectItems(AppCenter.CIPFM, lids.ToArray());
                if (result.suc)
                {
                    AddLog(result.msg);
                    await reloadTable();
                    AddLog("Delete completed ! " + AppCenter.CWBG + " " + result.msg);
                }
                else
                {
                    AddLog("Delete failed: " + result.msg);
                }
            }
            catch (Exception z)
            {
                AddLog("deleteSelects Error : " + z.Message);
            }
        }
        public void ClearPreFixList()
        {
            listBox1.BeginUpdate();
            listBox1.Items.Clear();
            listBox1.EndUpdate();
        }
        private async void button5_Click(object sender, EventArgs e)
        {
            if (AppCenter.SELECTED.Count > 0 && (AppCenter.CIPFM == CurIPFM.IPV4 || AppCenter.CIPFM == CurIPFM.IPV6))
            {
                var jj = AppCenter.ComSelectedPreFix();
                listBox1.BeginUpdate();
                listBox1.Items.Clear();
                foreach ((string pfbody, byte mk) j in jj)
                {
                    listBox1.Items.Add(j.pfbody + "/" + j.mk);
                }
                listBox1.EndUpdate();
                if (listBox1.Items.Count > 0)
                {
                    listBox1.SelectedIndex = 0;
                }
            }
            else
            {
                MessageBox.Show("Not Selected IpAddress.", "Alert", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            }
        }

        private async void button14_Click(object sender, EventArgs e)
        {
            await load_GRAY_ipv4();
        }

        private async void button15_Click(object sender, EventArgs e)
        {
            await load_GRAY_ipv6();
        }

        private async void button6_Click(object sender, EventArgs e)
        {
            if (listBox1.Items.Count > 0 && listBox1.SelectedIndex >= 0)
            {
                try
                {
                    string a = (string)listBox1.SelectedItem;
                    var j = IPTools.Expand(a);
                    if (DialogResult.Yes == MessageBox.Show("You want Make a new [WHITE.IPv" + (j.bin.Length == 5 ? "4" : "6") + "] item ( " + j.fullstr + " ) ?"
                        + "\r\nYour current selected address are from " + AppCenter.CWBG + "." + AppCenter.CIPFM + " DataTable."
                        + "\r\nPLEASE Confirm that is CORRECT,and that are your wanted."
                        , "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question))
                    {
                        bool isd = false;
                        if (j.bin.Length == 4)
                        {
                            var w = WorkClass.MakeWBGV4("App Extraced", j.fullstr, j.bin[0], j.pflen, textBox7.Text.Trim(), NameListType.White);
                            isd = await AppCenter.InsertIPV4(w);
                        }
                        else
                        {
                            var w = WorkClass.MakeWBGV6("App Extraced", j.fullstr, BLADE.TOOLS.NET.IPTools.GetIPV6_HeadString(8, j.bin), j.pflen, textBox7.Text.Trim(), NameListType.White);
                            isd = await AppCenter.InsertIPV6(w);
                        }
                        if (isd)
                        {
                            AddLog("Make new ListItem [" + AppCenter.CWBG + "." + AppCenter.CIPFM + "]  ( " + j.fullstr + " )  Done !");
                            if (DialogResult.OK == MessageBox.Show("The rule has been added. Would you like to continue clearing the selected original records? "
                                + "\r\n(This rule is summarized and extracted based on the selected records.)", "Caution Alert", MessageBoxButtons.OKCancel, MessageBoxIcon.Question))
                            {
                                List<long> ids = new List<long>();
                                foreach (selectedItem s in AppCenter.SELECTED)
                                {
                                    ids.Add(s.ID);
                                }
                                if (ids.Count > 0)
                                {
                                    await deleteSelects(ids);
                                }
                            }
                            await reloadTable();
                        }
                        else
                        {
                            AddLog("Insert failed. DataBase is Opened ? ");
                        }
                    }

                }
                catch { }
            }
            else
            {
                MessageBox.Show("You need compute and selected One PreFix item, then click save button.", "Caution", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            }
        }

        private async void button7_Click(object sender, EventArgs e)
        {
            if (listBox1.Items.Count > 0 && listBox1.SelectedIndex >= 0)
            {
                try
                {
                    string a = (string)listBox1.SelectedItem;
                    var j = IPTools.Expand(a);
                    if (DialogResult.Yes == MessageBox.Show("You want Make a new [BLACK.IPv" + (j.bin.Length == 5 ? "4" : "6") + "] item ( " + j.fullstr + " ) ?"
                        + "\r\nYour current selected address are from " + AppCenter.CWBG + "." + AppCenter.CIPFM + " DataTable."
                        + "\r\nPLEASE Confirm that is CORRECT,and that are your wanted."
                        , "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question))
                    {
                        bool isd = false;
                        if (j.bin.Length == 4)
                        {
                            var w = WorkClass.MakeWBGV4("App Extraced", j.fullstr, j.bin[0], j.pflen, textBox7.Text.Trim(), NameListType.Black);
                            isd = await AppCenter.InsertIPV4(w);
                        }
                        else
                        {
                            var w = WorkClass.MakeWBGV6("App Extraced", j.fullstr, BLADE.TOOLS.NET.IPTools.GetIPV6_HeadString(8, j.bin), j.pflen, textBox7.Text.Trim(), NameListType.Black);
                            isd = await AppCenter.InsertIPV6(w);
                        }
                        if (isd)
                        {
                            AddLog("Make new ListItem [" + AppCenter.CWBG + "." + AppCenter.CIPFM + "]  ( " + j.fullstr + " )  Done !");
                            if (DialogResult.OK == MessageBox.Show("The rule has been added. Would you like to continue clearing the selected original records? "
                                + "\r\n(This rule is summarized and extracted based on the selected records.)", "Caution Alert", MessageBoxButtons.OKCancel, MessageBoxIcon.Question))
                            {
                                List<long> ids = new List<long>();
                                foreach (selectedItem s in AppCenter.SELECTED)
                                {
                                    ids.Add(s.ID);
                                }
                                if (ids.Count > 0)
                                {
                                    await deleteSelects(ids);
                                }
                            }
                            await reloadTable();
                        }
                        else
                        {
                            AddLog("Insert failed. DataBase is Opened ? ");
                        }
                    }

                }
                catch { }
            }
            else
            {
                MessageBox.Show("You need compute and selected One PreFix item, then click save button.", "Caution", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            }
        }

        private void dataGridView1_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            if (dataGridView1.InvokeRequired)
            {
                dataGridView1.Invoke(new Action(() =>
                {
                    if (dataGridView1.Rows.Count > 0)
                        dataGridView1.FirstDisplayedScrollingRowIndex = dataGridView1.Rows.Count - 1;
                }));
            }
            else
            {
                if (dataGridView1.Rows.Count > 0)
                    dataGridView1.FirstDisplayedScrollingRowIndex = dataGridView1.Rows.Count - 1;
            }
        }

        private async void button12_Click(object sender, EventArgs e)
        {
            await SaveNewWBGitem();
        }

        private async void button13_Click(object sender, EventArgs e)
        {
            await SaveNewWBGitem();
        }
        public async ValueTask  SaveNewWBGitem()
        {
            string dombody = textBox4.Text.Trim();
            if (dombody.Length < 3) { ShowMB("Invalid Ipaddress or Domain or StringKey.", "Notice", false); return  ; }
            CurIPFM cf = CurIPFM.DOM;
            NameListType nn = NameListType.Gray;
            if (radioButton1.Checked) { nn = NameListType.Black; }
            else if(radioButton2.Checked) { nn = NameListType.White; }

            if ( !ShowMB("You will save [ "+dombody+" ] to " + nn + " List. Continue?", "Notice", true)) { return; }

            (bool suc, string msg) wr;
            if (radioButton11.Checked)
            { wr = await _saveNewDom(nn); cf = CurIPFM.DOM; }
            else if (radioButton12.Checked)
            { wr = await _saveNewIpv6(nn); cf = CurIPFM.IPV6; }
            else { wr = await _saveNewIpv4(nn); cf = CurIPFM.IPV4; }
            ShowMB("Save "+nn +" Item job is "+ (wr.suc ? "successful" : "failed") + ". " + wr.msg);
            if (wr.suc)
            {
                AppCenter.CIPFM = cf;
                AppCenter.CWBG = nn;
                await  reloadTable();
            }
          
        }
        private bool ShowMB(  string message,string title="Notice",bool tolog=true) 
        { 
            DialogResult r = MessageBox.Show(message, title, MessageBoxButtons.YesNo, MessageBoxIcon.Asterisk);
            if (tolog) { AddLog(title + " : " + message +" | DR:"+r.ToString()); }
            return (r == DialogResult.OK || r == DialogResult.Yes);
        }
        private async ValueTask<(bool suc, string msg)> _saveNewIpv4(NameListType wbg)
        {
            try
            {
                var j = IPTools.ParseIP(textBox4.Text.Trim());
                if (!j.Legal || j.Bytes.Length > 4 || j.Bytes.Length < 4) { ShowMB("Invalid IPv4 address."); return (false, "Invalid IPv4 address"); }
                var jj = IPTools.Expand(j.FullStr);
                var w = WorkClass.MakeWBGV4(textBox5.Text.Trim(), jj.fullstr, j.Bytes[0], jj.pflen, textBox6.Text.Trim(), wbg);
                return (await AppCenter.InsertIPV4(w), "Save Ipv4Item Done");
            }
            catch (Exception ze)
            { return (false,"Error save IPV4 : "+ ze.Message); }
        }
        private async ValueTask<(bool suc, string msg)> _saveNewIpv6(NameListType wbg)
        {
            try
            {
                var j = IPTools.ParseIP(textBox4.Text.Trim());
                if (!j.Legal ||   j.Bytes.Length < 16) { ShowMB("Invalid IPv6 address."); return (false, "Invalid IPv6 address"); }
                var jj = IPTools.Expand(j.FullStr);
                var w = WorkClass.MakeWBGV6(textBox5.Text.Trim(), jj.fullstr, IPTools.GetIPV6_HeadString(8,jj.bin), jj.pflen, textBox6.Text.Trim(), wbg);
                return (await AppCenter.InsertIPV6(w), "Save Ipv6Item Done");
            }
            catch (Exception ze)
            { return (false, "Error save IPV6 : " + ze.Message); }
        }
        private async ValueTask<(bool suc, string msg)> _saveNewDom(NameListType wbg)
        {
            try
            { 
                byte nettype = 5; byte DoR = 10;
                if (radioButton4.Checked) { nettype = 4; DoR = 10; }
                else if (radioButton6.Checked) { nettype = 6; DoR = 10; }
                else if (radioButton7.Checked) { nettype = 5; DoR = 10; }
                else if (radioButton5.Checked) { nettype = 5; DoR = 20; }
                else if (radioButton9.Checked) { nettype = 6; DoR = 20; }
                else if (radioButton8.Checked) { nettype = 4; DoR = 20; }

                var w = WorkClass.MakeWBGDOM(textBox5.Text.Trim(), textBox4.Text.Trim(), nettype, DoR, textBox6.Text.Trim(), wbg);
                return (await AppCenter.InsertDOM(w), "Save DomItem Done");
            }
            catch (Exception zee)
            { return (false, "Error save DOM : " + zee.Message); }
        }
    }
}
