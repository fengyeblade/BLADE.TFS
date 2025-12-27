using BLADE.TOOLS.NET.Dynamic;
using BLADE.TOOLS.WEB;
using BLADE.TOOLS.WEB.Razor;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Drawing;
using System.Security.Cryptography;
using static BLADE.TOOLS.WEB.DataModelTransTool;


namespace BLADE.SERVICEWEB.RAZORBODY9.Pages
{
    public class DYFORMModel : BLADE.TOOLS.WEB.Razor.BasePageModel
    {
        public string PageTITLE = "Dynamic Form Example";
        public string LoadTime = TimeProvider.LocalNowOffSet.ToDateTimeZoneString();

        [BindProperty]
        public string curssn { get; set; } = "";


        /// <summary>
        ///  STEP : createbyDyClass 创建  
        ///         open 开启编辑操作    
        ///         view 查看确认提交    
        ///         work 提交处理展示结果   
        ///         back 后退
        /// </summary>
        [BindProperty]
        public string curstep { get; set; } = "";
        public BLADE.TOOLS.WEB.DataModelTransTool.FormWebRawHtml RunForm = new DataModelTransTool.FormWebRawHtml();
        public string backtext = "返回表单列表";
        public string submittext = "提交保存表单";
        public DYFORMModel(BaseService _bs) : base(_bs)
        {  DataModelMNG.AutoTry_ReloadDMS(45); }
        public async Task<IActionResult> OnGetAsync()
        {
            RunForm.FormInfo = "未找到相应的表单";
            RunForm.FormName = "错误提示";

            GetClientIpAddress();
            string dfSN = Request.GetValueFromRequest("dfsn");
            string steP = Request.GetValueFromRequest("step").Trim().ToLower();
            ulong ssn = 0;
            if (steP.StartsWith("back"))
            {
                return RedirectToPage("/DYFORMLIST", new { t = TimeProvider.LocalNow.Millisecond.ToString() });
            }
            else if (steP.StartsWith("createbyDyClass")) 
            {
                var a = GetGSValue("CURDYCLASS_WORKING");
                if (a.Successful) {
                    var k = ((DyClass c, string t))(a.DataOrSender);
                    DyClass dc  = k.c; LoadTime = k.t;

                   var gettypes= DyClassTool.CreateDyClassType(dc);
                    if (gettypes.ctype != null)
                    {
                        var insts = gettypes.instance;
                        if (insts != null)
                        {
                            DyClassSaveTmp adc = new DyClassSaveTmp(dc, insts, gettypes.ctype, gettypes.entype, gettypes.asmName);
                            await DataModelMNG.SaveDyClassSaveTmp(adc);
                            if (DyClassTool.IsDynamicTypeExist(gettypes.asmName)) { }
                            else
                            {
                                DyClassTool.RegisterDynamicAssembly(gettypes.asmName, gettypes.ctype, dc, dc.exID, true, insts);
                            }
                            var fmt = new DataModelTransTool.FormModelTool(insts, "DmForm_" + dc.ClassName, 0, "undefined");
                            DMItem dm = new DMItem(fmt.FormSN, fmt.FormID, fmt.FormTitle, "0", BaseService.Instance.ServiceID,adc.AsmName);
                            dm.FM= fmt;
                            await DataModelMNG.AddDMI(dm, true);
                            ssn = fmt.FormSN;
                            SetGSValue("CURDYFORM_" + ssn.ToString(), dm);
                            dfSN = ssn.ToString();
                            steP = "open";
                            RemoveGSValue("CURDYCLASS_WORKING");
                            return RedirectToPage("/DYFORM", new { dfsn = ssn.ToString(), step = steP });
                        }
                    }
                }
            }
            curssn = ssn.ToString() ;
            curstep = steP;
            if (ssn>0 || ulong.TryParse(dfSN, out ssn))
            {
                DMItem? dmi = null;
                var d = GetGSValue("CURDYFORM_" + ssn.ToString());
                if (d.Successful) { dmi = (DMItem)d.DataOrSender; }
                else { dmi= await DataModelMNG.GetDMItemAndModel(ssn); }
                if (dmi != null && dmi.FM != null)
                {
                   
                    SetGSValue("CURDYFORM_" + ssn.ToString(), dmi);

                    if (steP.StartsWith("view"))
                    {
                        backtext = "返回修改表单";
                        submittext = "确认执行提交";
                        RunForm = dmi.FM.MakeWebFormDashBoard();
                    }
                    else if (steP.StartsWith("open"))
                    {
                        backtext = "返回表单列表";
                        submittext = "保存表单数据";
                        RunForm = dmi.FM.MakeWebFormInterAction();
                    }
                    else if (steP.StartsWith("work"))
                    {
                        backtext = "返回表单列表";
                        submittext = "结束";
                    }
                   
                }
                curssn = ssn.ToString();
                curstep = steP;
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            string act = Request.GetValueFromRequest("action").Trim().ToLower();

            ulong ssn = 0;
            string steP = curstep.Trim().ToLower();
            try { ssn = ulong.Parse(curssn); } catch { }

            if (act.StartsWith("back"))
            {
                if (steP.StartsWith("view"))
                {
                    return RedirectToPage("/DYFORM", new { dfsn = ssn.ToString(), step = "open" });
                }
                else if (steP.StartsWith("open"))
                {
                    return RedirectToPage("/DYFORMLIST", new { t = TimeProvider.LocalNow.Millisecond.ToString() });
                }
            }

            if (act.StartsWith("submit"))
            {
                if (steP.StartsWith("view"))
                {
                    return RedirectToPage("/DYFORM", new { dfsn = ssn.ToString(), step = "work" });
                }
                else if (steP.StartsWith("open"))
                {
                    DMItem? dmi = null;
                    var d = GetGSValue("CURDYFORM_" + ssn.ToString());
                    if (d.Successful) { dmi = (DMItem)d.DataOrSender; }
                    //   else { dmi = await DataModelMNG.GetDMItemAndModel(ssn); }

                    if (dmi == null || dmi.FM == null)
                    {
                        RunForm.FormName = "错误提示";
                        RunForm.FormInfo = "参数错误！未找到相应的表单";
                    }
                    else
                    {
                        getPostValues(dmi);
                        SetGSValue("CURDYFORM_" + ssn.ToString(), dmi);
                        return RedirectToPage("/DYFORM", new { dfsn = ssn.ToString(), step = "view" });
                    }

                }
            }
            return Page();
        }

        private void getPostValues(DMItem cdm)
        {
            var dynamicFields = new Dictionary<string, RequestValueMod >();

            foreach (var key in Request.Form.Keys)
            {
                if (key.StartsWith("AF"+DataModelTransTool.FieldBase.PH))
                {
                    // 获取该键对应的所有值
                    var values = Request.Form[key];
                    dynamicFields.Add(key, new RequestValueMod(key, values.ToArray()));
                }
            }
            bool ensk = false;
            foreach (var i in cdm.FM.Fields)
            {
                foreach (var pv in dynamicFields.Values)
                {
                    ensk = false;
                    try
                    {
                        if (pv.AfName.IsValid && pv.AfName.FieldName == i.FieldName)
                        {
                            ensk = true;
                            switch (pv.AfName.IAM)
                            {
                                case FieldWebItem.BinaryHex:
                                    {
                                        byte[] binv = Array.Empty<byte>();
                                        try { binv = Convert.FromHexString(pv.SingleValue); } catch { }
                                        if (DataModelTransTool.SetFieldValue(cdm.FM.OrgModel, i.FieldName, i.IsProperty, binv))
                                        { i.SetOrgValue(binv); }
                                    }
                                    break;
                                case FieldWebItem.CheckBox:
                                    {
                                        bool chkd = pv.SingleValue.Length > 0;
                                        if (DataModelTransTool.SetFieldValue(cdm.FM.OrgModel, i.FieldName, i.IsProperty, chkd))
                                        { i.SetOrgValue(chkd); }
                                    }
                                    break;
                                case FieldWebItem.TextLine:
                                    {
                                        string a = pv.SingleValue;
                                        if (DataModelTransTool.SetFieldValue(cdm.FM.OrgModel, i.FieldName, i.IsProperty, a))
                                        { i.SetOrgValue(a); }
                                    }
                                    break;

                                case FieldWebItem.Password:
                                case FieldWebItem.TextArea: 
                                case FieldWebItem.Email:
                                case FieldWebItem.Tel:
                                case FieldWebItem.UrlLink:
                                    {
                                        string a = pv.SingleValue;
                                        if (DataModelTransTool.SetFieldValue(cdm.FM.OrgModel, i.FieldName, i.IsProperty, a))
                                        { i.SetOrgValue(a); }
                                    }
                                    break;
                                case FieldWebItem.Selecter:
                                    {
                                        string selectedValue = pv.SingleValue;
                                        Field_Selecter tt = (Field_Selecter)i;
                                        tt.SetOrgValue(selectedValue);
                                        DataModelTransTool.SetFieldValue(cdm.FM.OrgModel, i.FieldName, i.IsProperty, tt.FieldValue);
                                    }
                                    break;
                                case FieldWebItem.DateTimePicker:
                                    {
                                        DateTime dt;
                                        if (DateTime.TryParse(pv.SingleValue, out dt))
                                        {
                                            Field_DateTimePicker tt = (Field_DateTimePicker)i;
                                            tt.SetOrgValue(pv.SingleValue);
                                            DataModelTransTool.SetFieldValue(cdm.FM.OrgModel, i.FieldName, i.IsProperty, tt.FieldValue);
                                        }
                                    }
                                    break;
                                case FieldWebItem.TimePicker:
                                    {
                                        TimeOnly dt;
                                        if (TimeOnly.TryParse(pv.SingleValue, out dt))
                                        {
                                            if (DataModelTransTool.SetFieldValue(cdm.FM.OrgModel, i.FieldName, i.IsProperty, dt))
                                            { i.SetOrgValue(dt); }
                                        }
                                    }
                                    break;
                                case FieldWebItem.DatePicker:
                                    {
                                        DateOnly dt;
                                        if (DateOnly.TryParse(pv.SingleValue, out dt))
                                        {
                                            if (DataModelTransTool.SetFieldValue(cdm.FM.OrgModel, i.FieldName, i.IsProperty, dt))
                                            { i.SetOrgValue(dt); }
                                        }
                                    }
                                    break;
                                case FieldWebItem.IntNum:
                                    {
                                        object? ov = null;
                                        if (pv.AfName.ValueName.EndsWith("sbyte", StringComparison.OrdinalIgnoreCase))
                                        {
                                            if (sbyte.TryParse(pv.SingleValue, out sbyte dv)) { ov = dv; }
                                        }
                                        else if (pv.AfName.ValueName.EndsWith("int", StringComparison.OrdinalIgnoreCase) || pv.AfName.ValueName.EndsWith("int32", StringComparison.OrdinalIgnoreCase))
                                        {
                                            if (int.TryParse(pv.SingleValue, out int dv)) { ov = dv; }
                                        }
                                        else if (pv.AfName.ValueName.EndsWith("short", StringComparison.OrdinalIgnoreCase) || pv.AfName.ValueName.EndsWith("int16", StringComparison.OrdinalIgnoreCase))
                                        {
                                            if (short.TryParse(pv.SingleValue, out short dv)) { ov = dv; }
                                        }
                                        else if (pv.AfName.ValueName.EndsWith("long", StringComparison.OrdinalIgnoreCase) || pv.AfName.ValueName.EndsWith("int64", StringComparison.OrdinalIgnoreCase))
                                        {
                                            if (long.TryParse(pv.SingleValue, out long dv)) { ov = dv; }
                                        }

                                        if (ov != null)
                                        {
                                            if (DataModelTransTool.SetFieldValue(cdm.FM.OrgModel, i.FieldName, i.IsProperty, ov))
                                            { i.SetOrgValue(ov); }
                                        }
                                    }
                                    break;
                                case FieldWebItem.UIntNum:
                                    {
                                        object? ov = null;
                                        if (pv.AfName.ValueName.EndsWith("byte", StringComparison.OrdinalIgnoreCase))
                                        {
                                            if (byte.TryParse(pv.SingleValue, out byte dv)) { ov = dv; }
                                        }
                                        else if (pv.AfName.ValueName.EndsWith("uint", StringComparison.OrdinalIgnoreCase) || pv.AfName.ValueName.EndsWith("uint32", StringComparison.OrdinalIgnoreCase))
                                        {
                                            if (uint.TryParse(pv.SingleValue, out uint dv)) { ov = dv; }
                                        }
                                        else if (pv.AfName.ValueName.EndsWith("ushort", StringComparison.OrdinalIgnoreCase) || pv.AfName.ValueName.EndsWith("uint16", StringComparison.OrdinalIgnoreCase))
                                        {
                                            if (ushort.TryParse(pv.SingleValue, out ushort dv)) { ov = dv; }
                                        }
                                        else if (pv.AfName.ValueName.EndsWith("ulong", StringComparison.OrdinalIgnoreCase) || pv.AfName.ValueName.EndsWith("uint64", StringComparison.OrdinalIgnoreCase))
                                        {
                                            if (ulong.TryParse(pv.SingleValue, out ulong dv)) { ov = dv; }
                                        }
                                        
                                        if (ov != null)
                                        {
                                            if (DataModelTransTool.SetFieldValue(cdm.FM.OrgModel, i.FieldName, i.IsProperty, ov))
                                            { i.SetOrgValue(ov); }
                                        }
                                    }
                                    break;
                                case FieldWebItem.DoubleNum:
                                    {
                                        object? ov=null;
                                        if (pv.AfName.ValueName.EndsWith("double", StringComparison.OrdinalIgnoreCase))
                                        {
                                            if (double.TryParse(pv.SingleValue, out double dv)) { ov = dv; }
                                        }
                                        else if (pv.AfName.ValueName.EndsWith("float", StringComparison.OrdinalIgnoreCase)|| pv.AfName.ValueName.EndsWith("Single", StringComparison.OrdinalIgnoreCase))
                                        { if (float.TryParse(pv.SingleValue, out float dv)) { ov = dv; } 
                                        }
                                        else if (pv.AfName.ValueName.EndsWith("decimal", StringComparison.OrdinalIgnoreCase)  )
                                        {
                                            if (decimal.TryParse(pv.SingleValue, out decimal dv)) { ov = dv; }
                                        }
                                        if (ov!=null)
                                        {
                                            if (DataModelTransTool.SetFieldValue(cdm.FM.OrgModel, i.FieldName, i.IsProperty, ov))
                                            { i.SetOrgValue(ov); }
                                        }
                                    }
                                    break;
                                case FieldWebItem.ColorPicker:
                                    {
                                        DataModelTransTool.Field_ColorPicker tt = (DataModelTransTool.Field_ColorPicker)i;
                                        tt.SetOrgValue(pv.SingleValue);
                                        DataModelTransTool.SetFieldValue(cdm.FM.OrgModel, i.FieldName, i.IsProperty, tt.FieldValue);
                                    }
                                    break;
                                case FieldWebItem.JosnBlock:
                                    {
                                        string a = pv.SingleValue;
                                        i.SetOrgValue(a);
                                        if (i.orgVALUE != null)
                                        {
                                            DataModelTransTool.SetFieldValue(cdm.FM.OrgModel, i.FieldName, i.IsProperty, i.orgVALUE);
                                        }
                                    } 
                                    break;
                                default:
                                    ensk = false;
                                    break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        BaseService.Instance?.AddLog(71, "SetField [" + i.FieldName + "] by PostValue[" + pv.SingleValue + "] Error: " + ex.Message);
                    }

                    if (ensk) { break; }
                }
            }
        }
    }
}
