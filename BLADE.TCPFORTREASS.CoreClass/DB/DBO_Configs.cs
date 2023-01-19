 using System;
using System.Collections.Generic;
using System.Web;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Reflection;

namespace BLADE.TCPFORTRESS.CoreClass.DB
{
    public class Configs
    {
        public static string DefConStr = "Data Source=127.0.0.1,22233;  Initial Catalog=TFS;User ID=TFS;Password =pAsSwOrD;";
        protected static bool Inited = false;
        public static void LoadDbConStr()
        {
            if (!Inited)
            { }

            if (String.IsNullOrEmpty(BLADE.DBOP.BC.DBVBASE.DefaultConnStr))
            {
                SetDbConStr(DefConStr);
            }
        }
        public static void SetDbConStr(string defconstring)
        {
            BLADE.DBOP.BC.DBVBASE.DefaultConnStr = defconstring;
        }
        public static BLADE.DBOP.BC.DBNAMES DBName = BLADE.DBOP.BC.DBNAMES.SQL;

    }

}
