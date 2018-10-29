using Helper.CmdMgr;
using Lib.Net.UDP;
using ServerRuntimeCmd.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Runtime.RuntimeFSM;
using Runtime.Main;
using Runtime.Time;
using Runtime.Entity;
using Runtime.Net;
using ServerRuntimeCmd.Server.FSM;

namespace ServerRuntimeCmd.Main
{
    class NetThread
    {
        //本地运行状态机
        LocalFSM localFSM = new LocalFSM();
        ServerFSM serverFSM = new ServerFSM();

        int sleepTime;
        public Thread netMainThread;
        public void Start()
        {
            //本地运行初始化
            localFSM.Init();
            serverFSM.Init();
            //主线程执行间隔
            sleepTime = 50;
            //主线程
            netMainThread = new Thread(NetMainThread);
            //启动主线程
            netMainThread.Start();
            //Console.WriteLine(Server.LocalEndPoint);
        }

        private void NetMainThread()
        {
            while (true)
            {
                Thread.Sleep(sleepTime);
                serverFSM.FSM.Runtime(sleepTime);
                localFSM.FSM.Runtime(sleepTime);
            }
        }
    }
}
