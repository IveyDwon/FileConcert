using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FileConverter
{
    public class Segment
    {
        /// <summary>
        /// 起始地址，16进制字符串
        /// </summary>
        public string startAddress { get; set; }
        /// <summary>
        /// 数据长度,字节数，16进制字符串
        /// </summary>
        public string length { get; set; }
        /// <summary>
        /// 数据内容，16进制字符串
        /// </summary>
        public StringBuilder data { get; set; }
        /// <summary>
        /// block值
        /// </summary>
        public string blockSize { get; set; }

    }
}
