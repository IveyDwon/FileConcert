using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FileConverter
{
    public class GestIntelValue
    {

        public static List<Segment> GetSegmentsFromFiles(List<string> fileNames,string outFilePath)
        {
            List<Segment> segs = new List<Segment>();
            foreach (string f in fileNames)
            {
                segs.AddRange(GestIntelData(f, outFilePath));
            }
            return segs;
        }

        /// <summary>
        /// 解析原始文件，或者起始地址和数据块
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static List<Segment> GestIntelData(string path, string outFilePath)
        {

            List<Segment> segmentList = new List<Segment>();
            string filePath = outFilePath;
            if (!Directory.Exists(filePath))
            {
                Directory.CreateDirectory(filePath);
            }
            string logPath = filePath + "\\log_" + path.Split('\\').LastOrDefault().Replace(path.Split('\\').LastOrDefault().Split('.').LastOrDefault(), "") + ".txt";
            Encoding reVal = EncodingType.GetType(path);
            try
            {
                using (StreamWriter sw = File.AppendText(logPath))
                {
                    try
                    {
                        int lineNumber = 1;
                        Dictionary<string, StringBuilder> segments = new Dictionary<string, StringBuilder>();
                        string oldAddress = "00000000";
                        string fileName = path;// @"C:\Users\Administrator\Desktop\531 1.4T 8P17_V9\531 1.4T 8P17_V9.cut";
                        sw.WriteLine("FilePath：" + path);
                        sw.WriteLine("FileEncoding：" + reVal.EncodingName.ToString());
                        sw.WriteLine("Date：" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        string[] allLines = File.ReadAllLines(fileName, reVal);
                        foreach (string line in allLines)
                        {
                            sw.WriteLine("Line：" + lineNumber);
                            sw.WriteLine("Data：" + line);
                            StringBuilder value = new StringBuilder();
                            bool isContinue = true;
                            #region
                            // Intel HEX file可以包含任意多行记录（record），每个record主要由5个部分（域）组成，每部分至少包含两个十六进制字符，即一个字节（8位），
                            //其具体形式为“ :llaaaatt[dd...]cc ”
                            //“:”表示record的开始
                            //“ll”表示record中数据位（dd）的长度(几个字节)
                            //“aaaa”表示record中的数据存储起始地址
                            //“tt”表示record类型，可以为00（数据record），01（文件结束record），02（扩展段地址record），04（扩展线性地址record）
                            //“dd”表示record数据的一位，一个record可能包含多个数据字节，数据字节的数量必须与ll中指定的相一致
                            //“cc”表示record的校验域，

                            //:02               0000                                  02                           1000       FC
                            //  数据长度   相对地址(此处无意义)  扩展段地址类型  段地址   Checksum

                            //:20                0000          00         1C8A03002E9A0300F010000070A9FFFF010000002E8A03004C9A0300F0110000    B3
                            // 数据长度    相对地址  数据    实际数据(长度0x20)                    Checksum
                            //    该段数据的实际地址为：段地址<<4 + 相对地址 =0x1000<<4 + 0x0000 =0x10000

                            //:20               0020           00         70A9FFFF010000003A8A0300949A0300F012000070A9FFFF01000000468A0300    C3
                            // 数据长度    相对地址  数据    实际数据(长度0x20)                                                                                                                   Checksum
                            // 该段数据的实际地址为：段地址<<4 + 相对地址 =0x1000<<4 + 0x0020 =0x10020

                            // :00               0000                                         01                           FF
                            // 数据长度   相对地址(此处无意义)   文件结束类型           Checksum
                            #endregion
                            string headTypeStr = line.Substring(7, 2).ToUpper();//数据类型
                            string dataLength = "";//数据长度
                            string relativeAddress = "";//相对地址
                            string segAddress = "";//段地址                 
                            string data = "";//数据       

                            switch (headTypeStr)
                            {
                                //00 – 数据记录
                                //01 – 文件结束记录
                                //02 – 扩展段地址记录
                                //04 – 扩展线性地址记录
                                case "00":
                                    //数据记录
                                    relativeAddress = line.Substring(3, 4);
                                    dataLength = line.Substring(1, 2);
                                    int count = Convert.ToInt32(dataLength, 16) * 2;
                                    data = line.Substring(1 + 2 + 4 + 2, count);
                                    Int64 StartAdd = Convert.ToInt64(oldAddress, 16);
                                    Int64 relativeAdd = Convert.ToInt64(relativeAddress, 16);

                                    string Address = (StartAdd + relativeAdd).ToString("X8");
                                    StringBuilder temp = new StringBuilder();
                                    temp.Append(data);
                                    if (!segments.ContainsKey(Address))
                                    {
                                        segments.Add(Address, temp);
                                    }
                                    else
                                    {
                                        segments[Address].Append(temp);
                                    }

                                    break;
                                case "01":
                                    //文件结束记录
                                    isContinue = false;

                                    break;
                                case "04":
                                    //02 – 扩展段地址记录
                                    //:02               0000                                  02                           1000       FC
                                    //  数据长度   相对地址(此处无意义)  扩展段地址类型  段地址   Checksum
                                    relativeAddress = line.Substring(3, 4);
                                    segAddress = line.Substring(1 + 2 + 4 + 2, 4) + "0000";
                                    oldAddress = segAddress;

                                    break;
                                case "02":
                                    //扩展线性地址记录

                                    //:02               0000                                  04                           1000       FC
                                    //  数据长度   相对地址(此处无意义)  扩展线性类型  段地址   Checksum
                                    relativeAddress = line.Substring(3, 4);
                                    segAddress = line.Substring(1 + 2 + 4 + 2, 4) + "0";
                                    oldAddress = segAddress;

                                    break;

                                case "05":
                                    break;
                                case "06":
                                    break;
                                case "07":
                                case "08":
                                case "09":
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
                    catch (Exception EX)
                    {
                        sw.WriteLine("Exception");
                        sw.WriteLine(EX.Message);
                    }
                    finally
                    {
                        sw.Flush();
                        sw.Close();
                        sw.Dispose();
                    }
                   
                }
            }
            catch (Exception ex)
            {

               
            }
            return segmentList;

        }


    }
}
