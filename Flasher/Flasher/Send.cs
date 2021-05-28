using Force.Crc32;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
//using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Flasher
{
    class Send
    {
        //Done
        public static void SendHeader(SerialPort serialPort, int transmissionCode, int length, int packetNum, int timeout = 10000)
        {
            int curr_timeout = serialPort.WriteTimeout;
            serialPort.WriteTimeout = timeout;
            byte[] header = HeaderFormat.HeaderByteFormat(transmissionCode, length, packetNum);
            serialPort.Write(header, 0, sizeof(int) * 4);
            Debug.WriteLine($"Data header sended");
            serialPort.WriteTimeout = curr_timeout;
        }
        //Done
        public static void SendFullBody(SerialPort serialPort, byte[] header,  byte[] rawData)
        {
            byte[] fullDataWithCrc = Utils.appendTwoArrays(header, BodyFormat.PrepareBodyToSend(rawData));
            serialPort.Write(fullDataWithCrc, 0, fullDataWithCrc.Length);
            Debug.WriteLine($"Full body sended");
        }
        //dOnE
        public static bool SendData(SerialPort serialPort, int transmissionCode, int length, byte[] body, int timeout = 1000)
        {
            HeaderFormat header;
            serialPort.WriteTimeout = timeout;
            while (true)
            {
                //bruh
                Thread.Sleep(10);
                //bruh
                SendHeader(serialPort, transmissionCode, length, TransmittedData.packetCounter, timeout);
                try
                {
                    header = Receive.ReceiveHeader(serialPort);
                }
                catch (Exception)
                {
                    serialPort.DiscardInBuffer();
                    Debug.WriteLine("Error occured, retrying in sendinng data Header");
                    continue;
                }
                if (!(HeaderFormat.CheckHeaderCrc(header))) {
                    serialPort.DiscardInBuffer();
                    Debug.WriteLine("Header CRC is invalid");
                    continue;
                }
                if (header.PacketNum + 1 == TransmittedData.packetCounter) {
                    serialPort.DiscardInBuffer();
                    Debug.WriteLine("Packet Number is invalid");
                    continue;
                }
                break;
            }

            TransmittedData.packetCounter++;
            if (length == 0)
            {
                return true;
            }

            while (true)
            {
                SendFullBody(serialPort, HeaderFormat.HeaderByteFormat(transmissionCode, length, TransmittedData.packetCounter), body);
                try
                {
                    header = Receive.ReceiveHeader(serialPort);
                }
                catch (Exception)
                {
                    serialPort.DiscardInBuffer();
                    Debug.WriteLine("Error occured, retrying in sendinng Full Data");
                    continue;
                }
                if (!(HeaderFormat.CheckHeaderCrc(header)))
                {
                    serialPort.DiscardInBuffer();
                    Debug.WriteLine("When receiving Full Data package, Header CRC is invalid");
                    continue;
                }
                if (header.PacketNum + 1 == TransmittedData.packetCounter)
                {
                    serialPort.DiscardInBuffer();
                    Debug.WriteLine("Packet Number is invalid");
                    continue;
                }
                TransmittedData.packetCounter++;
                return true;
            }
        }
        //Done
        public static void SendACK(SerialPort serialPort, int packetNum, int timeout = 10000)
        {
            byte[] rawData = HeaderFormat.HeaderByteFormat(Transmission.ACK, 0, packetNum);
            serialPort.Write(rawData, 0, rawData.Length);
            Debug.WriteLine("Acknowledge sended");
        }
    }
}
