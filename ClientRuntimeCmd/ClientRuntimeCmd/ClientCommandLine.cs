using Helper.CmdMgr;
using Lib.Net.UDP;
using Runtime.CmdLine;
using Runtime.Entity;
using Runtime.Main;
using Runtime.Net;
using Runtime.Node;
using Runtime.Order;
using Runtime.Time;
using Runtime.Util.Singleton;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ClientRuntimeCmd
{
    public class ClientCommandLine
    {
        public static ClientCommandLine Instance { get { return SingletonControl.GetInstance<ClientCommandLine>(); } }

        public void Init()
        {
            CommandLineArgumentProxy.Instance.ArgumentParserEvent += IsListArgument;
            CommandLineArgumentProxy.Instance.ArgumentParserEvent += IsConnectArgument;
            CommandLineArgumentProxy.Instance.ArgumentParserEvent += IsHelpArgument;
            CommandLineArgumentProxy.Instance.ArgumentParserEvent += IsQuitArgument;
            CommandLineArgumentProxy.Instance.ArgumentParserEvent += IsKeyArgument;
        }

        void IsListArgument(CommandLineArgumentParser arguments,ref int valid)
        {
            if (arguments.StartWith("/list"))
            {
                int count = 0;
                int validCount = 0;
                int overtimeCount = 0;
                int removeCount = 0;
                int reconnectCount = 0;
                string strInfo = "";
                foreach (var item in NetControl.Instance.ServerInfoDic)
                {
                    strInfo += "\n" + "[" + count + "]" + "[n]" + (item.Value.name != "" ? item.Value.name : "null") + "[rt]" + item.Value.remote + "[ov]" + (TimeControl.TickTime - item.Value.lastTime) + "ms" + "[rt]" + item.Value.isTryTest;
                    count += 1;

                    if (item.Value.isValid)
                    {
                        if (item.Value.isOvertime)
                        {
                            overtimeCount += 1;
                        }
                        else
                        {
                            validCount += 1;
                        }
                    }
                    else
                    {
                        //移除无效且超时的服务端
                        if (item.Value.isOvertime)
                        {
                            removeCount += 1;
                        }
                        else
                        {
                            reconnectCount += 1;
                        }
                    }
                }
                Console.WriteLine("[服务端]" + validCount + "[超时]" + overtimeCount + "[重连]" + reconnectCount + "[失效]" + removeCount + strInfo);
                valid += 1;
                return;
            }
            else
            {
                return;
            }
        }
        void IsConnectArgument(CommandLineArgumentParser arguments,ref int valid)
        {
            if (arguments.StartWith("/connect"))
            {
                if (arguments.Has("-t"))
                {
                    ServerInfo info = null;
                    if (arguments.Has("-f"))
                    {
                        foreach (var item in NetControl.Instance.ServerInfoDic)
                        {
                            if (item.Value.isValid)
                            {
                                info = item.Value;
                                NetControl.Instance.TryServerConnectTest(info);
                                valid += 1;return;
                            }
                        }
                        Console.WriteLine("未找到任何服务端");
                        valid += 1;return;
                    }
                    else
                    {
                        CommandLineArgument node = arguments.Get("-t");
                        if (node.Take() != null && node.Take() != "")
                        {
                            string ip = node.Take();
                            IPEndPoint point = UdpConfig.StrParseToIp(ip);
                            if (point == null)
                            {
                                point = UdpConfig.DnsToIPEndPoint(ip);
                            }
                            if (point != null)
                            {
                                NetControl.Instance.TryServerConnectTest(point);
                                valid += 1;return;
                            }
                            else
                            {
                                Console.WriteLine("无效的网络地址");
                                valid += 1;return;
                            }
                        }
                    }

                    if (info != null)
                    {
                        NetControl.Instance.TryServerConnectTest(info);
                    }
                    else
                    {
                        Console.WriteLine("未选择任何服务端");
                    }
                    valid += 1;
                    return;
                }
                if (arguments.Has("-c"))
                {
                    ServerInfo info = NetControl.Instance.CurrentConnectHost;
                    if (info != null)
                    {
                        NetControl.Instance.TryCanelConnectHost();
                    }
                    else
                    {
                        Console.WriteLine("未连接任何服务端");
                    }
                    valid += 1;return;
                }
                if (arguments.Has("-f"))
                {
                    foreach (var item in NetControl.Instance.ServerInfoDic)
                    {
                        if (item.Value.isValid)
                        {
                            NetControl.Instance.TryConnectHost(item.Value);
                            valid += 1;return;
                        }
                    }
                    Console.WriteLine("未找到任何服务端");
                    valid += 1;return;
                }
                if (arguments.Has("-i"))
                {
                    ServerInfo info = NetControl.Instance.CurrentConnectHost;
                    if (info != null)
                    {
                        Console.WriteLine("[n]" + info.name + "[d]" + info.description + "[r]" + info.remote + "[is]" + info.isSync + ":" + NodeControl.Instance.LocalPlayer.serial + "" + "[ic]" + info.isConnect + "[iv]" + info.isValid + "[io]" + info.isOvertime + "[nc]" + info.currentConnectNodeCount + "[mc]" + info.maxConnectNodeCount + "[ot]" + (TimeControl.TickTime - info.lastTime) + "ms");
                    }
                    else
                    {
                        Console.WriteLine("未连接任何服务端");
                    }
                    valid +=1;
                }
                Console.WriteLine("输入参考:\n参数 -c 取消当前连接\n参数 -f 连接第一个有效的服务端\n参数 -i 查看当前连接信息\n参数 -t[address] 尝试获取服务端信息");
                valid +=1;
            }
            else
            {
                return;
            }
        }
        void IsHelpArgument(CommandLineArgumentParser arguments,ref int valid)
        {
            if (arguments.StartWith("/help"))
            {
                Console.WriteLine("指令参考:\n指令 /list 查看当前服务端列表\n指令 /connect 连接服务端相关");
                valid +=1;
                return;
            }
            else
            {
                return;
            }
        }
        void IsQuitArgument(CommandLineArgumentParser arguments,ref int valid)
        {
            if (arguments.StartWith("/quit"))
            {
                Console.WriteLine("退出程序");
                Environment.Exit(0);
                valid += 1;return;
            }
            else
            {
                return;
            }
        }
        void IsKeyArgument(CommandLineArgumentParser arguments,ref int valid)
        {
            if (arguments.StartWith("/key"))
            {
                if (arguments.Has("-d"))
                {
                    if(EntityControl.Instance.MasterEntityControllIndex == 0)
                    {
                        Console.WriteLine("[无可操作实体]");
                        valid += 1;
                        return;
                    }
                    CommandLineArgument node = arguments.Get("-d");
                    IntVector2 vector = IntVector2.Zero;
                    int value = 0;
                    string s1 = null;
                    if (node != null)
                    {
                        s1 = node.Take();
                        if(s1!=null&&!int.TryParse(s1, out value))
                        {
                            value = 0;
                        }
                    }
                    string s2 = null;
                    if (s1!=null && node.Next !=null)
                    {
                        s2 = node.Next.Take();
                        if (s2!=null&&!IntVector2.TryParse(s2, out vector))
                        {
                            vector = IntVector2.Zero;
                        }
                    }
                    DriverOrderCode orderCode = new DriverOrderCode().SetTragerPoint(vector).SetValue(value).SetOrigin(EntityControl.Instance.MasterEntityControllIndex);
                    OrderCmd cmd = OrderCmd.Instance(orderCode.ToBytes());
                    ControlModel.NetCtrl.WhenCollectCmd(cmd.ToBytes());
                    Console.WriteLine("[索引]" + orderCode.index + "[速度]" + orderCode.vector +"[旋转]"+orderCode.value + "[长度]" + cmd.ToBytes().Length);
                    valid += 1;return;
                }
                Console.WriteLine("[无效指令]");
                valid += 1;return;
            }
            else
            {
                return;
            }
        }

    }
}
