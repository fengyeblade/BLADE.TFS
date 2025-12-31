using BLADE;
using BLADE.MSGCORE.Models;
using BLADE.TOOLS.BASE;
using BLADE.TOOLS.BASE.ThreadSAFE;
using BLADE.TOOLS.LOG;
using BLADE.TOOLS.WEB;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using static System.Net.WebRequestMethods;

namespace BLADE.TFS.HOMEGATE.COMM
{
    /// <summary>
    /// Home Gate 转发服务器的 核心类。 
    /// </summary>
    public class HomeGateCore : IDisposable
    {
        public void Dispose()
        {
            if (!Disposed)
            {
                Disposed = true;
                Running = false;
                // TODO : 释放资源 
                string mm = "";
                lock (_lk)
                {
                    try
                    {
                        foreach (var i in TunDic.Values)
                        {
                            i.Dispose();
                        }
                        TunDic.Clear();
                        foreach (var i in TransDic.Values)
                        {
                            i.Dispose();
                        }
                        TransDic.Clear();
                    }
                    catch (Exception ze)
                    {
                        mm = ze.Message;
                    }
                }
                try { RunCenter.AddLog("HomeGateCore", "Disposed  [" + mm + "]", LogCodeEnum.App); } catch { }
            }
        }
        public bool Running { get; private set; } = false;
        public bool Disposed { get; private set; } = false;
        public HomeGateCore()
        {
            if (RunCenter.Settings == null)
            {  throw new Exception("HomeGateCore() Error: RunCenter.Settings is null. Need Load it frist by RunCenter.InitAndStart() !"); }
        }
        /// <summary>
        /// 启动 HOMEGATE  
        /// 需要在调用此方法之前需要先加载RunCenter.Settings,  by RunCenter.InitAndStart() !
        /// </summary>
        /// <returns></returns>
        public async Task<(bool suc,string info)> StartWork()
        {
            string err = "";
            try
            {
                if (RunCenter.Settings == null)
                {  return  (false, "HomeGateCore.StartWork() Error: Settings is null.  Need Load it frist by RunCenter.InitAndStart() !" );   }
                Running = true;

                StringBuilder sb = new StringBuilder();

                // TODO: 初始化 侦听器， 初始化转发通道准备。
                lock (_lk)
                {
                    foreach (var lt in RunCenter.Settings.TunSettings.Tuns)
                    {
                         TcpListenerItem tli = new TcpListenerItem(lt);
                        try { var sr = tli.Start();
                            if (sr.suc)
                            { TunDic.Add(tli.ID, tli); sb.AppendLine("Make Listener OK " + lt.GetRoadInfo()); }
                            else {
                                sb.AppendLine("Make Listener  " + lt.GetRoadInfo() + " Failed: " + sr.info);
                            }
                        }
                        catch (Exception ze)
                        { sb.AppendLine("Make " + lt.GetRoadInfo() + " Listener EX: " + ze.ToString()); }
                    }
                }
                await Task.Delay(200);
                // TODO: 启动工作循环线程。
                Task.Run(async () => { await LoopWork(); });
                return  (true, "GateStarted" );
            }
            catch (Exception ze)
            { err = "HomeGateCore.StartWork() Error: " + ze.Message; }
            finally { }
            return  (false, err);
        }
        private Dictionary_TS<int, TcpListenerItem> TunDic = new Dictionary_TS<int, TcpListenerItem>( );
        private Dictionary_TS<int, Trans> TransDic = new Dictionary_TS<int, Trans>();
        public string ListRuntimeTrans()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Tun list: "+ TunDic.Count);
            foreach (var ls in TunDic.Values)
            { sb.AppendLine(ls.ID+" "+ls.TunSetting.GetRoadInfo()+" "+ ls.Running); }
            sb.AppendLine("Cur Trans: "+TransDic.Count);
            lock (_lk)
            {   foreach (var cd in TransDic.Values)  { sb.AppendLine(cd.GetTransInfo()); }   }
            return sb.ToString();
        }
        /// <summary>
        /// 运行时的转发线路数量。
        /// </summary>
        public int Count_Trans { get { return TransDic.Count; } }
        protected DateTime _lastscan = TimeProvider.UtcNow;
        protected Lock _lk= new Lock();
        private int _stjs = 0;

        /// <summary>
        /// 检查和清理 转发工作集合
        /// </summary>
        /// <returns></returns>
        protected async ValueTask<int> ScanTrans()
        {
            int jss = 0;
            if ((TimeProvider.UtcNow - _lastscan).TotalSeconds > 2)
            {
                _stjs++;
                if (_stjs > 90)
                { _stjs = 0; }
                _lastscan = TimeProvider.UtcNow;
                string ll = "";
                string lp = "";
                try
                {
                    lock (_lk)
                    {
                        List<int> rmlist = new List<int>();
                        foreach (var i in TransDic.Values)
                        {
                            if (i.disposed) { rmlist.Add(i.ID); }
                            if ((TimeProvider.UtcNow - i.LastActUTC).TotalSeconds > RunCenter.Settings.IdelBreakMilliseconds)
                            { i.Dispose(); jss++; ll = ll + "DisopseTrans:" + i.GetTransInfo() + "  || "; }
                            else
                            {
                                if (_stjs == 50)
                                {  lp = lp + "\r\n" + i.GetTransInfo();  }
                            }

                        }
                        foreach (var di in rmlist)
                        { TransDic.Remove(di); ll = ll + "RemoveTrans:" + di + "  || "; jss++; }
                    }
                }
                catch (Exception ze)
                {
                    ll = ll + " Exception:" + ze.Message;
                }
                if (ll.Length > 0)
                {
                    await RunCenter.AddLogAsync("ScanTrans", ll, LogCodeEnum.Note);
                }
                if (lp.Length > 0)
                {
                    await RunCenter.AddLogAsync("Info&Speed", lp, LogCodeEnum.Note);
                }

                _lastscan = TimeProvider.UtcNow;
            }

            return jss;
        }
        /// <summary>
        /// 检查侦听器 接收新连接
        /// </summary>
        /// <returns></returns>
        protected async ValueTask<int> ScanListener()
        {
            int a = 0;
            foreach (var i in TunDic.Values)
            {
                if (i.Running)
                {
                    var k = i.GetInCome();
                    if (k.suc)
                    {
                        a++; 
                        if (Count_Trans >= RunCenter.Settings.MaxConnection)
                        {
                            await RunCenter.AddLogAsync("MaxConn", "Drop TcpClient " + k.newInCome.Client.RemoteEndPoint + " Tun:" + i.TunSetting.GetRoadInfo());
                            k.newInCome.Dispose();
                        }
                        else { Task.Run(async () => { await ShenInCome(i, k.newInCome); }); }
                    }
                }
            } 
            return a;
        }

        /// <summary>
        /// 对新连接进行白名单判断，如果允许，则创建转发通道，并放入通道集合。
        /// </summary>
        /// <param name="tun"></param>
        /// <param name="inc"></param>
        /// <returns></returns>
        protected async ValueTask ShenInCome(TcpListenerItem tun, TcpClient inc)
        {
            IPEndPoint ipep = ((IPEndPoint)inc.Client.RemoteEndPoint);
            string nip = ipep.Address.ToString().ToLower().Trim();
            int nippt = ipep.Port;
            bool b =  await RunCenter.CheckIP(tun.TunSetting.TunName, nip);
            if (b)
            {
                await RunCenter.AddLogAsync("CheckIP", "Welcome = " + nip + ":" + nippt);
                try {
                    TcpClient ntc = new TcpClient(tun.TunSetting.OutAddress, tun.TunSetting.OutPort);
                    ntc.LingerState = new LingerOption(true, 1);
                    ntc.ReceiveTimeout = 33000;
                    ntc.SendTimeout = 33000;
                    ntc.ReceiveBufferSize = tun.TunSetting.MTUSize * 3;
                    ntc.SendBufferSize = tun.TunSetting.MTUSize * 3;
                    Trans tas = new Trans(inc, ntc, tun.TunSetting);
                    bool d = false;
                    lock (_lk) { if (TransDic.ContainsKey(tas.ID)) { tas.Dispose(); } else { TransDic.Add(tas.ID, tas); d = true;  } }
                    if (d) { await tas.StartWork();
                        await RunCenter.AddLogAsync( "TransDic" ,"Make a new Trans: "+tas.GetTransInfo(),LogCodeEnum.Note);
                    }
                }
                catch (Exception ze)
                {
                    await RunCenter.AddLogAsync("OpenTun", "work Tun " + nip + ":" + nippt +"  EX: "+ze.Message);
                }
            }
            else {
                if (RunCenter.DisConnectMsg.Length > 0)
                {    try{ await inc.GetStream().WriteAsync(RunCenter.DisConnectMsg); await inc.GetStream().FlushAsync(); } catch { } }
                inc.Dispose();
                await RunCenter.AddLogAsync("CheckIP", "Block Income = " + nip + ":" + nippt);
            } 
        }
        private int cjj = 0;
        private int cjj2 = 0;
        /// <summary>
        /// 循环工作
        /// </summary>
        /// <returns></returns>
        protected async ValueTask LoopWork()
        {
            while (Running)
            {
              
                cjj = await ScanListener();
                cjj+= await ScanTrans();
                if (cjj < 1)
                { await Task.Delay(70); }
                RunCenter.TransCount = Count_Trans;
                cjj2++;
                if (cjj2 > 10)
                {
                    cjj2 = 0;
                    Task.Run(async () => { await RunCenter.TryFlushWLR(); });
                }
            }
            await Task.Delay(24);
            Dispose();
        }
    }

    /// <summary>
    /// 侦听器 
    /// </summary>
    public class  TcpListenerItem:IDisposable
    {
        public bool Running { get; private set; } = false;
        protected TcpListener Listener;
        public TunSet TunSetting;
        public int ID { get; private set; } = 0;
        public TcpListenerItem(TunSet ts )
        {
            ID = Trans.NextID();
            TunSetting = ts;
            Listener = new TcpListener(IPAddress.Parse(ts.InAddress), ts.InPort);
            Listener.Server.LingerState = new LingerOption(true, 1);
            Listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        }
        /// <summary>
        /// 尝试检查并取出接入的连接
        /// </summary>
        /// <returns></returns>
        public (bool suc, TcpClient? newInCome) GetInCome()
        {
            if (Running) {

                try {
                    if (Listener.Pending())
                    {
                        TcpClient a = Listener.AcceptTcpClient();
                        return (true, a);
                    }
                   
                }
                catch (Exception ze)
                {
                    RunCenter.AddLog("GetInCome", TunSetting.GetRoadInfo() + " Error: " + ze.Message);
                    Dispose();
                } 
            }
            
            return (false, null);
        }
        /// <summary>
        /// 启动接收工作
        /// </summary>
        /// <returns></returns>
        public (bool suc, string info) Start()
        {
            if (Running) { return (false, "is Running"); }
            try
            {
                Listener.Start();
                RunCenter.AddLog("Listen", "StartListener [" + TunSetting.GetRoadInfo() + "]  OK" , LogCodeEnum.Note);
                Running = true;
                return (true, "");
            } catch (Exception ze)
            { RunCenter.AddLog("Listen", "StartListener ["+TunSetting.GetRoadInfo()+"]  Error :" + ze,LogCodeEnum.Warning);
                return (false,  ze.Message);
            } 
        }
        public async ValueTask Stop()
        {
            Running = false;
            await Task.Delay(10);
            Dispose();
        }
        public void Dispose()
        {
            Running = false;
            try
            {
                Listener.Stop();
                Listener.Dispose();
            }
            catch { }
            RunCenter.AddLog("Listener","Disposed "+ID,LogCodeEnum.Note);
        }  
    }


    /// <summary>
    /// 应用中心。静态资源和配置信息。
    /// </summary>
    public class RunCenter
    {
        /// <summary>
        /// 工作中的转发线路数量。
        /// </summary>
        public static int TransCount { get; set; }

        /// <summary>
        /// 检查IP 是否允许接入
        /// </summary>
        /// <param name="tunname">通道名</param>
        /// <param name="inIP">接入IP</param>
        /// <returns>true  允许接入，  false 拒绝</returns>
        public static async Task<bool> CheckIP(string tunname, string inIP)
        {
            return await WLR.CheckIP(tunname, inIP);
        }

        /// <summary>
        /// 运行时的白名单集合。包括固化的和动态的。
        /// </summary>
        public static WL_Runtime WLR = new WL_Runtime();
        /// <summary>
        /// 拒绝语。 留空就不会发送拒绝信息而直接挂断。
        /// </summary>
        public static byte[] DisConnectMsg = new byte[0];
        /// <summary>
        /// 尝试刷新白名单缓存，内部有间隔控制。 32秒内不会重复执行。
        /// </summary>
        /// <returns></returns>
        public static async ValueTask TryFlushWLR()
        {
            try
            {
                await WLR.ReGetWLRuntime();
                await WLR.Flush();
            }
            catch (Exception zz)
            {
                await RunCenter.AddLogAsync("TryFlushWLR", "TryFlushWLR Error : " + zz.Message, LogCodeEnum.Alert);
            }
        }

        /// <summary>
        /// BLADE LOGER 
        /// </summary>
        public static BLADE.TOOLS.LOG.Loger? CLOG = null;

        /// <summary>
        /// 从配置文件取出的 各种设置。
        /// </summary>
        public static GateSettings? Settings = null;

        /// <summary>
        /// 应用启动的路径。
        /// </summary>
        public static string AppStartPath { get; set; } = "";

        /// <summary>
        /// 是否完成初始化？
        /// </summary>
        public static bool Inited { get; private set; } = false;

        /// <summary>
        /// 修改运行模式，直接影响日志的详细记录情况。其他部分看情况使用。
        /// </summary>
        /// <param name="debug"></param>
        public static void ChangeDebugMode(bool debug)
        {
            Settings?.WebSettings.EnableDeBug = debug;
            CLOG?.Debug = debug;
        }

        /// <summary>
        /// 保存配置到文件。
        /// </summary>
        /// <param name="settingfile"></param>
        /// <returns></returns>
        public static async Task<bool> SaveSettingsToFile(string settingfile)
        {
            try
            {
                var se = BLADE.TOOLS.BASE.Json.JsonOptions.Serialize<GateSettings>(Settings);
                if (se != null && se.Length > 5)
                {
                    using (StreamWriter sr = System.IO.File.CreateText(settingfile))
                    { await sr.WriteAsync(se); }  return true;
                } 
            }
            catch { }
            return false;
        }

        /// <summary>
        /// 从文件加载配置信息。应该在初始化时使用。 运行时加载需要处理可能存在的冲突。
        /// </summary>
        /// <param name="settingsFile">配置文件。</param>
        /// <returns></returns>
        private static async Task<Result> LoadSettings(string settingsFile)
        {
            Result R = new Result(false, "null", null);
            bool isnull = true;
            try
            {
                if (System.IO.File.Exists(settingsFile))
                {
                    using (StreamReader sr = System.IO.File.OpenText(settingsFile))
                    {
                        string text = await sr.ReadToEndAsync();
                        if (text.Length > 5)
                        {
                            var se = BLADE.TOOLS.BASE.Json.JsonOptions.Deserialize<GateSettings>(settingsFile);
                            if (se != null)
                            {
                                Settings = se;
                                R = new Result(true, "加载配置文件成功", Settings); isnull = false;
                            }
                        }
                    }
                }
                else
                {
                    Settings = new GateSettings();
                    Settings.AdminUser.UserID =0;
                    Settings.AdminUser.UserName = "BladeAdmin";
                    Settings.AdminUser.RealName = "BladeAdmin";
                    Settings.AdminUser.ShowName = "BladeAdmin";
                    Settings.AdminUser.SetCryptPass("BladePass");
                    Settings.OperUser.UserID = 100;
                    Settings.OperUser.UserName = "BladeOper";
                    Settings.OperUser.RealName = "BladeOper";
                    Settings.OperUser.ShowName = "BladeOper";
                    Settings.OperUser.SetCryptPass("BladePass");
                    if(await SaveSettingsToFile(settingsFile))
                    {
                        R = new Result(true, "配置文件不存在，已使用默认数据创建配置文件", Settings); isnull = false;
                    }
                }
            }
            catch (Exception ze)
            {
                R = new Result(false, "加载配置文件异常：" + ze.Message, null); isnull = false;
            }
            if (isnull) { R = new Result(false, "未能正确加载配置文件，也未能完成使用默认数据创建配置文件", null); isnull = false; }
            return R;
        }
        /// <summary>
        ///  初始化工作。
        /// </summary>
        /// <param name="startPth"></param>
        /// <returns></returns>
        public static async Task<Result<GateSettings>> Init(string startPth)
        { 
            Result<GateSettings>? RR = null;
            startPth = startPth.Trim();
            if (startPth.EndsWith("\\") || startPth.EndsWith("/")) { }
            else { startPth = startPth + "/"; }
            AppStartPath = startPth;
            string setFile = AppStartPath + "HomeGateSettings.cfg";
            var a = await LoadSettings(setFile);
            if (!a.Successful)
            { RR = new Result<GateSettings>(false, "LoadSettings: " + setFile + " Error: " + a.Message, null); }
            else
            {
                Settings = (GateSettings)a.DataOrSender;
                CLOG = new Loger(AppStartPath + Settings.WebSettings.logsubdir + "/", "HG_", true, 500, 300, "log");
                CLOG.Debug = Settings.WebSettings.EnableDeBug;
                await CLOG.AddLogAsync(LogCodeEnum.App, "InitAndStart", "Load Settings File OK : " + setFile);
                RR = new Result<GateSettings>(true, "InitAndStart OK", Settings);
                BLADE.MSGCORE.ClientTools.ClientCore.RunSet = Settings.ClientSettings;
                if (Settings.WhiteListSettings.WL_Locals.Length > 0)
                { WLR.AddWL_Locals(Settings.WhiteListSettings.WL_Locals); }
                if (Settings.DisConnectMsg.Length > 0)
                {
                    DisConnectMsg = Encoding.UTF8.GetBytes(Settings.DisConnectMsg);
                }
            }
            Inited = RR.Successful;
            return RR;
        }

        /// <summary>
        /// 添加日志  同步方式。
        /// </summary>
        /// <param name="title"></param>
        /// <param name="msg"></param>
        /// <param name="code"></param>
        public static void AddLog(string title, string msg, LogCodeEnum code = LogCodeEnum.Debug)
        {
            if (CLOG != null)
            {
                if (code != LogCodeEnum.Debug || CLOG.Debug) { CLOG.AddLog(code, title, msg); }
            }
        }
        /// <summary>
        ///  添加日志  异步方式。
        /// </summary>
        /// <param name="title"></param>
        /// <param name="msg"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        public static async ValueTask AddLogAsync(string title, string msg, LogCodeEnum code = LogCodeEnum.Debug)
        {
            if (CLOG != null)
            {
                if (code != LogCodeEnum.Debug || CLOG.Debug) { await CLOG.AddLogAsync(code, title, msg); }
            }
        }

        /// <summary>
        /// 判断一个地址是 IP地址  地址段  或需要解析的 域名。
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static AddressType GetAddressType(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return AddressType.Invalid;
            }

            input = input.Trim().ToLower();

            // Check for IPv4
            if (IPAddress.TryParse(input, out var ipAddress))
            {
                return ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork
                    ? AddressType.IPv4
                    : AddressType.IPv6;
            }

            // Check for CIDR (e.g., 192.168.0.0/16)
            if (Regex.IsMatch(input, @"^(\d{1,3}\.){3}\d{1,3}\/\d{1,2}$"))
            {
                var parts = input.Split('/');
                if (parts.Length == 2 && IPAddress.TryParse(parts[0], out _) && int.TryParse(parts[1], out var prefix) && prefix >= 0 && prefix <= 32)
                {
                    return AddressType.CIDR;
                }
            }

            // Check for domain name
            string luo = input.Replace("mkv:", "").Replace("dns:", "");
            if (Regex.IsMatch(luo, @"^([allbody-zA-Z0-9-]+\.)+[allbody-zA-Z]{2,}$"))
            {   return AddressType.Domain;   }

            return AddressType.Invalid;
        }

        /// <summary>
        /// 判定 body 中是否包含 val地址。（包括地址段判断）
        /// </summary>
        /// <param name="body">解析结果全文</param>
        /// <param name="val">外来地址</param>
        /// <returns></returns>
        public static bool PanInclude(string body, string val)
        {
            val = val.Trim().ToLower();
            body = body.Trim().ToLower();

            //   return body.Contains(val);

            string[] bs = body.Split(new string[] { ";", " ", "#" }, StringSplitOptions.RemoveEmptyEntries);
            for (int z = 0; z < bs.Length; z++)
            {
                if (BLADE.TOOLS.NET.IPTools.ContainsIP(bs[z], val))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 从一个字符串中取出分隔符的前部，或后部。
        /// </summary>
        /// <param name="allbody">文本</param>
        /// <param name="spl">分隔符</param>
        /// <param name="getFrontPerfix">默认 true  返回分隔符的前段。  false 返回分隔符的后段。</param>
        /// <returns></returns>
        public static string SplString(string allbody, string spl, bool getFrontPerfix=true)
        {
            if (string.IsNullOrEmpty(spl)) return allbody;

            int index = allbody.IndexOf(spl);
            if (index == -1) return allbody;

            return getFrontPerfix ? allbody.Substring(0, index) : allbody.Substring(index + spl.Length);
        }

    }

    /// <summary>
    /// Gate 服务的整体配置信息
    /// </summary>
    public class GateSettings
    {
        public BLADE.TOOLS.WEB.Razor.UserData.IUserDataSession AdminUser { get; set; } = TOOLS.WEB.Razor.UserData.IUserDataSession.CreateBase();
        public BLADE.TOOLS.WEB.Razor.UserData.IUserDataSession OperUser { get; set; } = TOOLS.WEB.Razor.UserData.IUserDataSession.CreateBase();
        /// <summary>
        /// 建议的应用程序线程池设置。
        /// </summary>
        public ushort ThreadsMax { get; set; } = 256;
        /// <summary>
        /// 建议的应用程序线程池设置。 
        /// </summary>
        public ushort ThreadsMin { get; set; } = 24;
        /// <summary>
        /// 启用web管理界面。
        /// </summary>
        public bool EnableWeb { get; set; } = true;
        /// <summary>
        /// 拒绝连接的警告信息，留空则不发送任何信息直接断开连接
        /// </summary>
        public string DisConnectMsg { get; set; } = "";
        /// <summary>
        /// 允许的最大连接数。超出则直接拒绝新连接。
        /// </summary>
        public ushort MaxConnection { get; set; } = 200;
        /// <summary>
        /// 转发通道闲置时间  单位毫秒。 默认24秒。
        /// </summary>
        public ushort IdelBreakMilliseconds { get; set; } = 24000;

        /// <summary>
        /// MSGCORE 接口 客户端配置。包括服务地址，认证信息等。
        /// </summary>
        public BLADE.MSGCORE.ClientTools.ProSettings ClientSettings { get; set; } = new MSGCORE.ClientTools.ProSettings();
        /// <summary>
        /// web 管理界面的配置信息。包括web服务端口，日志信息，debug等。
        /// </summary>
        public BLADE.TOOLS.WEB.WorkSetting WebSettings { get; set; } = new WorkSetting();

        /// <summary>
        /// 转发通道配置信息
        /// </summary>
        public TunTransSettings TunSettings { get; set; } = new TunTransSettings();
        /// <summary>
        /// 白名单本地配置信息。将通过配置文件加载，此部分配置信息将在应用运行时保持。
        /// </summary>
        public WL_Local_Settings WhiteListSettings { get; set; } = new WL_Local_Settings();
    }
    /// <summary>
    /// 管道 Tao ，将TcpIn 读取的数据转发入 TcpOut 。 可分别承担上下行转发。
    /// </summary>
    public class Tao
    {

        /// <summary>
        /// 流入端
        /// </summary>
        public TcpClient TcpIn;
        /// <summary>
        /// 流出端
        /// </summary>
        public TcpClient TcpOut;
        /// <summary>
        /// true = UP W2N ||  false = Down N2W
        /// </summary>
        public bool Arr = true;

        public int BagSize = 1400;

        public long TransBytesCount = 0;
        public byte[] buf = new byte[0];

        /// <summary>
        /// 转发流量， 此方法内不设trycatch ，请在调用处处理异常，以便得到正确的断开信号。
        /// </summary>
        /// <returns></returns>
        public async ValueTask<int> TransWork()
        { 
                if (TcpIn.Available > 0)
                {
                    var rd = await TcpIn.GetStream().ReadAsync(buf);
                    if (rd > 0)
                    { await TcpOut.GetStream().WriteAsync(buf, 0, rd); TransBytesCount += rd; }
                    return rd;
                }
                return 0; 
        }

    }

    /// <summary>
    /// 运行时的 转发管道
    /// </summary>
    public class Trans :IDisposable
    {
        public static int NextID()
        {
            if (_idseed > 8000000) { _idseed = 1; }
            _idseed += 3; 
            return _idseed + 1;
        }
        private static int _idseed = 0;
        public bool disposed = false;
        public Tao Tao_W2N;
        public Tao Tao_N2W;
        public DateTime StartUTC= TimeProvider.UtcNow;
        public DateTime LastActUTC = TimeProvider.UtcNow;
        public TcpClient TcpW;
        public TcpClient TcpN;
        public TunSet TunSetting;
        public long LinkTimeSeconds
        {
            get
            {
                return (long)(TimeProvider.UtcNow - StartUTC).TotalSeconds;
            }
        }
        public Trans(TcpClient tw, TcpClient tn, TunSet ts)
        {
            TcpW = tw; TcpN = tn; TunSetting = ts;
            ID = NextID();
            //TcpW.ReceiveBufferSize = TunSetting.MTUSize * 3;
            //TcpN.ReceiveBufferSize = TunSetting.MTUSize * 3;
            //TcpW.SendBufferSize = TunSetting.MTUSize * 4;
            //TcpN.SendBufferSize = TunSetting.MTUSize * 4;
            Tao_W2N = new Tao();
            Tao_N2W = new Tao();
            Tao_W2N.Arr = true;
            Tao_N2W.Arr = false;
            Tao_N2W.BagSize = TunSetting.MTUSize;
            Tao_N2W.TcpIn = TcpN; Tao_N2W.TcpOut = TcpW;
            Tao_N2W.buf= new byte[ (int)(TunSetting.MTUSize*1.2)];
            Tao_W2N.BagSize = TunSetting.MTUSize;
            Tao_W2N.TcpIn = TcpW; Tao_W2N.TcpOut = TcpN;
            Tao_W2N.buf = new byte[(int)(TunSetting.MTUSize * 1.2)];
        }
        public int ID { get; private set; } = 0;

        private DateTime speedTime = TimeProvider.UtcNow;
        private long speedW2N = 0;
        private long speedN2W = 0;
        private int rd1 = 0;
        private int rd2 = 0;
        private int wavetmp = 0;
        public string Speed { get; set; } = "";
        /// <summary>
        /// 实际的转发工作， 内部有速度和间歇延迟控制。
        /// </summary>
        /// <returns></returns>
        protected async ValueTask<bool> transWork()
        {
            if (!disposed)
            {
                try
                {
                    rd1 = await Tao_N2W.TransWork(); speedN2W += rd1;
                    rd2 = await Tao_W2N.TransWork(); speedW2N += rd2;
                    wavetmp++;
                    if ((rd1 + rd2) > 0) { LastActUTC = TimeProvider.UtcNow;  } else { await Task.Delay(24); }
                    if (wavetmp > 10)
                    {
                        wavetmp = 0;
                        if ((TimeProvider.UtcNow - speedTime).TotalMilliseconds > 2000)
                        {
                            Speed = "IN "+ (speedW2N / 2048).ToString("F1")+" KB / OUT "+(speedN2W/2048).ToString("F1")+" KB";
                            if (RunCenter.TransCount > 2 && ((speedN2W / 512) > TunSetting.SpeedMax || (speedW2N / 512) > TunSetting.SpeedMax))
                            {
                                await Task.Delay(200);
                            }
                            speedW2N = 0;
                            speedN2W = 0;
                            speedTime = TimeProvider.UtcNow;
                        }
                    }
                    return true;
                }
                catch (Exception ze) { try { await RunCenter.AddLogAsync("transWork", "Error to Break : " + ze.Message); } catch { }
                    return false;
                }
            }
            return false;
        }
        
        /// <summary>
        /// 循环工作
        /// </summary>
        /// <returns></returns>
        protected async ValueTask LoopWork()
        {
            while (!disposed) {
                if (await transWork())
                { }
                else { break; }
            }

            try {
                Dispose();
                try { await RunCenter.AddLogAsync("Trans", "TransLoopWork Out while : " +ID, LogCodeEnum.Debug); } catch { }
            } catch(Exception ze) { try { await RunCenter.AddLogAsync("Trans", "TransLoopWork Stoped EX:" + ze.Message, LogCodeEnum.Warning); } catch { } }
        }
        public async ValueTask StartWork()
        {
             _= Task.Run(async() => { await LoopWork(); });
        }
        public string GetTransInfo()
        {
            return  TunSetting.GetRoadInfo()+ " ("+ID+") = [ W2N: " + (Tao_W2N.TransBytesCount/1024).ToString("F2") + " KB ][ N2W: " + (Tao_N2W.TransBytesCount/1024).ToString("F2") + " KB ] " + Speed;
        }
        public void Dispose()
        {
            disposed = true;
            try
            {
                TcpW?.Dispose();
            }
            catch { }
            try
            {
                TcpN?.Dispose();
            }
            catch { }
        }
    }
    /// <summary>
    /// 管道配置信息
    /// </summary>
    public class TunSet
    {
        /// <summary>
        /// 转发名称
        /// </summary>
        public string TunName { get; set; } = "DEF-2000-2222";
        /// <summary>
        /// 转发管道的  侦听端口 
        /// </summary>
        public int InPort { get; set; } = 2000;
        /// <summary>
        /// 转发管道的  侦听地址   默认 0.0.0.0
        /// </summary>
        public string InAddress { get; set; } = "0.0.0.0";
        /// <summary>
        /// 转发管道的  目标  远端端口
        /// </summary>
        public int OutPort { get; set; } = 2222;
        /// <summary>
        /// 转发管道的  目标  远端地址
        /// </summary>
        public string OutAddress { get; set; } = "192.168.100.100";
        public int MTUSize { get; set; } = 1400;
        /// <summary>
        /// 设置此管道是否应用名单规则  具体的应用规则由 Settings.RunWithWhiteOrBlack 决定。 true = 应用   false = 不应用规则，直通
        /// </summary>
        public bool UseRule { get; set; } = false;

        /// <summary>
        /// 速度限制   单向限制 单位 KB
        /// </summary>
        public int SpeedMax { get; set; } = 1024;

        /// <summary>
        /// 连接限制  每个通道单独配置 除白名单模式外  超过限制则封锁到灰名单
        /// </summary>
        public int LockCount { get; set; } = 9;
        
        /// <summary>
        /// 转发说明
        /// </summary>
        public string GetRoadInfo()
        { 
            return InAddress + ":" + InPort.ToString() + " TO " + OutAddress + ":" + OutPort.ToString() + " R_" + UseRule.ToString();
        }
    }

    /// <summary>
    /// 管道配置集合
    /// </summary>
    public class TunTransSettings
    {
        public TunSet[] Tuns { get; set; }
        public TunTransSettings()
        {
            Tuns = new TunSet[2];
            Tuns[0] = new TunSet();
            Tuns[0].TunName = "DefCreate_0";
            Tuns[0].InPort = 2221;
            Tuns[0].SpeedMax = 1200;
            Tuns[0].UseRule = true;

            Tuns[1] = new TunSet();
            Tuns[1].TunName = "DefCreate_1";
            Tuns[1].InPort = 2223;
            Tuns[1].SpeedMax = 850;
            Tuns[1].UseRule = false;
        }
    }

    #region  WhiteList Classes

    /// <summary>
    ///  白名单运行库。 内部缓存白名单信息，并间隔刷新解析结果。
    /// </summary>
    public class WL_Runtime
    { 
      
        protected Dictionary<string, List<string>> Locals = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        protected Dictionary<string, List<string>> RunTimes = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        protected BLADE.TOOLS.BASE.ThreadSAFE.Dictionary_TS<string, WL_Item> KVS = new BLADE.TOOLS.BASE.ThreadSAFE.Dictionary_TS<string, WL_Item>(StringComparer.OrdinalIgnoreCase);
        protected Lock _lk = new Lock();
        public WL_Runtime()
        { }
        public WL_Runtime(  WL_Local[] locs)
        {
            AddWL_Locals(locs);
        }
        /// <summary>
        /// 添加本地白名单配置，添加前会先尝试解析地址，所以本操作是准同步的。会消耗一定时间。
        /// </summary>
        /// <param name="locs">本地白名单配置。</param>
        /// <returns></returns>
        public int AddWL_Locals(WL_Local[] locs)
        {
            int a = 0;
            if (locs != null && locs.Length > 0)  
            {
                lock (_lk)
                {
                    foreach (var i in locs)
                    {
                        foreach (var ap in i.AddressOrIPCD)
                        {
                            WL_Item wi = new WL_Item(ap);
                            if (wi.TagType == AddressType.Domain)
                            {
                                Task.Run(() => FlushBack_work(wi));
                            }
                            // wi.Name = WL_Name;
                            // wi.AddressOrIPCD = ap.Trim().ToLower();
                            if (wi.TagType != AddressType.Invalid)
                            {
                                if (Locals.ContainsKey(i.WL_Name))
                                { }
                                else { Locals.Add(i.WL_Name, new List<string>()); }
                                var ll = Locals[i.WL_Name];
                                if (ll.Contains(wi.AddressOrIPCD))
                                { }
                                else { ll.Add(wi.AddressOrIPCD); a++; }

                                if (KVS.ContainsKey(wi.AddressOrIPCD)) { } else { KVS.Add(wi.AddressOrIPCD, wi); }
                            }

                        }
                    }
                }
            }
            return a;
        }

        /// <summary>
        /// 添加运行时白名单，来源是 MSGCORE 接口获取的动态白名单。
        /// 本方法是添加，不会去重已有的运行时白名单。
        /// 所以当需要重新刷新时，需要先清楚已经存在的通道白名单。
        /// </summary>
        /// <param name="tunname">通道名</param>
        /// <param name="rims">运行时白名单数组。</param>
        /// <returns></returns>
        public async Task<int> AddWL_Runtimes(string tunname,  WL_Item[] rims)
        {
            int a = 0;
            tunname = tunname.Trim().ToLower();
            if (tunname.Length < 1  ) { return a; }
            if (rims != null && rims.Length > 0)
            {
                for (int w = 0; w < rims.Length; w++)
                {
                    if (rims[w].TagType == AddressType.Domain)
                    {
                        await FlushBack_work(rims[w]);
                    }
                }

                lock (_lk) {
                    if (RunTimes.ContainsKey(tunname))
                    { }
                    else { RunTimes.Add(tunname, new List<string>()); }
                    var ll = RunTimes[tunname];
                    foreach (var i in rims)
                    {
                        if (i.TagType != AddressType.Invalid)
                        {
                            if (ll.Contains(i.AddressOrIPCD))
                            { }
                            else { ll.Add(i.AddressOrIPCD); a++; }
                            if (KVS.ContainsKey(i.AddressOrIPCD)) { } else { KVS.Add(i.AddressOrIPCD, i); }
                        }
                    }
                }
            }
            return a;
        }

        public async Task<bool> AddWL_Runtime(string tunname, string addressorIPCD)
        {
            tunname = tunname.Trim().ToLower();
            addressorIPCD = addressorIPCD.Trim().ToLower();
            if (tunname.Length < 1 || addressorIPCD.Length<1) { return false; }
            var i = new WL_Item(addressorIPCD);
            if(i.TagType == AddressType.Domain){ await FlushBack_work(i); }
            if (i.TagType == AddressType.Invalid) { return false; }
            lock (_lk)
            {
                if (RunTimes.ContainsKey(tunname))
                { }
                else { RunTimes.Add(tunname, new List<string>()); }
                var ll = RunTimes[tunname];
                
                if (ll.Contains(i.AddressOrIPCD))
                { }
                else { ll.Add(i.AddressOrIPCD); }
                if (KVS.ContainsKey(i.AddressOrIPCD)) { } else { KVS.Add(i.AddressOrIPCD, i); return true; }
            }
            return false;
        }
        /// <summary>
        /// 解析IP地址。 
        /// 输入 mkv:domian.com 模式会尝试从 MSGCORE 提取值， 
        /// 输入 dns:domain.com / domain.com 模式会使用dns解析，
        /// 输入  192.168.1.5 的IP地址模式dns 会直接返回原地址。
        /// 输入其他模式会返回空字符串，视作无效地址。
        /// </summary>
        /// <param name="input">需要判断是否在白名单之内的外来地址。</param>
        /// <returns>返回的地址可能是单个地址/地址段，也可能是多个。
        /// 当DNS会有多个解析结果时，结果是合并的地址字符串。
        /// 所以判断是否包含需要拆解，不能简单用相等判断。需要使用PanInclude判断</returns>
        public static async Task<string> GetIP(string input)
        {
            if (input.StartsWith("mkv:"))
            {
                string luo = input.Replace("mkv:", "");
                return await mkvIP(luo);
            }
            else {
                string ld = input.Replace("dns:", "");
                return await dnsIP(ld);
            }
        }
        protected static async Task<string> mkvIP(string ip)
        {
            try
            {
                var rr = await BLADE.MSGCORE.ClientTools.ClientCore.MKV_CreateQryPost(0, ip);
                if (rr != null && rr.StatusCode == 200)
                {
                    var pmi = BLADE.TOOLS.BASE.Json.JsonOptions.Deserialize<PostResponse>(rr.ResponseText);
                    if (pmi != null && pmi.secKEY == 99999999)
                    {
                        string jsonPostMkvItem = Encoding.UTF8.GetString(Convert.FromBase64String(pmi.Message));
                        var mi = BLADE.TOOLS.BASE.Json.JsonOptions.Deserialize<PostMkvItem>(jsonPostMkvItem);
                        if (mi != null && mi.KEYVALUE.Length > 0)
                        {
                            await RunCenter.AddLogAsync("MkvDNAME", "mkv: " + mi.KEYNAME + " = " + mi.KEYVALUE, TOOLS.LOG.LogCodeEnum.Debug);
                            return mi.KEYVALUE.Trim();
                        }
                    }
                    await RunCenter.AddLogAsync("MkvDNAME", "MKV get Null : " + ip, TOOLS.LOG.LogCodeEnum.Note);
                }
            }
            catch (Exception ze)
            { await RunCenter.AddLogAsync("MkvDNAME", "mkv Error: " + ip + " EX: " + ze.Message, TOOLS.LOG.LogCodeEnum.Note); }
            await RunCenter.AddLogAsync("MkvDNAME", "mkv : " + ip + " No Address.", TOOLS.LOG.LogCodeEnum.Debug);
            return "";
        }
        protected static async Task<string> dnsIP(string ip)
        {
            try
            {
                IPHostEntry IPinfo = Dns.GetHostEntry(ip);

                if (IPinfo.AddressList.Length > 0)
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (var ii in IPinfo.AddressList)
                    {
                        sb.Append(ii + "  ");
                    }
                    string nn = sb.ToString();
                    await RunCenter.AddLogAsync("DnsDNAME", "dns: " + ip + " = " + nn, TOOLS.LOG.LogCodeEnum.Debug);
                    return nn;
                }
            }
            catch (Exception ze) {
                await RunCenter.AddLogAsync("DnsDNAME", "dns Error: " + ip +" EX: "+ze.Message, TOOLS.LOG.LogCodeEnum.Note);
            }
            await RunCenter.AddLogAsync("DnsDNAME", "dns : " + ip +" No Address." , TOOLS.LOG.LogCodeEnum.Debug);
            return "";
        }

        /// <summary>
        /// 检查外来IP，是否在指定通道的白名单之内。
        /// </summary>
        /// <param name="tunname">管道名</param>
        /// <param name="inIP">外来地址</param>
        /// <returns>true 在白名单内，通过检查。  false 应该拒绝</returns>
        public async Task< bool> CheckIP(string tunname, string inIP)
        {
            tunname=tunname.Trim().ToLower();
            inIP = inIP.Trim();

            if (tunname.Length < 1 || inIP.Length < 3) { return false; }
            if (RunCenter.GetAddressType(inIP) == AddressType.Invalid)  { return false; }

            bool fd = false;
            if (Locals.ContainsKey(tunname))
            {
                var ll = Locals[tunname];
                
                if (ll.Contains(inIP))
                { fd = true; }
                else
                {
                    foreach (var ts in ll)
                    { 
                        if (KVS.ContainsKey(ts))
                        {
                            var wi = KVS[ts];
                            if (wi.TagType == AddressType.CIDR && BLADE.TOOLS.NET.IPTools.ContainsIP(wi.AddressOrIPCD, inIP))
                            {
                                fd = true; break;
                            }
                            if (wi.TagType == AddressType.Domain)
                            {
                                if (RunCenter.PanInclude(wi.ADVALUE.ToLower(), inIP)) { fd = true;   } 
                                if (!_flushing&&  (TimeProvider.UtcNow - wi.FlashUTC).TotalSeconds > 20 )
                                    { Task.Run(() => FlushBack_work(wi)); }
                                if (fd) { break; }
                            }
                        } 
                    } 
                }
            }
            if (fd == false)
            {
                if (RunTimes.ContainsKey(tunname))
                {
                    var ll = RunTimes[tunname]; 
                    if (ll.Contains(inIP))
                    { fd = true; }
                    else
                    {
                        foreach (var ts in ll)
                        {

                            if (KVS.ContainsKey(ts))
                            {
                                var wi = KVS[ts];
                                if (wi.TagType == AddressType.CIDR && BLADE.TOOLS.NET.IPTools.ContainsIP(wi.AddressOrIPCD, inIP))
                                {
                                    fd = true; break;
                                }
                                if (wi.TagType == AddressType.Domain)
                                {
                                    if (RunCenter.PanInclude(wi.ADVALUE.ToLower(), inIP)) { fd = true; }
                                    if (!_flushing && (TimeProvider.UtcNow - wi.FlashUTC).TotalSeconds > 20)
                                    { Task.Run(() => FlushBack_work(wi)); }
                                    if (fd) { break; }
                                }
                            }

                        }

                    }
                }
            }

            return false;
        }

        protected DateTime _lastflush = TimeProvider.UtcNow.AddMinutes(-2);
        protected DateTime _lastWL = TimeProvider.UtcNow.AddMinutes(-2);

        private static async ValueTask<bool> FlushBack_work(WL_Item wi)
        {
            try
            {
                DateTime cc = TimeProvider.UtcNow;
                if ((cc - wi.FlashUTC).TotalSeconds > 20 || wi.TagType == AddressType.Domain)
                {
                    wi.FlashUTC = cc; wi.ADVALUE = await GetIP(wi.AddressOrIPCD);
                    return true;
                }
            }
            catch(Exception zee)  { if (RunCenter.Settings.WebSettings.EnableDeBug) { try { 
                     await RunCenter.AddLogAsync("FlushBack_work", "Ex: " + zee.Message, LogCodeEnum.Warning);
                    } catch { } } }
            return false;
        }
        protected bool _flushing = false;

        /// <summary>
        /// 如果刷新间隔超过20秒，则启动刷新工作，
        /// 尝试在KVS 中寻找域名类型的地址进行解析刷新。
        /// </summary>
        /// <returns>本次刷新，解析的条目数</returns>
        public async ValueTask<int> Flush()
        {
            if (_flushing) { return 0; }
            DateTime cc = TimeProvider.UtcNow;
            int a = 0;
            if ((cc - _lastflush).TotalSeconds > 20)
            {
                try
                {
                    _flushing = true;
                    foreach (var vs in KVS.Values)
                    {
                        if (vs != null && vs.TagType == AddressType.Domain && (cc - vs.FlashUTC).TotalSeconds > 16)
                        { Task.Run(() => FlushBack_work(vs)); a++; }
                    }
                    _lastflush = TimeProvider.UtcNow;

                }
                catch { }
                finally { _flushing = false; }
            } 
            return a;
        }
        /// <summary>
        /// 按通道名，从 MSGCORE 接口重新获取运行时白名单。
        /// </summary>
        /// <returns></returns>
        public async Task< int> ReGetWLRuntime()
        {
            if (_flushing) { return 0; }
            DateTime cc = TimeProvider.UtcNow;
            int a = 0;
            if ((cc - _lastWL).TotalSeconds > 32)
            {
                try
                {
                    _flushing = true;
                    _lastWL = TimeProvider.UtcNow;

                    foreach (var t in RunCenter.Settings.TunSettings.Tuns)
                    {
                        try
                        {
                            var adv = await LoadWLRuntime(t.TunName);
                            lock (_lk)
                            {
                                if (RunTimes.ContainsKey(t.TunName))
                                {
                                    RunTimes.Remove(t.TunName);
                                }
                            }
                            if (adv != null)
                            {
                                WL_Item[] rm = new WL_Item[adv.Length];
                                for (int z = 0; z < adv.Length; z++)
                                {
                                    rm[z] = new WL_Item(adv[z]);
                                }
                                a = a + await AddWL_Runtimes(t.TunName, rm);
                                RunCenter.AddLog("GetWLRuntime", "Work Get WL Runtime [" + t.TunName + "] Count:" + adv.Length.ToString(), LogCodeEnum.Debug);
                            }
                        }
                        catch (Exception ze)
                        {
                            await RunCenter.AddLogAsync("GetWLRuntime", "Work Get WL Runtime [" + t.TunName + "] Ex:" + ze.Message, LogCodeEnum.Warning);
                        }
                    }
                }
                catch { }
                finally { _flushing = false; }
            }
            
            return a;
        }

        protected async Task<string[]> LoadWLRuntime(string tunname)
        {
            string[] a= new string[0];
            try {
                var rr = await BLADE.MSGCORE.ClientTools.ClientCore.WL_CreatePost(tunname, 10,"WLQRY");
                if (rr.Item1 != null || rr.Item1.StatusCode == 200)
                {
                    string jsn = rr.Item1.ResponseText;
                    var wr = BLADE.TOOLS.BASE.Json.JsonOptions.Deserialize< BLADE.MSGCORE.Models.WL_Response> (jsn);
                    if (wr.StateCode == 200)
                    {
                        a = new string[wr.ADDRS.Length];
                        for (int z = 0; z < wr.ADDRS.Length; z++)
                        {
                            a[z] = RunCenter.SplString(wr.ADDRS[z].Trim(), "##", true).Trim();
                        }
                        await RunCenter.AddLogAsync("LoadWLRuntime", "Work WLREQ [" + tunname + "] OK : " + a.Length, LogCodeEnum.Debug);
                    }
                }
                else {
                    await RunCenter.AddLogAsync("LoadWLRuntime", "Work WLREQ [" + tunname + "] : " +rr.Item2 , LogCodeEnum.Debug);
                }
            }
            catch (Exception ze)
            { 
                await RunCenter.AddLogAsync("LoadWLRuntime", "Work WLREQ [" + tunname + "] Ex:" + ze.Message, LogCodeEnum.Warning);
            }
            return a;
        }
    }
    /// <summary>
    /// 白名单条目
    /// </summary>
    public class WL_Item
    {
        /// <summary>
        /// 原始地址名：   地址，域名，地址段  
        /// </summary>
        public string AddressOrIPCD { get; set; } = "";
        /// <summary>
        /// 地址，地址段，或通过mkv/dns解析出来的地址。
        /// </summary>
        public string ADVALUE { get; set; } = "";
        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime FlashUTC { get; set; } = TimeProvider.UtcNow.AddMinutes(-30);
        /// <summary>
        /// 本项目的 原始地址明类型。 当前支持 IPv4， IPv6， CIDR， Domain 四种类型。默认是 Invalid 无效。
        /// Domain 时， 需要 dns/mkv 解析出实际地址后，存入 ADVALUE 字段。
        /// </summary>
        public AddressType TagType { get; set; } = AddressType.Invalid;
        public WL_Item(string addressOrIPcd)
        {
            AddressOrIPCD = addressOrIPcd.Trim().ToLower();
            TagType = RunCenter.GetAddressType(AddressOrIPCD);
            ADVALUE = AddressOrIPCD;
        }
    }

    /// <summary>
    /// 配置文件中的 本地白名单条目
    /// </summary>
    public class WL_Local
    { 
       public string WL_Name { get; set; } = "default";
       public string[] AddressOrIPCD { get; set; } = new string[] {"127.0.0.1","192.168.1.0/24","mkv:h.mlez.net","n.dmrham.com" };
    }
    /// <summary>
    /// 本地配置文件中的白名单集合
    /// </summary>
    public class WL_Local_Settings
    {
        public WL_Local[] WL_Locals { get; set; }

        public WL_Local_Settings()
        {
            WL_Locals = new WL_Local[2];
            WL_Locals[0] = new WL_Local();
            WL_Locals[1] = new WL_Local();
            WL_Locals[0].WL_Name = "home";
            WL_Locals[1].WL_Name = "pve";
        }
    }

    /// <summary>
    /// 地址类型
    /// </summary>
    public enum AddressType
    {
        Invalid,
        IPv4,
        IPv6,
        CIDR,
        Domain
    }

    #endregion
}