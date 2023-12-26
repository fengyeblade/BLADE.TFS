using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace BLADE.TCPFORTRESS.CoreNET7
{
    public class WorkMain
    {
        public string MutexName { get; set; }
        protected SortedList<string, TransPart.TunListener> TCPLS = new SortedList<string, TransPart.TunListener>(StringComparer.OrdinalIgnoreCase);


        public WorkMain(string muxName)
        {
            MutexName = muxName;
        }
        protected async Task<int> Start()
        {
            int a = 0;
            bool setreloadTun = false;
            for (int z = 0; z < ServiceRunCenter.RunSet.Tuns.Length; z++)
            {
                //循环启动转发器
                TransPart.TunListener tl = new TransPart.TunListener();

                try
                {
                    if (await tl.SetListenOpen(ServiceRunCenter.RunSet.Tuns[z]))
                    {
                        a++;
                        TCPLS.Add(ServiceRunCenter.RunSet.Tuns[z].TunName, tl);

                        if (setreloadTun == false)
                        {
                            tl.ReloadListAct = true;
                            setreloadTun = true;
                        }

                        // 启动转发器的循环线程
                        tl.startLoop();
                        ServiceRunCenter.LOG.AddLog(BASETOOL.NET7.LogCodeEnum.Info, "StartUp() OpenTun: " + ServiceRunCenter.RunSet.Tuns[z].TunName + " Road: " + ServiceRunCenter.RunSet.Tuns[z].Road);
                    }
                    else
                    {
                        tl.SetStop();
                    
                        tl = null;
                        ServiceRunCenter.LOG.AddLog(BASETOOL.NET7.LogCodeEnum.Alert, "StartUp() OpenTun Canot Listen : " + ServiceRunCenter.RunSet.Tuns[z].TunName + " Road: " + ServiceRunCenter.RunSet.Tuns[z].Road);

                    }
                }
                catch (Exception e)
                {
                    ServiceRunCenter.LOG.AddLog(BASETOOL.NET7.LogCodeEnum.Warning, "Error = StartUp() OpenTun: " + ServiceRunCenter.RunSet.Tuns[z].TunName + " Road: " + ServiceRunCenter.RunSet.Tuns[z].Road + " EX: " + e.Message);
                }

            }



            return a;
        }

        protected async Task<string> Stop()
        {
            TransPart.TunListener[] ts = TCPLS.Values.ToArray();
            for (int z = 0; z < ts.Length; z++)
            {
                // 关掉所有侦听器和连接
                try
                {
                    ts[z].SetAutoStop();
                }
                catch { }
            }
            string sss = "Stoping Service... Stop Listener: " + ts.Length.ToString();
            await ServiceRunCenter.LOG.AddLog(false,"EXIT", sss);
            Thread.Sleep(100);
            TCPLS.Clear();
            Thread.Sleep(100);
            await ServiceRunCenter.LOG.SaveLogs(true);

            return sss;
        }


        /// <summary>
        /// 探测指定名称MutexName 的互斥锁的存在,探测后立即释放互斥锁，不保留。    
        /// 如果互斥锁不存在，则返回True，表示可以继续。   
        /// 如果互斥锁已经存在，则返回False，表示应停止。 
        /// </summary>
        /// <returns>
        /// true = 可以继续，互斥锁目前不存在。    
        /// false = 不可继续，互斥锁已经存在。
        /// </returns>
        public bool TestMyMutex()
        {
            bool fff = false;
            using (Mutex M = new Mutex(true, MutexName, out fff))
            {
                M.ReleaseMutex();
                return !fff;
            }
        }
    }
}
