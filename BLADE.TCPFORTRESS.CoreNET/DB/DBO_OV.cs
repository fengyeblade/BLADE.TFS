 using System;
using System.Collections.Generic;
using System.Web;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Reflection;


namespace BLADE.TCPFORTRESS.CoreNET.DB.DBV
{
   

     // ======= table ==========

 
 [Serializable]
    public class TFS_Address : BLADE.TOOLS.DBO.DBCODER.DBOP.VO.DBValue
    {
     public TFS_Address() {  }
           public string TFS_AddressStr = ""; 
   public int TFS_AID = 0; 
   public DateTime TFS_ALastTime = DateTime.Now; 
   public bool TFS_CIDR = false; 
   public bool TFS_IpV6 = false; 
   public string TFS_K1 = ""; 
   public string TFS_K2 = ""; 
   public string TFS_K3 = ""; 
   public int TFS_ReactCount = 0; 
   public int TFS_WhiteOrBlack = 0; 
   public int X = 0; 

       
    }
  
  
  
 
 [Serializable]
    public class TFS_LOGS : BLADE.TOOLS.DBO.DBCODER.DBOP.VO.DBValue
    {
     public TFS_LOGS() {  }
           public string TFS_LOGAddress = ""; 
   public int TFS_LOGID = 0; 
   public string TFS_LOGINFO = ""; 
   public int TFS_LOGKEY = 0; 
   public DateTime TFS_LOGTime = DateTime.Now; 

       
    }
  
  
  
    // ====== view ==========  

 
}
