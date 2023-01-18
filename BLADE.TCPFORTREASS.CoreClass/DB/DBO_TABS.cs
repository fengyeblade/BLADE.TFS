 using System;
using System.Collections.Generic;
using System.Web;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Reflection;
using BLADE.TCPFORTRESS.CoreClass.DB.DBV;

namespace BLADE.TCPFORTRESS.CoreClass.DB
{
   

   
    [Serializable]
    public class TFS_Address_DBT : BLADE.DBOP.BC.DBTBASE
    {
        public TFS_Address_DBT()
            : base("TFS_Address", "TFS_AID", true, "TFS_AID" )
        {
            V = new TFS_Address(); Configs.LoadDbConStr();
        }
          public TFS_Address VO { get { return (TFS_Address)_v; } }
        public static TFS_Address_DBT[] SelectByWhere(int topnum, string wherestr ,BLADE.DBOP.BC.SQL_Pams Ps)
        {
            TFS_Address_DBT to = new TFS_Address_DBT();
            TFS_Address_DBT[] nay = new TFS_Address_DBT[0];
            BLADE.DBOP.BC.DBB b = new BLADE.DBOP.BC.DBB(DefaultConnStr);
            DataTable dt = new DataTable();
            dt = SelectWhere( topnum, wherestr, to.TableName, dt , Ps);
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
    public class TFS_LOGS_DBT : BLADE.DBOP.BC.DBTBASE
    {
        public TFS_LOGS_DBT()
            : base("TFS_LOGS", "TFS_LOGID", true, "TFS_LOGID" )
        {
            V = new TFS_LOGS(); Configs.LoadDbConStr();
        }
          public TFS_LOGS VO { get { return (TFS_LOGS)_v; } }
        public static TFS_LOGS_DBT[] SelectByWhere(int topnum, string wherestr ,BLADE.DBOP.BC.SQL_Pams Ps)
        {
            TFS_LOGS_DBT to = new TFS_LOGS_DBT();
            TFS_LOGS_DBT[] nay = new TFS_LOGS_DBT[0];
            BLADE.DBOP.BC.DBB b = new BLADE.DBOP.BC.DBB(DefaultConnStr);
            DataTable dt = new DataTable();
            dt = SelectWhere( topnum, wherestr, to.TableName, dt , Ps);
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
