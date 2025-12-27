using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

using BLADE.MSGCORE.ClientTools;
using BLADE.MSGCORE.Models;

namespace BLADE.TFS.HOMEGATE.COMM
{
    public class DNameItem
    {
        public string dname = "";
        public DateTime dnstime = DateTime.Now;
        public string IP = "";

        public DNameItem(string inDNAME)
        {
            dname = inDNAME.ToLower().Trim();
            dnstime = DateTime.Now.AddHours(-5);
            IP = "127.0.0.1";
        }
    }
    public class DNameCatch
    {
        protected static SortedList<string, DNameItem> COL = new SortedList<string, DNameItem>(StringComparer.OrdinalIgnoreCase);
        public static async Task<string> GetIP(string indname)
        {
            string nnn = indname.ToLower().Trim();
            string ii = "127.0.0.1";

            lock (COL)
            {

                if (COL.ContainsKey(nnn))
                {
                    if ((DateTime.Now - COL[nnn].dnstime).TotalMinutes < 6)
                    {
                        ii = COL[nnn].IP;
                    }
                    else
                    {
                        COL[nnn].IP = dnsIP(nnn).Result;
                        COL[nnn].dnstime = DateTime.Now;
                        ii = COL[nnn].IP;
                    }
                }
                else
                {
                    DNameItem td = new DNameItem(nnn);
                    td.IP = dnsIP(nnn).Result;
                    td.dnstime = DateTime.Now;
                    ii = td.IP;
                    COL.Add(nnn, td);

                }
            }


            return ii;
        }
        public static async Task<string> dnsIP(string indname)
        {
            string ii = "127.0.0.1";
            string la = indname.ToLower().Trim();
            string nnn = la.Replace("mkv:", "").Trim();
            if (la.StartsWith("mkv:"))
            {
                var rr = await BLADE.MSGCORE.ClientTools.ClientCore.MKV_CreateQryPost(0, nnn);
                var pmi = BLADE.TOOLS.BASE.Json.JsonOptions.Deserialize< PostResponse>(rr.ResponseText);
                if ( pmi!=null && pmi.secKEY==99999999)
                {
                    string jsonPostMkvItem = Encoding.UTF8.GetString(Convert.FromBase64String(pmi.Message));
                    var mi = BLADE.TOOLS.BASE.Json.JsonOptions.Deserialize<PostMkvItem>(jsonPostMkvItem);
                    if (mi!=null && mi.KEYVALUE.Length>0)
                    {
                        await RunCenter.AddLogAsync( "MkvDNAME", "mkv: " +  mi.KEYNAME + " = " + mi.KEYVALUE, TOOLS.LOG.LogCodeEnum.Note);
                        return mi.KEYVALUE.Trim();
                    }
                }
                await RunCenter.AddLogAsync("MkvDNAME", "MKV get Null : " + nnn  , TOOLS.LOG.LogCodeEnum.Note);
            }
            try
            {
                IPHostEntry IPinfo = Dns.GetHostEntry(nnn);

                if (IPinfo.AddressList.Length > 0)
                {
                    ii = IPinfo.AddressList[0].ToString();
                    await RunCenter.AddLogAsync( "DnsDNAME", "dns: " + nnn + " = " + ii, TOOLS.LOG.LogCodeEnum.Note);
                }

            }
            catch { }

            return ii;
        }
    }
}
