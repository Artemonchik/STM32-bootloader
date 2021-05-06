using System;
using System.IO;
using System.IO.Ports;
using System.Runtime.Remoting.Messaging;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Runtime;
using Force.Crc32;
using System.Reflection;
using System.Linq;

namespace Flasher
{
    /// <summary>
    /// This module going to send data by following format SDF (size defined format):
    /// 0 - 3 byte - integer which indicates len of data we want to transmit
    /// 4 - 7 byte - code of transmited data
    /// 8 - ... byte - data we want to send / receive
    /// </summary>
    struct Data_s
    {
        public int code;
        public int length;
        public byte[] data;
        public Data_s( int code, int length, byte[] data)
        {
            this.code = code;
            this.length = length;
            this.data = data;
        }
    }
    struct Body_s
    {
        public byte[] data;
        public uint crc;
        public Body_s(byte[] data, uint crc)
        {
            this.data = data;
            this.crc = crc;
        }
    }
    struct Header_s
    {
        public int message_code;
        public int length;
        public int num;
        public uint crc;
        public Header_s(int message_code, int length, int num, uint crc)
        {
            this.message_code = message_code;
            this.length = length;
            this.num = num;
            this.crc = crc;
        }
    }
    static class Transmition
    {
        public const int START_CODE = 0xAE;
        public const int STRING_MESSAGE = 1;
        public const int ERROR_MESSAGE = 2;
        public const int PROGRAM = 3;
        public const int REQUEST = 4;
        public const int ACK = 5;
        public const int NEXT = 6;
        public const int BAUDRATE = 7;
        public const int TIMEOUT = 8;
        public const int RELEASE = 9;
        public const int SECRET_KEY = 10;
    }


    class Data_transmition
    {

        private static int packet_counter = 0;


        /// <summary>
        /// Send data header to Serial.
        /// </summary>
        /// <param name="_serialPort"> - serialPort to receive Data.</param>
        /// <param name="data">- sending Data. </param>
        /// <param name="transmission_code"> -  (1 - string message, 2 - error message, 3 - binary code, 4 - request_block )</param>
        /// <param name="timeout">- max time to send data(default = 1000).</param>
        public static void SendDataHeader(SerialPort _serialPort, int transmission_code, int length, int num,  int timeout = 10000)
        {
            int curr_timeout = _serialPort.WriteTimeout;
            _serialPort.WriteTimeout = timeout;
            byte[] header = MakeRawDataHeader(transmission_code, length, num);
            _serialPort.Write(header, 0, sizeof(int) * 4);
            _serialPort.WriteTimeout = curr_timeout;
        }

        public static void SendRawDataBody(SerialPort _serialPort, byte[] raw_data) 
        {
            byte[] raw_data_crc = new byte[raw_data.Length + 4];
            raw_data.CopyTo(raw_data_crc, 0);
            Crc32Algorithm.ComputeAndWriteToEnd(raw_data_crc); 
            _serialPort.Write(raw_data_crc, 0, raw_data_crc.Length);
        }

        public static bool SendRawData(SerialPort _serialPort, int transmition_code, int length, byte[] body, int timeout = 10000)
        {
            Header_s header;
            _serialPort.WriteTimeout = timeout;
            while (true)
            {
                Thread.Sleep(10);
                SendDataHeader(_serialPort, transmition_code, length, packet_counter, timeout);
                try
                {
                    header = ReceiveDataHeader(_serialPort);
                }
                catch (Exception)
                {
                    Console.WriteLine("Error occured, retrying");
                    continue;
                }
                if (header.message_code != Transmition.ACK)
                {
                    continue;
                }
                break;
            }

            while (true)
            {
                if (length == 0)
                {
                    return true;
                }
                SendRawDataBody(_serialPort, body);
                try
                {
                    header = ReceiveDataHeader(_serialPort);
                }
                catch (Exception)
                {
                    Console.WriteLine("Error occured, retrying");
                    continue;
                }
                if (header.message_code != Transmition.ACK)
                {
                    continue;
                }
                return true;
            }
        }

        public static Data_s ReceiveRawData(SerialPort _serialPort) 
        {
            Data_s recieved_data = new Data_s(0, 0, new byte[5000000]);
            Header_s header;
            Body_s body = new Body_s(new byte[5000000], 0);
            while (true) 
            {
                try
                {
                    header = ReceiveDataHeader(_serialPort);
                }
                catch (Exception)
                {
                    continue;
                }
                if (!CheckHeaderCrc(header)) 
                {
                    continue;
                }
                SendACK(_serialPort, header.num + 1);
                if (header.length == 0)
                {
                    recieved_data.code = header.message_code;
                    recieved_data.length = header.length;
                    return recieved_data;

                }
                break;
            }
            while (true)
            {
                try
                {
                    body = ReceiveDataBody(_serialPort, header.length);
                }
                catch (Exception)
                {
                    continue;
                }
                if (!CheckBodyCrc(body.data, body.crc))
                {
                    continue;
                }
                SendACK(_serialPort, header.num + 1);
                recieved_data.code = header.message_code;
                recieved_data.length = header.length;
                recieved_data.data = body.data;
                return recieved_data;
            }

        }

        /// <summary>
        /// Decoding from bytes to string, if it's string or error message
        /// </summary>
        /// <param name="data"> sending Data.</param>
        /// <param name="transmission_code"> 
        /// 1 - string message</br>
        /// 2 - error message</br>
        /// 3 - binary code</br>
        /// 4 - requset block</param>
        /// <returns> Decoded string or null </returns>
        public static string Decode_data(Data_s data, int transmission_code = 1)
        {
            if (transmission_code == Transmition.STRING_MESSAGE || transmission_code == Transmition.ERROR_MESSAGE)
            {
                //return BitConverter.ToString(data.data, data.length).Replace("-", " ");
                return Encoding.ASCII.GetString(data.data, 0, sizeof(byte) * data.length);
            }
            return "Nope";
        }

        /// <summary>
        /// Waiting for data to read from serial port 
        /// </summary>
        /// <param name="_serialPort">opened serialPort</param>
        public static void Wait_for_data(SerialPort _serialPort)
        {
            while (true)
            {
                if (_serialPort.BytesToRead > 0)
                {
                    return;
                }
            }

        }
        public static byte[] addByteToArray(byte[] bArray, byte newByte)
        {
            byte[] newArray = new byte[bArray.Length + 1];
            bArray.CopyTo(newArray, 1);
            newArray[0] = newByte;
            return newArray;
        }

        public static byte[] ReadData(SerialPort _serialPort, byte[] responseBytes, int bytesExpected, int timeOut = 1000)
        {
            _serialPort.ReadTimeout = timeOut;
            int offset = 0, bytesRead;
            while (bytesExpected > 0 &&
              (bytesRead = _serialPort.Read(responseBytes, offset, bytesExpected)) > 0)
            {
                offset += bytesRead;
                bytesExpected -= bytesRead;
            }
            return responseBytes;
        }

        public static byte[] ReceiveRawDataHeader(SerialPort _serialPort , int timeout = 1000) {
            byte[] raw_data = new byte[20];
            ReadData(_serialPort, raw_data, sizeof(int) * 4);
            return raw_data;
        }

        public static Header_s ReceiveDataHeader(SerialPort _serialPort, int timeout = 1000)
        {
            return ParseHeader(ReceiveRawDataHeader(_serialPort, timeout));
        }

        public static  Body_s ReceiveDataBody(SerialPort _serialPort, int length, int timeout = 1000) 
        {
            byte[] raw_data = new byte[length + 4];
            _serialPort.WriteTimeout = timeout;
            ReadData(_serialPort, raw_data, length + 4);
            Body_s data_with_crc = new Body_s {data = raw_data.Take(length).ToArray() , crc = BitConverter.ToUInt32(raw_data, length) };
            return data_with_crc;
        }
        public static byte[] MakeRawDataHeader(int message_code, int length, int packet_num) 
        {
            var str = new MemoryStream();
            var bw = new BinaryWriter(str);
            bw.Write(message_code);
            bw.Write(length);
            bw.Write(packet_num);
            bw.Write(0); // for crc
            byte[] raw_data = str.ToArray();

            uint crc = Crc32Algorithm.ComputeAndWriteToEnd(raw_data);

            return raw_data;


        }

        public static Header_s ParseHeader(byte[] _header) 
        {
            Header_s header;
            header.message_code = BitConverter.ToInt32(_header, 0);
            header.length = BitConverter.ToInt32(_header, 4);
            header.num = BitConverter.ToInt32(_header, 8);
            header.crc = BitConverter.ToUInt32(_header, 12);
            return header;

        }
        public static void SendACK(SerialPort _serialPort, int packet_num)
        {
            byte[] raw_data = MakeRawDataHeader(Transmition.ACK, 0, packet_num);
            _serialPort.Write(raw_data, 0, raw_data.Length);
        }

        public static bool CheckHeaderCrc(Header_s header_received)
        {
            Header_s header_computed = ParseHeader(MakeRawDataHeader(header_received.message_code, header_received.length, header_received.num));
            return header_received.crc == header_computed.crc;
        }   
        public static bool CheckBodyCrc(byte[] data, uint crc) {
            return Crc32Algorithm.Compute(data) == crc;
        }

    }
}
