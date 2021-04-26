using System;
using System.IO;
using System.Collections.Generic;
using BootloaderFileFormat;
using System.Security.Cryptography;
using CommandLine;

namespace BootloaderFileGenerator
{
    class Program
    {
        private static byte[] _fileiv;

        
        /// <summary>
        /// main :)
        /// </summary>
        /// <param name="args">
        /// args[0] - input binary path <br/> 
        /// args[1] - encryption key path (binary) <br/>
        /// args[2] - manufacturer name <br/>
        /// args[3] - firmware version (e.g. 1.3.3.7) <br/>
        /// args[4] - output path <br/>
        /// </param>
        private static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args).WithParsed<Options>(RunOptions);
            
        }

        private static void RunOptions(Options opts)
        {
            var inputFilePath = opts.InputFilePath;
            var keyFilePath = opts.KeyFilePath;
            var manufacturerName = opts.ManufacturerName;
            var version = opts.Version;
            var outputFilePath = opts.OutputFilePath;
            
            var binaryReader =
                new BinaryReader(new FileStream(inputFilePath, FileMode.Open, FileAccess.Read, FileShare.Read));
            
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

            var key = new BinaryReader(new FileStream(keyFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)).ReadBytes(32);
            var encryptedBinary =  Encrypt(rawFirmwareBinaryList.ToArray(), key);

            var file = new BootloaderFile {Data = encryptedBinary, ManufacturerName = manufacturerName};
            var splitVersion = version.Split(".");
            for (var i = 0; i < 4; i++)
            {
                file.FirmwareVersion[i] = ushort.Parse(splitVersion[i]);
            }

            file.IV = _fileiv;
            file.WriteBootloaderFile(outputFilePath);
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
            
            for (var i = 0; i < padArr.Length; i += 16)
            {
                encryptor.TransformBlock(padArr, i, 16, temp, i);
            }
            return temp;
        }
        
    }

    internal class Options
    {
        [Value(0, 
            Default = null, 
            HelpText = "Firmware file binary",
            Required = true
        )]
        public string InputFilePath { get; set; }
        
        [Value(1, 
            Default = null, 
            HelpText = "Key file",
            Required = true
        )]
        public string KeyFilePath { get; set; }
        
        [Value(2, 
            Default = null, 
            HelpText = "Manufacturer",
            Required = true
        )]
        public string ManufacturerName { get; set; }
        
        [Value(3, 
            Default = null, 
            HelpText = "Version (should look like 1.2.3.4)",
            Required = true
        )]
        public string Version { get; set; }
        
        [Value(4, 
            Default = null, 
            HelpText = "Output file path",
            Required = true
        )]
        public string OutputFilePath { get; set; }
    }
}