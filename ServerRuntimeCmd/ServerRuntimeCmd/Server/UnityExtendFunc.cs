using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

/// <summary>
/// Unity自身功能拓展类
/// </summary>
public static class UnityExtendFunc
{
    #region IPAddress拓展
    public static bool IsBroadcast(this IPAddress ip)
    {
        if (ip.AddressFamily == AddressFamily.InterNetworkV6)
        {
            //
            // No such thing as a broadcast address for IPv6
            //
            return false;
        }
        else
        {
            
            return ip.GetAddressBytes() == IPAddress.Broadcast.GetAddressBytes();
        }
    }
    #endregion
    #region 值类型拓展
    public static string ToDetail(this List<List<string>> value)
    {
        string str = null;
        foreach (List<string> a in value)
        {
            string tmp = "<" + value.IndexOf(a) + ">" + "[";
            foreach (string b in a)
            {
                tmp += b + ",";
            }
            tmp = tmp.Remove(tmp.Length - 1) + "]";
            str += tmp +"\n";
        }
        str = str.Remove(str.Length - 1);
        return str;
    }

    public static string ToDetail(this string value)
    {
        string str = null;
        if (value.Length > 0)
        {
            for (int i = 0; i < value.Length; i++)
            {
                if (i == 0)
                {
                    if (value[i] != '\n')
                    {
                        str += "[" + value[i];
                    }
                    else
                    {
                        str += "[" + "|n";
                    }
                }
                else
                {
                    if (value[i] != '\n')
                    {
                        str += "," + value[i];
                    }
                    else
                    {
                        str += "," + "|n";
                    }
                }
            }
            str += "]";
        }
        else
        {
            str += "[null]";
        }
        return str;
    }
    public static string ToDetail(this byte[] value)
    {
        string str = null;
        if (value != null && value.Length > 0)
        {
            for (int i = 0; i < value.Length; i++)
            {
                if (i == 0) str += "[" + value[i];
                else str += "," + value[i];
            }
            str += "]";
        }
        else
        {
            str += "[null]";
        }
        return str;
    }
    public static string ToStr(this byte[] value,int start=0)
    {
        if (value.Length < 4) return null;
        int length = value.ToInt(start);
        byte[] tmp = new byte[length];
        for(int i = 0; i < length; i++)
        {
            tmp[i] = value[start + 4 + i];
        }
        string str = Encoding.UTF8.GetString(tmp);
        return str;
    }
    public static byte[] ToBytes(this string value,bool isLength = true)
    {
        if (isLength)
        {
            List<byte> list = new List<byte>();
            byte[] tmp = Encoding.UTF8.GetBytes(value);
            list.AddRange(tmp.Length.ToBytes());
            list.AddRange(tmp);
            return list.ToArray();
        }
        else
        {
            return Encoding.UTF8.GetBytes(value);
        }
    }
    public static byte[] ToBytes(this int value)
    {
        byte[] tmp = new byte[4];
        tmp[3] = (byte)(value >> 24);
        tmp[2] = (byte)(value >> 16);
        tmp[1] = (byte)(value >> 8);
        tmp[0] = (byte)(value);
        return tmp;
    }
    public static int ToInt(this byte[] value,int start = 0)
    {
        if (value.Length < 4) return 0;
        return (value[start+3] <<24) + (value[start+2] << 16) + (value[start+1] << 8) + value[start+0];
    }
    public static int ToInt(this string value,int start = 0)
    {
        //一个整数由四个字节组成
        byte[] num = new byte[4];
        byte[] tmp = value.ToBytes(false);
        for(int i = 0; i < 4; i++)
        {
            if (tmp.Length > i)
            {
                num[i] = tmp[i];
            }
            else
            {
                num[i] = (byte)0;
            }
        }
        //Debug.Log(num.ToDetail());
        return (num[3] << 24) + (num[2] << 16) + (num[1] << 8) + num[0];
    }
    //值类型拓展
    public static bool IsTrue(this uint value)
    {
        if (value == 0)
            return false;
        else
            return true;
    }

    public static bool IsTrue(this string value)
    {
        if(value=="0")
            return false;
        else
            return true;
    }
    /// <summary>
    /// 获得某个最大值百分比的具体数值
    /// </summary>
    /// <param name="value">当前数值</param>
    /// <param name="maxValue">最大值</param>
    /// <param name="ValueName">数值名字</param>
    /// <returns>返回的数值类型（浮点型或者整形）</returns>
    public static int ToChgPercentValue(this object value, uint maxValue,string ValueName)
    {
        if (value == null)
            return 0;
        if (value.GetType().Equals(typeof (float)))
        {
            //Loger.Log(ValueName + "改变：" + (float) value*(float) maxValue);
            return (int)((float) value*(float) maxValue);
        }
        else
        {
            //Loger.Log(ValueName + "改变：" + value);
            return (int) value;
        }
    }
    /// <summary>
    /// 获取二进制表示
    /// </summary>
    public static string ToBinary(this object value)
    {
        if (value == null) return "null";
        string str = "";
        Type type = value.GetType();
        byte[] buffer;
        if (type.Equals(typeof(bool)))
        {
            buffer = BitConverter.GetBytes((bool)value);
        }
        else if(type.Equals(typeof(char)))
        {
            buffer = BitConverter.GetBytes((char)value);
        }
        else if (type.Equals(typeof(int)))
        {
            buffer = BitConverter.GetBytes((int)value);
        }
        else if (type.Equals(typeof(long)))
        {
            buffer = BitConverter.GetBytes((long)value);
        }
        else if (type.Equals(typeof(int[])))
        {
            int[] tmp = value as int[];
            List<byte> list = new List<byte>();
            for(int i = 0; i < tmp.Length; i++)
            {
                list.AddRange(BitConverter.GetBytes(tmp[i]));
            }
            buffer = list.ToArray();
        }
        else if (type.Equals(typeof(byte[])))
        {
            buffer = value as byte[];
        }
        else
        {
            return "[unknow]"+type;
        }
        for (int i = buffer.Length - 1; i >= 0; i--)
        {
            str += String.Format("{0:X2}", buffer[i]) + " ";
        }
        return str;
    }
    /// <summary>
    /// 返回浮点型数据
    /// </summary>
    public static float ToChgPercentValue(this object value,string ValueName)
    {
        if (value == null)
            return 0;
        //Loger.Log(ValueName + "改变：" + (float)value);
        return ((float)value);
    }
    /// <summary>
    /// 判断是否是百分数
    /// </summary>
    public static object ToChgPercentValue(this string value,string ValueName)
    {
        if (value == "0")
            return float.Parse(value);

        //判断是否是百分比
        if (value.Contains("%"))
        {
            //Loger.Log(ValueName+":"+value+",转换："+float.Parse(value.Replace("%", ""))*0.01f);
            return float.Parse(value.Replace("%", ""))*0.01f;
        }
        else
        {
           // Loger.Log(ValueName + ":" + value + ",转换：" +  float.Parse(value));
            return int.Parse(value);
        }
    }


    private const float PRECISION = 0.000001f;
    //判断浮点数是否为0
    public static bool FloatIszero(this float x)
    {
        if (Math.Abs(x) <= PRECISION)
        {
            //浮点数x值为0
            return true;
        }
        else
        {
            //浮点数x值不为0
            return false;
        }
    }

    public static int GetNumberInt(this string str)
    {
        int result = 0;
        if (str != null && str != string.Empty)
        {
            // 正则表达式剔除非数字字符（不包含小数点.） 
            str = Regex.Replace(str, @"[^\d.\d]", "");
            // 如果是数字，则转换为decimal类型 
            if (Regex.IsMatch(str, @"^[+-]?\d*[.]?\d*$"))
            {
                result = int.Parse(str);
            }
        }
        return result;
    }
    #endregion
    #region 字典拓展
    public static string ToDetail<TKey, TValue>(this Dictionary<TKey, TValue> dict)
    {
        string str = null;
        foreach (var item in dict)
        {
            str += "<" + item.Key + ">" + "{" + item.Value.ToString() + "}" + "\n";
        }
        return str;
    }
    public static TValue GetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue value)
    {
        TValue v;
        if(dict.TryGetValue(key,out v))
        {
            return v;
        }
        else
        {
            dict.Add(key,value);
            return value;
        }
    }
    /// <summary>
    /// 尝试将键和值添加到字典中：如果不存在，才添加；存在，不添加也不抛导常
    /// </summary>
    public static Dictionary<TKey, TValue> TryAdd<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue value)
    {
        if (dict.ContainsKey(key) == false) dict.Add(key, value);
        return dict;
    }
    /// <summary>
    /// 将键和值添加或替换到字典中：如果不存在，则添加；存在，则替换
    /// </summary>
    public static Dictionary<TKey, TValue> AddOrUpdate<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue value)
    {
        dict[key] = value;
        return dict;
    }
    #endregion

    #region Null
    #endregion
}