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
    static class ClientCodes
    {
        public const int CONNECT = 1;
        public const int UPLOAD = 2;
        public const int DISCONNECT = 3;
        public const int DOWNLOAD = 4;
    }
    class Client
    {
        public const int DEFAULT_BAUDRATE = 115200; 

        public static SerialPort Connect(string portName, int baudRate, int dataBits, StopBits stopBits, BootloaderFile bootloaderFile, Form1 parentForm)
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
            TransmittedData.StartCommunication(serialPort);
            TransmittedData.TransmissionOfData(serialPort, baudRate, bootloaderFile, ClientCodes.CONNECT, parentForm);
            return serialPort;
        }

        public static void Disconnect(SerialPort serialPort, int baudRate, BootloaderFile bootloaderFile, Form1 parentForm)
        {
            TransmittedData.TransmissionOfData(serialPort, baudRate, bootloaderFile, ClientCodes.DISCONNECT, parentForm);
            serialPort.DiscardInBuffer();
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
            TransmittedData.TransmissionOfData(serialPort, baudRate, bootloaderFile, ClientCodes.UPLOAD, parentForm);
        }
    

    }
}
