using ScreenLib;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using ZeroMQ;

namespace UDPNATClient
{
    class Program
    {
        static UdpClient mUdpClient;

        static string RemoteClientIP;

        static int RemoteClientPort;
        static bool isTeacher = System.Configuration.ConfigurationManager.AppSettings["ProgramType"] == "TECH" ? true : false;
        static bool autoSend = System.Configuration.ConfigurationManager.AppSettings["AutoSend"] == "1" ? true : false;
        static string serverIP = System.Configuration.ConfigurationManager.AppSettings["ServerIP"];
        static void Main(string[] args)
        {
            try
            {
                RemoteClientIP = serverIP;
                RemoteClientPort = 55556;

                //  TestZeroMQ();

                //    return;

                // Console.WriteLine("请输入服务器IP");

                //  Console.WriteLine("请输入服务器端口");
                int Port = int.Parse(System.Configuration.ConfigurationManager.AppSettings["ServerPort"]);
                IPEndPoint fLocalIPEndPoint = new IPEndPoint(IPAddress.Any, 0);


                //IPEndPoint LocalPt = new IPEndPoint(Dns.GetHostEntry(Dns.GetHostName()).AddressList[0], 0);
                //mUdpClient = new UdpClient();
                //mUdpClient.ExclusiveAddressUse = false;
                //mUdpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                //mUdpClient.Client.Bind(LocalPt);


                mUdpClient = new UdpClient(fLocalIPEndPoint);
                uint IOC_IN = 0x80000000;
                uint IOC_VENDOR = 0x18000000;
                uint SIO_UDP_CONNRESET = IOC_IN | IOC_VENDOR | 12;
                //    IOControlCode.DataToRead
                mUdpClient.Client.IOControl((int)SIO_UDP_CONNRESET, new byte[] { Convert.ToByte(false) }, null);
                mUdpClient.Client.ReceiveBufferSize = 4096;

                //    mUdpClient.BeginReceive(new AsyncCallback(ReceiveCallback), null);
                RunReceive();
                SendUDP((isTeacher ? "hello" : "hello"), serverIP, Port);
                //  byte[] fHelloData = Encoding.UTF8.GetBytes("hello");
                //  mUdpClient.Send(fHelloData, fHelloData.Length, IP, Port);

                while (true)
                {
                    string Content = Console.ReadLine();
                    if (RemoteClientIP != null)
                    {
                        if (Content == "pic")
                        {
                            Screen screen = new Screen();
                            byte[] fData = screen.GetScreenCapture();
                            mUdpClient.Send(fData, fData.Length, RemoteClientIP, RemoteClientPort);
                            Console.WriteLine("发送地址 " + RemoteClientIP + ":" + RemoteClientPort + "   图片长度:" + fData.Length);
                        }
                        else if (Content.StartsWith("big"))
                        {
                            int length = Convert.ToInt32(Content.Substring(3));
                            string x = "";
                            for (int i = 0; i < length; i++)
                            {
                                x += "x";
                            }
                            byte[] fData = Encoding.UTF8.GetBytes(x);
                            mUdpClient.Send(fData, fData.Length, RemoteClientIP, RemoteClientPort);
                            Console.WriteLine("发送地址 " + RemoteClientIP + ":" + RemoteClientPort + "   大数据长度:" + fData.Length);
                        }
                        else
                        {
                            SendUDP(Content, RemoteClientIP, RemoteClientPort);
                        }
                        //  byte[] fData = Encoding.UTF8.GetBytes(Content);
                        //   mUdpClient.Send(fData, fData.Length, RemoteClientIP, RemoteClientPort);
                        //   Console.WriteLine("发送地址" + RemoteClientIP + ":" + RemoteClientPort + "   内容:" + Content);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("UDP服务启动失败,监听端口：6000");
                Console.WriteLine(ex.ToString());
            }
            Console.ReadKey();
        }

        //private static int SendVarData( byte[] data)
        //{
        //    int total = 0;
        //    int size = data.Length;
        //    int dataleft = size;
        //    int sent;
        //    byte[] datasize = new byte[4];
        //    datasize = BitConverter.GetBytes(size);
        //    sent = mUdpClient.Send(datasiz,e);
        //    while (total < size)
        //    {
        //        sent = s.Send(data, total, dataleft, SocketFlags.None);
        //        total += sent;
        //        dataleft -= sent;
        //    }
        //    return total;
        //}

        private static void TestZeroMQ()
        {
            if (isTeacher)
            {
                using (var context = ZmqContext.Create())
                {
                    using (var socket = context.CreateSocket(ZeroMQ.SocketType.PULL))
                    {
                        socket.Bind("tcp://127.0.0.1:44444");
                        Console.WriteLine("已监听:tcp://127.0.0.1:44444 ");
                        while (true)
                        {
                            Thread.Sleep(200);
                            var rcvdMsg = socket.Receive(Encoding.UTF8);
                            Console.WriteLine("Received: " + rcvdMsg);
                            //  socket.Send("ok", Encoding.UTF8);
                            //   Console.WriteLine("send: ok");
                        }
                    }
                }
            }
            else
            {
                using (var context = ZmqContext.Create())
                {
                    using (var socket = context.CreateSocket(ZeroMQ.SocketType.PUSH))
                    {
                        socket.Connect("tcp://" + serverIP + ":44444");
                        while (true)
                        {
                            string msg = Console.ReadLine();
                            socket.Send(msg, Encoding.UTF8);
                            //  var replyMsg = socket.Receive(Encoding.UTF8);
                            // Console.WriteLine("Received: " + replyMsg);
                        }

                        // var replyMsg = socket.Receive(Encoding.UTF8);
                    }
                }
            }
        }

        private static void SendUDP(string content, string ip, int port)
        {
            byte[] fData = Encoding.UTF8.GetBytes(content);
            mUdpClient.Send(fData, fData.Length, ip, port);
            Console.WriteLine("发送地址" + ip + ":" + port + "   内容:" + content);
        }


        static void RunReceive()
        {
            Thread th = new Thread(() =>
            {



                while (true)
                {
                    IPEndPoint fClientIPEndPoint = null;
                    byte[] fData = mUdpClient.Receive(ref fClientIPEndPoint);
                    if (fData.Length > 0)
                    {//数据接收成功,放入缓存
                        if (fData.Length < 100)
                        {
                            string fContent = Encoding.UTF8.GetString(fData);
                            Console.WriteLine("收到信息：" + fContent);
                            string fIPAddress = fClientIPEndPoint.Address.ToString() + ":" + fClientIPEndPoint.Port;
                            Console.WriteLine(fIPAddress + ":" + fContent);
                            if (fContent.StartsWith("/"))
                            {
                                string[] fClientData = fContent.Split(':');
                                RemoteClientIP = fClientData[0].Substring(1);
                                RemoteClientPort = int.Parse(fClientData[1]);
                                if (autoSend)
                                {
                                    SendUDP("hello", RemoteClientIP, RemoteClientPort);
                                }
                                Console.WriteLine("远程客户端已穿透，请输入需要发送的信息");
                            }
                        }
                        else
                        {
                            Console.WriteLine("收到大数据：长度" + fData.Length);
                        }

                    }
                    Thread.Sleep(200);
                }



            });
            th.IsBackground = true;
            th.Start();
        }

        /// <summary>
        /// 异步接收数据
        /// </summary>
        /// <param name="ar"></param>
        static void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                if (mUdpClient.Client != null)
                {
                    IPEndPoint fClientIPEndPoint = null;
                    byte[] fData = mUdpClient.EndReceive(ar, ref fClientIPEndPoint);
                    if (fData.Length > 0)
                    {//数据接收成功,放入缓存
                        string fContent = Encoding.UTF8.GetString(fData);
                        Console.WriteLine("收到信息：" + fContent);
                        string fIPAddress = fClientIPEndPoint.Address.ToString() + ":" + fClientIPEndPoint.Port;
                        Console.WriteLine(fIPAddress + ":" + fContent);
                        if (fContent.StartsWith("/"))
                        {
                            string[] fClientData = fContent.Split(':');
                            RemoteClientIP = fClientData[0].Substring(1);
                            RemoteClientPort = int.Parse(fClientData[1]);
                            if (autoSend)
                            {
                                SendUDP("hello", RemoteClientIP, RemoteClientPort);
                            }
                            Console.WriteLine("远程客户端已穿透，请输入需要发送的信息");
                        }
                        //else
                        //{
                        //    Console.WriteLine(fContent);
                        //}
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                try
                {
                    mUdpClient.BeginReceive(new AsyncCallback(ReceiveCallback), null);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
        }
    }
}
