using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FileConverter
{
    public class GestMotoValue
    {

        public static List<Segment> GetSegmentsFromFiles(List<string> fileNames, string outFilePath)
        {
            List<Segment> segs = new List<Segment>();
            foreach (string f in fileNames)
            {
                segs.AddRange(GetMotololaData(f, outFilePath));
            }
            return segs;
        }


        /// <summary>
        /// 解析原始文件，或者起始地址和数据块
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static List<Segment> GetMotololaData(string path, string outFilePath)
        {
            List<Segment> segmentList = new List<Segment>();
            string filePath = outFilePath;
            if (!Directory.Exists(filePath))
            {
                Directory.CreateDirectory(filePath);
            }
            string logPath = filePath + "\\log_" + path.Split('\\').LastOrDefault().Replace(path.Split('\\').LastOrDefault().Split('.').LastOrDefault(),"") + ".txt";
            Encoding reVal = EncodingType.GetType(path);
            using (StreamWriter sw = File.AppendText(logPath))
            {
                try
                {
                    Dictionary<string, StringBuilder> segments = new Dictionary<string, StringBuilder>();

                    string fileName = path;// @"C:\Users\Administrator\Desktop\531 1.4T 8P17_V9\531 1.4T 8P17_V9.cut";
                    string[] allLines = File.ReadAllLines(fileName, reVal);
                    int len = 0;
                    int lineNumber = 1;
                    sw.WriteLine("FilePath：" + path);
                    sw.WriteLine("FileEncoding：" + reVal.EncodingName.ToString());
                    sw.WriteLine("Date：" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    foreach (string line in allLines)
                    {
                        sw.WriteLine("Line：" + lineNumber);
                        sw.WriteLine("Data：" + line);
                        StringBuilder value = new StringBuilder();
                        bool isContinue = true;
                        //type(类型)：2个字符。用来描述记录的类型 (S0，S1，S2，S3，S5，S7，S8，S9)。
                        //count(计数)：2个字符。 用来组成和说明了一个16进制的值，显示了在记录中剩余成对字符的计数。
                        //address(地址)：4或6或8个字节。用来组成和说明了一个16进制的值，显示了数据应该装载的地址， 这部分的长度取决于载入地址的字节数。2个字节的地址占用4个字符，3个字节的地址占用6个字符，4个字节的地址占用8个字符。
                        //data(数据)：0—64字符。用来组成和说明一个代表了内存载入数据或者描述信息的16进制的值。
                        //checksum(校验和)：2个字符。
                        //S0Record：记录类型是“S0”(0x5330)。地址场没有被用，用零置位(0x0000)。数据场中的信息被划分为以下四个子域：  name(名称)：20个字符，用来编码单元名称
                        // ver(版本)：2个字符，用来编码版本号    rev(修订版本)：2个字符，用来编码修订版本号      description(描述)：0-36个字符，用来编码文本注释此行表示程序的开始，不需烧入memory。
                        //S1Record：记录类型是“S1”(0x5331)。地址场由2个字节地址来说明。数据场由可载入的数据组成。
                        //S2Record：记录类型是“S2”(0x5332)。地址场由3个字节地址来说明。数据场由可载入的数据组成。
                        //S3Record：记录类型是“S3”(0x5333)。地址场由4个字节地址来说明。数据场由可载入的数据组成。
                        //S5Record：记录类型是“S5”(0x5335)。地址场由2字节的值说明，包含了先前传输的S1、S2、S3记录的计数。没有数据场。
                        //S7Record：记录类型是“S7”(0x5337)。地址场由4字节的地址说明，包含了开始执行地址。没有数据场。此行表示程序的结束，不需烧入memory。
                        //S8Record：记录类型是“S8”(0x5338)。地址场由3字节的地址说明，包含了开始执行地址。没有数据场。此行表示程序的结束，不需烧入memory。
                        //S9Record：记录类型是“S9”(0x5339)。地址场由2字节的地址说明，包含了开始执行地址。没有数据场。此行表示程序的结束，不需烧入memory。
                        //根据不同的描述信息，在以上三种不同的结束行中选择一种使用

                        string headStr = line.Substring(0, 2).ToUpper();

                        string head = "";
                        string count = "";
                        string add = "";
                        string data = "";

                        switch (headStr)
                        {

                            case "S0":
                                //程序说明，丢弃
                                break;
                            case "S1":
                                //2+2+4+...+2;
                                //S1 23 C000 CF1400790011CC09395B105A124A8046304A8000300001C01BCB731400073400 27
                                //色块图例：type   count   address   data   checksum
                                #region S1
                                head = line.Substring(0, 2);
                                count = line.Substring(2, 2);
                                add = line.Substring(4, 4);
                                data = line.Substring(8, line.Length - 2 - 2 - 4 - 2);
                                len += data.Length / 2;
                                if (!segments.ContainsKey(add))
                                {
                                    value.Append(data);
                                    segments.Add(add, value);
                                }
                                else
                                {
                                    segments[add].Append(data);
                                }
                                #endregion
                                break;
                            case "S2":
                                //2+2+6+...+2;
                                #region S2
                                head = line.Substring(0, 2);
                                count = line.Substring(2, 2);
                                add = line.Substring(4, 6);
                                data = line.Substring(10, line.Length - 2 - 2 - 6 - 2);
                                len += data.Length / 2;
                                if (!segments.ContainsKey(add))
                                {
                                    value.Append(data);
                                    segments.Add(add, value);
                                }
                                else
                                {
                                    segments[add].Append(data);
                                }
                                #endregion
                                break;
                            case "S3":
                                //2+2+8+...+2;
                                #region S3
                                head = line.Substring(0, 2);
                                count = line.Substring(2, 2);
                                add = line.Substring(4, 8);
                                data = line.Substring(12, line.Length - 2 - 2 - 8 - 2);
                                len += data.Length / 2;
                                if (!segments.ContainsKey(add))
                                {
                                    value.Append(data);
                                    segments.Add(add, value);
                                }
                                else
                                {
                                    segments[add].Append(data);
                                }
                                #endregion
                                break;
                            case "S4":
                                break;
                            case "S5":
                                break;
                            case "S6":
                                break;
                            case "S7":
                            case "S8":
                            case "S9":
                                //结束
                                isContinue = false;
                                break;
                            default:
                                break;

                        }
                        if (!isContinue)
                        {
                            break;
                        }
                        lineNumber++;
                    }

                    Dictionary<string, StringBuilder> tempDic = Utils.MegreData(ref segments);
                    foreach (string key in tempDic.Keys)
                    {
                        Segment seg = new Segment();
                        seg.startAddress = key;
                        seg.data = tempDic[key];
                        if (seg.data.Length == 0)
                        {
                            continue;
                        }
                        seg.length = (seg.data.Length / 2).ToString("X2");
                        segmentList.Add(seg);
                    }
                    sw.WriteLine("**************************************************");

                }
                catch (Exception ex)
                {
                    sw.WriteLine("Exception");
                    sw.WriteLine(ex.Message);
                }
                finally
                {
                    sw.Flush();
                    sw.Close();
                    sw.Dispose();
                }
            }
            return segmentList;
        }
    }

}
