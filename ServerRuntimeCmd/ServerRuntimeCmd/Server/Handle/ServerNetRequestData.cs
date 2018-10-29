using Helper.CmdMgr;
using Lib.Net.UDP;
using Runtime.Entity;
using Runtime.Net;
using Runtime.Node;
using Runtime.Util.Binarization;
using ServerRuntimeCmd.Server.Server;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ServerRuntimeCmd.Server
{
    static class NetRequestData
    {
        static ServerControl ServerCtrl { get { return ServerControl.Instance; } }

        public static void OnRequestDataServer(UdpPoint point, IPEndPoint remote, RequestDataHandle handle)
        {
            byte[] value;
            switch (handle.request)
            {
                case RequestEnum.Public:
                    value = OnPulbic(point, remote, handle);
                    break;
                case RequestEnum.Register:
                    value = OnRegister(point, remote, handle);
                    break;
                case RequestEnum.ServerInfo:
                    value = OnServerInfo(point, remote, handle);
                    break;
                case RequestEnum.SyncInfo:
                    value = OnSyncInfo(point, remote, handle);
                    break;
                case RequestEnum.ServerCanel:
                    value = OnServerCancel(point, remote, handle);
                    break;
                case RequestEnum.ConnectTest:
                    value = OnConnectTest(point, remote, handle);
                    break;
                case RequestEnum.ConnectInfo:
                    value = OnConnectInfo(point, remote, handle);
                    break;
                case RequestEnum.ConnectValid:
                    value = OnConnectValid(point, remote, handle);
                    break;
                case RequestEnum.InitFrame:
                    value = OnInitFrame(point, remote, handle);
                    break;
                case RequestEnum.RequestCreateEntity:
                    value = OnRequestCreateEntity(point, remote, handle);
                    break;
                case RequestEnum.RequestInitData:
                    value = OnRequestInitData(point, remote, handle);
                    break;
                case RequestEnum.RequestInitStatus:
                    value = OnRequestInitStatus(point, remote, handle);
                    break;
                case RequestEnum.RequestInitFinish:
                    value = OnRequestInitFinish(point, remote, handle);
                    break;
                default:
                    value = new byte[0];
                    break;
            }
            //如果回复数据为null，则不发送回复
            if (value != null)
            {
                point.BunchTaskAdd(remote, ReplyDataHandle.Instance(handle.request, handle.verify, value));
                //Console.WriteLine("[回复验证值]"+handle.verify + "[至]" + remote);
            }
        }
        /*
         * 接收到指定标识包时的处理方法委托
         */
        private static byte[] OnPulbic(UdpPoint point, IPEndPoint remote, RequestDataHandle handle)
        {
            return new byte[0];
        }
        private static byte[] OnRegister(UdpPoint point, IPEndPoint remote, RequestDataHandle handle)
        {
            byte[] value = new byte[4];
            value[0] = (byte)ServerCtrl.LocalNetNode.id;
            value[1] = (byte)(ServerCtrl.LocalNetNode.id >> 8);
            value[2] = (byte)(ServerCtrl.LocalNetNode.id >> 16);
            value[3] = (byte)(ServerCtrl.LocalNetNode.id >> 24);
            return value;
        }
        private static byte[] OnServerInfo(UdpPoint point, IPEndPoint remote, RequestDataHandle handle)
        {
            byte[] value;

            List<byte> list = new List<byte>();
            byte[] name;
            if (ServerCtrl.HostInfo.name == null)
            {
                name = new byte[0];
            }
            else
            {
                name = Encoding.UTF8.GetBytes(ServerCtrl.HostInfo.name);
            }

            byte[] description;
            if (ServerCtrl.HostInfo.description == null)
            {
                description = new byte[0];
            }
            else
            {
                description = Encoding.UTF8.GetBytes(ServerCtrl.HostInfo.description);
            }
            list.AddRange(name.Length.ToBytes());
            list.AddRange(name);
            list.AddRange(description.Length.ToBytes());
            list.AddRange(description);
            value = list.ToArray();
            Console.WriteLine("[回复客户端]" + remote.Address + "[名称]" + name.Length + "[描述]" + description.Length);
            return value;
        }
        private static byte[] OnSyncInfo(UdpPoint point, IPEndPoint remote, RequestDataHandle handle)
        {
            byte[] value;
            byte[] previous;

            int serial = handle.data.ToInt(0);
            int license = handle.data.ToInt(4);
            int password = handle.data.ToInt(8);
            int delay = handle.data.ToInt(12);
            int length = handle.data.ToInt(16);
            List<string> message = new List<string>();
            List<string> nodeMessageList;
            int cursor = 20;
            for (int i = 0; i < length; i++)
            {
                message.Add(handle.data.ToStr(cursor));
                cursor += message[i].Length + 4;
            }
            NetNodeInfo info;
            if (NodeControl.Instance.NodeLicenseMapDic.TryGetValue(license, out info))
            {
                if (info.password == password)
                {
                    //第一次回复信息
                    if (!info.reply.TryGetValue(serial, out value))
                    {
                        //显示节点发出的消息
                        if (message.Count > 0)
                        {
                            string nodeMessage = "";
                            foreach (var m in message)
                            {
                                nodeMessage += "#" + info.name + ":" + m + "\n";
                            }
                            if (nodeMessage != "")
                            {
                                Console.Write(nodeMessage);
                            }
                        }
                        //修正延迟
                        info.delay = delay;
                        //设置最后同步时间
                        info.lastTime = ServerCtrl.TickTime;
                        //回复验证值结构[玩家ID][玩家验证码][玩家名字][延迟][消息数量][消息列表]
                        List<byte> list = new List<byte>();
                        int validPlayerCount = 0;
                        foreach (var item in NodeControl.Instance.NodeLicenseMapDic)
                        {
                            //跳过无效玩家
                            if (!item.Value.isValid) continue;
                            validPlayerCount += 1;
                            list.AddRange(item.Value.id.ToBytes());
                            list.AddRange(item.Value.license.ToBytes());
                            list.AddRange(item.Value.name.ToBytes());
                            list.AddRange(item.Value.delay.ToBytes());
                            //将来自玩家的消息则缓添加至回复信息内
                            if (info.message.TryGetValue(item.Value, out nodeMessageList))
                            {
                                //消息总量
                                list.AddRange(nodeMessageList.Count.ToBytes());
                                //具体消息
                                for (int i = 0; i < nodeMessageList.Count; i++)
                                {
                                    list.AddRange(nodeMessageList[i].ToBytes());
                                }
                                //移除该缓存
                                List<string> l;
                                info.message.TryRemove(item.Value, out l);
                            }
                            else
                            {
                                //无消息则写入全零
                                list.AddRange(0.ToBytes());
                            }
                            //为其他玩家添加消息缓存
                            if (item.Key != license)
                            {
                                if (item.Value.message.TryGetValue(info, out nodeMessageList))
                                {
                                    if (nodeMessageList == null) nodeMessageList = new List<string>();
                                    nodeMessageList.AddRange(message);
                                }
                                else
                                {
                                    nodeMessageList = new List<string>();
                                    nodeMessageList.AddRange(message);
                                    item.Value.message.TryAdd(info, nodeMessageList);
                                }
                            }
                        }

                        int[] removes = info.removes.ToArray();
                        list.AddRange(removes.ToBytes());
                        info.removes.Clear();

                        list.InsertRange(0, validPlayerCount.ToBytes());
                        value = list.ToArray();
                        //记录本次回复
                        info.reply.TryAdd(serial, value);
                        //尝试移除前一条信息
                        info.reply.TryRemove(serial - 1, out previous);
                        //Console.WriteLine("[验证值]" + handle.verify + "[首次序列]" + serial + "[验证有效]" + license + "[密码]" + password + "[消息数量]" + length + "[玩家数量]" + NodeControl.Instance.NodeLicenseMapDic.Count);
                        if (serial % 10 == 0)
                        {
                            //Console.WriteLine("[验证值]" + handle.verify + "[首次序列]" + serial + "[验证有效]" + license + "[密码]" + password + "[消息数量]" + length + "[玩家数量]" + NodeControl.Instance.NodeLicenseMapDic.Count);
                        }
                    }
                    else
                    {
                        Console.WriteLine("[验证值]" + handle.verify + "[重复序列]" + serial + "[验证有效]" + license + "[密码]" + password + "[消息数量]" + length + "[玩家数量]" + NodeControl.Instance.NodeLicenseMapDic.Count);
                    }
                }
                else
                {
                    Console.WriteLine("[验证失败]" + license + "[密码]" + password + "/" + info.password);
                    value = new byte[0];
                }
            }
            else
            {
                Console.WriteLine("[客户端验证值无效]" + license);
                value = new byte[0];
            }
            return value;
        }
        private static byte[] OnServerCancel(UdpPoint point, IPEndPoint remote, RequestDataHandle handle)
        {
            byte[] value;

            int license = handle.data.ToInt(0);
            int password = handle.data.ToInt(4);
            NetNodeInfo info;
            if (NodeControl.Instance.NodeLicenseMapDic.TryGetValue(license, out info))
            {
                if (info.password == password)
                {
                    NodeControl.Instance.RemoveNode(info);
                    Console.WriteLine("[客户端取消连接]" + license + "[密码]" + password);
                }
                else
                {
                    Console.WriteLine("[客户端取消失败]" + license + "[密码错误]" + password + "[正确密码]" + info.password);
                }
            }
            value = new byte[1];

            return value;
        }
        private static byte[] OnConnectTest(UdpPoint point, IPEndPoint remote, RequestDataHandle handle)
        {
            byte[] value;

            int rand = handle.data.ToInt(0);
            value = rand.ToBytes();

            //Console.WriteLine("[客户端连接测试]" + remote + "[随机值]" + rand);
            return value;
        }
        private static byte[] OnConnectInfo(UdpPoint point, IPEndPoint remote, RequestDataHandle handle)
        {
            if (handle.data == null || handle.data.Length == 0) return null ;
            byte[] value;
            //if(ServerCtrl.UniquenessNetNodeRandDic.TryGetValue())
            NetNodeInfo info = new NetNodeInfo();

            //获取玩家的识别码h
            int id = handle.data.ToInt(0);
            //获取玩家名称
            string name = handle.data.ToStr(4);
            //获取延迟
            int delay = handle.data.ToInt(name.Length + 8);
            //获取一个验证值
            int license = NodeControl.Instance.NodeLicenseRandom.Next();
            //获取一个密码值
            int password = ServerCtrl.random.Next();
            //为进行连接的玩家创建映射记录
            //Console.WriteLine("[名称]" + name + "[ID]" + id + "[许可]" + license + "[延迟]" + delay);
            info.id = id;
            info.isValid = false;
            info.license = license;
            info.remote = remote;
            info.name = name;
            info.password = password;
            info.delay = delay;
            info.lastTime = ServerCtrl.TickTime;
            if (info.message == null)
            {
                info.message = new ConcurrentDictionary<NetNodeInfo, List<string>>();
            }
            else
            {
                info.message.Clear();
            }
            if(info.reply == null)
            {
                info.reply = new ConcurrentDictionary<int, byte[]>();
            }
            else
            {
                info.reply.Clear();
            }
            info.removes = new ConcurrentStack<int>();
            //添加许可如果许可已经存在则直接覆盖
            NodeControl.Instance.NodeLicenseMapDic[license] = info;

            //回复验证值结构[玩家验证值][连接密码][主机验证值]
            List<byte> list = new List<byte>();
            list.AddRange(license.ToBytes());
            list.AddRange(password.ToBytes());
            list.AddRange(ServerCtrl.LocalNetNode.license.ToBytes());

            value = list.ToArray();

            Console.WriteLine("[客户端连接信息]" + remote + "[名称]" + name + "[许可]" + license);

            return value;
        }
        //玩家有效性确认
        private static byte[] OnConnectValid(UdpPoint point, IPEndPoint remote, RequestDataHandle handle)
        {
            byte[] value;

            int valid = handle.data.ToInt(0);
            int password = handle.data.ToInt(4);
            NetNodeInfo info;
            if (NodeControl.Instance.NodeLicenseMapDic.TryGetValue(valid, out info))
            {
                if (info.password == password)
                {
                    if (!info.isValid)
                    {
                        info.isValid = true;
                        Console.WriteLine("[连接确认]" + valid + "[密码]" + password);
                        //回复验证值结构[玩家验证值][连接密码][主机验证值]
                        List<byte> list = new List<byte>();
                        list.AddRange(valid.ToBytes());
                        list.AddRange(password.ToBytes());
                        list.AddRange(ServerCtrl.LocalNetNode.license.ToBytes());
                        list.AddRange(ServerCtrl.HostInfo.name.ToBytes());
                        list.AddRange(ServerCtrl.HostInfo.description.ToBytes());
                        value = list.ToArray();
                    }
                    else
                    {
                        Console.WriteLine("[重复确认]" + password + "[验证值]" + valid);
                        value = null;
                    }
                }
                else
                {
                    Console.WriteLine("[无效密码]" + password + "[验证值]" + valid);
                    value = new byte[0];
                }
            }
            else
            {
                Console.WriteLine("[无效确认]" + password + "[验证值]" + valid);
                value = new byte[0];
            }
            return value;
        }
        private static byte[] OnInitFrame(UdpPoint point, IPEndPoint remote, RequestDataHandle handle)
        {
            List<byte> buffer = new List<byte>();
            //CmdInfo info;
            //锁定获取封包的操作
            //lock (ServerCtrl.LastCmdInfoOperateLock)
            //{
            //    //获取封包信息
            //    info = ServerCtrl.LastCmdInfo;
            //}
            //buffer.AddRange(info.frame.ToBytes());
            //buffer.AddRange(info.cmd);
            Console.WriteLine("回复初始化信息");
            return buffer.ToArray();
        }
        //请求唯一值
        private static byte[] OnRequestCreateEntity(UdpPoint point, IPEndPoint remote, RequestDataHandle handle)
        {
            List<byte> buffer = new List<byte>();
            int rand;

            //Console.WriteLine("回复唯一实体随机值" + rand);
            int license = handle.data.ToInt(0);
            string id = handle.data.ToStr(4);
            if (NodeControl.Instance.NodeLicenseMapDic.ContainsKey(license) && EntityContent.Instance.EntityModelMemCacheDic.ContainsKey(id))
            {
                rand = ServerCtrl.EntityLicenseRandom.Next();
                //获取一个唯一随机值
                CreateEntityCmd cmd = CreateEntityCmd.Instance(rand, id, license, 0, 0);
                ServerCtrl.ReceiveCmdStack.Push(cmd);

                buffer.AddRange(rand.ToBytes());
            }
            else
            {
                Console.WriteLine("[节点许可无效或者实体识别无效]" + license + "," + id);
                return new byte[0];
            }

            return buffer.ToArray();
        }
        private static byte[] OnRequestInitData(UdpPoint point, IPEndPoint remote, RequestDataHandle handle)
        {
            List<byte> list = new List<byte>();
            int verify = handle.data.ToInt();
            int block = handle.data.ToInt(4);
            byte[] buffer;
            Dictionary<int, byte[]> bufferDic;
            if (NodeControl.Instance.InitDataCacheDic.TryGetValue(verify, out bufferDic))
            {
                if (bufferDic.TryGetValue(block, out buffer))
                {
                    list.AddRange(block.ToBytes());
                    list.AddRange(buffer.Length.ToBytes());
                    list.AddRange(buffer);
                }
            }
            return list.ToArray();
        }
        private static byte[] OnRequestInitStatus(UdpPoint point, IPEndPoint remote, RequestDataHandle handle)
        {
            List<byte> list = new List<byte>();
            BufferData data;
            int license = handle.data.ToInt(0);
            int password = handle.data.ToInt(4);
            NetNodeInfo info;
            if(NodeControl.Instance.NodeLicenseMapDic.TryGetValue(license,out info))
            {
                if (info.password == password)
                {
                    int dataLenght;
                    data = EntityContent.Instance.EntityDataCache;
                    byte[] buffer;
                    buffer = EntityContent.Instance.EntityDataByteCache;
                    if (buffer == null)
                    {
                        buffer = new BufferData().ToArray();
                    }
                    //创建内存流
                    MemoryStream stream = new MemoryStream(buffer);
                    dataLenght = (int)stream.Length;

                    Dictionary<int, byte[]> bufferDic = new Dictionary<int, byte[]>();
                    int length = 2048;
                    int block = 0;
                    int count = 0;
                    int offset = 0;
                    while (true)
                    {
                        //如果流的剩余长度超过单个区块的长度
                        if (stream.Length - offset - length >= 0)
                        {
                            count = length;
                        }
                        else
                        {
                            count = (int)(stream.Length - offset);
                        }
                        //检查流是否读取完毕
                        if (count <= 0) break;
                        buffer = new byte[count];
                        stream.Read(buffer, 0, count);

                        bufferDic.Add(block, buffer);

                        block += 1;
                        offset += count;
                    }
                    stream.Close();
                    //分割好数据并保存在缓存字典内
                    NodeControl.Instance.InitDataCacheDic[license] = bufferDic;
                    //数据一共有多长
                    list.AddRange(dataLenght.ToBytes());
                    //分成了几个区块
                    list.AddRange(block.ToBytes());
                    //每个区块的长度
                    list.AddRange(length.ToBytes());

                    Console.WriteLine("<<客户端请求初始化状态>>"+ license + "[密码]"+password + "[数据量]"+ dataLenght + "[运行]"+data.start+"/"+ServerCtrl.CurrentFrame +"[时间]"+data.time);
                    return list.ToArray();
                }
                else
                {
                    Console.WriteLine("<<客户端请求初始化状态>>" + license + "[密码错误]" + password + "[正确密码]"+ info.password);
                    return new byte[0];
                }
            }
            else
            {
                Console.WriteLine("<<客户端请求初始化状态>>" + license + "[许可错误]");
                return new byte[0];
            }
        }
        private static byte[] OnRequestInitFinish(UdpPoint point, IPEndPoint remote, RequestDataHandle handle)
        {
            int license = handle.data.ToInt();
            List<byte> list = new List<byte>();
            for (int i=0;i< NodeControl.Instance.InitDataCacheDic[license].Count; i++)
            {
                list.AddRange(NodeControl.Instance.InitDataCacheDic[license][i]);
            }
            BufferData data = list.ToArray().FromArray<BufferData>();
            NodeControl.Instance.InitDataCacheDic.Remove(license);
            Console.WriteLine("<<客户端成功接收初始化数据>>" + license);
            return new byte[0];
        }
    }
}
