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

namespace RAT
{
    public partial class Settings : Form
    {
        public bool IsListening;
        public Main ParentForm;
        public Settings(bool Status, Main CurrForm)
        {
            IsListening = Status;
            ParentForm = CurrForm;
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            using (StreamWriter Export = new StreamWriter("Settings.ini"))
            {
                Export.WriteLine(numericUpDown1.Value.ToString());
                Export.WriteLine(textBox1.Text);
            }
            MessageBox.Show("Done!");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (button2.Text.Contains("Start"))
            {
                IPEndPoint[] MachineListeners = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();
                bool PortInUse = false;
                foreach (IPEndPoint ep in MachineListeners)
                {
                    if (ep.Port == Convert.ToInt32(numericUpDown1.Value))
                    {
                        PortInUse = true;
                    }
                }
                if (PortInUse == false)
                {
                    ParentForm.StartListener(Convert.ToInt32(numericUpDown1.Value), ParentForm);
                    numericUpDown1.Enabled = false;
                    textBox1.Enabled = false;
                    button2.Text = "Stop Listener";
                }
                else
                {
                    MessageBox.Show("The requested port is in use, please choose a different port.", "Error!");
                }
            }
            else if (button2.Text.Contains("Stop"))
            {
                ParentForm.StopListener();
                numericUpDown1.Enabled = true;
                textBox1.Enabled = true;
                button2.Text = "Start Listener";
            }
        }

        private void Settings_Load(object sender, EventArgs e)
        {
            if (IsListening == true)
            {
                numericUpDown1.Enabled = false;
                textBox1.Enabled = false;
                button2.Text = "Stop Listener";
            }
            else
            {
                numericUpDown1.Enabled = true;
                textBox1.Enabled = true;
                button2.Text = "Start Listener";
            }
            if (File.Exists("Settings.ini") == true)
            {
                using (StreamReader Import = new StreamReader("Settings.ini"))
                {
                    numericUpDown1.Value = Convert.ToInt32(Import.ReadLine());
                    textBox1.Text = Import.ReadLine();
                }
            }
            else
            {
                using (StreamWriter Export = new StreamWriter("Settings.ini"))
                {
                    Export.WriteLine(numericUpDown1.Value.ToString());
                    Export.WriteLine(textBox1.Text);
                }
            }
        }
    }
}
