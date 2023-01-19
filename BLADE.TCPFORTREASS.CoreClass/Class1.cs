using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Runtime.InteropServices;
using BLADE.TCPFORTRESS.CoreClass.DB.DBV;
using System.Net;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace BLADE.TCPFORTRESS.CoreClass
{
    [Serializable]
    /// <summary>
    ///  基础设置信息类，用于序列化XML文件保存。
    /// </summary>
    public class Settings
    {
        /// <summary>
        /// 设置DEBUG模式
        /// </summary>
        public bool Debug = true;
        /// <summary>
        /// 数据库连接字符串
        /// </summary>
        public string DBStr = "Data Source=127.0.0.1,22233;  Initial Catalog=TFS;User ID=TFS;Password =pard;";
        public string DBName = "TFS";
        /// <summary>
        /// 运行名单模式   0=使用白名单    2=使用黑名单模式    1=灰名单运行模式，不判定白名单或黑名单，只是根据重连次数判定灰名单进行临时性限制。
        /// </summary>
        public int RunWithWhiteOrBlack = 2;

        /// <summary>
        /// 达到统计计数后 LongLockGray=false  对来访IP的封锁时长。
        /// </summary>
        public int TimeLockSecond = 600;
        /// <summary>
        /// 统计连接计数时间 秒
        /// </summary>
        public int TimeSecond = 90;
        /// <summary>
        /// 默认链接计数限制  此处仅是默认值。 每个Tun转发通道有单独的限制设置。
        /// </summary>
        public int TimeCount = 9;

        /// <summary>
        /// 无论运行名单模式选择黑白灰，对于重复连接达到计数值的地址进行灰名单封禁，并记录到数据库。 
        /// true = 不进行超时封禁   false = 超过Lock时间后解封
        /// </summary>
        public bool LongLockGray = true;

        /// <summary>
        /// 对于运行时产生的灰名单地址，自动记录到数据库是 false=记录为灰名单   true=直接记录为黑名单
        /// </summary>
        public bool RecordAutoAddBlackList = true;

        /// <summary>
        /// 文本日志保存时间间隔
        /// </summary>
        public int LogTimeSecond = 600;
        /// <summary>
        /// 文本日志保存数量间隔
        /// </summary>
        public int LogItemCount = 300;
        /// <summary>
        /// 日志文件目录
        /// </summary>
        public string LogFilePath = "\\logs";
        /// <summary>
        /// 暂时弃用
        /// </summary>
        public string LogFileName = "TFL_SN_.log";

        /// <summary>
        /// 端口转发映射管道  需要至少有一个！ 服务的生命周期需要
        /// </summary>
        public TunSet[] Tuns= new TunSet[1];
    }


    #region PAN 判定程序

    /// <summary>
    /// 黑名单判定类。  
    ///    加载黑名单数据库=》AllList  不需加载白名单和灰名单。 
    ///    运行时 将新接入IP 放入 TMPLIST  并判断时间和计数。 达到计数后将号码存入数据库（存灰或黑根据Settings决定）
    ///    计时周期内，未达到限制次数的连接地址，放行。 达到限制次数的地址，根据 Settings.LongLockGray=true 视作黑名单长时间封禁
    /// </summary>
    public class Pan_Black : iPan
    {
        protected string info = "Black List PAN.";
        public string WoB { get { return info; } }

        /// <summary>
        /// 检查地址是否
        /// </summary>
        /// <param name="inAddress">传入的地址对象</param>
        /// <returns> true = 允许通过   false = 禁止通过 </returns>
        public async Task<bool> CheckPass(TFS_Address inAddress, int tunLockCount)
        {
              
            if (ServiceRunCenter.AllList.ContainIP(inAddress.TFS_AddressStr, inAddress.TFS_K3, inAddress.TFS_K2, inAddress.TFS_K1))
            {

                // 检查地址在黑名单 禁止通过
               await ServiceRunCenter.LOG.AddLogDebug(302, "Check Blacked : " + inAddress.TFS_AddressStr);
                return false;
            }
            //检查地址 不存在于黑名单  
            AddressListItem AA = new AddressListItem();
            AA.SetUp(inAddress);
            //继续判断是否在运行时灰名单内
            return await ServiceRunCenter.TmpList.TLockIP(AA,   tunLockCount);

        }
        /// <summary>
        /// 从数据库加载名单
        /// </summary>
        /// <returns>加载数量</returns>
        public async Task<int> Load_AllList()
        {
            //加载黑名单
            return await ServiceRunCenter.LoadList(2);
        }
    }




    /// <summary>
    /// 灰名单判定类。
    ///     不加载黑白名单 AllList留空。
    ///     运行时 将新接入IP 放入 TMPLIST  并判断时间和计数。 达到计数后将号码存入数据库（存灰或黑根据Settings决定）
    ///     
    /// </summary>
    public class Pan_Gray : iPan
    {
        protected string info = "Gray List PAN.";
        public string WoB { get { return info; } }

        public async Task<bool> CheckPass(TFS_Address inAddress, int tunLockCount)
        {
            AddressListItem AA = new AddressListItem();
            AA.SetUp(inAddress);
            return await ServiceRunCenter.TmpList.TLockIP(AA,   tunLockCount);
        }
        public async Task<int> Load_AllList()
        {
            // throw new NotImplementedException();

            return 0;
        }
    }



    /// <summary>
    /// 白名单判定类。
    ///    加载白名单 =》AllList  不加载黑灰名单。
    ///    运行时 将新接入IP 放入 TMPLIST  并判断时间和计数。 达到计数后将号码存入数据库（存为灰名单，用于分析）
    /// </summary>
    public class Pan_White : iPan
    {
        protected string info = "White List PAN.";
        public string WoB { get { return info; } }

        public async Task<bool> CheckPass(TFS_Address inAddress, int tunLockCount)
        {
            
             
            if(ServiceRunCenter.AllList.ContainIP(inAddress.TFS_AddressStr, inAddress.TFS_K3, inAddress.TFS_K2, inAddress.TFS_K1))
            {
                await ServiceRunCenter.LOG.AddLogDebug(300, "Check WHITE : " + inAddress.TFS_AddressStr);
                return true; }
            else
            {
                AddressListItem AA = new AddressListItem();
                AA.SetUp(inAddress);
                 await ServiceRunCenter.TmpList.TLockIP(AA,   tunLockCount);
            }
            return false;
        }

        public async Task<int> Load_AllList()
        {
            return await ServiceRunCenter.LoadList(0);
        }
    }



    /// <summary>
    /// 名单判断类接口。黑白名单实体类实现。
    /// </summary>
    public interface iPan
    {
        /// <summary>
        /// 判断传入的IP是否允许通过。 并根据实际需要进行地址计数。
        /// 判断是否已经记录地址
        /// 判断连接计数
        /// 判断是否添加灰 黑名单
        /// </summary>
        /// <param name="inAddress">传入的地址对象</param>
        /// <returns> 检查IP的结果  true 表示允许通过，并不表示IP是否存在于黑白名单。false表示禁止通过。同样不表示是否存于黑白名单内。</returns>
       Task< bool> CheckPass(DB.DBV.TFS_Address inAddress  ,int tunLockCount );
        /// <summary>
        /// 获取实体类的说明信息，黑白名单和规则说明
        /// </summary>
        string WoB { get; }

        /// <summary>
        /// 根据PAN 判定程序的需要 从数据库加载AllList
        /// </summary>
        /// <returns> 读取的List数量 </returns>
          Task<int> Load_AllList();
    }
    #endregion



    /// <summary>
    /// SetApp 设置应用程序的 运行时配置类。包含XML的Settings读写编辑，Log日志管理。
    /// </summary>
    public class AppRunCenter
    {
        /// <summary>
        /// Error 信息 。   初始化 init 方法时如果出现异常（返回 999），LOG组件尚未初始化，所以需要通过此变量手动提取错误信息。
        /// </summary>
        public static string Error = "";

        /// <summary>
        /// 应用程序的运行目录
        /// </summary>
        public static string RunRoot = "";

        /// <summary>
        /// 设置类对象
        /// </summary>
        public static Settings RunSet= new Settings();
        /// <summary>
        /// LOG日志组件
        /// </summary>
        public static BLADE.BASETOOL.VNET4.Loger LOG;
        /// <summary>
        /// 黑白名单列表对象。黑白由Settings控制
        /// </summary>
        public static AddressList_L1 AllList= new AddressList_L1();

        /// <summary>
        /// 初始化标志   init 方法完成后 为 true
        /// </summary>
        protected static bool inited = false;
        /// <summary>
        /// 运行标志   App 或 Service
        /// </summary>
        public static string RunName = "App";

        /// <summary>
        /// 初始化方法，加载设置对象，加载日志对象
        /// </summary>
        /// <param name="inroot">应用程序启动路径，不含文件名</param>
        /// <returns>1=正常    999=出现异常，请检查Error变量</returns>
        public static async Task<int> Init(string inroot)
        {
           // int a = 0;
            RunRoot = inroot;


            string setFile = RunRoot + "\\Settings.cfg";
            try
            {
                if (File.Exists(setFile))
                {
                    // 读取设置类的xml 并加载设置对象
                    XmlSerializer xs = new XmlSerializer(typeof(Settings));
                    StreamReader sr = File.OpenText(setFile);
                    RunSet = (Settings)xs.Deserialize(sr);
                    sr.Close();
                    sr.Dispose();

                }
                else
                {
                    // 初始化默认设置对象 并生成xml设置文件
                    RunSet = new Settings();
                    RunSet.Tuns[0] = new TunSet();
                    XmlSerializer xs = new XmlSerializer(typeof(Settings));
                    StreamWriter sw = File.CreateText(setFile);
                    xs.Serialize(sw, RunSet);
                    sw.Close();
                    sw.Dispose();
                }
            }
            catch (Exception zeee)
            {
                Error = "Init Error " + zeee.ToString();
                return 999;
            }
          
            DB.Configs.SetDbConStr(RunSet.DBStr);
            string logp = RunSet.LogFilePath;
            if (logp.StartsWith("\\") || logp.StartsWith("/"))
            {
                logp = RunRoot + logp;
            }

            //初始化LOG对象
            LOG = new BASETOOL.VNET4.Loger(logp, RunName, false, RunSet.LogItemCount, RunSet.LogTimeSecond, "log");
            // 设置LOG组件启用debug模式   否则不记录debug日志条目
            LOG.Debug = RunSet.Debug;

            inited = true;

            //  初始化完成  AppRunCenter

            return 1;
        }

        /// <summary>
        /// 从数据库 读取名单
        /// </summary>
        /// <param name="wob">0=白名单   2=黑名单   1=灰名单</param>
        /// <returns>读取名单的数量</returns>
        public static async Task<int> LoadList(int wob)
        {
            int a = 0;
            

            AddressList_L1 curAL = new AddressList_L1();
            DB.TFS_Address_DBT[] ll;
            for(int z=0;z< 10;z++)
            {
                //  根据 X 字段分批加载数据库的 黑白名单   X 为地址的第二段首字符 0-9
                try
                {

                    ll = await (Task.Run(() => DB.TFS_Address_DBT.SelectByWhere(60000, " X=" + z.ToString() + " and TFS_WhiteOrBlack=" + wob.ToString(), null)));
                    a = a + ll.Length;
                    for (int w = 0; w < ll.Length; w++)
                    {
                        //放入名单列表

                        AddressListItem nn = new AddressListItem();

                        nn.SetUp(ll[w].VO);
                        nn.DB_saved = true;

                        curAL.AddItem(nn);
                    }
                }catch(Exception zxz)
                {
                    LOG.AddLog(" LoadList(int wob)  EX: " + zxz.ToString());
                }
            }

            AddressList_L1 claAL = AllList;
            AllList = curAL;
            claAL.ClearList();

            return a;
        }

        public static async Task<CoreClass.DB.DBV.TFS_Address[]> GetPardonList()
        {
            CoreClass.DB.TFS_Address_DBT[] pd = await (Task.Run(() => (CoreClass.DB.TFS_Address_DBT.SelectByWhere(500, "TFS_WhiteOrBlack = -99  ORDER BY TFS_AID DESC", null))));
            CoreClass.DB.DBV.TFS_Address[] pa= new TFS_Address[pd.Length];
            for (int z = 0; z < pd.Length; z++)
            {
                pa[z] = pd[z].VO;
            }
            return pa;
        }
    }
    /// <summary>
    /// Service 服务的 运行时配置类。继承自 AppRunCenter 。  包含XML的Settings读写编辑，Log日志管理。
    /// </summary>
    public class ServiceRunCenter : AppRunCenter
    {
        public static iPan PAN = null;
        public static DateTime loadlistTime = DateTime.Now;
       
        /// <summary>
        /// 运行时 名单列表。  多次出现的接入请求会被存入数据库。
        /// </summary>
        public static AddressList_L1 TmpList = new AddressList_L1();

        /// <summary>
        /// 初始化运行时对象 需要手动调用。会自动调用基类的 init
        /// </summary>
        /// <param name="runroot">应用程序启动巨路径  不含文件名</param>
        /// <returns>0=基类已经初始化  1=基类一同初始化  999=出现异常，请检查Error</returns>
        public static async Task<int> Init_2(string runroot)
        {
            int a = 0;
            RunName = "Service";

            //判断基类是否已经初始化
            if (!inited)
            {
                a = await Init(runroot);
                if (a == 999)
                {
                    return a;
                }
            }

            // 启用黑白名单模式
            if (RunSet.RunWithWhiteOrBlack == 2)
            {
                PAN = new Pan_Black();
            }
            else
            {
                if (RunSet.RunWithWhiteOrBlack == 1)
                { PAN = new Pan_Gray(); }
                else { PAN = new Pan_White(); }
            }


            await LOG.AddLog(false, 100, "Service Start With: " + PAN.WoB + " ListNum:" + (await PAN.Load_AllList()).ToString());

            return a;
        }

        /// <summary>
        /// 接入IP地址检查 
        /// </summary>
        /// <param name="inIp">接入端的IP地址</param>
        /// <param name="inSet">转发管道的 TunSet 设置信息</param>
        /// <returns> 检查IP的结果  true 表示允许通过，并不表示IP是否存在于黑白名单。false表示禁止通过。同样不表示是否存于黑白名单内。 </returns>
        public static async Task<bool> CheckInIP(string inIp, TunSet inSet)
        {
            bool cc = false;

            // 对传入的地址分段
            string[] nnn = inIp.Split(new string[] { ":", ",", ".", "/" }, StringSplitOptions.RemoveEmptyEntries);
            DB.DBV.TFS_Address ta = new DB.DBV.TFS_Address();
            ta.TFS_AddressStr = inIp;
            ta.TFS_ALastTime = DateTime.Now;
            if (nnn.Length > 0) { ta.TFS_K1 = nnn[0]; } else { ta.TFS_K1 = "0"; }
            if (nnn.Length > 1) { ta.TFS_K2 = nnn[1]; } else { ta.TFS_K2 = "0"; }
            if (nnn.Length > 2) { ta.TFS_K3 = nnn[2]; } else { ta.TFS_K3 = "0"; }
            ta.fenX();

            //暂时没写判断地址类型的代码，暂时不需要
            ta.TFS_IpV6 = false;
            ta.TFS_CIDR = false;

            //设置为灰名单
            ta.TFS_WhiteOrBlack = 1;
           
            try
            {
                if (inSet.UseRule)
                {
                    //规则启用  判定黑白名单
                    cc = await PAN.CheckPass(ta,inSet.LockCount);
                }
                else
                {
                    cc = true;
                }
            }
            catch (Exception zz)
            {
                await LOG.AddLog(false, 123, "PAN  " + inIp + "  " + inSet.TunName + "  EX: " + zz.ToString());
            }


            return cc;
        }

        public static async Task<int> PardonGray()
        {
            int a = 0;
            try
            {
                TFS_Address[] ts = await GetPardonList();
                lock (TmpList)
                {
                    for (int z = 0; z < ts.Length; z++)
                    {
                        AddressListItem tai = new AddressListItem();
                        tai.SetUp(ts[z]);
                        a = a + TmpList.DelAddr(tai).Result;


                    }
                }
            }catch(Exception zxz)
            {
                LOG.AddLog("PardonGray() EX : "+zxz.ToString());
            }
            return a;
        }
        
    }


    #region  Address地址类

    /// <summary>
    /// 地址对象
    /// </summary>
    public class AddressListItem  
    {
        /// <summary>
        /// 数据库地址对象
        /// </summary>
        public DB.DBV.TFS_Address Address = null;
        /// <summary>
        /// 地址判定程序
        /// </summary>
        public BLADE.BASETOOL.VNET4.IPCD IC = null;
        /// <summary>
        /// 是否已经存入数据库标记
        /// </summary>
        public bool DB_saved = false;
 

        protected bool _Ready = false;
        public bool Ready { get { return _Ready; } }

        /// <summary>
        /// 初始化方法
        /// </summary>
        /// <param name="ina">传入一个数据地址对象</param>
        /// <returns></returns>
        public bool SetUp(DB.DBV.TFS_Address ina)
        {
            
            ina.TFS_AddressStr = ina.TFS_AddressStr.Replace(" ", "").ToUpper().Trim();
            Address = ina;
          
            IC = new BASETOOL.VNET4.IPCD(Address.TFS_AddressStr);
         
            _Ready = true;
            return true;
        }
    }


    /// <summary>
    /// 地址 一级容器
    /// </summary>
    public class AddressList_L1
    {
        /// <summary>
        /// 一级KEY 分列的二级地址容器
        /// </summary>
        protected SortedList<string, AddressList_L2> AList = new SortedList<string, AddressList_L2>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 清理
        /// </summary>
        public void ClearList()
        {
            lock (AList)
            {
                for (int z = 0; z < AList.Count; z++)
                {
                    AList.Values[z].ClearList();
                }
                AList.Clear();
                AList = null;
                AList = new SortedList<string, AddressList_L2>(StringComparer.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// 添加地址容器
        /// </summary>
        /// <param name="K3">地址K3</param>
        /// <param name="K2">地址K2</param>
        /// <param name="K1">地址K1</param>
        /// <returns></returns>
        public int AddItemList(string K3, string K2,string K1)
        {
            lock (AList)
            {
                //添加一级KEY
                if (AList.ContainsKey(K1))
                {

                }
                else { AList.Add(K1, new AddressList_L2()); }
            }
            //添加下级
            AList[K1].AddItemList(K3,K2);
            return AList.Count;
        }
        //添加地址项目
        public void AddItem(AddressListItem inA)
        {
           
            //添加容器
            AddItemList(inA.Address.TFS_K3, inA.Address.TFS_K2, inA.Address.TFS_K1);
            //添加项目
            AList[inA.Address.TFS_K1].AddItem(inA);

        }

        /// <summary>
        /// 检查比对地址
        /// </summary>
        /// <param name="checkIP">判定IP地址</param>
        /// <param name="K3">地址K3</param>
        /// <param name="K2">地址K2</param>
        /// <param name="K1">地址K1</param>
        /// <returns>true = 检查地址存在名单内  false = 不存在</returns>
        public bool ContainIP(string checkIP, string K3, string K2, string K1)
        {
            bool rr = false;
            ServiceRunCenter.LOG.AddLogDebug(225, "ContainIP " + checkIP);
            try
            {
                if (AList.ContainsKey(K1))
                {
                    //向下级比对 完整比对三级
                    if (AList[K1].ContainIP(checkIP, K3, K2))
                    {
                        rr = true;
                    }
                    else
                    {
                        // 比对二级地址
                        if (AList[K1].ContainIP(checkIP, "0", K2))
                        {
                            rr = true;
                        }
                        else
                        {
                            //比对一级地址
                            if (AList[K1].ContainIP(checkIP, "0", "0"))
                            {
                                rr = true;
                            }

                        }
                    }
                }
            }catch(Exception zez)
            {
                ServiceRunCenter.LOG.AddLog("L1.ContainIP() EX : " + zez.ToString());
            }
            return rr;
        }

        /// <summary>
        /// 删除一个地址对象。 仅传入纯IP地址时 不会删除IP段
        /// </summary>
        /// <param name="inA"></param>
        /// <returns></returns>
        public async Task<int> DelAddr(AddressListItem inA)
        {
            int a = 0;
            if (AList.ContainsKey(inA.Address.TFS_K1))
            {
                if (AList[inA.Address.TFS_K1].AList.ContainsKey(inA.Address.TFS_K2))
                {
                    if (AList[inA.Address.TFS_K1].AList[inA.Address.TFS_K2].AList.ContainsKey(inA.Address.TFS_K3))
                    {
                        SortedList<string, AddressListItem> ttt = AList[inA.Address.TFS_K1].AList[inA.Address.TFS_K2].AList[inA.Address.TFS_K3].AList;
                       if( ttt.ContainsKey(inA.Address.TFS_AddressStr))
                        {
                            ttt.Remove(inA.Address.TFS_AddressStr);
                            a++;
                        }
                        
                        

                    }
                }
            }

            return a;
        }
        /// <summary>
        /// 逐级查找地址  仅针对纯IP集合有效  例如TMPLIST 。   对地址段混合查找不支持。
        /// </summary>
        /// <param name="inA">要查找的地址</param>
        /// <returns>查找到的地址对象</returns>
        public async Task<AddressListItem> FindA(AddressListItem inA)
        {
            if(AList.ContainsKey(inA.Address.TFS_K1))
            {
                if (AList[inA.Address.TFS_K1].AList.ContainsKey(inA.Address.TFS_K2))
                {
                    if (AList[inA.Address.TFS_K1].AList[inA.Address.TFS_K2].AList.ContainsKey(inA.Address.TFS_K3))
                    {
                        SortedList<string, AddressListItem> ttt = AList[inA.Address.TFS_K1].AList[inA.Address.TFS_K2].AList[inA.Address.TFS_K3].AList;
                        for(int z=0;z<ttt.Count; z++)
                        {
                            if( await (Task.Run(()=> ttt.Values[z].IC.Contains(inA.Address.TFS_AddressStr))))
                            {
                                return ttt.Values[z];
                            }
                        }
                       
                    }
                }
            }

            return null;
        }
        /// <summary>
        /// 综合判断 IP地址是否已经存在记录，判断计数，判断时间锁定，存入数据库.  此方法用于TMPLIST
        /// </summary>
        /// <param name="inA">传入的地址</param>
        ///    <param name="tunLockCount">所需要判断地址的通道中设置的连接限数</param>
        /// <returns>true 放行    false 禁止</returns>
        public async Task<bool> TLockIP(AddressListItem inA, int tunLockCount)
        {

            //判断列表中是否存在 检查的地址。   TMPLIST中只存在IP地址，没有地址段。所以ContainIP不会返回地址段包含true
            if (ContainIP(inA.Address.TFS_AddressStr, inA.Address.TFS_K3, inA.Address.TFS_K2, inA.Address.TFS_K1))
            {
                try
                {
                    // 取出找到的地址
                    AddressListItem aaa = await FindA(inA);

                    if (aaa == null)
                    {
                        // inA.Address.fenX();

                        AddItem(inA);
                        return true;
                    }
                    else
                    {
                        // 此地址的连接计数增加
                        aaa.Address.TFS_ReactCount++;
                        if (aaa.Address.TFS_ReactCount > (tunLockCount + 150))
                        { aaa.Address.TFS_ReactCount = (tunLockCount + 80); }





                        // 计数判断
                        if (aaa.Address.TFS_ReactCount > tunLockCount)
                        {
                            //计数超出限制

                            //锁定LOCK时间判断   如果最后一次连接时间差已经超过了锁定时间设置，判断 RunSet.LongLockGray 决定继续长久封锁  或 恢复连接计数到0 .
                            if ((DateTime.Now - aaa.Address.TFS_ALastTime).TotalSeconds > ServiceRunCenter.RunSet.TimeLockSecond)
                            {
                                await ServiceRunCenter.LOG.AddLogDebug(307, "Check TMPLISTed : " + inA.Address.TFS_AddressStr);
                                //时间过了锁定LOCK时间
                                if (ServiceRunCenter.RunSet.LongLockGray)
                                {
                                    return false;
                                }
                                else
                                {
                                    // 归零计数器
                                    aaa.Address.TFS_ReactCount = 0;
                                    aaa.Address.TFS_ALastTime = DateTime.Now;
                                    return true;
                                }

                            }



                            aaa.Address.TFS_ALastTime = DateTime.Now;
                            //如果没存，存入数据库
                            if (!aaa.DB_saved)
                            {
                                //保存数据库
                                if (ServiceRunCenter.RunSet.RecordAutoAddBlackList)
                                {
                                    aaa.Address.TFS_WhiteOrBlack = 2;
                                }
                                else { aaa.Address.TFS_WhiteOrBlack = 1; }
                                DB.TFS_Address_DBT AA = new DB.TFS_Address_DBT();

                                AA.V = aaa.Address;
                                try
                                {
                                    await (Task.Run(() => AA.SaveByInsert()));
                                    aaa.DB_saved = true;
                                }
                                catch (Exception zez)
                                {
                                    ServiceRunCenter.LOG.AddLog("L1.TLockIP() SaveAD to DB EX : " + zez.ToString());

                                }
                                await ServiceRunCenter.LOG.AddLog(false, 308, "= =       MaxCount:" + tunLockCount.ToString() + "  Save TMPLIST to DB : " + aaa.Address.TFS_AddressStr);
                            }

                            // 返回封禁
                            return false;
                        }



                        //计数未超出限制，判断时间间隔

                        if ((DateTime.Now - aaa.Address.TFS_ALastTime).TotalSeconds > ServiceRunCenter.RunSet.TimeSecond)
                        {
                            //时间过了 计数间隔时长

                            // 归零计数器
                            aaa.Address.TFS_ReactCount = 0;


                        }
                        aaa.Address.TFS_ALastTime = DateTime.Now;

                        //放行
                        return true;



                    }
                }
                catch (Exception zez)
                {
                    ServiceRunCenter.LOG.AddLog("L1.TLockIP() EX : " + zez.ToString());
                   
                }
                return true;
            }
            else
            {
                // 列表中未找到   填入列表中
                await ServiceRunCenter.LOG.AddLog(false,333, "+ +     Add List Item by TLockIP : "+inA.Address.TFS_AddressStr);
                try
                {
                    AddItem(inA);
                }
                catch (Exception zez)
                {
                    ServiceRunCenter.LOG.AddLog("L1.TLockIP() else EX : " + zez.ToString());
                }
                return true;
            }
        }

        
        public    string[]  ShowAllIPaddress()
        {
            
            List<string> mmm = new List<string>();
            lock(AList)
            {
                for(int z=0;z<AList.Count;z++)
                {
                    try
                    {
                        AddressList_L2 tl2 = AList.Values[z];
                        for(int x=0;x<tl2.AList.Count;x++)
                        {
                            AddressList_L3 tl3 = tl2.AList.Values[x];
                            for(int y=0;y<tl3.AList.Count;y++)
                            {
                                AddressList tl4 = tl3.AList.Values[y];
                                for(int p=0;p<tl4.AList.Count;p++)
                                {
                                    
                                    mmm.Add(  tl4.AList.Values[p].Address.TFS_AddressStr + " B:"
                                        + tl4.AList.Values[p].Address.TFS_WhiteOrBlack.ToString() + "  T:" + tl4.AList.Values[p].Address.TFS_ALastTime.ToString("yyyyMMdd HHmmss")
                                        + "  C:" + tl4.AList.Values[p].Address.TFS_ReactCount.ToString()+"  ");

                                }
                            }
                        }
                    }catch
                    { continue; }
                }
            }
           
            return mmm.ToArray();
        }
    }
    public class mess{ public int num = 0; public string Text = ""; }
    /// <summary>
    /// 地址 二级 容器
    /// </summary>
    public class AddressList_L2
    {
        public SortedList<string, AddressList_L3> AList = new SortedList<string, AddressList_L3>(StringComparer.OrdinalIgnoreCase);
        public void ClearList()
        {
            lock(AList)
                {
                for(int z=0;z<AList.Count;z++)
                {
                    AList.Values[z].ClearList();
                }
                AList.Clear();
                AList = null;
                AList = new SortedList<string, AddressList_L3>(StringComparer.OrdinalIgnoreCase);
            }
        }
        public void AddItem(AddressListItem inA)
        {

            AddItemList(inA.Address.TFS_K3,inA.Address.TFS_K2);
            AList[inA.Address.TFS_K2].AddItem(inA);

        }
        public int AddItemList(string K3,string K2)
        {
            lock (AList)
            {
                if (AList.ContainsKey(K2))
                {

                }
                else { AList.Add(K2, new AddressList_L3()); }
            }
            AList[K2].AddItemList(K3);
            return AList.Count;
        }
        public bool ContainIP(string checkIP, string K3,string K2)
        {
            bool rr = false;
            if (AList.ContainsKey(K2))
            {
                rr = AList[K2].ContainIP(checkIP,K3);
            }
            return rr;
        }
    }

    /// <summary>
    /// 地址 三级 容器
    /// </summary>
    public class AddressList_L3
    {
        public SortedList<string, AddressList> AList = new SortedList<string, AddressList>(StringComparer.OrdinalIgnoreCase);
        public void ClearList()
        {
            lock(AList)
            {
                for(int z=0;z<AList.Count;z++)
                {
                    AList.Values[z].ClearList();
                }
                AList.Clear();
                AList = null;
                AList = new SortedList<string, AddressList> (StringComparer.OrdinalIgnoreCase);
            }
        }
        public int AddItemList(  string K3)
        {
            lock(AList)
            {
                if(AList.ContainsKey(K3))
                {

                }
                else { AList.Add(K3, new AddressList()); }
            }
            return AList.Count;
        }
        public void AddItem(AddressListItem inA)
        {
            AddItemList(inA.Address.TFS_K3);
            AList[inA.Address.TFS_K3].AddItem(inA);

        }

        public bool ContainIP(string checkIP,string K3)
        {
            bool rr = false;
            if(AList.ContainsKey(K3))
            {
               rr=  AList[K3].ContainIP(checkIP);
            }
            return rr;
        }
    }

    /// <summary>
    /// 地址 四级 容器    AddressListItem 的底级 SortedDictionary 列表 。 其中的KEY 是完整的地址字符串 TFS_AddressStr 
    /// </summary>
    public class AddressList
    {
        /// <summary>
        /// 以完整地址（段）作为KEY 的地址集合
        /// </summary>
        public  SortedList<string,AddressListItem>  AList = new SortedList<string, AddressListItem>(StringComparer.OrdinalIgnoreCase);
        public void ClearList()
        {
            lock (AList)
            {
                AList.Clear();
                AList = null;

                AList = new SortedList<string, AddressListItem>(StringComparer.OrdinalIgnoreCase);
            }
        }
        /// <summary>
        /// 添加地址项 
        /// </summary>
        /// <param name="inA"> 需要填入的地址项</param>
        /// <returns>集合数量</returns>
        public int AddItem(AddressListItem inA)
        {
            lock (AList)
            {
                if(AList.ContainsKey(inA.Address.TFS_AddressStr))
                { }
                else
                {
                    AList.Add(inA.Address.TFS_AddressStr, inA);
                }
            }
            return AList.Count;
        }

        /// <summary>
        /// 地址范围判定程序  不依靠KEY  遍历列表中每个项目进行范围判定
        /// </summary>
        /// <param name="checkIP"></param>
        /// <returns></returns>
        public bool ContainIP(string checkIP)
        {
            bool rr = false;
            for(int z=0;z< AList.Count;z++)
            { 
                //遍历每个地址段  判定检查地址是否等于 集合项目或在项目段之内
                if (AList.Values[z].IC.Contains(checkIP.Trim().ToUpper()))
                {
                    AList.Values[z].Address.TFS_ALastTime = DateTime.Now;

                     rr= true;
                    break;
                }
            }
            return rr;
        }

    }

    #endregion 


    [Serializable]
    /// <summary>
    /// 端口转发管道配置
    /// </summary>
    public class TunSet
    {
        /// <summary>
        /// 转发名称
        /// </summary>
        public string TunName = "DEF 2000-2222";
        /// <summary>
        /// 转发管道的  侦听端口 
        /// </summary>
        public int InPort = 2000;
        /// <summary>
        /// 转发管道的  侦听地址   默认 0.0.0.0
        /// </summary>
        public string InAddress = "0.0.0.0";
        /// <summary>
        /// 转发管道的  目标  远端端口
        /// </summary>
        public int OutPort = 2222;
        /// <summary>
        /// 转发管道的  目标  远端地址
        /// </summary>
        public string OutAddress = "192.168.100.100";
        public int MTUSize = 1400;
        /// <summary>
        /// 设置此管道是否应用名单规则  具体的应用规则由 Settings.RunWithWhiteOrBlack 决定。 true = 应用   false = 不应用规则，直通
        /// </summary>
        public bool UseRule = false;

        /// <summary>
        /// 速度限制   单向限制 单位 KB
        /// </summary>
        public int SpeedMax = 1024;

        /// <summary>
        /// 连接限制  每个通道单独配置 除白名单模式外  超过限制则封锁到灰名单
        /// </summary>
        public int LockCount = 9;
        /// <summary>
        /// 转发说明
        /// </summary>
        public string Road
        {
            get
            {
                return InAddress + ":" + InPort.ToString() + " TO " + OutAddress + ":" + OutPort.ToString() + " R_" + UseRule.ToString();
            }
        }
    }
}
