using Lib.Net.UDP;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServerRuntimeCmd.Server.Main
{
    public static class ServerNetModel
    {
        public static ConcurrentUniquenessRand<UniquenessStruct> random = new ConcurrentUniquenessRand<UniquenessStruct>();
        private static int m_RendVerify;
        /// <summary>
        /// 获取一个不存在于请求字典中的随机值
        /// </summary>
        public static int SinglenessRand
        {
            get
            {
                Interlocked.Increment(ref m_RendVerify);
                int rand = new System.Random(System.Environment.TickCount + m_RendVerify).Next();
                int count = 100;
                //请求字典内不能有重复验证值
                while (SinglenessVerityMapDic.ContainsKey(rand))
                {
                    rand += 1;
                    if (count-- < 0)
                    {
                        Console.WriteLine("意外的重复");
                        break;
                    }
                }
                return rand;
            }
        }
        /// <summary>
        /// 唯一验证映射字典
        /// 记录和你在同一个房间的所有玩家验证值，通过此验证值可以识别玩家
        /// 每个玩家有一个对应的玩家识别码，一般情况下，这个识别码不会发生冲突
        /// </summary>
        private static int m_Rand;
        /// <summary>
        /// 获取一个随机值
        /// </summary>
        public static int Rand
        {
            get
            {
                Interlocked.Increment(ref m_Rand);
                int rand = new System.Random(System.Environment.TickCount + m_Rand).Next();
                return rand;
            }
        }

        public static Dictionary<int, NetNodeInfo> SinglenessVerityMapDic = new Dictionary<int, NetNodeInfo>();
        //本地节点信息
        public static NetNodeInfo LocalNetNode = new NetNodeInfo();

        /// <summary>
        /// 指令队列保存着客户端发来的历史信息，当客户端希望得到历史信息时可以从队列中获取，但是超过上限后将会移除最旧的信息
        /// </summary>
        public static ConcurrentQueue<CmdInfo> NodeCmdQueue = new ConcurrentQueue<CmdInfo>();
        public static ConcurrentStack<byte[]> NodeCmdBuffer = new ConcurrentStack<byte[]>();
        /// <summary>
        /// 最近一个指令封包,当客户端发出请求时，服务端会将该指令封包发送给客户端
        /// </summary>
        public static CmdInfo LastCmdInfo = new CmdInfo();
        /// <summary>
        /// 封包操作锁定
        /// </summary>
        public static object LastCmdInfoOperateLock = new object();

        /// <summary>
        /// 当前服务端执行的指令帧
        /// </summary>
        public static int CurrentFrame = 0;
        //启动时间计数器
        public static int StartTickTime = 0;
        //相对时间计数器
        public static int TickTime { get { return RealityTickTime - StartTickTime; } }
        //实际时间计数器
        public static int RealityTickTime { get { return System.Environment.TickCount; }}
        //最后一次更新的时间
        public static int LastTickTime = 0;
        //队列移除计数
        public static int NodeCmdQueueRemoveCount = 0;
        //缓存队列的队首帧
        public static int NodeCmdQueueFirstFrame = 0;

        //服务器信息
        public static ServerInfo HostInfo;
    }
}
