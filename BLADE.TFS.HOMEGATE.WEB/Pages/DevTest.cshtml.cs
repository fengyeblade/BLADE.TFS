using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Diagnostics;
using System.Web;

namespace BLADE.SERVICEWEB.RAZORBODY9.Pages
{
    public class DevTestModel : BLADE.TOOLS.WEB.Razor.BasePageModel
    {
        public class BINDINGDATA
        {
            public string SessionUserDataHtml = "";
            public string LoginInputHtml = "";
            public string RequestIDHtml = "";
         
            public string CookiesHtml = "";
            public string HeadersHtml = "";
            public string QueryStringHtml = "";
        }
        public BINDINGDATA bind = new BINDINGDATA();
        public DevTestModel(BLADE.TOOLS.WEB.BaseService _bs) : base(_bs)
        {
        }
        public async Task<IActionResult> OnGetAsync()
        {
            GetClientIpAddress();
            ReadyUserData();
            string reqid = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            if (string.IsNullOrEmpty(reqid)) { reqid = "[None]"; }
            bind.LoginInputHtml = "<b>Account:</b> " + (TempData["inputaccount"] ?? "[NONE]") + "<br /><b>Password:</b> " + (TempData["inputpassword"] ?? "[NONE]")+" <br /><b>SID: "+(TempData["SessionId"]??"--")+"</b>";
            bind.RequestIDHtml = "<b>Request ID:</b> " + reqid+ "<br /><b>URL:</b>"+Request.GetDisplayUrl()+"<br /><b>Referer:</b> " + RTP;
          
            string coks = "";
            foreach (var cok in HttpContext.Request.Cookies)
            { 
               coks += "<b>" + cok.Key + ":</b> " + cok.Value + "<br />";
            }
            bind.CookiesHtml = coks;
            string heads = "";
            foreach (var head in HttpContext.Request.Headers)
            {
                heads += "<b>" + head.Key + ":</b> " + head.Value + "<br />";
            }
            bind.HeadersHtml = heads;

            string qss = "";

            foreach (var qs in HttpContext.Request.Query)
            {
                qss += "<b>" + qs.Key + ":</b> " + qs.Value + "<br />";
            }
            bind.QueryStringHtml = qss;

            string userda = "";
            if (Userdata != null)
            {
                userda = "<b>" + ShowName + "</b>"+Userdata.UserName+"<br />";
            }
            else
            {
                userda = "<b> [NONE] </b><br />";
            }
            userda = userda + "<b> Session ID:</b> " + HttpContext.Session.Id + "<br />";
            userda = userda +" <br /> "+ Text2Html(HttpContext.Session.GetString("USERDATA") ?? "");

           bind. SessionUserDataHtml = userda;

            return Page();
        }
    }
}
