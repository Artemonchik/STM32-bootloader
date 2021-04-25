using System;
using System.IO;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using BootloaderFileFormat;
using System.Security.Cryptography;

namespace BootloaderFileGenerator
{
    class Program
    {
        private static byte[] _fileiv;

        //args[0] - input binary path
        //args[1] - encryption key path (binary)
        //args[2] - manufacturer name
        //args[3] - firmware version (e.g. 1.3.3.7)
        //args[4] - output path
        private static void Main(string[] args)
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

            var key = new BinaryReader(new FileStream(args[1], FileMode.Open, FileAccess.Read, FileShare.Read)).ReadBytes(32);
            var encryptedBinary =  Encrypt(rawFirmwareBinaryList.ToArray(), key);

            var file = new BootloaderFile {Data = encryptedBinary, ManufacturerName = args[2]};
            var splitVersion = args[3].Split(".");
            for (var i = 0; i < 4; i++)
            {
                file.FirmwareVersion[i] = ushort.Parse(splitVersion[i]);
            }

            file.IV = _fileiv;
            file.WriteBootloaderFile(args[4]);
            Console.WriteLine(file.ToFancyString());
        }
        
        private static byte[] Encrypt(IReadOnlyList<byte> arr, byte[] key)
        {
            var padArr = new byte[arr.Count + 16 - arr.Count % 16];
            var temp = new byte[padArr.Length];
            
            for (var i = 0; i < arr.Count; i++)
            {
                padArr[i] = arr[i];
            }

            for (var i = arr.Count; i < padArr.Length; i++)
            {
                padArr[i] = 0xFF;
            }
            var myCrypt = Aes.Create();
            myCrypt.KeySize = 256;
            myCrypt.BlockSize = 128;
            myCrypt.Key = key;
            myCrypt.Padding = PaddingMode.None;
            myCrypt.GenerateIV();
            _fileiv = myCrypt.IV;
            
            var encryptor = myCrypt.CreateEncryptor();
            var decryptor = myCrypt.CreateDecryptor();
            
            for (var i = 0; i < padArr.Length; i += 16)
            {
                encryptor.TransformBlock(padArr, i, 16, temp, i);
            }
            return temp;
        }
    }
    
}