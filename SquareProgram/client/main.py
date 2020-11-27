import itertools
from time import sleep

from serial import *

serial_port = Serial(port="COM7", baudrate=9600,
                     bytesize=8, timeout=None, stopbits=STOPBITS_ONE)
import sys

# This program going to send data by following format SDF (size defined format):
# 0 - 3 byte - integer which indicates len of data we want to transmit
# 4 - 7 byte - code of transmited data
# 8 - ... byte - data we want to send / receive


def send_data(serial_port: Serial, data: bytes, timeout: int = None):
    """
    Send {data} to {serial_port} with max wait of {timeout} seconds
    :param serial_port: port to send
    :param data: data
    :param timeout: max time to send data
    :return: no
    :except: SerialTimeoutException – in a case a {timeout} time is exceeded
    """
    curr_timeout = serial_port.write_timeout
    serial_port.write_timeout = timeout
    serial_port.write(data)
    serial_port.write_timeout = curr_timeout


def receive_data(serial_port: Serial, timeout: int = None):
    """
    Receive SDF data from serial
    :param serial_port:
    :param timeout:
    :return: number of bytes received and received data bytes
    """
    curr_timeout = serial_port.timeout
    serial_port.timeout = timeout

    bytes_number_b = serial_port.read(4)
    if(len(bytes_number_b) < 4):
        return 0, 0, []
    bytes_number = int.from_bytes(bytes_number_b, 'little')

    data_type_b = serial_port.read(4)
    data_type = int.from_bytes(data_type_b, 'little')

    data = serial_port.read(bytes_number)

    serial_port.timeout = curr_timeout
    return bytes_number, data_type, data


def encode_data(data: list):
    """
    Convert data in SDF to send it by serial port. It converts {data} to byte array and prepends size of the resulting byte array
    :param data: data we want to send
    :return: byte array of format [size + data]
    """
    data_list = map(lambda num: num.to_bytes(4, 'little'), data)
    data_list = list(itertools.chain.from_iterable(data_list))
    data_len = len(data_list)
    result_list = list(data_len.to_bytes(4, 'little')) + data_list
    return bytes(result_list)


def decode_data(data_type: int, data: bytes):
    """
    Decode bytearray data. Now just convert bytearray to int list
    :param data:
    :return: list of integers
    """
    if data_type == 1:
        return data.decode('koi8-r')
    else:
        result = [data[i:i + 4] for i in range(0, len(data), 4)]
    return list(map(lambda elem: int.from_bytes(elem, 'little'), result))


# data_to_send = encode_data([1, 2, 3, 4, 5, 6, 7, 8, 9, 10])
# send_data(serial_port, data_to_send, 5000)
# print(f'List we want to send:\n{[1, 2, 3, 4, 5, 6, 7, 8, 9, 10]}')
# print(f'The binary data going to be send:\n {data_to_send}\n\n\n')
serial_port.flushInput()


while 1:

    num = serial_port.in_waiting
    sleep(0.1)
    if num > 0:
        size, data_type, data = receive_data(serial_port, 3008)  # read the size of data we need to read
        # print(f"Was received {size} bytes")
        # print("The data was received:")
        print(decode_data(data_type, data))


# print(decode_data(encode_data([1, 2, 3, 4, 5, 2324])))
