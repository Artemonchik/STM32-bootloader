using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace BootloaderFileFormat
{
    public class BootloaderFile
    {
        private static readonly byte[] HeaderBytes = { (byte)'H', (byte)'E', (byte)'A', (byte)'D', (byte)'E', (byte)'R' };
        private static readonly byte[] DataBytes = {(byte) 'D', (byte) 'A', (byte) 'T', (byte) 'A'};
        
        
        private string _manufacturerName;
        /// <summary>
        /// The manufacturer's name. 
        /// </summary>
        public string ManufacturerName
        {
            set
            {
                _manufacturerName = value;
                _manufacturerNameSize = (short) value.Length;
            }
            get => _manufacturerName;
        }

        private byte[] _data;
        /// <summary>
        /// Cipher text.
        /// </summary>
        public byte[] Data
        {
            set
            {
                _data = value;
                Size = value.Length;
            }
            get => _data;
        }

        private short _manufacturerNameSize;
        /// <summary>
        /// Size of the cipher text.
        /// </summary>
        private int Size { set; get; }
        /// <summary>
        /// Time at which the BootloaderFile was created.
        /// </summary>
        private long UnixCreationTime { set; get; }
        /// <summary>
        /// Version of the firmware.
        /// </summary>
        public ushort[] FirmwareVersion { set; get; }
        /// <summary>
        /// Initialization vector used while encrypting the cipher text.
        /// </summary>
        public byte[] IV { set; get; }
        public byte[] FirstBlock { get; set; }

        /// <summary>
        /// Creates an empty BootloaderFile <br/>
        /// with current time in UnixCreationTime.  
        /// </summary>
        public BootloaderFile()
        {
            FirmwareVersion = new ushort[4];
            UnixCreationTime = DateTimeOffset.Now.ToUnixTimeSeconds();
        }
        
        /// <summary>
        /// Creates a BootloaderFile that is read from a file. 
        /// </summary>
        /// <param name="filepath">
        /// Path to the file.
        /// </param>
        /// <exception cref="FormatException">
        /// Thrown when the file is formatted improperly.
        /// </exception>
        public BootloaderFile(string filepath)
        {
            var fileStream = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var binaryReader = new BinaryReader(fileStream, Encoding.ASCII);
            if (!binaryReader.ReadBytes(6).SequenceEqual(HeaderBytes))
            {
                throw new FormatException();
            }

            _manufacturerNameSize = binaryReader.ReadInt16();
            ManufacturerName = new string(binaryReader.ReadChars(_manufacturerNameSize));
            
            FirmwareVersion = new ushort[4];
            for (var i = 0; i < 4; i++)
            {
                FirmwareVersion[i] = binaryReader.ReadUInt16();    
            }
            
            UnixCreationTime = binaryReader.ReadInt64();

            if (!binaryReader.ReadBytes(4).SequenceEqual(DataBytes))
            {
                throw new FormatException();
            }

            IV = binaryReader.ReadBytes(16);
            
            Size = binaryReader.ReadInt32();
            FirstBlock = binaryReader.ReadBytes(16);
            Data = binaryReader.ReadBytes(Size);
            binaryReader.Close();
        }
        
        /// <summary>
        /// Writes the BootloaderFile on disk. 
        /// </summary>
        /// <param name="filepath">
        /// Path to the file that is going to be written to.
        /// </param>
        public void WriteBootloaderFile(string filepath)
        {
            if (!File.Exists(filepath))
            {
                File.Create(filepath);
            }
            using var fileStream = new FileStream(filepath, FileMode.Truncate, FileAccess.Write, FileShare.Write);
            using var binaryWriter = new BinaryWriter(fileStream, Encoding.ASCII);
            binaryWriter.Write(HeaderBytes);
            binaryWriter.Write(_manufacturerNameSize);
            binaryWriter.Write(ManufacturerName.ToCharArray());
            for (var i = 0; i < 4; i++)
            {
                binaryWriter.Write(FirmwareVersion[i]);    
            }
            binaryWriter.Write(UnixCreationTime);
            binaryWriter.Write(DataBytes);
            binaryWriter.Write(IV);
            binaryWriter.Write(Size);
            binaryWriter.Write(FirstBlock);
            binaryWriter.Write(Data);
            binaryWriter.Close();
        }

        /// <summary>
        /// Makes BootloaderFile look fancy in a string format.
        /// </summary>
        /// <returns>
        /// A fancy representation of the BootloaderFile's metainfo.
        /// </returns>
        public string ToFancyString()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append("Manufacturer: ");
            stringBuilder.AppendLine(ManufacturerName);
            stringBuilder.Append("Version: ");
            for (var i = 0; i < 4; i++)
            {
                stringBuilder.Append(FirmwareVersion[i]);
                if (i < 3)
                {
                    stringBuilder.Append('.');
                }
            }
            stringBuilder.AppendLine();
            stringBuilder.Append("Creation time: ");
            stringBuilder.AppendLine(DateTimeOffset.FromUnixTimeSeconds(UnixCreationTime).ToString());
            stringBuilder.Append("Firmware size: ");
            stringBuilder.Append(Size);
            stringBuilder.AppendLine("B");
            return stringBuilder.ToString();
        }

        /// <summary>
        /// Packs this BootloaderFile's metadata into a byte array
        /// </summary>
        /// <returns>A byte array representing this BootloaderFile's metadata</returns>
        public byte[] GetMetadataAsByteArray()
        {
            byte[] zeros = new byte[32 - ManufacturerName.Length];
            var arr = Array.Empty<byte>();
            arr = arr
                .Concat(HeaderBytes)
                .Concat(Encoding.ASCII.GetBytes(ManufacturerName))
                .Concat(zeros)
                .Concat(BitConverter.GetBytes(FirmwareVersion[0]))
                .Concat(BitConverter.GetBytes(FirmwareVersion[1]))
                .Concat(BitConverter.GetBytes(FirmwareVersion[2]))
                .Concat(BitConverter.GetBytes(FirmwareVersion[3]))
                .Concat(BitConverter.GetBytes(UnixCreationTime))
                .Concat(DataBytes)
                .Concat(IV)
                .Concat(BitConverter.GetBytes(Size))
                .ToArray();
            return arr;
        }

        /// <summary>
        /// Sets this BootloaderFile's metadata to metadata contained in an array.
        /// This is supposed to be done with an empty BootloaderFile to prevent data loss.
        /// </summary>
        /// <param name="arr">Byte array containing metadata</param>
        /// <exception cref="FormatException">If metadata doesn't contain "HEADER" and "DATA bytes in place"</exception>
        public void GetMetadataFromByteArray(byte[] arr)
        {
            using var reader = new BinaryReader(new MemoryStream(arr));
            if (!reader.ReadBytes(6).SequenceEqual(HeaderBytes))
            {
                throw new FormatException();
            }
            
            
            var temparr = reader.ReadBytes(32);
            ManufacturerName = "";
            foreach (var b in temparr)
            {
                if (b == 0)
                    break;
                ManufacturerName += Convert.ToChar(b);
            }
            
            for (var i = 0; i < 4; i++)
            {
                FirmwareVersion[i] = reader.ReadUInt16();
            }

            UnixCreationTime = reader.ReadInt64();

            if (!reader.ReadBytes(4).SequenceEqual(DataBytes))
            {
                throw new FormatException();
            }

            IV = reader.ReadBytes(16);
            Size = reader.ReadUInt16();
        }
    }

    public static class Utilities
    {
        /// <summary>
        /// Encrypts a byte array in context of a BootloaderFile.
        /// </summary>
        /// <param name="arr">Array to be encrypted</param>
        /// <param name="key">Key to encrypt the array with</param>
        /// <param name="file">A BootloaderFile (so that the generated IV can be stored in it)</param>
        /// <returns>Encrypted byte array</returns>
        public static byte[] Encrypt(IReadOnlyList<byte> arr, byte[] key, ref BootloaderFile file)
        {
            var padArr = new byte[arr.Count];
            var temp = new byte[padArr.Length];
            
            for (var i = 0; i < arr.Count; i++)
            {
                padArr[i] = arr[i];
            }
            var myCrypt = Aes.Create();
            myCrypt.KeySize = 256;
            myCrypt.BlockSize = 128;
            myCrypt.Key = key;
            myCrypt.Padding = PaddingMode.None;
            myCrypt.GenerateIV();
            file.IV = myCrypt.IV;
            
            var encryptor = myCrypt.CreateEncryptor();
            
            for (var i = 0; i < padArr.Length; i += 16)
            {
                encryptor.TransformBlock(padArr, i, 16, temp, i);
            }
            return temp;
        }
        /// <summary>
        /// Decrypts an array.
        /// </summary>
        /// <param name="arr">Array to decrypt</param>
        /// <param name="key">Key for decryption</param>
        /// <param name="iv">IV for decryption</param>
        /// <returns>Decrypted array</returns>
        public static byte[] Decrypt(byte[] arr, byte[] key, byte[] iv)
        {
            var myCrypt = Aes.Create();
            myCrypt.KeySize = 256;
            myCrypt.Key = key;
            myCrypt.Padding = PaddingMode.None;
            myCrypt.IV = iv;
            var decryptor = myCrypt.CreateDecryptor();
            var outarr = new byte[arr.Length];
            for (var i = 0; i < arr.Length; i += 16)
            {
                decryptor.TransformBlock(arr, i, 16, outarr, i);
            }

            return outarr;
        }
        /// <summary>
        /// Calculates a CRC32 for a given byte array.
        /// </summary>
        /// <param name="arr">Array to calculate CRC32 for.</param>
        /// <returns>CRC32</returns>
        public static uint CalculateCrc32(byte[] arr)
        {
            uint crc=0xFFFFFFFF;
            foreach (var t in arr)
            {
                var ch = (sbyte)t;
                for(var j=0; j < 8; j++) {
                    var b = (uint)((ch ^ crc) & 1);
                    crc >>= 1;
                    if (b != 0)
                        crc ^= 0xEDB88320;
                    ch >>= 1;
                }
            }

            return ~crc;
        }
    }
}