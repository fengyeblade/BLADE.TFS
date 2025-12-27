using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BLADE.TOOLS.WEB;
using System.IO;
using GS = BLADE.TOOLS.BASE.Session.GlobalAndSession;

namespace BLADE.SERVICEWEB.RAZORBODY9.Pages
{
   
    public class DYFORMTABModel : BLADE.TOOLS.WEB.Razor.BasePageModel
    {
        public string PageTITLE = "Dynamic Form List";
        public string DFsavetime = "";
        public List<(ulong sn, ulong id, string name, string talk, long service)> DFL { get; set; } = null;
        public DYFORMTABModel(BaseService _bs) : base(_bs)
        { DataModelMNG.AutoTry_ReloadDMS(45); }
        public async Task<IActionResult> OnGetAsync()
        {
            
            var rs = GetGSValue("CURDYFORM_LIST");
            if (rs.Successful)
            {
                DFL = (List<(ulong sn, ulong id, string name, string talk, long service)>)rs.DataOrSender;
            }
            else { 
                DFL= await DataModelMNG.GetDMList();
                SetGSValue("CURDYFORM_LIST", DFL);
            }
            DFsavetime = DataModelMNG.lastsave;
            return Page();
        }
    }
}
