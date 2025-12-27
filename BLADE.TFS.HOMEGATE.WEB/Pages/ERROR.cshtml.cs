using System.Diagnostics;
using BLADE.TOOLS.WEB;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BLADE.SERVICEWEB.RAZORBODY9.Pages
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [IgnoreAntiforgeryToken]
    public class ERRORModel : BLADE.TOOLS.WEB.Razor.ErrorBasePageModel
    {
        

      //  private readonly ILogger<ERRORModel> _logger;

        public ERRORModel( BaseService _bs):base(_bs)
        {
           // _logger = logger;
        }

        public override void OnGet()
        {
            base.OnGet();
        }


        #region base fields
        ///// <summary>
        ///// TempData["EXTITLE"]
        ///// </summary>
        //public string ExTitle { get; set; } = "null";

        ///// <summary>
        ///// TempData["EXMSG"]
        ///// </summary>
        //public string ExMsg { get; set; } = "";

        ///// <summary>
        ///// Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        ///// </summary>
        //public string? RequestId { get; set; }

        ///// <summary>
        ///// HttpContext.Session.GetString("ExceptionMessage");
        ///// </summary>
        //public string ErrorTitle = "";

        ///// <summary>
        ///// HttpContext.Session.GetString("ExceptionStackTrace");
        ///// </summary>
        //public string ErrorText = "";

        //public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        #endregion

    }

}
