using Lib.Net.UDP;
using Runtime.Entity;
using Runtime.Net;
using Runtime.Node;
using Runtime.Res;
using ServerRuntimeCmd.Server;
using ServerRuntimeCmd.Server.Main;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServerRuntimeCmd.Main
{
    class NetMain
    {
        //主线程
        public static NetThread NetThread;
        //输入字符串缓存
        //public static string InStrBuffer = "";
        //输出字符串缓存
        //public static string OutStrBuffer = "";


        private static void Main(string[] args)
        {
            Init();
            NetThread = new NetThread();
            //启动主线程
            NetThread.Start();
            Parameters(args);
            //欢迎界面
            Console.WriteLine("Hello World");
            //指令交互界面
            while (true)
            {
                //Console.Write(">");
                //if (HandleManager.OutStr != "")
                //{
                //    Console.WriteLine(HandleManager.OutStr);
                //    HandleManager.OutStr = "";
                //}
                Thread.Sleep(100);
                //InStrBuffer = Console.ReadLine();
            }
        }
        private static void Parameters(string[] args)
        {
            string str = "";
            //服务端接受到数据请求时
            foreach (var s in args)
            {
                str += s;
            }
            Console.WriteLine("参数:" + str);

        }

        private static void Init()
        {
            //NetControl.Instance.Init();
        }
    }
}
