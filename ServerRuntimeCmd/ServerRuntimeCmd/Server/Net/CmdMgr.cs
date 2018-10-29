using Lib.Net.UDP;
using System;
using System.Collections.Generic;
using System.Text;

/* **************************指令集*************************************************
* 因为网络传输的指令只能是整数型，并且为了减少数据量，这里使用32位作为指令的承载单元
* 某些信息可能需求更多的位数表示，所以可能需要多个单元才能表达
* 指令定义:
* 0x00 心跳指令:无意义指令，仅仅确认通道正常{byte}
* 0x01 同步指令:紧跟一个时间值(4-bytes){5-bytes}
* 0x02 玩家指令:紧跟一个玩家ID值(4-bytes)，接收到此指令时更新当前发出指令的玩家{5-bytes}
* 0x03 按键指令:紧跟一个时间偏移值(byte)并跟随一个键值信息(2-byte){4-bytes}
* **/
namespace Helper.CmdMgr
{
    public static class CmdManager
    {
        private static Dictionary<CmdEnum, int> CmdLenghtDic;
        private static Dictionary<byte, CmdEnum> EnumCmdMapDic;
        //操作动态映射字典
        private static Dictionary<CmdEnum, bool> CmdDynamicMapDic;

        //转为指令形式,从value中的current位置开始输出数组指令到cmd
        public static CmdEnum ToCmd(byte[] value, ref int current,out byte[] array)
        {
            //Debug.Log("当前位置" + current);
            CmdEnum cmd = ByteToCmd(value,current);
            if (cmd != CmdEnum.Null)   //如果指令映射的长度大于当前指令值
            {
                //根据获取的类型值查询对应数组长度
                int length = CmdLenghtDic[cmd];
                //获取动态数据的长度
                if (CmdDynamicMapDic[cmd]) length += value[current +1] + (value[current + 2] << 8) + (value[current + 3] << 16) + (value[current + 4] << 24);
                if (length > 0)
                {
                    array = new byte[length];
                }
                else
                {
                    array = new byte[1] { (byte)CmdEnum.Null };
                    //current += 1;
                    return CmdEnum.Null;
                }
                for(int i=0;i< length; i++)
                {
                    array[i] = value[current+i];
                }
                //移动游标
                current += length - 1;
            }
            else
            {
                //current += 1;
                cmd = CmdEnum.Heartbeat;
                array = new byte[1] { (byte)cmd };
            }
            return cmd;
        }
        static CmdManager()
        {
            EnumCmdMapDic = new Dictionary<byte, CmdEnum>();
            CmdLenghtDic = new Dictionary<CmdEnum, int>();
            CmdDynamicMapDic = new Dictionary<CmdEnum, bool>();
            foreach (var item in Enum.GetValues(typeof(CmdEnum)) as CmdEnum[])
            {
                EnumCmdMapDic[(byte)item] = item;
            }
            Bind<BasetimeCmd>();
            Bind<SyncCmd>();
            Bind<PlayerCmd>();
            Bind<KeyCmd>();
            Bind<CreateEntityCmd>();
        }
        //从start位置开始读取，判断操作类型
        internal static CmdEnum ByteToCmd(byte[] value, int start = 0)
        {
            byte cmd = value[start];
            CmdEnum tmp;
            if (EnumCmdMapDic.TryGetValue(cmd, out tmp))
            {
                return tmp;
            }
            else
            {
                return CmdEnum.Null;
            }
        }
        //绑定操作类型到类并指定回调函数
        internal static void Bind<T>() where T : BaseCmd
        {
            using (T tmp = (T)(Activator.CreateInstance(typeof(T), new byte[] { })))
            {
                CmdEnum cmd = tmp.type;
                CmdLenghtDic[cmd] = tmp.Used + tmp.Length;
                CmdDynamicMapDic[cmd] = tmp.isDynamic;
            }
        }
        //转化为字节组形式
        public static byte[] ToBytes(List<BaseCmd> cmd)
        {
            List<byte> value = new List<byte>();
            foreach (var item in cmd)
            {
                value.AddRange(item.ToBytes());
            }
            cmd.Clear();
            return value.ToArray();
        }
    }
    //基本指令，拥有固定的长度
    public abstract class BaseCmd : IDisposable
    {
        //封包时刻的指令帧
        //public int frame;
        //基本指令类型占用5字节空间
        public virtual int Used { get { return 1; } }
        /* 判断是否为动态**/
        public virtual bool isDynamic { get { return false; } }
        /* 8位用于信息识别**/
        public abstract CmdEnum type { get; }
        /* 指令对应的字节长度**/
        public abstract int Length { get; }
        //转为字节数组形式
        public virtual byte[] ToBytes()
        {
            byte[] value = new byte[Used + Length];
            value[0] = (byte)type;
            //value[1] = (byte)(frame);
            //value[2] = (byte)(frame >> 8);
            //value[3] = (byte)(frame >> 16);
            //value[4] = (byte)(frame >> 24);
            return value;
        }
        public BaseCmd(byte[] value = null)
        {
            //如果数据有效则将封包时刻的指令帧写入变量内
            //if (TryValid(value))
            //{
            //    frame = (value[1] + (value[2] << 8) + (value[3] << 16) + (value[4] << 24));
            //}
        }
        //检查封包是否有效
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
    public abstract class FrameCmd : BaseCmd
    {
        public int frame;
        public override int Used { get { return base.Used + 4; } }
        public override byte[] ToBytes()
        {
            byte[] value = base.ToBytes();
            value[base.Used + 0] = (byte)(frame);
            value[base.Used + 1] = (byte)(frame >> 8);
            value[base.Used + 2] = (byte)(frame >> 16);
            value[base.Used + 3] = (byte)(frame >> 24);

            return value;
        }
        public FrameCmd(byte[] value = null) : base(value)
        {
            if (TryValid(value))
            {
                frame = (value[base.Used + 0] + (value[base.Used + 1] << 8) + (value[base.Used + 2] << 16) + (value[base.Used + 3] << 24));
            }
            else
            {
                frame = 0;
            }
        }
    }
    //带有时间参数的指令
    public abstract class ShortTimeCmd : FrameCmd
    {
        //时间偏移值，单位是1毫秒
        //一般单个更新回合不会超过1秒
        public ushort offsetTime;
        //已被使用掉的字节数加上该指令需要的字节数
        public override int Used { get { return base.Used + 2; } }
        public override byte[] ToBytes()
        {
            byte[] value = base.ToBytes();
            value[base.Used+0] = (byte)(offsetTime);
            value[base.Used+1] = (byte)(offsetTime >> 8);
            return value;
        }
        public ShortTimeCmd(byte[] value = null) : base(value)
        {
            if (TryValid(value))
            {
                offsetTime = (ushort)(value[base.Used + 0] + (value[base.Used + 1] << 8));
            }
            else
            {
                offsetTime = 0;
            }
        }
    }
    public abstract class TimeCmd : FrameCmd
    {
        public int time;

        public override int Used { get { return base.Used + 4; } }
        public override byte[] ToBytes()
        {
            byte[] value = base.ToBytes();
            value[base.Used + 0] = (byte)(time);
            value[base.Used + 1] = (byte)(time >> 8);
            value[base.Used + 2] = (byte)(time >> 16);
            value[base.Used + 3] = (byte)(time >> 24);

            return value;
        }
        public TimeCmd(byte[] value = null) : base(value)
        {
            if (TryValid(value))
            {
                time = (value[base.Used + 0] + (value[base.Used + 1] << 8)+ (value[base.Used + 2] << 16)+ (value[base.Used + 3] << 24));
            }
            else
            {
                time = 0;
            }
        }
    }

    public class KeyCmd : ShortTimeCmd
    {
        public override CmdEnum type { get { return CmdEnum.Key; } }
        //该指令包含数据的长度
        public override int Length{get{return 6;}}

        public bool isDown;     //是否为按下(占用前1位)
        public int key;     //键值(占用后15位)
        public int index;   //受令单位索引
        //使用数据中第0字节的第0比特记录是否为按下或抬起按键
        public override byte[] ToBytes()
        {
            byte[] value = base.ToBytes();
            int tmp = (int)key;                 //转化键值为整数
            if (isDown)
            {
                value[base.Used] = (byte)((tmp >> 8) & 0x7F);           //首位置0
            }
            else
            {
                value[base.Used] = (byte)(((tmp >> 8) & 0xFF)|0x80);    //首位置1
            }
            value[Used+1] = (byte)(tmp & 0xFF);     //取后8位
            value[base.Used + 2] = (byte)(index);
            value[base.Used + 3] = (byte)(index >> 8);
            value[base.Used + 4] = (byte)(index >> 16);
            value[base.Used + 5] = (byte)(index >> 24);
            return value;
        }
        public KeyCmd(byte[] value = null) : base(value)
        {
            if (TryValid(value))
            {
                //判断是否为按下
                isDown = (value[base.Used] >> 7 == 0) ? true : false;
                key = (((value[base.Used] & 0x7F) << 8) + value[base.Used +1]);
                index = value[base.Used + 2] + (value[base.Used + 3] << 8) + (value[base.Used + 4] << 16) + (value[base.Used + 5] << 24);
            }
        }

        public static KeyCmd Instance(int index,bool isDown, int key, int offsetTime,int frame)
        {
            KeyCmd cmd = new KeyCmd();
            cmd.index = index;
            cmd.key = key;
            cmd.isDown = isDown;
            cmd.frame = frame;
            cmd.offsetTime = (ushort)offsetTime;
            return cmd;
        }
    }
    /// <summary>
    /// 同步指令
    /// </summary>
    public class SyncCmd : FrameCmd
    {
        public override CmdEnum type { get { return CmdEnum.Sync; } }
        public override int Length { get { return 4; } }
        //从游戏开始到当前经过的时间
        public int time;

        public override byte[] ToBytes()
        {
            byte[] value = base.ToBytes();
            value[Used] = (byte)(time);
            value[Used+1] = (byte)(time >> 8);
            value[Used+2] = (byte)(time >> 16);
            value[Used+3] = (byte)(time >> 24);
            return value;
        }
        public SyncCmd(byte[] value = null) : base(value)
        {
            if (TryValid(value))
            {
                time = value[Used+0] + (value[Used+1] << 8) + (value[Used+2] << 16) + (value[Used+3] << 24);
            }
        }
        //这里告知服务端本机时间
        public static SyncCmd Instance(int time, int frame)
        {
            SyncCmd cmd = new SyncCmd();
            cmd.time = time;
            cmd.frame = frame;
            return cmd;
        }
    }
    /// <summary>
    /// 基准时间指令
    /// </summary>
    public class BasetimeCmd : TimeCmd
    {
        public override CmdEnum type { get { return CmdEnum.Basetime; } }
        public override int Length { get { return 8; } }
        //服务端识别码(客户端用于确认信息有效)
        public int verify;
        //指令的最大有效时间
        public int maxValidTime;

        public override byte[] ToBytes()
        {
            byte[] value = base.ToBytes();
            value[Used + 0] = (byte)(verify);
            value[Used + 1] = (byte)(verify >> 8);
            value[Used + 2] = (byte)(verify >> 16);
            value[Used + 3] = (byte)(verify >> 24);
            value[Used + 4] = (byte)(maxValidTime);
            value[Used + 5] = (byte)(maxValidTime >> 8);
            value[Used + 6] = (byte)(maxValidTime >> 16);
            value[Used + 7] = (byte)(maxValidTime >> 24);
            return value;
        }
        public BasetimeCmd(byte[] value = null) : base(value)
        {
            if (TryValid(value))
            {
                verify = value[Used+0] + (value[Used + 1] << 8) + (value[Used + 2] << 16) + (value[Used + 3] << 24);
                //verify = value.ToInt(Used);
                maxValidTime = value[Used + 4] + (value[Used + 5] << 8) + (value[Used + 6] << 16) + (value[Used + 7] << 24);
            }
        }

        public static BasetimeCmd Instance(int verity, int time,int maxValidTime,int frame)
        {
            BasetimeCmd cmd = new BasetimeCmd();
            cmd.verify = verity;
            cmd.frame = frame;
            cmd.maxValidTime = maxValidTime;
            cmd.time = time;
            return cmd;
        }
    }
    public class PlayerCmd : BaseCmd
    {
        public override CmdEnum type { get { return CmdEnum.Player; } }
        public override int Length { get { return 4; } }
        //玩家信息
        public NetNodeInfo player;

        public override byte[] ToBytes()
        {
            byte[] value = base.ToBytes();
            value[Used] = (byte)(player.id);
            value[Used+1] = (byte)(player.id >> 8);
            value[Used+2] = (byte)(player.id >> 16);
            value[Used+3] = (byte)(player.id >> 24);
            return value;
        }
        public PlayerCmd(byte[] value = null) : base(value)
        {
            if (TryValid(value))
            {
                int ID = value[Used] + (value[Used+1] << 8) + (value[Used+2] << 16) + (value[Used+3] << 24);
                //尝试获取玩家
                player = new NetNodeInfo();
                player.id = ID;
                //if (!PlayerManager.Instance.PlayerVerityMapDic.TryGetValue(ID, out player))
                //{
                //    player = PlayerManager.Instance.UnknowPlayer;
                //}
            }
        }

        public static PlayerCmd Instance(NetNodeInfo player)
        {
            PlayerCmd cmd = new PlayerCmd();
            cmd.player = player;
            return cmd;
        }
    }
    //动态操作，该类操作不定长
    public abstract class DynamicCmd : BaseCmd
    {
        public DynamicCmd(byte[] value) : base(value) { }
        //动态范围
        public override bool isDynamic { get { return true; } }
        //动态操作类型需要占用5字节空间
        public override int Used { get { return 5; } }
        //其后跟随的数据所占空间大小
        public abstract int Size { get; }
        public override byte[] ToBytes()
        {
            byte[] value = new byte[Length + Size + Used];
            value[0] = (byte)type;
            value[1] = (byte)(Size);
            value[2] = (byte)(Size >> 8);
            value[3] = (byte)(Size >> 16);
            value[4] = (byte)(Size >> 24);
            return value;
        }
        protected override bool TryValid(byte[] value)
        {
            return value != null && value.Length >= (Used + Length + Size);
        }
    }
    public class CreateEntityCmd : DynamicCmd
    {
        public override CmdEnum type { get { return CmdEnum.CreateEntity; } }
        //该指令包含数据的长度
        public override int Length { get { return 16; } }
        //识别号
        public string id;
        //单位索引
        public int index;
        //创建者
        public int player;

        public int frame;

        public int offsetTime;
        //携带字符串的大小
        public override int Size
        {
            get
            {
                if (id != null)
                    return Encoding.UTF8.GetBytes(id).Length;
                else
                    return 0;
            }
        }
        //使用数据中第0字节的第0比特记录是否为按下或抬起按键
        public override byte[] ToBytes()
        {
            byte[] value = base.ToBytes();
            value[base.Used + 0] = (byte)(index);
            value[base.Used + 1] = (byte)(index >> 8);
            value[base.Used + 2] = (byte)(index >> 16);
            value[base.Used + 3] = (byte)(index >> 24);
            value[base.Used + 4] = (byte)(player);
            value[base.Used + 5] = (byte)(player >> 8);
            value[base.Used + 6] = (byte)(player >> 16);
            value[base.Used + 7] = (byte)(player >> 24);
            value[base.Used + 8] = (byte)(frame);
            value[base.Used + 9] = (byte)(frame >> 8);
            value[base.Used + 10] = (byte)(frame >> 16);
            value[base.Used + 11] = (byte)(frame >> 24);
            value[base.Used + 12] = (byte)(offsetTime);
            value[base.Used + 13] = (byte)(offsetTime >> 8);
            value[base.Used + 14] = (byte)(offsetTime >> 16);
            value[base.Used + 15] = (byte)(offsetTime >> 24);
            byte[] tmp = Encoding.UTF8.GetBytes(id);
            //为动态范围赋值
            for (int i = Used + Length; i < value.Length; i++)
            {
                value[i] = tmp[i - (Used + Length)];
            }
            return value;
        }
        public CreateEntityCmd(byte[] value = null) : base(value)
        {
            if (TryValid(value))
            {
                index = value[base.Used + 0] + (value[base.Used + 1] << 8) + (value[base.Used + 2] << 16) + (value[base.Used + 3] << 24);
                player = value[base.Used + 4] + (value[base.Used + 5] << 8) + (value[base.Used + 6] << 16) + (value[base.Used + 7] << 24);
                frame = value[base.Used + 8] + (value[base.Used + 9] << 8) + (value[base.Used + 10] << 16) + (value[base.Used + 11] << 24);
                offsetTime = value[base.Used + 12] + (value[base.Used + 13] << 8) + (value[base.Used + 14] << 16) + (value[base.Used + 15] << 24);

                byte[] tmp = new byte[value.Length - Used - Length];
                for (int i = 0; i < tmp.Length; i++)
                {
                    tmp[i] = value[Used + Length + i];
                }
                id = Encoding.UTF8.GetString(tmp);
            }
        }

        public static CreateEntityCmd Instance(int index, string id, int player, int offsetTime, int frame)
        {
            CreateEntityCmd cmd = new CreateEntityCmd();
            cmd.index = index;
            cmd.id = id;
            cmd.player = player;
            cmd.frame = frame;
            cmd.offsetTime = (ushort)offsetTime;
            return cmd;
        }
    }
    //public class InitCmd : FrameCmd
    //{
    //    public override CmdEnum type { get { return CmdEnum.Init; } }
    //    public override int Length { get { return 4; } }
    //    //从游戏开始到当前经过的时间
    //    //public int frame;

    //    public override byte[] ToBytes()
    //    {
    //        byte[] value = base.ToBytes();
    //        //value[Used] = (byte)(time);
    //        //value[Used + 1] = (byte)(time >> 8);
    //        //value[Used + 2] = (byte)(time >> 16);
    //        //value[Used + 3] = (byte)(time >> 24);
    //        return value;
    //    }
    //    public InitCmd(byte[] value = null) : base(value)
    //    {
    //        if (TryValid(value))
    //        {
    //            //time = value[Used + 0] + (value[Used + 1] << 8) + (value[Used + 2] << 16) + (value[Used + 3] << 24);
    //        }
    //    }
    //    //这里告知服务端本机时间
    //    public static InitCmd Instance(int frame)
    //    {
    //        InitCmd cmd = new InitCmd();
    //        cmd.frame = frame;
    //        return cmd;
    //    }
    //}

    /********************************************************************/
    public enum CmdEnum : byte
    {
        Basetime,//基准时间
        Null,           //空指令
        Heartbeat,      //心跳指令
        Sync,           //同步时间
        Player,         //玩家指令
        Key,            //键值指令
        CreateEntity,   //创建实体指令
        //Init,           //初始化指令
    }
}
