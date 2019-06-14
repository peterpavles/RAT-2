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

namespace HeraStub
{
    public partial class Chat : Form
    {
        public Socket ServerSocket;
        public Chat(Socket CurrentSocket)
        {
            ServerSocket = CurrentSocket;
            InitializeComponent();
        }

        public void DisplayServerMessage(String Message, Chat ChatInstance)
        {
            if (ChatInstance.InvokeRequired)
            {
               ChatInstance.BeginInvoke((MethodInvoker)delegate ()
               {
                   textBox1.Text = textBox1.Text + "Server> " + Message + Environment.NewLine;
                   textBox1.SelectionStart = textBox1.Text.Length;
                   textBox1.ScrollToCaret();
               });
            }
            else
            {
                textBox1.Text = textBox1.Text + "Server> " + Message + Environment.NewLine;
                textBox1.SelectionStart = textBox1.Text.Length;
                textBox1.ScrollToCaret();
            }
        }

        private void textBox2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                ServerSocket.Send(Encoding.ASCII.GetBytes("MessageResponse|" + textBox2.Text));
                textBox1.Text = textBox1.Text + "You> " + textBox2.Text + Environment.NewLine;
                textBox1.SelectionStart = textBox1.Text.Length;
                textBox1.ScrollToCaret();
                textBox2.Text = "";
            }
        }

        private void Chat_Load(object sender, EventArgs e)
        {

        }
    }
}
