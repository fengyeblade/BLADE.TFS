namespace BLADE.TFS.HOMEGATE.WinMng
{
    public partial class AMAIN : Form
    {
        protected string logtext = string.Empty;
        private Lock _lll=new Lock();
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
        }

        private void AMAIN_Load(object sender, EventArgs e)
        {
            _timer = new(timeCall, null, 700, 700);
            richTextBox1.Text = "";
            logtext = "App Started.";
            dataGridView1.Get_DG_Extend().WBG = CurWBG.White;
            //dataGridView1.Get_DG_Extend().IPFM = CurIPFM.NONE;
            //dataGridView1.Get_DG_Extend().Loaded = false;
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
    }
}
