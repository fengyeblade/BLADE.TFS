using System;
using System.Collections.Generic;
using System.Text;
using BLADE.TOOLS.BASE;
using BLADE.SERVICEWEB.RAZORBODY9;
using BLADE.TOOLS.LOG;
using BLADE.TOOLS.WEB;
using BLADETIME = BLADE.TimeProvider;
using Microsoft.Extensions.Logging;

namespace BLADE.TFS.HOMEGATE.COMM
{
    public class WebContorl :IDisposable
    {
        public void Dispose()
        {
            if (WebM != null)
            {
                WebM.StopSiteWork().Wait();
                WebM.Dispose();
                WebM = null;
                RunCenter.AddLog("WebContorl", "SITEManager WebM Disposed!!", LogCodeEnum.Note);
            }
        }
    
        protected SITEManager? WebM=null;

        public WebContorl()
        {
            if (RunCenter.Settings == null)
            { throw new Exception("WebContorl() Error: RunCenter.Settings is null. Need Load it frist by RunCenter.Init() !"); }
            try
            {
                ILogger<BLADE.TOOLS.WEB.BaseService> syslog = BLADE.TOOLS.WEB.BaseService.CreateILogger<BLADE.TOOLS.WEB.BaseService>(RunCenter.Settings.WebSettings.AppName);

                WebM = new SITEManager(RunCenter.Settings.WebSettings, syslog, RunCenter.CLOG, RunCenter.AppStartPath, RunCenter.AppStartPath + "wwwroot/", "");
            }
            catch { } 
        }
        public async ValueTask<(bool suc, string info)> StartWEB()
        {
            if (WebM == null)
            { 
                return (false, "WebContorl.StartWEB() Error: SITEManager WebM is null !");
            }
           var a =  await WebM.StartSiteWork();
           return (a.Succ, a.Info); 
        }
    }

    public class ServerCore
    {

        // TODO : 初始化  RunCenter 

        // TODO ：启动  HOMEGATECORE

        // TODO : 启动  WebContorl

        // TODO : 配置  WebContorl 和 HOMEGATECORE 之间的通信层。

        // TODO ：添加 WebContorl 的功能页面。
    }
}
