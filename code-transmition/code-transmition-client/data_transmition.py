# This module going to send data by following format SDF (size defined format):
# 0 - 3 byte - integer which indicates len of data we want to transmit
# 4 - 7 byte - code of transmited data
# 8 - ... byte - data we want to send / receive

# CODE TABLE #
# 1 - string message
# 2 - error message
# 3 - binary code
from time import sleep


def send_data(serial_port, data: bytes, transmission_code=1, timeout=10000):
    """
    Send len of data, data code and data to serial_port with max wait of timeout milliseconds
    :param serial_port: port to send
    :param data: data
    :param timeout: max time to send data
    :param: code from code Table
    :return: no
    :except: SerialTimeoutException – in a case a {timeout} time is exceeded
    """

    length = len(data)

    curr_timeout = serial_port.write_timeout
    serial_port.write_timeout = timeout

    # serial_port.write(length.to_bytes(4, 'little'))
    # serial_port.write(transmission_code.to_bytes(4, 'little'))
    serial_port.write(data)

    serial_port.write_timeout = curr_timeout


def send_data_header(serial_port, data: bytes, transmission_code=1, timeout=10000):
    length = len(data)

    curr_timeout = serial_port.write_timeout
    serial_port.write_timeout = timeout

    serial_port.write(length.to_bytes(4, 'little'))
    serial_port.write(transmission_code.to_bytes(4, 'little'))

    serial_port.write_timeout = curr_timeout


def receive_data(serial_port, timeout=1000):
    """
    Receive SDF data from serial
    :param serial_port:
    :param timeout:
    :return: number of bytes received and received data bytes
    """
    curr_timeout = serial_port.timeout
    serial_port.timeout = timeout

    bytes_number_b = serial_port.read(4)
    if len(bytes_number_b) < 4:
        return 0, 0, []
    data_length = int.from_bytes(bytes_number_b, 'little')

    data_type_b = serial_port.read(4)
    data_type = int.from_bytes(data_type_b, 'little')

    data = serial_port.read(data_length)

    serial_port.timeout = curr_timeout
    return data_length, data_type, data


def decode_data(data, transmission_code=1):
    if transmission_code in [1, 2]:
        return data.decode("koi8-r")


def wait_for_data(serial_port):
    while 1:
        sleep(0.001)
        if serial_port.in_waiting > 0:
            return
