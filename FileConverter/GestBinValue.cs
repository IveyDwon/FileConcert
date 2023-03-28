using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;


namespace FileConverter
{
    public class GestBinValue
    {
        static string vName;
        static string eName;
        static int bintype = 0;

        public static List<Segment> GetSegmentsFromFiles(List<string> fileNames, string vehicleName, string ecuName, int BinType)
        {
            vName = vehicleName;
            eName = ecuName;
            bintype = BinType;

            List<Segment> segs = new List<Segment>();
            foreach (string f in fileNames)
            {
                segs.AddRange(GestBinData(f));
            }
            return segs;
        }


        /// <summary>
        /// 解析原始文件，或者起始地址和数据块
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static List<Segment> GestBinData(string path)
        {
            List<Segment> segmentList = new List<Segment>();
            string oldAddress = "";
            string fileName = path;// @"C:\Users\Administrator\Desktop\531 1.4T 8P17_V9\531 1.4T 8P17_V9.cut";

            string xmlPath = AppDomain.CurrentDomain.BaseDirectory + "Reprogramming.xml";
            string xpath = @"//Vehicle[@Name='" + vName + "']/Family[@Name='" + eName + "']/ECU/DriverFile";
            if (bintype == 0)
            {
                //驱动文件
                xpath = @"//Vehicle[@Name='" + vName + "']/Family[@Name='" + eName + "']/ECU/DriverFile";
            }
            else
            {
                xpath = @"//Vehicle[@Name='" + vName + "']/Family[@Name='" + eName + "']/ECU/ApplicationFile";
            }

            XmlDocument doc = new XmlDocument();
            doc.Load(xmlPath);

            XmlNode node = doc.SelectSingleNode(xpath);
            if (node == null)
            {
                return null;
            }
            string startAdd = node.Attributes["StartAddress"] != null ? node.Attributes["StartAddress"].Value.ToString() : "";
            string SegmentOffset = node.Attributes["SegmentOffset"] != null ? node.Attributes["SegmentOffset"].Value.ToString() : "";
            string BlockSize = node.Attributes["BlockSize"] != null ? node.Attributes["BlockSize"].Value.ToString() : "";

            byte[] allBytes = File.ReadAllBytes(path);
            if (bintype == 0)
            {
                Segment sg = new Segment();
                sg.startAddress = startAdd;
                //实际在BIN文件中，第一个字节表示段数，后面的4+4 表示的是起始地址和长度，此处作弊
                string Head = node.Attributes["Head"] != null ? node.Attributes["Head"].Value.ToString() : "";
                if (Head.ToUpper().Equals("TRUE"))
                {
                    sg.length = ((allBytes.Length - 9)).ToString("X4");
                    sg.data = new StringBuilder();
                    sg.blockSize = BlockSize;
                    byte[] newByte = new byte[allBytes.Length - 9];
                    Array.Copy(allBytes, 9, newByte, 0, allBytes.Length - 9);

                    sg.data.Append(Utils.byteToHexStr(newByte));
                    segmentList.Add(sg);
                }
                else
                {
                    sg.length = (allBytes.Length).ToString("X4");
                    sg.data = new StringBuilder();
                    sg.blockSize = BlockSize;
                    sg.data.Append(Utils.byteToHexStr(allBytes));
                    segmentList.Add(sg);
                }
            }
            else
            {
                int indexSegmentOffset = Convert.ToInt32(SegmentOffset);

                int segMentCount = Convert.ToInt32(allBytes[indexSegmentOffset]);
                int addressInfoLength = segMentCount * 8;

                int firstIndex = 2 * (indexSegmentOffset + 1 + addressInfoLength);
                int header = 2 * (indexSegmentOffset + 1);
                string alldatas = Utils.byteToHexStr(allBytes);
                for (int i = 0; i < segMentCount; i++)
                {
                    string start = alldatas.Substring(header + 2 * i * 8, 8);
                    string datalength = alldatas.Substring(header + 2 * i * 8 + 8, 8);
                    int _datalength = Convert.ToInt32(datalength, 16) * 2;
                    string data = alldatas.Substring(firstIndex, _datalength);
                    firstIndex += _datalength;

                    Segment sg = new Segment();
                    sg.startAddress = start;
                    sg.length = (data.Length / 2).ToString("X4");
                    sg.data = new StringBuilder();
                    sg.data.Append(data);
                    sg.blockSize = BlockSize;
                    segmentList.Add(sg);
                }

            }

            return segmentList;
        }
    }
}
