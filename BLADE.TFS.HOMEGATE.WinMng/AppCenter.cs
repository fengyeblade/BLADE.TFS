using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.CompilerServices;
namespace BLADE.TFS.HOMEGATE.WinMng
{
    public class AppCenter
    { 
        public static bool DBOPENED = false;
        public static CurWBG CWBG = CurWBG.NONE;
        public static CurIPFM CIPFM = CurIPFM.NONE;
        public static string DBTYPE = "SQLSERVER";
        public static string DBCONNSTR = "Server=192.168.1.11;Database=BLADEUC;User Id=bladeuc;Password=Blade2026;TrustServerCertificate=True;Encrypt=True;Connection Timeout=20;Command Timeout=20;MultipleActiveResultSets=True;";
        public static List<selectedItem> SELECTED=new List<selectedItem>();
        public static void UpdateSELECTED(CurWBG wbg, CurIPFM ipf, List<selectedItem> items)
        {
            CWBG = wbg; CIPFM = ipf; SELECTED = items;
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
            if (AddressFULL.IndexOf("/") < 0)
            { 
            AddressFULL = AddressFULL + "/" + msk.ToString();
            }
        }
    }
    public enum CurWBG
    {
        White,Black,Gray ,DOM,NONE
    }
    public enum CurIPFM
    { 
      IPV4,IPV6 ,NONE
    }

    public class DG_Extend
    {
        // 你要扩充的自定义属性，任意数量
        public CurWBG WBG { get; set; } = CurWBG.NONE;
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
