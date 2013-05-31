/***************************************************************************************************
 * 
 * Copyright 1998-2003 XiaoCui' Technology Co.,Ltd.
 * 
 * DESCRIPTION: 
 * 	   
 * modification history
 * --------------------
 * 01a, 12.03.2010, cuiqingwei written
 * --------------------
 **************************************************************************************************/

/*                   著作权所有(C) Edutech〈2009-2010〉〈开发者：崔庆伟〉

本程序为自由软件；您可依据自由软件基金会所发表的GNU通用公共授权条款规定，就本程序再为发布与／或修改；
无论您依据的是本授权的第二版或（您自行选择的）任一日后发行的版本。

本程序是基于使用目的而加以发布，然而不负任何担保责任；亦无对适售性或特定目的适用性所为的默示性担保。
详情请参照GNU通用公共授权。                                                                       */

#region 相关引用
using System;
using System.Data;
using System.Text;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using System.ComponentModel;
using System.Diagnostics;
using System.Collections.Generic;
using Telnet.Properties;
using System.Xml.Serialization;

using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Threading;
#endregion

namespace Telnet
{
    #region 公共枚举
    public enum DataMode   { Text, Hex }
    public enum LogMsgType { Incoming, Outgoing, Normal, Warning, Error, Copyright };
    #endregion

    public partial class frmTelnet : Form
    {
        #region 本地变量

        // The main control for communicating through the telnet port
        private Socket socket;
        private short[] parsedata = new short[10];
        protected bool sw_ugoahead;
        protected bool sw_igoahead;
        protected bool sw_echo;
        protected bool sw_termsent;

        // Various colors for logging info
        private Color[] LogMsgTypeColor = { Color.Blue, Color.Green, Color.Black, Color.Orange, Color.Red, Color.OrangeRed };

        // Temp holder for whether a key was pressed
        private bool KeyHandled = false;

        private int checkPrint;

        #endregion

        #region 构造
        public frmTelnet()
        {
            // Build the form
            InitializeComponent();

            // Restore the users settings
            InitializeControlValues();

            // Enable/disable controls based on the current state
            EnableControls();

            IPAddress[] AddressList = Dns.GetHostByName(Dns.GetHostName()).AddressList;

            string ip = AddressList[0].ToString();

            if (ip != "")
            {
                this.sbpLocIP.Text = "本地IP地址：" + ip;
            }
            else
            {
                this.sbpStatus.Text = "无法获得本机IP地址，请检查网络连接";
            }
        }
        #endregion

        #region 自定义函数

        /// <summary> Save the user's settings. </summary>
        private void SaveSettings()
        {
            Settings.Default.HostIP = txtRemoteAddress.Text;
            Settings.Default.Port = txtPort.Text;
            Settings.Default.DataMode = CurrentDataMode;
            Settings.Default.Save();
        }
       
        private void loadAutomationButtonGroupConfig()
        {
            FileStream stream = new FileStream("Telnet.xml", FileMode.Open);
            XmlSerializer serializer = new XmlSerializer(typeof(buttonItemCollection));
            this.btnItems = (buttonItemCollection)serializer.Deserialize(stream);

            foreach (buttonItem item in this.btnItems.buttonItems)
            {
                int index = item.buttonID - 1;
                if (index < this.automationButtonGroup.Length)
                {
                    Button button = this.automationButtonGroup[index];
                    button.Text = item.buttonTitle;
                    Font font = new Font(this.btn1.Font.FontFamily, this.btn1.Font.Size, this.btn1.Font.Style);
                    SizeF ef = button.CreateGraphics().MeasureString("    " + item.buttonTitle + "    ", font, (SizeF)this.btn1.Size);
                    button.Width = ((int)ef.Width) * 4;
                }
            }
            stream.Close();
        }
        /// <summary> Populate the form's controls with default settings. </summary>
        private void InitializeControlValues()
        {
            this.automationButtonGroup = new Button[] { this.btn1, this.btn2, this.btn3, this.btn4, this.btn5, this.btn6,this.btn7,this.btn8};

            txtRemoteAddress.Text = Settings.Default.HostIP;
            txtPort.Text = Settings.Default.Port.ToString();
            CurrentDataMode = Settings.Default.DataMode;

            try
            {
                this.loadAutomationButtonGroupConfig();
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, "Automation XML file parser error. Details:" + exception.Message, "automation XML parser error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                base.Close();
            }

            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, 5000);

        }

        /// <summary> Enable/disable controls based on the app's current state. </summary>
        private void EnableControls()
        {
            // Enable/disable controls based on whether the port is open or not
            gbPortSettings.Enabled = !socket.Connected;
            gbCustomBtn.Enabled = socket.Connected;

            txtSendData.Enabled = btnSend.Enabled = socket.Connected;

            if (socket.Connected)
            {
                btnOpenPort.Text = "&Close Port";
            }
            else
            {
                btnOpenPort.Text = "&Open Port";
            }
        }

        /// <summary> Send the user's data currently entered in the 'send' box.</summary>
        private void SendData()
        {
            if (CurrentDataMode == DataMode.Text)
            {
                // Send the user's text straight out the port
                SocketSend(txtSendData.Text);

                // Show in the terminal window the user's text
                Log(LogMsgType.Outgoing,txtSendData.Text + "\n");
            }
            else
            {
                try
                {
                    // Convert the user's string of hex digits (ex: B4 CA E2) to a byte array
                    byte[] data = HexStringToByteArray(txtSendData.Text);

                    // Send the binary data out the port
                    SocketSend(data);

                    // Show the hex digits on in the terminal window
                    Log(LogMsgType.Outgoing, ByteArrayToHexString(data) + "\n");
                }
                catch (FormatException)
                {
                    // Inform the user if the hex string was not properly formatted
                    Log(LogMsgType.Error, "Not properly formatted hex string: " + txtSendData.Text + "\n");
                }
            }
            txtSendData.SelectAll();
        }

        /// <summary> Log data to the terminal window. </summary>
        /// <param name="msgtype"> The type of message to be written. </param>
        /// <param name="msg"> The string containing the message to be shown. </param>
        private void Log(LogMsgType msgtype, string msg)
        {
            switch(msgtype)
            {
                default:
                case LogMsgType.Copyright:
                    rtbConsole.Invoke(new EventHandler(delegate
                    {
                        rtbConsole.SelectedText = string.Empty;
                        rtbConsole.SelectionFont = new Font(rtbConsole.SelectionFont, FontStyle.Bold);
                        rtbConsole.SelectionColor = LogMsgTypeColor[(int)msgtype];
                        rtbConsole.AppendText(msg);
                        rtbConsole.ScrollToCaret();
                    }));
                    break;
                case LogMsgType.Outgoing:
                    rtbConsole.Invoke(new EventHandler(delegate
                    {
                        rtbConsole.SelectedText = string.Empty;
                        rtbConsole.SelectionFont = new Font(rtbConsole.SelectionFont, FontStyle.Bold);
                        rtbConsole.SelectionColor = LogMsgTypeColor[(int)msgtype];
                        rtbConsole.AppendText(DateTime.Now.ToLongTimeString());
                        rtbConsole.AppendText(" Tx:");
                        rtbConsole.AppendText(msg);
                        rtbConsole.ScrollToCaret();
                    }));
                    break;
                case LogMsgType.Incoming:
                    rtbConsole.Invoke(new EventHandler(delegate
                    {
                        rtbConsole.SelectedText = string.Empty;
                        rtbConsole.SelectionFont = new Font(rtbConsole.SelectionFont, FontStyle.Bold);
                        rtbConsole.SelectionColor = LogMsgTypeColor[(int)msgtype];
                        rtbConsole.AppendText(DateTime.Now.ToLongTimeString());
                        rtbConsole.AppendText(" Rx:");
                        rtbConsole.AppendText(msg);
                        rtbConsole.AppendText("\n");
                        rtbConsole.ScrollToCaret();
                    }));
                    break;
            }
           
        }

        /// <summary> Convert a string of hex digits (ex: E4 CA B2) to a byte array. </summary>
        /// <param name="s"> The string containing the hex digits (with or without spaces). </param>
        /// <returns> Returns an array of bytes. </returns>
        private byte[] HexStringToByteArray(string s)
        {
            s = s.Replace(" ", "");
            byte[] buffer = new byte[s.Length / 2];
            for (int i = 0; i < s.Length; i += 2)
            {
                buffer[i / 2] = (byte)Convert.ToByte(s.Substring(i, 2), 16);
            }
            
            return buffer;
        }

        /// <summary> Converts an array of bytes into a formatted string of hex digits (ex: E4 CA B2)</summary>
        /// <param name="data"> The array of bytes to be translated into a string of hex digits. </param>
        /// <returns> Returns a well formatted string of hex digits with spacing. </returns>
        private string ByteArrayToHexString(byte[] data)
        {
            StringBuilder sb = new StringBuilder(data.Length * 3);
            foreach (byte b in data)
            {
                sb.Append(Convert.ToString(b, 16).PadLeft(2, '0').PadRight(3, ' '));
            }
            
            return sb.ToString().ToUpper();
        }
        #endregion

        #region Socket
        //向Telnet服务器发送命令
        private void SocketSend(string msg)
        {
            System.Byte[] message = System.Text.Encoding.ASCII.GetBytes(msg.ToCharArray());
            socket.Send(message, message.Length, 0);
        }

        //向Telnet服务器发送命令
        private void SocketSend(char[] chr)
        {
            System.Byte[] message = System.Text.Encoding.ASCII.GetBytes(chr);
            socket.Send(message, message.Length, 0);
        }

        //向Telnet服务器发送数据
        private void SocketSend(byte[] data)
        {
            System.Byte[] message = data;
            socket.Send(message, message.Length, 0);
        }

        //接收数据
        private string Receive()
        {
            //用于接收数据的缓冲
            byte[] buf;
            string result = "";

            int count = socket.Available;
            if (count > 0)
            {
                buf = new byte[count];
                socket.Receive(buf);
                if (CurrentDataMode == DataMode.Hex)
                {
                    result = ByteArrayToHexString(buf);
                }
                else
                {
                    result = ProcessOptions(buf);
                }
            }

            return result;
        }
        #region
#if false
        //处理命令字符，buf是包含数据的缓冲
        private string ProcessOptions(byte[] buf)
        {
            string strNormal = "";
            int i = 0;
            while (i < buf.Length)
            {
                strNormal += System.Text.Encoding.Default.GetString(buf, i, 1);
                i++;
            }
            return strNormal;
        }
#else
        #region 常量
        const char GO_NORM = (char)0;
        const char SUSP = (char)237;

        const char ABORT = (char)238;
        const char SE = (char)240; //子选项结束Subnegotiation End
        const char NOP = (char)241;
        const char DM = (char)242; //Data Mark
        const char BREAK = (char)243; //BREAK
        const char IP = (char)244; //Interrupt Process
        const char AO = (char)245; //Abort Output
        const char AYT = (char)246; //Are you there
        const char EC = (char)247; //Erase character
        const char EL = (char)248; //Erase Line
        const char GOAHEAD = (char)249; //Go Ahead
        const char SB = (char)250; //子选项开始Subnegotiation Begin

        const char WILL = (char)251;
        const char WONT = (char)252;
        const char DO = (char)253;
        const char DONT = (char)254;
        const char IAC = (char)255;

        const char BINARY = (char)0;
        const char IS = (char)0;
        const char SEND = (char)1;
        const char ECHO = (char)1;
        const char RECONNECT = (char)2;
        const char SGA = (char)3;
        const char AMSN = (char)4;
        const char STATUS = (char)5;
        const char TIMING = (char)6;
        const char RCTAN = (char)7;
        const char OLW = (char)8;
        const char OPS = (char)9;
        const char OCRD = (char)10;
        const char OHTS = (char)11;
        const char OHTD = (char)12;
        const char OFFD = (char)13;
        const char OVTS = (char)14;
        const char OVTD = (char)15;
        const char OLFD = (char)16;
        const char XASCII = (char)17;
        const char LOGOUT = (char)18;
        const char BYTEM = (char)19;
        const char DET = (char)20;
        const char SUPDUP = (char)21;
        const char SUPDUPOUT = (char)22;
        const char SENDLOC = (char)23;
        const char TERMTYPE = (char)24;

        const char EOR = (char)25;
        const char TACACSUID = (char)26;
        const char OUTPUTMARK = (char)27;
        const char TERMLOCNUM = (char)28;
        const char REGIME3270 = (char)29;
        const char X3PAD = (char)30;
        const char NAWS = (char)31;
        const char TERMSPEED = (char)32;
        const char TFLOWCNTRL = (char)33;
        const char LINEMODE = (char)34;
        const char DISPLOC = (char)35;

        const char ENVIRON = (char)36;
        const char AUTHENTICATION = (char)37;
        const char UNKNOWN39 = (char)39;
        const char EXTENDED_OPTIONS_LIST = (char)255;
        const char RANDOM_LOSE = (char)256;

        const char CR = (char)13;	//回车
        const char LF = (char)10;	//换行
        const string BACK = "[P";
        #endregion
        //处理命令字符，buf是包含数据的缓冲
        private string ProcessOptions(byte[] buf)
        {
            string strNormal = "";
            int i = 0;
            while (i < buf.Length)
            {
                if (buf[i] == IAC)
                {
                    switch ((char)buf[++i])
                    {
                        case DO:
                            Console.Write("--------------接收到 DO ");
                            ProcessDo(buf[++i]);
                            break;
                        case DONT:
                            Console.Write("--------------接收到 DONT ");
                            ProcessDont(buf[++i]);
                            break;
                        case WONT:
                            Console.Write("--------------接收到 WONT ");
                            ProcessWont(buf[++i]);
                            break;
                        case WILL:
                            Console.Write("--------------接收到 WILL ");
                            ProcessWill(buf[++i]);
                            break;
                        case IAC:
                            //正常字符
                            strNormal += System.Text.Encoding.Default.GetString(buf, i, 1);
                            break;
                        case SB:
                            //子会话开始
                            int j = 0;
                            while (buf[++i] != SE)
                            {
                                parsedata[j++] = buf[i];
                            }
                            //子会话结束:
                            switch ((char)parsedata[0])
                            {
                                case TERMTYPE:
                                    break;
                                case TERMSPEED:
                                    if (parsedata[1] == 1)
                                    {
                                        Console.WriteLine("发送: SB TERMSPEED 57600,57600");
                                        SocketSend(IAC + SB + TERMSPEED + IS + "57600,57600" + IAC + SE);
                                    }
                                    break;
                            }
                            break;
                        default:
                            Console.WriteLine("无效的命令" + buf[1]);
                            i++;
                            break;
                    };
                }
                else
                {
                    //正常的文字
                    strNormal += System.Text.Encoding.Default.GetString(buf, i, 1);
                }
                i++;
            }
            return strNormal;
        }

        private void ProcessDo(short ch)
        {
            //处理DO，以WILL或者WONT响应
            switch ((char)ch)
            {
                case BINARY:
                    Console.WriteLine(BINARY);
                    SocketSend(new char[] { IAC, WONT, BINARY });
                    Console.WriteLine("发送: WONT BINARY");
                    break;
                case ECHO:
                    Console.WriteLine(ECHO);
                    SocketSend(new char[] { IAC, WONT, ECHO });
                    Console.WriteLine("发送: WONT ECHO");
                    break;
                case SGA:
                    Console.WriteLine(SGA);
                    if (!sw_igoahead)
                    {
                        SocketSend(new char[] { IAC, WILL, SGA });
                        Console.WriteLine("发送: WILL SGA");
                        sw_igoahead = true;
                    }
                    else
                    {
                        Console.WriteLine("不发送响应");
                    }
                    break;
                case TERMSPEED:
                    Console.WriteLine(TERMSPEED);
                    SocketSend(new char[] { IAC, WILL, TERMSPEED });
                    Console.WriteLine("发送: WILL TERMSPEED");

                    SocketSend(IAC + SB + TERMSPEED + (char)0 + "57600,57600" +
                                                          IAC + SE);
                    Console.WriteLine("发送:SB TERMSPEED 57600");
                    break;
                case TFLOWCNTRL:
                    Console.WriteLine(TFLOWCNTRL);
                    SocketSend(new char[] { IAC, WONT, TFLOWCNTRL });
                    Console.WriteLine("发送: WONT TFLOWCNTRL");
                    break;
                case LINEMODE:
                    Console.WriteLine(LINEMODE);
                    SocketSend(new char[] { IAC, WONT, LINEMODE });
                    Console.WriteLine("发送: WONT LINEMODE");
                    break;
                case STATUS:
                    Console.WriteLine(STATUS);
                    SocketSend(new char[] { IAC, WONT, STATUS });
                    Console.WriteLine("发送: WONT STATUS");
                    break;
                case TIMING:
                    Console.WriteLine(TIMING);
                    SocketSend(new char[] { IAC, WONT, TIMING });
                    Console.WriteLine("发送: WONT TIMING");
                    break;
                case DISPLOC:
                    Console.WriteLine(DISPLOC);
                    SocketSend(new char[] { IAC, WONT, DISPLOC });
                    Console.WriteLine("发送: WONT DISPLOC");
                    break;
                case ENVIRON:
                    Console.WriteLine(ENVIRON);
                    SocketSend(new char[] { IAC, WONT, ENVIRON });
                    Console.WriteLine("发送: WONT ENVIRON");
                    break;
                case UNKNOWN39:
                    Console.WriteLine(UNKNOWN39);
                    SocketSend(new char[] { IAC, WILL, UNKNOWN39 });
                    Console.WriteLine("发送: WILL UNKNOWN39");
                    break;
                case AUTHENTICATION:
                    Console.WriteLine(AUTHENTICATION);
                    SocketSend(new char[] { IAC, WONT, AUTHENTICATION });
                    Console.WriteLine("发送: WONT AUTHENTICATION");
                    Console.WriteLine("发送: SB AUTHENTICATION");
                    SocketSend(IAC + SB + AUTHENTICATION + (char)0 + (char)0 + (char)0 + (char)0 + "" + IAC + SE);
                    break;
                default:
                    Console.WriteLine("未知的选项");
                    break;
            }
        }

        //处理DONT
        private void ProcessDont(short ch)
        {
            switch ((char)ch)
            {
                case SE:
                    Console.WriteLine(SE);
                    Console.WriteLine("接收到: RECEIVED SE");
                    break;
                case ECHO:
                    Console.WriteLine(ECHO);
                    if (!sw_echo)
                    {
                        sw_echo = true;
                        SocketSend(new char[] { IAC, WONT, ECHO });
                        Console.WriteLine("发送: WONT ECHO");
                    }
                    break;
                case SGA:
                    Console.WriteLine(SGA);
                    if (!sw_ugoahead)
                    {
                        SocketSend(new char[] { IAC, WONT, SGA });
                        Console.WriteLine("发送: WONT SGA");
                        sw_ugoahead = true;
                    }
                    break;
                case TERMSPEED:
                    Console.WriteLine(TERMSPEED);
                    SocketSend(new char[] { IAC, WONT, TERMSPEED });
                    Console.WriteLine("发送: WONT TERMSPEED");
                    break;
                case TFLOWCNTRL:
                    Console.WriteLine(TFLOWCNTRL);
                    SocketSend(new char[] { IAC, WONT, TFLOWCNTRL });
                    Console.WriteLine("发送: WONT TFLOWCNTRL");
                    break;
                case STATUS:
                    Console.WriteLine(STATUS);
                    SocketSend(new char[] { IAC, WONT, STATUS });
                    Console.WriteLine("发送: WONT STATUS");
                    break;
                case TIMING:
                    Console.WriteLine(TIMING);
                    SocketSend(new char[] { IAC, WONT, TIMING });
                    Console.WriteLine("发送: WONT TIMING");
                    break;
                case DISPLOC:
                    Console.WriteLine(DISPLOC);
                    SocketSend(new char[] { IAC, WONT, DISPLOC });
                    Console.WriteLine("发送: WONT DISPLOC");
                    break;
                case ENVIRON:
                    Console.WriteLine(ENVIRON);
                    SocketSend(new char[] { IAC, WONT, ENVIRON });
                    Console.WriteLine("发送: WONT ENVIRON");
                    break;
                case UNKNOWN39:
                    Console.WriteLine(UNKNOWN39);
                    SocketSend(new char[] { IAC, WILL, UNKNOWN39 });
                    Console.WriteLine("发送: WILL UNKNOWN39");
                    break;
                default:
                    break;
            }
        }

        //处理WONT
        private void ProcessWont(short ch)
        {
            switch ((char)ch)
            {
                case ECHO:
                    Console.WriteLine(ECHO);
                    if (sw_echo)
                    {
                        sw_echo = false;
                        SocketSend(new char[] { IAC, DONT, ECHO });
                        Console.WriteLine("发送: DONT ECHO");
                    }
                    break;
                case SGA:
                    Console.WriteLine(SGA);
                    SocketSend(new char[] { IAC, DONT, SGA });
                    Console.WriteLine("发送: DONT SGA");
                    sw_igoahead = false;
                    break;
                case TERMSPEED:
                    Console.WriteLine(TERMSPEED);
                    SocketSend(new char[] { IAC, DONT, TERMSPEED });
                    Console.WriteLine("发送: DONT TERMSPEED");
                    break;
                case TFLOWCNTRL:
                    Console.WriteLine(TFLOWCNTRL);
                    SocketSend(new char[] { IAC, DONT, TFLOWCNTRL });
                    Console.WriteLine("发送: DONT TFLOWCNTRL");
                    break;
                case LINEMODE:
                    Console.WriteLine(LINEMODE);
                    SocketSend(new char[] { IAC, DONT, LINEMODE });
                    Console.WriteLine("发送: DONT LINEMODE");
                    break;
                case STATUS:
                    Console.WriteLine(STATUS);
                    SocketSend(new char[] { IAC, DONT, STATUS });
                    Console.WriteLine("发送: DONT STATUS");
                    break;
                case TIMING:
                    Console.WriteLine(TIMING);
                    SocketSend(new char[] { IAC, WONT, TIMING });
                    Console.WriteLine("发送: WONT TIMING");
                    break;
                case DISPLOC:
                    Console.WriteLine(DISPLOC);
                    SocketSend(new char[] { IAC, DONT, DISPLOC });
                    Console.WriteLine("发送: DONT DISPLOC");
                    break;
                case ENVIRON:
                    Console.WriteLine(ENVIRON);
                    SocketSend(new char[] { IAC, DONT, ENVIRON });
                    Console.WriteLine("发送: DONT ENVIRON");
                    break;
                case UNKNOWN39:
                    Console.WriteLine(UNKNOWN39);
                    SocketSend(new char[] { IAC, DONT, UNKNOWN39 });
                    Console.WriteLine("发送: DONT UNKNOWN39");
                    break;
                default:
                    Console.WriteLine("未知的选项");
                    break;
            }
        }

        //处理WILL，以DO或者DONT响应
        private void ProcessWill(short ch)
        {
            switch ((char)ch)
            {
                case ECHO:
                    Console.WriteLine(ECHO);
                    if (!sw_echo)
                    {
                        sw_echo = true;
                        SocketSend(new char[] { IAC, DO, ECHO });
                        Console.WriteLine("发送: DO ECHO");
                    }
                    break;
                case SGA:
                    Console.WriteLine(SGA);
                    if (!sw_ugoahead)
                    {
                        SocketSend(new char[] { IAC, DO, SGA });
                        Console.WriteLine("发送: DO SGA");
                        sw_ugoahead = true;
                    }
                    else
                    {
                        Console.WriteLine("不发送响应");
                    }
                    break;
                case TERMTYPE:
                    Console.WriteLine("TERMTYPE");
                    if (!sw_termsent)
                    {
                        SocketSend(new char[] { IAC, WILL, TERMTYPE });
                        SocketSend(IAC + SB + TERMTYPE + (char)0 + "VT100" + IAC + SE);
                        sw_termsent = true;
                        Console.WriteLine("发送: SB TERMTYPE VT100");
                    }
                    break;
                case TERMSPEED:
                    Console.WriteLine(TERMSPEED);
                    SocketSend(new char[] { IAC, DONT, TERMSPEED });
                    Console.WriteLine("发送: DONT TERMSPEED");
                    break;
                case TFLOWCNTRL:
                    Console.WriteLine(TFLOWCNTRL);
                    SocketSend(new char[] { IAC, DONT, TFLOWCNTRL });
                    Console.WriteLine("发送: DONT TFLOWCNTRL");
                    break;
                case LINEMODE:
                    Console.WriteLine(LINEMODE);
                    SocketSend(new char[] { IAC, WONT, LINEMODE });
                    Console.WriteLine("发送: WONT LINEMODE");
                    break;
                case STATUS:
                    Console.WriteLine(STATUS);
                    SocketSend(new char[] { IAC, DONT, STATUS });
                    Console.WriteLine("发送: DONT STATUS");
                    break;
                case TIMING:
                    Console.WriteLine(TIMING);
                    SocketSend(new char[] { IAC, DONT, TIMING });
                    Console.WriteLine("发送: DONT TIMING");
                    break;
                case DISPLOC:
                    Console.WriteLine(DISPLOC);
                    SocketSend(new char[] { IAC, DONT, DISPLOC });
                    Console.WriteLine("发送: DONT DISPLOC");
                    break;
                case ENVIRON:
                    Console.WriteLine(ENVIRON);
                    SocketSend(new char[] { IAC, DONT, ENVIRON });
                    Console.WriteLine("发送: DONT ENVIRON");
                    break;
                case UNKNOWN39:
                    Console.WriteLine(UNKNOWN39);
                    SocketSend(new char[] { IAC, DONT, UNKNOWN39 });
                    Console.WriteLine("发送: DONT UNKNOWN39");
                    break;
                default:
                    Console.WriteLine("未知的选项");
                    break;
            }
        }
#endif
        #endregion
        private void TelnetThread()
        {
            while (socket.Connected)
            {
                try
                {
                    string str = Receive();
                    str = str.Replace("\0", "");
                    string delim = "\b";
                    str = str.Trim(delim.ToCharArray());
                    if (str.Length > 0)
                    {
                        Console.WriteLine(str);

                        if (str == OUTPUTMARK + BACK)
                        {
                            //BackupSpace键处理
                            this.rtbConsole.ReadOnly = false;
                            int curpos = rtbConsole.SelectionStart;
                            this.rtbConsole.Select(curpos - 1, 1);
                            this.rtbConsole.SelectedText = "";
                            this.rtbConsole.ReadOnly = true;
                        }
                        else
                        {
                            Log(LogMsgType.Incoming, str);
                        }
                    }
                    Thread.Sleep(100);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
            this.sbpStatus.Text = "状态：已断开";
        }

        private void Connect()
        {
            sw_igoahead = false;
            sw_ugoahead = true;
            sw_igoahead = false;
            sw_echo = true;
            sw_termsent = false;

            Console.WriteLine("连接服务器" + txtRemoteAddress.Text + "...");

            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, 5000);

            IPAddress ipAdd = IPAddress.Parse(txtRemoteAddress.Text);
            int port = System.Convert.ToInt32(txtPort.Text);
            IPEndPoint hostEndPoint = new IPEndPoint(ipAdd, port);

            try
            {
                socket.Connect(hostEndPoint);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                this.sbpStatus.Text = "状态：服务器未准备好";
                return;
            }

            if (socket.Connected)
            {
                //更新状态
                this.sbpStatus.Text = "状态：已连接";
                this.sbpHost.Text = "服务器地址：" + txtRemoteAddress.Text;
             
                Thread thread = new Thread(new ThreadStart(this.TelnetThread));
                thread.Start();
                Control.CheckForIllegalCrossThreadCalls = false;
            }
        }

        private void Disconnect()
        {
            if (socket != null && socket.Connected)
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }
            this.sbpStatus.Text = "状态：断开连接...";

        }	

        #endregion

        #region 属性
        private DataMode CurrentDataMode
        {
            get
            {
                if (rbHex.Checked)
                {
                    return DataMode.Hex;
                }
                else
                {
                    return DataMode.Text;
                }
            }
            set
            {
                if (value == DataMode.Text)
                {
                    rbText.Checked = true;
                }
                else
                {
                    rbHex.Checked = true;
                }
            }
        }
        #endregion

        #region 事件处理程序
        private void lnkAbout_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            // Show the user the about dialog
            (new frmAbout()).ShowDialog(this);
        }
        private void linkUserGuide_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            e.Link.Visited = true;
            Process.Start("http://www.educationtek.com/download/Telnet%20User%20Guide.htm");
        }
        private void pbLogo_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("http://www.educationtek.com/");
        }
        private void frmTelnet_Shown(object sender, EventArgs e)
        {
            this.Log(LogMsgType.Copyright, string.Format("{0} v{1} Started at {2}\n", Application.ProductName, Application.ProductVersion, DateTime.Now));
            this.Log(LogMsgType.Copyright, "Copyright \x00a9 2009-2010 www.educationtek.com All Right Reserved.\n");
        }
        private void frmTelnet_FormClosing(object sender, FormClosingEventArgs e)
        {
            // The form is closing, save the user's preferences
            SaveSettings();
        }

        private void rbText_CheckedChanged(object sender, EventArgs e)
        {
            if (rbText.Checked)
            {
                CurrentDataMode = DataMode.Text;
            }
        }
        private void rbHex_CheckedChanged(object sender, EventArgs e)
        {
            if (rbHex.Checked)
            {
                CurrentDataMode = DataMode.Hex;
            }
        }

        private void btnOpenPort_Click(object sender, EventArgs e)
        {
            // If the port is open, close it.
            if (socket.Connected)
            {
                this.Disconnect();
            }
            else
            {
                rtbConsole.Clear();

                // Open the port
                this.Connect();
            }
 
            // Change the state of the form's controls
            EnableControls();

            // If the port is open, send focus to the send data box
            if (socket.Connected)
            {
                txtSendData.Focus();
            }
        }
        private void btnSend_Click(object sender, EventArgs e)
        { 
            SendData(); 
        }
        private void txtSendData_KeyDown(object sender, KeyEventArgs e)
        { 
            // If the user presses [ENTER], send the data now
            if (KeyHandled = e.KeyCode == Keys.Enter) 
            { 
                e.Handled = true; 
                SendData(); 
            } 
        }
        private void txtSendData_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = KeyHandled; 
        }
        #endregion

        #region 打印
        private void btnExportLog_Click(object sender, EventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "LogFile(*.txt)|*.txt|All Files|*.*";
            dialog.FilterIndex = 1;
            dialog.RestoreDirectory = true;
            dialog.AddExtension = true;
            dialog.InitialDirectory = Path.Combine(Application.StartupPath, "log");
            dialog.DefaultExt = "txt";
            dialog.Title = "Select export Logfile path and name";
            if ((dialog.ShowDialog() == DialogResult.OK) && !File.Exists(dialog.FileName))
            {
                TextWriter writer = new StreamWriter(dialog.FileName);
                string str = this.rtbConsole.Text.ToString().Replace("\n", "\r\n");
                writer.WriteLine(str);
                writer.Close();
                Process.Start("notepad", dialog.FileName);
            }
        }

        private void btnPageSetup_Click(object sender, EventArgs e)
        {
            this.pageSetupDialog1.Document = printDocument1;
            this.pageSetupDialog1.ShowDialog();
        }

        private void btnPrintPreview_Click(object sender, EventArgs e)
        {
            this.printPreviewDialog1.ShowDialog();
        }

        private void btnPrint_Click(object sender, EventArgs e)
        {
            if (this.printDialog1.ShowDialog() == DialogResult.OK)
            {
                this.printDocument1.Print();
            }
        }

        private void printDocument1_BeginPrint(object sender, PrintEventArgs e)
        {
            this.checkPrint = 0;
        }

        private void printDocument1_PrintPage(object sender, PrintPageEventArgs e)
        {
            this.checkPrint = rtbConsole.Print(this.checkPrint, this.rtbConsole.TextLength, e);
            if (this.checkPrint < this.rtbConsole.TextLength)
            {
                e.HasMorePages = true;
            }
            else
            {
                e.HasMorePages = false;
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            rtbConsole.Clear();
        }
        #endregion

        #region 自定义按钮操作
        private void customBtn_click(int buttonIndex)
        {
            if (!socket.Connected)
            {
                this.Log(LogMsgType.Error, "Error: you need to Open Telnet Port first.\n");
            }
            else
            {
                buttonItem item = this.btnItems.buttonItems[buttonIndex - 1];
                if (item.buttonID == buttonIndex)
                {
                    if (string.Compare(item.isCopyOrSend, "Copy", true) == 0)
                    {
                        this.txtSendData.Text = item.txString;
                        if (string.Compare(item.isHexOrAscii, "Ascii", true) == 0)
                        {
                            this.rbText.Checked = true;
                        }
                        else
                        {
                            this.rbHex.Checked = true;
                        }
                    }
                    else if (string.Compare(item.isHexOrAscii, "Ascii", true) == 0)
                    {
                        this.rbText.Checked = true;
                        SocketSend(item.txString);
                        this.Log(LogMsgType.Outgoing, item.txString + "\n");
                    }
                    else
                    {
                        try
                        {
                            this.rbHex.Checked = true;
                            byte[] buffer = this.HexStringToByteArray(item.txString);
                            SocketSend(buffer);
                            this.Log(LogMsgType.Outgoing, this.ByteArrayToHexString(buffer) + "\n");
                        }
                        catch (FormatException)
                        {
                            this.Log(LogMsgType.Error, "Not properly formatted hex string: " + item.txString + "\n");
                        }
                    }
                }
            }
        }

        private void btn1_Click(object sender, EventArgs e)
        {
            this.customBtn_click(1);
        }

        private void btn2_Click(object sender, EventArgs e)
        {
            this.customBtn_click(2);
        }

        private void btn3_Click(object sender, EventArgs e)
        {
            this.customBtn_click(3);
        }

        private void btn4_Click(object sender, EventArgs e)
        {
            this.customBtn_click(4);
        }

        private void btn5_Click(object sender, EventArgs e)
        {
            this.customBtn_click(5);
        }

        private void btn6_Click(object sender, EventArgs e)
        {
            this.customBtn_click(6);
        }

        private void btn7_Click(object sender, EventArgs e)
        {
            this.customBtn_click(7);
        }

        private void btn8_Click(object sender, EventArgs e)
        {
            this.customBtn_click(8);
        }
        #endregion


    }
}

/*-------------------------------------------------------------------------------------------------
								             	 0ooo
							           	ooo0     (   )
								        (   )     ) /
							           	 \ (     (_/
	    				                  \_)        By:cuiqingwei [gary]
--------------------------------------------------------------------------------------------------*/