

using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.CodeDom;
using System.Text.Json.Serialization;

using BLADE.TOOLS.WEB;
using BLADE.TOOLS.BASE.Json; 
using BLADE.TOOLS.NET.Dynamic;
using BLADE.TOOLS.BASE.ThreadSAFE;


namespace BLADE.SERVICEWEB.RAZORBODY9
{
    /// <summary>
    /// 动态表单数据模型管理器
    /// 动态表单使用目录dml文件 加 表单模型json文件分别存放。需要分步加载。 
    /// </summary>
    public static class DataModelMNG
    {
        /// <summary>
        /// 目录字典档。初始化时 表单模型FM为空，按需加载。
        /// </summary>
        private static Dictionary_TS<ulong, DMItem> DMS = new Dictionary_TS<ulong, DMItem>();
        private static Lock _biz = new Lock();
        private static bool _loading = false;
        private static DateTime lastload = TimeProvider.UtcNow.AddHours(-2);

        /// <summary>
        /// 自动尝试重新加载目录。
        /// </summary>
        /// <param name="mins"></param>
        /// <returns></returns>
        public static async ValueTask<int> AutoTry_ReloadDMS(ushort mins = 60)
        {
            if ((TimeProvider.UtcNow - lastload).TotalMinutes > mins)
            { return await LoadDMS(); }
            return -1;
        }
        public static string lastsave = "";
        /// <summary>
        /// 从文件加载动态表单目录。
        /// </summary>
        /// <returns></returns>
        public static async ValueTask<int> LoadDMS()
        {
            lock (_biz)
            {
                if (_loading) { return 0; }
                _loading = true;
            }
            int a = 0;
            DMItemsBox db = new DMItemsBox();

            if (BaseService.Instance != null)
            {
                string listfile = BaseService.Instance.WwwRootPath + "datamodel/_modellist.dml";
                try
                {
                    if (File.Exists(listfile))
                    {
                        string json = "";
                        using (var ws = File.OpenText(listfile))
                        {
                            json = await ws.ReadToEndAsync();
                        }
                        if (json.Length > 10)
                        {
                            var tdb = JsonOptions.Deserialize<DMItemsBox>(json);
                            if (tdb != null)
                            { db = tdb; }
                        }
                    }
                    else
                    {
                        var mcm = new MiddleCommandMessage(BaseService.Instance.ServiceID, null, "text string", false);
                        var fmt = new DataModelTransTool.FormModelTool(new MiddleCommandMessage(BaseService.Instance.ServiceID, true, "tempModel", new MCM_ACC(1, "guest", "token"),
                            new MCM_Data("daItem", "defaultstring"), "sender", "worker",
                            mcm, "user session ID", "request ID"), "MiddleCommandMessage Model Temp Form");
                        await BaseService.Instance.AddLogAsync(10, fmt.ReMakeFields().log);
                        DMItem mdmd = new DMItem(fmt.FormSN, fmt.FormID, "TempModel_MiddleCommandMessage", "BaseService MiddleCommandService MiddleCommandMessage", BaseService.Instance.ServiceID);
                        mdmd.FM = fmt;

                        MCM_SafeShell msmss = new MCM_SafeShell(mcm, "TempMCM_SafeShell");
                        var fmt2 = new DataModelTransTool.FormModelTool(msmss, "MCM_SafeShell Temp Form");
                        DMItem msms2 = new DMItem(fmt2.FormSN, fmt2.FormID, "TempModel_MCM_SafeShell", "BaseService MiddleCommandService MCM_SafeShell", BaseService.Instance.ServiceID);
                        msms2.FM = fmt2;
                        //   db.Itmes.Add(mdmd);

                        await AddDMI(msms2, true);
                        await AddDMI(mdmd, true);

                        //string json = JsonOptions.Serialize<DMItemsBox>(db);
                        //try
                        //{
                        //    using (var ws = File.CreateText(listfile))
                        //    {
                        //        await ws.WriteAsync(json);
                        //    }
                        //    await BaseService.Instance.AddLogAsync(10, "Load File: [" + listfile + "] OK");
                        //}
                        //catch (Exception wez) { await BaseService.Instance.AddLogAsync(11, "Write file : [" + listfile + "]  Error: " + wez.Message); }
                    }
                }
                catch (Exception ze)
                {
                    await BaseService.Instance.AddLogAsync(12, "Load file : [" + listfile + "]  Error: " + ze.Message);
                }
            }
            if (db.Itmes.Count > 0)
            {
                lastsave = db.SaveTime;
                lock (_biz)
                {
                    DMS.Clear();
                    foreach (var b in db.Itmes)
                    {
                        DMS.Add(b.SN, b);
                        a++;
                    }
                }
            }
            lastload = TimeProvider.UtcNow;
            _loading = false;

            return a;
        }
        /// <summary>
        /// 列出全部动态表单目录。
        /// </summary>
        /// <returns></returns>
        public static async ValueTask<List<DMItem>> GetAllDMItems()
        {
            if (DMS.Count < 1)
            {
                await LoadDMS();
                await Task.Delay(20);
            }
            List<DMItem> r = new List<DMItem>();
            lock (_biz)
            {
                foreach (var b in DMS.Values)
                {
                    r.Add(b);
                }
            }
            return r;
        }
        /// <summary>
        /// 列出动态表单目录信息
        /// </summary>
        /// <returns></returns>
        public static async ValueTask<List<(ulong sn, ulong id, string name, string talk, long service)>> GetDMList()
        {
            if (DMS.Count < 1) { await LoadDMS(); await Task.Delay(20); }
            List<(ulong sn, ulong id, string name, string talk, long service)> r = new List<(ulong sn, ulong id, string name, string talk, long service)>();
            lock (_biz) { foreach (var b in DMS.Values) { r.Add(b.GetInfo()); } }
            return r;
        }
        /// <summary>
        /// 取出指定sn的动态表单信息（只从字典档取出，不尝试加载表单模型）
        /// </summary>
        /// <param name="sn">sn编号</param>
        /// <returns></returns>
        public static async ValueTask<DMItem?> GetDMItem(ulong sn)
        {
            if (DMS.Count < 1)
            {
                await LoadDMS();
                await Task.Delay(40);
            }
            lock (_biz)
            {
                if (DMS.ContainsKey(sn))
                { return DMS[sn]; }
            }
            return null;
        }
        /// <summary>
        /// 取出指定sn的动态表单信息（并且会检查表单模型FM，如果未加载则自动加载，且会自动处理动态程序集的加载）
        /// </summary>
        /// <param name="sn"></param>
        /// <returns></returns>
        public static async ValueTask<DMItem?> GetDMItemAndModel(ulong sn)
        {
            var dm = await GetDMItem(sn);
            if (dm != null)
            {
                if (dm.FM != null) { return dm; }
                else
                {

                    for (int z = 0; z < 20; z++)
                    {
                        if (_loading) { await Task.Delay(20); }
                        else
                        {
                            _loading = true; break;
                        }
                    }
                    _loading = false;
                    string mdfile = BaseService.Instance?.WwwRootPath + "datamodel/" + dm.LinkFileName;
                    if (mdfile == null) { mdfile = "BaseService.Instance is null"; }
                    try
                    {
                        string mdjson = "";
                        using (var m = File.OpenText(mdfile))
                        { mdjson = await m.ReadToEndAsync(); }
                        if (mdjson.Length > 10)
                        {
                            if (dm.IncADC != "")
                            {
                                try
                                {
                                    var ld = DataModelMNG.LoadDyClassSaveTmp(dm.IncADC, true);
                                }
                                catch { }
                            }
                            var fmt = JsonOptions.Deserialize<DataModelTransTool.FormModelTool>(mdjson);
                            if (fmt != null)
                            {
                                dm.FM = fmt;
                                _loading = false;
                                return dm;
                            }
                        }
                    }
                    catch (Exception ze)
                    {
                        BaseService.Instance?.AddLog(12, "Load File: [" + mdfile + "] ERROR: " + ze.Message);
                    }
                    _loading = false;
                }
            }
            return null;
        }
        /// <summary>
        ///  添加一个动态表单信息对象。
        /// </summary>
        /// <param name="dmi">动态表单信息对象</param>
        /// <param name="savetolist">是否要保存到文件（包括目录和表单json）</param>
        /// <returns></returns>
        public static async ValueTask AddDMI(DMItem dmi, bool savetolist = true)
        {
            lock (_biz)
            {
                if (DMS.ContainsKey(dmi.SN))
                { DMS[dmi.SN] = dmi; }
                else
                { DMS[dmi.SN] = dmi; }
            }
            if (savetolist)
            {
                await SaveDMS();
                if (dmi.FM != null)
                { await SaveModelFile(dmi); }
            }
        }
        /// <summary>
        /// 保存动态表单模型FM 到文件。
        /// </summary>
        /// <param name="dmi"></param>
        /// <returns></returns>
        public static async ValueTask SaveModelFile(DMItem dmi)
        {
            string mdfile = BaseService.Instance?.WwwRootPath + "datamodel/" + dmi.LinkFileName;
            if (mdfile == null) { mdfile = "BaseService.Instance is null"; }
            try
            {
                if (dmi.FM != null)
                {
                    string mdjson = JsonOptions.Serialize<DataModelTransTool.FormModelTool>(dmi.FM);
                    using (var m = File.CreateText(mdfile))
                    {
                        await m.WriteAsync(mdjson);
                    }
                    await BaseService.Instance.AddLogAsync(10, "Save File: [" + mdfile + "] OK");
                }
                else
                {
                    await BaseService.Instance.AddLogAsync(13, "Save File: [" + mdfile + "]  Failed: FM is null");
                }
            }
            catch (Exception wez) { await BaseService.Instance.AddLogAsync(11, "Write file : [" + mdfile + "]  Error: " + wez.Message); }
        }
        /// <summary>
        /// 保存目录
        /// </summary>
        /// <returns></returns>
        public static async ValueTask SaveDMS()
        {
            DMItemsBox db = new DMItemsBox();
            lock (_biz)
            {
                foreach (var b in DMS.Values)
                {
                    db.Itmes.Add(b);
                }
            }
            string listfile = BaseService.Instance.WwwRootPath + "datamodel/_modellist.dml";
            try
            {
                string json = JsonOptions.Serialize<DMItemsBox>(db);
                using (var ws = File.CreateText(listfile))
                {
                    await ws.WriteAsync(json);
                }
                await BaseService.Instance.AddLogAsync(10, "Save File: [" + listfile + "] OK");
            }
            catch (Exception wez) { await BaseService.Instance.AddLogAsync(11, "Write file : [" + listfile + "]  Error: " + wez.Message); }
        }
        /// <summary>
        /// 保存 动态类实例包装对象 到adc文件。
        /// </summary>
        /// <param name="adc"></param>
        /// <returns></returns>
        public static async ValueTask SaveDyClassSaveTmp(DyClassSaveTmp adc)
        {
            string mdfile = BaseService.Instance?.WwwRootPath + "datamodel/" + adc.AsmName + ".adc";
            if (mdfile == null) { mdfile = "BaseService.Instance is null"; }
            try
            {
                string mdjson = JsonOptions.Serialize<DyClassSaveTmp>(adc);
                using (var m = File.CreateText(mdfile))
                {
                    await m.WriteAsync(mdjson);
                }
                await BaseService.Instance.AddLogAsync(10, "Save  AsmDyClass File: [" + mdfile + "] OK");
            }
            catch (Exception wez) { await BaseService.Instance.AddLogAsync(11, "Write file : [" + mdfile + "]  Error: " + wez.Message); }
        }
        /// <summary>
        /// 从adc文件加载 动态类实列包装对象
        /// </summary>
        /// <param name="asmName"></param>
        /// <param name="autoload"></param>
        /// <returns></returns>
        public static async ValueTask<DyClassSaveTmp?> LoadDyClassSaveTmp(string asmName, bool autoload = false)
        {
            string mdfile = BaseService.Instance?.WwwRootPath + "datamodel/" + asmName + ".adc";
            if (mdfile == null) { mdfile = "BaseService.Instance is null"; }
            try
            {
                string mdjson = "";
                using (var m = File.OpenText(mdfile))
                { mdjson = await m.ReadToEndAsync(); }
                if (mdjson.Length > 10)
                {
                    var fmt = JsonOptions.Deserialize<DyClassSaveTmp>(mdjson);
                    if (fmt != null)
                    {
                        if (autoload)
                        {
                            try
                            {
                                if (DyClassTool.IsDynamicTypeExist(fmt.AsmName)) { }
                                else { DyClassTool.RegisterDynamicAssembly(fmt.AsmName, null, fmt.DYC, fmt.DYC.exID, true, null); }
                                DyClassTool.GetOrCreateDynamicType(fmt.AsmName);
                            }
                            catch (Exception ex) { await BaseService.Instance.AddLogAsync(14, "Load DyClassAsm ex: " + ex.ToString()); }
                        }
                        return fmt;
                    }
                }
            }
            catch (Exception ze)
            {
                await BaseService.Instance.AddLogAsync(12, "Load AsmDyClass File: [" + mdfile + "] ERROR: " + ze.Message);
            }
            return null;
        }
        public static bool Disposed { get; private set; } = false;
        /// <summary>
        /// 清空并释放内部数据。
        /// </summary>
        /// <param name="savedms"></param>
        /// <returns></returns>
        public static async ValueTask Dispose(bool savedms = true)
        {
            if (Disposed) return;
            if (savedms)
            { await SaveDMS(); }
            DMS.Clear();
            Disposed = true;
        }
    }

    /// <summary>
    /// 保存动态表单项列表的容器。
    /// 其中的表单项不包含动态表单模型内容，模型内容需要按需加载。
    /// </summary>
    public class DMItemsBox
    {
        public string SaveTime { get; set; } = TimeProvider.UtcNowOffSet.ToDateTimeZoneString();
        public List<DMItem> Itmes { get; set; } = new List<DMItem>();
    }

    /// <summary>
    /// 动态表单项。
    /// 用于存储动态表单的信息。
    /// 其内部动态表单模型需要使用LinkFileName文件名进行按需加载，直接的序列化不包含动态表单模型内容。
    /// </summary>
    public class DMItem
    {
        public ulong SN { get; set; } = 0;
        public ulong ID { get; set; } = 0;
        /// <summary>
        /// 需要引用的 动态程序集。 
        /// 如果本动态表单的原型为本应用包含的程序集类型，则本属性留空。
        /// 如果表单模型的原型对象为动态类等情况，则需要先确认程序集已经加载，再尝试加载（反序列化，反射）表单对象。因为表单对象中包含原型类实列。
        /// </summary>
        public string IncADC { get; set; } = "";
        public string Name { get; set; } = string.Empty;

        public string TalkID { get; set; } = string.Empty;
        public long ServiceID { get; set; } = 0;

        /// <summary>
        /// 表单的实际模型对象（包含表单和原型）。
        /// 在 DMItem 的json序列化中不包括这个模型。
        /// 需要在使用时按需加载 LinkFileName 文件。
        /// </summary>
        [JsonIgnore]
        public DataModelTransTool.FormModelTool? FM = null;

        public string LinkFileName { get { return "DM_" + SN.ToString() + "_" + ID.ToString() + ".json"; } } 
        
        public DMItem(ulong inSN, ulong inID=0, string inName="default", string inTalk="0" ,long inserviceid=0,string incadc="")
        {
            SN = inSN;
            ID = inID;
            Name = inName.Trim();
            TalkID = inTalk.Trim();
            ServiceID = inserviceid;
            IncADC = incadc.Trim();
             
        }

        public (ulong sn, ulong id, string name, string talk, long service) GetInfo()
        {
            return (SN, ID, Name, TalkID, ServiceID);
        }
    }
}
