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

namespace Flasher
{
    static class Constants
    {
        public const int DataBits = 8;
    }

    class Client
    {
        public static void Main(string portName, int baudRate, int dataBits, StopBits stopBits, byte[] code)
        {
            int block_size = 16;
            String decoded_data;
            SerialPort _serialPort = new SerialPort(portName, baudRate, Parity.None , dataBits, stopBits);

            Console.WriteLine("Waiting for the start code/n");
            Console.Write(code);
             //TODO: add 00 to the end of code, if % blocksize != 0

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
            while (true) 
            {
                if (_serialPort.BytesToRead > 0)
                {
                    byte[] uno_Byte = new byte[10];
                    _serialPort.Read(uno_Byte, 0, sizeof(byte) * 1);
                    int transmission_code = BitConverter.ToInt32(uno_Byte, 0);
                    if (transmission_code == 0xAE) {
                        Console.WriteLine("The 0xAE was received. Started sending data/n");

                        break;
                    }
                    else {
                        Console.WriteLine($"{transmission_code} doesn't match the standard 0xAE");

                    }
                }
            }
            Data_transmition.Send_data_header(_serialPort, code, 3);
            int i = 0;
            while (true) 
            {
                Data_transmition.Wait_for_data(_serialPort);
                Data_s recieved_data = Data_transmition.Receive_data(_serialPort);
                Console.WriteLine($"Data Length: {recieved_data.length} Data type:{recieved_data.type}");

                if (recieved_data.type == 4)
                {

                    Data_transmition.Send_data(_serialPort, code, block_size, i + block_size, 3);
                    i += block_size;
                    
                    if (i > code.Length)
                    {
                        break;
                    }
                }
                else
                {
                    
                    decoded_data = Data_transmition.Decode_data(recieved_data);
                    Console.WriteLine($"Data: {decoded_data}");
                    Array.Clear(recieved_data.data, 0, recieved_data.length);
                }
            }

            _serialPort.Close();


        }

    }
}
