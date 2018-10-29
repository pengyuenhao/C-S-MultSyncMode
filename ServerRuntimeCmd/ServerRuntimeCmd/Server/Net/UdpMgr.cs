using System.Collections;
using System.Collections.Generic;
using System;
using System.Reflection;
using System.Net.Sockets;
using System.Security.Permissions;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;
using System.Threading;

namespace Lib.Net.UDP
{
    #region 操作管理
    public class HandleManager : IDisposable
    {
        //public static string OutStr = "";

        private static int TickTime { get { return System.Environment.TickCount; } }
        //private static TimeManager TimeMgr { get { return TimeManager.Instance; } }
        #region 操作事件处理
        //请求失败事件
        public event Action<UdpPoint, IPEndPoint, RequestHandle> OnRequestFailure;

        public event Action<UdpPoint, IPEndPoint, ConnectHandle> OnConnectHandle;
        public event Action<UdpPoint, IPEndPoint, MessageHandle> OnMessageHandle;
        public event Action<UdpPoint, IPEndPoint, ReadBufferHandle> OnReadBufferHandle;
        public event Action<UdpPoint, IPEndPoint, WriteBufferHandle> OnWriteBufferHandle;
        public event Action<UdpPoint, IPEndPoint, CmdDataHandle> OnCmdDataHandle;

        public event Action<UdpPoint, IPEndPoint, RequestDataHandle> OnRequestDataHandle;
        public event Action<UdpPoint, IPEndPoint, ReplyDataHandle> OnReplyDataHandle;

        public event Action<UdpPoint, IPEndPoint, InformHandle> OnInformHandle;

        public event Action<UdpPoint, IPEndPoint, RequestCmdHandle> OnRequestCmdHandle;
        public event Action<UdpPoint, IPEndPoint, ReplyCmdHandle> OnReplyCmdHandle;

        public void DisposeRequestFailure(Request request)
        {
            //Dispose(point, request.remote, OnRequestFailure, request.value);
            if (OnRequestFailure != null) OnRequestFailure(point, request.remote, request.handle as RequestHandle);
        }
        private void DisposeHandle(IPEndPoint origin, HandleEnum handle,  byte[] value)
        {
            switch (handle)
            {
                case HandleEnum.Connect:
                    if (OnConnectHandle != null)
                    {
                        Dispose(point, origin, OnConnectHandle, value);
                    }
                    else
                    {
                        Console.WriteLine("未配置" + HandleEnum.Connect);
                    }
                    break;
                case HandleEnum.Message:
                    if (OnMessageHandle != null)
                    {
                        Dispose(point, origin, OnMessageHandle, value);
                    }
                    else
                    {
                        Console.WriteLine("未配置" + HandleEnum.Message);
                    }
                    break;
                case HandleEnum.ReadBuffer:
                    if (OnReadBufferHandle != null)
                    {
                        Dispose(point, origin, OnReadBufferHandle, value);
                    }
                    else
                    {
                        Console.WriteLine("未配置" + HandleEnum.ReadBuffer);
                    }
                    break;
                case HandleEnum.WriteBuffer:
                    if (OnWriteBufferHandle != null)
                    {
                        Dispose(point, origin, OnWriteBufferHandle, value);
                    }
                    else
                    {
                        Console.WriteLine("未配置" + HandleEnum.WriteBuffer);
                    }
                    break;
                case HandleEnum.CmdData:
                    if (OnCmdDataHandle != null)
                    {
                        Dispose(point, origin, OnCmdDataHandle, value);
                    }
                    else
                    {
                        Console.WriteLine("未配置" + HandleEnum.CmdData);
                    }
                    break;
                case HandleEnum.RequestData:
                    if (OnRequestDataHandle != null)
                    {
                        Dispose(point, origin, OnRequestDataHandle, value);
                    }
                    else
                    {
                        Console.WriteLine("未配置" + HandleEnum.RequestData);
                    }
                    break;
                case HandleEnum.ReplyData:
                    if (OnReplyDataHandle != null)
                    {
                        Dispose(point, origin, OnReplyDataHandle, value);
                    }
                    else
                    {
                        Console.WriteLine("未配置" + HandleEnum.ReplyData);
                    }
                    break;
                case HandleEnum.RequestCmd:
                    if (OnRequestCmdHandle != null)
                    {
                        Dispose(point, origin, OnRequestCmdHandle, value);
                    }
                    else
                    {
                        Console.WriteLine("未配置" + HandleEnum.RequestCmd);
                    }
                    break;
                case HandleEnum.ReplyCmd:
                    if (OnReplyCmdHandle != null)
                    {
                        Dispose(point, origin, OnReplyCmdHandle, value);
                    }
                    else
                    {
                        Console.WriteLine("未配置" + HandleEnum.ReplyCmd);
                    }
                    break;
                case HandleEnum.Inform:
                    if (OnInformHandle != null)
                    {
                        Dispose(point, origin, OnInformHandle, value);
                    }
                    else
                    {
                        Console.WriteLine("未配置" + HandleEnum.Inform);
                    }
                    break;
                case HandleEnum.Null:
                    Console.WriteLine("未配置" + HandleEnum.Null);
                    break;
            }
        }
        private static void Dispose<UdpPoint, IPEndPoint, T>(UdpPoint point, IPEndPoint remote, Action<UdpPoint, IPEndPoint, T> action, byte[] value) where T : BaseHandle
        {
            if (action != null) action(point, remote, Activator.CreateInstance(typeof(T), value) as T);
        }
        //处理指定的操作
        #endregion
        #region 映射字典
        //读取缓存字典(本机数据存储于此)
        internal Dictionary<BufferEnum, byte[]> ReadBufferDic;
        //写入缓存字典(返回数据存储于此)
        internal Dictionary<BufferEnum, byte[]> WriteBufferDic;

        //枚举值映射字典
        internal static Dictionary<int, HandleEnum> EnumHandleMapDic;
        //枚举值映射字典
        internal static Dictionary<int, BufferEnum> EnumBufferMapDic;
        //枚举值映射字典
        internal static Dictionary<int, RequestEnum> EnumRequestMapDic;

        //操作数组映射字典(操作类型对应的数组形式)
        internal static Dictionary<HandleEnum, byte[]> HandleToByteMapDic;
        //操作数组映射字典(操作类型对应的数组形式)
        internal static Dictionary<BufferEnum, byte[]> BufferToByteMapDic;
        //操作数组映射字典(操作类型对应的数组形式)
        internal static Dictionary<RequestEnum, byte[]> RequestToByteMapDic;

        //操作长度映射字典
        internal static Dictionary<HandleEnum, int> HandleLengthMapDic;
        //操作动态映射字典
        internal static Dictionary<HandleEnum, bool> HandleDynamicMapDic;
        //操作请求映射字典
        internal static Dictionary<HandleEnum, bool> HandleRequestMapDic;

        #endregion
        #region 主体部分
        //绑定操作类型到类并指定回调函数
        internal static void Bind<T>() where T : BaseHandle
        {
            using (T tmp = (T)(Activator.CreateInstance(typeof(T),new byte[] { })))
            {
                HandleEnum handle = tmp.type;
                HandleLengthMapDic[handle]= tmp.Used + tmp.Length;
                HandleDynamicMapDic[handle]= tmp.isDynamic;
                HandleRequestMapDic[handle]= tmp.isRequest;
            }
        }
        //从start位置开始读取，判断操作类型
        internal UdpPoint point;
        internal static HandleEnum ByteToHandle(byte[] value, int start = 0)
        {
            int handle = value[start] + (value[start + 1] << 8) + (value[start + 2] << 16) + (value[start + 3] << 24);
            HandleEnum tmp;
            if (EnumHandleMapDic.TryGetValue(handle, out tmp))
            {
                return tmp;
            }
            else
            {
                return HandleEnum.Null;
            }
        }
        internal static BufferEnum ByteToBuffer(byte[] value,int start = 0)
        {
            int handle = value[start] + (value[start + 1] << 8) + (value[start + 2] << 16) + (value[start + 3] << 24);
            BufferEnum tmp;
            if (EnumBufferMapDic.TryGetValue(handle, out tmp))
            {
                return tmp;
            }
            else
            {
                return BufferEnum.Null;
            }
        }
        internal static RequestEnum ByteToRequest(byte[] value,int start = 0)
        {
            int handle = value[start] + (value[start + 1] << 8) + (value[start + 2] << 16) + (value[start + 3] << 24);
            RequestEnum tmp;
            if (EnumRequestMapDic.TryGetValue(handle, out tmp))
            {
                return tmp;
            }
            else
            {
                return RequestEnum.Null;
            }
        }
        //转为指令形式,从value中的current位置开始输出数组指令到array
        internal static HandleEnum FromByteToHandle(byte[] value, ref int current, out byte[] array)
        {
            //Debug.Log("当前位置"+current);
            HandleEnum handle = ByteToHandle(value, current);
            if (handle != HandleEnum.Null)   //如果指令为空
            {
                //验证值
                //int verify = 0;
                //根据获取的类型值查询对应数组长度
                int length = HandleLengthMapDic[handle];
                //动态范围则需要加上其后续数据的长度
                if (HandleDynamicMapDic[handle]) length += value[current + 4] + (value[current + 5] << 8) + (value[current + 6] << 16) + (value[current + 7] << 24);
                //if (HandleRequestMapDic[handle])
                //{
                //    verify = value[current + 8] + (value[current + 9] << 8) + (value[current + 10] << 16) + (value[current + 11] << 24);
                //    if (point.RetryDic.TryAdd(verify, TimeMgr.TickTime))
                //    {
                //        Debug.Log("[有效请求]"+verify);
                //    }
                //}
                array = new byte[length];
                for (int i = 0; i < length; i++)
                {
                    array[i] = value[current + i];
                }
                //移动游标
                current += length - 1;
            }
            else
            {
                handle = HandleEnum.Null;
                array = HandleToByteMapDic[HandleEnum.Null];
            }
            return handle;
        }
        //执行绑定映射的事件
        public int Dispose(IPEndPoint origin, byte[] value)
        {
            //Debug.Log("[节点]"+ point.LocalEndPoint + "[来源]"+ origin + "[长度]" + value.Length);
            int count=0;
            byte[] current;
            for (int i = 0; i < value.Length; i++)
            {
                //获取当前的操作
                HandleEnum handle = FromByteToHandle(value, ref i, out current);
                //Debug.Log("获取操作"+handle+"当前委托"+ EnumActionMapDic[handle]);
                //调用映射的委托
                DisposeHandle(origin, handle, current);
                count += 1;
            }
            return count;
        }
        //完成请求验证
        public static void FinishRequest(UdpPoint point,ReplyHandle handle)
        {
            Request request;
            //尝试移除验证值，如果不存在或者处于竞争状态则抛出警告
            if (point.RequestDic.TryRemove(handle.verify, out request))
            {
                //每次成功收到回复都会修正网络状态
                Netstat netstat = point.NetstatDic[request.remote];
                netstat.timelast = TickTime;
                netstat.delay = TickTime - request.timeSend;
                netstat.totalDelay += netstat.delay;
                //Console.WriteLine("[验证成功]" + point.LocalEndPoint + "[验证值]" + handle.verify + "[本次延迟]" + netstat.delay);
                request.succeed(request, handle.data);
                //OutStr += "[验证成功]" + point.LocalEndPoint + "[验证值]" + handle.verify + "[本次延迟]" + netstat.delay +"\n";
            }
            else
            {
                string str = "[验证总数]" + point.RequestDic.Count + "[详细]";
                foreach (var item in point.RequestDic.Keys)
                {
                    str += "<" + item + ">";
                }
                str += "[不包含]" + "<" + handle.verify + ">";
                //OutStr += str + "\n";
                //Console.WriteLine(str);
            }
        }
        #endregion
        #region 本地缓存操作
        //写入本地读取缓存
        public void UpdateReadBuffer(BufferEnum buffer,byte[] value)
        {
            ReadBufferDic[buffer]= value;
        }
        public void UpdateWriteBuffer(BufferEnum buffer, byte[] value)
        {
            WriteBufferDic[buffer]=value;
        }
        public byte[] GetReadBuffer(BufferEnum buffer)
        {
            return ReadBufferDic[buffer];
        }
        public byte[] GetWriteBuffer(BufferEnum buffer)
        {
            return WriteBufferDic[buffer];
        }
        #endregion
        #region 构造函数
        public HandleManager(UdpPoint point)
        {
            #region 初始化
            this.point = point;

            ReadBufferDic = new Dictionary<BufferEnum, byte[]>();
            WriteBufferDic = new Dictionary<BufferEnum, byte[]>();

            HandleLengthMapDic = new Dictionary<HandleEnum, int>();
            HandleDynamicMapDic = new Dictionary<HandleEnum, bool>();
            HandleRequestMapDic = new Dictionary<HandleEnum, bool>();

            HandleToByteMapDic = new Dictionary<HandleEnum, byte[]>();
            BufferToByteMapDic = new Dictionary<BufferEnum, byte[]>();
            RequestToByteMapDic = new Dictionary<RequestEnum, byte[]>();

            EnumHandleMapDic = new Dictionary<int, HandleEnum>();
            EnumBufferMapDic = new Dictionary<int, BufferEnum>();
            EnumRequestMapDic = new Dictionary<int, RequestEnum>();


            foreach (var item in Enum.GetValues(typeof(HandleEnum)) as HandleEnum[])
            {
                EnumHandleMapDic[(int)item] = item;
                HandleToByteMapDic[item] = new byte[4]
                {
                    (byte)((int)item),
                    (byte)((int)item>>8),
                    (byte)((int)item>>16),
                    (byte)((int)item>>24)
                };
            }
            foreach (var item in Enum.GetValues(typeof(RequestEnum)) as RequestEnum[])
            {
                EnumRequestMapDic[(int)item] = item;
                RequestToByteMapDic[item] = new byte[4]
                {
                    (byte)((int)item),
                    (byte)((int)item>>8),
                    (byte)((int)item>>16),
                    (byte)((int)item>>24)
                };
            }
            foreach (var item in Enum.GetValues(typeof(BufferEnum)) as BufferEnum[])
            {
                EnumBufferMapDic[(int)item]= item;
                BufferToByteMapDic[item] = new byte[4]
                {
                    (byte)((int)item),
                    (byte)((int)item>>8),
                    (byte)((int)item>>16),
                    (byte)((int)item>>24)
                };
            }
            #endregion
            #region 功能实现
            OnReadBufferHandle += OnReadBufferCallback;
            OnWriteBufferHandle += OnWriteBufferCallback;
            OnReplyDataHandle += OnReplyDataCallback;
            OnReplyCmdHandle += OnReplyCmdCallback;
            #endregion
            Bind<ConnectHandle>();
            Bind<MessageHandle>();
            Bind<ReadBufferHandle>();
            Bind<WriteBufferHandle>();
            Bind<CmdDataHandle>();
            Bind<RequestDataHandle>();
            Bind<ReplyDataHandle>();
            Bind<RequestCmdHandle>();
            Bind<ReplyCmdHandle>();
            Bind<InformHandle>();
        }
        #endregion
        #region 功能实现
        private void OnWriteBufferCallback(UdpPoint point, IPEndPoint remote, WriteBufferHandle handle)
        {
            if (handle.data != null&&handle.data.Length>0)
            {
                //Debug.Log("写入=>" + handle.write + "[数据大小]" + handle.data.Length);
                WriteBufferDic[handle.buffer] = handle.data;
            }
            else
            {
                //Debug.Log("写入=>" + handle.write + "[没有数据]" + "null");
            }
            FinishRequest(point, handle);
        }
        private void OnReadBufferCallback(UdpPoint point, IPEndPoint remote, ReadBufferHandle handle)
        {
            byte[] value;
            if(!ReadBufferDic.TryGetValue(handle.buffer, out value))
            {
                value = new byte[0];
                //Debug.Log("读取=>" + handle.buffer +"[没有数据]"+"null");
            }
            else
            {
                //Debug.Log("读取=>" + handle.buffer + "[数据大小]"+value.Length);
            }
            point.BunchTaskAdd(remote, WriteBufferHandle.Instance(handle.buffer, handle.verify, value));
            //point.ReplyCollect(WriteBufferHandle.Instance(handle.buffer, handle.verify, value).ToBytes());
        }
        private void OnReplyDataCallback(UdpPoint point,IPEndPoint remote,ReplyDataHandle handle)
        {
            //Debug.Log("[完成回复]"+ handle.verify);
            FinishRequest(point, handle);
        }
        private void OnReplyCmdCallback(UdpPoint point, IPEndPoint remote, ReplyCmdHandle handle)
        {
            FinishRequest(point, handle);
        }

        public void Dispose()
        {
            ReadBufferDic.Clear();
            ReadBufferDic = null;
            WriteBufferDic.Clear();
            WriteBufferDic = null;

            OnRequestFailure = null;
            OnConnectHandle = null;
            OnMessageHandle = null;
            OnReadBufferHandle = null;
            OnWriteBufferHandle = null;
            OnCmdDataHandle = null;
            OnRequestDataHandle = null;
            OnReplyDataHandle = null;
            OnInformHandle = null;
            OnRequestCmdHandle = null;
            OnReplyCmdHandle = null;
        }


        #endregion
    }
    #endregion
    #region 指令集合
    //基本操作，拥有固定的长度
    public abstract class BaseHandle : IDisposable
    {
        //protected static TimeManager TimeMgr { get { return TimeManager.Instance; } }
        //基本操作类型占用4字节空间
        public virtual int Used { get { return 4; } }
        /* 操作的类型识别**/
        public abstract HandleEnum type { get; }
        /* 操作的字节长度**/
        public abstract int Length { get; }
        /* 判断是否为动态**/
        public virtual bool isDynamic { get { return false; } }
        /* 判断是否为请求*/
        public virtual bool isRequest { get { return false; } }
        /* 转为字节数组形式**/
        public virtual byte[] ToBytes()
        {
            byte[] value = new byte[Used+Length];
            byte[] tmp = HandleManager.HandleToByteMapDic[type];
            value[0] = tmp[0];
            value[1] = tmp[1];
            value[2] = tmp[2];
            value[3] = tmp[3];
            return value;
        }

        public BaseHandle(byte[] value = null) { }
        //判断是否有效
        protected virtual bool TryValid(byte[] value)
        {
            return value != null && value.Length >= (Used + Length);
        }
        public void Dispose()
        {
            //手动调用了Dispose释放资源，那么析构函数就是不必要的了，这里阻止GC调用析构函数
            System.GC.SuppressFinalize(this);
        }
    }
    //动态操作，该类操作不定长
    public abstract class DynamicHandle : BaseHandle
    {
        public DynamicHandle(byte[] value) : base(value){}
        //动态范围
        public override bool isDynamic { get { return true; } }
        //动态操作类型需要占用4字节空间
        public override int Used { get { return base.Used+4; } }
        //其后跟随的数据所占空间大小
        public abstract int Size { get; }
        public override byte[] ToBytes()
        {
            byte[] value = new byte[Length + Size + Used];
            byte[] tmp = HandleManager.HandleToByteMapDic[type];
            value[0] = tmp[0];
            value[1] = tmp[1];
            value[2] = tmp[2];
            value[3] = tmp[3];
            value[4] = (byte)(Size);
            value[5] = (byte)(Size >> 8);
            value[6] = (byte)(Size >> 16);
            value[7] = (byte)(Size >> 24);
            return value;
        }
        protected override bool TryValid(byte[] value)
        {
            return value != null && value.Length >= (Used + Length + Size);
        }
    }
    //请求数据操作，可以携带数据，无顺序的可靠消息
    public abstract class RequestHandle : DynamicHandle
    {
        public byte[] data;
        public override bool isRequest { get { return true; } }

        public RequestHandle(byte[] value) : base(value)
        {
            if (TryValid(value))
            {
                verify = value[8] + (value[9] << 8) + (value[10] << 16) + (value[11] << 24);
                data = new byte[value.Length - Used - Length];
                for (int i = 0; i < data.Length; i++)
                {
                    data[i] = value[Used + Length + i];
                }
            }
        }
        public override int Used { get { return 12; } }

        public override int Size
        {
            get
            {
                if (data != null)
                    return data.Length;
                else
                    return 0;
            }
        }
        //验证码
        public int verify;

        public override byte[] ToBytes()
        {
            byte[] value = base.ToBytes();
            value[8] = (byte)(verify);
            value[9] = (byte)(verify >> 8);
            value[10] = (byte)(verify >> 16);
            value[11] = (byte)(verify >> 24);
            //为动态范围赋值
            if (data != null)
            {
                for (int i = Used + Length; i < value.Length; i++)
                {
                    value[i] = data[i - (Used + Length)];
                }
            }
            return value;
        }
    }
    //回复操作，拥有动态长度，用于验证请求
    public abstract class ReplyHandle : DynamicHandle
    {
        public byte[] data;
        public int verify;
        //回复操作类型占用12字节空间
        public override int Used { get { return 12; } }
        public override int Size
        {
            get
            {
                if (data != null)
                    return data.Length;
                else
                    return 0;
            }
        }

        public ReplyHandle(byte[] value = null) : base(value)
        {
            if (TryValid(value))
            {
                verify = value[8] + (value[9] << 8) + (value[10] << 16) + (value[11] << 24);
                data = new byte[value.Length - Used - Length];
                for (int i = 0; i < data.Length; i++)
                {
                    data[i] = value[Used + Length + i];
                }
            }
        }
        public override byte[] ToBytes()
        {
            byte[] value = base.ToBytes();
            value[8] = (byte)(verify);
            value[9] = (byte)(verify >> 8);
            value[10] = (byte)(verify >> 16);
            value[11] = (byte)(verify >> 24);
            //为动态范围赋值
            if (data != null)
            {
                for (int i = Used + Length; i < value.Length; i++)
                {
                    value[i] = data[i - (Used + Length)];
                }
            }
            return value;
        }
    }

    /// <summary>
    /// 读取指定缓存区
    /// </summary>
    public class ReadBufferHandle : RequestHandle
    {
        //请求读取的缓存区
        public BufferEnum buffer;
        //操作类型
        public override HandleEnum type { get { return HandleEnum.ReadBuffer; } }
        //操作长度
        public override int Length { get { return 4; } }

        public ReadBufferHandle(byte[] value) : base(value)
        {
            if (TryValid(value))
            {
                buffer = HandleManager.ByteToBuffer(value, Used);
            }
        }
        public override byte[] ToBytes()
        {
            byte[] value = base.ToBytes();
            byte[] tmp = HandleManager.BufferToByteMapDic[buffer];
            value[Used + 0] = tmp[0];
            value[Used + 1] = tmp[1];
            value[Used + 2] = tmp[2];
            value[Used + 3] = tmp[3];
            return value;
        }
        public static ReadBufferHandle Instance(BufferEnum buffer,int verify,byte[] data = null)
        {
            ReadBufferHandle handle = new ReadBufferHandle(null);
            handle.verify = verify;
            handle.buffer = buffer;
            handle.data = data;
            return handle;
        }
    }
    /// <summary>
    /// 写入指定缓存区
    /// </summary>
    public class WriteBufferHandle : ReplyHandle
    {
        public override int Length { get { return 4; } }
        public override HandleEnum type { get { return HandleEnum.WriteBuffer; } }

        public BufferEnum buffer;

        public WriteBufferHandle(byte[] value) : base(value)
        {
            if (TryValid(value))
            {
                buffer = HandleManager.ByteToBuffer(value, Used);
            }
        }
        public override byte[] ToBytes()
        {
            byte[] value = base.ToBytes();
            byte[] tmp = HandleManager.BufferToByteMapDic[buffer];
            value[Used + 0] = tmp[0];
            value[Used + 1] = tmp[1];
            value[Used + 2] = tmp[2];
            value[Used + 3] = tmp[3];
            return value;
        }

        public static WriteBufferHandle Instance(BufferEnum write, int verify, byte[] data)
        {
            WriteBufferHandle handle = new WriteBufferHandle(null);
            handle.buffer = write;
            handle.verify = verify;
            handle.data = data;
            return handle;
        }
    }
    public class ReplyDataHandle : ReplyHandle
    {
        public RequestEnum request;

        public ReplyDataHandle(byte[] value = null) : base(value)
        {
            if (TryValid(value))
            {
                request = HandleManager.ByteToRequest(value, Used);
            }
        }
        public override byte[] ToBytes()
        {
            byte[] value = base.ToBytes();
            byte[] tmp = HandleManager.RequestToByteMapDic[request];
            value[Used + 0] = tmp[0];
            value[Used + 1] = tmp[1];
            value[Used + 2] = tmp[2];
            value[Used + 3] = tmp[3];
            return value;
        }

        public override HandleEnum type{get{return HandleEnum.ReplyData;}}
        public override int Length { get { return 4; } }

        public static ReplyDataHandle Instance(RequestEnum request,int verify, byte[] data)
        {
            ReplyDataHandle handle = new ReplyDataHandle(null);
            handle.request = request;
            handle.verify = verify;
            handle.data = data;
            return handle;
        }
    }
    
    public class ReplyCmdHandle : ReplyHandle
    {
        public int currentFrame;
        //public int frameLength;

        public ReplyCmdHandle(byte[] value = null) : base(value)
        {
            if (TryValid(value))
            {
                currentFrame = value[Used] + (value[Used + 1] << 8) + (value[Used + 2] << 16) + (value[Used + 3] << 24);
                //frameLength = value[Used +4] + (value[Used + 5] << 8) + (value[Used + 6] << 16) + (value[Used + 7] << 24);
            }
        }
        public override byte[] ToBytes()
        {
            byte[] value = base.ToBytes();
            value[Used + 0] = (byte)(currentFrame);
            value[Used + 1] = (byte)(currentFrame >> 8);
            value[Used + 2] = (byte)(currentFrame >> 16);
            value[Used + 3] = (byte)(currentFrame >> 24);
            //value[Used + 4] = (byte)(frameLength);
            //value[Used + 5] = (byte)(frameLength >> 8);
            //value[Used + 6] = (byte)(frameLength >> 16);
            //value[Used + 7] = (byte)(frameLength >> 24);
            return value;
        }

        public override HandleEnum type { get { return HandleEnum.ReplyCmd; } }
        public override int Length { get { return 4; } }

        public static ReplyCmdHandle Instance(int currentFrame,int verify, byte[] data)
        {
            ReplyCmdHandle handle = new ReplyCmdHandle(null);
            handle.currentFrame = currentFrame;
            //handle.frameLength = frameLength;
            handle.verify = verify;
            handle.data = data;
            return handle;
        }
    }
    /// <summary>
    /// 请求指定的类型数据
    /// </summary>
    public class RequestDataHandle: RequestHandle
    {
        //请求类型
        public RequestEnum request;

        public RequestDataHandle(byte[] value = null) : base(value)
        {
            if (TryValid(value))
            {
                request = HandleManager.ByteToRequest(value, Used);
            }
        }
        public override byte[] ToBytes()
        {
            byte[] value = base.ToBytes();
            byte[] tmp = HandleManager.RequestToByteMapDic[request];
            value[Used + 0] = tmp[0];
            value[Used + 1] = tmp[1];
            value[Used + 2] = tmp[2];
            value[Used + 3] = tmp[3];
            //Debug.Log("[转换详细]" + value.ToDetail());
            return value;
        }
        public override HandleEnum type { get { return HandleEnum.RequestData; } }
        public override int Length { get { return 4; } }
        public static RequestDataHandle Instance(RequestEnum request, int verify,byte[] data=null)
        {
            RequestDataHandle handle = new RequestDataHandle(null);
            handle.verify = verify;
            handle.request = request;
            handle.data = data;
            return handle;
        }
    }
    /// <summary>
    /// 请求指定的游戏帧的指令集
    /// </summary>
    public class RequestCmdHandle : RequestHandle
    {
        public override HandleEnum type { get { return HandleEnum.RequestCmd; } }
        public override int Length { get { return 8; } }

        //请求的起始帧
        public int startFrame;
        //请求的帧长度
        public int frameLength;

        public RequestCmdHandle(byte[] value = null) : base(value)
        {
            if (TryValid(value))
            {
                startFrame = value[Used] + (value[Used + 1] << 8) + (value[Used + 2] << 16) + (value[Used + 3] << 24);
                frameLength = value[Used + 4] + (value[Used + 5] << 8) + (value[Used + 6] << 16) + (value[Used + 7] << 24);
            }
        }
        public override byte[] ToBytes()
        {
            byte[] value = base.ToBytes();
            value[Used + 0] = (byte)(startFrame);
            value[Used + 1] = (byte)(startFrame >> 8);
            value[Used + 2] = (byte)(startFrame >> 16);
            value[Used + 3] = (byte)(startFrame >> 24);
            value[Used + 4] = (byte)(frameLength);
            value[Used + 5] = (byte)(frameLength >> 8);
            value[Used + 6] = (byte)(frameLength >> 16);
            value[Used + 7] = (byte)(frameLength >> 24);
            return value;
        }

        public static RequestCmdHandle Instance(int startFrame,int frameLength, int verify, byte[] data = null)
        {
            RequestCmdHandle handle = new RequestCmdHandle(null);
            handle.verify = verify;
            handle.startFrame = startFrame;
            handle.frameLength = frameLength;
            handle.data = data;
            return handle;
        }
    }
    public class ConnectHandle : BaseHandle
    {
        //时间
        public int time;
        public override HandleEnum type { get { return HandleEnum.Connect; } }

        //本操作占用4个字节(实际为8个字节)
        public override int Length { get { return 4; } }
        //转为数组
        public override byte[] ToBytes()
        {
            byte[] value = base.ToBytes();
            value[Used + 0] = (byte)(time);
            value[Used + 1] = (byte)(time >> 8);
            value[Used + 2] = (byte)(time >> 16);
            value[Used + 3] = (byte)(time >> 24);
            return value;
        }

        public ConnectHandle(byte[] value = null) : base(value)
        {
            if (TryValid(value))
            {
                time = value[Used] + (value[Used + 1] << 8) + (value[Used + 2] << 16) + (value[Used + 3] << 24);
            }
        }
        public static ConnectHandle Instance(int time)
        {
            ConnectHandle handle = new ConnectHandle();
            handle.time = time;
            return handle;
        }
    }
    /// <summary>
    /// 通告操作
    /// </summary>
    public class InformHandle : BaseHandle
    {
        //通告可以连接的远端地址
        public IPEndPoint remote;

        public override int Length{get{return 6;}}
        public override HandleEnum type{get{return HandleEnum.Inform; }}

        public InformHandle(byte[] value = null) : base(value)
        {
            if (TryValid(value))
            {
                byte[] tmp = new byte[4];
                tmp[0] = value[Used + 0];
                tmp[1] = value[Used + 1];
                tmp[2] = value[Used + 2];
                tmp[3] = value[Used + 3];
                IPAddress address = new IPAddress(tmp);
                int port = value[Used + 4] + (value[Used + 5] << 8);
                remote = new IPEndPoint(address, port);
            }
        }
        public override byte[] ToBytes()
        {
            byte[] value = base.ToBytes();
            byte[] tmp = remote.Address.GetAddressBytes();
            value[Used + 0] = tmp[0];
            value[Used + 1] = tmp[1];
            value[Used + 2] = tmp[2];
            value[Used + 3] = tmp[3];
            value[Used + 4] = (byte)remote.Port;
            value[Used + 5] = (byte)(remote.Port>>8);
            return value;
        }

        public static InformHandle Instance(IPEndPoint remote)
        {
            InformHandle handle = new InformHandle();
            handle.remote = remote;
            return handle;
        }
    }

    public class MessageHandle : DynamicHandle
    {
        //携带的消息
        public string message = null;

        //读取指令后4字节返回长度
        public override int Length { get { return 0; } }
        //返回消息的长度
        public override int Size
        {
            get
            {
                if (message != null)
                    return Encoding.UTF8.GetBytes(message).Length;
                else
                    return 0;
            }
        }

        public override HandleEnum type{get{return HandleEnum.Message; } }

        public override byte[] ToBytes()
        {
            byte[] value = base.ToBytes();
            byte[] tmp = Encoding.UTF8.GetBytes(message);
            //Debug.Log("数据"+message +"大小"+ tmp.Length + "操作长度"+value.Length);
            //为动态范围赋值
            for (int i = Used+Length; i < value.Length; i++)
            {
                value[i] = tmp[i - (Used + Length)];
            }
            return value;
        }
        public MessageHandle(byte[] value) : base(value)
        {
            if (TryValid(value))
            {
                byte[] tmp = new byte[value.Length - Used - Length];
                for(int i = 0; i < tmp.Length; i++)
                {
                    tmp[i] = value[Used + Length + i];
                }
                message = Encoding.UTF8.GetString(tmp);
            }
        }
        public static MessageHandle Instance(string message)
        {
            MessageHandle handle = new MessageHandle(null);
            handle.message = message;
            return handle;
        }
    }
    /// <summary>
    /// 指令操作，携带不定长的指令数据，无回复
    /// </summary>
    public class CmdDataHandle : DynamicHandle
    {
        //当前游戏帧(游戏帧)
        public int frame;
        public byte[] cmd;
        public override HandleEnum type { get { return HandleEnum.CmdData; } }

        public CmdDataHandle(byte[] value=null) : base(value)
        {
            if (TryValid(value))
            {
                frame = value[Used] + (value[Used + 1] << 8) + (value[Used + 2] << 16) + (value[Used + 3] << 24);
                cmd = new byte[value.Length - Used - Length];
                for (int i = 0; i < cmd.Length; i++)
                {
                    cmd[i] = value[Used + Length + i];
                }
            }
        }

        public override int Length{get{return 4;}}

        public override int Size
        {
            get
            {
                if (cmd != null)
                    return cmd.Length;
                else
                    return 0;
            }
        }
        public override byte[] ToBytes()
        {
            byte[] value = base.ToBytes();
            value[Used + 0] = (byte)(frame);
            value[Used + 1] = (byte)(frame >> 8);
            value[Used + 2] = (byte)(frame >> 16);
            value[Used + 3] = (byte)(frame >> 24);
            //为动态范围赋值
            for (int i = Used + Length; i < value.Length; i++)
            {
                value[i] = cmd[i - (Used + Length)];
            }
            return value;
        }

        public static CmdDataHandle Instance(int frame,byte[] cmd)
        {
            CmdDataHandle handle = new CmdDataHandle();
            handle.frame = frame;
            handle.cmd = cmd;
            return handle;
        }
    }

    #endregion
    #region 数据管理
    public enum HandleEnum
    {
        Null,           //空操作
        ReadBuffer,     //读取缓存(请求)
        WriteBuffer,    //返回缓存(回复)
        Connect,        //连接操作(请求)
        Message,        //消息操作(动态)
        CmdData,        //指令操作(动态)
        RequestData,    //请求操作(请求)
        ReplyData,      //回复操作(回复)
        RequestCmd,     //请求操作(请求)
        ReplyCmd,       //回复操作(回复)
        Inform,         //消息操作(普通)
    }
    public enum BufferEnum
    {
        Null,           //无效区域
        PublicBuffer,   //公共缓存区
        AnnunciateBuffer,   //公共缓存区
    }
    public enum RequestEnum
    {
        Null,           //请求无效
        Public,         //请求公共
        Register,       //请求注册
        ServerInfo,     //请求信息
        ConnectInfo,    //连接信息
        ConnectTest,    //连接测试
        ConnectValid,   //连接验证
        SyncInfo,       //请求同步
        ServerCanel,    //请求取消
        InitFrame,      //初始化帧
        RequestEntityUniqueness,//请求实体字典的唯一值
        RequestInitStatus,      //请求初始化状态
        UploadingEntity,        //上传单位状态
    }
    //请求
    public class Request
    {
        public int overtime;
        //已经成功发送了
        public bool isSend;
        //远端地址
        public IPEndPoint remote;
        //请求成功回调函数
        public Action<Request, byte[]> succeed;
        //请求失败回调函数
        public Action<Request> failed;
        //数据
        public byte[] value { get { return handle.ToBytes(); } }
        //指令
        public BaseHandle handle;
        //发出时间
        public int timeSend;
        //开始时间
        public int timeStart;
        //重试尝试
        public int retry;
        //验证值
        public int verify;
        public Request(RequestHandle handle, IPEndPoint remote, Action<Request, byte[]> succeed, Action<Request> failed)
        {
            this.remote = remote;
            this.succeed = succeed;
            this.failed = failed;
            this.handle = handle;

            verify = handle.verify;
            //数据包建立时间
            retry = 0;
        }
    }
    public class Netstat
    {
        //平均延迟
        public int AverageDelay
        {
            get
            {
                if ((request - loss) > 5)
                    //平均延迟=(长期延迟+当前延迟)/2
                    return (totalDelay / request + delay)/2;
                else
                    //估计延迟为100毫秒
                    return 100;
            }
        }
        public float LossRate
        {
            get
            {
                return ((float)loss / (float)request);
            }
        }
        //总延迟
        public int totalDelay;
        //网络延迟
        public int delay;
        //发出请求数
        public int request;
        //丢包数
        public int loss;
        //网络质量
        public int quality;
        //最后通信时间
        public int timelast;
        //重连次数
        public int retry;
        //验证值
        public int verify;
    }
    public class NetTask
    {
        public List<Request> requests = new List<Request>();
        public List<BaseHandle> handles = new List<BaseHandle>();
    }
    #endregion
}