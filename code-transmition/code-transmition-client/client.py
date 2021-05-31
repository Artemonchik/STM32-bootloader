import random
import zlib

import serial
import sys

from security import *
from data_transmition import *

block_size = 16 * 16 * 3 * 4
serial_port = serial.Serial(port="COM7", baudrate=115200,
                            bytesize=8, timeout=None, stopbits=serial.STOPBITS_ONE)

# The transmission between us and STM-32 start with sending by STM32 0xAB that means it wants to receive data
if len(sys.argv) < 1:
    print("You must pass filename of the program you want to send")

# read code from the file
code_path = sys.argv[1]
with open(code_path, 'rb') as code_file:
    code = bytearray(code_file.read())

# code = bytes(code)
key = b"11145678901234567890123456789012"
print(f"key: {key}")
iv = b"susususususususu"
serial_port.reset_input_buffer()
# add zero padding
while len(code) % 16 != 0:
    code = code + b'\x00'
header = struct.pack("<II", 0x08000000, 0x08040000)
header = struct.pack(f"<I{len(header)}sI", zlib.crc32(header), header, zlib.crc32(code))
code = header + code
code_start_address = len(header)

# encrypt code

code = encrypt(code, key, iv)
with open("./encrypted.bin", "wb") as file:
    file.write(code)
code = bytes(code)
print("Waiting for communication")
wait_for_data(serial_port)
if int.from_bytes(serial_port.read(1), signed=False, byteorder="little") == 0xAE:
    print("session successful started")
else:
    print("ERROR IN CODE")
serial_port.write(0xAE.to_bytes(byteorder="little", length=1))
print("Communication was started")

while 1:
    wait_for_data(serial_port)
    message_code, length, data = receive_raw_data(serial_port)
    mes = "Allow haka"
    if message_code == Transmition.NEXT:
        print("Request for next block received")
    elif message_code == Transmition.REQUEST:
        f, t = struct.unpack("<II", data)
        f = f + code_start_address
        t = t + code_start_address
        print(f"{f} {t} and len of sended data is {len(code[f:t])}")
        send_raw_data(serial_port, Transmition.PROGRAM, len(code[f:t]), code[f:t], timeout=1.5)
        continue
    else:
        print(f"Data received: {data}")
        continue
    val = int(input("Enter what you want to do: "))
    if val == Transmition.BAUDRATE:
        baudrate = int(input("Введите новое значение baudrate: "))
        send_raw_data(serial_port, Transmition.BAUDRATE, 4, baudrate.to_bytes(length=4, byteorder="little"), timeout=1)
        serial_port.baudrate = baudrate
        sleep(0.1)
    if val == 1:
        send_raw_data(serial_port, 1, len(mes), mes.encode(), 1.2)
        print(decode_data(receive_raw_data(serial_port)))
    if val == Transmition.TIMEOUT:
        timeout = int(input("Введите новое значение timeout: "))
        send_raw_data(serial_port, Transmition.TIMEOUT, 4, timeout.to_bytes(length=4, byteorder="little"))
    if val == Transmition.PROGRAM:
        data = struct.pack("<I", len(code) - len(header))
        print(f"Data len is {len(code) - len(header)}")
        send_raw_data(serial_port, Transmition.PROGRAM, len(data), data, 1.4)
    if val == Transmition.RELEASE:
        send_raw_data(serial_port, Transmition.RELEASE, 0, bytes(), 0.4)
        print("Bye bye")
        break
    if val == Transmition.IV:
        b = lambda x: x.encode(encoding="ascii")
        data = struct.pack("<6s32s8sQ4s16sL", b("HEADER"), b("Mew mew mew we are the cats"), b("05.04.03"), 23333333,
                           b("DATA"), iv, len(code) - len(header))
        send_raw_data(serial_port, Transmition.IV, len(data), data)
    if val == Transmition.ADDRESSES_INFO:
        send_raw_data(serial_port, Transmition.ADDRESSES_INFO, len(header), code[:len(header)])
    if val == Transmition.FIRMWARE_INFO_UPLOAD:
        send_raw_data(serial_port, Transmition.FIRMWARE_INFO_UPLOAD, 0, bytes(), 0.4)
        print(receive_raw_data(serial_port))
    if val == Transmition.UPLOAD_CODE:
        send_raw_data(serial_port, val, 0, bytes(), 3.4)
        message_code = 122222
        received_code = bytes()
        while message_code != Transmition.END_OF_UPLOAD:
            message_code, length, data = receive_raw_data(serial_port)
            received_code += data
        received_code = decrypt(received_code, key, iv)  # u receive IV by FIRMWARE_INFO_UPLOAD code
        crc1, f, t, crc2 = struct.unpack("<IIII", received_code[:16])
        received_code = received_code[16:]
        print(f"{crc1} {f:x} {t:x} {crc2}")
        print(f"{len(received_code)}")
        print(f"{len(code)}")
        print(received_code)
        print(decrypt(code, key, iv)[16:])

        if received_code == decrypt(code, key, iv)[16:]:
            print("code is the same")
        else:
            print("code is wrong :(")
    if val == Transmition.UPLOAD_DATA:
        send_raw_data(serial_port, val, 0, bytes(), 3.4)
        message_code = 122222
        received_data = bytes()
        while message_code != Transmition.END_OF_UPLOAD:
            message_code, length, data = receive_raw_data(serial_port)
            received_data += data
        received_data = decrypt(received_data, key, iv)  # u receive IV by FIRMWARE_INFO_UPLOAD code
        print(received_data)
        with open("data.bin", "wb") as file:
            file.write(received_data)
