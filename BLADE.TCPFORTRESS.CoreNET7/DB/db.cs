using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BV7 = BLADE.BASECLASS_NET7.DBOP.BC;
using BLADE.TCPFORTRESS.CoreNET7.DB.DBV;

namespace BLADE.TCPFORTRESS.CoreNET7.DB
{
    public class Configs
    {
        public static string DefConStr = "Data Source=127.0.0.1,22233;  Initial Catalog=TFS;User ID=TFS;Password =pAsSwOrD;";
        protected static bool Inited = false;
        public static void LoadDbConStr()
        {
            if (!Inited)
            { }

            if (String.IsNullOrEmpty(BLADE.BASECLASS_NET7.DBOP.BC.DBVBASE.DefaultConnStr))
            {
                SetDbConStr(DefConStr);
            }
        }
        public static void SetDbConStr(string defconstring)
        {
            BLADE.BASECLASS_NET7.DBOP.BC.DBVBASE.DefaultConnStr = defconstring;
        }
        public static BLADE.BASECLASS_NET7.DBOP.BC.DBNAMES DBName = BLADE.BASECLASS_NET7.DBOP.BC.DBNAMES.SQL;

    }

    [Serializable]
    public class TFS_Address_DBT : BV7.DBTBASE
    {
        public TFS_Address_DBT()
            : base("TFS_Address", "TFS_AID", true, "TFS_AID")
        {
            V = new TFS_Address(); Configs.LoadDbConStr();
        }
        public TFS_Address VO { get { return (TFS_Address)_v; } }
        public static TFS_Address_DBT[] SelectByWhere(int topnum, string wherestr, BV7.SQL_Pams Ps)
        {
            TFS_Address_DBT to = new TFS_Address_DBT();
            TFS_Address_DBT[] nay = new TFS_Address_DBT[0];
            BV7.DBB b = new BV7.DBB(DefaultConnStr);
            DataTable dt = new DataTable();
            dt = SelectWhere(topnum, wherestr, to.TableName, dt, Ps);
            if (dt.Rows.Count > 0)
            {
                nay = new TFS_Address_DBT[dt.Rows.Count];
                for (int z = 0; z < nay.Length; z++)
                {
                    nay[z] = new TFS_Address_DBT();
                    nay[z].Fill(dt.Rows[z]);
                    nay[z].IsNew = false;
                    nay[z].IsNull = false;
                }
            }
            dt.Dispose();
            return nay;

        }
    }


    [Serializable]
    public class TFS_LOGS_DBT : BV7.DBTBASE
    {
        public TFS_LOGS_DBT()
            : base("TFS_LOGS", "TFS_LOGID", true, "TFS_LOGID")
        {
            V = new TFS_LOGS(); Configs.LoadDbConStr();
        }
        public TFS_LOGS VO { get { return (TFS_LOGS)_v; } }
        public static TFS_LOGS_DBT[] SelectByWhere(int topnum, string wherestr, BV7.SQL_Pams Ps)
        {
            TFS_LOGS_DBT to = new TFS_LOGS_DBT();
            TFS_LOGS_DBT[] nay = new TFS_LOGS_DBT[0];
            BV7.DBB b = new BV7.DBB(DefaultConnStr);
            DataTable dt = new DataTable();
            dt = SelectWhere(topnum, wherestr, to.TableName, dt, Ps);
            if (dt.Rows.Count > 0)
            {
                nay = new TFS_LOGS_DBT[dt.Rows.Count];
                for (int z = 0; z < nay.Length; z++)
                {
                    nay[z] = new TFS_LOGS_DBT();
                    nay[z].Fill(dt.Rows[z]);
                    nay[z].IsNew = false;
                    nay[z].IsNull = false;
                }
            }
            dt.Dispose();
            return nay;

        }
    }




}
