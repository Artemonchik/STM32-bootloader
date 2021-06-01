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
using BootloaderFileFormat;
using System.Diagnostics;

namespace Flasher
{

    public partial class Form1 : Form
    {
        string fileName;
        string filepath= @"C:\Users\KazukiNagasa\Desktop\Project\Code.bin";
        string filepath1 = @"C:\Users\KazukiNagasa\Desktop\Project\Data.bin";
        string portName;
        int baudrate;
        bool isClickedButton2 = false;
        bool isClickedButton3 = false;
        BootloaderFile bff;
        const int RESTART_FILE_PARAMS = 1;
        const int RESTART_SUCCESS_PARAMS = 2;
        SerialPort serialPort;
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            comboBox1.DataSource = SerialPort.GetPortNames();
        }
        public void Restart_Params(int scenario)
        {
            switch (scenario) {
                case (RESTART_FILE_PARAMS):
                    textBox1.Text = null;
                    fileName = null;
                    richTextBox1.Text = null;
                    progressBar1.Value = 0;
                    break;
                case (RESTART_SUCCESS_PARAMS):
                    progressBar1.Value = 0;
                    button1.Enabled = true;
                    break;
            }
        }


        //Open file and store it path...
        private void Button2_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                textBox1.Text = ofd.SafeFileName;
                fileName = ofd.FileName;
                Debug.WriteLine($"FilePath: {filepath} ");
                try
                {
                    bff = new BootloaderFile(fileName);
                    string result = BitConverter.ToString(bff.IV);
                    Debug.WriteLine($"{ bff.ToFancyString()} IV: {result} ");
                    richTextBox1.Text = bff.ToFancyString();
                    isClickedButton2 = true;
                    if (isClickedButton3 == true) {
                        button1.Enabled = true;
                    }
                }
                catch (Exception ex) 
                {
                    Restart_Params(RESTART_FILE_PARAMS);
                    UpdateStatus(true, ex.Message);
                    button1.Enabled = false;
                }
            }
        }

        //Select COM -> Upload button enabled
        private void ComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox1.SelectedIndex == -1)
            {
                button1.Enabled = false;
                button2.Enabled = false;
                button3.Enabled = false;
                button4.Enabled = false;
                button5.Enabled = false;
                button6.Enabled = false;
            }
            else
            {
                portName = (string)comboBox1.SelectedItem;
            }
        }

        private  void Button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
            //BootloaderFile bff = new BootloaderFile(file_name);
            try
            {
                //var bin =  ReadFile(file_name);
                new Client().Upload( serialPort, baudrate, bff, this);
                MessageBox.Show("Success", "Notification", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                /* set message */
                UpdateStatus(true, ex.Message);
            }
            Restart_Params(RESTART_SUCCESS_PARAMS);
        }
        private void ComboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox2.SelectedIndex == -1)
            {
                button1.Enabled = false;
                button2.Enabled = false;
                button3.Enabled = false;
                button4.Enabled = false;
                button5.Enabled = false;
                button6.Enabled = false;
            }
            else
            {
                baudrate = int.Parse((string)comboBox2.SelectedItem);
                button3.Enabled = true;
            }
        }

        private void UpdateStatus(bool ding, string text)
        {
            if (ding)
            {
                SystemSounds.Exclamation.Play();
            }
            MessageBox.Show(text, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void ProgressBar1_Click(object sender, EventArgs e)
        {
            progressBar1.Minimum = 0;
            progressBar1.Maximum = 100;

        }
        public void ProgressChanged(int progress)
        {
            progressBar1.Value = progress;
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Created by Patrushev Borya," + Environment.NewLine + "Vasilev Pavel and Tarasov Artem", "Creators", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                serialPort = Client.Connect(portName, baudrate,  Constants.DataBits, StopBits.One, bff, this);
                MessageBox.Show("Connected", "Notification", MessageBoxButtons.OK, MessageBoxIcon.Information);
                button4.Enabled = true;
                button5.Enabled = true;
                button6.Enabled = true;
                isClickedButton3 = true;
                if (isClickedButton2 == true) 
                {
                    button1.Enabled = true;
                }

            }
            catch (Exception ex)
            {
                /* set message */
                UpdateStatus(true, ex.Message);
            }
            richTextBox2.Text = TransmittedData.recievedMetaInfo.ToFancyString();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            try{
                Client.Disconnect(serialPort, baudrate, bff, this);
                button1.Enabled = false;
                button2.Enabled = false;
                button3.Enabled = true;
                button4.Enabled = false;
                button5.Enabled = false;
                button6.Enabled = false;
            }
            catch (Exception ex)
            {
                /* set message */
                UpdateStatus(true, ex.Message);
            }

        }

        private void button5_Click(object sender, EventArgs e)
        {
            try
            {
                new Client().DownloadCode(serialPort, baudrate, bff, this);
                MessageBox.Show("Success", "Notification", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                /* set message */
                UpdateStatus(true, ex.Message);
            }
            /*
            if (!File.Exists(filepath))
            {
                using (FileStream fs = File.Create(filepath))
                {
                    fs.Write(TransmittedData.downloadedSmth, 0, TransmittedData.downloadedSmth.Length);
                }
            }*/
            TransmittedData.recievedMetaInfo.WriteBootloaderFile(filepath);
            //binaryWriter.Write(downloadedSmth);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            try
            {
                new Client().DownloadData(serialPort, baudrate, bff, this);
            }
            catch (Exception ex)
            {
                /* set message */
                UpdateStatus(true, ex.Message);
            }
            if (!File.Exists(filepath1))
            {
                using (FileStream fs = File.Create(filepath1))
                {
                    fs.Write(TransmittedData.downloadedData, 0, TransmittedData.downloadedData.Length);
                }
            }
        }
    }
}
