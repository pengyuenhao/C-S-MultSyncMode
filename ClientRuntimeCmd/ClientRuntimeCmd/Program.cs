using Lib.Net.UDP;
using Microsoft.SqlServer.Server;
using Runtime.Component;
using Runtime.Entity;
using Runtime.FSM;
using Runtime.Main;
using Runtime.Net;
using Runtime.Node;
using Runtime.Order;
using Runtime.Time;
using Runtime.Util;
using Runtime.Util.Binarization;
using Runtime.Util.Rand;
using Runtime.Util.Xml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization.Formatters.Soap;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ClientRuntimeCmd
{
    class Program
    {
        static void Main(string[] args)
        {
            //切换为指令控制模式
            ClientCommandLine.Instance.Init();
            int frame = 50;

            Func func = new Func();
            func.Start();

            Random random = new Random();

            while (true)
            {
                Thread.Sleep(frame);
                func.netFSM.FSM.Runtime(frame);
                func.gameFSM.FSM.Runtime(frame);

                //Console.WriteLine(TimeControl.TickTime);
            }
        }


    }

    public class Func
    {
        public NetFSM netFSM;
        public GameFSM gameFSM;

        public void Start()
        {
            //int iii = 0;
            //OrderControl.Instance.Init();
            //Action<DriverOrderCode> action = (DriverOrderCode order) => { iii++; };
            //OrderControl.Instance.AddListener(action);
            //OrderControl.Instance.RemoveListener(action);
            //OrderControl.Instance.AddListener((DriverOrderCode order) =>
            //{
            //    iii+=10000;
            //    //throw new Exception();
            //});
            //OrderControl.Instance.AddListener(1,(DriverOrderCode order) =>
            //{
            //    throw new Exception();
            //});
            //OrderControl.Instance.RemoveListener<DriverOrderCode>(1);
            //byte[] buffer;
            //DriverOrderCode orderCode = new DriverOrderCode();
            //orderCode.index = 1;
            //orderCode.targer = 2;
            //orderCode.value = 3;
            //orderCode.vector = new IntVector2(4, 5);
            //buffer = orderCode.ToBytes();

            //OrderControl.Instance.RunListener(buffer);
            //long t = TestTool.RunTime(() =>
            //{
            //    for (int i = 0; i < 10000; i++)
            //    {
            //        OrderControl.Instance.RunListener(buffer);
            //    }
            //});
            //Console.WriteLine("[方法]" + t + "ms");
            //throw new Exception();
            //OrderCode order = new OrderCode();
            //order.origin = 123;
            //order.vector = new IntVector2(1, 2);
            //order.value = 999;
            //order.targer = 33;
            //byte[] aaa = OrderControl.Instance.ReflectOrderToBytes(order);
            //byte[] bbb = order.ToBytes();

            //long t = TestTool.RunTime(() => 
            //{
            //    OrderCode o1 = OrderControl.Instance.ReflectBytesToOrder<OrderCode>(aaa);
            //});
            //Console.WriteLine("[方法1]" + t + "ms");
            //t = TestTool.RunTime(() =>
            //{
            //    OrderCode o2;
            //    for (int i = 0; i < 100; i++)
            //    {
            //        o2 = OrderControl.Instance.ReflectBytesToOrder<OrderCode>(bbb);
            //    }
            //});
            //Console.WriteLine("[方法2]" + t + "ms");
            //t = TestTool.RunTime(() =>
            //{
            //    OrderCode o3;
            //    for (int i = 0; i < 100; i++)
            //    {
            //        o3 = new OrderCode(bbb);
            //    }
            //});
            //Console.WriteLine("[方法3]" + t + "ms");

            EntityModel test = new EntityModel();
            test.Id = "TestUnit";
            test.name = "Test";
            test.components = new List<EntityComponent>();
            test.components.Add(new EntityController());
            test.components.Add(new EntityDriver());
            test.ToXml("./Base/Entity/Test/Test.xml","TestUnit");
            //test.ToXml("D:/Program/ServerRuntimeProject/ServerRuntimeCmd/ServerRuntimeCmd/bin/Debug/Base/Entity/Test/Test.xml", "TestUnit");


            EntityModel a = test.Clone();
            EntityModel b = test.Clone();

            ((EntityDriver)a.components[1]).count = 888;
            ((EntityDriver)b.components[1]).count = 777;

            BufferData data = new BufferData();
            data.entitys = new KeyValuePair<int, EntityModel>[2]
            {
                new KeyValuePair<int, EntityModel>(123,a),
                new KeyValuePair<int, EntityModel>(456,b)
            };
            data.ToXml("./EntityCache.xml", "null");

            netFSM = new NetFSM();
            netFSM.Init();

            gameFSM = new GameFSM();
            gameFSM.Init();
        }
    }
}
