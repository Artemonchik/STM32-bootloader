using System;
using System.IO;
using System.Collections.Generic;
using BootloaderFileFormat;

namespace BootloaderFileGenerator
{
    class Program
    {
        //args[0] - input binary path
        //args[1] - manufacturer name
        //args[2] - firmware version (e.g. 1.3.3.7)
        //args[3] - output path
        static void Main(string[] args)
        {
            var binaryReader =
                new BinaryReader(new FileStream(args[0], FileMode.Open, FileAccess.Read, FileShare.Read));
            
            var rawFirmwareBinaryList = new List<byte>();
            while (true)
            {
                byte tempByte;
                try
                {
                    tempByte = binaryReader.ReadByte();
                }
                catch (EndOfStreamException)
                {
                    break;
                }

                rawFirmwareBinaryList.Add(tempByte);
            }

            var encryptedBinary =  Encrypt(rawFirmwareBinaryList.ToArray());

            var file = new BootloaderFile {Data = encryptedBinary, ManufacturerName = args[1]};
            var splitVersion = args[2].Split(".");
            for (var i = 0; i < 4; i++)
            {
                file.FirmwareVersion[i] = ushort.Parse(splitVersion[i]);
            }
            file.WriteBootloaderFile(args[3]);
            Console.WriteLine(file.ToFancyString());
        }

        //TBD
        private static byte[] Encrypt(byte[] arr)
        {
           return arr;
        }
    }
    
}