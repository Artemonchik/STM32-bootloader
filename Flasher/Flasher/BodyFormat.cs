using Force.Crc32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flasher
{
    class BodyFormat
    {
        public byte[] Data{ set; get; }

        public uint Crc { set; get; }

        /// <summary>
        ///
        /// </summary>
        /// <param name="someData">Length + 4bytes for crc</param>
        public BodyFormat(byte[] someData)
        {
            Data = someData.Take(someData.Length - 4).ToArray();
            Crc = BitConverter.ToUInt32(someData, (someData.Length - 4));
        }

        public static byte[] PrepareBodyToSend(byte[] someData) 

        {
            byte[] someDataWithCrc = new byte[someData.Length + 4];
            someData.CopyTo(someDataWithCrc, 0);
            Crc32Algorithm.ComputeAndWriteToEnd(someDataWithCrc);
            return someDataWithCrc;
        }

        public static bool CheckBodyCrc(byte[] data, uint crc)
        {
            return Crc32Algorithm.Compute(data) == crc;
        }
    }
}
