using BLADE.TOOLS.NET;
using BLADE.TOOLS.NET.IPGATE_SqlBase;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace BLADE.TFS.HOMEGATE.WinMng
{
    public class WorkClass
    {
        public static AMAIN? MainForm = null;
        public const string utcfm = "yyyy-MM-dd HH:mm:ss";
        public static async ValueTask<(bool suc, string msg)> OpenIDB(string sqltype, string connstr, bool forcetrus)
        {  
            bool suc = false;
            string msg = string.Empty;
            sqltype = sqltype.Trim().ToUpper();
            if(sqltype != "SQLSERVER" && sqltype != "MYSQL" && sqltype != "POSTGRESQL")
            {   msg = "Invalid DB type.";      return (suc, msg);     }
            connstr = connstr.Trim();
            if (connstr.Length < 3)
            { msg = "Invalid connection string.";  return (suc, msg); }
            BLADE.TOOLS.NET.IPGATE_SqlBase.IPGATE_MODEL_CONFIG MC = new IPGATE_MODEL_CONFIG();
            MC.ConnectionString = connstr;
            MC.DBType = sqltype;
            MC.ForceTrust= forcetrus;
            MC.ConfigName = "WinConfig";
            try
            {
                if (MC.DBType == "SQLSERVER")
                { AppCenter.IDB = new IPGATE_DBO(MC); }
                else { AppCenter.IDB = new IPGATE_DBMU(MC); }
                suc = true; msg = "DB "+MC.DBType+" Ready";
                var j = await AppCenter.IDB.TestConnection();
                if(j.conn )
                {
                    if (j.hasInited)
                    {   suc = true;    }
                    else {   suc =true; msg = "Not inited.";   } 
                }
            }
            catch (Exception ze)
            { msg = "OpenDB Ex : "+ze.Message;  }
            
            return (suc, msg);
        }

        public static (List<WBGshow> lst, string info) FormatAndSortList<Twbg>(List<Twbg> list) where Twbg : notnull
        {
            string info = "";
            Type t = typeof(Twbg);
            List<WBGshow> L = new List<WBGshow>();
            StringBuilder sb = new StringBuilder();
            WBGshow? previous = null;
            int a = 0;
            if (t == typeof(WBG_V4))
            {
                foreach (var i in list)
                {
                    if (i is WBG_V4 v4)
                    {
                        try
                        {
                            WBGshow w = WBGshow.Form(v4);
                            L.Add(w);

                        }
                        catch (Exception zte)
                        { sb.AppendLine("Covert v4[" + v4.IP_ID + "] WBGshow EX:" + zte.Message); }
                    }
                }
            }
            else if (t == typeof(WBG_V6))
            {
                foreach (var i in list)
                {
                    if (i is WBG_V6 v6)
                    {
                        try
                        {
                            WBGshow w = WBGshow.Form(v6);
                            L.Add(w);

                        }
                        catch (Exception zte)
                        { sb.AppendLine("Covert v6[" + v6.IP_ID + "] WBGshow EX:" + zte.Message); }
                    }
                }
            }
            else if (t == typeof(WBG_DOM))
            {
                foreach (var i in list)
                {
                    if (i is WBG_DOM dom)
                    {
                        try
                        {
                            WBGshow w = WBGshow.Form(dom);
                            L.Add(w);

                        }
                        catch (Exception zte)
                        { sb.AppendLine("Covert dom[" + dom.DOM_ID + " @ " + dom.DOMAIN + "] WBGshow EX:" + zte.Message); }
                    }
                }
            }
            else { sb.AppendLine("Invalid List<Twbg> type, must be List<WBG_V4> or List<WBG_V6> or List<WBG_DOM> "); }
            if (L.Count > 0)
            {
                L.Sort((x, y) => x.Num.CompareTo(y.Num));
            }

            foreach (var p in L)
            {
                if (previous != null)
                { WBGshow.CheckRed(previous, p); }
                previous = p;
            }
            info = sb.ToString();
            return (L, info);
        }

        public static void BindDataGrid(  List<WBGshow>? data )
        { if (MainForm.dataGridView1.InvokeRequired)
            {   
               MainForm.dataGridView1.Invoke(new Action(() => { 
                   MainForm.dataGridView1.DataSource = data;
                   MainForm.dataGridView1.ClearSelection(); 
                   MainForm.dataGridView1.CurrentCell = null;
                   MainForm.label9.Text = AppCenter.CWBG.ToString();
                   MainForm.label10.Text = AppCenter.CIPFM.ToString();
               }));
              
            }
            else
            {
                MainForm.dataGridView1.DataSource = data;
                MainForm.dataGridView1.ClearSelection();
                MainForm.dataGridView1.CurrentCell = null;
                MainForm.label9.Text = AppCenter.CWBG.ToString();
                MainForm.label10.Text = AppCenter.CIPFM.ToString();
            }
        }

        public static void reshowSelected()
        { 
           if(MainForm.listBox2.InvokeRequired)
           {
               MainForm.listBox2.Invoke(new Action(() => { bindListSELECTED(); }));
           }
           else
           {
               bindListSELECTED();
           }
        }
        private static void bindListSELECTED()
        {
            MainForm.listBox2.BeginUpdate();
            MainForm.listBox2.Items.Clear();
            foreach (var ii in AppCenter.SELECTED)
            {
                MainForm.listBox2.Items.Add(ii.ToString());
            }
            MainForm.listBox2.EndUpdate();
            MainForm.ClearPreFixList();
        }

        public static async ValueTask<(bool suc, string msg)> DeleteSelectItems(CurIPFM ipfm, long[] ids)
        {
            bool suc = false; string msg = "";
            if (ids.Length < 1) { msg = "No items selected."; return (suc, msg); }
            // Implement the logic to delete selected items
            if (AppCenter.IDB != null)
            {
                int cz = 0;
                if (ipfm == CurIPFM.DOM)
                {
                    foreach (var id in ids)
                    { cz += await AppCenter.IDB.Delete_WBG_DOM(id); }
                }
                else if (ipfm == CurIPFM.IPV4)
                {
                    cz = await AppCenter.IDB.Delete_WBG_V4(ids);
                }
                else if (ipfm == CurIPFM.IPV6)
                {
                    cz = await AppCenter.IDB.Delete_WBG_V6(ids);
                }
                else { }
                suc = true;
                msg = $"Deleted {ipfm.ToString()} : {cz}  items.";
            }
            else
            {
                msg = "Database not initialized.";
            }
            return (suc, msg);

        }

        public static WBG_V4 MakeWBGV4(string cmt,string addr,byte hd, byte msk,string mth, NameListType wbgType)
        {
            WBG_V4 w = new WBG_V4();
            w.COMMENT = cmt;
            w.IPADDRESS = addr;
            w.Mask = msk;
            w.WBGTYPE = (byte)wbgType;
            w.MakeUTC = BLADE.TimeProvider.UtcNow;
            w.Match = mth.Trim();
            w.IPHEAD = hd;
            return w;
        }
        public static WBG_V6 MakeWBGV6(string cmt, string addr, string hd, byte msk, string mth, NameListType wbgType)
        {
            WBG_V6 w = new WBG_V6();
            w.COMMENT = cmt;
            w.IPADDRESS = addr;
            w.Mask = msk;
            w.WBGTYPE = (byte)wbgType;
            w.MakeUTC = BLADE.TimeProvider.UtcNow;
            w.Match = mth.Trim();
            w.IPHEAD = hd;
            return w;
        }
        public static WBG_DOM MakeWBGDOM(string cmt, string dom,byte nettype,byte DoR, string mth, NameListType wbgType)
        {
            WBG_DOM w = new WBG_DOM();
            w.COMMENT = cmt;
            w.DOMAIN = dom;
            w.WBGTYPE = (byte)wbgType;
            w.MakeUTC = BLADE.TimeProvider.UtcNow;
            w.Match = mth.Trim();
            w.NETTYPE = nettype;
            w.DOMTYPE = DoR;
            return w;
        }
    }

    public class WBGshow  
    {
      
        public long TID { get; set; } = 0; 
        public string HD { get; set; } = ""; 
        public string IPADDRESS { get; set; } = ""; 
        public byte Mask { get; set; } = 8; 
        public NameListType WBGTYPE { get; set; } = NameListType.ALL; 
        public string COMMENT { get; set; } = ""; 
        public string CUTC { get; set; } = ""; 
        public string Match { get; set; } = string.Empty;
        [Browsable(false)]
        public UInt128 Num { get; set; } = 0;
        [Browsable(false)]
        public UInt128 PF_S { get; set; } = UInt128.Zero;
        [Browsable(false)]
        public UInt128 PF_M { get; set; } = UInt128.Zero;
        [Browsable(false)]
        public UInt128 PF_L { get; set; } = UInt128.Zero;
        public byte RedLevel { get; set; } = 0;

        public static WBGshow Form(WBG_DOM i)
        {
            WBGshow wb = new WBGshow();
            wb.TID = i.DOM_ID;
            wb.HD = "DNS"; if (i.DOMTYPE == 20) { wb.HD = "RED"; } else if(i.DOMTYPE==30){ wb.HD = "RXG"; }
            wb.Mask = 5; if (i.NETTYPE == 4) { wb.Mask = 4; } else if (i.NETTYPE == 6) { wb.Mask = 6; }
            wb.IPADDRESS = i.DOMAIN;
            wb.COMMENT = i.COMMENT;
            wb.CUTC = i.MakeUTC.ToString(WorkClass.utcfm);
            wb.Match = i.Match.Trim();
            wb.Num = UInt128.Zero;
            wb.PF_S = UInt128.Zero;
            wb.PF_M = UInt128.Zero;
            wb.PF_L = UInt128.Zero;

            return wb;
        }
        public static WBGshow Form(WBG_V4 i)
        { 
            WBGshow wb = new WBGshow();
            wb.TID = i.IP_ID;
            wb.HD = i.IPHEAD.ToString();
            wb.Mask = i.Mask;
            wb.IPADDRESS = i.IPADDRESS;
            if (wb.Mask>0 && wb.IPADDRESS.IndexOf("/") < 0)
            { wb.IPADDRESS += "/" + wb.Mask; }
            wb.WBGTYPE = AppCenter.WBGNAME(i.WBGTYPE);
            wb.COMMENT = i.COMMENT.Trim();
            wb.CUTC = i.MakeUTC.ToString(WorkClass.utcfm);
            wb.Match = i.Match.Trim();
            var jj = IPTools.Expand(wb.IPADDRESS);
            wb.IPADDRESS = jj.fullstr;
            wb.Mask = jj.pflen;
            wb.Num = jj.NumericValue; 
            wb.PF_S = IPTools.ToUInt128(jj.bin, 24);
            wb.PF_M = IPTools.ToUInt128(jj.bin, 16);
            wb.PF_L = IPTools.ToUInt128(jj.bin, 12); 
            return wb;
        }
        public static WBGshow Form(WBG_V6 i)
        {
            WBGshow wb = new WBGshow();
            wb.TID = i.IP_ID;
            wb.HD = i.IPHEAD;
            wb.Mask = i.Mask;
            wb.IPADDRESS = i.IPADDRESS;
            if (wb.Mask > 0 && wb.IPADDRESS.IndexOf("/") < 0)
            { wb.IPADDRESS += "/" + wb.Mask; }
            wb.WBGTYPE = AppCenter.WBGNAME(i.WBGTYPE);
            wb.COMMENT = i.COMMENT.Trim();
            wb.CUTC = i.MakeUTC.ToString(WorkClass.utcfm);
            wb.Match = i.Match.Trim();
            var jj = IPTools.Expand(wb.IPADDRESS);
            wb.IPADDRESS = jj.fullstr;
            wb.Mask = jj.pflen;
            wb.Num = jj.NumericValue;
            wb.PF_S = IPTools.ToUInt128(jj.bin, 64);
            wb.PF_M = IPTools.ToUInt128(jj.bin, 56);
            wb.PF_L = IPTools.ToUInt128(jj.bin, 48);
            return wb;
        }

        public static void CheckRed(WBGshow Previous, WBGshow Next)
        {
            
            if (Previous.PF_S > 0 && Previous.PF_S == Next.PF_S)
            {
                if (Previous.RedLevel < 200) { Previous.RedLevel = 200; }   
                Next.RedLevel = 200;  
            }
            else if (Previous.PF_M > 0 && Previous.PF_M == Next.PF_M)
            {
                if (Previous.RedLevel < 130) { Previous.RedLevel = 130; }
                Next.RedLevel = 130;
            }
            else if (Previous.PF_L > 0 && Previous.PF_L == Next.PF_L)
            {
                if (Previous.RedLevel < 80) { Previous.RedLevel =80; }
                  Next.RedLevel = 80;
            }
            else
            {    Next.RedLevel = 0;  }
        }
    }

}
