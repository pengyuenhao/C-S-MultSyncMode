#define DEBUG
//using UnityEngine;
using System.Collections;
using System;
using System.Net.Sockets;
using System.Threading;
using Lib.Net.UDP;
using System.Net;
using System.Collections.Generic;
using System.Collections.Concurrent;


namespace Lib.Net.UDP
{
    //负责以数组形式收发数据
    public class UdpPoint : IDisposable
    {
        private static int TickTime { get { return System.Environment.TickCount; } }
        //private static TimeManager TimeMgr { get { return TimeManager.Instance; } }
        /// <summary>
        /// 监听管理器
        /// </summary>
        public HandleManager Mgr;
        //等待超时设为5000毫秒
        private const int OVERTIME = 5000;
        private const int FRAME = 10;
        private UdpClient Client;
        private IPEndPoint m_localEndPoint;
        public IPEndPoint LocalEndPoint { get { return m_localEndPoint; } }
        private IAsyncResult Iar;
        private bool isClose;
        private Thread udpThread;
        private Thread udpReceiveThread;
        private int ThreadOvertime = 0;


        //应答缓存
        //private List<byte> ReplyList;
        /*********************************注意多线程冲突*****************************************
         * 这里有可能会出现多线程竞争，例如主线程正在创建新的请求，而异步接收数据包时调用了字典
         */
        //请求字典(本机发出的请求)
        internal ConcurrentDictionary<int, Request> RequestDic;
        //回复字典(本机接收的请求)
        //internal ConcurrentDictionary<int, int> RetryDic;
        //各个地址的网络状况字典
        internal ConcurrentDictionary<IPEndPoint, Netstat> NetstatDic;
        //网络任务字典
        private ConcurrentDictionary<IPEndPoint, NetTask> NetTaskDic;
        //请求发送列表字典，完成发送后会清空
        //internal ConcurrentDictionary<IPEndPoint,List<Request>> RequestSendDic;
        //指令集束字典，完成发送后会清空
        //internal ConcurrentDictionary<IPEndPoint, List<BaseHandle>> HandleSendDic;
        /* 
         * 接收到的请求队列
         * 按来源地址存储所有接收到的请求
         */
        private static ConcurrentQueue<UdpQueueStruct> ReceiveQueue = new ConcurrentQueue<UdpQueueStruct>();
        //队列结构体
        private struct UdpQueueStruct
        {
            public byte[] buffer;
            public UdpPoint point;
            public IPEndPoint remote;

            public UdpQueueStruct(byte[] buffer, UdpPoint point, IPEndPoint remote)
            {
                this.buffer = buffer;
                this.point = point;
                this.remote = remote;
            }
        }
        /**************************************************************************************/
        private int m_RequestVerify;
        /// <summary>
        /// 获取一个不存在于请求字典中的随机值
        /// </summary>
        public int RandVerify
        {
            get
            {
                Interlocked.Increment(ref m_RequestVerify);
                int rand = new System.Random(TickTime + m_RequestVerify).Next();
                int count = 10;
                //请求字典内不能有重复验证值
                while (RequestDic.ContainsKey(rand))
                {
                    rand += 1;
                    if (count-- < 0)
                    {
//#if Debug
//                        Loger.LogError("意外的重复");
//#endif
                        break;
                    }
                }
                return rand;
            }
        }

        //public void ReplyCollect(byte[] value)
        //{
        //    ReplyList.AddRange(value);
        //}
        //public void ReplySend(IPEndPoint remote)
        //{
        //    if (ReplyList.Count > 0)
        //    {
        //        Debug.Log("[回复]" + LocalEndPoint + "[总数]" + ReplyList.Count + "[目标]" + remote);
        //        SendTo(ReplyList.ToArray(), remote);
        //        ReplyList.Clear();
        //    }
        //}
        private ManualResetEvent receiveDone = null;
        public UdpPoint(int port = 0,bool isThread = true)
        {
            receiveDone = new ManualResetEvent(false);
            isClose = false;
            Client = new UdpClient(port);
            //Client.MulticastLoopback = true;
            m_localEndPoint = new IPEndPoint(UdpConfig.LocalIP, ((IPEndPoint)Client.Client.LocalEndPoint).Port);
            Iar = Client.BeginReceive(ReceiveCallback, this);
            //ReplyList = new List<byte>();

            //RetryDic = new Dictionary<IPEndPoint, List<byte>>();
            //RequestSendDic = new ConcurrentDictionary<IPEndPoint, List<Request>>();
            //HandleSendDic = new ConcurrentDictionary<IPEndPoint, List<BaseHandle>>();
            NetTaskDic = new ConcurrentDictionary<IPEndPoint, NetTask>();
            RequestDic = new ConcurrentDictionary<int, Request>();
            NetstatDic = new ConcurrentDictionary<IPEndPoint, Netstat>();
            //RetryDic = new ConcurrentDictionary<int, int>();

            Mgr = new HandleManager(this);
            udpThread = new Thread(UdpThread);
            if (isThread)
            {
                udpThread.Start();
            }
            udpReceiveThread = new Thread(UdpReceiveThread);
            Console.WriteLine("UDP线程启动");
        }
        public void Update()
        {
            Run();
        }

        //UDP专用线程，用于处理接收队列
        private void UdpThread()
        {
            //设置最大工作线程为10条，最大排队线程为100条
            ThreadPool.SetMaxThreads(10, 100);

            ThreadOvertime = 0;
            while (!isClose)
            {
                Run();
                Thread.Sleep(FRAME);
            }
            //Debug.Log("结束进程");
        }
        private void Run()
        {
            /*队列处理*/
            int work;
            int queue;
            //获取所有接收队列
            UdpQueueStruct udpQueue;
            while (ReceiveQueue.TryDequeue(out udpQueue))
            {
                //将队列中的数据取出放入线程池内处理
                ThreadPool.QueueUserWorkItem(new WaitCallback(QueueCallback), udpQueue);
                ThreadPool.GetAvailableThreads(out work, out queue);
                if (work >= 10) break;
            }
            /*队列处理*/


            foreach (var item in RequestDic)
            {
                //Request item.Value = item.Value;
                //对于还未发送成功的请求直接跳过,除非超过1秒还未发送
                if (!item.Value.isSend && item.Value.overtime > 0)
                {
                    item.Value.overtime -= 1;
                    continue;
                }
                Netstat netstat = NetstatDic[item.Value.remote];
                int wait = (int)((3 * netstat.AverageDelay + 50 * item.Value.retry + 100) * (1f + (float)item.Value.retry * 0.5f)) - 0;
                //Debug.Log("@"+request.GetHashCode()+"等待时间" + wait + "延迟" + netstat.AverageDelay + "重发" + request.retry);

                //基础等待时间等于延迟的两倍
                //每次重连都会延长一半等待时间
                if (TickTime - item.Value.timeStart < OVERTIME)
                {
                    if (TickTime - item.Value.timeSend > wait)
                    {
                        //continue;
                        //丢包重发机制
                        item.Value.retry += 1;
                        //数据包重发时间
                        item.Value.timeSend = TickTime; ;
                        BunchTaskAdd(item.Value.remote, item.Value.handle);
                        //List<byte> buffer = RetryDic.GetOrAdd(item.Value.remote, new List<byte>());
                        //buffer.AddRange(item.Value.value);
                        //#if Debug
                        //                            Loger.LogError("[重发]"+item.Value.verify + "[等待超时]" + wait);
                        //#endif
                    }
                    else
                    {
                        //Debug.Log("等待时间" + wait + "延迟" + netstat.AverageDelay+ "重发"+ request.retry);
                    }
                }
                else
                {
                    Request request;
                    if (RequestDic.TryRemove(item.Value.verify, out request))
                    {
                        //增加一次丢包
                        netstat.loss += 1;
                        //发布请求失败事件
                        Mgr.DisposeRequestFailure(item.Value);
                        request.failed(item.Value);
                        //#if Debug
                        //                           Loger.LogError("[数据丢包]" + item.Value.verify);
                        //#endif
                    }
                    else
                    {
                        //#if Debug
                        //                           Loger.LogError("[临界回复]"+item.Value.verify);
                        //#endif
                    }
                }
            }
            //定时合并发送
            BunchSend();
            if (RequestDic.Count > 0)
            {
                ThreadOvertime += 1;
            }
            else
            {
                ThreadOvertime = 0;
            }
            if (ThreadOvertime >= 1000 / FRAME)
            {
                ThreadOvertime = 0;
                //#if Debug
                //                   Loger.LogError("[未完成的请求]" + RequestDic.Count);
                //#endif
            }
        }
        private static void QueueCallback(Object stateInfo)
        {
            UdpQueueStruct udpQueue = (UdpQueueStruct)stateInfo;
            udpQueue.point.Mgr.Dispose(udpQueue.remote, udpQueue.buffer);
            //Console.WriteLine("处理队列"+udpQueue.remote + udpQueue.buffer.ToDetail());
        }
        //接收回调函数，执行尽量简短的命令，应该使用一个队列来处理，这里只进行收集工作
        private void ReceiveCallback(IAsyncResult iar)
        {
            byte[] buffer = null;
            IPEndPoint remote;
            UdpPoint point = iar.AsyncState as UdpPoint;
            if (iar.IsCompleted)
            {
                remote = null;
                try
                {
                    buffer = point.Client.EndReceive(iar, ref remote);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
                finally
                {
                    //重新开始接收
                    if (!point.isClose)
                    {
                        //Debug.Log("[监听]" + point.LocalEndPoint + "[来源]" + remote + "[时刻]"+TimeMgr.TickTime);
                        //point.Iar = point.Client.BeginReceive(ReceiveCallback, point);
                        //加入请求队列
                        if(buffer!=null)ReceiveQueue.Enqueue(new UdpQueueStruct(buffer, point, remote));
                        //HandleManager.OutStr += "接收数据" + remote + "长度"+buffer.Length+"\n";
                        Client.BeginReceive(ReceiveCallback, this);
                    }
                    else
                    {
                        point.Iar = null;
                        //Debug.Log("结束监听" + point.Client.Client.LocalEndPoint);
                    }
                }
                return;
            }
        }

        //专用接收线程
        private void UdpReceiveThread()
        {
            //while (true)
            //{
            //    Client.r
            //}
        }
        /// <summary>
        /// 一整个列表的请求发送回调函数
        /// </summary>
        private static void SendListCallback(IAsyncResult iar)
        {
            List<Request> list = iar.AsyncState as List<Request>;
            if (list == null) return;
            if (iar.IsCompleted)
            {
                foreach(var item in list)
                {
                    //Debug.Log("[发送成功]" + item.verify + "[时刻]" + TimeMgr.TickTime);
                    //数据包真实发出时间
                    item.timeSend = TickTime; ;
                    item.isSend = true;
                    //Console.WriteLine("发送数据至" + item.remote + "类型" + item.handle);
                    //HandleManager.OutStr += "合并发送"+item.verify+"数据至" + item.remote + "类型" + item.handle + "\n";
                }
            }
        }
        /// <summary>
        /// 单个请求的发送回调函数
        /// </summary>
        private static void SendCallback(IAsyncResult iar)
        {
            Request request = iar.AsyncState as Request;
            if (request == null) return;
            if (iar.IsCompleted)
            {
                request.timeSend = TickTime; ;
                request.isSend = true;
                //HandleManager.OutStr += "单独发送"+request.verify+"数据至" + request.remote + "类型" + request.handle + "\n";
            }
        }
        /// <summary>
        /// 立刻发送数据到指定网络地址
        /// </summary>
        public void SendTo(byte[] buffer, IPEndPoint remote)
        {
            //SendTo(buffer, remote, null);
            Client.BeginSend(buffer, buffer.Length, null, null);
            //Console.WriteLine("发送非请求至" + remote);
            //Client.Send(buffer, buffer.Length, remote);
        }
        public void SendTo(byte[] buffer, IPEndPoint remote, List<Request> list)
        {
            Client.BeginSend(buffer, buffer.Length, remote, SendListCallback, list);
            //Console.WriteLine("发送请求至" + remote);
        }
        public void SendTo(byte[] buffer, IPEndPoint remote, Request request)
        {
            Client.BeginSend(buffer, buffer.Length, remote, SendCallback, request);
            //Console.WriteLine("发送请求至" + remote);
        }
        //private void BeginSend(byte[] datagram, int bytes, IPEndPoint endPoint, AsyncCallback requestCallback, object state)
        //{
        //    //将队列中的数据取出放入线程池内处理
        //    ThreadPool.QueueUserWorkItem(new WaitCallback(QueueCallback), udpQueue);
        //}
        //private 
        /// <summary>
        /// 广播到固定的端口,如果有多个可用端口则每次都会更换一个端口
        /// </summary>
        public void Broadcast(byte[] buffer)
        {
            Client.BeginSend(buffer, buffer.Length, UdpConfig.Broadcast, SendListCallback, this);
            //Console.WriteLine("[广播]" + UdpConfig.Broadcast);
        }
        /// <summary>
        /// 获取指定地址的网络状况
        /// </summary>
        public Netstat GetNetstat(IPEndPoint remote)
        {
            Netstat netstat;
            if (NetstatDic.TryGetValue(remote, out netstat))
            {
                return netstat;
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// 请求读取远端数据,返回一条操作
        /// 需要指定读取的数据缓存区和远端网络地址
        /// 返回的数据会缓存在回调函数的参数里
        /// </summary>
        public void SingleRequestBuffer(BufferEnum read, IPEndPoint remote, Action<Request, byte[]> succeed, Action<Request> failed)
        {
            ReadBufferHandle handle = ReadBufferHandle.Instance(read, RandVerify);
            Request request = new Request(handle, remote, succeed, failed);
            request.isSend = false;
            request.overtime = 1000 / FRAME;
            request.timeSend = TickTime;
            request.timeStart = TickTime;
            request.verify = handle.verify;

            //添加请求列表
            if (RequestDic.TryAdd(handle.verify, request))
            {
                Netstat netstat = NetstatDic.GetOrAdd(remote, new Netstat());
                netstat.request += 1;
                //注册完成后直接发送
                SendTo(handle.ToBytes(), remote, request);
                //Debug.Log("[注册验证]" + request.verify + "[验证总数]" + RequestDic.Count);
            }
            //Console.WriteLine("单独发送数据请求"+remote + "[ReadBufferHandle]");
            //HandleManager.OutStr += "单独发送" + handle.verify + "数据请求至" + remote + "[ReadBufferHandle]" + "\n";
        }
        //打包请求读取远端数据
        public void BunchRequestBuffer(BufferEnum read, IPEndPoint remote, Action<Request, byte[]> succeed, Action<Request> failed)
        {
            ReadBufferHandle handle = ReadBufferHandle.Instance(read, RandVerify);
            //监听请求
            BunchRequestAdd(handle, remote, succeed, failed);
            //Debug.Log("发送请求");
            BunchSend();
        }
        public void SingleRequestCmd(int startFrame,int frameLength ,IPEndPoint remote, Action<Request, byte[]> succeed, Action<Request> failed)
        {
            RequestCmdHandle handle = RequestCmdHandle.Instance(startFrame, frameLength, RandVerify);
            Request request = new Request(handle, remote, succeed, failed);
            request.isSend = false;
            request.overtime = 1000 / FRAME;
            request.timeSend = TickTime;
            request.timeStart = TickTime;
            request.verify = handle.verify;

            //添加请求列表
            if (RequestDic.TryAdd(handle.verify, request))
            {
                Netstat netstat = NetstatDic.GetOrAdd(remote, new Netstat());
                netstat.request += 1;
                //注册完成后直接发送
                SendTo(handle.ToBytes(), remote, request);
                //Debug.Log("[注册验证]" + request.verify + "[验证总数]" + RequestDic.Count);
            }
            //Console.WriteLine("单独发送数据请求" + remote + "[RequestCmdHandle]");
            //HandleManager.OutStr += "单独发送" + handle.verify + "数据请求至" + remote + "[RequestCmdHandle]" + "\n";
        }
        //打包请求命令帧
        public void BunchRequestCmd(int startFrame, int frameLength, IPEndPoint remote, Action<Request, byte[]> succeed, Action<Request> failed)
        {
            RequestCmdHandle handle = RequestCmdHandle.Instance(startFrame,frameLength, RandVerify);
            //监听请求
            BunchRequestAdd(handle, remote, succeed, failed);
            //Debug.Log("发送请求");
            BunchSend();
        }
        /// <summary>
        /// 单独请求服务端数据,成功或失败都会回调函数
        /// </summary>
        public void SingleRequestData(RequestEnum requests, IPEndPoint remote, Action<Request, byte[]> succeed, Action<Request> failed,byte[] data = null)
        {
            RequestDataHandle handle = RequestDataHandle.Instance(requests, RandVerify, data);
            Request request = new Request(handle, remote, succeed, failed);
            request.isSend = false;
            request.overtime = 1000 / FRAME;
            request.timeSend = TickTime;
            request.timeStart = TickTime;
            request.verify = handle.verify;

            //添加请求列表
            if (RequestDic.TryAdd(handle.verify, request))
            {
                Netstat netstat = NetstatDic.GetOrAdd(remote, new Netstat());
                netstat.request += 1;
                //注册完成后直接发送
                SendTo(handle.ToBytes(), remote, request);
                //Debug.Log("[注册验证]" + request.verify + "[验证总数]" + RequestDic.Count);
            }
            //Console.WriteLine("单独发送数据请求" + remote + "[RequestDataHandle]");
            //HandleManager.OutStr += "单独发送"+handle.verify+"数据请求至" + remote + "[RequestDataHandle]" + "\n";
        }
        public void BunchRequestData(RequestEnum request, IPEndPoint remote, Action<Request, byte[]> succeed, Action<Request> failed, byte[] data = null)
        {
            RequestDataHandle handle = RequestDataHandle.Instance(request, RandVerify, data);
            //监听请求
            BunchRequestAdd(handle, remote, succeed, failed);
            BunchSend();
        }
        /// <summary>
        /// 复合指令集束添加请求
        /// </summary>
        public void BunchRequestAdd(RequestHandle handle, IPEndPoint remote, Action<Request, byte[]> succeed, Action<Request> failed)
        {
            //int time = TimeMgr.TickTime;
            //新建请求
            Request request = new Request(handle, remote, succeed, failed);
            request.isSend = false;
            request.overtime = 1000/FRAME;
            request.timeSend = TickTime;
            request.timeStart = TickTime;
            request.verify = handle.verify;

            //添加请求列表
            if (RequestDic.TryAdd(handle.verify, request))
            {
                Netstat netstat = NetstatDic.GetOrAdd(remote, new Netstat());
                netstat.request += 1;
                //添加集束任务
                BunchTaskAdd(remote, request);
                //Debug.Log("[注册验证]" + request.verify + "[验证总数]" + RequestDic.Count);
            }
            else
            {
#if Debug
                Loger.LogError("[注册失败]" + handle.verify);
#endif
            }
            //time = TimeMgr.TickTime - time;
            //Debug.Log("耗时" + time);
        }
        /// <summary>
        /// 复合指令集束添加指令
        /// </summary>
        public void BunchTaskAdd(IPEndPoint remote, BaseHandle handle)
        {
            NetTaskDic.AddOrUpdate(remote,
                r =>
                {
                    NetTask task = new NetTask();
                    task.handles.Add(handle);
                    return task;
                },
                (r, task) =>
                {
                    task.handles.Add(handle);
                    return task;
                });
        }
        public void BunchTaskAdd(IPEndPoint remote, Request request)
        {
            //多线程安全
            NetTaskDic.AddOrUpdate(remote,
                (r) =>
                {
                    NetTask task = new NetTask();
                    task.requests.Add(request);
                    task.handles.Add(request.handle);
                    return task;
                },
                (r, task) =>
                {
                    task.requests.Add(request);
                    task.handles.Add(request.handle);
                    return task;
                });
        }
        /// <summary>
        /// 复合指令集束合并发送
        /// </summary>
        public void BunchSend()
        {
            if (NetTaskDic.Count <= 0) return;
            //int time = TimeMgr.TickTime;
            NetTask task;
            List<byte> list = new List<byte>();
            //多线程支持
            var remotes = NetTaskDic.Keys;
            foreach(var item in remotes)
            {
                //Debug.Log("[集束发送]" + HandleSendDic.Count);
                if (NetTaskDic.Count > 10||true)
                {
                    if (NetTaskDic.TryRemove(item, out task))
                    {
                        for (int i = 0; i < task.handles.Count; i++)
                        {
                            list.AddRange(task.handles[i].ToBytes());
                        }
                        SendTo(list.ToArray(), item, task.requests);
                    }
                }
                else
                { 
                    NetTaskDic.AddOrUpdate(item, new NetTask(), (r, t) =>
                    {
                        for (int i = 0; i < t.handles.Count; i++)
                        {
                            list.AddRange(t.handles[i].ToBytes());
                        }
                        SendTo(list.ToArray(), item, t.requests);
                        t.handles.Clear();
                        t.requests = new List<Request>();
                        return t;
                    });
                }
            }
            //time = TimeMgr.TickTime - time;
            //Debug.Log("耗时" + time);
        }
#region 释放资源
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                isClose = true;
                new Thread(() =>
                {
                //等待进程关闭
                Thread.Sleep(1000);
                //关闭UDP执行
                while (Iar != null && !Iar.IsCompleted)
                    {
                    //Debug.Log("等待资源释放" + Iar.ToString() + "完成" + Iar.IsCompleted + "本地" + LocalEndPoint);
                    Client.BeginSend(new byte[] { 0xFF }, 1, LocalEndPoint, null, this);
                        Thread.Sleep(10);
                    }
                    FreeResources();
                }).Start();
            }
        }
        private void FreeResources()
        {
            //Debug.Log("释放资源");
            udpThread.Abort();
            udpThread = null;
            Client.Close();
            Client = null;
            //RetryDic.Clear();
            //ReplyList.Clear();
            //RequestSendDic.Clear();
            //HandleSendDic.Clear();

            RequestDic.Clear();
            NetstatDic.Clear();
            NetTaskDic.Clear();
            Mgr.Dispose();
            //RetryDic = null;
            //ReplyList = null;
            RequestDic = null;
            NetstatDic = null;
            NetTaskDic = null;
            //HandleSendDic = null;
            //RequestSendDic = null;
            GC.SuppressFinalize(this);
        }
        //~UdpPoint() { Debug.Log("绝对释放"); }
        public void Dispose()
        {
            Dispose(true);
        }
#endregion
    }
}