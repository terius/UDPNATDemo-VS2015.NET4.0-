using ScreenLib;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using ZeroMQ;

namespace UDPNATServer
{
    class Program
    {
        static UdpClient mUdpReciver;

        static List<string> ClientIPPort = new List<string>();
        static int port = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["port"]);
        static void Main(string[] args)
        {
            try
            {

                IPEndPoint fLocalIPEndPoint = new IPEndPoint(IPAddress.Any, port);
                mUdpReciver = new UdpClient(fLocalIPEndPoint);
                mUdpReciver.Client.ReceiveBufferSize = 4096;
                uint IOC_IN = 0x80000000;
                uint IOC_VENDOR = 0x18000000;
                uint SIO_UDP_CONNRESET = IOC_IN | IOC_VENDOR | 12;
                mUdpReciver.Client.IOControl((int)SIO_UDP_CONNRESET, new byte[] { Convert.ToByte(false) }, null);
                //  mUdpReciver.BeginReceive(new AsyncCallback(ReceiveCallback), null);
                RunReceive();
                Console.WriteLine("UDP服务启动成功,监听端口：" + port);
                while (true)
                {
                    string Content = Console.ReadLine();

                    foreach (string item in ClientIPPort)
                    {
                        string[] ClientAddress = item.Split(':');
                        if (Content == "pic")
                        {
                            Screen screen = new Screen();
                            byte[] fData = screen.GetScreenCapture();
                            mUdpReciver.Send(fData, fData.Length, ClientAddress[0], int.Parse(ClientAddress[1]));
                            Console.WriteLine("发送地址 " + ClientAddress[0] + ":" + ClientAddress[1] + "   图片长度:" + fData.Length);
                        }
                        else
                        {
                            byte[] fData = Encoding.UTF8.GetBytes(Content);
                            mUdpReciver.Send(fData, fData.Length, ClientAddress[0], int.Parse(ClientAddress[1]));
                            Console.WriteLine("发送地址" + ClientAddress[0] + ":" + ClientAddress[1] + "   内容:" + Content);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("UDP服务启动失败,监听端口：" + port);
                Console.WriteLine(ex.ToString());
            }
            Console.ReadKey();
        }

        private static void TestZeroMQ()
        {
            using (var context = ZmqContext.Create())
            {
                using (var socket = context.CreateSocket(ZeroMQ.SocketType.REP))
                {

                    socket.Bind("tcp://127.0.0.1:44444");
                    Console.WriteLine("已监听:tcp://127.0.0.1:44444 ");
                    while (true)
                    {
                        Thread.Sleep(200);
                        var rcvdMsg = socket.Receive(Encoding.UTF8);
                        Console.WriteLine("Received: " + rcvdMsg);
                        var replyMsg = "replyMsg" + DateTime.Now.Ticks;
                        Console.WriteLine("Sending : " + replyMsg + Environment.NewLine);
                        socket.Send(replyMsg, Encoding.UTF8);
                    }
                }
            }
        }

        static object obLock = new object();
        static string _teacherIP;
        static int _teacherPort;

        static void RunReceive()
        {
            Thread th = new Thread(() =>
            {

                lock (obLock)
                {
                    IPEndPoint fClientIPEndPoint = null;
                    while (true)
                    {
                        byte[] fData = mUdpReciver.Receive(ref fClientIPEndPoint);
                        string fIPAddress = fClientIPEndPoint.Address.ToString() + ":" + fClientIPEndPoint.Port;
                        Console.WriteLine("收到客户端请求：" + fIPAddress + "   数据大小：" + fData.Length);
                        if (fData.Length > 0)
                        {//数据接收成功,放入缓存
                            if (fData.Length < 100)
                            {
                                _teacherIP = fClientIPEndPoint.Address.ToString();
                                _teacherPort = fClientIPEndPoint.Port;
                            }
                            else
                            {
                                if (!string.IsNullOrWhiteSpace(_teacherIP) && _teacherPort > 0)
                                {
                                    mUdpReciver.Send(fData, fData.Length, _teacherIP, _teacherPort);
                                }
                            }

                            //string fContent = Encoding.UTF8.GetString(fData);
                            //Console.WriteLine(fIPAddress + ":" + fContent);
                            //if (fContent == "hello")
                            //{
                            //    if (ClientIPPort.Count < 2)
                            //    {
                            //        if (!ClientIPPort.Contains(fIPAddress))
                            //        {
                            //            ClientIPPort.Add(fIPAddress);
                            //            Console.WriteLine("客户端注册成功,当前已注册客户端数量：" + ClientIPPort.Count);
                            //            if (ClientIPPort.Count == 2)
                            //            {
                            //                //将客户端1的地址发送给客户端2
                            //                //  byte[] Client1AddressData = Encoding.UTF8.GetBytes("/" + ClientIPPort[0]);
                            //                string[] Client1Address = ClientIPPort[1].Split(':');
                            //                SendUDP("/" + ClientIPPort[0], Client1Address[0], int.Parse(Client1Address[1]));
                            //                //   mUdpReciver.Send(Client1AddressData, Client1AddressData.Length, Client1Address[0], int.Parse(Client1Address[1]));
                            //                Thread.Sleep(1000);
                            //                //将客户端2的地址发送给客户端1
                            //                string[] Client2Address = ClientIPPort[0].Split(':');
                            //                SendUDP("/" + ClientIPPort[1], Client2Address[0], int.Parse(Client2Address[1]));
                            //                //  byte[] Client2AddressData = Encoding.UTF8.GetBytes("/" + ClientIPPort[1]);
                            //                //  string[] Client2Address = ClientIPPort[0].Split(':');
                            //                // mUdpReciver.Send(Client2AddressData, Client2AddressData.Length, Client2Address[0], int.Parse(Client2Address[1]));
                            //            }
                            //        }
                            //    }
                            //    else
                            //    {
                            //        Console.WriteLine("客户端已满");
                            //    }
                            //}
                        }
                        Thread.Sleep(10);
                    }
                }


            });
            th.IsBackground = true;
            th.Start();
        }

        private static void SendUDP(string content, string ip, int port)
        {
            byte[] fData = Encoding.UTF8.GetBytes(content);
            mUdpReciver.Send(fData, fData.Length, ip, port);
            Console.WriteLine("发送地址" + ip + ":" + port + "   内容:" + content);
        }

        ///// <summary>
        ///// 异步接收数据
        ///// </summary>
        ///// <param name="ar"></param>
        //static void ReceiveCallback(IAsyncResult ar)
        //{
        //    try
        //    {
        //        if (mUdpReciver.Client != null)
        //        {
        //            IPEndPoint fClientIPEndPoint = null;
        //            byte[] fData = mUdpReciver.EndReceive(ar, ref fClientIPEndPoint);
        //            if (fData.Length > 0)
        //            {//数据接收成功,放入缓存
        //                string fIPAddress = fClientIPEndPoint.Address.ToString() + ":" + fClientIPEndPoint.Port;
        //                string fContent = Encoding.UTF8.GetString(fData);
        //                Console.WriteLine(fIPAddress + ":" + fContent);
        //                if (fContent == "hello")
        //                {
        //                    if (ClientIPPort.Count < 2)
        //                    {
        //                        if (ClientIPPort.Contains(fIPAddress) == false)
        //                        {
        //                            ClientIPPort.Add(fIPAddress);
        //                            Console.WriteLine("客户端注册成功,当前已注册客户端数量：" + ClientIPPort.Count);
        //                            if (ClientIPPort.Count == 2)
        //                            {
        //                                //将客户端1的地址发送给客户端2
        //                                byte[] Client1AddressData = Encoding.UTF8.GetBytes("/" + ClientIPPort[0]);
        //                                string[] Client1Address = ClientIPPort[1].Split(':');
        //                                mUdpReciver.Send(Client1AddressData, Client1AddressData.Length, Client1Address[0], int.Parse(Client1Address[1]));
        //                                Thread.Sleep(2000);
        //                                //将客户端2的地址发送给客户端1
        //                                byte[] Client2AddressData = Encoding.UTF8.GetBytes("/" + ClientIPPort[1]);
        //                                string[] Client2Address = ClientIPPort[0].Split(':');
        //                                mUdpReciver.Send(Client2AddressData, Client2AddressData.Length, Client2Address[0], int.Parse(Client2Address[1]));
        //                            }
        //                        }
        //                    }
        //                    else
        //                    {
        //                        Console.WriteLine("客户端已满");
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.ToString());
        //    }
        //    finally
        //    {
        //        try
        //        {
        //            mUdpReciver.BeginReceive(new AsyncCallback(ReceiveCallback), null);
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine(ex.ToString());
        //        }
        //    }
        //}



    }
}
