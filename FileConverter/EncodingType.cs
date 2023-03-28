using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FileConverter
{
    public class EncodingType
    //编码问题目前为止，基本上没人解决，就连windows的IE的自动识别有时还识别错编码呢。
    //如果文件有BOM则判断，如果没有就用系统默认编码，缺点：没有BOM的非系统编码文件会显示乱码。   
    //调用方法： EncodingType.GetType(filename)   

    {
        public static System.Text.Encoding GetType(string FILE_NAME)
        {
            FileStream fs = new System.IO.FileStream(FILE_NAME, System.IO.FileMode.Open,
FileAccess.ReadWrite, FileShare.ReadWrite);
            System.Text.Encoding r = GetType(fs);
            fs.Close();
            return r;
        }
        public static System.Text.Encoding GetType(FileStream fs)
        {
            /*byte[] Unicode=new byte[]{0xFF,0xFE};  
            byte[] UnicodeBIG=new byte[]{0xFE,0xFF};  
            byte[] UTF8=new byte[]{0xEF,0xBB,0xBF};*/

            BinaryReader r = new BinaryReader(fs, System.Text.Encoding.Default);
            byte[] ss = r.ReadBytes(3);
            r.Close();
            //编码类型 Coding=编码类型.ASCII;   
            if (ss[0] >= 0xEF)
            {
                if (ss[0] == 0xEF && ss[1] == 0xBB && ss[2] == 0xBF)
                {
                    return System.Text.Encoding.UTF8;
                }
                else if (ss[0] == 0xFE && ss[1] == 0xFF)
                {
                    return System.Text.Encoding.BigEndianUnicode;
                }
                else if (ss[0] == 0xFF && ss[1] == 0xFE)
                {
                    return System.Text.Encoding.Unicode;
                }
                else
                {
                    return System.Text.Encoding.Default;
                }
            }
            else
            {
                return System.Text.Encoding.Default;
            }
        }
    }
}
