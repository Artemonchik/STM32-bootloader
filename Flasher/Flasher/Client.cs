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
        public static void main(string portName, int baudRate, int dataBits, StopBits stopBits, byte[] code)
        {
            int block_size = 16;
            Data_s recieved_data = new Data_s(0, 0, null);
            byte[] decoded_data = null;
            SerialPort _serialPort = new SerialPort(portName, baudRate, Parity.None , dataBits, stopBits);

            Console.WriteLine("Waiting for the start code");
            Console.WriteLine(code);

            //waiting for communication
            while (true) 
            {
                Thread.Sleep(1);
                if (_serialPort.BytesToRead > 0)
                {
                    byte[] uno_Byte = null;
                    _serialPort.Read(uno_Byte, 0, 1);
                    int transmission_code = BitConverter.ToInt32(uno_Byte, 0);
                    if (transmission_code == 0xAE) {
                        Console.WriteLine("The 0xAE was received. Started sending data");
                        break;
                    }
                    else {
                        Console.WriteLine("%d doesn't match the standard 0xAE", transmission_code);
                        Environment.Exit(666);
                    }
                }
            }
            data_transmition.Send_data_header(_serialPort, code, 3);
            int i = 0;
            while (true) 
            {
                data_transmition.Wait_for_data(_serialPort);
                data_transmition.Receive_data(_serialPort, recieved_data);
    
                if (recieved_data.type == 4)
                {
                    i = i + block_size;
                    data_transmition.Send_data(_serialPort, code, 3);
                    i += block_size;
                    if (code.Length < i)
                    {
                        break;
                    }
                    else 
                    {
                        decoded_data = data_transmition.Decode_data(code);
                        Console.WriteLine();
                    }
                }
            }


        }

    }
}
