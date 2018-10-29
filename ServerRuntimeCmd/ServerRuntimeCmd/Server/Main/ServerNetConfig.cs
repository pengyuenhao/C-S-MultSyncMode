using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServerRuntimeCmd.Server
{
    //服务端信息
    //public class ServerInfo
    //{
    //    //是否有效
    //    public bool isValid = true;
    //    //是否连接
    //    public bool isConnect = false;
    //    //是否隐藏
    //    public bool isHide = true;
    //    public int current;
    //    public int max;
    //    public string name;
    //    public string description;
    //    public IPEndPoint remote;
    //    //最后通信时间
    //    public int lastTime;
    //    //是否正在同步
    //    public bool isSync = false;
    //    //成功通信计数
    //    public int count = 0;
    //    //连接随机数
    //    public int rand;
    //}
    ////指令封包信息
    //public struct CmdInfo
    //{
    //    //当前指令帧
    //    public int frame;
    //    //指令集合
    //    public byte[] cmd;
    //    //指令封包时刻
    //    public int time;
    //}
    //public class ConcurrentUniquenessRandDic<T>
    //{
    //    private int m_RendVerify;
    //    private System.Random random;
    //    private ConcurrentDictionary<int, T> UniquenessRandMapDic = new ConcurrentDictionary<int, T>();
    //    /// <summary>
    //    /// 获取一个不存在于请求字典中的随机值
    //    /// </summary>
    //    public int Rand
    //    {
    //        get
    //        {
    //            Interlocked.Increment(ref m_RendVerify);
    //            random = new System.Random(System.Environment.TickCount ^ m_RendVerify);
    //            int rand = random.Next();
    //            int count = 100;
    //            //请求字典内不能有重复验证值
    //            while (UniquenessRandMapDic.ContainsKey(rand))
    //            {
    //                rand = random.Next();
    //                if (count-- < 0)
    //                {
    //                    //Console.WriteLine("意外的重复");
    //                    throw new Exception("产生了意外的重复值，可能是字典已满");
    //                }
    //            }
    //            return rand;
    //        }
    //    }
    //    public ConcurrentDictionary<int, T> GetDic()
    //    {
    //        return UniquenessRandMapDic;
    //    }
    //    public int GetAndAdd(ref T t)
    //    {
    //        int rand = Rand;
    //        UniquenessRandMapDic[rand] = t;
    //        return rand;
    //    }
    //    public T this[int index]
    //    {
    //        get
    //        {
    //            return UniquenessRandMapDic[index];
    //        }
    //        set
    //        {
    //            UniquenessRandMapDic[index] = value;
    //        }
    //    }
    //    public bool TryGetValue(int index,out T t)
    //    {
    //        return UniquenessRandMapDic.TryGetValue(index, out t);
    //    }
    //    public int Count { get { return UniquenessRandMapDic.Count; } }
    //}
    //public class UniquenessEntity
    //{
    //    public bool isUsed;
    //    public int rand;
    //    //序列化值
    //    public byte[] serialize;
    //}
}
