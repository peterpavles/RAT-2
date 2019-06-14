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
    public partial class FrmChat : Form
    {
        public Socket client;
        public FrmChat(Socket ClientSocket)
        {
            client = ClientSocket;
            InitializeComponent();
        }

        public void DisplayClientMessage(String Message, String ClientAddressDetails)
        {
                textBox1.Text = textBox1.Text + ClientAddressDetails + "> " + Message + Environment.NewLine;
                textBox1.SelectionStart = textBox1.Text.Length;
                textBox1.ScrollToCaret();
        }

        private void textBox2_KeyPress(object sender, KeyPressEventArgs e)
        {
            
        }

        private void textBox2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                client.Send(Encoding.ASCII.GetBytes("Chat|" + textBox2.Text));
                textBox1.Text = textBox1.Text + "You> " + textBox2.Text + Environment.NewLine;
                textBox1.SelectionStart = textBox1.Text.Length;
                textBox1.ScrollToCaret();
                textBox2.Text = "";
            }
        }

        private void FrmChat_Load(object sender, EventArgs e)
        {
            this.Text = "Remote Chat - [" + client.RemoteEndPoint.ToString() + "]";
            client.Send(Encoding.ASCII.GetBytes("SysSetup|Chat"));
        }

        private void FrmChat_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                client.Send(Encoding.ASCII.GetBytes("SysSetup|ChatClose"));
            }
            catch
            {

            }
        }
    }
}
