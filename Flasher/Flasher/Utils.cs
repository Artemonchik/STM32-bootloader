using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flasher
{
    class Utils
    {
       
        public static string Decode_data(TransmittedData data)
        {
            if (data.MessageCode == Transmission.STRING_MESSAGE || data.MessageCode == Transmission.ERROR_MESSAGE)
            {
                //return BitConverter.ToString(data.data, data.length).Replace("-", " ");
                return Encoding.ASCII.GetString(data.Data, 0, sizeof(byte) * data.Length);
            }
            return "Nope";
        }

        public static void Wait_for_data(SerialPort serialPort)
        {
            while (true)
            {
                if (serialPort.BytesToRead > 0)
                {
                    return;
                }
            }

        }
        public static byte[] addByteToArray(byte[] byteArray, byte newByteToAdd)
        {
            byte[] newArray = new byte[byteArray.Length + 1];
            byteArray.CopyTo(newArray, 1);
            newArray[0] = newByteToAdd;
            return newArray;
        }
        public static byte[] appendTwoArrays(byte[] byteArray1, byte[] byteArray2)
        {
            byte[] newArray = new byte[byteArray1.Length + byteArray2.Length];
            System.Buffer.BlockCopy(byteArray1, 0, newArray, 0, byteArray1.Length);
            System.Buffer.BlockCopy(byteArray2, 0, newArray, byteArray1.Length, byteArray2.Length);
            return newArray;
        }
        public static byte[] ReadData(SerialPort serialPort, byte[] responseBytes, int bytesExpected, int timeout = 1000)
        {
            serialPort.ReadTimeout = timeout;
            int offset = 0, bytesRead;
            while (bytesExpected > 0 &&
              (bytesRead = serialPort.Read(responseBytes, offset, bytesExpected)) > 0)
            {
                offset += bytesRead;
                bytesExpected -= bytesRead;
            }
            return responseBytes;
        }
    }
}
