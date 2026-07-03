using BLADE.TOOLS.NET;
using BLADE.TOOLS.NET.IPGATE_SqlBase;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
namespace BLADE.TFS.HOMEGATE.WinMng
{
    public class AppCenter
    { 
        
        public static bool DBOPENED = false;
        public static NameListType CWBG = NameListType.ALL;
        public static CurIPFM CIPFM = CurIPFM.NONE;
        public static string DBTYPE = "SQLSERVER";
        public static string DBCONNSTR = "Server=192.168.1.11;Database=BLADEUC;User Id=bladeuc;Password=Blade2026;TrustServerCertificate=True;Encrypt=True;Connection Timeout=20;MultipleActiveResultSets=True;";
        public static List<selectedItem> SELECTED=new List<selectedItem>();
        public static List<WBGshow>? CurWorkList = new List<WBGshow>();
        public static void UpdateWorkList(NameListType wbg, CurIPFM ipf, List<WBGshow>? work)
        {
            CWBG = wbg; CIPFM = ipf;
            CurWorkList = work;
            
            WorkClass.BindDataGrid(CurWorkList);
            UpdateSELECTED(new List<selectedItem>());

        }
        public static void UpdateSELECTED(List<selectedItem> items)
        {
            SELECTED = items;
            WorkClass.reshowSelected();
        }
        public static byte[] mkary = new byte[] {80,64,54,48,32,24,20,16,8 };
        public static List<(string pfbody,byte mk)> ComSelectedPreFix( )
        { 
            List<(string pfbody, byte mk)> result = new List<(string pfbody, byte mk)>();
            List < (byte[] oip, byte omk)> tl = new List<(byte[] oip, byte omk)>();
            foreach (var item in SELECTED)
            {
                try
                {
                    var t = IPTools.Expand(item.AddressFULL);
                    tl.Add((t.bin, t.pflen));
                }
                catch { }
            }
            if(tl.Count > 0) { result= IpMaskCalculator.ComputePreFix(tl, mkary); }
            return result;
        }

        public static   Interface_IPGATE_DB? IDB = null;

        public static NameListType WBGNAME(byte wbg)
        {
            try { return (NameListType)wbg; } catch { return NameListType.ALL; }
        }
        public static async ValueTask<bool> InsertIPV4(WBG_V4 w)
        {
            if (IDB != null)
            { 
               var k = await IDB.Save_WBG_V4(w);
                return (k > 0);
            }
            return false;
        }
        public static async ValueTask<bool> InsertIPV6(WBG_V6 w)
        {
            if (IDB != null)
            { 
                var k = await IDB.Save_WBG_V6(w);
                return (k > 0);
            }
            return false;
        }
        public static async ValueTask<bool> InsertDOM(WBG_DOM w)
        {
            if (IDB != null)
            { 
                var k = await IDB.Save_WBG_DOM(w);
                return (k > 0);
            }
            return false;
        }
    }

    public class selectedItem
    {
        public long ID { get; set; } = 0;
        public string AddressFULL { get; set; } = string.Empty;
        public selectedItem() { }
        public selectedItem(long id, string address,byte msk)
        {
            ID = id;
            AddressFULL = address;
            if (msk > 0 && AddressFULL.IndexOf("/") < 0)
            {
                AddressFULL = AddressFULL + "/" + msk.ToString();
            }
        }

        public override string ToString()
        {
            return ID+" | " + AddressFULL;
        }
    }
  
    public enum CurIPFM
    { 
      IPV4,IPV6 ,DOM,NONE
    }

    public class DG_Extend
    {
        // 你要扩充的自定义属性，任意数量
        public NameListType WBG { get; set; } = NameListType.ALL;
        public CurIPFM IPFM { get; set; } = CurIPFM.NONE;
        public bool Loaded { get; set; } = false;
    }
    // 全局单例附加数据容器（整个AppDomain共用）
    public static class ObjectExtraStore
    {
        // 弱引用表：Key=原始任意对象实例，Value=附加自定义属性
        private static readonly ConditionalWeakTable<DataGridView, DG_Extend> _DG_extraTable = new();

        // 获取或创建该对象的附加属性实例
        public static DG_Extend Get_DG_Extend(this DataGridView target)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            return _DG_extraTable.GetOrCreateValue(target);
        }

        // 尝试获取，不存在返回null
        public static bool TryGet_DG_Extend(this DataGridView target, out DG_Extend? extra)
        {
            if (target == null)
            {
                extra = null;
                return false;
            }
            return _DG_extraTable.TryGetValue(target, out extra);
        }

        // 移除某个对象的附加数据
        public static void Remove_DG_Extend(this DataGridView target)
        {
            if (target != null)
                _DG_extraTable.Remove(target);
        }
    }
}
