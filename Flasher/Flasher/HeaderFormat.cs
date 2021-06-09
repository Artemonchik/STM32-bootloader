using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
//using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using Force.Crc32;
namespace Flasher
{
    class HeaderFormat
    {
        public int MessageCode { set; get; }

        public int Length { set; get; }

        public int PacketNum { set; get; }

        public uint Crc { set; get; }

        public HeaderFormat(byte[] data)
        {
            MessageCode = BitConverter.ToInt32(data, 0);
            Length = BitConverter.ToInt32(data, 4);
            PacketNum = BitConverter.ToInt32(data, 8);
            Crc = BitConverter.ToUInt32(data, 12);
        }

        public HeaderFormat(int messageCode, int length, int packetNum) 
        {
            byte[] data = HeaderByteFormat( messageCode, length, packetNum);

            MessageCode = BitConverter.ToInt32(data, 0);
            Length = BitConverter.ToInt32(data, 4);
            PacketNum = BitConverter.ToInt32(data, 8);
            Crc = BitConverter.ToUInt32(data, 12);
        }
        public static int getHeaderFormatSize() {
            //return Marshal.SizeOf(typeof(HeaderFormat));
            return 16;
        }


        public static byte[] HeaderByteFormat(HeaderFormat header)
        {
            var memoryStream = new MemoryStream();
            var binaryWriter = new BinaryWriter(memoryStream);

            binaryWriter.Write(header.MessageCode);
            binaryWriter.Write(header.Length);
            binaryWriter.Write(header.PacketNum);
            binaryWriter.Write(header.Crc);
            binaryWriter.Close();
            return memoryStream.ToArray();
        }
        public static byte[] HeaderByteFormat(int messageCode, int length, int packetNum)
        {
            var memoryStream = new MemoryStream();
            var binaryWriter = new BinaryWriter(memoryStream);

            binaryWriter.Write(messageCode);
            binaryWriter.Write(length);
            binaryWriter.Write(packetNum);
            binaryWriter.Write(0); // for crc
            binaryWriter.Close();
            byte[] data = memoryStream.ToArray();

            uint crc = Crc32Algorithm.ComputeAndWriteToEnd(data);
            return data;
        }

        public static bool CheckHeaderCrc(HeaderFormat headerReceived)
        {
            HeaderFormat headerComputed = new HeaderFormat(headerReceived.MessageCode, headerReceived.Length, headerReceived.PacketNum);
            return headerReceived.Crc == headerComputed.Crc;
        }
    }
}
