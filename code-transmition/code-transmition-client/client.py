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

code = bytes(code)
key = b"11111111111111111111111111111111"
print(f"key: {key}")
iv = b"1111111111111111"

# add zero padding
while len(code) % block_size != 0:
    code = code + b'\x00'
# encrypt code
code = encrypt(code, key, iv)

print("Waiting for communication")
while 1:
    wait_for_data(serial_port)
    t, l, d = receive_data(serial_port)
    print(f"\033[92mReceived data {t} {l} {d}\033[0m")# len, type, data
    if t == Transmition.START_CODE:
        print("Communication was started")
        send_raw_data_header(serial_port, code, Transmition.BINARY_CODE)
    elif t == Transmition.REQUEST_BLOCK:
        block_num = d
        send_raw_data_body(serial_port, code[block_num: block_num + block_size], Transmition.BINARY_CODE)
    elif t == Transmition.STRING_MESSAGE:
        message = d
        print(message)
    elif t == Transmition.ERROR_MESSAGE:
        message = d
        print(message)
    else:
        print("Unknown type of message")