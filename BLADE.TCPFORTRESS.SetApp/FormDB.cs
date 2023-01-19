using BLADE.TCPFORTRESS.CoreClass;
using BLADE.TCPFORTRESS.CoreClass.DB.DBV;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BLADE.TCPFORTRESS.SetApp
{
    public partial class FormDB : Form
    {
        public FormDB()
        {
            InitializeComponent();
        }

        protected object OBJ = null;
        public void InSet(object inO)
        {
            OBJ = inO;
        }
        private void FormDB_FormClosing(object sender, FormClosingEventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        private void FormDB_Load(object sender, EventArgs e)
        {
            dataGridView1.DefaultCellStyle.BackColor = Color.FromArgb(0, 50, 60);
            dataGridView1.DefaultCellStyle.ForeColor = Color.FromArgb(160, 20, 130);

            dataGridView2.DefaultCellStyle.BackColor = Color.FromArgb(20, 30, 40);
            dataGridView2.DefaultCellStyle.ForeColor = Color.FromArgb(220, 20, 220);

            dataGridView4.DefaultCellStyle.BackColor = Color.FromArgb(5, 20, 10);
            dataGridView4.DefaultCellStyle.ForeColor = Color.FromArgb(30, 180, 90);

            dataGridView3.DefaultCellStyle.BackColor = Color.FromArgb(64, 20, 0);
            dataGridView3.DefaultCellStyle.ForeColor = Color.FromArgb(100, 10, 240);
            try
            {
                Thread.Sleep(10);
                ShowGray();
                Thread.Sleep(10);
                ShowBlack();
                ShowWhite();
                ShowPardon();
            }
            catch (Exception zez)
            {
                MessageBox.Show("FormDB_Load() EX: " + zez.ToString() + " \r\n\r\nPlease ReOpen it.");
            }
        }

        protected async Task<int> ShowPardon()
        {
            try
            {
                dataGridView4.Rows.Clear();
                TFS_Address[] T = await AppRunCenter.GetPardonList();
                for (int z = 0; z < T.Length; z++)
                {
                    dataGridView4.Rows.Add(T[z].TFS_AID, T[z].TFS_AddressStr, false);
                }
                label11.Text = T.Length.ToString();
                return T.Length;
            }
            catch (Exception zez)
            {
                MessageBox.Show("ShowGray() EX: " + zez.ToString() + " \r\n\r\nPlease try it later.");
            }
            return -1;
        }

        private void butLoadgray_Click(object sender, EventArgs e)
        {
            try
            {
                ShowGray();
            }catch(Exception zez)
            {
                MessageBox.Show("ShowGray() EX: " + zez.ToString()+" \r\n\r\nPlease try it later.");
            }
        }

        protected void ShowGray()
        {
            int rw = dataGridView1.FirstDisplayedScrollingRowIndex;
            dataGridView1.Rows.Clear();
            CoreClass.DB.TFS_Address_DBT[] T = LoadAddress(1);
            int m = 85;
            string T1 = "255";
            string T2 = "0";
            string T3 = "0";
            int v = 170;

            for (int z = 0; z < T.Length; z++)
            {

                dataGridView1.Rows.Add(T[z].VO.TFS_AID, T[z].VO.TFS_ReactCount, T[z].VO.TFS_WhiteOrBlack, T[z].VO.TFS_CIDR, T[z].VO.TFS_IpV6, T[z].VO.TFS_ALastTime,
                T[z].VO.TFS_AddressStr, T[z].VO.TFS_K1, T[z].VO.TFS_K2, T[z].VO.TFS_K3, T[z].VO.X, false);
                if (T[z].VO.TFS_K1 == T1 && T[z].VO.TFS_K2 == T2)
                {
                    if (T[z].VO.TFS_K3 == T3)
                    {
                        v = 170;
                    }
                    else { v = 5; }
                    dataGridView1.Rows[z - 1].DefaultCellStyle.BackColor = Color.FromArgb(v, m, 60);
                    dataGridView1.Rows[z].DefaultCellStyle.BackColor = Color.FromArgb(v, m, 60);
                }
                else
                {
                    T1 = T[z].VO.TFS_K1;
                    T2 = T[z].VO.TFS_K2;
                    T3 = T[z].VO.TFS_K3;
                    m = m + 20;
                    if (m > 200)
                    { m = 90; }
                }
            }
            graynum.Text = T.Length.ToString();
            try
            {
                if (rw < dataGridView1.Rows.Count)
                {
                    dataGridView1.FirstDisplayedScrollingRowIndex = rw;
                }
            }
            catch { }


        }
        protected void ShowWhite()
        {
            int rw = dataGridView3.FirstDisplayedScrollingRowIndex;
            dataGridView3.Rows.Clear();
            CoreClass.DB.TFS_Address_DBT[] T = LoadAddress(0);
            int m = 90;
            string T1 = "255";
            string T2 = "0";
            string T3 = "0";
            int v = 170;

            for (int z = 0; z < T.Length; z++)
            {

                dataGridView3.Rows.Add(T[z].VO.TFS_AID, T[z].VO.TFS_AddressStr, T[z].VO.TFS_WhiteOrBlack, T[z].VO.TFS_CIDR, T[z].VO.TFS_IpV6,
                T[z].VO.TFS_K1, T[z].VO.TFS_K2, T[z].VO.TFS_K3, T[z].VO.X, T[z].VO.TFS_ALastTime);
                if (T[z].VO.TFS_K1 == T1 && T[z].VO.TFS_K2 == T2)
                {
                    if (T[z].VO.TFS_K3 == T3)
                    {
                        v = 170;
                    }
                    else { v = 5; }
                    dataGridView3.Rows[z - 1].DefaultCellStyle.BackColor = Color.FromArgb(v, m, 80);
                    dataGridView3.Rows[z].DefaultCellStyle.BackColor = Color.FromArgb(v, m, 80);
                }
                else
                {
                    T1 = T[z].VO.TFS_K1;
                    T2 = T[z].VO.TFS_K2;
                    T3 = T[z].VO.TFS_K3;
                    m = m + 20;
                    if (m > 200)
                    { m = 90; }

                }
            }
            whitenum.Text = T.Length.ToString();
            try
            {
                if (rw < dataGridView3.Rows.Count)
                {
                    dataGridView3.FirstDisplayedScrollingRowIndex = rw;
                }
            }
            catch { }
        }

        protected CoreClass.DB.TFS_Address_DBT[] LoadAddress(int wbg)
        {
            CoreClass.DB.TFS_Address_DBT[] tFS_Address_DBTs = CoreClass.DB.TFS_Address_DBT.SelectByWhere(60000, " TFS_WhiteOrBlack=" + wbg.ToString() + " order by TFS_K1, TFS_K2, TFS_K3 ", null);
            return tFS_Address_DBTs;

        }
        protected void ShowBlack()
        {
            int rw = dataGridView2.FirstDisplayedScrollingRowIndex;
            dataGridView2.Rows.Clear();
            CoreClass.DB.TFS_Address_DBT[] T = LoadAddress(2);
            int m = 90;
            int v = 160;
            string T1 = "255";
            string T2 = "0";
            string T3 = "0";
            for (int z = 0; z < T.Length; z++)
            {

                dataGridView2.Rows.Add(T[z].VO.TFS_AID, T[z].VO.TFS_AddressStr, T[z].VO.TFS_WhiteOrBlack, T[z].VO.TFS_CIDR, T[z].VO.TFS_IpV6,
                T[z].VO.TFS_K1, T[z].VO.TFS_K2, T[z].VO.TFS_K3, T[z].VO.X, T[z].VO.TFS_ALastTime);
                if (T[z].VO.TFS_K1 == T1 && T[z].VO.TFS_K2 == T2)
                {
                    if (T[z].VO.TFS_K3 == T3)
                    {
                        v = 160;
                    }
                    else { v = 5; }
                    dataGridView2.Rows[z - 1].DefaultCellStyle.BackColor = Color.FromArgb(v, m, 70);
                    dataGridView2.Rows[z].DefaultCellStyle.BackColor = Color.FromArgb(v, m, 70);
                }
                else
                {
                    T1 = T[z].VO.TFS_K1;
                    T2 = T[z].VO.TFS_K2;
                    T3 = T[z].VO.TFS_K3;
                    m = m + 20;
                    if (m > 200)
                    { m = 90; }
                }
            }
            blacknum.Text = T.Length.ToString(); 
            try
            {
                if (rw < dataGridView2.Rows.Count)
                {
                    dataGridView2.FirstDisplayedScrollingRowIndex = rw;
                }
            }
            catch { }
        }
        private void butLOADWHITE_Click(object sender, EventArgs e)
        {
            try { 
            ShowWhite();
            }
            catch (Exception zez)
            {
                MessageBox.Show("butLOADWHITE_Click() EX: " + zez.ToString() + " \r\n\r\nPlease try it later.");
            }
        }

        private void butLOADBLACK_Click(object sender, EventArgs e)
        {
            try
            {
                ShowBlack();
            }
            catch (Exception zez)
            {
                MessageBox.Show("butLOADBLACK_Click() EX: " + zez.ToString() + " \r\n\r\nPlease try it later.");
            }
        }

        private void butSETBLACK_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Set GrayList: " + UpdateList(2).ToString() + " items TO BLACK LIST");
        }
        protected int[] GetSelectGRAYLIST()
        {
            List<int> ids = new List<int>();
            List<int> roind = new List<int>();
            // new SortedList<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int z = 0; z < dataGridView1.Rows.Count; z++)
            {
                DataGridViewCheckBoxCell c11 = (DataGridViewCheckBoxCell)dataGridView1.Rows[z].Cells[11];
                if (Convert.ToBoolean(c11.EditedFormattedValue))
                {
                    ids.Add(int.Parse(dataGridView1.Rows[z].Cells[0].Value.ToString()));
                    roind.Add(z);
                }
            }
            zhuanyongIDS = ids.ToArray();
            zhuanyongRowIndex = roind.ToArray();
            return ids.ToArray();
        }
        protected int UpdateList(int Nwob)
        {

            int a = 0;
            try
            {
                a = OPDB.UpdateList(GetSelectGRAYLIST(), Nwob);
                Thread.Sleep(200);
                ShowGray();
                if (Nwob == 2)
                {
                    ShowBlack();
                }
                else
                {
                    ShowWhite();
                }
            }
            catch (Exception zez)
            {
                MessageBox.Show("UpdateList() EX: " + zez.ToString() + " \r\n\r\nPlease try it later.");
            }
            return a;
        }

        private void butSETWHITE_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Set GrayList: " + UpdateList(0).ToString() + " items TO WHITE LIST");
        }

        private void butDELGRAY_Click(object sender, EventArgs e)
        {
            try
            {
                List<int> ids = new List<int>();
                // new SortedList<string, int>(StringComparer.OrdinalIgnoreCase);
                for (int z = 0; z < dataGridView1.Rows.Count; z++)
                {
                    DataGridViewCheckBoxCell c11 = (DataGridViewCheckBoxCell)dataGridView1.Rows[z].Cells[11];
                    if (Convert.ToBoolean(c11.EditedFormattedValue))
                    {
                        ids.Add(int.Parse(dataGridView1.Rows[z].Cells[0].Value.ToString()));
                    }
                }
                int a = OPDB.DeleteList(ids.ToArray());
                Thread.Sleep(200);
                ShowGray();
                MessageBox.Show("Deleted " + a.ToString() + " Gray items");
            }
            catch (Exception zez)
            {
                MessageBox.Show("butDELGRAY_Click() EX: " + zez.ToString() + " \r\n\r\nPlease try it later.");
            }
        }

        private void butDELBLACK_Click(object sender, EventArgs e)
        {
            try
            {
                List<int> ids = new List<int>();
                // new SortedList<string, int>(StringComparer.OrdinalIgnoreCase);

                DataGridViewSelectedRowCollection sr = dataGridView2.SelectedRows;
                for (int z = 0; z < sr.Count; z++)
                {

                    ids.Add(int.Parse(sr[z].Cells[0].Value.ToString()));

                }
                int a = OPDB.DeleteList(ids.ToArray());
                Thread.Sleep(200);
                ShowBlack();
                MessageBox.Show("Deleted " + a.ToString() + " Black items");
            }
            catch (Exception zez)
            {
                MessageBox.Show("butDELBLACK_Click() EX: " + zez.ToString() + " \r\n\r\nPlease try it later.");
            }
        }

        private void butDELWHITE_Click(object sender, EventArgs e)
        {
            try
            {
                List<int> ids = new List<int>();

                DataGridViewSelectedRowCollection sr = dataGridView3.SelectedRows;
                for (int z = 0; z < sr.Count; z++)
                {

                    ids.Add(int.Parse(sr[z].Cells[0].Value.ToString()));

                }
                int a = OPDB.DeleteList(ids.ToArray());
                Thread.Sleep(200);

                ShowWhite();
                MessageBox.Show("Deleted " + a.ToString() + " WHITE items");
            }
            catch (Exception zez)
            {
                MessageBox.Show("butDELWHITE_Click() EX: " + zez.ToString() + " \r\n\r\nPlease try it later.");
            }
        }

        protected int[] zhuanyongRowIndex = new int[0];
        protected int[] zhuanyongIDS = new int[0];
        private void butMERGESELECT_Click(object sender, EventArgs e)
        {
            //根据选择的灰名单项目 合并成IP段
            zhuanyongIDS = GetSelectGRAYLIST();

            if (zhuanyongIDS.Length < 1)
            {
                MessageBox.Show("None Selected Items . Click Selete !");
                return;
            }
            checkIPV6.Checked = false;
            string tids = "";

            string k1 = "0";
            string k2 = "0";
            string k3 = "0";
            string k4 = "0";
            string zk1 = "0";
            string zk2 = "0";
            string zk3 = "0";
            string zk4 = "0";
            bool tong1 = true;
            bool tong2 = true;
            bool tong3 = true;



            for (int z = 0; z < zhuanyongIDS.Length; z++)
            {
                tids = tids + zhuanyongIDS[z].ToString() + " , ";
                if (z == 0)
                {
                    k1 = dataGridView1.Rows[zhuanyongRowIndex[z]].Cells[7].Value.ToString();
                    k2 = dataGridView1.Rows[zhuanyongRowIndex[z]].Cells[8].Value.ToString();
                    k3 = dataGridView1.Rows[zhuanyongRowIndex[z]].Cells[9].Value.ToString();
                }
                zk1 = dataGridView1.Rows[zhuanyongRowIndex[z]].Cells[7].Value.ToString();
                zk2 = dataGridView1.Rows[zhuanyongRowIndex[z]].Cells[8].Value.ToString();
                zk3 = dataGridView1.Rows[zhuanyongRowIndex[z]].Cells[9].Value.ToString();
                if (k1 != zk1)
                {
                    tong1 = false;
                }
                if (k2 != zk2)
                {
                    tong2 = false;
                }
                if (k3 != zk3)
                {
                    tong3 = false;
                }



            }

            if (zhuanyongIDS.Length > 1)
            {
                checkCIDR.Checked = true;

                textIDS.Text = tids;
                if (tong1)
                {
                    textK1.Text = k1;
                    textDUAN.Text = "/8";
                    if (tong2)
                    {
                        textK2.Text = k2;
                        textDUAN.Text = "/16";
                        if (tong3)
                        {
                            textK3.Text = k3;
                            textDUAN.Text = "/24";
                        }
                        else
                        {

                            textK3.Text = "0";
                            textK4.Text = "0";
                        }
                    }
                    else
                    {

                        textK2.Text = "0";
                        textK3.Text = "0";
                        textK4.Text = "0";
                    }
                }
                else
                {
                    textDUAN.Text = "/0";
                    textK1.Text = "0";
                    textK2.Text = "0";
                    textK3.Text = "0";
                    textK4.Text = "0";

                }
                textADDRCIDR.Text = textK1.Text + "." + textK2.Text + "." + textK3.Text + "." + textK4.Text + textDUAN.Text.ToUpper().Trim();
            }
            else
            {
                textIDS.Text = zhuanyongIDS[0].ToString();
                textK1.Text = k1;
                textK2.Text = k2;
                textK3.Text = k3;
                checkCIDR.Checked = false;
                textADDRCIDR.Text = dataGridView1.Rows[zhuanyongRowIndex[0]].Cells[6].Value.ToString();
            }
        }

        private void butMERGEBLACK_Click(object sender, EventArgs e)
        {
            try { 
            CoreClass.DB.DBV.TFS_Address AA = ChangeTo(2);

            string jk = "Clear " + OPDB.DeleteList(zhuanyongIDS).ToString() + " Items.";
            OPDB.SavetoWOB(AA);
            jk = jk + " Make a new BLACK " + AA.TFS_AID.ToString();
            ;
            Thread.Sleep(100);
            ShowGray();
            Thread.Sleep(100);
            ShowBlack();
            MessageBox.Show(jk);
            }
            catch (Exception zez)
            {
                MessageBox.Show("butMERGEBLACK_Click() EX: " + zez.ToString() + " \r\n\r\nPlease try it later.");
            }
        }

        private void butMERGEWHITE_Click(object sender, EventArgs e)
        {
            try
            {
                CoreClass.DB.DBV.TFS_Address AA = ChangeTo(0);

                string jk = "Clear " + OPDB.DeleteList(zhuanyongIDS).ToString() + " Items.";
                OPDB.SavetoWOB(AA);
                jk = jk + " Make a new WHITE " + AA.TFS_AID.ToString();
                ;
                Thread.Sleep(100);
                ShowGray();
                Thread.Sleep(100);
                ShowWhite();
                MessageBox.Show(jk);
            }
            catch (Exception zez)
            {
                MessageBox.Show("butMERGEWHITE_Click() EX: " + zez.ToString() + " \r\n\r\nPlease try it later.");
            }
        }

        protected CoreClass.DB.DBV.TFS_Address ChangeTo(int wob)
        {
            CoreClass.DB.DBV.TFS_Address A = new CoreClass.DB.DBV.TFS_Address();
            A.TFS_AddressStr = textADDRCIDR.Text.ToUpper().Trim();
            A.TFS_CIDR = checkCIDR.Checked;
            A.TFS_IpV6 = checkIPV6.Checked;
            A.TFS_ReactCount = 88;
            A.TFS_WhiteOrBlack = wob;
            A.TFS_K1 = textK1.Text.ToUpper().Trim();
            A.TFS_K2 = textK2.Text.ToUpper().Trim();
            A.TFS_K3 = textK3.Text.ToUpper().Trim();
            A.fenX();
            return A;
        }

        private void dataGridView2_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void checkIPV6_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            // 测试用
            BLADE.BASETOOL.VNET4.IPCD IC = new BASETOOL.VNET4.IPCD(testcidr.Text.Trim());
            MessageBox.Show("Test IP:" + testip.Text.ToString().Trim() + " with in " + testcidr.Text.Trim() + " is " + IC.Contains(testip.Text.Trim()).ToString().ToUpper());

        }

        private void buttDelPardon_Click(object sender, EventArgs e)
        {
            try
            {
                // 删除选定的 赦免IP清单
                List<int> ids = new List<int>();

                for (int z = 0; z < dataGridView4.Rows.Count; z++)
                {
                    DataGridViewCheckBoxCell c2 = (DataGridViewCheckBoxCell)dataGridView4.Rows[z].Cells[2];
                    if (Convert.ToBoolean(c2.EditedFormattedValue))
                    {
                        ids.Add(int.Parse(dataGridView4.Rows[z].Cells[0].Value.ToString()));
                    }
                }
                int a = OPDB.DeleteList(ids.ToArray());
                Thread.Sleep(200);
                ShowPardon();
                MessageBox.Show("Deleted " + a.ToString() + " Pardon items");
            }
            catch (Exception zez)
            {
                MessageBox.Show("buttDelPardon_Click() EX: " + zez.ToString() + " \r\n\r\nPlease try it later.");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                TFS_Address tt = new TFS_Address();
                tt.TFS_AddressStr = textPardon.Text.Trim().ToUpper();
                tt.TFS_CIDR = false;
                tt.TFS_IpV6 = false;
                tt.TFS_WhiteOrBlack = -99;
                string[] nnn = tt.TFS_AddressStr.Split(new string[] { ":", ".", "/" }, StringSplitOptions.RemoveEmptyEntries);
                if (nnn.Length > 0)
                {
                    tt.TFS_K1 = nnn[0];
                }
                if (nnn.Length > 1)
                {
                    tt.TFS_K2 = nnn[1];
                }
                if (nnn.Length > 2)
                {
                    tt.TFS_K3 = nnn[2];
                }
                tt.fenX();

                CoreClass.DB.TFS_Address_DBT aaa = new CoreClass.DB.TFS_Address_DBT();
                aaa.V = tt;
                aaa.SaveByInsert();
                Thread.Sleep(50);
                ShowPardon();
                MessageBox.Show("Saved a Pardon IP : " + tt.TFS_AddressStr);
            }
            catch (Exception zez)
            {
                MessageBox.Show("button2_Click() EX: " + zez.ToString() + " \r\n\r\nPlease try it later.");
            }
        }

        protected void changeip(TextBox intb)
        {
            intb.Text = intb.Text.Trim().ToUpper();
            if (intb.Text.Length < 1)
            {
                intb.Text = "1";
            }
            if (intb.Text.Length > 5)
            {
                intb.Text = "1";
            }
        }
        private void textK1_Leave(object sender, EventArgs e)
        {
            changeip(textK1);
            ChangeText();
        }

        private void textK2_Leave(object sender, EventArgs e)
        {
            changeip(textK2);
            ChangeText();
        }

        private void textK3_Leave(object sender, EventArgs e)
        {
            changeip(textK3);
            ChangeText();
        }

        private void textK4_Leave(object sender, EventArgs e)
        {
            changeip(textK4);
            ChangeText();
        }

        private void textDUAN_Leave(object sender, EventArgs e)
        {
            textDUAN.Text = textDUAN.Text.Trim().ToUpper();
            if (textDUAN.Text.Length < 2)
            {
                textDUAN.Text = "/8";
            }
            if (textDUAN.Text.Length > 3)
            {
                textDUAN.Text = "/24";
            }
            ChangeText();
        }

        protected void ChangeText()
        {
            string kk1 = textK1.Text + "." + textK2.Text + "." + textK3.Text + "." + textK4.Text;
            if (checkCIDR.Checked)
            {
                kk1 = kk1 + textDUAN.Text;
            }
            textADDRCIDR.Text = kk1;
        }

        private void textK4_TextChanged(object sender, EventArgs e)
        {

        }

        private void checkCIDR_CheckedChanged(object sender, EventArgs e)
        {
            ChangeText();
        }

        private void textADDRCIDR_Leave(object sender, EventArgs e)
        {
            textADDRCIDR.Text = textADDRCIDR.Text.Trim().ToUpper();
            if (textADDRCIDR.Text.Length < 7)
            {
                textADDRCIDR.Text = "1.0.0.0/8";
            }
            string[] fenge = textADDRCIDR.Text.Split(new string[] { "/" }, StringSplitOptions.RemoveEmptyEntries);
            if (fenge.Length > 1)
            {
                checkCIDR.Checked = true;
                textDUAN.Text = "/" + fenge[fenge.Length - 1];
            }
            string[] adfen = fenge[0].Split(new string[] { ".", ",", ":", " " }, StringSplitOptions.RemoveEmptyEntries);

            if (adfen.Length > 0)
            { textK1.Text = adfen[0]; }
            else { textK1.Text = "1"; }

            if (adfen.Length > 1)
            { textK2.Text = adfen[1]; }
            else { textK2.Text = "0"; }
            if (adfen.Length > 2)
            { textK3.Text = adfen[2]; }
            else { textK3.Text = "0"; }
            if (adfen.Length > 3)
            { textK4.Text = adfen[3]; }
            else { textK4.Text = "0"; }


        }
    }

    public class OPDB
    {

        public static  int  SavetoWOB(CoreClass.DB.DBV.TFS_Address inA)
        {
            CoreClass.DB.TFS_Address_DBT ttt = new CoreClass.DB.TFS_Address_DBT();
            ttt.V = inA;
            return ttt.SaveByInsert();
        }
        public static int UpdateList(int[] tids,  int nwob)
        {
            int a = 0;
            BLADE.DBOP.BC.DBD DD = new DBOP.BC.DBD(CoreClass.DB.Configs.DefConStr);
            for (int z=0;z< tids.Length;z++)
            {
               
                a = a + (int)DD.FM_GeneralSQL("  update TFS_Address SET TFS_WhiteOrBlack ="+nwob.ToString()+"  where TFS_AID=" + tids[z].ToString(), DBOP.BC.SqlTYPE.UPDATE, null);
                

            }
            DD.Dispose();

            return a;
        }

        public static int DeleteList(int[] tids  )
        {
            int a = 0;
            BLADE.DBOP.BC.DBD DD = new DBOP.BC.DBD(CoreClass.DB.Configs.DefConStr);
            for (int z = 0; z < tids.Length; z++)
            {
                a = a + (int)DD.FM_GeneralSQL(" delete TFS_Address WHERE TFS_AID = " + tids[z].ToString(), DBOP.BC.SqlTYPE.DELETE, null);

            }
            DD.Dispose();

            return a;
        }
    }
}
