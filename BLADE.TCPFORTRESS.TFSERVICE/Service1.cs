using BLADE.TCPFORTRESS.CoreClass;
using BLADE.TCPFORTRESS.CoreClass.TransPart;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BLADE.TCPFORTRESS.TFSERVICE
{
    public partial class Service1 : ServiceBase
    {
        public Service1()
        {
            InitializeComponent();
        }
       
        protected async override void OnStart(string[] args)
        {
            eventLog1.Source = "TFS EVENT";
            
            try
            {
               if( (await CoreClass.ServiceRunCenter.Init_2(System.AppDomain.CurrentDomain.BaseDirectory) )==999)
                {
                    //初始化异常  写入系统日志
                    eventLog1.WriteEntry(CoreClass.ServiceRunCenter.Error);
                    eventLog1.Close();
                    return;
                }

                await ServiceRunCenter.LOG.AddLog(false,105, "Start Listener: "+  (await StartUp()).ToString());

            }catch(Exception ze)
            {
                eventLog1.WriteEntry("TFS ONSTART()  EX: "+ze.Message,EventLogEntryType.Warning);
                eventLog1.Close();
            }


        }

        protected override void OnStop()
        {
            TunListener[] ts = TCPLS.Values.ToArray();
            for(int z=0;z<ts.Length;z++)
            {
                // 关掉所有侦听器和连接
                try { ts[z].SetAutoStop();
                }
                catch { }
            }
            
            ServiceRunCenter.LOG.AddLog(false, 106, "Stoping Service... Stop Listener: " + ts.Length.ToString());
            Thread.Sleep(130);
            TCPLS.Clear();
            Thread.Sleep(120);
            ServiceRunCenter.LOG.SaveLogs(true);

        }

        protected SortedList<string , TunListener> TCPLS=new SortedList<string, TunListener>(StringComparer.OrdinalIgnoreCase);
        protected async Task<int> StartUp()
        {
            int a = 0;
            bool setreloadTun = false;
            for (int z = 0; z < ServiceRunCenter.RunSet.Tuns.Length; z++)
            {
                //循环启动转发器
                TunListener tl = new TunListener();
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
                    ServiceRunCenter.LOG.AddLog("Service StartUp() OpenTun: " + ServiceRunCenter.RunSet.Tuns[z].TunName + " Road: " + ServiceRunCenter.RunSet.Tuns[z].Road);
                }
                else
                {
                    tl.SetStop();
                    tl = null;
                }

            }

            await ServiceRunCenter.LOG.SaveLogs(true);

            return a;
        }
    }
}
