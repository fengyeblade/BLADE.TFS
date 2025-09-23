using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using BLADE.TOOLS.LOG;
using BLADE.TOOLS.BASE;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Runtime.CompilerServices;

namespace BLADE.TCPFORTRESS.CoreNET
{
    public class TfsCORE : IDisposable
    {
        protected SortedList<string, TunListener> TCPLS = new SortedList<string, TunListener>(StringComparer.OrdinalIgnoreCase);
        private bool _disposed = false;
         
        private bool _running = false;
        public bool Running { get { return _running; }  private set { _running = value;   } }
        public string State
        {
            get   {   string kk = " Tuns Running: " + TCPLS.Count.ToString()+" \r\n\r\n";
                try
                {
                    TunListener[] tvs = TCPLS.Values.ToArray();
                    foreach (var tlt in tvs)
                    {
                        try
                        {
                            kk = kk + "  --  " + tlt.State + "\r\n";
                        }
                        catch (Exception zex)
                        {
                            kk = kk + "  --  item ex: " + zex.Message + "\r\n";
                        }
                    }
                }
                catch (Exception ex)
                {
                    kk = kk + "  --  Exception: " + ex.Message + "\r\n";
                }
                return kk;
            }
        }
        public TfsCORE()
        {
            // Initialize logging or other resources here if needed
 
        }
        public  void Dispose()
        {
            Running = false;
            if (!_disposed)
            {
                _disposed = true;
                ServiceRunCenter.LOG.SaveLogs(true);
            }
            // Clean up resources here if needed
            GC.SuppressFinalize(this);
            
        }
        ~TfsCORE()
        {
            Dispose();
        }


        public async Task<string> StartWork()
        {
            string r = "";
            // TODO:   start work with tfs listener
            bool setreloadTun = false;
            TimeProvider.Global.TimeoutMicroseconds = 2560;
            for (int z = 0; z < ServiceRunCenter.RunSet.Tuns.Length; z++)
            {
                //循环启动转发器
                TunListener tl = new TunListener();
                if (await tl.SetListenOpen(ServiceRunCenter.RunSet.Tuns[z]))
                {
                    
                    TCPLS.Add(ServiceRunCenter.RunSet.Tuns[z].TunName, tl);

                    if (setreloadTun == false)
                    {
                        tl.ReloadListAct = true;
                        setreloadTun = true;
                    }

                    // 启动转发器的循环线程
                    tl.startLoop();
                    ServiceRunCenter.LOG.AddLog("Service StartUp() OpenTun: " + ServiceRunCenter.RunSet.Tuns[z].TunName + " Road: " + ServiceRunCenter.RunSet.Tuns[z].Road);
                }
                else
                {
                    tl.SetStop();
                    tl = null;
                }

            }

            await ServiceRunCenter.LOG.SaveLogs(true);
           
            return r;
        }
        public async Task<string> StopWork()
        {
            string r = "";
            if (TCPLS.Count  <1 )
            {
                r = "Listener is not initialized.";
            }
            else {
                TunListener[] ts = TCPLS.Values.ToArray();
                for (int z = 0; z < ts.Length; z++)
                {
                    // 关掉所有侦听器和连接
                    try
                    {
                        ts[z].SetAutoStop();
                    }
                    catch { }
                }
               
                ServiceRunCenter.LOG.AddLog(false, 106, "Stoping Service... Stop Listener: " + ts.Length.ToString());
                Thread.Sleep(130);
                TCPLS.Clear();
                Thread.Sleep(120);
               
            }
            await ServiceRunCenter.LOG.AddLog(false, 88, "TFS CORE Stop Work ");
            Dispose();
            return r;
        }
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

        public int BagSize = 4096;
    }

    /// <summary>
    /// TCP转发器，使用线程池生成两个 管道 Tao ， 分别负责上下行， 使用ThreadPool
    /// </summary>
    public class Trans
    {
        public DateTime LastTran = DateTime.Now;
        /// <summary>
        /// 接入端
        /// </summary>
        public TcpClient inClient = null;
        /// <summary>
        /// 转接端
        /// </summary>
        public TcpClient outClient = null;
        protected TunSet _tunset = null;
        public string Road { get { return _tunset.Road; } }
        protected bool _running = false;
        public bool IsRunning { get { return _running; } }
        public async Task<bool> StopClose()
        {
            _running = false;
            Thread.Sleep(30);
            try
            {
                if (inClient != null)
                {

                    if (inClient.Connected)
                    {
                        await (Task.Run(() => inClient.Close()));
                    }
                    inClient.Dispose();
                }
                inClient = null;
            }
            catch { }
            try
            {
                if (outClient != null)
                {
                    if (outClient.Connected)
                    {
                        await (Task.Run(() => outClient.Close()));
                    }
                    outClient.Dispose();
                }
                outClient = null;
            }
            catch { }


            return true;
        }

        /// <summary>
        /// 启动双向转发   依赖 ThreadPool
        /// </summary>
        /// <param name="inSet"></param>
        public void StartTrans(TunSet inSet)
        {
            _tunset = inSet;
            _running = true;
            try
            {
                // new outClient
                outClient = new TcpClient(_tunset.OutAddress, _tunset.OutPort);
                outClient.LingerState = new LingerOption(true, 1);
                inClient.ReceiveTimeout = 38000;
                inClient.SendTimeout = 38000;
                inClient.ReceiveBufferSize = _tunset.MTUSize * 8;
                inClient.SendBufferSize = _tunset.MTUSize * 8;
                outClient.ReceiveTimeout = 38000;
                outClient.SendTimeout = 38000;
                outClient.ReceiveBufferSize = _tunset.MTUSize * 8;
                outClient.SendBufferSize = _tunset.MTUSize * 8;

                // 上行 Tao
                Tao shangxing = new Tao();
                shangxing.Arr = true;
                shangxing.BagSize = _tunset.MTUSize;
                shangxing.TcpIn = inClient;
                shangxing.TcpOut = outClient;

                //下行 Tao
                Tao xiaxing = new Tao();
                xiaxing.Arr = false;
                xiaxing.BagSize = _tunset.MTUSize;
                xiaxing.TcpIn = outClient;
                xiaxing.TcpOut = inClient;


                // start Thread
                ThreadPool.QueueUserWorkItem(new WaitCallback(transfer), shangxing);
                ThreadPool.QueueUserWorkItem(new WaitCallback(transferD), xiaxing);
                ServiceRunCenter.LOG.AddLogDebug(260, " Opened  ThreadPool.QueueUserWorkItem by StartTrans at " + _tunset.TunName);
            }
            catch (Exception zee)
            {
                _running = false;
                ServiceRunCenter.LOG.AddLog(false, 117, "Open Trans EX: " + inSet.TunName + "  " + zee.ToString());
            }

        }
        protected int looptim = 0;
        /// <summary>
        /// 上行计数
        /// </summary>
        protected long UpCount = 0;
        /// <summary>
        /// 下行计数
        /// </summary>
        protected long DownCount = 0;


        /// <summary>
        /// 流量计速
        /// </summary>
        protected long _CurSpeedUP = 0;
        /// <summary>
        /// 流量计速
        /// </summary>
        protected long _CurSpeedDOWN = 0;
        public string CurSpeed
        {
            get
            {
                return "UP:" + GetDANWEI(_CurSpeedUP) + "/s  DOWN：" + GetDANWEI(_CurSpeedDOWN) + "/s";
            }
        }
        /// <summary>
        /// 获取流量计数带单位
        /// </summary>
        /// <param name="inlong">流量计数</param>
        /// <returns>流量带单位</returns>
        protected string GetDANWEI(long inlong)
        {
            string U = "B";
            if (inlong > 2000000)
            {
                if (inlong > 2000000000)
                {
                    U = (inlong / ((decimal)(1024 * 1024 * 1024))).ToString("#.##") + " G";
                }
                else
                {
                    U = (inlong / ((decimal)(1024 * 1024))).ToString("#.##") + " M";
                }
            }
            else
            {
                if (inlong > 2000)
                {
                    U = (inlong / ((decimal)1024)).ToString("#.##") + " K";
                }
                else
                {
                    U = inlong.ToString() + " B";
                }
            }
            return U;
        }

        /// <summary>
        /// 获取 上下行流量计数，带单位
        /// </summary>
        public string UpDownCount
        {
            get
            {
                return "Up: " + GetDANWEI(UpCount + 1) + "  Down: " + GetDANWEI(DownCount + 1);


            }
        }
        /// <summary>
        /// 转发接管 子线程方法  上行 
        /// </summary>
        /// <param name="obj">Tao 接管对象</param>
        public void transfer(object obj)
        {
            //速度计数
            long stepcount = 1;

            //速度计时
            DateTime timestep = DateTime.Now;
            //速度计步
            int step = 1;
            Tao ttaa = (Tao)obj;
            byte[] bt = new byte[ttaa.BagSize];
            NetworkStream ns1 = ttaa.TcpIn.GetStream();
            NetworkStream ns2 = ttaa.TcpOut.GetStream();

            //最新通信时间
            LastTran = DateTime.Now;

            //单次读写计数
            int count = 0;

            //速度计时 毫秒差
            int jst = 1;

            //速度 再次控制开关
            bool jxsleep = false;
            double cpssU = 0;
            while (_running)
            {
                try
                {

                    //再次控制速度
                    if (jxsleep)
                    {
                        jxsleep = false;
                        Thread.Sleep(300);
                    }

                    step++;

                    //速度计步
                    if (step > 80)
                    {
                        jst = (int)((TimeProvider.LocalNow - timestep).TotalMilliseconds);
                        //计速 时间差大于1秒
                        if (jst > 1000)
                        {
                            step = 0;
                            timestep = TimeProvider.LocalNow;
                            cpssU = (stepcount / 1024) * 1000 / jst;
                            cpssU = cpssU / (_tunset.SpeedMax + 1);
                            if (cpssU > 1)
                            {
                                //判断超限

                                //控速 0.2秒
                                Thread.Sleep(200);
                                if (cpssU < 1.3)
                                {
                                    //不增加限速

                                }
                                else
                                {
                                    //超过 1.3倍 增加限速
                                    if (cpssU < 1.7)
                                    {
                                        //控速 +0.2秒
                                        Thread.Sleep(200);

                                    }
                                    else
                                    {
                                        // 超过 1.7倍  控速 +0.2秒  打开再控
                                        Thread.Sleep(200);
                                        //再次控速开关 开
                                        jxsleep = true;
                                    }
                                }

                            }
                            else
                            {
                                //未超速

                                if (cpssU < 0.8)
                                {
                                    //全速
                                }
                                else
                                {
                                    //预警轻控速 0.08秒
                                    Thread.Sleep(80);
                                }
                            }

                            if ((_tunset.SpeedMax * 2) < cpssU)
                            {   //再次控速开关 开
                                jxsleep = true;
                            }


                            if (_tunset.SpeedMax < cpssU)
                            {   //控速 0.4秒
                                Thread.Sleep(300);
                            }
                            _CurSpeedUP = stepcount + 1;
                            stepcount = 1;
                        }
                    }

                    //有活动数据
                    if (ns1.DataAvailable)
                    {
                        count = ns1.Read(bt, 0, bt.Length);
                        ns2.Write(bt, 0, count);

                        // 速度计数
                        stepcount = stepcount + count;

                        // 上行流量计数
                        UpCount = UpCount + count;
                        LastTran = TimeProvider.LocalNow;
                        looptim = 0;
                    }
                    else
                    {   //  无活动数据
                        looptim++;
                        if (looptim > 12)
                        {

                            //检查连接活性
                            looptim = 0;
                            if ((TimeProvider.LocalNow - LastTran).TotalSeconds > 30)
                            {
                                //  30秒无通信  断开并清理
                                ServiceRunCenter.LOG.AddLog(false, 117, "transfer UP TimeOut 30s. Closing Trans. " + _tunset.TunName + " : " + ttaa.TcpIn.Client.RemoteEndPoint.ToString());
                                _running = false;
                                ttaa.TcpIn.Dispose();
                                ttaa.TcpOut.Dispose();
                            }
                        }
                        else
                        {
                            Thread.Sleep(6 * looptim);
                        }
                    }


                }
                catch (Exception zee)
                {
                    _running = false;
                    if (ttaa.TcpIn != null)
                    {
                        try { ttaa.TcpIn.Dispose(); } catch { }
                    }
                    if (ttaa.TcpOut != null)
                    {
                        try { ttaa.TcpOut.Dispose(); } catch { }
                    }

                    ServiceRunCenter.LOG.AddLog(false, 119, "transfer UP " + _tunset.TunName + "  EX: " + zee.ToString());
                }
            }
            ServiceRunCenter.LOG.AddLogDebug(119, "=       NetCount  " + this._tunset.TunName + "   " + UpDownCount);
        }
        /// <summary>
        /// 转发接管 子线程方法  下行   
        /// </summary>
        /// <param name="obj">Tao 接管对象</param>
        public void transferD(object obj)
        {


            #region 原理 与上行 transfer 相同，原本是一个方法，为了区分控制 分为两个方法。

            long stepcount = 1;

            DateTime timestep = DateTime.Now;
            int step = 1;
            Tao ttaa = (Tao)obj;
            byte[] bt = new byte[ttaa.BagSize];
            NetworkStream ns1 = ttaa.TcpIn.GetStream();
            NetworkStream ns2 = ttaa.TcpOut.GetStream();
            LastTran = DateTime.Now;
            int count = 0;
            int jst = 1;
            bool jxsleep = false;

            while (_running)
            {
                try
                {
                    if (jxsleep)
                    {
                        jxsleep = false;
                        Thread.Sleep(400);
                    }
                    step++;
                    if (step > 80)
                    {
                        jst = (int)((TimeProvider.LocalNow - timestep).TotalMilliseconds);
                        if (jst > 1000)
                        {
                            step = 0;
                            timestep = TimeProvider.LocalNow;
                            if ((_tunset.SpeedMax * 2) < (int)(stepcount / 1024))
                            { jxsleep = true; }
                            if (_tunset.SpeedMax < (int)(stepcount / 1024))
                            {
                                Thread.Sleep(400);
                            }
                            _CurSpeedDOWN = stepcount + 1;
                            stepcount = 0;
                        }
                    }
                    if (ns1.DataAvailable)
                    {
                        count = ns1.Read(bt, 0, bt.Length);
                        ns2.Write(bt, 0, count);
                        stepcount = stepcount + count;
                        DownCount = DownCount + count;
                        LastTran = TimeProvider.LocalNow;
                        looptim = 0;
                    }
                    else
                    {
                        looptim++;
                        if (looptim > 12)
                        {
                            looptim = 0;
                            if ((TimeProvider.LocalNow - LastTran).TotalSeconds > 30)
                            {
                                ServiceRunCenter.LOG.AddLog(false, 117, "transfer Down TimeOut 30s. Closing Trans. " + _tunset.TunName + " : " + ttaa.TcpOut.Client.RemoteEndPoint.ToString());
                                _running = false;
                                ttaa.TcpIn.Dispose();
                                ttaa.TcpOut.Dispose();
                            }
                        }
                        else
                        {
                            Thread.Sleep(6 * looptim);
                        }
                    }


                }
                catch (Exception zee)
                {
                    _running = false;
                    if (ttaa.TcpIn != null)
                    {
                        try { ttaa.TcpIn.Dispose(); } catch { }
                    }
                    if (ttaa.TcpOut != null)
                    {
                        try { ttaa.TcpOut.Dispose(); } catch { }
                    }

                    ServiceRunCenter.LOG.AddLog(false, 119, "transfer DOWN " + _tunset.TunName + " EX: " + zee.ToString());
                }
            }
            #endregion

        }

    }

    /// <summary>
    /// Tun 侦听器    对应每个转发管道生成一个侦听器。使用单独线程循环处理。 每有远端客户端连入，判断对端IP是否通过验证，生成转发器Trans。
    /// </summary>
    public class TunListener
    {
        protected Thread selfThread = null;
        protected DateTime ShowAlives = DateTime.Now;
        /// <summary>
        /// 重加载名单开关
        /// </summary>
        public bool ReloadListAct = false;
        /// <summary>
        /// Trans 转发器集合
        /// </summary>
        protected SortedList<string, Trans> TransList = new SortedList<string, Trans>(StringComparer.OrdinalIgnoreCase);
        /// <summary>
        /// 转发器设置对象
        /// </summary>
        protected TunSet _tunSet = null;

        public string TunName { get { return _tunSet.TunName; } }

        public int TransCount { get { return TransList.Count ; } }

        public string State
        {
            get
            {
                string k = _tunSet.TunName + " " + TransList.Count + "  ##  ";

                string[] ks = TransList.Keys.ToArray();
                foreach (var i in ks)
                {
                    k = k +" [ "+ i + " ] ";
                }
                return k;
            }
        }
        /// <summary>
        /// TCP 侦听器
        /// </summary>
        protected TcpListener Listener = null;

        /// <summary>
        /// 循环控制变量
        /// </summary>
        protected bool _listening = false;

        /// <summary>
        /// 获取 侦听器是否在运行中
        /// </summary>
        public bool Listening { get { return _listening; }  set { if (value) { } else { SetAutoStop(); } }   }

        /// <summary>
        /// 启动Listener，设置开始接受连接，但未启动处理循环。  处理循环需另外手动调用 startLoop() 方法来启动循环线程。
        /// </summary>
        /// <param name="inTunSet">转发管道设置</param>
        /// <returns>返回 _listening  运行状态。并非是启动成功与否。</returns>
        public async Task<bool> SetListenOpen(TunSet inTunSet)
        {
            _tunSet = inTunSet;
            // 先清理
            if (Listener != null)
            {
                if (_listening)
                {
                    _listening = false;
                    await (Task.Run(() => Listener.Stop()));
                }
            }

            // 初始化侦听器
            Listener = new TcpListener(System.Net.IPAddress.Parse(inTunSet.InAddress), inTunSet.InPort);

            await ServiceRunCenter.LOG.AddLogDebug(220, "Open Listen: " + _tunSet.InAddress + ":" + _tunSet.InPort.ToString());
            Listener.Server.LingerState = new LingerOption(true, 1);
            Listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            // 开始侦听 
            await (Task.Run(() => Listener.Start()));
            _listening = true;
            return _listening;
        }


        /// <summary>
        /// 启动工作循环线程。开始接受传入的连接请求。 必须在 SetListenOpen（） 方法之后手动调用
        /// </summary>
        public void startLoop()
        {
            if (selfThread != null)
            {
                _listening = false;
                Thread.Sleep(50);
                selfThread = null;
            }
            _listening = true;
            selfThread = new Thread(new ThreadStart(RunLoop));
            selfThread.Start();
            ServiceRunCenter.LOG.AddLogDebug(220, "Open LoopThread: " + selfThread.ManagedThreadId.ToString());
        }

        protected int heartCount = 0;


        /// <summary>
        /// 侦听循环
        /// </summary>
        protected async void RunLoop()
        {
            bool kp = false;
            int js = 0;
            ServiceRunCenter.LOG.AddLog("Work Thread Listener RunLoop " + this._tunSet.TunName + " : " + Thread.CurrentThread.ManagedThreadId.ToString() + " Starting!");
            while (_listening)
            {
                kp = false;
                js = js + 1;
                if (this.ReloadListAct)
                {
                    heartCount++;
                    // 心跳日志   大约间隔4分钟
                    if (heartCount > 3999)
                    {
                        heartCount = 0;

                        // await ServiceRunCenter.LOG.AddLog(false, 555, "HEART Log");
                        string pdnips = "error pardon";
                        try { pdnips = (await ServiceRunCenter.PardonGray()).ToString(); } catch (Exception ppe) { pdnips = ppe.Message; }
                        await ServiceRunCenter.LOG.AddLog(false, 99, "Heart and Make Pardon ips : " + pdnips);

                    }
                }
                //获取新进入的连接
                if (Listener.Pending())
                {
                    try
                    {

                        //获取传入连接
                        TcpClient getNTCP = Listener.AcceptTcpClient();
                        getNTCP.LingerState = new LingerOption(true, 1);
                        string nip = ((IPEndPoint)getNTCP.Client.RemoteEndPoint).Address.ToString().ToUpper().Trim();
                        if (_tunSet.DName != "")
                        { _tunSet.OutAddress = DNameCatch.GetIP(_tunSet.DName); }
                        await ServiceRunCenter.LOG.AddLog(true, 101, _tunSet.InPort.ToString() + " Get Tcp In. From : " + getNTCP.Client.RemoteEndPoint.ToString());

                        //判断IP是否允许通过
                        if (await ServiceRunCenter.CheckInIP(nip, _tunSet))
                        {
                            //允许连接， 生成Trans 转发器
                            Trans ttt = new Trans();
                            ttt.inClient = getNTCP;

                            TransList.Add(GetSn.GetStringSn() + "_" + nip, ttt);
                            //启动转发子线程
                            ttt.StartTrans(_tunSet);

                        }
                        else
                        {
                            //扔掉连接
                            getNTCP.Close();
                            getNTCP.Dispose();
                            getNTCP = null;
                            await ServiceRunCenter.LOG.AddLog(true, 114, " Kill TcpClient From: " + nip);

                        }
                    }
                    catch (Exception zee)
                    {
                        await ServiceRunCenter.LOG.AddLog(false, 301, _tunSet.TunName + " RunLoop() EX: " + zee.ToString());
                    }

                    kp = true;
                }

                if (kp)
                {

                }
                else
                {
                    //无新连接进入，则让出线程。
                    Thread.Sleep(60);

                }

                // 清理已经中断的连接
                if (js > 28)
                {
                    js = 0;
                    lock (TransList)
                    {
                        for (int z = 0; z < TransList.Count; z++)
                        {
                            string sssnn = TransList.Keys[z];
                            Trans ts = TransList[sssnn];
                            if (!ts.IsRunning)
                            {
                                ts.StopClose();
                                TransList.Remove(sssnn);
                                ServiceRunCenter.LOG.AddLogDebug(105, "Remove Tcp Trans: " + sssnn);
                                break;
                            }
                        }
                    }

                    if ((DateTime.Now - this.ShowAlives).TotalMinutes > 10)
                    {
                        // 每20分 整理一次 活动中的连接清单
                        string comm = this._tunSet.TunName + " TransList: \r\n";
                        for (int z = 0; z < TransList.Count; z++)
                        {
                            try
                            {
                                Trans ttr = TransList.Values[z];
                                comm = comm + "  with:    " + ((IPEndPoint)ttr.inClient.Client.RemoteEndPoint).Address.ToString() + " CUR: " + ttr.UpDownCount + "  Cspd: " + ttr.CurSpeed + "\r\n";
                            }
                            catch
                            { continue; }
                        }
                        this.ShowAlives = DateTime.Now;
                        await ServiceRunCenter.LOG.AddLog(false, 12, comm);
                    }

                    if (this.ReloadListAct && (DateTime.Now - ServiceRunCenter.loadlistTime).TotalMinutes > 60)
                    {

                        //  所有侦听器中的第一个  每一个小时 整理全局的 灰名单，重载黑白名单，处理赦免清单
                        ServiceRunCenter.loadlistTime = DateTime.Now;
                        string[] grayarry = ServiceRunCenter.TmpList.ShowAllIPaddress();
                        await ServiceRunCenter.LOG.AddLog(false, 11, "RunningGrayIPCount:       " + grayarry.Length.ToString());
                        string xiaoshi = "";

                        for (int s = 0; s < grayarry.Length; s = s + 10)
                        {
                            for (int t = 0; t < 10; t++)
                            {
                                if (s + t < grayarry.Length)
                                {
                                    xiaoshi = xiaoshi + "\r\n" + grayarry[s + t];
                                }
                            }
                            await ServiceRunCenter.LOG.AddLog(false, 11, "CurGrayList: " + xiaoshi);
                            xiaoshi = "";
                        }
                        await ServiceRunCenter.LOG.AddLog(false, 277, " ReLoad List " + ServiceRunCenter.PAN.WoB + " " + (await ServiceRunCenter.PAN.Load_AllList()).ToString());
                        //   await ServiceRunCenter.LOG.AddLog(false, 99, " Make Pardon ips : " + (await ServiceRunCenter.PardonGray()).ToString());

                        await ServiceRunCenter.LOG.SaveLogs(true);
                        try { GC.Collect(); } catch { }
                    }
                }
            }


            //循环跳出  停止侦听和关闭连接
            await ServiceRunCenter.LOG.AddLog(false, 201, "Stop Listener Loop " + this._tunSet.TunName);
            try
            {
                await SetStop();
            }
            catch { }
        }


        /// <summary>
        /// 设置循环控制变量关闭，可以让子线程循环自动停止。
        /// </summary>
        public async void SetAutoStop()
        {

            _listening = false;
            await ServiceRunCenter.LOG.AddLogDebug(220, "SetAutoStop: " + _tunSet.TunName);
        }

        /// <summary>
        /// 设置控制变量关闭，并及时清理资源。
        /// </summary>
        /// <returns></returns>
        public async Task<bool> SetStop()
        {
            _listening = false;
            Thread.Sleep(50);
            try
            {
                await (Task.Run(() => Listener.Stop()));
            }
            catch { }

            for (int z = 0; z < TransList.Count; z++)
            {
                await TransList[TransList.Keys[z]].StopClose();
            }
            TransList.Clear();
            try { selfThread.Abort(); }
            catch { }
            selfThread = null;

            await ServiceRunCenter.LOG.AddLog(false, 220, "SetStop: " + _tunSet.TunName);

            return _listening;
        }

    }

}
