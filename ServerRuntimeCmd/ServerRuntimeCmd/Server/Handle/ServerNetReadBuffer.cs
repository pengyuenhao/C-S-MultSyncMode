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
    public static class NetReadBuffer
    {
        static ServerControl ServerCtrl { get { return ServerControl.Instance; } }

        public static void OnReadBufferServer(UdpPoint point, IPEndPoint remote, ReadBufferHandle handle)
        {
            ReplyDataHandle replyCmdHandle;

        }
    }
}
