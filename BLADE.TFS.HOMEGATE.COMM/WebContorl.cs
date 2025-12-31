using System;
using System.Collections.Generic;
using System.Text;
using BLADE.TOOLS.BASE;
using BLADE.SERVICEWEB.RAZORBODY9;
using BLADE.TOOLS.LOG;
using BLADE.TOOLS.WEB;
using BLADETIME = BLADE.TimeProvider;
using Microsoft.Extensions.Logging;
using BLADE.MSGCORE.Models;

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

        public void SetMCM(MCD_RequestAccTokenHandler rath, MCD_ProcessMiddleCommandMessageHandler pmcmh)
        {
            if (WebM != null && WebM.BS != null)
            { 
                WebM.BS.MCS.RequestAccTokenHandler += rath;
                WebM.BS.MCS.ProcessMiddleCommandMessageHandler += pmcmh;
            }
        }
        protected SITEManager? WebM=null;

        public WebContorl()
        {
            if (RunCenter.Settings == null)
            { throw new Exception("WebContorl() Error: RunCenter.Settings is null. Need Load it frist by RunCenter.InitAndStart() !"); }
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
        private string AppStartPath = "";
        protected WebContorl? Web =null ;
        protected HomeGateCore? Gate=null;
        public ServerCore(string appstart)
        {
            appstart = appstart.Trim();
            if (appstart.EndsWith("\\") || appstart.EndsWith("/"))
            { }
            else { appstart = appstart + "/"; }

            AppStartPath = appstart;
        }
        public async ValueTask<(bool suc,string info)> InitAndStart()
        {
            string initInfo = "";
            // TODO : 初始化  RunCenter gate  web
            try
            {
                var ir = await RunCenter.Init(AppStartPath);
                if (ir != null)
                {
                    ThreadPool.SetMinThreads(RunCenter.Settings.ThreadsMin, RunCenter.Settings.ThreadsMin);
                    ThreadPool.SetMaxThreads(RunCenter.Settings.ThreadsMax, RunCenter.Settings.ThreadsMax);
                    initInfo = initInfo + "\r\n RunCenter:[" + ir.Successful + "] " + ir.Message;
                    if (ir.Successful)
                    {
                        Gate = new HomeGateCore();
                        var sg = await Gate.StartWork();

                        initInfo = initInfo + "\r\n HomeGateCore StartWork:[" + sg.suc + "] " + sg.info;
                        if (sg.suc && RunCenter.Settings.EnableWeb)
                        {
                            Web = new WebContorl();
                            var sw = await Web.StartWEB();
                            initInfo = initInfo + "\r\n WebContorl StartWEB:[" + sw.suc + "] " + sw.info;
                            await Task.Delay(100);
                            if (sw.suc)
                            {
                                Web.SetMCM(MCD_RequestAccTokenHandler, MCD_ProcessMiddleCommandMessageHandler);
                            }
                            else {
                                if (Web != null) { Web.Dispose(); Web = null; }
                            }
                                return (true, initInfo);
                        }
                        else { if (Gate != null) { Gate.Dispose(); Gate = null; } }
                    }
                }
            }
            catch (Exception ex)
            {
                initInfo = initInfo + "\r\n ServerCore InitAndStart Exception:" + ex.Message;
                if (Gate != null) { Gate.Dispose(); Gate = null; }
                if (Web != null) { Web.Dispose(); Web = null; }
                if (RunCenter.Settings != null && RunCenter.CLOG != null)
                { await RunCenter.AddLogAsync("ServerCore", "InitAndStart Ex: " + ex.ToString()+" \r\n-" + initInfo, LogCodeEnum.Alert); }
            }
            initInfo = initInfo + "\r\n RunCenter.InitAndStart Failed";
            return (false, initInfo);
        }

        // TODO ：启动  HOMEGATECORE

        // TODO : 启动  WebContorl

        // TODO : 配置  WebContorl 和 HOMEGATECORE 之间的通信层。

        // TODO ：添加 WebContorl 的功能页面。
        protected MCM_ACC? CurOper;
        protected MCM_ACC? CurAdmin;
        protected int CurLoginSeed = 10;
        public async Task<Result<MCM_ACC>> MCD_RequestAccTokenHandler(long accid = 0, string name = "", string pass = "")
        {
            CurLoginSeed++;
            if (CurLoginSeed > 100000) { CurLoginSeed = 13; }
            name = name.Trim().ToLower();
            pass = pass.Trim();
            if (accid > 0)
            { if (name == RunCenter.Settings.OperUser.UserName.ToLower().Trim()) 
                { 
                    if (RunCenter.Settings.OperUser.CheckCryptPass(pass))
                    {
                        MCM_ACC ma = new MCM_ACC(100, name, CurLoginSeed.ToString()+ TimeProvider.UtcNow.Millisecond.ToString()+ RunCenter.Settings.OperUser.CryptPass);
                        CurOper = ma;
                        return new Result<MCM_ACC>(true, "", ma);
                    } 
                } 
            }
            else {
                if (name == RunCenter.Settings.AdminUser.UserName.ToLower().Trim())
                {
                    if (RunCenter.Settings.AdminUser.CheckCryptPass(pass))
                    {
                        MCM_ACC ma = new MCM_ACC(0, name, CurLoginSeed.ToString()+ TimeProvider.UtcNow.Millisecond.ToString() + RunCenter.Settings.AdminUser.CryptPass);
                        CurAdmin = ma;
                        return new Result<MCM_ACC>(true, "", ma);
                    }
                }
            }
            MCM_ACC mm = new MCM_ACC(-1, "GUEST", "");
            return new Result<MCM_ACC>(false, "Login Check Fail!",mm);
        }
        public async Task<Result<MiddleCommandMessage>> MCD_ProcessMiddleCommandMessageHandler(MiddleCommandMessage mcm)
        {
            bool ok = false;
            if (mcm.Acc != null)
            {
                if (mcm.Acc.ID == 0)
                { if (CurAdmin!=null && mcm.Acc.TOKEN == CurAdmin.TOKEN)
                    { ok = true; }
                }
                else if (mcm.Acc.ID == 100)
                {
                    if (CurOper != null && mcm.Acc.TOKEN == CurOper.TOKEN)
                    { ok = true; }
                }
                if (!ok) { return new Result<MiddleCommandMessage>(false, "Login State is timeout", null); }
                return await WorkMCM(mcm);
            }
            return new Result<MiddleCommandMessage>(false,"Not Find Premison Acc",null);
        }
        public async Task<Result<MiddleCommandMessage>> WorkMCM(MiddleCommandMessage mcm)
        {
            MiddleCommandMessage rm = new MiddleCommandMessage();
            rm.IsResponse = true;
            // TODO  :  work mcm
            switch (mcm.MessageType)
            {
                case MCM_Type.GET:
                    string jj = mcm.MessageText.ToUpper();
                    if (jj == "ALLSETTINGS" || mcm.MessageText == "GATESETTINGS")
                    {
                        rm.MessageType = MCM_Type.Json;
                        rm.MessageInfo = "BLADE.TFS.HOMEGATE.COMM.GateSettings";
                        rm.MessageText = BLADE.TOOLS.BASE.Json.JsonOptions.Serialize<GateSettings>(RunCenter.Settings);
                    }
                    else if (jj == "TUNTRANSSETTINGS")
                    {
                        rm.MessageType = MCM_Type.Json;
                        rm.MessageInfo = "BLADE.TFS.HOMEGATE.COMM.TunTransSettings";
                        rm.MessageText = BLADE.TOOLS.BASE.Json.JsonOptions.Serialize<TunTransSettings>(RunCenter.Settings.TunSettings);
                    }
                    else if (jj == "WORKSETTING")
                    {
                        rm.MessageType = MCM_Type.Json;
                        rm.MessageInfo = "BLADE.TOOLS.WEB.WorkSetting";
                        rm.MessageText = BLADE.TOOLS.BASE.Json.JsonOptions.Serialize<WorkSetting>(RunCenter.Settings.WebSettings);
                    }
                    else if (jj == "WL_LOCAL_SETTINGS")
                    {
                        rm.MessageType = MCM_Type.Json;
                        rm.MessageInfo = "BLADE.TFS.HOMEGATE.COMM.WL_Local_Settings";
                        rm.MessageText = BLADE.TOOLS.BASE.Json.JsonOptions.Serialize<WL_Local_Settings>(RunCenter.Settings.WhiteListSettings);
                    }
                    else if (jj == "PROSETTINGS")
                    {
                        rm.MessageType = MCM_Type.Json;
                        rm.MessageInfo = "BLADE.MSGCORE.ClientTools.ProSettings";
                        rm.MessageText = BLADE.TOOLS.BASE.Json.JsonOptions.Serialize<MSGCORE.ClientTools.ProSettings>(RunCenter.Settings.ClientSettings);
                    }
                    else if (jj == "GATETRANS")
                    {
                        rm.MessageType = MCM_Type.Text;
                        rm.MessageInfo = "string";
                        rm.MessageText = "GATETRANS  " + Gate?.ListRuntimeTrans();
                    }
                    else {
                        rm.MessageType = MCM_Type.Text;
                        rm.MessageInfo = "string";
                        rm.MessageText = "Not supt this GET: "+jj ;
                    }
                    break;

                case MCM_Type.Update:
                    string j2 = mcm.MessageText.ToUpper();
                    if (j2 == "DEBUG")
                    {
                        RunCenter.Settings?.WebSettings.EnableDeBug = !RunCenter.Settings.WebSettings.EnableDeBug;
                        RunCenter.CLOG?.Debug = RunCenter.Settings.WebSettings.EnableDeBug;
                        rm.MessageType = MCM_Type.Text;
                        rm.MessageInfo = "RunCenter.Settings.WebSettings.EnableDeBug";
                        rm.MessageText = "EnableDeBug  SET TO :"+ RunCenter.Settings?.WebSettings.EnableDeBug;
                        await RunCenter.AddLogAsync("MCM.Update", rm.MessageText, LogCodeEnum.Note);
                    }
                    else if (j2 == "WLDREG")
                    {
                        var t = await RunCenter.WLR.AddWL_Runtime(mcm.MessageInfo, mcm.MessageText);
                        rm.MessageType = MCM_Type.Text;
                        rm.MessageInfo = "WLDREG";
                        rm.MessageText = "AddWL_Runtime " + mcm.MessageInfo+"/"+mcm.MessageText+" "+t;
                    }
                    else if (j2 == "WLREG")
                    {
                        WL_Local wl = new WL_Local();
                        wl.WL_Name = mcm.MessageInfo;
                        wl.AddressOrIPCD = new string[] { mcm.MessageText };
                        var t =  RunCenter.WLR.AddWL_Locals( new WL_Local[] { wl });
                        rm.MessageType = MCM_Type.Text;
                        rm.MessageInfo = "WLREG";
                        rm.MessageText = "AddWL_Locals " + mcm.MessageInfo + "/" + mcm.MessageText + " " + t;
                    }
                    else
                    {
                        rm.MessageType = MCM_Type.Text;
                        rm.MessageInfo = "string";
                        rm.MessageText = "Not supt this UPDATE: " + j2;
                    }
                    break;
                default:
                    return new Result<MiddleCommandMessage>(true, "Not Implemented " + mcm.MessageType + "/" + mcm.MessageInfo + "/" + mcm.MessageText, rm);
            }
            return new Result<MiddleCommandMessage>(true, "Response", rm);
        }
    }
}
