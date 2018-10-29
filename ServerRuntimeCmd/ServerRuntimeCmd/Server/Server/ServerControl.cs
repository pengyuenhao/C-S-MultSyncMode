using Helper.CmdMgr;
using Lib.Net.UDP;
using Runtime.Entity;
using Runtime.Main;
using Runtime.Net;
using Runtime.Node;
using Runtime.Res;
using Runtime.Time;
using Runtime.Util.Rand;
using Runtime.Util.Singleton;
using ServerRuntimeCmd.Main;
using ServerRuntimeCmd.Server.Main;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServerRuntimeCmd.Server.Server
{
    public class ServerControl
    {
        public static ServerControl Instance { get { return SingletonControl.GetInstance<ServerControl>(); } }

        ServerPropertieConfig Config { get { return ResControl.Instance.ServerConifg; } }
        /// <summary>
        /// 实体数据容器，所有实体数据都保存在这里
        /// </summary>
        EntityContent Content { get { return EntityContent.Instance; } }
        /// <summary>
        ///唯一实体随机数
        /// </summary>
        public NoRepeatRandom EntityLicenseRandom = new NoRepeatRandom();

        //public Dictionary<int, NetNodeInfo> NodeInfoDic = new Dictionary<int, NetNodeInfo>();
        bool m_isBroadcast = false;
        public bool IsBroadcast { get { return m_isBroadcast; } }

        public SimpleRandom random = new SimpleRandom();

        //本地节点信息
        public NetNodeInfo LocalNetNode = new NetNodeInfo();

        /// <summary>
        /// 指令队列保存着客户端发来的历史信息，当客户端希望得到历史信息时可以从队列中获取，但是超过上限后将会移除最旧的信息
        /// </summary>
        public ConcurrentQueue<CmdInfo> NodeCmdQueue = new ConcurrentQueue<CmdInfo>();
        public ConcurrentStack<byte[]> NodeCmdBuffer = new ConcurrentStack<byte[]>();

        public ConcurrentStack<BaseCmd> ReceiveCmdStack = new ConcurrentStack<BaseCmd>();
        /// <summary>
        /// 最近一个指令封包,当客户端发出请求时，服务端会将该指令封包发送给客户端
        /// </summary>
        //public CmdInfo LastCmdInfo = new CmdInfo();
        /// <summary>
        /// 封包操作锁定
        /// </summary>
        public object LastCmdInfoOperateLock = new object();

        /// <summary>
        /// 当前服务端执行的指令帧
        /// </summary>
        public int CurrentFrame = 0;
        //启动时间计数器
        public int StartTickTime = 0;
        //相对时间计数器
        public int TickTime { get { return TimeControl.TickTime; } }
        //实际时间计数器
        //public int RealityTickTime { get { return System.Environment.TickCount; } }
        //最后一次更新的时间
        //public int LastTickTime = 0;
        //队列移除计数
        public int NodeCmdQueueRemoveCount = 0;
        //缓存队列的队首帧
        public int NodeCmdQueueFirstFrame = 0;

        //服务器信息
        public ServerInfo HostInfo;

        public int MaxQueueBuffer;
        public int QueueBufferCount;

        UdpPoint Server;

        public void Init()
        {
            ServerCommandLine.Instance.Init();
            ResControl.Instance.Init();
            NodeControl.Instance.Init();
            EntityContent.Instance.Init();

            IPEndPoint point = UdpConfig.DnsToIPEndPoint(Config.primarilyIp);
            if (point == null)
            {
                Server = new UdpPoint(Config.primarilyPort, false);
            }
            else
            {
                Server = new UdpPoint(point, false);
            }

            m_isBroadcast = Config.isEnableBroadcast;
            //最大存储1000帧指令
            MaxQueueBuffer = 1000;
            //缓存队列计数
            QueueBufferCount = 0;
            //设置一个随机验证值
            LocalNetNode.license = random.Next();
            //新的服务器信息
            HostInfo = new ServerInfo();
            HostInfo.name = Config.name;
            HostInfo.description = Config.description;
            //服务端启动时间
            //StartTickTime = RealityTickTime;
            Console.WriteLine("[Server状态机执行初始化]" + IsBroadcast);

            Server.Mgr.OnRequestDataHandle += NetRequestData.OnRequestDataServer;
            Server.Mgr.OnCmdDataHandle += NetCmdData.OnCmdDataServer;
            Server.Mgr.OnReadBufferHandle += NetReadBuffer.OnReadBufferServer;
            Server.Mgr.OnRequestCmdHandle += NetRequestCmd.OnRequestCmdServer;

            //Console.WriteLine(Server.LocalEndPoint);
        }
        public void Update()
        {
            if (Server != null)
            {
                Server.Update();
            }
        }
        public void BroadcastServerInfo()
        {
            if (m_isBroadcast)
            {
                Server.Broadcast(InformHandle.Instance(Server.LocalEndPoint).ToBytes());
            }
        }
        /// <summary>
        /// 重构指令数据
        /// </summary>
        private void ReconstructionCmd(ref byte[] cmd)
        {
            string str = "";
            //当前执行的数组指令
            byte[] currentCmd;
            byte[] buffer;
            //当前执行的时刻值
            int currentTime;
            //当前执行的指令发出的玩家
            NetNodeInfo currentPlayer;
            //查看具体指令
            //str = "[>解码列表<]" + "[当前运行]" + ServerNetModel.CurrentFrame + "[缓存]" + ServerNetModel.NodeCmdQueue.Count + "[帧]";
            //读取指令过程
            for (int i = 0; i < cmd.Length; i++)
            {
                switch (CmdManager.ToCmd(cmd, ref i, out currentCmd))
                {
                    case CmdEnum.Heartbeat:
                        str += "<" + "[心跳]" + ">" + ",";
                        break;
                    case CmdEnum.BasetimeInform:
                        //BasetimeCmd basetime = new BasetimeCmd(currentCmd);
                        ////根据服务端发出的指令帧来进行本地更新,这里表示服务端已经完成了该基时帧的封包
                        //str += "<" + "[指令帧]" + basetime.frame + "[最大有效时间]" + basetime.maxValidTime + "[时间]" + basetime.time + "/" + ServerNetModel.TickTime +"[误差]" + (ServerNetModel.TickTime - basetime.time) + "[验证]" + basetime.verify + ">" + "\n";
                        break;
                    case CmdEnum.Sync:
                        //SyncCmd sync = new SyncCmd(currentCmd);
                        //str += "<" + "[服务端当前帧]" + sync.frame + "[同步时间]" + sync.time + ">" + "\n";
                        break;
                    case CmdEnum.Player:
                        //PlayerCmd playerCmd = new PlayerCmd(currentCmd);
                        //str += "<" + "[玩家]" + playerCmd.player.name + "[识别码]" + playerCmd.player.id + ">" + ",";
                        break;
                    case CmdEnum.Key:
                        KeyCmd keyCmd = new KeyCmd(currentCmd);
                        //str += "<" + "[指令帧]" + keyCmd.frame + "[按键]" + (keyCmd.isDown ? "<按下>" : "<释放>") + keyCmd.key + "[偏移]" + keyCmd.offsetTime + ">";
                        int l = i - currentCmd.Length + 1;
                        keyCmd.frame = CurrentFrame;
                        buffer = keyCmd.ToBytes();
                        for (int j = 0; j < buffer.Length; j++)
                        {
                            cmd[l + j] = buffer[j];
                        }
                        //str += "\n";
                        break;
                    case CmdEnum.Order:
                        OrderCmd orderCmd = new OrderCmd(currentCmd);
                        //str += "<" + "[指令帧]" + keyCmd.frame + "[按键]" + (keyCmd.isDown ? "<按下>" : "<释放>") + keyCmd.key + "[偏移]" + keyCmd.offsetTime + ">";
                        int o = i - currentCmd.Length + 1;
                        orderCmd.frame = CurrentFrame;
                        buffer = orderCmd.ToBytes();
                        for (int j = 0; j < buffer.Length; j++)
                        {
                            cmd[o + j] = buffer[j];
                        }
                        break;
                    case CmdEnum.CreateEntity:
                        //CreateEntityCmd createCmd = new CreateEntityCmd(currentCmd);
                        ////创建单位
                        //UniquenessEntity uniqueness;
                        //if(ServerNetModel.UniquenessEntityRandDic.TryGetValue(createCmd.index,out uniqueness))
                        //{
                        //    uniqueness.isUsed = true;
                        //    str += "<" + "[创建实体]" + createCmd.index + "[识别码]" + createCmd.id + "[创建者]" + createCmd.player + ">" + ",";
                        //}
                        //else
                        //{
                        //    str += "<" + "[创建实体无效]" + ">" + ",";
                        //}
                        break;
                    case CmdEnum.RequestCreateEntity:
                        //RequestCreateEntityCmd requestCmd = new RequestCreateEntityCmd(currentCmd);
                        break;
                    default:
                        //Loger.Log("未知指令");
                        str += "<" + "[未知]" + ">" + ",";
                        break;
                }
            }
            if (str != "")
            {
                //Console.WriteLine(str);
            }
        }
        /// <summary>
        /// 检查节点验证字典
        /// </summary>
        public void CheckNodeVerityMapDic()
        {
            //记录当前时间
            NetNodeInfo info;
            LocalNetNode.lastTime = TickTime;
            List<NetNodeInfo> removeList = new List<NetNodeInfo>();
            List<NetNodeInfo> createList = new List<NetNodeInfo>();
            //遍历服务当前连接的节点
            foreach (var item in NodeControl.Instance.NodeLicenseMapDic)
            {
                //设置超时10000毫秒强制移除玩家,不会主动移除本机玩家
                if (LocalNetNode.license != item.Value.license && TickTime - item.Value.lastTime > 10000)
                {
                    removeList.Add(item.Value);
                    Console.WriteLine("[等待超时]" + item.Value.name);
                }
            }
            foreach (var item in removeList)
            {
                Console.WriteLine("[强制移除]" + item.name + "," + item.remote + "," + item.isValid);
                //释放被占用的值
                NodeControl.Instance.RemoveNode(item);
            }
        }
        public void CheckReceiveCmdStack()
        {
            BaseCmd cmd;
            while (ReceiveCmdStack.TryPop(out cmd))
            {
                byte[] buffer;
                switch (cmd.type)
                {
                    case CmdEnum.Key:
                        KeyCmd key = (KeyCmd)cmd;
                        key.frame = CurrentFrame;
                        buffer = key.ToBytes();
                        break;
                    case CmdEnum.Order:
                        OrderCmd order = (OrderCmd)cmd;
                        order.frame = CurrentFrame;
                        buffer = order.ToBytes();
                        break;
                    case CmdEnum.CreateEntity:
                        CreateEntityCmd create = (CreateEntityCmd)cmd;
                        create.frame = CurrentFrame;
                        buffer = create.ToBytes();
                        Console.WriteLine("[服务端同意创建]" + create.player + "[识别]" + create.id + "[索引]" + create.index + "[当前帧]"+ CurrentFrame + "[时间]"+ TickTime);
                        break;
                    default:
                        buffer = cmd.ToBytes();
                        break;
                }
                NodeCmdBuffer.Push(buffer);
            }
        }
        /// <summary>
        /// 收集客户端的指令数据并压入队列内
        /// </summary>
        public void CollectCmdServer()
        {
            //用于临时存储指令数据的列表
            List<byte> list = new List<byte>();
            //将基时指令包写入列表
            list.AddRange(BasetimeCmd.Instance(LocalNetNode.license, TickTime, 1000, CurrentFrame).ToBytes());
            //list.AddRange(SyncCmd.Instance(ServerNetModel.TickTime,ServerNetModel.CurrentFrame).ToBytes());

            //用于临时存储缓存栈数据
            byte[] result;
            //尝试将指令集缓存栈清空
            while (NodeCmdBuffer.TryPop(out result))
            {
                //重构指令
                ReconstructionCmd(ref result);
                //将数据加入列表
                list.AddRange(result);
            }
            //创建指令封包
            CmdInfo info = new CmdInfo();
            //写入指令封包数据
            info.cmd = list.ToArray();
            info.frame = CurrentFrame;
            info.time = TickTime;
            //记录最后一次封包行为，在锁定内进行操作，防止其他线程获取错误的信息
            //lock (LastCmdInfoOperateLock)
            //{
            //    LastCmdInfo = info;
            //}
            //将指令封包加入队列此时当前服务端的一帧已经结束并保存在缓存内
            NodeCmdQueue.Enqueue(info);
            QueueBufferCount += 1;
            //本地直接运行指令
            LocalRunFrameCmd(info.cmd);
            //记录服务端最后一次更新的时间计数
            //LastTickTime = info.time;
            //记录当前服务端的指令帧
            CurrentFrame += 1;
        }
        public void RemoveOverMaxQueue()
        {
            CmdInfo info;
            //检测队列缓存，超过缓存上限后移除队首的值
            if (NodeCmdQueue.Count >= MaxQueueBuffer)
            {
                //尝试移除队首
                if (NodeCmdQueue.TryDequeue(out info))
                {
                    NodeCmdQueueRemoveCount += 1;
                    //移除队首后第二位成为新的队首
                    NodeCmdQueueFirstFrame = info.frame + 1;
                }
                else
                {
                    Console.WriteLine("移除队首失败");
                }
                //Console.WriteLine("移除第"+info.current + "帧，于" + info.time +"时封包");
            }
        }
        public void LocalRunFrameCmd(byte[] cmd)
        {
            //直接存入指令封包
            ControlModel.FrameCtrl.FrameByteBufferEnqueue(cmd);
        }
        
    }
}
