using System;
using System.IO;
using System.Linq;
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
        /// Creates a BootloaderFile that is read from an actual file. 
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
            var fileStream = new FileStream(filepath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write);
            var binaryWriter = new BinaryWriter(fileStream, Encoding.ASCII);
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

    }
}