using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RAT
{
    public partial class EncDecInput : Form
    {
        public String ReturnValue { get; set; }
        public EncDecInput(String Type)
        {
            InitializeComponent();
            this.Text = "Please enter the " + Type + "ion password";
            this.button1.Text = Type;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.ReturnValue = textBox1.Text;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
