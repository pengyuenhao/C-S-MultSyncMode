using System.Collections;
using Lib.Net.UDP;

namespace Lib.Net.UDP
{
	public class UdpManager
	{
        /// <summary>
        /// 单例
        /// </summary>
        public static UdpManager Instance = new UdpManager();
        //服务端节点
        public UdpPoint Server;
        //客户端节点
        public UdpPoint Client;
        //广播端节点
        public UdpPoint Broadcast;
    }
}