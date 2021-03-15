import serial
import sys

from security import *
from data_transmition import *

block_size = 16
serial_port = serial.Serial(port="COM3", baudrate=115200,
                            bytesize=8, timeout=None, stopbits=serial.STOPBITS_ONE)

# The transmission between us and STM-32 start with sending by STM32 0xAB that means it wants to receive data
if len(sys.argv) < 1:
    print("You must pass filename of the program you want to send")

code_path = sys.argv[1]
with open(code_path, 'rb') as code_file:
    code = bytearray(code_file.read())

code = bytes(code)
key = b"11111111111111111111111111111111"
print(f"key: {key}")
iv = b"1111111111111111"
print(f"code before encoding: {len(code)}")
print(code)
while len(code) % block_size != 0:
    code = code + b'\x00'
code = encrypt(code, key, iv)
print(f"code after encoding: {len(code)}")
print(code)
# waiting for communication
print("Waiting for the start code")
while 1:
    sleep(0.01)
    num = serial_port.in_waiting
    if num > 0:
        byte = serial_port.read(1)
        transmission_code = int.from_bytes(byte, 'little')
        if transmission_code == 0xAE:
            print("The 0xAE was received. Started sending data")
            break
        else:
            print(f"{transmission_code} doesn't match the standard 0xAE")
            exit(666)

print(f"This code has {len(code) // block_size} blocks")
send_data_header(serial_port, code, 3)
i = 0

while 1:
    wait_for_data(serial_port)
    l, t, d = receive_data(serial_port)  # len, type, data
    if t == 4:
        send_data(serial_port, code[i: i + block_size], 3)
        i += block_size
        if i > len(code):
            break
    else:
        decoded_data = decode_data(d, t)
        print(decoded_data.encode())
