using FileConverter;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace FileConApp
{
    public partial class Form1 : Form
    {
        string sourceFileName = "";
        string destinationFilePath = "";
        int Filetype = 0;
        int binType = 0;
        public Form1()
        {
            InitializeComponent();
            this.panel1.Visible = false;
            this.comboBox1.SelectedIndex = 0;
            this.comboBox2.SelectedIndex = 1;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();            
            string strLocation = this.GetType().Assembly.Location;
            openFileDialog1.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            openFileDialog1.Title = "打开待转换文件";
            openFileDialog1.FileName = "";

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    sourceFileName = openFileDialog1.FileName;
                    this.textBox1.Text = sourceFileName;
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog openDiag = new FolderBrowserDialog();
            if (DialogResult.OK == openDiag.ShowDialog())
            {
                string path = openDiag.SelectedPath;
                destinationFilePath = path;
                this.textBox2.Text = destinationFilePath;
            }
        }

        /// <summary>
        /// 转换
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button3_Click(object sender, EventArgs e)
        {


            string fileName = sourceFileName;
            string outFilePath = destinationFilePath;

            if (string.IsNullOrWhiteSpace(fileName) || string.IsNullOrWhiteSpace(outFilePath))
            {
                MessageBox.Show("Please select the source file and save directory.");
                return;
            }

           
            List<Segment> segments = new List<Segment>();
            List<string> files=new List<string>();
            files.Add(fileName);
            string fileType = "moto";
            Encoding reVal1 = EncodingType.GetType(fileName);
            string[] temps= File.ReadAllLines(fileName, reVal1);
            if(temps[0].StartsWith(":"))
            {
                fileType = "intel";
            }
            if (Filetype == 0)
            {
                if (fileType.Equals("moto"))
                {
                    segments = FileConverter.GestMotoValue.GetSegmentsFromFiles(files, outFilePath);
                }
                else
                {
                    segments = FileConverter.GestIntelValue.GetSegmentsFromFiles(files, outFilePath);
                }
            }
            else if (Filetype == 1)
            {
                string vehicleName = this.comboBox1.Text.Trim();
                string ecuName = this.comboBox2.Text.Trim();
                if (string.IsNullOrWhiteSpace(vehicleName) || string.IsNullOrWhiteSpace(ecuName))
                {
                    MessageBox.Show("请选择车型及ECU");
                    return;
                }
                segments = FileConverter.GestBinValue.GetSegmentsFromFiles(files,vehicleName,ecuName,binType);
            }

            string[] fileNames = Directory.GetFiles(outFilePath,"*.DEL");
            for (int i = fileNames.Length-1; i >= 0; i--)
            {
                File.Delete(fileNames[i]);
            }
            if (Filetype == 1)
            {
                foreach (Segment s in segments)
                {
                    int address = Convert.ToInt32(s.startAddress.ToString(), 16);
                    int length = Convert.ToInt32(s.length.ToString(), 16);
                    int blocksize = Convert.ToInt32(s.blockSize);
                    string name = outFilePath + "\\" + address.ToString("X8") + "_" + length.ToString("X8") + ".DEL";
                    string header =(blocksize+2).ToString("X2");
                    int blockCount = (s.data.Length / 2) / blocksize;
                    
                    StringBuilder HeaderStr = new StringBuilder();
                    HeaderStr.Append("1");
                    for (int i = 3 - header.Length; i > 0; i--)
                    {
                        HeaderStr.Append("0");
                    }
                    HeaderStr.Append(header);
                    ///整除部分
                    List<string> tempData = new List<string>();
                    byte[] data = new byte[s.data.Length / 2 + blockCount * 2];
                    int j = 1;
                    for (int i = 0; i < blockCount; i++)
                    {
                        if (j > 0xFF)
                        {
                            j = 0;
                        }
                        tempData.Add(HeaderStr+"36"+j.ToString("X2")+s.data.ToString().Substring(i * blocksize*2, blocksize*2));
                        j++;
                    }
                    string fixHex = Properties.Settings.Default.FixHex.ToString();

                    ///剩余的部分
                    int num = (s.data.Length / 2) % blocksize;
                    if (num >0)
                    {
                        StringBuilder dataLeft = new StringBuilder();
                        //dataLeft.Append(s.data.ToString().Substring(s.data.Length - blockCount * blocksize));
                        dataLeft.Append(s.data.ToString().Substring(s.data.Length - num * 2, num * 2));
                        tempData.Add("10"+dataLeft.Length.ToString("X2") + "36" + j.ToString("X2") + dataLeft.ToString());
                    }
                    #region 根据需求，需要将长帧拆分为单帧
                    #endregion


                    List<string> newDataList = cutFlashData(tempData, fixHex);
                    StringBuilder templist = new StringBuilder();
                    foreach(string ss in newDataList)
                    {
                        templist.Append(ss);
                    }
                    byte[] tempBytes = Utils.Hex2ByteArr(templist.ToString());
                    System.IO.File.WriteAllBytes(name, tempBytes);
                }
            }
            else
            {
                foreach (Segment s in segments)
                {
                    int address = Convert.ToInt32(s.startAddress.ToString(), 16);
                    int length = Convert.ToInt32(s.length.ToString(), 16);
                    string name = outFilePath + "\\" + address.ToString("X8") + "_" + length.ToString("X8") + ".txt";
                    //byte[] data = new byte[s.data.Length / 2];
                    //data = Utils.Hex2ByteArr(s.data.ToString());
                    System.IO.File.WriteAllText(name, s.data.ToString(), reVal1);
                }
            }
            MessageBox.Show("Convert completed。", "Tips", MessageBoxButtons.OK, MessageBoxIcon.Information);    
            if (openDir)
            {
                DirectoryInfo DirPath =new DirectoryInfo(textBox2.Text);
                System.Diagnostics.Process.Start("explorer.exe", DirPath.ToString());
            }
              
               
        }

        private static List<string> cutFlashData(List<string> tempData, string fixHex)
        {
            List<string> rArrayList = new List<string>();
            for (int i = 0; i < tempData.Count; i++)
            {
                byte[] data = Utils.Hex2ByteArr(tempData[i]);

                int frameCount = (data.Length - 8) / 7;

                int left = (data.Length - 8) % 7;

                ///补充不足数据
                byte[] newData = new byte[data.Length + 7-(7 - left)];

                Array.Copy(data, 0, newData, 0, data.Length);
                byte[] finalyData = null;
                if (left > 0)
                {
                    for (int x = 0; x < 7 - left; x++)
                    {
                        byte fix = Convert.ToByte(fixHex, 16);
                        newData[data.Length + x] = fix;
                    }
                    finalyData = new byte[newData.Length + (frameCount + 1)];
                    frameCount += 1;
                }
                else
                {
                    finalyData = new byte[newData.Length + frameCount];
                }
                //需要添加的分隔符为frameCount + 1个；
                //切割数据
                
                byte y = 0x21;

                Array.Copy(newData, 0, finalyData, 0, 8);//前8个直接赋值

                for (int x = 0; x < frameCount; x++)
                {
                    if (y > 0x2f)
                    {
                        y = 0x20;
                    }
                    finalyData[x * 8 + 8] = y;
                    Array.Copy(newData, 8 + x * 7, finalyData, x * 8 + 9, 7);
                    y++;
                }
                rArrayList.Add(Utils.byteToHexStr(finalyData));

            }

            return rArrayList;
           
        }

       
        bool openDir = true;
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                openDir = true;
            }
            else
            {
                openDir = false;
            }
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            Filetype = 1;
            this.panel1.Visible = true;
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            Filetype = 0;
            this.panel1.Visible = false;
        }

        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {
            binType = 0;
        }

        private void radioButton4_CheckedChanged(object sender, EventArgs e)
        {
            binType = 1;
        }  
    }
}
