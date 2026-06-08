using System.Runtime.InteropServices;

namespace BLADE.TFS.HOMEGATE.WINAPP
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        public BLADE.TFS.HOMEGATE.COMM.WorkCore? WC = null;
        public string RuntimeStatus = "";
        private System.Threading.Timer? timer = null;
        public void ShowUI()
        {

            this.Invoke(new Action(() =>
            {
                label1.Text = BLADE.TimeProvider.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                if (RuntimeStatus.Length > 0)
                {
                    string tt = "Status \r\n" + RuntimeStatus;
                    textBox1.SuspendDrawing();
                    textBox1.Text = tt;
                    textBox1.ResumeDrawing();

                }
            }));
        }
        private DateTime _loadsta = BLADE.TimeProvider.UtcNow;
        private void timerCall(object? s)
        {
            ShowUI();
            trystatus();
        }
        private async void trystatus()
        {
            if (WC != null && (BLADE.TimeProvider.UtcNow - _loadsta).TotalSeconds > 15)
            {
                UpdateStatus(WC.RunStatus);
            }
        }
        public void UpdateStatus(string status)
        {
            RuntimeStatus = BLADE.TimeProvider.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " " + status + "\r\n" + RuntimeStatus;
            if (RuntimeStatus.Length > 2000)
            {
                RuntimeStatus = RuntimeStatus.Substring(0, 1998) + " ... ...";
            }
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            timer = new System.Threading.Timer(timerCall, null, 800, 800);
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            if (WC != null)
            {
                UpdateStatus("StartUp Fail, WorkCore is not null.");
                return;
            }
            WC = new BLADE.TFS.HOMEGATE.COMM.WorkCore();
            var j = await WC.StartUp(Application.StartupPath);
            UpdateStatus("StartUp " + j.suc + ", Msg: " + j.msg);
            if (j.suc)
            {
                button1.Enabled = false;
                button2.Enabled = true;
            }
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            if (WC == null)
            {
                UpdateStatus("ShutDown Fail, WorkCore is null.");
                return;
            }
            var j = await WC.Stop();
            WC.Dispose();
            WC = null;
            UpdateStatus("ShutDown " + j.suc + ", Msg: " + j.msg);

            button1.Enabled = true;
            button2.Enabled = false;

        }
    }


    public static class ControlHelper
    {
        // Windows API：禁止控件重绘
        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int wMsg, int wParam, int lParam);
        private const int WM_SETREDRAW = 0x000B;

        /// <summary>
        /// 暂停控件重绘
        /// </summary>
        public static void SuspendDrawing(this Control control)
        {
            SendMessage(control.Handle, WM_SETREDRAW, 0, 0);
        }

        /// <summary>
        /// 恢复控件重绘
        /// </summary>
        public static void ResumeDrawing(this Control control)
        {
            SendMessage(control.Handle, WM_SETREDRAW, 1, 0);
            control.Refresh();
        }
    }
}
