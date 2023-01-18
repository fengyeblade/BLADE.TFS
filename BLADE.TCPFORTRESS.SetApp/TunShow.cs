using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BLADE.TCPFORTRESS.SetApp
{
    public partial class TunShow : UserControl
    {
        public int key = 0;
        public TunShow()
        {
            InitializeComponent();
        }
        protected Form1 Par;
        protected CoreClass.TunSet S;
        private void checkUserule_CheckedChanged(object sender, EventArgs e)
        {

        }

        public void ShowTunSet(CoreClass.TunSet inSet , Form1 inPar)
        {
            S = inSet;
            Par = inPar;
            key = Par.GetKey();
            label8.Text = key.ToString();

            int cc = key % 8;
            this.BackColor = Color.FromArgb(8, 60 - (cc * 3), 70 + (cc * 10));

            textName.Text = S.TunName;
            textMTU.Text = S.MTUSize.ToString();
            checkUserule.Checked = S.UseRule;

            textInAddr.Text = S.InAddress;
            textOutAddr.Text = S.OutAddress;
            textInport.Text = S.InPort.ToString();
            textOutport.Text = S.OutPort.ToString();
            textSpeed.Text = S.SpeedMax.ToString();
            textLockCount.Text = S.LockCount.ToString();
        }
        public void SetLockCount(int p)
        {
            S.LockCount = p;
            textLockCount.Text = p.ToString();
        }
        public CoreClass.TunSet GetSET()
        {
            S.UseRule = checkUserule.Checked;
            S.InAddress = textInAddr.Text.Trim();
            S.OutAddress = textOutAddr.Text.Trim();
            S.InPort = int.Parse(textInport.Text.Trim());
            S.OutPort = int.Parse(textOutport.Text.Trim());
            S.TunName = textName.Text.Trim();
            S.MTUSize = int.Parse(textMTU.Text.Trim());
            S.SpeedMax = int.Parse(textSpeed.Text);
            S.LockCount = int.Parse(textLockCount.Text);
            return S;
        }

        private void textName_TextChanged(object sender, EventArgs e)
        {
            if(textName.Text.Trim().Length<2)
            {
                textName.Text = "TunName" + DateTime.Now.Second.ToString()+"."+ DateTime.Now.Millisecond.ToString();
            }
        }

        private void textMTU_TextChanged(object sender, EventArgs e)
        {
            int p = 1400;
            try { p = int.Parse(textMTU.Text);
                if (p < 1000) { p = 1000; }
                if (p > 8192) { p = 8192; }
            }
            catch { p = 1400; }
            textMTU.Text = p.ToString();
        }

        private void textInport_TextChanged(object sender, EventArgs e)
        {
            int p = 2000;
            try { p = int.Parse(textInport.Text);
                if (p < 1) { p = 1; }
                if (p > 65535) { p = 65535; }
            }
            catch { p = 2000; }
            textInport.Text = p.ToString();
        }

        private void textOutport_TextChanged(object sender, EventArgs e)
        {
            int p = 2222;
            try
            {
                p = int.Parse(textOutport.Text);
                if (p < 1) { p = 1; }
                if (p > 65535) { p = 65535; }
            }
            catch { p = 2222; }
            textOutport.Text = p.ToString();
        }

        

        private void butdelete_Click(object sender, EventArgs e)
        {
            Par.DelTun(this);
        }

        private void TunShow_Load(object sender, EventArgs e)
        {
            
        }

        private void textSpeed_TextChanged(object sender, EventArgs e)
        {
            int sk = 1024;
            try
            {
                sk = int.Parse(textSpeed.Text);
            }
            catch
            { sk = 1024; }
            textSpeed.Text = sk.ToString();
        }
    }
}
