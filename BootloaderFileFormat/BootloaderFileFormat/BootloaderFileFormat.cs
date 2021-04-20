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
        public byte[] Data
        {
            set
            {
                _data = value;
                _size = value.Length;
            }
            get => _data;
        }

        private short _manufacturerNameSize;
        private int _size { set; get; }
        private long _unixCreationTime { set; get; }
        public ushort[] FirmwareVersion { set; get; }

        public BootloaderFile()
        {
            FirmwareVersion = new ushort[4];
            _unixCreationTime = DateTimeOffset.Now.ToUnixTimeSeconds();
        }
        
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
            
            _unixCreationTime = binaryReader.ReadInt64();

            if (!binaryReader.ReadBytes(4).SequenceEqual(DataBytes))
            {
                throw new FormatException();
            }
            
            _size = binaryReader.ReadInt32();
            Data = binaryReader.ReadBytes(_size);
            binaryReader.Close();
        }

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
            binaryWriter.Write(_unixCreationTime);
            binaryWriter.Write(DataBytes);
            binaryWriter.Write(_size);
            binaryWriter.Write(Data);
            binaryWriter.Close();
        }

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
            stringBuilder.AppendLine(DateTimeOffset.FromUnixTimeSeconds(_unixCreationTime).ToString());
            stringBuilder.Append("Firmware size: ");
            stringBuilder.Append(_size);
            stringBuilder.AppendLine("B");
            return stringBuilder.ToString();
        }

    }
}