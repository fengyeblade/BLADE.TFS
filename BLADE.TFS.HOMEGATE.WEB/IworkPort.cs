
using Azure;
using BLADE.TOOLS.WEB;
using BLADE.TOOLS.WEB.Razor;
using BLADE.TOOLS.NET;
using BLADE.TOOLS.BASE.Json;
namespace BLADE.SERVICEWEB.RAZORBODY9.WORKPORT
{
    public interface IworkPort :IDisposable
    {
        public   Task<bool> DoWorkPort(DataModelTransTool.WorkPortInfo WP,object data);
        public Task<(bool suc, string msg, object? response)> RequestWorkWork(DataModelTransTool.WorkPortInfo WP, object data);
    }
    public class DefWP:IworkPort
    {
        public void Dispose()
        { 
        }
        public async Task<bool> DoWorkPort(DataModelTransTool.WorkPortInfo WP, object data)
        {
            return true;
        }
        public async Task<(bool suc, string msg, object? response)> RequestWorkWork(DataModelTransTool.WorkPortInfo WP, object data)
        {
            return (false, "DefWP : not real work .", null);
        }
    }
    public class MCMS : IworkPort
    {
        public void Dispose()
        {
           // throw new NotImplementedException();
        } 
        public async Task<bool> DoWorkPort(DataModelTransTool.WorkPortInfo WP, object data)
        {
           // throw new NotImplementedException();
          if(  BaseService.Instance!=null && data is MiddleCommandMessage mm)
            {
                try
                {
                    var w = await BaseService.Instance.MCS.ProcessMiddleCommandMessage(mm);
                    if (w.Successful)
                    {
                        var nw = new  DataModelTransTool.WorkPortInfo(WP.PortName,WP.PortTarget,"MCMS", WP.PortAct, DataModelTransTool.WorkPortCrypt.Orginal, WP,
                          true,WP.RequestFormat,WP.ResponseFormat,w.DataOrSender  );
                        await BaseService.Instance.AddLogAsync(20, "MCM work OK : " + w.Message + "   " + w.DataOrSender?.ToString() ?? "null");
                    }
                    else { await BaseService.Instance.AddLogAsync(20, "MCM work fail : " + w.Message ); }
                    return true;
                }
                catch (Exception ex)
                {   await BaseService.Instance.AddLogAsync(50, "MCM work EX : " + ex.ToString());     } 
            }
            return  false;
        }

        public async Task<(bool suc, string msg, object? response)> RequestWorkWork(DataModelTransTool.WorkPortInfo WP, object data)
        {
            bool s = false; string m = ""; object? n = null;
            if (BaseService.Instance != null && data is MiddleCommandMessage mm)
            {
                var w = await BaseService.Instance.MCS.ProcessMiddleCommandMessage(mm);
                if (w.Successful)
                {
                    var nw = new DataModelTransTool.WorkPortInfo(WP.PortName, WP.PortTarget, "MCMS", WP.PortAct, DataModelTransTool.WorkPortCrypt.Orginal, WP,
                       true, WP.RequestFormat, WP.ResponseFormat, w.DataOrSender);
                    s = true; m= w.Message; n= w.DataOrSender;
                    await BaseService.Instance.AddLogAsync(20, "MCM work OK : " + w.Message );
                    return (s,m,n);
                }
            }
            return (s, m, n);
        } 
    }
    public class HTTPJSONPOST : IworkPort
    {
        public void Dispose()
        {
            
        }

        public async Task<bool> DoWorkPort(DataModelTransTool.WorkPortInfo WP, object data)
        {
            bool s = false;  
            using (GHttpClient h = new GHttpClient())
            {
                string ccc = "";
                if (data is string sss) { ccc = sss.Trim(); }
                else { ccc = JsonOptions.Serialize(data, data.GetType()); }
                var r = await h.POSTStringAsync(WP.PortTarget, ccc, WP.RequestFormat);
                WP.ResponseFormat = r.ContextType;
                WP.WorkData= r.IsStringContext ? r.Content : r.ByteArray;
                s = true;
            }
            return s;
        }

        public async Task<(bool suc, string msg, object? response)> RequestWorkWork(DataModelTransTool.WorkPortInfo WP, object data)
        {
            bool s = false; string m = ""; object? n = null;
            using (GHttpClient h = new GHttpClient())
            {
                string ccc = "";
                if (data is string sss) { ccc = sss.Trim(); }
                else { ccc = JsonOptions.Serialize(data, data.GetType()); }
                var r =  await h.POSTStringAsync(WP.PortTarget, ccc, WP.RequestFormat);
                WP.ResponseFormat = r.ContextType;
                WP.WorkData = r.IsStringContext ? r.Content : r.ByteArray;
                s = true; m = r.ContextType+"   ||  "+r.Headers; n = r.IsStringContext? r.Content : r.ByteArray;
            }
            return (s, m, n);
        }
    }

    public class HTTPREQUEST : IworkPort
    {
        public void Dispose()
        {
           // throw new NotImplementedException();
        }

        public async Task<bool> DoWorkPort(DataModelTransTool.WorkPortInfo WP, object data)
        {
            bool s = false;
            using (GHttpClient h = new GHttpClient())
            {
                string ccc = "";
                if (data is string sss) { ccc = sss.Trim(); }
                else { ccc = JsonOptions.Serialize(data, data.GetType()); }
                var r = await h.GETStringAsync(WP.PortTarget);
                WP.ResponseFormat = r.ContextType;
                WP.WorkData = r.IsStringContext ? r.Content : r.ByteArray;
                s = true;
            }
            return s;
        }

        public async Task<(bool suc, string msg, object? response)> RequestWorkWork(DataModelTransTool.WorkPortInfo WP, object data)
        {
            bool s = false; string m = ""; object? n = null;
            using (GHttpClient h = new GHttpClient())
            {
                string ccc = "";
                if (data is string sss) { ccc = sss.Trim(); }
                else { ccc = JsonOptions.Serialize(data, data.GetType()); }
                var r = await h.GETStringAsync(WP.PortTarget);
                WP.ResponseFormat = r.ContextType;
                WP.WorkData = r.IsStringContext ? r.Content : r.ByteArray;
                s = true; m = r.ContextType + "   ||  " + r.Headers; n = r.IsStringContext ? r.Content : r.ByteArray;
            }
            return (s, m, n);
        }
    }
}
