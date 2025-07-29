 using System;
using System.Collections.Generic;
using System.Web;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Reflection;

namespace BLADE.TCPFORTRESS.CoreNET.DB
{
   public class Configs
    {
         public static string DefConStr = "server=110.42.187.186;  Initial Catalog=TFS;User ID=TFS;Password =tfs2023;TrustServerCertificate=True;";
         protected static bool Inited = false;
        public static void LoadDbConStr()
        {
            if (!Inited)
            {}

            if (String.IsNullOrEmpty(BLADE.TOOLS.DBO.DBCODER.DBOP.DBVBASE.DefaultConnStr))
            {
                SetDbConStr(DefConStr);
            }
        }
        public static void SetDbConStr(string defconstring)
        {
            BLADE.TOOLS.DBO.DBCODER.DBOP.DBVBASE.DefaultConnStr=defconstring;
        }
        public static string DBName = BLADE.TOOLS.DBO.DBCODER.DBNAMES.SQL;
         
    }
  
}
