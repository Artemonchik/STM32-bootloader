# This module going to send data by following format SDF (size defined format):
# 0 - 3 byte - integer which indicates len of data we want to transmit
# 4 - 7 byte - code of transmited data
# 8 - ... byte - data we want to send / receive

# CODE TABLE #
# 1 - string message
# 2 - error message
# 3 - binary code
from time import sleep


def send_raw_data_body(serial_port, data: bytes, transmission_code=1, timeout=10000):
    length = len(data)

    curr_timeout = serial_port.write_timeout
    serial_port.write_timeout = timeout
    serial_port.write(data)

    serial_port.write_timeout = curr_timeout


def send_raw_data_header(serial_port, data: bytes, transmission_code=1, timeout=10000):
    length = len(data)

    curr_timeout = serial_port.write_timeout
    serial_port.write_timeout = timeout

    serial_port.write(transmission_code.to_bytes(4, 'little'))
    serial_port.write(length.to_bytes(4, 'little'))

    serial_port.write_timeout = curr_timeout


# returns DataCode, DataLen, DataBody
def send_raw_data(serial_port, data: bytes, transmission_code=1, timeout=10000):
    send_raw_data_header(serial_port, data, transmission_code, timeout)
    send_raw_data_body(serial_port, data, transmission_code, timeout)


def receive_raw_data(serial_port, timeout=1000):
    curr_timeout = serial_port.timeout
    serial_port.timeout = timeout

    data_type_b = serial_port.read(4)
    if len(data_type_b) < 4:
        return data_type_b, 0, []

    bytes_number_b = serial_port.read(4)
    if len(bytes_number_b) < 4:
        return 0, 0, []

    data_type = int.from_bytes(data_type_b, 'little')
    data_length = int.from_bytes(bytes_number_b, 'little')



    data = serial_port.read(data_length)

    serial_port.timeout = curr_timeout
    return  data_type, data_length, data


def receive_data(serial_port, timeout=1000):
    t, l, d = receive_raw_data(serial_port, timeout)
    return t, l, decode_data(d, t)


def decode_data(data, transmission_code=1):
    if transmission_code == Transmition.REQUEST_BLOCK:
        block_num = int.from_bytes(data, 'little')
        return block_num
    elif transmission_code in [1, 2]:
        return data.decode("koi8-r")


def wait_for_data(serial_port):
    while 1:
        sleep(0.001)
        if serial_port.in_waiting > 0:
            return


class Transmition:
    START_CODE = 0xAE
    STRING_MESSAGE = 1
    ERROR_MESSAGE = 2
    BINARY_CODE = 3
    REQUEST_BLOCK = 4
    