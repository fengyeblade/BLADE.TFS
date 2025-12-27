using BLADE;
using BLADE.MSGCORE.Models;
using BLADE.TOOLS.BASE;
using BLADE.TOOLS.LOG;
using BLADE.TOOLS.WEB;
using System;
using System.Collections.Generic;
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
    public class HomeGateCore
    {

    }
    public enum AddressType
    {
        Invalid,
        IPv4,
        IPv6,
        CIDR,
        Domain
    }
    public class RunCenter
    {
        public static WL_Runtime WLR = new WL_Runtime();

        public static async ValueTask TryFlushWLR()
        {
            await WLR.ReGetWLRuntime();
            WLR.Flush();
        }
        public static BLADE.TOOLS.LOG.Loger? CLOG = null;
        public static GateSettings? Settings = null;
        public static string AppStartPath { get; set; } = "";
        public static bool Inited { get; private set; } = false;
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
                    var se = BLADE.TOOLS.BASE.Json.JsonOptions.Serialize<GateSettings>(Settings);
                    if (se != null && se.Length > 5)
                    {
                        using (StreamWriter sr = System.IO.File.CreateText(settingsFile))
                        { await sr.WriteAsync(se); }
                        R = new Result(true, "配置文件不存在，已创建默认配置文件", Settings); isnull = false;
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
                await CLOG.AddLogAsync(LogCodeEnum.App, "Init", "Load Settings File OK : " + setFile);
                RR = new Result<GateSettings>(true, "Init OK", Settings);
                BLADE.MSGCORE.ClientTools.ClientCore.RunSet = Settings.ClientSettings;
                if (Settings.WhiteListSettings.WL_Locals.Length > 0)
                { WLR.AddWL_Locals(Settings.WhiteListSettings.WL_Locals); }
            }
            Inited = RR.Successful;
            return RR;
        }

        public static void AddLog(string title, string msg, LogCodeEnum code = LogCodeEnum.Debug)
        {
            if (CLOG != null)
            {
                if (code != LogCodeEnum.Debug || CLOG.Debug) { CLOG.AddLog(code, title, msg); }
            }
        }
        public static async ValueTask AddLogAsync(string title, string msg, LogCodeEnum code = LogCodeEnum.Debug)
        {
            if (CLOG != null)
            {
                if (code != LogCodeEnum.Debug || CLOG.Debug) { await CLOG.AddLogAsync(code, title, msg); }
            }
        }
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

        public static bool PanInclude(string body, string val)
        {
            val = val.Trim().ToLower();
            string[] bs = body.Split(new string[] { ";", " ", "#" }, StringSplitOptions.RemoveEmptyEntries);
            for (int z = 0; z < bs.Length; z++)
            {
                if (bs[z].Trim().ToLower() == val)
                {
                    return true;
                }
            }
            return false;
        }
        public static string SplString(string allbody, string spl, bool usefont=true)
        {
            if (string.IsNullOrEmpty(spl)) return allbody;

            int index = allbody.IndexOf(spl);
            if (index == -1) return allbody;

            return usefont ? allbody.Substring(0, index) : allbody.Substring(index + spl.Length);
        }

    }
    public class GateSettings
    {
        public string DisConnectMsg { get; set; } = "";
        public ushort MaxConnection { get; set; } = 200;
        public ushort IdelBreakMilliseconds { get; set; } = 24000;
        public BLADE.MSGCORE.ClientTools.ProSettings ClientSettings { get; set; } = new MSGCORE.ClientTools.ProSettings();
        public BLADE.TOOLS.WEB.WorkSetting WebSettings { get; set; } = new WorkSetting();
        public TunTransSettings TunSettings { get; set; } = new TunTransSettings();
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
        /// true = UP   false = Down
        /// </summary>
        public bool Arr = true;

        public int BagSize = 1400;

        public long TransBytesCount = 0;
    }
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

        public int AddWL_Runtimes(string tunname,  WL_Item[] rims)
        {
            int a = 0;
            tunname = tunname.Trim().ToLower();
            if (tunname.Length < 1  ) { return a; }
            if (rims != null && rims.Length > 0)
            {
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

        public bool AddWL_Runtime(string tunname, string addressorIPCD)
        {
            tunname = tunname.Trim().ToLower();
            addressorIPCD = addressorIPCD.Trim().ToLower();
            if (tunname.Length < 1 || addressorIPCD.Length<1) { return false; }
            var i = new WL_Item(addressorIPCD);
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
        public async Task<string> GetIP(string input)
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
        protected async Task<string> mkvIP(string ip)
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
        protected async Task<string> dnsIP(string ip)
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
        public bool CheckIP(string tunname, string inIP)
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
                                lock (_lk)
                                {
                                    if ((TimeProvider.UtcNow - wi.FlashUTC).TotalSeconds > 15)
                                    { wi.ADVALUE = GetIP(wi.AddressOrIPCD).Result; wi.FlashUTC = TimeProvider.UtcNow; }
                                    if (RunCenter.PanInclude( wi.ADVALUE.ToLower() , inIP)) { fd = true; break; }
                                }
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
                                    lock (_lk)
                                    {
                                        if ((TimeProvider.UtcNow - wi.FlashUTC).TotalSeconds > 17)
                                        { wi.ADVALUE = GetIP(wi.AddressOrIPCD).Result; wi.FlashUTC = TimeProvider.UtcNow; }
                                        if (RunCenter.PanInclude(wi.ADVALUE.ToLower(), inIP)) { fd = true; break; }
                                    }
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
        public int Flush()
        {
            DateTime cc = TimeProvider.UtcNow;
            int a = 0;
            if ((cc - _lastflush).TotalSeconds > 20)
            {
                lock (_lk)
                {
                    foreach (var vs in KVS.Values)
                    {
                        if (vs != null && vs.TagType==AddressType.Domain && (cc - vs.FlashUTC).TotalSeconds > 15)
                        {   vs.ADVALUE= GetIP(vs.AddressOrIPCD).Result; vs.FlashUTC = TimeProvider.UtcNow; a++;  }
                    }

                }
                _lastflush = TimeProvider.UtcNow;
            }
            return a;
        }
        public async Task< int> ReGetWLRuntime()
        {
            DateTime cc = TimeProvider.UtcNow;
            int a = 0;
            if ((cc - _lastWL).TotalSeconds > 32)
            {
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
                            if (adv != null)
                            {
                                WL_Item[] rm = new WL_Item[adv.Length];
                                for (int z = 0; z < adv.Length; z++)
                                {
                                    rm[z] = new WL_Item(adv[z]);
                                }
                                a =a + AddWL_Runtimes(t.TunName, rm);
                                RunCenter.AddLog("GetWLRuntime", "Work Get WL Runtime [" + t.TunName + "] Count:" + adv.Length.ToString(), LogCodeEnum.Debug);
                            }
                        }
                        
                    }
                    catch (Exception ze)
                    {
                        await RunCenter.AddLogAsync("GetWLRuntime", "Work Get WL Runtime [" + t.TunName + "] Ex:" + ze.Message,LogCodeEnum.Warning);
                    }
                }
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
    public class WL_Item
    {
      //  public string Name { get; set; } = "";
        public string AddressOrIPCD { get; set; } = "";
        public string ADVALUE { get; set; } = "";
        public DateTime FlashUTC { get; set; } = TimeProvider.UtcNow.AddMinutes(-30);
        public AddressType TagType { get; set; } = AddressType.Invalid;
        public WL_Item(string addrorpipcd)
        {
            AddressOrIPCD = addrorpipcd.Trim().ToLower();
            TagType = RunCenter.GetAddressType(AddressOrIPCD);
        }
    }
    public class WL_Local
    { 
       public string WL_Name { get; set; } = "default";
       public string[] AddressOrIPCD { get; set; } = new string[] {"127.0.0.1","192.168.1.0/24","mkv:h.mlez.net","n.dmrham.com" };
    }
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
}