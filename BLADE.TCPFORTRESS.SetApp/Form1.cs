using BLADE.TCPFORTRESS.CoreClass;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;


namespace BLADE.TCPFORTRESS.SetApp
{
    public partial class Form1 : Form
    {

        protected CoreClass.Settings RunSettings;

        public Form1()
        {
            InitializeComponent();

        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void ListWhite_CheckedChanged(object sender, EventArgs e)
        {
            listGray.Checked = false;
            listBlack.Checked = false;
        }

        private void listGray_CheckedChanged(object sender, EventArgs e)
        {
            //  listGray.Checked = false;
            listBlack.Checked = false;
            ListWhite.Checked = false;
        }

        private void listBlack_CheckedChanged(object sender, EventArgs e)
        {
            listGray.Checked = false;
            // listBlack.Checked = false;
            ListWhite.Checked = false;
        }

        protected int WOB 
        {
            get
            {
                if (ListWhite.Checked)
                {
                    return 0;
                }
                if (listGray.Checked)
                {
                    return 1;
                }

                return 2;
            }
            set
            {
                if(value<1)
                {
                    ListWhite.Checked = true;
                    listGray.Checked = false;
                    listBlack.Checked = false;
                }
                else
                {
                    if (value > 1)
                    {
                        ListWhite.Checked = false;
                        listGray.Checked = false;
                        listBlack.Checked = true;
                    }
                    else
                    {
                        
                            ListWhite.Checked = false;
                            listGray.Checked = true;
                            listBlack.Checked = false;
                        
                    }
                }

            }

        }
        private void button3_Click(object sender, EventArgs e)
        {
            TunShow ttts = new TunShow();
            flowLayoutPanel1.Controls.Add(ttts);
            ttts.ShowTunSet(new TunSet(), this);
            ttts.SetLockCount(AppRunCenter.RunSet.TimeCount);
            TunList.Add(ttts.key, ttts);
            flowLayoutPanel1.SetFlowBreak(ttts, true);

        }
        public void DelTun(TunShow inshow)
        {
            if (TunList.ContainsKey(inshow.key))
            {
                TunList.Remove(inshow.key);
                flowLayoutPanel1.Controls.Remove(inshow);
                inshow.Dispose();
            }
        }
        protected SortedList<int, TunShow> TunList = new SortedList<int, TunShow>();

        private async void Form1_Load(object sender, EventArgs e)
        {
            textAppCurPath.Text = Application.StartupPath;
            await CoreClass.AppRunCenter.Init(textAppCurPath.Text);
            await CoreClass.AppRunCenter.LOG.AddLog(false, 100, "SetApp run Form1_Load()");
            GetService();
            try { serviceStatus.Text ="Service: "+ serviceController1.Status.ToString(); }
            catch { }
            
        }
        protected string filename = "";
        private void butLoadXML_Click(object sender, EventArgs e)
        {
            openFileDialog1.RestoreDirectory = true;
            openFileDialog1.InitialDirectory = Application.StartupPath;
            DialogResult dr = openFileDialog1.ShowDialog();
            if(dr==DialogResult.OK || dr == DialogResult.Yes)
            {
                 filename= openFileDialog1.FileName;
                using (  StreamReader sr = File.OpenText(filename))
                {
                    string xmltext = sr.ReadToEnd();
                    textXML.Text = xmltext.Trim();
                    ExptoRight();
                    MessageBox.Show("Open File : \"" + filename + "\" OK ! ","Open OK",MessageBoxButtons.OK,MessageBoxIcon.Information);
                }
            }
            button2.Visible = true;
        }

        private void butsaveXML_Click(object sender, EventArgs e)
        {
            string fff = textXML.Text.Trim();
            if(filename.Length<2)
            {
                MessageBox.Show("None Selected File. \r\nPlease Open XML file frist. ","Alert" ,MessageBoxButtons.OK,MessageBoxIcon.Warning);
                return;
            }
            using (StreamWriter sw = File.CreateText(filename))
            {
                sw.Write(fff);
            }
            AppRunCenter.LOG.AddLog("File \"" + filename + "\" Saaved!");
            DialogResult dr = MessageBox.Show("File : \"" + filename + "\" Save OK \r\n Do you want ReStart Service ?" , "Save OK",MessageBoxButtons.YesNo,MessageBoxIcon.Question);
            if(dr == DialogResult.Yes)
            {
                RestartService();
            }
          
            AppRunCenter.LOG.SaveLogs(true);
        }

       

        private void butExpto_Click(object sender, EventArgs e)
        {

            ExptoRight();
        }

        protected void ExptoRight()
        {
            XmlSerializer xs = new XmlSerializer(typeof(CoreClass.Settings));
            byte[] mmm = Encoding.UTF8.GetBytes(textXML.Text.Trim());
            MemoryStream ms = new MemoryStream(mmm);
            AppRunCenter.RunSet = (CoreClass.Settings)xs.Deserialize(ms);

            ms.Dispose();
            ShowSet();

        }
        protected int jskey = 3000;
        public int GetKey()
        {
            jskey = jskey +3;
            if(jskey> (int.MaxValue-30000))
            {
                jskey = 2000;
            }
            return jskey;
        }
        protected void ShowSet()
        {
            checkDebug.Checked = AppRunCenter.RunSet.Debug;
            checkLonglock.Checked = AppRunCenter.RunSet.LongLockGray;
            checkautoblack.Checked = AppRunCenter.RunSet.RecordAutoAddBlackList;

            textDBstr.Text = AppRunCenter.RunSet.DBStr;
            textDBname.Text = AppRunCenter.RunSet.DBName;
            textLockTime.Text = AppRunCenter.RunSet.TimeLockSecond.ToString();
            textCountTime.Text = AppRunCenter.RunSet.TimeSecond.ToString();
            textCount.Text = AppRunCenter.RunSet.TimeCount.ToString();

            textLogPath.Text = AppRunCenter.RunSet.LogFilePath.Trim();

            WOB = AppRunCenter.RunSet.RunWithWhiteOrBlack;

            flowLayoutPanel1.Controls.Clear();
            TunShow[] ss = TunList.Values.ToArray(); ;
            for (int z = 0; z < ss.Length; z++)
            {
                ss[z].Dispose();
            }
            TunList.Clear();

            for (int z = 0; z < AppRunCenter.RunSet.Tuns.Length; z++)
            {
                TunShow ttts = new TunShow();
                flowLayoutPanel1.Controls.Add(ttts);
                ttts.ShowTunSet(AppRunCenter.RunSet.Tuns[z], this);
                TunList.Add(ttts.key, ttts);
                flowLayoutPanel1.SetFlowBreak(ttts, true);
            }
            AppRunCenter.LOG.AddLog("Show Set OK ");
        }

        private void buttoXML_Click(object sender, EventArgs e)
        {
            DecodeSET();
            XmlSerializer xs = new XmlSerializer(typeof(CoreClass.Settings));
            MemoryStream ms = new MemoryStream();
            xs.Serialize(ms,AppRunCenter.RunSet);
            byte[] sss = ms.GetBuffer();
            string xxx = Encoding.UTF8.GetString(sss);
            ms.Dispose();
            textXML.Text = xxx;

           
            
        }
        protected void DecodeSET()
        {
            AppRunCenter.RunSet.Debug = checkDebug.Checked;
            AppRunCenter.RunSet.LongLockGray = checkLonglock.Checked;
            AppRunCenter.RunSet.RecordAutoAddBlackList = checkautoblack.Checked;

            AppRunCenter.RunSet.DBStr = textDBstr.Text.Trim();
            AppRunCenter.RunSet.DBName = textDBname.Text.Trim();
            AppRunCenter.RunSet.TimeLockSecond = int.Parse(textLockTime.Text);
            AppRunCenter.RunSet.TimeSecond = int.Parse(textCountTime.Text);
            AppRunCenter.RunSet.TimeCount = int.Parse(textCount.Text);

            AppRunCenter.RunSet.LogFilePath = textLogPath.Text.Trim();

            AppRunCenter.RunSet.RunWithWhiteOrBlack = WOB;

            CoreClass.TunSet[] k = new TunSet[flowLayoutPanel1.Controls.Count];
            for(int z=0;z< k.Length;z++)
            {
                k[z] = ((TunShow) flowLayoutPanel1.Controls[z]).GetSET();

            }
            AppRunCenter.RunSet.Tuns = k;
            AppRunCenter.LOG.AddLog("DecodeSet OK ");

        }
        protected bool Getserviced = false;
        protected void GetService()
        {
            ServiceController[]  ts = ServiceController.GetServices();
            for(int z=0;z<ts.Length;z++)
            {
                if (ts[z].ServiceName =="TFService")
                {
                    serviceController1 = ts[z];
                    break;
                }
            }
        }
        private void butStop_Click(object sender, EventArgs e)
        {
            try
            {
                StopService();
                
                MessageBox.Show("Service Stoped ! ");
            }
            catch { }
        }

        protected void StopService()
        {

            serviceController1.Stop();
            Thread.Sleep(1000);
            AppRunCenter.LOG.AddLog("Service Stop Succeed.");
            Thread.Sleep(2000);
           
            serviceStatus.Text = "Service: " + serviceController1.Status.ToString();
        }
        protected void StartService()
        {
            serviceController1.Start();
            Thread.Sleep(1000);
            AppRunCenter.LOG.AddLog("Service Start Succeed.");
            Thread.Sleep(2000);
          
            serviceStatus.Text = "Service: " + serviceController1.Status.ToString();
        }
        protected void RestartService()
        {
            try { StopService();
               
            } catch { }
            Thread.Sleep(300);
            try
            {
                StartService();
               
            }
            catch(Exception zee) {
                AppRunCenter.LOG.AddLog("Start Servicec Error: " + zee.ToString());
              
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            RestartService();
            MessageBox.Show("Service Started ! ");
        }
        protected FormDB FDB=null;
        private void button2_Click(object sender, EventArgs e)
        {
            if(FDB ==null)
            {
                FDB = new FormDB();
                DialogResult dr = FDB.ShowDialog();

                FDB.Dispose();
                FDB = null;
            }
        }
    }
}
