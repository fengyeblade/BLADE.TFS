using BLADE;
//using BLADE.MSGCORE.Models;
using BLADE.TOOLS.BASE;
using BLADE.TOOLS.BASE.ThreadSAFE;
using BLADE.TOOLS.LOG;
using BLADE.TOOLS.NET;
using BLADE.TOOLS.NET.GreenTCP;
using BLADE.TOOLS.WEB;
using BLADE.UC.Models;
using BLADE.UC.RRClientCore;
using Microsoft.EntityFrameworkCore.Update.Internal;
//using Microsoft.AspNetCore.Mvc.Formatters;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Xml;
using static BLADE.TOOLS.NET.IPGateManager;
using static System.Net.WebRequestMethods;

namespace BLADE.TFS.HOMEGATE.COMM
{
    public class ApiMsg
    {
        /// <summary>
        /// 验证用字符串
        /// </summary>
        public string SecKey { get; set; } = string.Empty;
        /// <summary>
        /// 访问API 的身份
        /// </summary>
        public string ClientName { get; set; } = string.Empty;

        /// <summary>
        /// API命令码。 见下文约定
        ///     0. Command=0 留空，视为无效信息。
        ///     1. Command=1 列出当前转发通道的状态信息。  Response 输出文本格式的转发通道列表。
        ///     2. Command=2 关闭指定的转发通道。 PerParam 需要提供 TunName 参数来指定要关闭的通道。 Response 输出操作结果文本。
        ///     3. Command=3 启动指定的转发通道。 PerParam 需要提供 TunName 参数来指定要刷新的通道。 Response 输出操作结果文本。
        ///     4. Command=4 获取当前服务使用的三个Settings，序列化给调用者。
        ///     5. Command=5 提取提交上来的Settings，保存在配置文件中。不会及时影响服务器行为，在下次启动时生效。 Response 输出操作结果文本。
        ///     6. Command=6 添加临时白名单到 IPGATE 中 。
        ///     7. Command=7 添加临时黑名单到 IPGATE 中 。
        ///     8. Command=8 从 IPGATE 中临时赦免指定地址 。
        ///     9. Command=9 尝试 IPGATE 数据重置，清理 IPGATE 的缓存层，并尝试重新加载数据库数据。
        /// </summary>
        public int Command { get; set; } = 0;
        /// <summary>
        /// 主参数 （例如查询地址的key，关闭通道的 TunName 等最直观参数）
        /// </summary>
        public string PerParam { get; set; } = string.Empty;

        /// <summary>
        ///  可选参数。其顺序需要保证（或采用 key:value 方式的转义字符，需要解析方进行解析处理）。
        /// </summary>
        public string[] Params { get; set; } = Array.Empty<string>(); 
        /// <summary>
        /// 结果输出。
        /// </summary>
        public string Response { get; set; } = string.Empty;

       
    }
    public class GreenTcpAPI : IDisposable
    {
        private static byte[] XOR = new byte[] { 0xD9, 0x21, 0x77,0x9C ,0x29, 0xF1, 0x77};
        private static long _seed = 10000;
        public static long NextSN()
        {
            return Interlocked.Increment(ref _seed);
        }
        public struct GreenMSG
        {
            public long ClientID { get; set; } = 0;
            public string Remote { get; set; } = "";
            public string MsgJson { get; set; } = "";
            public DateTime UTC { get; set; } = BLADE.TimeProvider.UtcNow;

            public long SN { get; set; } = 0;

            public GreenMSG()
            { SN = NextSN(); }
        }
        //================================
        private HomeGateCenter? Center = null;
        private BLADE.TOOLS.NET.GreenTCP.GreenTcpServer? Server = null;
        private AtomicBoolean isPumping = new();

        /// <summary>
        /// 这个 GreenTcpApi 本身没有设置循环工作线程，因为 HomeGateCore 组件里已经有很多后台循环线程了，依靠外部触发作为引擎带动就足够了。
        /// 所以这个方法是用于外部带动循环工作的。
        /// </summary>
        public void PumpWork()
        {
            if (Disposed) { return; }
            if (isPumping.TrySetTrue())
            {
                Task.Run(async () => await pump());
            }
        }
        public bool Disposed { get; private set; } = false;
        public void Dispose()
        {
            Disposed = true;

            try
            {
                ApiGreenMSG = null;
                Server?.Dispose();
            }
            catch { }
        }
        public EventHandler<List<GreenMSG>>? ApiGreenMSG = null;
        public GreenTcpAPI(HomeGateCenter inCenter)
        {
            Center = inCenter;
            try
            {
                Server = new BLADE.TOOLS.NET.GreenTCP.GreenTcpServer(Center.Settings.GreenAPIPort, Center.Settings.GreenAPIAddress);
                Server.PingIntervalSeconds = 7;
                Server.IOTimeoutSeconds = 11;
                Server.OnClientBreak += Server_OnClientBreak;
                Server.OnClientIn += Server_OnClientIn;
                Server.OnReceviced += Server_OnReceviced;
                Server.Start();
            }
            catch (Exception ze)
            {
                _ = HomeGateCenter.AddLog("GreenTCP", "Start GreenTcpServer Error: " + ze.Message);
            }
        }

        //=============================
        /// <summary>
        /// 接收来自 GreenTCP 客户端的消息，解析出其中的有效信息，并通过 ApiGreenMSG 事件传递给外部。 这个事件的订阅者通常是 HomeGateCore 组件。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Server_OnReceviced(object? sender, (long clientid, List<GreenTcpProtocol> gtp) e)
        {
            if (Disposed) { return; }
            List<GreenMSG> msgs = new List<GreenMSG>();
            foreach (var gt in e.gtp)
            {
                if (gt.BodyType == 5 && gt.ProtocolType == 19)
                {
                    var gm = new GreenMSG();
                    gm.ClientID = e.clientid;
                    gm.MsgJson = Encoding.UTF8.GetString( BLADE.TOOLS.BASE.HASH.HashCalculator.XorCopy(  gt.GetRealBody(),XOR));
                    msgs.Add(gm);
                }
            }
            ApiGreenMSG?.Invoke(this, msgs);
        }
        private async void Server_OnClientIn(object? sender, (long id, string remote) e)
        {
            if (Disposed) { return; }
            var ip = IPEndPoint.Parse(e.remote).Address.ToString();
            if (!Center.CheckIPAllow("GreenAPI", ip)) { Server.DropCLient(e.id); return; }
            await HomeGateCenter.AddLog("GreenAPI", "InCome from " + e.remote + " ID:" + e.id);
        }
        private async void Server_OnClientBreak(object? sender, (long id, string remote) e)
        {
            if (Disposed) { return; }
            await HomeGateCenter.AddLogDEBUG("GreenAPI", "Break " + e.remote + " ID:" + e.id);
        }
        private async ValueTask pump()
        {
            try
            {
                if (Disposed) { return; }
                if (Server != null) { await Server.LoopStep(); }
            }
            catch { }
            finally { isPumping.TrySetFalse(); }
        }

        /// <summary>
        /// 上层程序向目标客户发送消息
        /// </summary>
        /// <param name="clientid"></param>
        /// <param name="msgjson"></param>
        /// <returns></returns>
        public bool Send(long clientid, string msgjson)
        {
            if (Disposed) { return false; }
            if (Server != null)
            { 
                //gtp.ProtocolType = 19;
                //gtp.BodyType = 5;
                //gtp.SetRealBody(Encoding.UTF8.GetBytes(msgjson));
                return Server.SendToClient(clientid, BLADE.TOOLS.BASE.HASH.HashCalculator.XorCopy(Encoding.UTF8.GetBytes(msgjson), XOR),ProType.Data,DataType.Bytes);
            }
            return false;
        }
    }

    /// <summary>
    /// Home Gate 转发服务器的 核心类。 
    /// </summary>
    public class HomeGateCore : IDisposable 
    { 
        protected static BLADE.TOOLS.HOTDIC.HotStringDictionary<string> routingTab = new TOOLS.HOTDIC.HotStringDictionary<string>(32, false, 16, true);

        /// <summary>
        /// 存入 反向外侧地址查询。
        /// 注意参数中 port2port 是本机转发服务的本地端口=远程端口 的格式字符串， 例如 "8080=80" 
        /// 对于被转发的业务主机就是反过来的 远端端口=本地端口 ，字符串值还是  "8080=80" ，因为它看来 8080 是远端端口。
        /// </summary>
        /// <param name="port2port">端口映射字符串，格式为 "本地端口=远程端口"</param>
        /// <param name="RealClientIPEP">真实用户的远端公网地址和端口</param>
        /// <param name="tcp">是否为TCP协议</param>
        public static void SetBackRoutingTab(string port2port, string RealClientIPEP, bool tcp = true)
        {
            port2port = port2port.ToUpper().Trim();
            RealClientIPEP = RealClientIPEP.ToUpper().Trim();
            if (string.IsNullOrEmpty(port2port) || string.IsNullOrEmpty(RealClientIPEP))
            {
                return;
            }
            string key = (tcp ? "TCP_" : "UDP_") + port2port;
            routingTab.AddOrUpdate(key, RealClientIPEP, 30);
        }

        /// <summary>
        /// 查询 反向外侧地址。 参数中 port2port 是本机转发服务的本地端口=远程端口 的格式字符串， 例如 "8080=80"
        /// 对于查询者（实际业务主机要获得真实用户IP）则需要对应提供 它自己的 远端端口=本地端口 来查询。字符串值还是  "8080=80" ，因为它看来 8080 是远端端口。
        /// </summary>
        /// <param name="port2port"></param>
        /// <param name="tcp"></param>
        /// <returns></returns>
        public static string GetBackRoutingTab(string port2port, bool tcp = true)
        {
            port2port = port2port.ToUpper().Trim();
            if (string.IsNullOrEmpty(port2port))
            {
                return "";
            }
            string key = (tcp ? "TCP_" : "UDP_") + port2port;
            var j = routingTab.TryGetValue(key, TOOLS.HOTDIC.GetAct.renew);
            if (j.suc)
            {
                return j.value;
            }
            return "";
        }
        /// <summary>
        /// 删除 反向外侧地址记录。 参数中 port2port 是本机转发服务的本地端口=远程端口 的格式字符串， 例如 "8080=80"
        /// </summary>
        /// <param name="port2port"></param>
        /// <param name="tcp"></param>
        public static void DelBackRoutingTab(string port2port, bool tcp = true)
        {
            port2port = port2port.ToUpper().Trim();
            if (string.IsNullOrEmpty(port2port))
            {
                return;
            }
            string key = (tcp ? "TCP_" : "UDP_") + port2port;
            routingTab.Remove(key);
        }

        // public static readonly CancellationTokenSource _cts = new CancellationTokenSource(600);
        //   public static readonly CancellationToken tk = _cts.Token;

        /// <summary>
        /// 将较大的数值转换为  KB  MB 形式。
        /// </summary>
        /// <param name="a"></param>
        /// <returns></returns>
        public static string GetKM(ulong a)
        {
            if (a < 1024)
            {
                return a + " B";
            }
            else if (a < 1228800)
            {
                return (a / 1024.0).ToString("0.0") + " KB";
            }
            else { 
             return (a / 1024.0 / 1024.0).ToString("0.0") + " MB";
            } 
        }

       // public static int TransCount { get; set; } = 0;
        public HomeGateCenter Center { get; private set; }
        protected UDPTransManager? UM=null;
        public void Dispose()
        {
            if (!Disposed)
            {
                Disposed = true;
                Running = false;
                // TODO : 释放资源 
              //  routingTab.Dispose();
                UM?.Dispose();
                GreenAPI?.Dispose();
               
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
                try { _= HomeGateCenter.AddLog("HomeGateCore", "Disposed  [" + mm + "]" ); } catch { }

                Center.Dispose();
            }
        }

        /// <summary>
        /// 运行状态。
        /// </summary>
        public bool Running { get; private set; } = false;
        /// <summary>
        /// 释放过的？
        /// </summary>
        public bool Disposed { get; private set; } = false;
        public HomeGateCore(string startpath)
        {
            Center = new HomeGateCenter(startpath.Trim());
        }
        protected GreenTcpAPI? GreenAPI = null;
        /// <summary>
        /// 初始化工作，并启动核心工作线程。
        /// </summary>
        /// <returns></returns>
        public async Task<(bool suc, string msg)> InitAndStart()
        {
            var j = await Center.InitReady();

            if (j.suc)
            {
                if (Center.Settings.EnableGreenAPI)
                {
                    _ = Task.Run(async () => { await StartGreenAPI(); });
                }
                if (Center.Settings.TunSettings.EnableUDP)
                {
                    _ = Task.Run(async () => { await StartUDP(); });
                }
                return  await  StartWork();
            }
           
            return (false, j.msg);
        }
        private async ValueTask StartGreenAPI()
        { 
            GreenAPI = new GreenTcpAPI(Center);
            GreenAPI.ApiGreenMSG += GreenAPI_ApiGreenMSG;
            await HomeGateCenter.AddLog("HomeGateCore", "GreenAPI Started");
        }
        private async void GreenAPI_ApiGreenMSG(object? sender, List<GreenTcpAPI.GreenMSG> e)
        {
            Task.Run(async () => { await workAPIMSG(e); });
        }
        private async Task workAPIMSG(List<GreenTcpAPI.GreenMSG> e)
        {
            foreach (var m in e)
            { await workAPIMSG(m); }
        }
        private async Task workAPIMSG(GreenTcpAPI.GreenMSG one)
        {
            if (one.MsgJson.Length > 3)
            {
                var j = BLADE.TOOLS.BASE.Json.JsonOptions.Deserialize<ApiMsg>(one.MsgJson);
                if (j != null)
                {
                    if (j.SecKey.Length < 5 || j.ClientName.Length < 5) {
                        return;
                    }
                    string r = "Unknown Command";
                    switch (j.Command)
                    {
                        case 1:
                            r = ListRuntimeTrans();
                            break;
                        case 6:
                            Center.AddIP(j.PerParam);
                            r = "Added IP " + j.PerParam+" White";
                            break;
                        case 7:
                            Center.AddIP(j.PerParam, true);
                            r = "Added IP " + j.PerParam+" Black";
                            break;
                        case 8:
                            if (j.PerParam.Length > 1) { Center.Pardon(new string[] { j.PerParam }); r = "Pardon executed "+j.PerParam; }
                            else if (j.Params.Length > 0) { Center.Pardon(j.Params); r = "Pardon executed "+j.Params.Length+" IPs"; }
                            else { r = "No IP provided for pardon."; }
                            break;
                        case 4:
                            string kk = j.PerParam.Trim().ToUpper();
                            if (kk == "HOMEGATE")
                            {
                                var h = await Center.GetSettings(true, false, false);
                                if (h.tmpGate != null)
                                {
                                    r = BLADE.TOOLS.BASE.Json.JsonOptions.Serialize(h.tmpGate);
                                }
                                else
                                {
                                    r = "Failed to retrieve HOMEGATE settings.";
                                }
                            }
                            else if (kk == "RRCORE")
                            {
                                var h = await Center.GetSettings(false, true, false);
                                if (h.tmpRRC != null)
                                {
                                    r = BLADE.TOOLS.BASE.Json.JsonOptions.Serialize(h.tmpRRC);
                                }
                                else
                                {
                                    r = "Failed to retrieve RRCORE settings.";
                                }
                            }
                            else if (kk == "IPGATE")
                            {
                                var h = await Center.GetSettings(false, false, true);
                                if (h.tmpIPG != null)
                                {
                                    r = BLADE.TOOLS.BASE.Json.JsonOptions.Serialize(h.tmpIPG);
                                }
                                else
                                {
                                    r = "Failed to retrieve IPGATE settings.";
                                }
                            }
                            else {
                                r = "UNKNOWN Settings";
                            }
                            break;
                        case 5:
                            if (j.PerParam == "UPDATESETTINGS")
                            {
                                IpGateSettingsMOD? ig = null;
                                RRCoreSettings? rr = null;
                                GateSettings? gs = null;
                                try { ig = BLADE.TOOLS.BASE.Json.JsonOptions.Deserialize<IpGateSettingsMOD>(j.Params[2]);
                                    if (ig != null)
                                    { r = r + " get IpGateSettingsMOD; "; }
                                    else { r = r + " not found IpGateSettingsMOD; "; }
                                }
                                catch {  r= r+ " error IpGateSettingsMOD; "; }

                                try
                                {
                                    rr = BLADE.TOOLS.BASE.Json.JsonOptions.Deserialize<RRCoreSettings>(j.Params[1]);
                                    if (rr != null)
                                    { r = r + " get RRCoreSettings; "; }
                                    else { r = r + " not found RRCoreSettings; "; }
                                }
                                catch { r = r + " error RRCoreSettings; "; }

                                try
                                {
                                    gs = BLADE.TOOLS.BASE.Json.JsonOptions.Deserialize<GateSettings>(j.Params[0]);
                                    if (gs != null)
                                    { r = r + " get GateSettings; "; }
                                    else { r = r + " not found GateSettings; "; }
                                }
                                catch { r = r + " error GateSettings; "; }
                                var k = await Center.SaveSettings(gs, rr, ig);
                                r = r + "  SaveSettings " +  k ;

                            }
                            else {
                                r = "WRONG update";
                            }
                            break;
                        default:
                            r = "Unknown Command";
                            break;
                    }
                    j.Response = r;
                    string repjson = BLADE.TOOLS.BASE.Json.JsonOptions.Serialize<ApiMsg>(j);
                    try
                    {
                        GreenAPI.Send(one.ClientID, repjson);
                    }
                    catch { }
                }
            }
        }
        private async ValueTask  StartUDP()
        {
            bool suc = false;  string msg = "";
            try
            {
                UM = new UDPTransManager(Center);
                UM.Start();
                suc = true;
            }
            catch (Exception tex) { msg = "StartUDP() Error: " + tex.Message; }
            await HomeGateCenter.AddLog("HomeGateCore", "StartUDP:"+suc+" "+ msg);
        }
        /// <summary>
        /// 启动时的时间。
        /// </summary>
        public DateTime StartUTC { get; private set; } = BLADE.TimeProvider.UtcNow;
        /// <summary>
        /// 启动 HOMEGATE  TCP 业务的工作循环
        ///  在 InitAndStart() 方法确定初始化完成后自动调用。
        /// </summary>
        /// <returns></returns>
        protected async Task<(bool suc,string info)> StartWork()
        {
            string err = "";
            try
            {
               
                Running = true;

                StringBuilder sb = new StringBuilder();

                // TODO: 初始化 侦听器， 初始化转发通道准备。
                lock (_lk)
                {
                    foreach (var lt in Center.Settings.TunSettings.TcpTuns)
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
                StartUTC   = BLADE.TimeProvider.UtcNow;
                // TODO: 启动工作循环线程。
                Task.Run(async () => { await LoopWork(); });
                return  (true, "GateStarted" );
            }
            catch (Exception ze)
            { err = "HomeGateCore.StartWork() Error: " + ze.Message; Running = false; }
            finally { }
            return  (false, err);
        }
        private Dictionary_TS<int, TcpListenerItem> TunDic = new Dictionary_TS<int, TcpListenerItem>( );
        private Dictionary_TS<int, TcpTrans> TransDic = new Dictionary_TS<int, TcpTrans>();
        public string ListRuntimeTrans()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("TcpTun list: "+ TunDic.Count);
            foreach (var ls in TunDic.Values)
            { sb.AppendLine(ls.ID+" "+ls.TunSetting.GetRoadInfo()+" "+ ls.Running); }
            sb.AppendLine("Cur TcpTrans: "+TransDic.Count);
            lock (_lk)
            {   foreach (var cd in TransDic.Values)  { try{ TcpTrans ttt = cd;  sb.AppendLine(ttt.GetTransInfo()); } catch { } } }

            if (UM != null)
            { 
              sb.AppendLine("Cur UdpTrans: "+ UM.TransCount);
                var a = UM.GetTransStatus();
                foreach (var u in a) {
                sb.AppendLine("UT: " + u);
                }
            }
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
            if ((TimeProvider.UtcNow - _lastscan).TotalSeconds > 3)
            {
                _stjs++;
                if (_stjs > 310)
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
                            if ((TimeProvider.UtcNow - i.LastActUTC).TotalMilliseconds > Center.Settings.TcpIdelBreakMilliseconds)
                            { i.Dispose(); jss++; ll = ll + "DisopseTrans:" + i.GetTransInfo() + "  || "; }
                            else
                            {
                                if ((_stjs % 40) == 3)
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
                    await HomeGateCenter.AddLogDEBUG("ScanTrans", ll);
                }
                if (lp.Length > 0)
                {
                    await HomeGateCenter.AddLogDEBUG("Info&Speed", lp);
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
                        if (Count_Trans >= Center.Settings.MaxConnection)
                        {
                            await HomeGateCenter.AddLog("MaxConn", "Drop TcpClient " + k.newInCome.Client.RemoteEndPoint + " Tun:" + i.TunSetting.GetRoadInfo());
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
            bool b = true;  if (tun.TunSetting.UseRule) { b = Center.CheckIPAllow(tun.TunSetting.TunName, nip); }
            if (b)
            {
                await HomeGateCenter.AddLogDEBUG("CheckIP", "Welcome = " + nip + ":" + nippt);
                try {
                    TcpClient ntc = new TcpClient(tun.TunSetting.LanAddress, tun.TunSetting.LanPort);
                    ntc.LingerState = new LingerOption(true, 1);
                    ntc.ReceiveTimeout = 33000;
                    ntc.SendTimeout = 33000;
                    ntc.ReceiveBufferSize = tun.TunSetting.MTUSize * 3;
                    ntc.SendBufferSize = tun.TunSetting.MTUSize * 3;
                    TcpTrans tas = new TcpTrans(this, inc, ntc, tun.TunSetting);
                    bool d = false;
                    lock (_lk) { if (TransDic.ContainsKey(tas.ID)) { tas.Dispose(); } else { TransDic.Add(tas.ID, tas); d = true;  } }
                    if (d) { await tas.StartWork();
                        await HomeGateCenter.AddLogDEBUG( "TransDic" ,"Make a new TCPTrans: "+tas.GetTransInfo());
                    }
                }
                catch (Exception ze)
                {
                    inc.Dispose();
                    await HomeGateCenter.AddLogDEBUG("OpenTun", "work Tun " + nip + ":" + nippt +"  EX: "+ze.Message);
                }
            }
            else {
                if (Center.DisConnectMsg.Length > 0)
                {    try{ await inc.GetStream().WriteAsync(Center.DisConnectMsg); await inc.GetStream().FlushAsync(); } catch { } }
                inc.Dispose();
                await HomeGateCenter.AddLogDEBUG("CheckIP", "Block Income = " + nip + ":" + nippt);
            } 
        }
        private int cjj = 0;
        private int cjj2 = 0;
        /// <summary>
        /// TCP 业务的 循环工作
        /// </summary>
        /// <returns></returns>
        protected async ValueTask LoopWork()
        {
            while (Running)
            {
                if (GreenAPI != null)
                {  GreenAPI.PumpWork(); }
                cjj = await ScanListener();
                cjj += await ScanTrans();
                if (cjj < 1)
                { await Task.Delay(30); }
                //HomeGateCore.TransCount = Count_Trans;
                cjj2++;
                if (cjj2 > 60)
                {
                    cjj2 = 0;

                    Task.Run(async () => { await ReportWork(); });

                }
                if (cjj2 == 13 || cjj2 == 19)
                { 
                    if ((BLADE.TimeProvider.UtcNow - _lastflushdnsred).TotalSeconds >= 200)
                    {
                        // 刷新 Tuns 转发通道中需要DNS或RED 解析的部分。解析地址后更新到 LanAddress 字段中。
                        Task.Run(async () => await FlushTuns());
                    }
                }
                if ((cjj2 % 11) == 5)
                { await Task.Delay(10); }
            }
            await Task.Delay(30);

            Dispose();
        }
        private DateTime _lastflushdnsred = BLADE.TimeProvider.UtcNow.AddMinutes(-30);

        private bool _dnsredflushing = false;
        /// <summary>
        /// 刷新 Tuns 转发通道中需要DNS或RED 解析的部分。解析地址后更新到 LanAddress 字段中。
        /// </summary>
        /// <returns></returns>
        protected async ValueTask FlushTuns()
        {
            if (_dnsredflushing) { return; }
            _lastflushdnsred = BLADE.TimeProvider.UtcNow;
            _dnsredflushing = true;

            try
            {
               
                string p = "";
                int jjj = 0;
                HashSet<string> ndm = new HashSet<string>();
                foreach (var i in Center.Settings.TunSettings.TcpTuns)
                {
                    p = i.LanDOM.Trim();
                    if (p.Length > 3)
                    {
                        ndm.Add(p);
                    }
                }
                foreach (var i in Center.Settings.TunSettings.UdpTuns)
                {
                    p = i.LanDOM.Trim();
                    if (p.Length > 3)
                    {
                        ndm.Add(p);
                    }
                }
                if (ndm.Count < 1) { return; }
                try
                {
                    if (Center.IPGM != null)
                    {
                        var dl = await Center.IPGM.DnsRedFlush(ndm.ToList());
                        var da = dl.ToArray();
                        foreach (var i in Center.Settings.TunSettings.TcpTuns)
                        {
                            p = i.LanDOM.Trim();
                            if (p.Length > 3)
                            {
                                foreach (var pd in da)
                                {
                                    if (pd.Dom == p && pd.IpAddr.Length > 0)
                                    {
                                        i.LanAddress = pd.IpAddr[0];
                                        jjj++;
                                        break;
                                    }
                                }
                            }
                        }
                        foreach (var i in Center.Settings.TunSettings.UdpTuns)
                        {
                            p = i.LanDOM.Trim();
                            if (p.Length > 3)
                            {
                                foreach (var pd in da)
                                {
                                    if (pd.Dom == p && pd.IpAddr.Length > 0)
                                    {
                                        i.LanAddress = pd.IpAddr[0];
                                        jjj++;
                                        break;
                                    }
                                }
                            }
                        }
                        await HomeGateCenter.AddLog("IPGM.DnsRedFlush", "Work update DNS-RED : " + jjj + " IpAddress");
                    }
                    else {
                        await HomeGateCenter.AddLog("IPGM.DnsRedFlush", "IPGM is null " );
                    }
                }
                catch (Exception z)
                {
                    await HomeGateCenter.AddLog("IPGM.DnsRedFlush", "WorkEx: " + z.Message);
                }
            }
            finally { _dnsredflushing = false; }
        }
        private DateTime _lassubmit1 = BLADE.TimeProvider.UtcNow.AddMinutes(-2);
        private int _substep = 0;

        /// <summary>
        /// 向 RRCore web服务汇报自身工作状态。
        /// </summary>
        /// <returns></returns>
        protected async ValueTask ReportWork()
        {
            if ((BLADE.TimeProvider.UtcNow - _lassubmit1).TotalSeconds <90)
            {
                // 90秒内不重新提交。
                return;
            }
            routingTab.ClearTimeOut();
            _lassubmit1=BLADE.TimeProvider.UtcNow;
            _substep++;

            string reptext ="{ \"HomeGate\":\""+ Center.Settings.ComName + "\", \"Running\":"+Running+ " ,  \"ListenCount\": " + TunDic.Count + " ,  \"TransCount\": " + Count_Trans + " ,  \"WorkMins\": " + (BLADE.TimeProvider.UtcNow-StartUTC).TotalMinutes+" }";
            if (Center.RRCORE != null)
            {
                if (_substep >= 5)
                {
                    //  90 * 5 秒提交一次详细的转发报告，报告会按照RRCore 的 Report 业务逻辑进行数据存档。 其他时间只提交基本的状态信息。
                    _substep = 0;
                    //int[] at = TransDic.KeysArray;
                    //if (at.Length > 0)
                    //{
                    //    StringBuilder sbb = new StringBuilder();
                    //    for (int z = 0; z < at.Length; z++)
                    //    {
                    //        if (TransDic.TryGetValue(at[z], out var t))
                    //        { sbb.AppendLine( t.GetTransInfo()); }
                    //    }
                    //    await HomeGateCenter.AddLog("TransInfo", "Name: " + Center.Settings.ComName + "\r\n" + sbb.ToString());
                    //}
                    string xiangxi = reptext + "\r\n"+ ListRuntimeTrans();
                    await HomeGateCenter.AddLog("TransInfo", xiangxi);
                    //  报告提交动作。结果成功则不产生日志。失败了会记录异常信息。
                    var a = await Center.RRCORE.HF_ApiReport_Submit(Center.Settings.ComName, xiangxi);
                    if (a.suc && a.RB != null && a.RB.StatusCode == 200)
                    {
                       
                    }
                    else
                    {
                        await HomeGateCenter.AddLog("Submit Error", "HF_ApiReport_Submit Error: " + a.msg + " _ " + a.RB?.StatusCode);
                    }
                }

                //  简单的状态信息，每间隔90秒可以向RRCore 的 comm接口提交一次。这个信息是在服务中暂存，有查阅者便提供，无查阅会自动销毁，不记录到数据库。
                var b = await Center.RRCORE.HF_ApiComm_Submit(Center.rrCoreSettings.UC_User_ORGID, ((Center.Settings.ComCode - 11) / 7), Center.Settings.ComChannel, Center.Settings.ComFreq,
                    Center.Settings.ComTarget,  Center.Settings.ComName, reptext);
                if(b.suc && b.CR!=null && b.CR.StatusCode == 200)
                { }
                else {
                    await HomeGateCenter.AddLog("Submit Error", "HF_ApiComm_Submit Error: " + b.msg + " _ " + b.CR?.StatusCode);
                }
            }

           
        }
    }

    /// <summary>
    /// 侦听器 
    /// </summary>
    public class  TcpListenerItem:IDisposable
    {

        /// <summary>
        /// 运行状态
        /// </summary>
        public bool Running { get; private set; } = false;
        protected TcpListener Listener;
        /// <summary>
        /// TCP 转发通道的配置信息。
        /// </summary>
        public TcpTunSet TunSetting;

        /// <summary>
        /// 侦听器ID，自动生成。 
        /// </summary>
        public int ID { get; private set; } = 0;
        /// <summary>
        /// 按配置构建一个新的侦听器对象。 但不启动侦听工作。 需要调用 Start() 方法来启动。
        /// </summary>
        /// <param name="ts"></param>
        public TcpListenerItem(TcpTunSet ts )
        {
            ID = TcpTrans.NextID();
            TunSetting = ts;
            Listener = new TcpListener(IPAddress.Parse(ts.WanAddress), ts.WanPort);
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
                    _= HomeGateCenter.AddLog("GetInCome", TunSetting.GetRoadInfo() + " Error: " + ze.Message);
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
                HomeGateCenter.AddLog("ListenWAN", "StartListener [" + TunSetting.GetRoadInfo() + "]  OK"  );
                Running = true;
                return (true, "");
            } catch (Exception ze)
            {
                HomeGateCenter.AddLog("ListenWAN", "StartListener ["+TunSetting.GetRoadInfo()+"]  Error :" + ze.Message);
                return (false,  ze.Message);
            } 
        }

        /// <summary>
        /// 停止并释放，异步的
        /// </summary>
        /// <returns></returns>
        public async ValueTask Stop()
        {
            Running = false;
            await Task.Delay(10);
            Dispose();
        }
        /// <summary>
        /// 停止并释放。
        /// </summary>
        public void Dispose()
        {
            Running = false;
            try
            {
                Listener.Stop();
                Listener.Dispose();
            }
            catch { }
            _= HomeGateCenter.AddLog("Listener","Disposed "+ID);
        }  
    }


    public class HomeGateCenter : IDisposable
    {
        public  async   ValueTask<string> SaveSettings(GateSettings? tmpGate=null,RRCoreSettings? tmpRRC=null,IpGateSettingsMOD? tmpIPG=null  ) 
        {
            string msg = "";
            if(tmpGate != null)
            {
                // 保存 GateSettings 的逻辑
                if (tmpGate.RRCoreCfg != Settings.RRCoreCfg)
                {
                    if (tmpRRC == null) { tmpRRC = rrCoreSettings; }
                }
                if (tmpGate.IpGateCfg != Settings.IpGateCfg)
                {
                    var t = TryGetIPGateSettings();
                    if (tmpIPG == null && t != null) { tmpIPG = t; }
                }
                string jj = BLADE.TOOLS.BASE.Json.JsonOptions.Serialize<GateSettings>(tmpGate);
                string fp = Path.Combine(AppStartPath, "HomeGateSettings.cfg");
                try
                {
                    using (var fs = System.IO.File.CreateText(fp))
                    {
                        await fs.WriteAsync(jj);
                    }
                    Settings.IpGateCfg = tmpGate.IpGateCfg;
                    Settings.RRCoreCfg = tmpGate.RRCoreCfg;
                    msg=msg+ "\r\nUpdate New HomeGateSettings saved successfully. "; 
                }
                catch (Exception ze)
                {
                      await HomeGateCenter.AddLog("SaveSettings", "Save HomeGateSettings [" + fp + "] Error: " + ze.Message);
                    msg = msg + "\r\n" + "Save HomeGateSettings Error: " + ze.Message;
                }
            }
            if(tmpRRC != null)
            {
                // 保存 rrCoreSettings 的逻辑
                string jj= BLADE.TOOLS.BASE.Json.JsonOptions.Serialize<RRCoreSettings>(tmpRRC);
                string fp = Path.Combine(AppStartPath, Settings.RRCoreCfg);
                try
                {
                    using (var fs = System.IO.File.CreateText(fp))
                    {
                        await fs.WriteAsync(jj);
                    }
                    msg = msg + "\r\nUpdate New rrCoreSettings saved successfully. ";
                }
                catch (Exception ze)
                {
                    await HomeGateCenter.AddLog("SaveSettings", "Save rrCoreSettings [" + fp + "] Error: " + ze.Message);
                    msg = msg + "\r\n" + "Save rrCoreSettings Error: " + ze.Message;
                }
               
            }
            if(tmpIPG != null)
            {
                // 保存 IpGateSettingsMOD 的逻辑
                string jj = BLADE.TOOLS.BASE.Json.JsonOptions.Serialize<IpGateSettingsMOD>(tmpIPG);
                string fp = Path.Combine( AppStartPath, Settings.IpGateCfg );
                try
                {
                    using (var fs = System.IO.File.CreateText(fp))
                    {
                        await fs.WriteAsync(jj);
                    }
                    msg = msg + "\r\nUpdate New IpGateSettingsMOD saved successfully. ";
                }
                catch(Exception ze)
                {
                    await HomeGateCenter.AddLog("SaveSettings", "Save IpGateSettingsMOD [" + fp + "] Error: " + ze.Message);
                    msg=msg +"\r\n" + "Save IpGateSettingsMOD Error: " + ze.Message;
                }
            }

            return msg;
        }
        public async ValueTask<(GateSettings? tmpGate, RRCoreSettings? tmpRRC, IpGateSettingsMOD? tmpIPG)> GetSettings(bool getGate = false, bool getRRC = false, bool getIPG = false)
        {
            GateSettings? tmpGate = null; RRCoreSettings? tmpRRC = null; IpGateSettingsMOD? tmpIPG = null;
            if (getGate)
            { tmpGate = Settings; }
            if (getRRC)
            { tmpRRC = rrCoreSettings; }
            if (getIPG)
            { tmpIPG = TryGetIPGateSettings(); }
            return (tmpGate, tmpRRC, tmpIPG);
        }
        /// <summary>
        /// BLADE LOGER 对象，静态的，共享的。  如果未初始化会在InitAsync中创建。
        /// </summary>
        public static BLADE.TOOLS.LOG.Loger? CLOG { get; set; } = null;

      
        public void Dispose()
        {
            try
            {
                if (IPGM != null)
                { IPGM.Dispose(); IPGM = null; }
            }
            catch { }
            try
            {
                if (RRCORE != null)
                { RRCORE.Dispose(); RRCORE = null; }
            }
            catch { }
            CLOG?.AddLog("HomeGateCenter", "Disposed");
            CLOG?.SaveLogs(true);
        }

        /// <summary>
        /// 核心配置对象，在InitAsync中加载。
        /// </summary>
        public GateSettings Settings { get; set; } = new GateSettings();

        public IpGateSettingsMOD? TryGetIPGateSettings()
        {
            if (IPGM != null)
            {
                return IPGM.MOD;
            }
            return null;
        }
        /// <summary>
        /// 应用启动路径。
        /// </summary>
        public string AppStartPath { get; set; } = "";
        /// <summary>
        /// 拒绝TCP连接时发送的信息。默认是从Settings.DisConnectMsg 取出的字符串转换成的字节数组。 
        /// 如果设置为长度为0的数组，就直接挂断连接而不发送任何信息。  
        /// </summary>
        public byte[] DisConnectMsg { get; set; } = Array.Empty<byte>() ;

        /// <summary>
        /// IPGateManager 对象，负责IP黑白灰名单的管理和检查。  在InitAsync中创建并初始化。
        /// </summary>
        public BLADE.TOOLS.NET.IPGateManager? IPGM { get; set; } = null;

        /// <summary>
        /// rrCoreSettings 对象，存储了与 RRCore 相关的配置信息。 在InitAsync中加载。
        /// </summary>
        public BLADE.UC.RRClientCore.RRCoreSettings? rrCoreSettings { get; set; } = null;

        /// <summary>
        /// RRCore 对象，负责与 BLADE RRCore Web服务的交互。 在InitAsync中创建并初始化。
        /// </summary>
        public BLADE.UC.RRClientCore.RRCore? RRCORE { get; set; } = null;
        public HomeGateCenter(string startpath)
        {
            AppStartPath = startpath.Trim();

        }
        public void Pardon(string[] ip)
        {
            if (IPGM != null) { IPGM.Pardon(ip);   }
        }
        /// <summary>
        /// 初始化做好资源准备。并会启动 IpGateManager 的内部工作线程。
        /// </summary>
        /// <returns></returns>
        public async Task<(bool suc, string msg)> InitReady()
        {
            bool suc = false; string msg = "";

            try
            {
                // 准备日志和配置目录
                string logdir = Path.Combine(AppStartPath, "Logs");
                string cfgdir = Path.Combine(AppStartPath, "Configs");
                try
                {
                    if (!Directory.Exists(logdir))
                    {
                        Directory.CreateDirectory(logdir);

                    }
                }
                catch (Exception nzz)
                {
                    // 如果这两个目录无法创建，后续的日志记录和配置文件操作都会失败，所以直接返回错误信息，不继续执行后续操作。
                    msg = msg + "\r\nMake DIR " + logdir + " ERROR:" + nzz.Message; suc = false;
                    return (suc, msg);
                }
                try
                {
                    if (!Directory.Exists(cfgdir))
                    {
                        Directory.CreateDirectory(cfgdir);
                    }
                }
                catch (Exception nzz)
                {
                    // 如果这两个目录无法创建，后续的日志记录和配置文件操作都会失败，所以直接返回错误信息，不继续执行后续操作。
                    msg = msg + "\r\nMake DIR " + cfgdir + " ERROR:" + nzz.Message; suc = false;
                    return (suc, msg);
                }

                // 准备加载核心配置文件 HomeGateSettings.cfg，如果文件不存在，就使用默认设置并创建配置文件。但是默认配置文件是肯定无法正常工作的，需要先手动修改。
                string cfg = Path.Combine(cfgdir, "HomeGateSettings.cfg");
                if (System.IO.File.Exists(cfg))
                {
                    try
                    {
                        using (var fs = System.IO.File.OpenText(cfg))
                        {
                            var k = await fs.ReadToEndAsync();
                            if (k.Length > 5)
                            {
                                var se = BLADE.TOOLS.BASE.Json.JsonOptions.Deserialize<GateSettings>(k);
                                if (se != null)
                                {
                                    Settings = se;
                                    msg = "加载HomeGateSettings.cfg配置文件成功"; suc = true;
                                }
                                else
                                { msg = "加载HomeGateSettings.cfg配置文件失败，反序列化结果为 null "; suc = false; }
                            }
                            else
                            { msg = "加载HomeGateSettings.cfg配置文件失败，文件内容无效: " + cfg; suc = false; }
                        }
                    }
                    catch (Exception nze)
                    {
                        msg = "加载HomeGateSettings.cfg配置文件失败 [" + cfg + "] Ex: " + nze.Message; suc = false;
                    }
                }
                else
                {
                    try
                    {
                        using (var fs = System.IO.File.CreateText(cfg))
                        {
                            await fs.WriteAsync(BLADE.TOOLS.BASE.Json.JsonOptions.Serialize<GateSettings>(Settings));
                        }
                        msg = "配置文件 HomeGateSettings.cfg 不存在，已使用默认数据创建配置文件: " + cfg+" 请先修改文件到正确业务需要。"; suc = false;
                    }
                    catch (Exception nze) { msg = "Make new [" + cfg + "] Ex: " + nze.Message; suc = false; }
                }

                //  如果配置文件加载成功了。继续检查和加载其他配置信息。
                if (suc)
                {
                    DisConnectMsg = Encoding.UTF8.GetBytes(Settings.DisConnectMsg);
                    if (CLOG == null)
                    { 
                        //全局日志模块不存在，创建一个。
                        CLOG = new Loger(logdir + "/", "HG_", true, 600, 500, "log");
                        CLOG.Debug = Settings.Debug;
                    }

                    // 尝试加载 RRCore 配置文件。 文件名在核心配置中指定。
                    string rrcorecfg = Path.Combine(cfgdir, Settings.RRCoreCfg);
                    if (System.IO.File.Exists(rrcorecfg))
                    {
                        try
                        {
                            using (var fs = System.IO.File.OpenText(rrcorecfg))
                            {
                                var k = await fs.ReadToEndAsync();
                                if (k.Length > 5)
                                {
                                    var se = BLADE.TOOLS.BASE.Json.JsonOptions.Deserialize<RRCoreSettings>(k);
                                    if (se != null)
                                    {
                                        rrCoreSettings = se;
                                        msg = msg + "\r\n加载RRCoreSettings.cfg配置文件成功"; suc = true;
                                    }
                                    else
                                    { msg = msg + "\r\n加载RRCoreSettings.cfg配置文件失败，反序列化结果为 null "; suc = false; }
                                }
                                else
                                { msg = msg + "\r\n加载RRCoreSettings.cfg配置文件失败，文件内容无效: " + rrcorecfg; suc = false; }
                            }
                        }
                        catch (Exception nze)
                        {
                            msg = msg + "\r\n加载RRCoreSettings.cfg配置文件失败 [" + rrcorecfg + "] Ex: " + nze.Message; suc = false;
                        }
                    }
                    else
                    {
                        try
                        {
                            using (var fs = System.IO.File.CreateText(rrcorecfg))
                            {
                                rrCoreSettings = new RRCoreSettings() ;
                                rrCoreSettings.ConfigName = "NewRRsettings";
                                rrCoreSettings.SetPassword("RawPassword",valueType.Text);
                            
                                await fs.WriteAsync(BLADE.TOOLS.BASE.Json.JsonOptions.Serialize<RRCoreSettings>(rrCoreSettings));
                            }
                            msg = msg + "\r\n配置文件 rrCoreSettings.cfg 不存在，已使用默认数据创建配置文件: " + rrcorecfg; suc = true;
                        }
                        catch (Exception nze) { msg = msg + "\r\nMake new [" + rrcorecfg + "] Ex: " + nze.Message; suc = false; }
                    }

                    // 当RRCore配置加载成功后 创建 RRCore对象和 IPGateManager对象。
                    if (suc)
                    {
                        RRCORE = new(rrCoreSettings, RunMsgHandler);
                        string ipgatesafecfg = Path.Combine(cfgdir, Settings.IpGateCfg);
                        IPGateManager im = new IPGateManager(ipgatesafecfg, RRCORE);
                        var jg = await im.InitAsync();
                        if (jg.suc)
                        {
                            IPGM = im;
                            msg = msg + "\r\nIPGateManager 初始化成功  " + jg.msg; suc = true;
                            IPGM.GrayEvent += IPGM_GrayEvent;
                            IPGM.MessageEvent += IPGM_MessageEvent;
                        }
                        else { msg = msg + "\r\n|| IPGateManager 初始化失败 " + jg.msg; suc = false; }
                    }
                }
            }
            catch (Exception ze)
            {
                msg = msg + "\r\nInit Exception [[ " + ze.Message + " ]] "; suc = false;
            }
            if (CLOG != null) { await CLOG.AddLogAsync(LogCodeEnum.Note, "InitReady", "HomeGateCenter InitReady:" + suc.ToString() + "  " + msg); }
            // 至此核心的初始化工作已经做完，结果是成功还是失败都已经记录在日志和 suc/msg 里。
            return (suc, msg);
        }
        private async void IPGM_MessageEvent(object? sender, MsgEventArgs e)
        {
            if (CLOG != null) { await CLOG.AddLogAsync(LogCodeEnum.Info, "IPGateManager", "MessageEvent: " + e.EventMsg); }
        }
        private async void IPGM_GrayEvent(object? sender, GrayEventArgs e)
        {

            string inf = "";
            if (e.Ig.HasValue)
            {
                inf = e.Ig.Value.Address + " BC:" + e.Ig.Value.BlockCount + " MT:" + e.Ig.Value.Match + " NT:" + e.Ig.Value.NetType + " LC:" + e.Ig.Value.LastCheck_utc.ToString("yyyy-MM-dd HH:mm:ss");
            }
                if (CLOG != null) { await CLOG.AddLogAsync(LogCodeEnum.Info, "IPGateManager", "GrayEvent: " + e.Msg + " Info [" + inf+" ]"); }
            
        }
        /// <summary>
        /// 将RRCore运行时的消息通过日志记录下来。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected async void RunMsgHandler(object? sender, RunMsgEventArgs e)
        {
            await AddLogDEBUG("RunMsgEventArgs", $"TITLE:[ {e.Title} ],  Msg:[ {e.Message} ], Fid:[{e.Finished}]");
        }
        /// <summary>
        /// IP地址合法性检查。使用黑白灰名单。
        /// </summary>
        /// <param name="tunName"></param>
        /// <param name="inip"></param>
        /// <returns></returns>
        public (WBG_CheckResult wbg, int hit) CheckIP(string tunName, string inip)
        {
            if (IPGM == null) { return (WBG_CheckResult.Disposed, 0); }
            return IPGM.CheckIP(inip, tunName);
        }
        /// <summary>
        /// IP地址检查结果的简单翻译 true/false
        /// </summary>
        /// <param name="tunName"></param>
        /// <param name="inip"></param>
        /// <returns></returns>
        public bool CheckIPAllow(string tunName, string inip)
        {
            if (IPGM == null) { return false; }
            var r = IPGM.CheckIP(inip, tunName);
            if (r.wbg == WBG_CheckResult.WhitePass || r.wbg == WBG_CheckResult.BlackPass || r.wbg == WBG_CheckResult.GrayPass || r.wbg == WBG_CheckResult.NotFound)
            {  AddLogTask(  "CheckIPAllow", $"IP {inip} for {tunName} is {r.wbg.ToString()}"); return true; }
             
            return false;
        }

        /// <summary>
        /// 从外部触发黑白灰名单的重新加载，不是必须。 
        /// IPGateManager 内部有间隔控制，短时间内重复调用不会重复执行重新加载操作。
        /// 且 IPGateManager 会按照settings配置的时间自动重新加载名单。
        /// </summary>
        /// <returns></returns>
        public async Task ReLoadIPGate()
        { 
           if(IPGM != null)
           {
               var rl = await IPGM.ReFill(true);
                if (rl != null)
                {
                    await AddLogDEBUG("ReLoadIPGate", rl);
                }
           }
        }
        public void AddIP(string ipaddr, bool isblack = false)
        {
            if (IPGM != null)
            { 
              IPGM.Run_Add( isblack ? NameListType.Black : NameListType.White, ipaddr);
            }
        }
        /// <summary>
        /// 静态共享方法， 记录日志。
        /// </summary>
        /// <param name="title"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static async ValueTask AddLog(string title, string msg)
        {
            if (CLOG != null) {     await CLOG.AddLogAsync(LogCodeEnum.Note, title.Trim(), msg.Trim());     }
        }
        public static  void   AddLogTask(string title, string msg)
        {
             Task.Run(async()=> await AddLog(title, msg));
        }

        /// <summary>
        /// 静态共享方法，记录日志（调试Debug信息，当配置未启用Debug模式时，这些debug信息不会被真正记录）
        /// 正式投产环境debug会被关闭，减少无用信息记录。重要信息要使用 AddLog() 方法记录。
        /// 逻辑需要时，可以使用 HomeGateCenter.ChangeDebugMode(true) 来开启Debug模式， 这会影响全局的日志记录行为。
        /// </summary>
        /// <param name="title"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static async ValueTask AddLogDEBUG(string title, string msg)
        {
            if (CLOG != null)
            { await CLOG.AddLogDebug(title.Trim(), msg.Trim()); }
        }
    }
    /// <summary>
    /// 应用中心。静态资源和配置信息。
    /// </summary>
    //public class RunCenter
    //{
    //    /// <summary>
    //    /// 工作中的转发线路数量。
    //    /// </summary>
    //    public static int TransCount { get; set; } = 0;

    //    /// <summary>
    //    /// 检查IP 是否允许接入
    //    /// </summary>
    //    /// <param name="tunname">通道名</param>
    //    /// <param name="inIP">接入IP</param>
    //    /// <returns>true  允许接入，  false 拒绝</returns>
    //    public static async Task<bool> CheckIP(string tunname, string inIP)
    //    {
    //        return await WLR.CheckIP(tunname, inIP);
    //    }

    //    /// <summary>
    //    /// 运行时的白名单集合。包括固化的和动态的。
    //    /// </summary>
    //    public static WL_Runtime WLR = new WL_Runtime();
    //    /// <summary>
    //    /// 拒绝语。 留空就不会发送拒绝信息而直接挂断。
    //    /// </summary>
    //    public static byte[] DisConnectMsg = new byte[0];
    //    /// <summary>
    //    /// 尝试刷新白名单缓存，内部有间隔控制。 32秒内不会重复执行。
    //    /// </summary>
    //    /// <returns></returns>
    //    public static async ValueTask TryFlushWLR()
    //    {
    //        try
    //        {
    //            await WLR.ReGetWLRuntime();
    //            await WLR.Flush();
    //        }
    //        catch (Exception zz)
    //        {
    //            await RunCenter.AddLogAsync("TryFlushWLR", "TryFlushWLR Error : " + zz.Message, LogCodeEnum.Alert);
    //        }
    //    }

    //    /// <summary>
    //    /// BLADE LOGER 
    //    /// </summary>
    //    public static BLADE.TOOLS.LOG.Loger? CLOG = null;

    //    /// <summary>
    //    /// 从配置文件取出的 各种设置。
    //    /// </summary>
    //    public static GateSettings? Settings = null;

    //    /// <summary>
    //    /// 应用启动的路径。
    //    /// </summary>
    //    public static string AppStartPath { get; set; } = "";

    //    /// <summary>
    //    /// 是否完成初始化？
    //    /// </summary>
    //    public static bool Inited { get; private set; } = false;

    //    /// <summary>
    //    /// 修改运行模式，直接影响日志的详细记录情况。其他部分看情况使用。
    //    /// </summary>
    //    /// <param name="debug"></param>
    //    public static void ChangeDebugMode(bool debug)
    //    {
    //       // Settings?.WebSettings.EnableDeBug = debug;
    //        CLOG?.Debug = debug;
    //    }

    //    /// <summary>
    //    /// 保存配置到文件。
    //    /// </summary>
    //    /// <param name="settingfile"></param>
    //    /// <returns></returns>
    //    public static async Task<bool> SaveSettingsToFile(string settingfile)
    //    {
    //        try
    //        {
    //            var se = BLADE.TOOLS.BASE.Json.JsonOptions.Serialize<GateSettings>(Settings);
    //            if (se != null && se.Length > 5)
    //            {
    //                using (StreamWriter sr = System.IO.File.CreateText(settingfile))
    //                { await sr.WriteAsync(se); }  return true;
    //            } 
    //        }
    //        catch { }
    //        return false;
    //    }

    //    /// <summary>
    //    /// 从文件加载配置信息。应该在初始化时使用。 运行时加载需要处理可能存在的冲突。
    //    /// </summary>
    //    /// <param name="settingsFile">配置文件。</param>
    //    /// <returns></returns>
    //    private static async Task<Result> LoadSettings(string settingsFile)
    //    {
    //        Result R = new Result(false, "null", null);
    //        bool isnull = true;
    //        try
    //        {
    //            if (System.IO.File.Exists(settingsFile))
    //            {
    //                using (StreamReader sr = System.IO.File.OpenText(settingsFile))
    //                {
    //                    string text = await sr.ReadToEndAsync();
    //                    if (text.Length > 5)
    //                    {
    //                        var se = BLADE.TOOLS.BASE.Json.JsonOptions.Deserialize<GateSettings>(settingsFile);
    //                        if (se != null)
    //                        {
    //                            Settings = se;
    //                            R = new Result(true, "加载配置文件成功", Settings); isnull = false;
    //                        }
    //                    }
    //                }
    //            }
    //            else
    //            {
    //                Settings = new GateSettings();
    //                Settings.AdminUser.UserID =0;
    //                Settings.AdminUser.UserName = "BladeAdmin";
    //                Settings.AdminUser.RealName = "BladeAdmin";
    //                Settings.AdminUser.ShowName = "BladeAdmin";
    //                Settings.AdminUser.SetCryptPass("BladePass");
    //                Settings.OperUser.UserID = 100;
    //                Settings.OperUser.UserName = "BladeOper";
    //                Settings.OperUser.RealName = "BladeOper";
    //                Settings.OperUser.ShowName = "BladeOper";
    //                Settings.OperUser.SetCryptPass("BladePass");
    //                if(await SaveSettingsToFile(settingsFile))
    //                {
    //                    R = new Result(true, "配置文件不存在，已使用默认数据创建配置文件", Settings); isnull = false;
    //                }
    //            }
    //        }
    //        catch (Exception ze)
    //        {
    //            R = new Result(false, "加载配置文件异常：" + ze.Message, null); isnull = false;
    //        }
    //        if (isnull) { R = new Result(false, "未能正确加载配置文件，也未能完成使用默认数据创建配置文件", null); isnull = false; }
    //        return R;
    //    }
    //    /// <summary>
    //    ///  初始化工作。
    //    /// </summary>
    //    /// <param name="startPth"></param>
    //    /// <returns></returns>
    //    public static async Task<Result<GateSettings>> Init(string startPth)
    //    { 
    //        Result<GateSettings>? RR = null;
    //        startPth = startPth.Trim();
    //        if (startPth.EndsWith("\\") || startPth.EndsWith("/")) { }
    //        else { startPth = startPth + "/"; }
    //        AppStartPath = startPth;
    //        string setFile = AppStartPath + "HomeGateSettings.cfg";
    //        var a = await LoadSettings(setFile);
    //        if (!a.Successful)
    //        { RR = new Result<GateSettings>(false, "LoadSettings: " + setFile + " Error: " + a.Message, null); }
    //        else
    //        {
    //            Settings = (GateSettings)a.DataOrSender;
    //            CLOG = new Loger(AppStartPath + Settings.WebSettings.logsubdir + "/", "HG_", true, 500, 300, "log");
    //            CLOG.Debug = Settings.WebSettings.EnableDeBug;
    //            await CLOG.AddLogAsync(LogCodeEnum.App, "InitAndStart", "Load Settings File OK : " + setFile);
    //            RR = new Result<GateSettings>(true, "InitAndStart OK", Settings);
    //            BLADE.MSGCORE.ClientTools.ClientCore.RunSet = Settings.ClientSettings;
    //            if (Settings.WhiteListSettings.WL_Locals.Length > 0)
    //            { WLR.AddWL_Locals(Settings.WhiteListSettings.WL_Locals); }
    //            if (Settings.DisConnectMsg.Length > 0)
    //            {
    //                DisConnectMsg = Encoding.UTF8.GetBytes(Settings.DisConnectMsg);
    //            }
    //        }
    //        Inited = RR.Successful;
    //        return RR;
    //    }

    //    /// <summary>
    //    /// 添加日志  同步方式。
    //    /// </summary>
    //    /// <param name="title"></param>
    //    /// <param name="msg"></param>
    //    /// <param name="code"></param>
    //    public static void AddLog(string title, string msg, LogCodeEnum code = LogCodeEnum.Debug)
    //    {
    //        if (CLOG != null)
    //        {
    //            if (code != LogCodeEnum.Debug || CLOG.Debug) { CLOG.AddLog(code, title, msg); }
    //        }
    //    }
    //    /// <summary>
    //    ///  添加日志  异步方式。
    //    /// </summary>
    //    /// <param name="title"></param>
    //    /// <param name="msg"></param>
    //    /// <param name="code"></param>
    //    /// <returns></returns>
    //    public static async ValueTask AddLogAsync(string title, string msg, LogCodeEnum code = LogCodeEnum.Debug)
    //    {
    //        if (CLOG != null)
    //        {
    //            if (code != LogCodeEnum.Debug || CLOG.Debug) { await CLOG.AddLogAsync(code, title, msg); }
    //        }
    //    }

    //    /// <summary>
    //    /// 判断一个地址是 IP地址  地址段  或需要解析的 域名。
    //    /// </summary>
    //    /// <param name="input"></param>
    //    /// <returns></returns>
    //    public static AddressType GetAddressType(string input)
    //    {
    //        if (string.IsNullOrWhiteSpace(input))
    //        {
    //            return AddressType.Invalid;
    //        }

    //        input = input.Trim().ToLower();

    //        // Check for IPv4
    //        if (IPAddress.TryParse(input, out var ipAddress))
    //        {
    //            return ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork
    //                ? AddressType.IPv4
    //                : AddressType.IPv6;
    //        }

    //        // Check for CIDR (e.g., 192.168.0.0/16)
    //        if (Regex.IsMatch(input, @"^(\d{1,3}\.){3}\d{1,3}\/\d{1,2}$"))
    //        {
    //            var parts = input.Split('/');
    //            if (parts.Length == 2 && IPAddress.TryParse(parts[0], out _) && int.TryParse(parts[1], out var prefix) && prefix >= 0 && prefix <= 32)
    //            {
    //                return AddressType.CIDR;
    //            }
    //        }

    //        // Check for domain name
    //        string luo = input.Replace("mkv:", "").Replace("dns:", "");
    //        if (Regex.IsMatch(luo, @"^([allbody-zA-Z0-9-]+\.)+[allbody-zA-Z]{2,}$"))
    //        {   return AddressType.Domain;   }

    //        return AddressType.Invalid;
    //    }

    //    /// <summary>
    //    /// 判定 body 中是否包含 val地址。（包括地址段判断）
    //    /// </summary>
    //    /// <param name="body">解析结果全文</param>
    //    /// <param name="val">外来地址</param>
    //    /// <returns></returns>
    //    public static bool PanInclude(string body, string val)
    //    {
    //        val = val.Trim().ToLower();
    //        body = body.Trim().ToLower();

    //        //   return body.Contains(val);

    //        string[] bs = body.Split(new string[] { ";", " ", "#" }, StringSplitOptions.RemoveEmptyEntries);
    //        for (int z = 0; z < bs.Length; z++)
    //        {
    //            if (BLADE.TOOLS.NET.IPTools.ContainsIP(bs[z], val))
    //            {
    //                return true;
    //            }
    //        }
    //        return false;
    //    }

    //    /// <summary>
    //    /// 从一个字符串中取出分隔符的前部，或后部。
    //    /// </summary>
    //    /// <param name="allbody">文本</param>
    //    /// <param name="spl">分隔符</param>
    //    /// <param name="getFrontPerfix">默认 true  返回分隔符的前段。  false 返回分隔符的后段。</param>
    //    /// <returns></returns>
    //    public static string SplString(string allbody, string spl, bool getFrontPerfix=true)
    //    {
    //        if (string.IsNullOrEmpty(spl)) return allbody;

    //        int index = allbody.IndexOf(spl);
    //        if (index == -1) return allbody;

    //        return getFrontPerfix ? allbody.Substring(0, index) : allbody.Substring(index + spl.Length);
    //    }

    //}

    /// <summary>
    /// Gate 服务的整体配置信息
    /// </summary>
    public class GateSettings
    {

        public bool EnableGreenAPI { get; set; } = true;
        public ushort GreenAPIPort { get; set; } = 3399;
        public string GreenAPIAddress { get; set; } = "0.0.0.0";

        //  public BLADE.TOOLS.WEB.Razor.UserData.IUserDataSession AdminUser { get; set; } = TOOLS.WEB.Razor.UserData.IUserDataSession.CreateBase();
        //  public BLADE.TOOLS.WEB.Razor.UserData.IUserDataSession OperUser { get; set; } = TOOLS.WEB.Razor.UserData.IUserDataSession.CreateBase();
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
       // public bool EnableWeb { get; set; } = true;
        /// <summary>
        /// 拒绝连接的警告信息，留空则不发送任何信息直接断开连接
        /// </summary>
        public string DisConnectMsg { get; set; } = "";
        /// <summary>
        /// 允许的最大连接数。超出则直接拒绝新连接。
        /// </summary>
        public ushort MaxConnection { get; set; } = 200;
        /// <summary>
        /// 转发通道闲置时间  单位毫秒。 默认32秒。
        /// </summary>
        public ushort TcpIdelBreakMilliseconds { get; set; } = 32000;

        /// <summary>
        /// MSGCORE 接口 客户端配置。包括服务地址，认证信息等。
        /// </summary>
       // public BLADE.MSGCORE.ClientTools.ProSettings ClientSettings { get; set; } = new MSGCORE.ClientTools.ProSettings();
        /// <summary>
        /// web 管理界面的配置信息。包括web服务端口，日志信息，debug等。
        /// </summary>
       // public BLADE.TOOLS.WEB.WorkSetting WebSettings { get; set; } = new WorkSetting();

        /// <summary>
        /// 转发通道配置信息
        /// </summary>
        public TunTransSettings TunSettings { get; set; } = new TunTransSettings();
        /// <summary>
        /// 白名单本地配置信息。将通过配置文件加载，此部分配置信息将在应用运行时保持。
        /// </summary>
       // public WL_Local_Settings WhiteListSettings { get; set; } = new WL_Local_Settings();
        public bool Debug { get; set; } = true;
        public string IpGateCfg { get; set; } = "ipgatesafe.cfg";
        public string RRCoreCfg { get; set; } = "rrcoresafe.cfg";

        public long ComChannel { get; set; } = 980;
        public string ComName { get; set; } = "HomeGateService001";
        public byte ComFreq { get; set; } = 0;
        public string ComTarget { get; set; } = "BLADE.UC.HOMEGATE";
        /// <summary>
        /// SafeCode * 7 + 11 = ComCode   (SafeCode 是 RRCore 对应业务模块的安全码，必须一致才能正确提交数据。)
        /// </summary>
        public long ComCode { get; set; } = 622222227;
       
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

        public ulong TransBytesCount = 0;
        public byte[] buf = new byte[0];
       
        /// <summary>
        /// 转发流量， 此方法内不设trycatch ，请在调用处处理异常，以便得到正确的断开信号。
        /// </summary>
        /// <returns></returns>
        public async ValueTask<int> TransWork()
        {
            if (TcpIn.Available > 0)
            {
                int rd = 0;

                try
                {
                    using CancellationTokenSource _cts = new CancellationTokenSource(600);
                    var tk =   _cts.Token;
                    
                    rd = await TcpIn.GetStream().ReadAsync(buf, tk);
                    if (rd > 0)
                    {
                       // tk = _cts.Token;
                        await TcpOut.GetStream().WriteAsync(buf, 0, rd, tk);
                        TransBytesCount += (ulong)rd;
                    }
                }
                catch (OperationCanceledException) { return -9; }
                return rd;
            }
            return 0;
        }

    }

    /// <summary>
    /// 运行时的 转发管道
    /// </summary>
    public class TcpTrans :IDisposable
    {
        public static int NextID()
        {
            if (_idseed > 8000000) { Interlocked.Exchange(ref _idseed, 1); }
           int v= Interlocked.Add(ref _idseed, 3);
            return v + 1;
        }
        private static int _idseed = 0;
        public bool disposed = false;
        public Tao Tao_W2N;
        public Tao Tao_N2W;
        public DateTime StartUTC= TimeProvider.UtcNow;
        public DateTime LastActUTC = TimeProvider.UtcNow;
        public TcpClient TcpW;
        public TcpClient TcpN;
        public TcpTunSet TunSetting;
        private HomeGateCore matherCore;
        public string LanSideIPEP { get; set; } = "";
        public long LinkTimeSeconds
        {
            get
            {
                return (long)(TimeProvider.UtcNow - StartUTC).TotalSeconds;
            }
        }
        public TcpTrans(HomeGateCore mather, TcpClient tw, TcpClient tn, TcpTunSet ts)
        {
            matherCore = mather;
            TcpW = tw; TcpN = tn; TunSetting = ts;
            ID = NextID();
            try
            {
                LanSideIPEP = ((IPEndPoint)tn.Client.LocalEndPoint).Port.ToString()+"="+((IPEndPoint)tn.Client.RemoteEndPoint).Port.ToString(); 
                if (LanSideIPEP.Length>=3)
                {    HomeGateCore.SetBackRoutingTab(LanSideIPEP, tw.Client.RemoteEndPoint.ToString(), true);    }
            }
            catch { }
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
        private ulong speedW2N = 0;
        private ulong speedN2W = 0;
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
                    rd1 = await Tao_N2W.TransWork(); 
                    rd2 = await Tao_W2N.TransWork();
                    if (rd1 < 0 || rd2 < 0)
                    {
                        await HomeGateCenter.AddLog("transWork", "OperationCanceledException Break !!! "  );

                        return false;
                    }

                    speedN2W += (ulong)rd1;
                    speedW2N += (ulong)rd2;
                    wavetmp++;
                    if ((rd1 + rd2) > 0) { LastActUTC = TimeProvider.UtcNow;  } else { await Task.Delay(24); }
                    if (wavetmp > 10)
                    {
                        wavetmp = 0;
                        if ((TimeProvider.UtcNow - speedTime).TotalMilliseconds > 2000)
                        {
                            Speed = "IN "+ HomeGateCore.GetKM( speedW2N / 2)+" / OUT "+HomeGateCore.GetKM( speedN2W / 2);
                            if (matherCore.Count_Trans > 2 && ((speedN2W / 2048.0) > TunSetting.SpeedMax || (speedW2N / 2048.0) > TunSetting.SpeedMax))
                            {
                                await Task.Delay(120);
                            }
                            speedW2N = 0;
                            speedN2W = 0;
                            speedTime = TimeProvider.UtcNow;
                        }
                    }
                    return true;
                }
                catch (Exception ze) { try { await HomeGateCenter.AddLog("transWork", "Error to Break : " + ze.Message); } catch { } 
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
                if (  await transWork())
                { }
                else { break; }
            }

            try {
                Dispose();
                try { await HomeGateCenter.AddLog("TcpTrans", "TransLoopWork Out while : " +ID); } catch { }
            } catch(Exception ze) { try { await HomeGateCenter.AddLog("TcpTrans WARRING", "TransLoopWork Stoped EX:" + ze.Message); } catch { } }
        }
        public async ValueTask StartWork()
        {
             _= Task.Run(async() => { await LoopWork(); });
        }
        public string GetTransInfo()
        {
            return  TunSetting.GetRoadInfo()+ " ("+ID+") = [ W2N: " + HomeGateCore.GetKM(Tao_W2N.TransBytesCount) + " ][ N2W: " + HomeGateCore.GetKM(Tao_N2W.TransBytesCount) + " ] " + Speed;
        }
        public void Dispose()
        {
            disposed = true;
            try {  HomeGateCore.DelBackRoutingTab(LanSideIPEP, true);  } catch { }
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
    public class TcpTunSet:UdpTunSet
    {
        public int MTUSize { get; set; } = 1400;
        /// <summary>
        /// 速度限制   单向限制 单位 KB
        /// </summary>
        public int SpeedMax { get; set; } = 1024;
        // /// <summary>
        // /// (Gate模式下 此属性无用)  连接限制  每个通道单独配置 除白名单模式外  超过限制则封锁到灰名单
        // /// </summary>
        // public int LockCount { get; set; } = 9;

        // /// <summary>
        // /// 未生效， 现版本是对全部TCP 连接使用转发处理。 下个版本实现http解析并提供代理处理。
        // /// </summary>
        // public bool HttpProxy { get; set; } = false;
        /// <summary>
        /// 转发说明
        /// </summary>
        public string GetRoadInfo()
        { 
            return WanAddress + ":" + WanPort.ToString() + " TO " + LanAddress + ":" + LanPort.ToString() + " R_" + UseRule.ToString();
        }
    }

    public class UdpTunSet
    {
      //  public bool UdpHolePunch { get; set; } = true;
        public string WanSideKey { get { return WanAddress+":"+WanPort.ToString(); } }   
        /// <summary>
        /// 转发名称
        /// </summary>
        public string TunName { get; set; } = "DEF-2000-2222";
        /// <summary>
        /// 转发管道的  侦听端口 
        /// </summary>
        public int WanPort { get; set; } = 2000;
        /// <summary>
        /// 转发管道的  侦听地址   默认 0.0.0.0
        /// </summary>
        public string WanAddress { get; set; } = "0.0.0.0";
        /// <summary>
        /// 转发管道的  目标  远端端口
        /// </summary>
        public int LanPort { get; set; } = 2222;
        /// <summary>
        /// 转发管道的  目标  远端地址  不可空
        /// 当需要使用动态接卸地址时，请使用 LanDOM 属性，此值随便写（会被解析替换）
        /// </summary>
        public string LanAddress { get; set; } = "192.168.100.100";

        /// <summary>
        /// DNS OR DOM ITEM ex:  "V4:h.mlez.net",  "V6:h.mlez.net",  "R6:PVE_GATE", "R4:PVE_GATE"
        ///  需要DNS 或者 RED 解析的地址 当此属性存在时，HOMEGATE 将解析此地址并替换 LanAddress 进行转发连接
        ///  不需要解析时，此属性无必留空，HOMEGATE会直接使用 LanAddress 进行连接
        /// </summary>
        public string LanDOM { get; set; } = "";
        /// <summary>
        /// 设置此管道是否应用名单规则  具体的应用规则由 Settings.RunWithWhiteOrBlack 决定。 true = 应用   false = 不应用规则，直通
        /// </summary>
        public bool UseRule { get; set; } = true;

        
    }
    /// <summary>
    /// 管道配置集合
    /// </summary>
    public class TunTransSettings
    {
        //private bool rebuild = false;
        public TcpTunSet[] TcpTuns { get; set; }= Array.Empty<TcpTunSet>();
        public UdpTunSet[] UdpTuns { get; set; }=Array.Empty<UdpTunSet>();

        public bool EnableUDP { get; set; } = true;
        public int UdpIdelBreakMilliseconds { get; set; } = 32000;
        public TunTransSettings()
        {
            TcpTuns = new TcpTunSet[2];
            TcpTuns[0] = new TcpTunSet();
            TcpTuns[0].TunName = "Def2221";
            TcpTuns[0].WanPort = 2221;
            TcpTuns[0].SpeedMax = 1200;
            TcpTuns[0].UseRule = true;

            TcpTuns[1] = new TcpTunSet();
            TcpTuns[1].TunName = "Def2223";
            TcpTuns[1].WanPort = 2223;
            TcpTuns[1].SpeedMax = 850;
            TcpTuns[1].UseRule = false;

            UdpTuns = new UdpTunSet[2]; 
            UdpTuns[0] = new UdpTunSet();
            UdpTuns[0].TunName = "DefUdp2221";
            UdpTuns[0].WanPort = 2221;
           // UdpTuns[0].SpeedMax = 1200;
            UdpTuns[0].UseRule = true;

            UdpTuns[1] = new UdpTunSet();
            UdpTuns[1].TunName = "DefUdp2223";
            UdpTuns[1].WanPort = 2223;
           // UdpTuns[1].SpeedMax = 850;
            UdpTuns[1].UseRule = false;
        }
    }

    #region  WhiteList Classes

    /// <summary>
    ///  白名单运行库。 内部缓存白名单信息，并间隔刷新解析结果。
    /// </summary>
    //public class WL_Runtime
    //{ 
      
    //    protected Dictionary<string, List<string>> Locals = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
    //    protected Dictionary<string, List<string>> RunTimes = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
    //    protected BLADE.TOOLS.BASE.ThreadSAFE.Dictionary_TS<string, WL_Item> KVS = new BLADE.TOOLS.BASE.ThreadSAFE.Dictionary_TS<string, WL_Item>(StringComparer.OrdinalIgnoreCase);
    //    protected Lock _lk = new Lock();
    //    public WL_Runtime()
    //    { }
    //    public WL_Runtime(  WL_Local[] locs)
    //    {
    //        AddWL_Locals(locs);
    //    }
    //    /// <summary>
    //    /// 添加本地白名单配置，添加前会先尝试解析地址，所以本操作是准同步的。会消耗一定时间。
    //    /// </summary>
    //    /// <param name="locs">本地白名单配置。</param>
    //    /// <returns></returns>
    //    public int AddWL_Locals(WL_Local[] locs)
    //    {
    //        int a = 0;
    //        if (locs != null && locs.Length > 0)  
    //        {
    //            lock (_lk)
    //            {
    //                foreach (var i in locs)
    //                {
    //                    foreach (var ap in i.AddressOrIPCD)
    //                    {
    //                        WL_Item wi = new WL_Item(ap);
    //                        if (wi.TagType == AddressType.Domain)
    //                        {
    //                            Task.Run(() => FlushBack_work(wi));
    //                        }
    //                        // wi.Name = WL_Name;
    //                        // wi.AddressOrIPCD = ap.Trim().ToLower();
    //                        if (wi.TagType != AddressType.Invalid)
    //                        {
    //                            if (Locals.ContainsKey(i.WL_Name))
    //                            { }
    //                            else { Locals.Add(i.WL_Name, new List<string>()); }
    //                            var ll = Locals[i.WL_Name];
    //                            if (ll.Contains(wi.AddressOrIPCD))
    //                            { }
    //                            else { ll.Add(wi.AddressOrIPCD); a++; }

    //                            if (KVS.ContainsKey(wi.AddressOrIPCD)) { } else { KVS.Add(wi.AddressOrIPCD, wi); }
    //                        }

    //                    }
    //                }
    //            }
    //        }
    //        return a;
    //    }

    //    /// <summary>
    //    /// 添加运行时白名单，来源是 MSGCORE 接口获取的动态白名单。
    //    /// 本方法是添加，不会去重已有的运行时白名单。
    //    /// 所以当需要重新刷新时，需要先清楚已经存在的通道白名单。
    //    /// </summary>
    //    /// <param name="tunname">通道名</param>
    //    /// <param name="rims">运行时白名单数组。</param>
    //    /// <returns></returns>
    //    public async Task<int> AddWL_Runtimes(string tunname,  WL_Item[] rims)
    //    {
    //        int a = 0;
    //        tunname = tunname.Trim().ToLower();
    //        if (tunname.Length < 1  ) { return a; }
    //        if (rims != null && rims.Length > 0)
    //        {
    //            for (int w = 0; w < rims.Length; w++)
    //            {
    //                if (rims[w].TagType == AddressType.Domain)
    //                {
    //                    await FlushBack_work(rims[w]);
    //                }
    //            }

    //            lock (_lk) {
    //                if (RunTimes.ContainsKey(tunname))
    //                { }
    //                else { RunTimes.Add(tunname, new List<string>()); }
    //                var ll = RunTimes[tunname];
    //                foreach (var i in rims)
    //                {
    //                    if (i.TagType != AddressType.Invalid)
    //                    {
    //                        if (ll.Contains(i.AddressOrIPCD))
    //                        { }
    //                        else { ll.Add(i.AddressOrIPCD); a++; }
    //                        if (KVS.ContainsKey(i.AddressOrIPCD)) { } else { KVS.Add(i.AddressOrIPCD, i); }
    //                    }
    //                }
    //            }
    //        }
    //        return a;
    //    }

    //    public async Task<bool> AddWL_Runtime(string tunname, string addressorIPCD)
    //    {
    //        tunname = tunname.Trim().ToLower();
    //        addressorIPCD = addressorIPCD.Trim().ToLower();
    //        if (tunname.Length < 1 || addressorIPCD.Length<1) { return false; }
    //        var i = new WL_Item(addressorIPCD);
    //        if(i.TagType == AddressType.Domain){ await FlushBack_work(i); }
    //        if (i.TagType == AddressType.Invalid) { return false; }
    //        lock (_lk)
    //        {
    //            if (RunTimes.ContainsKey(tunname))
    //            { }
    //            else { RunTimes.Add(tunname, new List<string>()); }
    //            var ll = RunTimes[tunname];
                
    //            if (ll.Contains(i.AddressOrIPCD))
    //            { }
    //            else { ll.Add(i.AddressOrIPCD); }
    //            if (KVS.ContainsKey(i.AddressOrIPCD)) { } else { KVS.Add(i.AddressOrIPCD, i); return true; }
    //        }
    //        return false;
    //    }
    //    /// <summary>
    //    /// 解析IP地址。 
    //    /// 输入 mkv:domian.com 模式会尝试从 MSGCORE 提取值， 
    //    /// 输入 dns:domain.com / domain.com 模式会使用dns解析，
    //    /// 输入  192.168.1.5 的IP地址模式dns 会直接返回原地址。
    //    /// 输入其他模式会返回空字符串，视作无效地址。
    //    /// </summary>
    //    /// <param name="input">需要判断是否在白名单之内的外来地址。</param>
    //    /// <returns>返回的地址可能是单个地址/地址段，也可能是多个。
    //    /// 当DNS会有多个解析结果时，结果是合并的地址字符串。
    //    /// 所以判断是否包含需要拆解，不能简单用相等判断。需要使用PanInclude判断</returns>
    //    public static async Task<string> GetIP(string input)
    //    {
    //        if (input.StartsWith("mkv:"))
    //        {
    //            string luo = input.Replace("mkv:", "");
    //            return await mkvIP(luo);
    //        }
    //        else {
    //            string ld = input.Replace("dns:", "");
    //            return await dnsIP(ld);
    //        }
    //    }
    //    protected static async Task<string> mkvIP(string ip)
    //    {
    //        try
    //        {
    //            var rr = await BLADE.MSGCORE.ClientTools.ClientCore.MKV_CreateQryPost(0, ip);
    //            if (rr != null && rr.StatusCode == 200)
    //            {
    //                var pmi = BLADE.TOOLS.BASE.Json.JsonOptions.Deserialize<PostResponse>(rr.ResponseText);
    //                if (pmi != null && pmi.secKEY == 99999999)
    //                {
    //                    string jsonPostMkvItem = Encoding.UTF8.GetString(Convert.FromBase64String(pmi.Message));
    //                    var mi = BLADE.TOOLS.BASE.Json.JsonOptions.Deserialize<PostMkvItem>(jsonPostMkvItem);
    //                    if (mi != null && mi.KEYVALUE.Length > 0)
    //                    {
    //                        await RunCenter.AddLogAsync("MkvDNAME", "mkv: " + mi.KEYNAME + " = " + mi.KEYVALUE, TOOLS.LOG.LogCodeEnum.Debug);
    //                        return mi.KEYVALUE.Trim();
    //                    }
    //                }
    //                await RunCenter.AddLogAsync("MkvDNAME", "MKV get Null : " + ip, TOOLS.LOG.LogCodeEnum.Note);
    //            }
    //        }
    //        catch (Exception ze)
    //        { await RunCenter.AddLogAsync("MkvDNAME", "mkv Error: " + ip + " EX: " + ze.Message, TOOLS.LOG.LogCodeEnum.Note); }
    //        await RunCenter.AddLogAsync("MkvDNAME", "mkv : " + ip + " No Address.", TOOLS.LOG.LogCodeEnum.Debug);
    //        return "";
    //    }
    //    protected static async Task<string> dnsIP(string ip)
    //    {
    //        try
    //        {
    //            IPHostEntry IPinfo = Dns.GetHostEntry(ip);

    //            if (IPinfo.AddressList.Length > 0)
    //            {
    //                StringBuilder sb = new StringBuilder();
    //                foreach (var ii in IPinfo.AddressList)
    //                {
    //                    sb.Append(ii + "  ");
    //                }
    //                string nn = sb.ToString();
    //                await RunCenter.AddLogAsync("DnsDNAME", "dns: " + ip + " = " + nn, TOOLS.LOG.LogCodeEnum.Debug);
    //                return nn;
    //            }
    //        }
    //        catch (Exception ze) {
    //            await RunCenter.AddLogAsync("DnsDNAME", "dns Error: " + ip +" EX: "+ze.Message, TOOLS.LOG.LogCodeEnum.Note);
    //        }
    //        await RunCenter.AddLogAsync("DnsDNAME", "dns : " + ip +" No Address." , TOOLS.LOG.LogCodeEnum.Debug);
    //        return "";
    //    }

    //    /// <summary>
    //    /// 检查外来IP，是否在指定通道的白名单之内。
    //    /// </summary>
    //    /// <param name="tunname">管道名</param>
    //    /// <param name="inIP">外来地址</param>
    //    /// <returns>true 在白名单内，通过检查。  false 应该拒绝</returns>
    //    public async Task< bool> CheckIP(string tunname, string inIP)
    //    {
    //        tunname=tunname.Trim().ToLower();
    //        inIP = inIP.Trim();

    //        if (tunname.Length < 1 || inIP.Length < 3) { return false; }
    //        if (RunCenter.GetAddressType(inIP) == AddressType.Invalid)  { return false; }

    //        bool fd = false;
    //        if (Locals.ContainsKey(tunname))
    //        {
    //            var ll = Locals[tunname];
                
    //            if (ll.Contains(inIP))
    //            { fd = true; }
    //            else
    //            {
    //                foreach (var ts in ll)
    //                { 
    //                    if (KVS.ContainsKey(ts))
    //                    {
    //                        var wi = KVS[ts];
    //                        if (wi.TagType == AddressType.CIDR && BLADE.TOOLS.NET.IPTools.ContainsIP(wi.AddressOrIPCD, inIP))
    //                        {
    //                            fd = true; break;
    //                        }
    //                        if (wi.TagType == AddressType.Domain)
    //                        {
    //                            if (RunCenter.PanInclude(wi.ADVALUE.ToLower(), inIP)) { fd = true;   } 
    //                            if (!_flushing&&  (TimeProvider.UtcNow - wi.FlashUTC).TotalSeconds > 20 )
    //                                { Task.Run(() => FlushBack_work(wi)); }
    //                            if (fd) { break; }
    //                        }
    //                    } 
    //                } 
    //            }
    //        }
    //        if (fd == false)
    //        {
    //            if (RunTimes.ContainsKey(tunname))
    //            {
    //                var ll = RunTimes[tunname]; 
    //                if (ll.Contains(inIP))
    //                { fd = true; }
    //                else
    //                {
    //                    foreach (var ts in ll)
    //                    {

    //                        if (KVS.ContainsKey(ts))
    //                        {
    //                            var wi = KVS[ts];
    //                            if (wi.TagType == AddressType.CIDR && BLADE.TOOLS.NET.IPTools.ContainsIP(wi.AddressOrIPCD, inIP))
    //                            {
    //                                fd = true; break;
    //                            }
    //                            if (wi.TagType == AddressType.Domain)
    //                            {
    //                                if (RunCenter.PanInclude(wi.ADVALUE.ToLower(), inIP)) { fd = true; }
    //                                if (!_flushing && (TimeProvider.UtcNow - wi.FlashUTC).TotalSeconds > 20)
    //                                { Task.Run(() => FlushBack_work(wi)); }
    //                                if (fd) { break; }
    //                            }
    //                        }

    //                    }

    //                }
    //            }
    //        }

    //        return false;
    //    }

    //    protected DateTime _lastflush = TimeProvider.UtcNow.AddMinutes(-2);
    //    protected DateTime _lastWL = TimeProvider.UtcNow.AddMinutes(-2);

    //    private static async ValueTask<bool> FlushBack_work(WL_Item wi)
    //    {
    //        try
    //        {
    //            DateTime cc = TimeProvider.UtcNow;
    //            if ((cc - wi.FlashUTC).TotalSeconds > 20 || wi.TagType == AddressType.Domain)
    //            {
    //                wi.FlashUTC = cc; wi.ADVALUE = await GetIP(wi.AddressOrIPCD);
    //                return true;
    //            }
    //        }
    //        catch(Exception zee)  { if (RunCenter.Settings.WebSettings.EnableDeBug) { try { 
    //                 await RunCenter.AddLogAsync("FlushBack_work", "Ex: " + zee.Message, LogCodeEnum.Warning);
    //                } catch { } } }
    //        return false;
    //    }
    //    protected bool _flushing = false;

    //    /// <summary>
    //    /// 如果刷新间隔超过20秒，则启动刷新工作，
    //    /// 尝试在KVS 中寻找域名类型的地址进行解析刷新。
    //    /// </summary>
    //    /// <returns>本次刷新，解析的条目数</returns>
    //    public async ValueTask<int> Flush()
    //    {
    //        if (_flushing) { return 0; }
    //        DateTime cc = TimeProvider.UtcNow;
    //        int a = 0;
    //        if ((cc - _lastflush).TotalSeconds > 20)
    //        {
    //            try
    //            {
    //                _flushing = true;
    //                foreach (var vs in KVS.Values)
    //                {
    //                    if (vs != null && vs.TagType == AddressType.Domain && (cc - vs.FlashUTC).TotalSeconds > 16)
    //                    { Task.Run(() => FlushBack_work(vs)); a++; }
    //                }
    //                _lastflush = TimeProvider.UtcNow;

    //            }
    //            catch { }
    //            finally { _flushing = false; }
    //        } 
    //        return a;
    //    }
    //    /// <summary>
    //    /// 按通道名，从 MSGCORE 接口重新获取运行时白名单。
    //    /// </summary>
    //    /// <returns></returns>
    //    public async Task< int> ReGetWLRuntime()
    //    {
    //        if (_flushing) { return 0; }
    //        DateTime cc = TimeProvider.UtcNow;
    //        int a = 0;
    //        if ((cc - _lastWL).TotalSeconds > 32)
    //        {
    //            try
    //            {
    //                _flushing = true;
    //                _lastWL = TimeProvider.UtcNow;

    //                foreach (var t in RunCenter.Settings.TunSettings.TcpTuns)
    //                {
    //                    try
    //                    {
    //                        var adv = await LoadWLRuntime(t.TunName);
    //                        lock (_lk)
    //                        {
    //                            if (RunTimes.ContainsKey(t.TunName))
    //                            {
    //                                RunTimes.Remove(t.TunName);
    //                            }
    //                        }
    //                        if (adv != null)
    //                        {
    //                            WL_Item[] rm = new WL_Item[adv.Length];
    //                            for (int z = 0; z < adv.Length; z++)
    //                            {
    //                                rm[z] = new WL_Item(adv[z]);
    //                            }
    //                            a = a + await AddWL_Runtimes(t.TunName, rm);
    //                            RunCenter.AddLog("GetWLRuntime", "Work Get WL Runtime [" + t.TunName + "] Count:" + adv.Length.ToString(), LogCodeEnum.Debug);
    //                        }
    //                    }
    //                    catch (Exception ze)
    //                    {
    //                        await RunCenter.AddLogAsync("GetWLRuntime", "Work Get WL Runtime [" + t.TunName + "] Ex:" + ze.Message, LogCodeEnum.Warning);
    //                    }
    //                }
    //            }
    //            catch { }
    //            finally { _flushing = false; }
    //        }
            
    //        return a;
    //    }

    //    protected async Task<string[]> LoadWLRuntime(string tunname)
    //    {
    //        string[] a= new string[0];
    //        try {
    //            var rr = await BLADE.MSGCORE.ClientTools.ClientCore.WL_CreatePost(tunname, 10,"WLQRY");
    //            if (rr.Item1 != null || rr.Item1.StatusCode == 200)
    //            {
    //                string jsn = rr.Item1.ResponseText;
    //                var wr = BLADE.TOOLS.BASE.Json.JsonOptions.Deserialize< BLADE.MSGCORE.Models.WL_Response> (jsn);
    //                if (wr.StateCode == 200)
    //                {
    //                    a = new string[wr.ADDRS.Length];
    //                    for (int z = 0; z < wr.ADDRS.Length; z++)
    //                    {
    //                        a[z] = RunCenter.SplString(wr.ADDRS[z].Trim(), "##", true).Trim();
    //                    }
    //                    await RunCenter.AddLogAsync("LoadWLRuntime", "Work WLREQ [" + tunname + "] OK : " + a.Length, LogCodeEnum.Debug);
    //                }
    //            }
    //            else {
    //                await RunCenter.AddLogAsync("LoadWLRuntime", "Work WLREQ [" + tunname + "] : " +rr.Item2 , LogCodeEnum.Debug);
    //            }
    //        }
    //        catch (Exception ze)
    //        { 
    //            await RunCenter.AddLogAsync("LoadWLRuntime", "Work WLREQ [" + tunname + "] Ex:" + ze.Message, LogCodeEnum.Warning);
    //        }
    //        return a;
    //    }
    //}

    /// <summary>
    /// 白名单条目
    /// </summary>
    //public class WL_Item
    //{
    //    /// <summary>
    //    /// 原始地址名：   地址，域名，地址段  
    //    /// </summary>
    //    public string AddressOrIPCD { get; set; } = "";
    //    /// <summary>
    //    /// 地址，地址段，或通过mkv/dns解析出来的地址。
    //    /// </summary>
    //    public string ADVALUE { get; set; } = "";
    //    /// <summary>
    //    /// 更新时间
    //    /// </summary>
    //    public DateTime FlashUTC { get; set; } = TimeProvider.UtcNow.AddMinutes(-30);
    //    /// <summary>
    //    /// 本项目的 原始地址明类型。 当前支持 IPv4， IPv6， CIDR， Domain 四种类型。默认是 Invalid 无效。
    //    /// Domain 时， 需要 dns/mkv 解析出实际地址后，存入 ADVALUE 字段。
    //    /// </summary>
    //    public AddressType TagType { get; set; } = AddressType.Invalid;
    //    public WL_Item(string addressOrIPcd)
    //    {
    //        AddressOrIPCD = addressOrIPcd.Trim().ToLower();
    //        TagType = RunCenter.GetAddressType(AddressOrIPCD);
    //        ADVALUE = AddressOrIPCD;
    //    }
    //}

    /// <summary>
    /// 配置文件中的 本地白名单条目
    /// </summary>
    //public class WL_Local
    //{ 
    //   public string WL_Name { get; set; } = "default";
    //   public string[] AddressOrIPCD { get; set; } = new string[] {"127.0.0.1","192.168.1.0/24","mkv:h.mlez.net","n.dmrham.com" };
    //}

    /// <summary>
    /// 本地配置文件中的白名单集合
    /// </summary>
    //public class WL_Local_Settings
    //{
    //    public WL_Local[] WL_Locals { get; set; }

    //    public WL_Local_Settings()
    //    {
    //        WL_Locals = new WL_Local[2];
    //        WL_Locals[0] = new WL_Local();
    //        WL_Locals[1] = new WL_Local();
    //        WL_Locals[0].WL_Name = "home";
    //        WL_Locals[1].WL_Name = "pve";
    //    }
    //}

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


    #region  UDP trans
    public class UDPTransManager :IDisposable
    {
        private static Socket CreateBoundUdpSocket(IPEndPoint ipep)
        {
            var sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            // 关键：在 Bind 之前设置
            // Windows 语义：这能让“同地址同端口”更容易立刻可重绑（前提是你没有 EXCLUSIVEADDRUSE 冲突）
            sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            // 如果你还想避免“别的进程/别的用户”劫持，需要理解 Windows 的 SO_EXCLUSIVEADDRUSE 权衡：
            // 这里通常不做 Exclusive=true，否则你自己重建时反而容易自锁（除非你完全清楚自己在做独占服务）。
            // sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse, false);

            sock.Bind(ipep);
            return sock;
        }

        /// <summary>
        /// 需要上层指定一个检查IP的方法
        /// </summary>
        /// <param name="tunname">通道名或有意义的分类标记</param>
        /// <param name="ip">需要检查的IP地址</param>
        /// <returns>返回 true 表示通过检查，false 表示拒绝</returns>
        public delegate bool CheckIPDelegate(string tunname, string ip);

        /// <summary>
        /// 【可选】上层应用提供对来源IP检查的方法。返回 true 通过检查， false 拒绝。 
        /// 如果不提供方法，则使用 Center 来检查，这是默认处理方法。
        /// </summary>
        public CheckIPDelegate? CheckIP_func = null;
        public bool Running { get; private set; } = false;
        private readonly UdpTunSet[] _udpTunSets;
        private readonly ConcurrentDictionary<string, UdpTrans> _transfers = new();

        private Channel <tempTrans> _temptrans;
      //  private readonly ConcurrentQueue<tempTrans> _temptrans = new();
        private readonly TimeSpan _timeout = TimeSpan.FromSeconds(30);
        private readonly int _udpIdelBreakMilliseconds;
        protected HomeGateCenter Center;
        private ulong _dropWanCount = 0;
        public int TransCount { get { return _transfers.Count; } }
        public string[] GetTransStatus()
        {
            List<string> a = new List<string>();
            a.Add("UdpTrans: "+_transfers.Count + " | Dropped bag: " + _dropWanCount);
            foreach(var i in _transfers.Values)
            {
                a.Add(i.RoutingKey + " | " + i.GetStatusCount());
            }
            return a.ToArray();
        }
        public UDPTransManager(     HomeGateCenter theCenter )
        {
            Center = theCenter;
            _udpTunSets = Center.Settings.TunSettings.UdpTuns;
            _udpIdelBreakMilliseconds = Center.Settings.TunSettings.UdpIdelBreakMilliseconds  ;
            _temptrans = Channel.CreateBounded<tempTrans>(new BoundedChannelOptions(_udpTunSets.Length*300)
            {
                SingleWriter = true,
                SingleReader = true,
                FullMode = BoundedChannelFullMode.Wait
            });
        }

        public void Start()
        {
            if (_udpTunSets.Length > 0)
            {
                Running = true;
                foreach (var tunSet in _udpTunSets)
                {
                    var localTunSet = tunSet;
                    Task.Run(async () => await ListenWAN(localTunSet));
                }
                if (_udpTunSets.Length > 0)
                {
                    Task.Run(async () => await RequestNewTrans());
                }
            }
        }

        private ushort atmc = (ushort)BLADE.TimeProvider.UtcNow.Millisecond;
        private async ValueTask ListenWAN(UdpTunSet tunSet)
        { 
            using var sk = CreateBoundUdpSocket(new IPEndPoint(IPAddress.Parse(tunSet.WanAddress), tunSet.WanPort));
            using var udpWanClient = new UdpClient { Client = sk };
            UdpReceiveResult _curResult;
            IPEndPoint _curRemoteEndPoint;
            string _curKey = "";
            int idleCount = 0;
            while (Running)
            {
                atmc++;
                if (atmc > 50000) { atmc = 0; }
                try
                {
                    if (udpWanClient.Available > 0)
                    {
                        idleCount = 0;
                        using CancellationTokenSource _ccc = new CancellationTokenSource();
                        _curResult = await udpWanClient.ReceiveAsync(_ccc.Token);
                        _curRemoteEndPoint = _curResult.RemoteEndPoint;
                        _curKey = $"{_curRemoteEndPoint}#{udpWanClient.Client.LocalEndPoint}";

                        if (_transfers.TryGetValue(_curKey, out var trans))
                        {
                            // 已获准来源的包不再检查，直接进入转发队列。
                            TransBag(trans, _curResult.Buffer);
                        }
                        else
                        {
                            if (_temptrans.Reader.CanCount)
                            {
                                if (_temptrans.Reader.Count < (_udpTunSets.Length * 256) && _transfers.Count < Center.Settings.MaxConnection)
                                {
                                    // 未获准的包，要进行检查后处理。
                                    _temptrans.Writer.TryWrite(new tempTrans(tunSet, udpWanClient, _curRemoteEndPoint, _curResult, _curKey));
                                }
                                else { _dropWanCount++; }
                                
                            }
                            else {
                                if ( _transfers.Count < Center.Settings.MaxConnection)
                                {
                                    // 未获准的包，要进行检查后处理。
                                      _temptrans.Writer.TryWrite(new tempTrans(tunSet, udpWanClient, _curRemoteEndPoint, _curResult, _curKey));
                                }
                                else { _dropWanCount++; }
                            }
                            //// if (_temptrans.Count < (_udpTunSets.Length* 256) && _transfers.Count < Center.Settings.MaxConnection )
                            //if ( _temptrans.Reader.Count < (_udpTunSets.Length* 256) && _transfers.Count < Center.Settings.MaxConnection)
                            //{
                            //    // 未获准的包，要进行检查后处理。
                            //    await _temptrans.Writer.WriteAsync(new tempTrans(tunSet, udpWanClient, _curRemoteEndPoint, _curResult, _curKey));
                            //}
                            
                            //  出现大量洪水包则丢弃。
                        }
                        if ((atmc % 450) == 9) { await HomeGateCenter.AddLog("Clear UDP", "Clear Died trans: " + ClearDiedTrans()); }
                    }
                    else
                    {
                        idleCount++;
                        if (idleCount > 3)
                        { await Task.Delay(15); }

                        if (idleCount > 20)
                        {
                            idleCount = 0;
                            await Task.Delay(10);
                        }
                        if ((atmc % 500) == 9) { await HomeGateCenter.AddLogDEBUG("Clear UDP", "Clear Died trans: "+  ClearDiedTrans()); }
                    }
                }
                catch (Exception ex)
                {
                    udpWanClient.Dispose();

                    await HomeGateCenter.AddLog("ListenWAN Error", $"Ex ListenWAN: {ex.Message} \r\n Will Rebuild ListenWAN thread.");
                    await rebuildListenWAN(tunSet);
                }
            }
        }
        private struct tempTrans
        {
            public string curkey;
            public UdpTunSet tunset;
            public UdpClient wanClietn;
            public IPEndPoint remote;
            public UdpReceiveResult res;
            public tempTrans(UdpTunSet intunset,UdpClient inwan, IPEndPoint inremote,UdpReceiveResult indata,string inkey) {
                tunset = intunset; wanClietn = inwan; remote = inremote; res = indata; curkey = inkey;
            }
        }
        private void TransBag(UdpTrans ut , byte[] buf)
        {
            if (ut.Running)
            {
                ut.TranWan2Lan(buf);
            }
        }
        private async ValueTask RequestNewTrans()
        {
            await Task.Delay(80);
            
            while (Running)
            {
                try
                {
                    var tt = await _temptrans.Reader.ReadAsync();

                    if (_transfers.TryGetValue(tt.curkey, out var trans))
                    {
                        TransBag(trans, tt.res.Buffer);
                    }
                    else
                    {
                        if (tt.tunset.UseRule)
                        {
                            if (!CheckIP(tt.tunset.TunName, tt.remote.Address.ToString()))
                            {
                                continue;
                            }
                        }
                        var ntr = new UdpTrans(tt.tunset, tt.wanClietn, tt.remote, tt.res.Buffer, _udpIdelBreakMilliseconds);
                        ntr.Break_TaskRun += takeBreakEvent;
                        _transfers[tt.curkey] = ntr;
                    }
                }
                catch(ChannelClosedException)
                { }
                catch (Exception ) { }
                
            }

        }
        private async void takeBreakEvent(object? sender, string e)
        {
            if (sender != null)
            {   _transfers.TryRemove(((UdpTrans)sender).RoutingKey, out var _);  }
        }
        private async Task rebuildListenWAN(UdpTunSet tunSet)
        {
            if (Running)
            {
                await Task.Delay(700);
                _ = Task.Run(async () => await ListenWAN(tunSet));
            }
        }

        /// <summary>
        /// 检查IP true 通过，false 禁止。
        /// </summary>
        /// <param name="tunname">通道名或有意义的分类管理标记</param>
        /// <param name="address">外部IP地址</param>
        /// <returns>返回 true 表示通过检查，false 表示拒绝</returns>
        private bool CheckIP(string tunname, string address)
        {
            if (CheckIP_func != null) {return CheckIP_func(tunname, address); }
            return Center.CheckIPAllow(tunname, address);
        }

        public void Dispose()
        {
            Running = false;
            _temptrans.Writer.TryComplete();
            Thread.Sleep(100);
             foreach(var i in _transfers.Values)
             {
                 i.Dispose("UDPTransManager Disposing...");
             }
            _transfers.Clear();
            Thread.Sleep(80);
          
            // throw new NotImplementedException();
        }
        private DateTime _lastClearutc = BLADE.TimeProvider.UtcNow;
        public int ClearDiedTrans()
        {
            if ((BLADE.TimeProvider.UtcNow - _lastClearutc).TotalSeconds > 11)
            {
                _lastClearutc = BLADE.TimeProvider.UtcNow;
                if (_transfers.Count > 0)
                {
                    List<string> rm = new List<string>();
                    foreach (var kv in _transfers.Values)
                    {
                        if (!kv.Running)
                        {
                            rm.Add(kv.RoutingKey);
                        }
                    }
                    foreach(var sk  in rm)
                    {
                        if (_transfers.TryRemove(sk, out var ts))
                        {   ts.Dispose("ClearDiedTrans Clear");   }
                    }
                    return rm.Count;
                }
            }

            return 0;
        }
        public class UdpTrans
        {
             
            //=================
            private int UdpIdelBreakMilliseconds { get; set; } = 32000;

            private ulong _w2lBytes = 0;
            private ulong _l2wBytes = 0;
            private ulong _w2lbags = 0;
            private ulong _l2wbags = 0;
            protected void SetW2L(ulong bytes)
            {
                _w2lBytes += bytes;
                _w2lbags++;
            }
            protected void SetL2W(ulong bytes)
            {
                _l2wBytes += bytes;
                _l2wbags++;
            }
            public (ulong bytes, ulong bags) GetW2L() { return (_w2lBytes, _w2lbags); }
            public (ulong bytes, ulong bags) GetL2W() { return (_l2wBytes, _l2wbags); } 
            public string GetStatusCount()
            {
                return $"W2L: {HomeGateCore.GetKM(_w2lBytes)} / {_w2lbags} bags | L2W: {HomeGateCore.GetKM(_l2wBytes)} / {_l2wbags} bags";
            }
            
            /// <summary>
            ///  转发断开， 例如连续的读写超时， 超时闲置等触发事件，通知上层订阅者。
            /// </summary>
            public EventHandler<string>? Break_TaskRun = null;
            private readonly UdpTunSet _tunSet;
            private readonly IPEndPoint RemoteIPEP;
            private readonly UdpClient _WanSide;
            private readonly UdpClient _LanSide;
            private DateTime _lastActivity;
            private byte idleCount = 0;
            private string LansideIPEP { get; set; } = "";
            /// <summary>
            /// 路由Key，用于上层查找对应的转发路径。
            /// </summary>
            public string RoutingKey { get { return $"{RemoteIPEP}#{_WanSide.Client.LocalEndPoint}"; } }
            public UdpTrans(UdpTunSet tunSet, UdpClient wanside, IPEndPoint remoteIPEP, byte[] fristData, int udpIdelBreakMilliseconds = 32000)
            {

                _tunSet = tunSet;
                RemoteIPEP = remoteIPEP;
                _WanSide = wanside;
                UdpIdelBreakMilliseconds = udpIdelBreakMilliseconds;
                _LanSide = new UdpClient();
                _lastActivity = BLADE.TimeProvider.UtcNow;
                Running = true; 
                _LanSide.Connect(_tunSet.LanAddress, _tunSet.LanPort);
                try
                {
                    LansideIPEP = ((IPEndPoint)_LanSide.Client.LocalEndPoint).Port.ToString() + "=" + ((IPEndPoint)_LanSide.Client.RemoteEndPoint).Port.ToString();
                    if (LansideIPEP.Length >= 3)
                    {
                        HomeGateCore.SetBackRoutingTab(LansideIPEP, RemoteIPEP.ToString(), false);
                    }
                }
                catch { }
                _LanSide.Send(fristData, fristData.Length);
                Task.Run(async () => TranLan2Wan());
            }

            /// <summary>
            /// 刷新活动时间，一般不需从外部调用，内部读写会自动调用。
            /// </summary>
            public void Refresh()
            {
                _lastActivity = BLADE.TimeProvider.UtcNow;
            }
             

         //   private int brokenCount = 0;

            /// <summary>
            /// 是否在运行状态， false表示已经停止工作。
            /// </summary>
            public bool Running { get; private set; } = false;

            /// <summary>
            /// 核心工作线程，有数据时进行转发作业，无数据则空闲等待，超时则触发断开事件。
            /// </summary>
            private async void TranLan2Wan()
            {

                while (Running)
                {
                    try
                    {

                        if (wan2landata.Count > 0)
                        {
                            if (wan2landata.TryDequeue(out var data))
                            {
                                //try
                                //{
                                using CancellationTokenSource _cts = new CancellationTokenSource(300);
                                idleCount = 0;
                                var j = await _LanSide.SendAsync(data, _cts.Token);
                                SetW2L((ulong)j);
                                Refresh();
                                // brokenCount = 0;
                                //}
                                //catch (OperationCanceledException)
                                //{ //brokenCount++;
                                //    break;
                                // }
                            }
                        }
                        if (_LanSide.Available > 0)
                        {
                            //try
                            //{
                            using CancellationTokenSource _cts = new CancellationTokenSource(300);
                            var result = await _LanSide.ReceiveAsync(_cts.Token);
                            var j = await _WanSide.SendAsync(result.Buffer, RemoteIPEP,_cts.Token);
                            SetL2W((ulong)j);
                            Refresh();
                            //    brokenCount = 0;
                            //}
                            //catch (OperationCanceledException)
                            //{ brokenCount++; }

                            idleCount = 0;
                        }
                        else
                        {
                            idleCount++;
                            if (idleCount >= 3)
                            {
                                await Task.Delay(15);
                                if (idleCount > 8)
                                {
                                    idleCount = 1;
                                }
                            }
                        }
                        //if (brokenCount > 20)
                        //{
                        //    Dispose("OperationCanceledException 20 times.");
                        //}

                        if ((BLADE.TimeProvider.UtcNow - _lastActivity).TotalMilliseconds > UdpIdelBreakMilliseconds)
                        {
                            Dispose("Idle timeout.");
                        }

                    }
                    catch (Exception ex)
                    {
                        // Console.WriteLine($"Error in TranLan2Wan: {ex.Message}");
                        Running = false;
                        Dispose(ex);
                        break;
                    }
                }
            }
            
            /// <summary>
            /// 外来数据的缓存队列。由TranLan2Wan负责读取，TranWan2Lan写入
            /// </summary>
            private ConcurrentQueue<byte[]> wan2landata = new ConcurrentQueue<byte[]>();
            /// <summary>
            /// 上层接收到外网数据时，调用此方法将数据写入队列，等待核心线程转发到内网。
            /// </summary>
            /// <param name="data"></param>
            /// <returns></returns>
            public bool TranWan2Lan(byte[] data)
            {
                if (Running)
                {
                    wan2landata.Enqueue(data);
                    return true;
                }

                return false;
            }

            private bool disposed = false;
            /// <summary>
            /// 断开并释放，尝试触发 Break_TaskRun 事件通知上层，参数是断开原因说明。 例如异常信息，或闲置超时等。
            /// </summary>
            /// <param name="msg"></param>
            public void Dispose(string msg = "")
            {
                if (disposed) { return; }
                Running = false;
                disposed = true;
                HomeGateCore.DelBackRoutingTab(LansideIPEP, false);
                //_WanSide.Dispose();
                _LanSide.Dispose();
                wan2landata.Clear();
                if (Break_TaskRun != null)
                {
                    if (!string.IsNullOrEmpty(msg))
                    {
                        Task.Run(() => Break_TaskRun.Invoke(this, $"Dispose with Exception: {msg}"));
                    }
                    else
                    {
                        Task.Run(() => Break_TaskRun.Invoke(this, ""));
                    }
                }
            }
            /// <summary>
            /// 断开并释放，尝试触发 Break_TaskRun 事件通知上层，参数是断开原因说明。 例如异常信息，或闲置超时等。
            /// </summary>
            /// <param name="ex"></param>
            public void Dispose(Exception? ex = null)
            {
                if (ex != null)
                {
                    Dispose(ex.Message);
                }
                else
                {
                    Dispose("");
                }
            }
        }
    }

    #endregion
    public class AtomicBoolean
    {
        private byte _value = 0; // 0 = false, 1 = true

        public bool TrySetTrue()
        {
            return Interlocked.CompareExchange(ref _value, 1, 0) == 0;
        }

        public bool TrySetFalse()
        {
            return Interlocked.CompareExchange(ref _value, 0, 1) == 1;
        }

        public bool Value => _value == 1;
    }
}