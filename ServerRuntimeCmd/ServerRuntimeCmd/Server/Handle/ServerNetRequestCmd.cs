using Helper.CmdMgr;
using Lib.Net.UDP;
using Runtime.Entity;
using Runtime.Net;
using ServerRuntimeCmd.Server.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ServerRuntimeCmd.Server
{

    public static class NetRequestCmd
    {
        static ServerControl ServerCtrl { get { return ServerControl.Instance; } }

        //接收到客户端发出的指令请求数据包时
        public static void OnRequestCmdServer(UdpPoint point, IPEndPoint remote, RequestCmdHandle handle)
        {
            //回复客户端的句柄
            ReplyCmdHandle replyCmdHandle;
            //缓存
            List<byte> buffer = new List<byte>();
            //每经过10帧将指令帧同步包写入列表
            //if (ServerNetModel.QueueBufferCount % 3 == 0)
            //{
            //}
            //指令同步包用于通知客户端上传当前的重要信息(单位数据，地图数据，配置等)
            buffer.AddRange(SyncCmd.Instance(ServerCtrl.TickTime, ServerCtrl.CurrentFrame).ToBytes());
            //Console.WriteLine("[同步客户端]" + ServerCtrl.TickTime + "[当前运行]" + ServerCtrl.CurrentFrame);
            //客户端请求的命令帧
            int clientRequestStartFrame = handle.startFrame;
            //客户端请求的帧长度
            int clientRequestFrameLength = handle.frameLength;
            //差值
            int diff;
            //缓存队列的长度计数
            int bufferQueueCount = ServerCtrl.NodeCmdQueue.Count;
            //缓存队列的首帧
            int bufferQueueFirstFrame;
            //拷贝队列副本
            CmdInfo[] queue = ServerCtrl.NodeCmdQueue.ToArray();
            //服务端无法回复请求
            if (clientRequestStartFrame == ServerCtrl.CurrentFrame)
            {
                buffer.AddRange(BasetimeFailedCmd.Instance(clientRequestStartFrame, clientRequestFrameLength).ToBytes());
                //Console.WriteLine("无法回复命令帧" + clientRequestStartFrame + "长度" + clientRequestFrameLength);
            }
            //如果指令缓存副本有效
            if (queue != null && queue.Length != 0)
            {
                bufferQueueFirstFrame = queue[0].frame;
                //客户端请求的帧与缓存队列的差值
                diff = clientRequestStartFrame - bufferQueueFirstFrame;
                //Console.WriteLine("客户端请求" + clientRequestStartFrame + "队首" + bufferQueueFirstFrame);
                //如果客户端请求的帧比队列缓存队首的帧更小
                if (diff < 0)
                {
                    //长度缩小至减去超出范围的部分
                    clientRequestFrameLength += diff;
                    //从队首开始
                    diff = 0;
                }
                //如果客户端请求的帧长度大于0表示缓存内存在客户端的需求
                if (clientRequestFrameLength > 0)
                {
                    //如果客户端请求的帧长度超过了缓存队列的长度
                    //if (clientRequestFrameLength + diff > bufferQueueCount)
                    //{
                    //    //设置请求对列的长度为缓存队列的最大长度
                    //    clientRequestFrameLength = bufferQueueCount - diff;
                    //}
                    int k = 0;
                    int tmp = clientRequestFrameLength;
                    //从客户端希望获取的帧开始
                    for (int i = diff; i < bufferQueueCount; i++)
                    {
                        
                        //获取到足够长的数据后取消
                        if (tmp-- <= 0) break;
                        //将客户端请求的指令数据加入缓存
                        buffer.AddRange(queue[i].cmd);
                        k++;
                    }
                    //Console.WriteLine("当前"+ServerNetModel.CurrentFrame+"回复客户端" + clientRequestStartFrame + "长度" + clientRequestFrameLength + "实际" + k);
                }
            }
            else
            {
                //如果客户端请求的不是最新数据
                if (clientRequestFrameLength != -1)
                {
                    //缓存无效无法回复客户端
                    clientRequestFrameLength = 0;
                }
            }
            //如果长度为-1则表示获取强制更新包
            if (clientRequestFrameLength == -1)
            {
                //存储最后一次封包信息的临时变量
                //CmdInfo info;
                //锁定获取封包的操作
                //lock (ServerCtrl.LastCmdInfoOperateLock)
                //{
                //    //获取封包信息
                //    info = ServerCtrl.LastCmdInfo;
                //}
                //buffer.AddRange(info.cmd);
                replyCmdHandle = ReplyCmdHandle.Instance(ServerCtrl.CurrentFrame, handle.verify, buffer.ToArray());
            }
            else if (clientRequestFrameLength > 0)
            {
                //将客户端请求的所有指令帧打包回复
                replyCmdHandle = ReplyCmdHandle.Instance(ServerCtrl.CurrentFrame, handle.verify, buffer.ToArray());
            }
            else
            {
                replyCmdHandle = ReplyCmdHandle.Instance(ServerCtrl.CurrentFrame, handle.verify, buffer.ToArray());
            }
            point.BunchTaskAdd(remote, replyCmdHandle);
            //发送一条基准时间包回复客户端
            point.BunchSend();
            //Console.WriteLine("客户端请求命令帧" + clientRequestStartFrame + "长度" + clientRequestFrameLength + "当前运行帧" + ServerCtrl.CurrentFrame + "验证值" + replyCmdHandle.verify + "数据量" + replyCmdHandle.Length);
        }
    }
}
