﻿using System;
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

namespace Flasher
{

    public partial class Form1 : Form
    {
        string file_name;
        string port_name;
        int baudrate;
        BootloaderFile bff;
        const int RESTART_FILE_PARAMS = 1;
        const int RESTART_SUCCESS_PARAMS = 2;
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
                    file_name = null;
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
                file_name = ofd.FileName;
                try
                {
                    bff = new BootloaderFile(file_name);
                    //string result = System.Text.Encoding.UTF8.GetString(bff.IV);
                    //Console.WriteLine($"{ bff.ToFancyString()} IV: {result} ");
                    richTextBox1.Text = bff.ToFancyString();
                }
                catch (Exception ex) 
                {
                    Restart_Params(RESTART_FILE_PARAMS);
                    UpdateStatus(true, ex.Message);
                }
            }
        }

        //Select COM -> Upload button enabled
        private void ComboBox1_SelectedIndexChanged(object sender, EventArgs e)
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

        private  void Button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
            //BootloaderFile bff = new BootloaderFile(file_name);
            try
            {
                //var bin =  ReadFile(file_name);
                new Client().Main(port_name, baudrate, Constants.DataBits, StopBits.One, bff.Data, this);
            }
            catch (Exception ex)
            {
                /* set message */
                UpdateStatus(true, ex.Message);
            }
            finally
            {
                MessageBox.Show("Success", "Notification", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Restart_Params(RESTART_SUCCESS_PARAMS);
            }
        }
        private void ComboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            baudrate = int.Parse((string)comboBox2.SelectedItem);
        }

        /*
        private byte[] ReadFile(string fname)
        {
            byte[] bin;

            var s = new FileStream(fname, FileMode.Open, FileAccess.Read);
            var s = new
            bin = new byte[s.Length];
            s.Read(bin, 0, bin.Length);
            return bin;
        }
        */
        private void UpdateStatus(bool ding, string text)
        {
            /* play a system sound? */
            if (ding)
            {
                /* ^^ ding! */
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

    }
}