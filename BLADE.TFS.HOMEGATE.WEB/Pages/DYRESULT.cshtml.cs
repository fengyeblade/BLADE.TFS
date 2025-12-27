using BLADE.TOOLS.WEB;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BLADE.SERVICEWEB.RAZORBODY9.Pages
{
    public class DYRESULTModel : BLADE.TOOLS.WEB.Razor.BasePageModel
    {
        public string WorkState = "错误";
        public string WorkResult = "未找到正确的表单";
        public string WorkResultText = "无结果";
        public string WorkResultObject = "无对象";
        public string PageTITLE = "Dynamic Form Work Result";
        public DMItem? dmi = null;
        public string LoadTime = TimeProvider.LocalNowOffSet.ToDateTimeZoneString();
      //  public BLADE.TOOLS.WEB.DataModelTransTool.FormWebRawHtml RunForm = new DataModelTransTool.FormWebRawHtml();
        public DYRESULTModel(BaseService _bs) : base(_bs)
        { }
        public async Task<IActionResult> OnGetAsync()
        {
            string dfSN = Request.GetValueFromRequest("dfsn");
            string steP = Request.GetValueFromRequest("step").Trim().ToLower();
            ulong ssn = 0;
            if (steP.StartsWith("work"))
            {
                if (ssn > 0 || ulong.TryParse(dfSN, out ssn))
                {
                    var d = GetGSValue("CURDYFORM_" + ssn.ToString());
                    if (d.Successful) { dmi = (DMItem)d.DataOrSender; }
                    else { dmi = await DataModelMNG.GetDMItemAndModel(ssn); }
                    if (dmi != null && dmi.FM != null)
                    {
                        if (dmi.IncADC != "")
                        {
                            try
                            {
                                var ld = DataModelMNG.LoadDyClassSaveTmp(dmi.IncADC, true);
                            }
                            catch { }
                        }
                        try
                        {
                            var a = DataModelTransTool.WorkPortInfo.FromJson(dmi.FM.OrgPort);
                            if (a != null)
                            {
                                Type iwt = Type.GetType(a.PortType);
                                if (iwt != null)
                                {
                                    var iwp = (WORKPORT.IworkPort)iwt.GetConstructor(new Type[] { }).Invoke(new object[] { });
                                    var res = await iwp.RequestWorkWork(a, dmi.FM.OrgModel);
                                    WorkState = res.suc.ToString();
                                    WorkResult = res.msg + "  " + TimeProvider.LocalNowOffSet.ToDateTimeZoneString();
                                    if (res.response is string srs)
                                    {
                                        WorkResultText = srs;
                                    }
                                    else if (res.response is byte[] bb)
                                    {
                                        WorkResultText = Convert.ToHexString(bb);
                                    }
                                    else
                                    {
                                        WorkResultText = res.response?.ToString() ?? "Null Text";
                                    }
                                    WorkResultObject = res.response?.GetType().FullName ?? "Null Object";
                                }
                                else
                                {
                                    WorkState = "错误";
                                    WorkResult = "未找到正确的OrgModel Type";
                                    WorkResultText = "无结果";
                                    WorkResultObject = "无对象";
                                }
                            }
                        }
                        catch (Exception xze)
                        {
                            WorkState = "执行错误";
                            WorkResult = "Error: " + xze.Message;
                            WorkResultText = xze.ToString();
                            WorkResultObject = xze.Message;
                        }
                    }
                }
            }
            else {
                WorkState = "参数错误";
                WorkResult = "无进一步处理";
                WorkResultText = "无结果";
                WorkResultObject = "无对象";
            }

            return Page();
        }
    }
}
