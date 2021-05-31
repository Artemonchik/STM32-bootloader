using System;
using System.IO;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using BootloaderFileFormat;
using CommandLine;
using Force.Crc32;

namespace BootloaderFileGenerator
{
    class Program
    {
        /// <summary>
        /// main :)
        /// </summary>
        /// <param name="args">
        /// args[0] - input binary path <br/> 
        /// args[1] - encryption key path (binary) <br/>
        /// args[2] - manufacturer name <br/>
        /// args[3] - firmware version (e.g. 1.3.3.7) <br/>
        /// args[4] - custom data start address
        /// args[5] - custom data end address
        /// args[6] - output path <br/>
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
            uint startAddr = 0;
            uint endAddr = 0;
            // check ManufacturerName length
            if (manufacturerName.Length > 31)
            {
                throw new ConstraintException("Manufacturer name is too long (max: 31)");
            }
            
            // parse startAddr and endAddr
            try
            {
                startAddr = Convert.ToUInt32(opts.StartAddr, 16);
            }
            catch (FormatException)
            {
                startAddr = Convert.ToUInt32(opts.StartAddr);
            }
            try
            {
                endAddr = Convert.ToUInt32(opts.EndAddr, 16);
            }
            catch (FormatException)
            {
                endAddr = Convert.ToUInt32(opts.EndAddr);
            }
            
            // read firmware binary
            var rawFirmwareBinaryList = new List<byte>();

            using (var binaryReader =
                new BinaryReader(new FileStream(inputFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
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
            }

            while (rawFirmwareBinaryList.Count % 16 != 0)
            {
                rawFirmwareBinaryList.Add(0);
            }
            
            // calculate CRC32
            var addrCrc = Crc32Algorithm.Compute(BitConverter.GetBytes(startAddr).Concat(BitConverter.GetBytes(endAddr)).ToArray());
            var dataCrc = Crc32Algorithm.Compute(rawFirmwareBinaryList.ToArray());
            
            var firstBlock = 
                BitConverter.GetBytes(addrCrc)
                    .Concat(BitConverter.GetBytes(startAddr))
                    .Concat(BitConverter.GetBytes(endAddr))
                    .Concat(BitConverter.GetBytes(dataCrc));
            // read key
            byte[] key;
            using (var binaryReader = new BinaryReader(new FileStream(keyFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                key = binaryReader.ReadBytes(32);
            }
            // create BootloaderFile
            var file = new BootloaderFile();
            // encrypt
            var encryptedBinary =  Utilities.Encrypt(
                firstBlock.Concat(rawFirmwareBinaryList).ToArray(), 
                key,
                ref file
            );
            
            file.Data = encryptedBinary.Skip(16).ToArray();
            file.ManufacturerName = manufacturerName;
            file.FirstBlock = encryptedBinary.Take(16).ToArray();
            // parse version
            var splitVersion = version.Split(".");
            for (var i = 0; i < 4; i++)
            {
                file.FirmwareVersion[i] = ushort.Parse(splitVersion[i]);
            }

            file.WriteBootloaderFile(outputFilePath);
            Console.WriteLine(file.ToFancyString());
            Console.Write("IV: ");
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
            HelpText = "Custom data start address",
            Required = true
        )]
        public string StartAddr  { get; set; }
        
        [Value(5, 
            Default = null, 
            HelpText = "Custom data end address",
            Required = true
        )]
        public string EndAddr  { get; set; }
        
        [Value(6, 
            Default = null, 
            HelpText = "Output file path",
            Required = true
        )]
        public string OutputFilePath { get; set; }
    }
}