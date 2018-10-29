using Lib.Net.UDP;
using ServerRuntimeCmd.Server.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ServerRuntimeCmd.Server
{
    static class NetCmdData
    {
        static ServerControl ServerCtrl { get { return ServerControl.Instance; } }
        //接收所有指令并缓存在队列内
        public static void OnCmdDataServer(UdpPoint point, IPEndPoint remote, CmdDataHandle handle)
        {
            //Console.WriteLine("客户端的更新帧" + handle.frame+"数据大小"+ handle.Size);
            //将客户端发出数据的更新帧指令集压入进入缓存
            ServerCtrl.NodeCmdBuffer.Push(handle.cmd);
        }
    }
}
