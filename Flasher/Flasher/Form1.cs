using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;
using System.IO;
using System.Media;

namespace Flasher
{

    public partial class Form1 : Form
    {
        //for storing path
        string file_name;
        string port_name;
        int baudrate;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            comboBox1.DataSource = SerialPort.GetPortNames();
        }

        //Open file and store it path...
        private void button2_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                textBox1.Text = ofd.SafeFileName;
                file_name = ofd.FileName;
            }
        }

        //Select COM -> Upload button enabled
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox1.SelectedIndex == -1)
            {
                button1.Enabled = false;
            }
            else
            {
                port_name = (string)comboBox1.SelectedItem;
                button1.Enabled = true;
            }
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
            try
            {
                var bin = await ReadFile(file_name);
                /* try to upload */
                Client.main(port_name, baudrate, Constants.DataBits, StopBits.One, bin);
            }
            catch (Exception ex)
            {
                /* set message */
                UpdateStatus(true, ex.Message);
            }
            finally
            {
                button1.Enabled = true;
            }
        }
        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            baudrate = int.Parse((string)comboBox2.SelectedItem);
        }

        private async Task<byte[]> ReadFile(string fname)
        {
            byte[] bin;

            /* open file */
            using (var s = new FileStream(fname, FileMode.Open,
                    FileAccess.Read))
            {
                /* allocate memory */
                bin = new byte[s.Length];
                /* read file contents */
                await s.ReadAsync(bin, 0, bin.Length);
            }

            /* return binary image */
            return bin;
        }
        private void UpdateStatus(bool ding, string text)
        {

            /* play a system sound? */
            if (ding)
            {
                /* ^^ ding! */
                SystemSounds.Exclamation.Play();
            }
        }
    }
}
