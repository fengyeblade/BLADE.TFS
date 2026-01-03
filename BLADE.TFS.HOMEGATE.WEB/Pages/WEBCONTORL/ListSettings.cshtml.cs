using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http.Extensions;
using System.Diagnostics;
using System.Web;
using System.IO;
using System.Text;
using GS = BLADE.TOOLS.BASE.Session.GlobalAndSession;
using BLADE.TOOLS.WEB;

namespace BLADE.TFS.HOMEGATE.WEB.Pages.WEBCONTORL
{
    public class ListSettingsModel : BLADE.TOOLS.WEB.Razor.BasePageModel
    {
        public class BINDINGDATA
        {
            public string mcmtype = "";
            public string mcminfo = "";
            public string mcmtext = "";

        }
        public BINDINGDATA bind = new BINDINGDATA();
        public ListSettingsModel(BLADE.TOOLS.WEB.BaseService _bs) : base(_bs)
        {
        }
        public async Task<IActionResult> OnGetAsync()
        {
            GetClientIpAddress();
            try
            {
                ReadyUserData();

                var RR = GS.GetGlobalValue(Userdata.ShowName);
                if (RR.Successful)
                {
                    BLADE.TOOLS.WEB.MCM_ACC a = (BLADE.TOOLS.WEB.MCM_ACC)RR.DataOrSender;
                    if (a!=null && a.TOKEN == Userdata.TokenValue)
                    {
                        MiddleCommandMessage mcm = new MiddleCommandMessage(BS.RunSet.ServiceID, false, "", a, null, "", "", null);
                        mcm.MessageType = MCM_Type.GET;
                        mcm.MessageInfo = "ALLSETTINGS";
                        mcm.MessageText = "ALLSETTINGS";
                       var mr = await   BaseService.Instance.MCS.ProcessMiddleCommandMessage(mcm);
                        if (mr != null && mr.Successful && mr.DataOrSender!=null)
                        {
                            MiddleCommandMessage mcr = (MiddleCommandMessage)mr.DataOrSender;
                            bind.mcminfo = mcr.MessageInfo ;
                            bind.mcmtype = mcr.MessageType.ToString();
                            bind.mcmtext = mcr.MessageText ;
                        }
                    }
                    else {
                        await BS.AddLogAsync(141, "Log Out by other user ");
                        return RTPage("/LOGIN");
                    }
                }
            }
            catch (Exception ze)
            {
                await BS.AddLogAsync(144, "Error: " + ze.Message);
                return RTPage("/LOGIN");
            }

                return Page();
        }
    }
}
