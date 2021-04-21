using System;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Threading;

namespace Flasher
{
    /*
        This module going to send data by following format SDF (size defined format):
        0 - 3 byte - integer which indicates len of data we want to transmit
        4 - 7 byte - code of transmited data
        8 - ... byte - data we want to send / receive

    # CODE TABLE #
        1 - string message
        2 - error message
        3 - binary code
    */
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
    class data_transmition
    {
        /*
         * Send len of data, data code and data to serial_port with max wait of timeout milliseconds
         * :param serial_port: port to send
         * :param data: data
         * :param timeout: max time to send data
         * :param: code from code Table
         * :return: no
         * :except: SerialTimeoutException – in a case a {timeout} time is exceeded
         */
        public static void Send_data(SerialPort _serialPort, byte[] data, int timeout = 10000)
        {

            int length = data.Length;
            int curr_timeout = 0;

            try 
            {
                curr_timeout = _serialPort.WriteTimeout;
                _serialPort.WriteTimeout = timeout;
                _serialPort.Open();
            }
            catch (UnauthorizedAccessException Ex) 
            {
                Console.WriteLine(Ex.ToString());
                Console.WriteLine("Access is denied to the port.");
                _serialPort.Close();
            }
            catch (ArgumentOutOfRangeException Ex) 
            {
                Console.WriteLine(Ex.ToString());
                Console.WriteLine("One of the properties is invalid");
                _serialPort.Close();
            }
            catch (IOException Ex) 
            {
                Console.WriteLine(Ex.ToString());
                Console.WriteLine("The port is in an invalid state");
                _serialPort.Close();
            }
            catch (InvalidOperationException Ex) 
            {
                Console.WriteLine(Ex.ToString());
                Console.WriteLine("SerialPort is already open");
                _serialPort.Close();
            }

            _serialPort.Write(data, 0, length);
            _serialPort.WriteTimeout = curr_timeout;
            _serialPort.Close();


        }
        public static void Send_data_header(SerialPort _serialPort, byte[] data, int transmission_code = 1, int timeout = 10000)
        {
            int length = data.Length;

            int curr_timeout = _serialPort.WriteTimeout;
            _serialPort.WriteTimeout = timeout;

            _serialPort.Write(BitConverter.GetBytes(length), 0, length);
            _serialPort.Write(BitConverter.GetBytes(transmission_code), 0, length);

            _serialPort.WriteTimeout = curr_timeout;


        }

        /**
         * Receive SDF data from serial
         * -param serial_port:
         * -param timeout:
         * +return: number of bytes received and received data bytes
         */
        public static Data_s Receive_data(SerialPort _serialPort, Data_s recieved_data, int timeout = 1000)
        {
            byte[] bytes_number_b = null;
            byte[] data_type_b = null;
            int curr_timeout = 0;
            try
            {
                curr_timeout = _serialPort.ReadTimeout;
                _serialPort.ReadTimeout = timeout;
                _serialPort.Open();
            }
            catch (UnauthorizedAccessException Ex)
            {
                Console.WriteLine(Ex.ToString());
                Console.WriteLine("Access is denied to the port.");
                _serialPort.Close();
            }
            catch (ArgumentOutOfRangeException Ex)
            {
                Console.WriteLine(Ex.ToString());
                Console.WriteLine("One of the properties is invalid");
                _serialPort.Close();
            }
            catch (IOException Ex)
            {
                Console.WriteLine(Ex.ToString());
                Console.WriteLine("The port is in an invalid state");
                _serialPort.Close();
            }
            catch (InvalidOperationException Ex)
            {
                Console.WriteLine(Ex.ToString());
                Console.WriteLine("SerialPort is already open");
                _serialPort.Close();
            }

            _serialPort.Read(bytes_number_b, 0, 4);
            if (bytes_number_b.Length < 4) 
            {
                return recieved_data;
            }
            recieved_data.length = BitConverter.ToInt32(bytes_number_b, 0);

            _serialPort.Read(data_type_b, 0, 4);
            recieved_data.length = BitConverter.ToInt32(data_type_b, 0);

            _serialPort.Read(recieved_data.data, 0, recieved_data.length);
            _serialPort.ReadTimeout = curr_timeout;

            return recieved_data;
        }
        public static byte[] Decode_data(byte[] data, int transmission_code = 1) 
        {
            if (transmission_code == 1 || transmission_code == 2)
            {

                return Encoding.Convert(Encoding.ASCII, Encoding.Unicode, data);
            }

            return data;
        }

        public static void Wait_for_data(SerialPort _serialPort)
        {
            while (true)
            {
                Thread.Sleep(1);
                if (_serialPort.BytesToRead > 0)
                {
                    return;
                }
            }

        }
        
    }
}
