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
        public TFS_Address() { }
        /// <summary>
        /// IP��ַ  IPV4  IPV6   ������IP��IP��   192.168.100.100    192.168.20.0/24 
        /// </summary>
        public string TFS_AddressStr = "";
        /// <summary>
        /// ���� ID
        /// </summary>
        public int TFS_AID = 0;
        /// <summary>
        /// ���ʱ��   
        /// </summary>
        public DateTime TFS_ALastTime = DateTime.Now.AddMinutes(-10);
        /// <summary>
        ///  false = IP��ַ     true = IP��
        /// </summary>
        public bool TFS_CIDR = false;
        /// <summary>
        /// false= IPV4   true = IPV6
        /// </summary>
        public bool TFS_IpV6 = false;
        /// <summary>
        /// ���Ӽ���  ��ֵ��������ʹ�ã��������ݿ��ֵ������
        /// </summary>
        public int TFS_ReactCount = 0;
        /// <summary>
        ///  0=������    1=������    2=������   
        ///   ������=����������    ������=����������    ������=�������м������Ӵ�������λʱ�������Ӵ����ﵽ��ֵ��Ϊ������
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
    public class TFS_LOGS : BLADE.TOOLS.DBO.DBCODER.DBOP.VO.DBValue
    {
        public TFS_LOGS() { }
        /// <summary>
        /// ������־�� ����IP��ַ
        /// </summary>
        public string TFS_LOGAddress = "";
        /// <summary>
        /// ����ID
        /// </summary>
        public int TFS_LOGID = 0;
        /// <summary>
        /// ��־����
        /// </summary>
        public string TFS_LOGINFO = "";
        /// <summary>
        /// ��־�ؼ��� KEY
        /// </summary>
        public int TFS_LOGKEY = 0;
        /// <summary>
        /// ��־ʱ��
        /// </summary>
        public DateTime TFS_LOGTime = DateTime.Now;

    }



    // ====== view ==========  


}
