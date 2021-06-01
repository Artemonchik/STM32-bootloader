using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using BootloaderFileFormat;
using System.IO;

namespace Flasher
{
    static class Transmission
    {
        public const int START_CODE = 0xAE;
        public const int STRING_MESSAGE = 1;
        public const int ERROR_MESSAGE = 2;
        public const int PROGRAM = 3;
        public const int REQUEST = 4;
        public const int ACK = 5;
        public const int NEXT = 6;
        public const int BAUDRATE = 7;
        public const int TIMEOUT = 8;
        public const int RELEASE = 9;
        public const int FIRMWARE_INFO_FROM_BOOTLOADER = 10;
        public const int FIRMWARE_INFO = 11;
        public const int ADDRESSES_INFO = 12;
        public const int DOWNLOAD_CODE = 13;
        public const int END_OF_DOWNLOAD = 14;
        public const int DOWNLOAD_DATA = 15;
        public const int DATA = 15;

        public static int[] TransmissionForConnect = { BAUDRATE, FIRMWARE_INFO_FROM_BOOTLOADER, 100 };
        public static int[] TransmissionForUpload = { BAUDRATE, FIRMWARE_INFO, ADDRESSES_INFO, PROGRAM, 100 };
        public static int[] TransmissionForDisconnect = { BAUDRATE, RELEASE, 100 };
        public static int[] TransmissionForDownloadCode = { BAUDRATE, DOWNLOAD_CODE, 100 };
        public static int[] TransmissionForDownloadData = { BAUDRATE, DOWNLOAD_DATA, 100 };
    }
    class TransmittedData
    {
        public int MessageCode { set; get; }

        public int Length { set; get; }
        public byte[] Data { set; get; }
        public TransmittedData(int code, int length, byte[] data)
        {
            this.MessageCode = code;
            this.Length = length;
            this.Data = data;
        }

        public static int packetCounter = 0;
        public static int transmissionCode = Transmission.FIRMWARE_INFO;
        public static byte[] downloadedCode = new byte[0];
        public static byte[] downloadedData = new byte[0];
        public static byte[] downloadedSmth = new byte[0];
        public static BootloaderFile recievedMetaInfo = new BootloaderFile();
        public static BootloaderFile downloadedFile = new BootloaderFile();
        public static void StartCommunication(SerialPort serialPort) {
            Debug.WriteLine("Waiting for the start code");

            //waiting for communication with SerialPort
            Debug.WriteLine("Waiting for communication");
            while (true)
            {
                if (serialPort.BytesToRead > 0)
                {
                    byte[] oneByteToRead = new byte[10];
                    serialPort.Read(oneByteToRead, 0, sizeof(byte) * 1);
                    int transmissionCode = BitConverter.ToInt32(oneByteToRead, 0);
                    if (transmissionCode == Transmission.START_CODE)
                    {
                        Debug.WriteLine("Session successful started");

                        break;
                    }
                    else
                    {
                        Debug.WriteLine("ERROR IN CODE");

                    }
                }
            }
            serialPort.Write(BitConverter.GetBytes(Transmission.START_CODE), 0, 1);
            Debug.WriteLine("Communication was started");
            while (true)
            {
                Utils.Wait_for_data(serialPort);
                TransmittedData recievedData = Receive.ReceiveData(serialPort);

                if (recievedData.MessageCode == Transmission.NEXT)
                {
                    Debug.WriteLine("Request for next block received");
                    break;
                }
            }
        }
        private static int state;
        private static int breakFlag = 0;
        private static void ChangeCode(int code) 
        {
            switch (code) 
            {
                case ClientCodes.CONNECT:
                    transmissionCode = Transmission.TransmissionForConnect[state];
                    //state = (state + 1) % Transmission.TransmissionForConnect.Length;
                    state++;
                    if (Transmission.TransmissionForConnect.Length == state) {
                        breakFlag = 1;
                        state = 0;
                    }
                    break;
                case ClientCodes.UPLOAD:
                    transmissionCode = Transmission.TransmissionForUpload[state];
                    //state = (state + 1) % Transmission.TransmissionForUpload.Length;
                    state++;
                    if (Transmission.TransmissionForUpload.Length == state)
                    {
                        breakFlag = 1;
                        state = 0;
                    }
                    break;
                case ClientCodes.DOWNLOAD_CODE:
                    transmissionCode = Transmission.TransmissionForDownloadCode[state];
                    //state = (state + 1) % Transmission.TransmissionForUpload.Length;
                    state++;
                    if (Transmission.TransmissionForDownloadCode.Length == state)
                    {
                        breakFlag = 1;
                        state = 0;
                    }
                    break;
                case ClientCodes.DOWNLOAD_DATA:
                    transmissionCode = Transmission.TransmissionForDownloadData[state];
                    //state = (state + 1) % Transmission.TransmissionForUpload.Length;
                    state++;
                    if (Transmission.TransmissionForDownloadData.Length == state)
                    {
                        breakFlag = 1;
                        state = 0;
                    }
                    break;
                case ClientCodes.DISCONNECT:
                    transmissionCode = Transmission.TransmissionForDisconnect[state];
                    state = (state + 1) % Transmission.TransmissionForDisconnect.Length;
                    if (Transmission.TransmissionForDisconnect.Length == state)
                    {
                        breakFlag = 1;
                        state = 0;
                    }
                    break;
            }
        }


        public static void TransmissionOfData(SerialPort serialPort, int baudRate, BootloaderFile bootloaderFile, int clientCode, Form1 parentForm) 
        {
            int currentBytesSended = 0;
            String decodedData;
            state = 0;

            while (true)
            {
                ChangeCode(clientCode);
                if (transmissionCode == Transmission.BAUDRATE)
                {
                    if (serialPort.BaudRate != baudRate)
                    {
                        Send.SendData(serialPort, Transmission.BAUDRATE, sizeof(int), BitConverter.GetBytes(baudRate));
                        serialPort.BaudRate = baudRate;
                    }
                    else if (breakFlag == 1) {
                        breakFlag = 0;
                        break;
                    }
                    else
                    {
                        continue;
                    }
                }
                if (transmissionCode == Transmission.FIRMWARE_INFO_FROM_BOOTLOADER)
                {
                    byte[] firmware = new byte[1];
                    Send.SendData(serialPort, Transmission.FIRMWARE_INFO_FROM_BOOTLOADER, 0, firmware);
                }
                if (transmissionCode == Transmission.FIRMWARE_INFO)
                {
                    Send.SendData(serialPort, Transmission.FIRMWARE_INFO, bootloaderFile.GetMetadataAsByteArray().Length, bootloaderFile.GetMetadataAsByteArray());
                    Debug.WriteLine(BitConverter.ToString(bootloaderFile.GetMetadataAsByteArray()));
                    Debug.WriteLine("Firmware Info is sended");
                }
                if (transmissionCode == Transmission.ADDRESSES_INFO)
                {
                    Send.SendData(serialPort, Transmission.ADDRESSES_INFO, bootloaderFile.FirstBlock.Length, bootloaderFile.FirstBlock);
                    Debug.WriteLine(BitConverter.ToString(bootloaderFile.FirstBlock));
                    Debug.WriteLine("Addresses Info is sended");
                }
                if (transmissionCode == Transmission.DOWNLOAD_CODE) 
                {
                    byte[] downloadCodeUselessArray= new byte[1];
                    Send.SendData(serialPort, Transmission.DOWNLOAD_CODE, 0, downloadCodeUselessArray);
                    Debug.WriteLine("Code Downloading statred");

                }
                if (transmissionCode == Transmission.DOWNLOAD_DATA)
                {
                    byte[] downloadDataUselessArray = new byte[1];
                    Send.SendData(serialPort, Transmission.DOWNLOAD_DATA, 0, downloadDataUselessArray);
                    Debug.WriteLine("Data Downloading statred");

                }
                if (transmissionCode == Transmission.PROGRAM)
                {
                    byte[] program = BitConverter.GetBytes(bootloaderFile.Data.Length);
                    Debug.WriteLine($"Data len is {bootloaderFile.Data.Length}");
                    Send.SendData(serialPort, Transmission.PROGRAM, program.Length, program);
                }
                //перенести
                if (transmissionCode == Transmission.RELEASE)
                {
                    byte[] release = new byte[1];
                    Send.SendData(serialPort, Transmission.RELEASE, 0, release);
                    Debug.WriteLine("Bye-Bye");
                    break;
                }
                if (breakFlag == 1)
                {
                    breakFlag = 0;
                    break;

                }

                while (true) 
                {
                    //Utils.Wait_for_data(serialPort);
                    TransmittedData recievedData = Receive.ReceiveData(serialPort);

                    if (recievedData.MessageCode == Transmission.NEXT)
                    {
                        Debug.WriteLine("Request for next block received");
                        break;
                    }
                    else if (recievedData.MessageCode == Transmission.END_OF_DOWNLOAD) 
                    {
                        //добавить флаг только для кода
                        Debug.WriteLine("Code/Data Downloaded");
                        //downloadedSmth = recievedMetaInfo.GetMetadataAsByteArray();
                        recievedMetaInfo.FirstBlock = downloadedCode.Take(16).ToArray();
                        recievedMetaInfo.Data = downloadedCode.Skip(16).ToArray();
                        //downloadedSmth = recievedMetaInfo.GetMetadataAsByteArray().Concat(downloadedCode).ToArray();
                        //вынести в форму
                        //var fileStream = new FileStream(filepath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write);
                        //using var binaryWriter = new BinaryWriter(fileStream, Encoding.ASCII);
                        //binaryWriter.Write(downloadedSmth);
                    }
                    else if (recievedData.MessageCode == Transmission.REQUEST)
                    {
                        Debug.WriteLine($"{recievedData.Data}");
                        uint f = BitConverter.ToUInt32(recievedData.Data, 0);
                        uint t = BitConverter.ToUInt32(recievedData.Data, 4);
                        Debug.WriteLine($"{f} {t} and len of sended data is {bootloaderFile.Data.Skip((int)f).Take((int)(t - f)).ToArray().Length}");
                        Send.SendData(serialPort, Transmission.PROGRAM, bootloaderFile.Data.Skip((int)f).Take((int)(t - f)).ToArray().Length, bootloaderFile.Data.Skip((int)f).Take((int)(t - f)).ToArray());
                        currentBytesSended += ((int)(t - f));
                        parentForm.ProgressChanged((int)(100 * currentBytesSended / bootloaderFile.Data.Length));
                        continue;
                    }
                    else if (recievedData.MessageCode == Transmission.FIRMWARE_INFO_FROM_BOOTLOADER)
                    {
                        recievedMetaInfo.GetMetadataFromByteArray(recievedData.Data);
                        Debug.WriteLine("MetaInfo was recieved");
                        continue;
                    }
                    else if (recievedData.MessageCode == Transmission.PROGRAM)
                    {
                        downloadedCode = downloadedCode.Concat(recievedData.Data).ToArray();
                    }
                    else if (recievedData.MessageCode == Transmission.DATA)
                    {
                        downloadedData = downloadedData.Concat(recievedData.Data).ToArray();
                    }
                    else
                    {
                        decodedData = Utils.Decode_data(recievedData);
                        Debug.WriteLine($"Data: {decodedData}");
                        continue;
                    }

                }

                /*
                switch (flag) {
                    //Que
                    case Transmission.BAUDRATE:
                        if (serialPort.BaudRate != baudRate)
                        {
                            Send.SendData(serialPort, Transmission.BAUDRATE, sizeof(int), BitConverter.GetBytes(baudRate));
                            serialPort.BaudRate = baudRate;
                            continue;
                        }
                        break;
                    case Transmission.FIRMWARE_INFO_FROM_BOOTLOADER:
                        byte[] firmware = new byte[1];
                        Send.SendData(serialPort, Transmission.FIRMWARE_INFO_FROM_BOOTLOADER, 0, firmware);
                        continue;

                    case Transmission.FIRMWARE_INFO:
                        Send.SendData(serialPort, Transmission.FIRMWARE_INFO, bootloaderFile.GetMetadataAsByteArray().Length, bootloaderFile.GetMetadataAsByteArray());
                        Debug.WriteLine("Firmware Info is sended");
                        continue;
                    case Transmission.ADDRESSES_INFO:
                        Send.SendData(serialPort, Transmission.FIRMWARE_INFO, bootloaderFile.FirstBlock.Length, bootloaderFile.FirstBlock);
                        Debug.WriteLine("Addresses Info is sended");
                        continue;
                    case Transmission.PROGRAM:
                        byte[] program = BitConverter.GetBytes(bootloaderFile.Data.Length);
                        Debug.WriteLine($"Data len is {bootloaderFile.Data.Length}");
                        Send.SendData(serialPort, Transmission.PROGRAM, program.Length, program);
                        continue;
                    case Transmission.RELEASE:
                        byte[] release = new byte[1];
                        Send.SendData(serialPort, Transmission.RELEASE, 0, release);
                        Debug.WriteLine("Bye-Bye");
                        break;
                }
                */

            }

        }
    }
}
