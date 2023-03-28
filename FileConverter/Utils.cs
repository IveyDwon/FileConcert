using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FileConverter
{
    public class Utils
    {
        /// <summary> 
        /// 16进制字符串转为16进制字符数组 
        /// </summary> 
        /// <param name="newString"></param> 
        /// <returns></returns> 
        public static byte[] Hex2ByteArr(string newString)
        {
            int len = newString.Length / 2;
            byte[] arr = new byte[len];
            for (int i = 0; i < len; i++)
            {
                arr[i] = Convert.ToByte(newString.Substring(i * 2, 2), 16);
            }
            return arr;
        }

        /// <summary> 
        /// 字符串转16进制字节数组 
        /// </summary> 
        /// <param name="hexString"></param> 
        /// <returns></returns> 
        private static byte[] strToToHexByte(string hexString)
        {
            hexString = hexString.Replace(" ", "");
            if ((hexString.Length % 2) != 0)
                hexString += " ";
            byte[] returnBytes = new byte[hexString.Length / 2];
            for (int i = 0; i < returnBytes.Length; i++)
                returnBytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            return returnBytes;
        }

        /// <summary> 
        /// 字节数组转16进制字符串 
        /// </summary> 
        /// <param name="bytes"></param> 
        /// <returns></returns> 
        public static string byteToHexStr(byte[] bytes)
        {
            StringBuilder returnStr = new StringBuilder();
            if (bytes != null)
            {
                for (int i = 0; i < bytes.Length; i++)
                {
                    returnStr.Append(bytes[i].ToString("X2"));
                }
            }
            return returnStr.ToString();
        }

        /// <summary> 
        /// 从汉字转换到16进制 
        /// </summary> 
        /// <param name="s"></param> 
        /// <param name="charset">编码,如"utf-8","gb2312"</param> 
        /// <param name="fenge">是否每字符用逗号分隔</param> 
        /// <returns></returns> 
        public static string ToHex(string s, string charset, bool fenge)
        {
            if ((s.Length % 2) != 0)
            {
                s += " ";//空格 
                //throw new ArgumentException("s is not valid chinese string!"); 
            }
            System.Text.Encoding chs = System.Text.Encoding.GetEncoding(charset);
            byte[] bytes = chs.GetBytes(s);
            string str = "";
            for (int i = 0; i < bytes.Length; i++)
            {
                str += string.Format("{0:X}", bytes[i]);
                if (fenge && (i != bytes.Length - 1))
                {
                    str += string.Format("{0}", ",");
                }
            }
            return str.ToLower();
        }

        ///<summary> 
        /// 从16进制转换成汉字 
        /// </summary> 
        /// <param name="hex"></param> 
        /// <param name="charset">编码,如"utf-8","gb2312"</param> 
        /// <returns></returns> 
        public static string UnHex(string hex, string charset)
        {
            if (hex == null)
                throw new ArgumentNullException("hex");
            hex = hex.Replace(",", "");
            hex = hex.Replace("\n", "");
            hex = hex.Replace("\\", "");
            hex = hex.Replace(" ", "");
            if (hex.Length % 2 != 0)
            {
                hex += "20";//空格 
            }
            // 需要将 hex 转换成 byte 数组。 
            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                try
                {
                    // 每两个字符是一个 byte。 
                    bytes[i] = byte.Parse(hex.Substring(i * 2, 2), System.Globalization.NumberStyles.HexNumber);
                }
                catch
                {
                    // Rethrow an exception with custom message. 
                    throw new ArgumentException("hex is not a valid hex number!", "hex");
                }
            }
            System.Text.Encoding chs = System.Text.Encoding.GetEncoding(charset);
            return chs.GetString(bytes);
        }

        /// <summary>
        /// 合并一组特殊的数据
        /// </summary>
        /// <param name="segments"></param>
        /// <returns></returns>
        public static Dictionary<string, StringBuilder> MegreData(ref Dictionary<string, StringBuilder> segments)
        {
            Dictionary<string, StringBuilder> tempDic = new Dictionary<string, StringBuilder>();

            List<string> keys = segments.Keys.ToList();
            keys.Sort();
            int loopCount = keys.Count;
            List<bool> noMegredList = new List<bool>();

            for (int i = 0; i < loopCount; i++)
            {
                StringBuilder oldSB = new StringBuilder();
                StringBuilder tempSB = new StringBuilder();

                if (segments[keys[i]].Length == 0)
                {
                    continue;
                }
                else
                {
                    tempSB=segments[keys[i]];
                    oldSB = segments[keys[i]];
                    //初始化数据，用来合并
                   // tempSB.Append(str);
                    //oldSB.Append(str);
                }
                Int64 currenAddress = Convert.ToInt64(keys[i], 16);

                try
                {
                    //逐个循环合并
                    for (int j = 1; j < loopCount; j++)
                    {
                        Int64 NextAddress = Convert.ToInt64(keys[j], 16);

                        StringBuilder nextValue = segments[keys[j]];

                        if (nextValue.Length == 0)
                        {
                            //空行忽略
                            continue;
                        }
                        else if (currenAddress + tempSB.Length / 2 == NextAddress)//当前地址+数据长度=下一行长度
                        {
                            tempSB.Append(nextValue);

                            segments[keys[j]].Clear();
                        }
                    }

                }
                catch (Exception ex)
                { 
                
                }
                segments[keys[i]] = tempSB;

                if (isSameStringBuilder(oldSB,tempSB))
                {
                    noMegredList.Add(true);
                }
            }

            ///去掉空白行
            for (int i = 0; i < keys.Count; i++)
            {
                if (!tempDic.ContainsKey(keys[i]) && segments[keys[i]].Length > 0)
                {
                    tempDic.Add(keys[i], segments[keys[i]]);
                }
            }
            if (noMegredList.Count == tempDic.Count)//如果最终都不需要合并，那就退出
            {
                segments = tempDic;
                return tempDic;
            }
            MegreData(ref tempDic);
            return tempDic;
        }

        private static bool isSameStringBuilder(StringBuilder a, StringBuilder b)
        {
            if (a.Length != b.Length)
            {
                return false;
            }
            if (a.Equals(b))
            {
                return true;
            }

            return true;
        }
    }
}
