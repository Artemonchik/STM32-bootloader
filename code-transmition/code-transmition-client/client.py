import random
import zlib

import serial
import sys

from security import *
from data_transmition import *

block_size = 16 * 16
serial_port = serial.Serial(port="COM3", baudrate=115200,
                            bytesize=8, timeout=None, stopbits=serial.STOPBITS_ONE)

# The transmission between us and STM-32 start with sending by STM32 0xAB that means it wants to receive data
if len(sys.argv) < 1:
    print("You must pass filename of the program you want to send")

# read code from the file
code_path = sys.argv[1]
with open(code_path, 'rb') as code_file:
    code = bytearray(code_file.read())

# code = bytes(code)
key = b"12345678901234567890123456789012"
print(f"key: {key}")
iv = key

# add zero padding
while len(code) % block_size != 0:
    code = code + b'\x00'
# encrypt code

code = encrypt(code, key, iv)
with open("./encrypted.bin", "wb") as file:
    file.write(code)
code = bytearray(code)
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
        print(data)
        f, t = struct.unpack("<II", data)
        print(f"{f} {t} and len of sended data is {len(code[f:t])}")
        send_raw_data(serial_port, Transmition.PROGRAM, len(code[f:t]), code[f:t], timeout=1)
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
        send_raw_data(serial_port, 1, len(mes), mes.encode(), 10)
        print(decode_data(receive_raw_data(serial_port)))
    if val == Transmition.TIMEOUT:
        timeout = int(input("Введите новое значение timeout: "))
        send_raw_data(serial_port, Transmition.TIMEOUT, 4, timeout.to_bytes(length=4, byteorder="little"))
    if val == Transmition.PROGRAM:
        data = struct.pack("<I", len(code))
        print(f"Data len is {len(code)}")
        send_raw_data(serial_port, Transmition.PROGRAM, len(data), data, 10)
    if val == Transmition.RELEASE:
        send_raw_data(serial_port, Transmition.RELEASE, 0, bytes(), 0.4)
        print("Bye bye")
        break
    if val == Transmition.SECRET_KEY:
        send_raw_data(serial_port, Transmition.SECRET_KEY, 0, bytes(1))