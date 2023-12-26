using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLADE.TCPFORTRESS.CoreNET7.DB.DBV
{
    // ======= table ==========


    [Serializable]
    public class TFS_Address : BLADE.BASECLASS_NET7.DBOP.BC.DBV.DBValue
    {
        public TFS_Address() { }
        /// <summary>
        /// IP地址  IPV4  IPV6   可以是IP或IP段   192.168.100.100    192.168.20.0/24 
        /// </summary>
        public string TFS_AddressStr = "";
        /// <summary>
        /// 主键 ID
        /// </summary>
        public int TFS_AID = 0;
        /// <summary>
        /// 最后活动时间   
        /// </summary>
        public DateTime TFS_ALastTime = DateTime.Now.AddMinutes(-10);
        /// <summary>
        ///  false = IP地址     true = IP段
        /// </summary>
        public bool TFS_CIDR = false;
        /// <summary>
        /// false= IPV4   true = IPV6
        /// </summary>
        public bool TFS_IpV6 = false;
        /// <summary>
        /// 连接计数  此值在运行中使用，存入数据库的值无意义
        /// </summary>
        public int TFS_ReactCount = 0;
        /// <summary>
        ///  0=白名单    1=灰名单    2=黑名单   
        ///   白名单=无条件放行    黑名单=无条件放行    灰名单=在运行中计数连接次数，单位时间内链接次数达到阈值记为灰名单
        /// </summary>
        public int TFS_WhiteOrBlack = 0;

        public string TFS_K1 = "0";
        public string TFS_K2 = "0";
        public string TFS_K3 = "0";

        public int X = 0;
        public void fenX()
        {
            int z = 0;
            try
            {
                z = int.Parse(TFS_K2.Substring(0, 1));
                X = z;
            }
            catch
            {
                X = -9;
            }


        }




    }
    [Serializable]
    public class TFS_LOGS : BLADE.BASECLASS_NET7.DBOP.BC.DBV.DBValue
    {
        public TFS_LOGS() { }
        /// <summary>
        /// 触发日志的 接入IP地址
        /// </summary>
        public string TFS_LOGAddress = "";
        /// <summary>
        /// 主键ID
        /// </summary>
        public int TFS_LOGID = 0;
        /// <summary>
        /// 日志文字
        /// </summary>
        public string TFS_LOGINFO = "";
        /// <summary>
        /// 日志关键字 KEY
        /// </summary>
        public int TFS_LOGKEY = 0;
        /// <summary>
        /// 日志时间
        /// </summary>
        public DateTime TFS_LOGTime = DateTime.Now;


    }



    // ====== view ==========  

}
