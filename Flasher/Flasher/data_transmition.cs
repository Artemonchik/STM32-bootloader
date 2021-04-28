using System;
using System.IO;
using System.IO.Ports;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;

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
        public int length;
        public int type;
        public byte[] data;
        public Data_s(int length, int type, byte[] data)
        {
            this.length = length;
            this.type = type;
            this.data = data;
        }
    }


    class Data_transmition
    {
        /// <summary>
        /// Send len of data, data code and data to serial_port 
        /// with max wait of timeout milliseconds.
        /// </summary>
        /// <param name="_serialPort">- SerialPort to send</param>
        /// <param name="data"> - sending data. </param>
        /// <param name="block_size"></param>
        /// <param name="current_bytes"></param>
        /// <param name="timeout">- max time to send data(default = 1000).</param>
        public static void Send_data(SerialPort _serialPort, byte[] data, int block_size, int current_bytes, int timeout = 10000)
        {
            int curr_timeout = _serialPort.WriteTimeout;
            _serialPort.WriteTimeout = timeout;
            _serialPort.Write(data, current_bytes, sizeof(byte)* block_size);
            _serialPort.WriteTimeout = curr_timeout;


        }


        /// <summary>
        /// Send data header to Serial.
        /// </summary>
        /// <param name="_serialPort"> - serialPort to receive Data.</param>
        /// <param name="data">- sending Data. </param>
        /// <param name="transmission_code"> -  (1 - string message, 2 - error message, 3 - binary code, 4 - request_block )</param>
        /// <param name="timeout">- max time to send data(default = 1000).</param>
        public static void Send_data_header(SerialPort _serialPort, byte[] data, int transmission_code = 1, int timeout = 10000)
        {
            int length = data.Length;
            byte[] length_b = BitConverter.GetBytes(length);
            byte [] transmission_code_b =  BitConverter.GetBytes(transmission_code);

            int curr_timeout = _serialPort.WriteTimeout;
            _serialPort.WriteTimeout = timeout;

            _serialPort.Write(length_b, 0, sizeof(byte) * 4);
            _serialPort.Write(transmission_code_b, 0, sizeof(byte)*4);

            _serialPort.WriteTimeout = curr_timeout;


        }


        /// <summary>
        /// Receive SDF data from serial.
        /// </summary>
        /// <param name="_serialPort"> serialPort to receive Data.</param>
        /// <param name="timeout"> timeout(default = 1000). </param>
        /// <returns> number of bytes received and received data bytes. </returns>
        public static Data_s Receive_data(SerialPort _serialPort, int timeout = 1000000)
        {
            Data_s recieved_data = new Data_s(0, 0, new byte[5000000]);
            //Thread.Sleep(20);
            Array.Clear(recieved_data.data, 0, 30000);
            byte[] data_len_b = new byte[4];
            byte[] data_type_b = new byte[4];
            int curr_timeout = _serialPort.ReadTimeout;
            _serialPort.ReadTimeout = timeout;

            //_serialPort.Read(data_len_b, 0, sizeof(byte) * 4);
            data_len_b = ReadData(_serialPort, data_len_b, sizeof(byte) * 4);
            if (data_len_b.Length < 4) 
            {
                return recieved_data;
            }
            recieved_data.length = BitConverter.ToInt32(data_len_b, 0);

            //_serialPort.Read(data_type_b, 0, sizeof(byte) * 4);
            data_type_b = ReadData(_serialPort, data_type_b, sizeof(byte) * 4);
            recieved_data.type = BitConverter.ToInt32(data_type_b, 0);

            //_serialPort.Read(recieved_data.data, 0, sizeof(byte) * recieved_data.length);
            recieved_data.data = ReadData(_serialPort, recieved_data.data, sizeof(byte) * recieved_data.length);
            _serialPort.ReadTimeout = curr_timeout;

            return recieved_data;
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
        public static String Decode_data(Data_s data, int transmission_code = 1) 
        {
            if (transmission_code == 1 || transmission_code == 2)
            {
                //return BitConverter.ToString(data.data, data.length).Replace("-", " ");
                return Encoding.ASCII.GetString(data.data, 0, sizeof(byte) * data.length);
            }

            return "deez nuts";
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
        //Если будет работать, то перенести в другой класс
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
    }
}
