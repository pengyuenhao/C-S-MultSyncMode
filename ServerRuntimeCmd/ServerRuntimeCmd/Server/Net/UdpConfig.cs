using System.Collections;
using System.Net;
using System.Net.Sockets;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace Lib.Net.UDP
{
    public static class UdpConfig
    {
        /// <summary>
        /// 时间修正值
        /// </summary>
        public static int offset = 0;
        //远程服务默认端口
        public static int ServerPort = 25565;
        //默认广播接收端口
        public static int[] BroadcastPort = new int[] {20000,30000,40000,50000,60000};

        //主服务器DNS信息
        private static string[] DNS = { "www.pengyunhao.top:30000", "pengyuenhao.vicp.cc:11407", "450651f1.nat123.net:21786" };

        //主服务器地址
        public static IPEndPoint MainServerIP { get { return DnsToIPEndPoint(DNS[0]); } }
        //本机广播地址
        public static IPEndPoint Broadcast
        {
            get
            {
                if (m_CurrentPort >= BroadcastPort.Length -1)
                {
                    m_CurrentPort = 0;
                }
                else
                {
                    m_CurrentPort += 1;
                }
                //Debug.Log("[广播地址]"+new IPEndPoint(GetBroadcastIP(), BroadcastPort[m_CurrentPort]));
                return new IPEndPoint(GetBroadcastIP(), BroadcastPort[m_CurrentPort]);
            }
        }
        private static int m_CurrentPort = 0;
        /// <summary>
        /// 本机服务端地址
        /// </summary>
        public static IPEndPoint LocalServerIP { get { return new IPEndPoint(IPAddress.Parse(GetLocalAddress()), ServerPort); } }
        public static string LocalAddress { get { return GetLocalAddress(); } }
        public static IPAddress LocalIP { get { return GetLocalIP(); } }

        private static string GetLocalAddress()
        {
            return GetLocalIP().ToString();
        }
        private static IPAddress GetLocalIP()
        {
            try
            {
                string HostName = Dns.GetHostName(); //得到主机名
                IPHostEntry IpEntry = Dns.GetHostEntry(HostName);
                for (int i = 0; i < IpEntry.AddressList.Length; i++)
                {
                    //从IP地址列表中筛选出IPv4类型的IP地址
                    //AddressFamily.InterNetwork表示此IP为IPv4,
                    //AddressFamily.InterNetworkV6表示此地址为IPv6类型
                    if (IpEntry.AddressList[i].AddressFamily == AddressFamily.InterNetwork)
                    {
                        return IpEntry.AddressList[i];
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        private static IPAddress GetBroadcastIP()
        {
            string addr = GetLocalAddress();
            addr = addr.Substring(0,addr.LastIndexOf('.'));
            addr += ".255";
            return IPAddress.Parse(addr);
        }

        public static IPEndPoint DnsToIPEndPoint(string value)
        {
            if (value.LastIndexOf(':') == -1) return null;
            string address = value.Substring(0, value.LastIndexOf(':'));
            IPHostEntry hostinfo = Dns.GetHostEntry(address);
            IPAddress[] aryIP = hostinfo.AddressList;
            if (aryIP == null || aryIP[0] == null) return null;
            int port = int.Parse(value.Substring(value.LastIndexOf(':') + 1));
            if (port >= 0 && port <= 65535)
            {
                return new IPEndPoint(aryIP[0], port);
            }
            else
            {
                return null;
            }
        }

        public static bool ValidatePort(int port)
        {
            // on false, API should throw new ArgumentOutOfRangeException("port");
            return port >= IPEndPoint.MinPort && port <= IPEndPoint.MaxPort;
        }
    }
    //网络节点信息
    public class NetNodeInfo
    {
        //验证码，用于各端通信互相识别
        public int verify;
        //密码，用于各端通信识别
        public int password;
        //玩家唯一识别码
        public int id;
        //玩家名称
        public string name;
        //玩家当前网络地址
        public IPEndPoint remote;
        //通信延迟
        public int delay;
        //通信最后同步时间
        public int lastTime;
        //是否被创建
        public bool isVisible;
        //是否有效
        public bool isValid;
        //消息缓存
        public ConcurrentDictionary<NetNodeInfo, List<string>> message;
        //通信序列号
        public int serial;
        //回复字典
        public ConcurrentDictionary<int, byte[]> reply;
    }
}