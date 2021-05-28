using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
namespace Flasher
{
    class Receive
    {
        private static byte[] headerByteArrayCrutch = new byte[16];
        //Done
        public static TransmittedData ReceiveData(SerialPort serialPort)
        {
            TransmittedData recievedData = new TransmittedData(0, 0, new byte[5000000]);
            HeaderFormat header;
            BodyFormat body;
            while (true)
            {
                Thread.Sleep(10);
                try
                {
                    header = ReceiveHeader(serialPort);
                }
                catch (Exception)
                {
                    serialPort.DiscardInBuffer();
                    Debug.WriteLine("Wrong format Header");
                    continue;
                }
                if (!HeaderFormat.CheckHeaderCrc(header))
                {
                    serialPort.DiscardInBuffer();
                    Debug.WriteLine("Wrong CRC Header");
                    continue;
                }
                if (header.PacketNum + 1 == TransmittedData.packetCounter)
                {
                    serialPort.DiscardInBuffer();
                    Debug.WriteLine("Packet Number is invalid");
                    Send.SendACK(serialPort, TransmittedData.packetCounter);
                }
                Send.SendACK(serialPort, TransmittedData.packetCounter + 1);
                if (header.Length == 0)
                {
                    recievedData.MessageCode = header.MessageCode;
                    recievedData.Length = header.Length;
                    return recievedData;
                }
                break;
            }
            while (true)
            {
                HeaderFormat recievedHeader;
                try
                {
                    body = ReceiveFullBody(serialPort, header.Length);
                    recievedHeader = new HeaderFormat(headerByteArrayCrutch);
                }
                catch (Exception)
                {
                    serialPort.DiscardInBuffer();
                    Debug.WriteLine("Wrong format Header");
                    continue;
                }
                if (!BodyFormat.CheckBodyCrc(body.Data, body.Crc))
                {
                    serialPort.DiscardInBuffer();
                    Debug.WriteLine("When receiving Full Data package, Header CRC is invalid");
                    continue;
                }
                if (recievedHeader.PacketNum + 1 == TransmittedData.packetCounter)
                {
                    serialPort.DiscardInBuffer();
                    Debug.WriteLine("Packet Number is invalid");
                    Send.SendACK(serialPort, TransmittedData.packetCounter);
                }
                if (recievedHeader.Length == 0 || !BodyFormat.CheckBodyCrc(body.Data, body.Crc))
                {
                    serialPort.DiscardInBuffer();
                    Debug.WriteLine("CRC of Full Body is invalid");
                    continue;
                }
                Send.SendACK(serialPort, TransmittedData.packetCounter + 1);
                recievedData.MessageCode = recievedHeader.MessageCode;
                recievedData.Length = recievedHeader.Length;
                recievedData.Data = body.Data;
                break;
            }
            TransmittedData.packetCounter++;
            return recievedData;
        }
        //Done
        public static byte[] ReceiveRawHeader(SerialPort serialPort, int timeout = 1000)
        {
            byte[] rawData = new byte[20];
            Utils.ReadData(serialPort, rawData, sizeof(int) * 4);
            Debug.WriteLine($"Received header");
            return rawData;
        }
        //Done
        public static HeaderFormat ReceiveHeader(SerialPort serialPort, int timeout = 1000)
        {
            HeaderFormat header = new HeaderFormat(ReceiveRawHeader(serialPort, timeout));
            return header;
        }

        //DoneWithCrutch
        public static BodyFormat ReceiveFullBody(SerialPort serialPort, int length, int timeout = 1000)
        {
            byte[] rawData = new byte[HeaderFormat.getHeaderFormatSize() + length + 4];
            serialPort.WriteTimeout = timeout;
            Utils.ReadData(serialPort, rawData, HeaderFormat.getHeaderFormatSize() + length + 4);
            Debug.WriteLine($"Data header received");
            //Crutch
            headerByteArrayCrutch = rawData.Take(HeaderFormat.getHeaderFormatSize()).ToArray();
            byte[] bodyTmp = rawData.Skip(HeaderFormat.getHeaderFormatSize()).Take(length + 4).ToArray();
            BodyFormat dataWithCrc = new BodyFormat(bodyTmp);
            //Crutch
            return dataWithCrc;
        }

    }
}
