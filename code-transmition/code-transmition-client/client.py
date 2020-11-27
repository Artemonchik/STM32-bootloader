import itertools
from time import sleep
import serial
from data_transmition import *
import sys
import threading


def run_receiving_messages_tread():
    '''
    Runs a thread that will read messages from serial port and then print them
    '''

    def receive_messages():
        while 1:
            sleep(0.1)
            if serial_port.in_waiting > 0:
                length, message_code, data = receive_data(serial_port)
                string_message = decode_data(data, message_code)
                print(f"\033[94m{string_message}\033[0m")

    thread = threading.Thread(target=receive_messages)
    thread.start()


serial_port = serial.Serial(port="COM6", baudrate=9600,
                            bytesize=8, timeout=None, stopbits=serial.STOPBITS_ONE)

# The transmission between us and STM-32 start with sending by STM32 0xAB that means it wants to receive data
if len(sys.argv) < 1:
    print("You must pass filename of the program you want to send")

code_path = sys.argv[1]
with open(code_path, 'rb') as code_file:
    code = bytearray(code_file.read())

while len(code) % 16 != 0:
    code = code + b'\x00'

code = bytes(code)
print(code)
# waiting for communication
print("Waiting for the start code")
while 1:
    sleep(0.1)
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

print(f"This code has {len(code) // 16} blocks")

# Sending data
run_receiving_messages_tread()
send_data(serial_port, code, 3)
