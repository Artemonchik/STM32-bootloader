using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Security.Cryptography;
using System.IO;
using System.Threading;
using System.Diagnostics.Tracing;
using System.Windows.Forms;
using System.ComponentModel.Design;
//using System.Runtime.Remoting.Messaging;
using System.Diagnostics;
using BootloaderFileFormat;

namespace Flasher
{
    static class Constants
    {
        public const int DataBits = 8;
    }

    class Client
    {
        public const int DEFAULT_BAUDRATE = 115200; 

        public static SerialPort Connect(string portName, int dataBits, StopBits stopBits)
        {
            SerialPort serialPort = new SerialPort(portName, DEFAULT_BAUDRATE, Parity.None, dataBits, stopBits);
            try
            {
                if (!serialPort.IsOpen)
                {
                    serialPort.Open();
                }
            }
            catch (UnauthorizedAccessException Ex)
            {
                Debug.WriteLine(Ex.ToString());
                Debug.WriteLine("Access is denied to the port.");
                serialPort.Close();
            }
            catch (ArgumentOutOfRangeException Ex)
            {
                Debug.WriteLine(Ex.ToString());
                Debug.WriteLine("One of the properties is invalid");
                serialPort.Close();
            }
            catch (IOException Ex)
            {
                Debug.WriteLine(Ex.ToString());
                Debug.WriteLine("The port is in an invalid state");
                serialPort.Close();
            }
            catch (InvalidOperationException Ex)
            {
                Debug.WriteLine(Ex.ToString());
                Debug.WriteLine("SerialPort is already open");
                serialPort.Close();
            }
            Debug.WriteLine("SerialPort is open");
            return serialPort;
        }

        public static void Disconnect(SerialPort serialPort)
        {
           // serialPort.DiscardInBuffer();
            serialPort.Close();
        }
        /// <summary>
        /// Client's point of entry
        /// </summary>
        /// <param name="portName"> Name of SerialPort</param>
        /// <param name="baudRate"> Baud rate for the serial port </param>
        /// <param name="dataBits"> Standard number of data bits per byte</param>
        /// <param name="stopBits">Stop Bits for SerialPort </param>
        /// <param name="bootloaderFile">binary data to transmit</param>
        public void Upload(SerialPort serialPort, int baudRate, BootloaderFile bootloaderFile, Form1 parentForm)
        {

            Debug.WriteLine("Waiting for the start code");

            //waiting for communication with SerialPort
            Debug.WriteLine("Waiting for communication");
            while (true) 
            {
                if (serialPort.BytesToRead > 0)
                {
                    byte[] oneByteToRead = new byte[10];
                    serialPort.Read(oneByteToRead, 0, sizeof(byte) * 1);
                    int transmissionCode = BitConverter.ToInt32(oneByteToRead, 0);
                    if (transmissionCode == Transmission.START_CODE) {
                        Debug.WriteLine("Session successful started");

                        break;
                    }   
                    else {
                        Debug.WriteLine("ERROR IN CODE");

                    }
                }
            }
            serialPort.Write(BitConverter.GetBytes(Transmission.START_CODE), 0, 1);
            Debug.WriteLine("Communication was started");
            TransmittedData.TransmissionOfData(serialPort, baudRate, bootloaderFile, parentForm);
            Disconnect(serialPort);
        }
    

    }
}
