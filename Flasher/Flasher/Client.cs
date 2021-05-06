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

namespace Flasher
{
    static class Constants
    {
        public const int DataBits = 8;
    }

    class Client
    {
        /// <summary>
        /// Client's point of entry
        /// </summary>
        /// <param name="portName"> Name of SerialPort</param>
        /// <param name="baudRate"> Baud rate for the serial port </param>
        /// <param name="dataBits"> Standard number of data bits per byte</param>
        /// <param name="stopBits">Stop Bits for SerialPort </param>
        /// <param name="code">binary data to transmit</param>
        public void Main(string portName, int baudRate, int dataBits, StopBits stopBits, byte[] code, Form1 parentForm)
        {
            int flag = 0;
            int currentBytesSended = 0;
            String decoded_data;
            SerialPort _serialPort = new SerialPort(portName, baudRate, Parity.None , dataBits, stopBits);

            Console.WriteLine("Waiting for the start code");
            Console.Write(code);

            //open SerialPort and if smth wrong -> catch ex
            try
            {
                if (!_serialPort.IsOpen)
                {
                    _serialPort.Open();
                }
            }
            catch (UnauthorizedAccessException Ex)
            {
                Console.WriteLine(Ex.ToString());
                Console.WriteLine("Access is denied to the port./n");
                _serialPort.Close();
            }
            catch (ArgumentOutOfRangeException Ex)
            {
                Console.WriteLine(Ex.ToString());
                Console.WriteLine("One of the properties is invalid/n");
                _serialPort.Close();
            }
            catch (IOException Ex)
            {
                Console.WriteLine(Ex.ToString());
                Console.WriteLine("The port is in an invalid state/n");
                _serialPort.Close();
            }
            catch (InvalidOperationException Ex)
            {
                Console.WriteLine(Ex.ToString());
                Console.WriteLine("SerialPort is already open/n");
                _serialPort.Close();
            }


            //waiting for communication with SerialPort
            Console.WriteLine("Waiting for communication");
            //Data_transmition.Wait_for_data(_serialPort);
            while (true) 
            {
                if (_serialPort.BytesToRead > 0)
                {
                    byte[] uno_Byte = new byte[10];
                    _serialPort.Read(uno_Byte, 0, sizeof(byte) * 1);
                    int transmission_code = BitConverter.ToInt32(uno_Byte, 0);
                    if (transmission_code == Transmition.START_CODE) {
                        Console.WriteLine("session successful started");

                        break;
                    }   
                    else {
                        Console.WriteLine("ERROR IN CODE");

                    }
                }
            }
            _serialPort.Write(BitConverter.GetBytes(Transmition.START_CODE), 0, 1);
            Console.WriteLine("Communication was started");
            while (true) 
            {
                Data_transmition.Wait_for_data(_serialPort);
                Data_s recieved_data = Data_transmition.ReceiveRawData(_serialPort);

                if (recieved_data.code == Transmition.NEXT)
                {
                    Console.WriteLine("Request for next block received");
                }
                else if (recieved_data.code == Transmition.REQUEST)
                {
                    Console.WriteLine($"{recieved_data.data}");
                    uint f = BitConverter.ToUInt32(recieved_data.data, 0);
                    uint t = BitConverter.ToUInt32(recieved_data.data, 4);
                    Console.WriteLine($"{f} {t} and len of sended data is {code.Skip((int)f).Take((int)(t - f)).ToArray().Length}");
                    Data_transmition.SendRawData(_serialPort, Transmition.PROGRAM, code.Skip((int)f).Take((int)(t - f)).ToArray().Length, code.Skip((int)f).Take((int)(t - f)).ToArray());
                    currentBytesSended += code.Skip((int)f).Take((int)(t - f)).ToArray().Length;
                    parentForm.ProgressChanged((int)(100 * currentBytesSended / code.Length));
                    continue;
                }
                else {
                    //Console.WriteLine($"Data received: {recieved_data.data}");
                    decoded_data = Data_transmition.Decode_data(recieved_data);
                    Console.WriteLine($"Data: {decoded_data}");
                    continue;
                }
                if (flag == 0)
                {
                    byte[] program = BitConverter.GetBytes(code.Length);
                    Console.WriteLine($"Data len is {code.Length}");
                    Data_transmition.SendRawData(_serialPort, Transmition.PROGRAM, program.Length, program);
                    flag = 1;
                    continue;
                }else
                {
                    byte[] release = new byte[1];
                    Data_transmition.SendRawData(_serialPort, Transmition.RELEASE, 0, release);
                    Console.WriteLine("Bye-Bye");
                    break;
                }
                /*
                int val = Convert.ToInt32(Console.ReadLine());
                switch (val) 
                {
                    case Transmition.BAUDRATE:
                        Console.WriteLine("Enter New BaudRate");
                        int baudrate = Convert.ToInt32(Console.ReadLine());
                        Data_transmition.SendRawData(_serialPort, Transmition.BAUDRATE, sizeof(int), BitConverter.GetBytes(baudRate));
                        _serialPort.BaudRate = baudrate;
                        continue;
                    case (Transmition.STRING_MESSAGE):
                        Data_transmition.SendRawData(_serialPort, Transmition.STRING_MESSAGE, mes.Length, Encoding.ASCII.GetBytes(mes), 10);
                        Console.WriteLine($"{Data_transmition.Decode_data(Data_transmition.ReceiveRawData(_serialPort))}");
                        continue;
                    case (Transmition.TIMEOUT):
                        Console.WriteLine("Enter New timeout");
                        int timeout = Convert.ToInt32(Console.ReadLine());
                        Data_transmition.SendRawData(_serialPort, Transmition.TIMEOUT, sizeof(int), BitConverter.GetBytes(timeout));
                        continue;
                    case (Transmition.PROGRAM):
                        byte[] program = BitConverter.GetBytes(code.Length);
                        Console.WriteLine($"Data len is {code.Length}");
                        Data_transmition.SendRawData(_serialPort, Transmition.PROGRAM, program.Length, program);
                        continue;
                    case (Transmition.RELEASE):
                        byte[] release = new byte[0];
                        Data_transmition.SendRawData(_serialPort, Transmition.RELEASE, 0, release);
                        Console.WriteLine("Bye-Bye");
                        break;
                    case (Transmition.SECRET_KEY):
                        byte[] key = new byte[1];
                        Data_transmition.SendRawData(_serialPort, Transmition.SECRET_KEY, 0, key);
                        continue;


            }
            */


            }

            //Clearing Buffer and close Port for restarting next 
            _serialPort.DiscardInBuffer();
            _serialPort.Close();


        }

    }
}
