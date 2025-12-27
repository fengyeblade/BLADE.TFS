using BLADE.TOOLS.NET.Dynamic;
using BLADE.TOOLS.WEB;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using static BLADE.TOOLS.WEB.DataModelTransTool;

namespace BLADE.SERVICEWEB.RAZORBODY9.Pages
{
    public class DYCREATEModel : BLADE.TOOLS.WEB.Razor.BasePageModel
    {
        public string PageTITLE = "CREADTEE NEW FORM AND ORGMODEL";
        public string LoadTime = TimeProvider.LocalNowOffSet.ToDateTimeZoneString();

        [BindProperty]
        public DyClass WorkingDYCLASS { get; set; } = new DyClass("newclass_" + TimeProvider.Global.GetFullSeq().ToString(),"","",true);
        public  string CmbString(string[] a)
        {
            return DyClassTool.CmbStringAry(a);
        }

        public  string[] SplitUsingStr(string ss)
        { return DyClassTool.SplitUsingSring(ss); }
        public  string[] SplitEnumStr(string ss)
        { return DyClassTool.SplitEnumString(ss); }
        [BindProperty]
        public string ActMessage { get; set; } = "";
        [BindProperty]
        public string cmbUsingString { get; set; } = "";
        [BindProperty]
        public string NewFieldName { get; set; } = "";
        [BindProperty]
        public string NewValueString { get; set; } = "";
        [BindProperty]
        public string NewAttriString { get; set; } = "";
        [BindProperty]
        public bool NewIsProperty { get; set; } = true;
        [BindProperty]
        public bool NewNullable { get; set; } = true;

        [BindProperty]
        public string NewValueType { get; set; } = "String";

        //NewEnumOptions
        [BindProperty]
        public string NewEnumOptions { get; set; } = "";
        [BindProperty]
        public string NewApdType { get; set; } = "";
        [BindProperty]
        public string NewEnumName { get; set; } = "";

        [BindProperty(Name = "action")] // 绑定到按钮的name
        public string ButtonAction { get; set; } = "";
        // curBaseClassName
        [BindProperty]
        public string curClassName { get; set; } = "";
        [BindProperty]
        public string curBaseClassName { get; set; } = "";
        [BindProperty]
        public string curNameSpace { get; set; } = "";


        public DYCREATEModel(BaseService _bs) : base(_bs)
        {  DataModelMNG.AutoTry_ReloadDMS(45); }    
        public async Task<IActionResult> OnGetAsync()
        {
            loadcurdyc();
            flshow();
            return Page();
        }
        public async Task<IActionResult> OnPostAsync()
        {
            loadcurdyc();
            if (curClassName.Trim().Length > 0) { WorkingDYCLASS.insetClassName(curClassName); }
            if (curBaseClassName.Trim().Length > 0) { WorkingDYCLASS.insetClassName(curBaseClassName); }
            if (curNameSpace.Trim().Length > 0) { WorkingDYCLASS.insetClassName(curNameSpace); }
            if (cmbUsingString.Trim().Length > 0) { string[] ua = SplitUsingStr(cmbUsingString);
                if (ua.Length > 0) { WorkingDYCLASS.UsingAreas = ua; }    }


            var aa = workButAct();
            if (aa != null)
            { return aa; }
            flshow();
            return Page();
        }
        public void flshow()
        {
            cmbUsingString = CmbString(WorkingDYCLASS.UsingAreas);
            curBaseClassName = WorkingDYCLASS.BaseClassName;
            curClassName = WorkingDYCLASS.ClassName;
            curNameSpace = WorkingDYCLASS.NameSpace;
        }
        public void loadcurdyc()
        {
            var a = GetGSValue("CURDYCLASS_WORKING");
            if (a.Successful)
            {
                var k = ((DyClass c, string t))(a.DataOrSender);
                WorkingDYCLASS = k.c; LoadTime = k.t;
                ActMessage = "Load DyClass from Session.";
            }
            else
            {
                SetGSValue("CURDYCLASS_WORKING", (WorkingDYCLASS, LoadTime));
                ActMessage = "Create New DyClass and set to Session. "+WorkingDYCLASS.ClassName;
            }
           
        }

        protected IActionResult? workButAct()
        {
            string bv = ButtonAction.Trim();
            if (bv.StartsWith("SubmitAll"))
            { }
            else if (bv.StartsWith("SubmitEnum"))
            {
               string enn= NewEnumName.Trim();
                if (DyClassTool.IsValidIdentifier(enn))
                {
                    bool ren = false;

                    foreach (var e in WorkingDYCLASS.Enums) { if (e.Name == enn) { ren = true; break; } }
                    if (ren) { ActMessage = "EnumName:" + enn + " already exists. Duplication is not allowed"; }
                    else
                    {
                        string[] eo = SplitEnumStr(NewEnumOptions);
                        if (eo.Length > 0)
                        { 
                            DyEnum de = new DyEnum(enn, eo);
                            WorkingDYCLASS.Enums.Add(de);
                            ActMessage = "Add New Enum:[ "+enn+" ] done !";
                        }
                        else { ActMessage = "EnumOps:" + eo + " has no ValidIdentifier"; }
                    }
                }
                else { ActMessage = "EnumNAME:"+enn+ " is not a ValidIdentifier"; }
            }
            else if (bv.StartsWith("SubmitField"))
            {
                bool gowk = true;
                string fnn = NewFieldName.Trim();
                enableTypes et = enableTypes.String;
                if (Enum.TryParse<enableTypes>(NewValueType, true, out var tet))
                {
                    et = tet;
                }
                if (et == enableTypes.Enum)
                {
                    bool enok = true;
                   // string[] enuval = NewAttriString.Trim().Split(new string[] { ".", " ", ";" }, StringSplitOptions.RemoveEmptyEntries);
                    string enuty = NewApdType.Trim(); string enuva = NewValueString.Trim();
                    if (enuty.Length > 0 && enuva.Length>0) {  } else { enok = false; }
                    bool fd = false;
                    string[] ops = Array.Empty<string>(); 
                    if (enok)
                    {
                        foreach (var i in WorkingDYCLASS.Enums)
                        {
                            if (i.Name == enuty) { fd = true; NewApdType = i.Name; ops = i.Items.ToArray();  break; }
                        }
                        if (fd && ops.Length>0)
                        {
                            fd = false;
                            foreach (var s in ops)
                            {
                                if (s.ToLower() == enuva.Trim())
                                {
                                    fd = true; break;
                                }
                            }
                            if (fd) { } else { enuva = ops[0]; NewValueString = enuva;  fd = true; }
                        } 
                    }
                    if (enok && fd)
                    {
                        //NewValueString = enuty + "." + enuva;
                    }
                    else {
                        gowk = false;
                        ActMessage = "ThisFiled EnumValue:" + NewApdType+"."+ NewValueString + " is not in Enums";
                    }

                }
                if (gowk)
                {
                    if (DyClassTool.IsValidIdentifier(fnn))
                    {
                        bool ren = false;
                        foreach (var e in WorkingDYCLASS.FieldList) { if (e.Name == fnn) { ren = true; break; } }
                        if (ren) { ActMessage = "FieldName:" + fnn + " already exists. Duplication is not allowed"; }
                        else
                        {
                            DyItem df = new DyItem(fnn, et, NewValueString.Trim(), NewApdType.Trim(), NewIsProperty, NewNullable, NewAttriString.Trim());
                            WorkingDYCLASS.FieldList.Add(df);
                            ActMessage = "Add New Field:[ " + fnn + " ] done !";
                        }
                    }
                    else
                    {
                        ActMessage = "FieldNAME:" + fnn + " is not a ValidIdentifier";
                    }
                }
            }
            else if (bv.StartsWith("deleteField_"))
            {
                string fieldname = bv.Substring(12).Trim();
                if (fieldname.Length < 1)
                {
                    ActMessage = "select Field is null, not deleted.";
                }
                else {
                    for (int z = 0; z < WorkingDYCLASS.FieldList.Count; z++)
                    {
                        if (fieldname == WorkingDYCLASS.FieldList[z].Name)
                        {
                            WorkingDYCLASS.FieldList.RemoveAt(z);
                            ActMessage = "Field: [ "+fieldname+" ] is deleted done.";
                            break;
                        }
                    }
                }
            }
            else if (bv.StartsWith("deleteEnum_"))
            {
                string enumname = bv.Substring(11).Trim();
                if (enumname.Length < 1)
                {
                    ActMessage = "select Enum is null, not deleted.";
                }
                else {
                    for (int z = 0; z < WorkingDYCLASS.Enums.Count; z++)
                    {
                        if (enumname == WorkingDYCLASS.Enums[z].Name)
                        {
                            WorkingDYCLASS.Enums.RemoveAt(z);
                            ActMessage = "Enum: [ " + enumname + " ] is deleted done."; 
                            break;
                        }
                    }
                }
            }
            else
            {
                // SubmitShow
                ActMessage = "Flush show the [ "+WorkingDYCLASS.ClassName+ " ] info. ";
            }

            return null;
        }
    }
}
