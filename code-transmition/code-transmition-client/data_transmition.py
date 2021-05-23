# This module going to send data by following format SDF (size defined format):
# 0 - 3 byte - integer which indicates len of data we want to transmit
# 4 - 7 byte - code of transmited data
# 8 - ... byte - data we want to send / receive

# CODE TABLE #
# 1 - string message
# 2 - error message
# 3 - binary code
import struct
import zlib
from time import sleep

packet_counter = 0


def send_data_header(serial_port, code, length, num, timeout=1000):
    header = make_raw_data_header(code, length, num)
    serial_port.write(header)


def send_raw_data_body(serial_port, data):
    serial_port.write(data + zlib.crc32(data).to_bytes(length=4, byteorder="little"))


def send_raw_data(serial_port, transmition_code, length, body, timeout=2300):
    serial_port.timeout = timeout
    while 1:
        sleep(0.01)
        send_data_header(serial_port, transmition_code, length, packet_counter, timeout)
        try:
            a, l, b, c = receive_data_header(serial_port)
        except:
            print("Error occured, retring")
            continue
        if a != Transmition.ACK:
            continue
        break

    while 1:
        if length == 0:
            return True
        send_raw_data_body(serial_port, body)
        # wait_for_data(serial_port)
        try:
            a, l, b, c = receive_data_header(serial_port)
        except:
            print("Error occured, retring")
            continue
        if a != Transmition.ACK:
            continue
        return True


def receive_raw_data_header(serial_port, timeout=1000):
    raw_data = serial_port.read(4 * 4)
    return raw_data


def receive_data_header(serial_port, timeout=1000):
    return parse_header(receive_raw_data_header(serial_port, timeout))


def make_raw_data_header(message_code, length, num):
    raw_data = struct.pack("<III", message_code, length, num)
    crc = zlib.crc32(raw_data)
    raw_header = struct.pack("<IIII", message_code, length, num, crc)

    return raw_header


def parse_header(header):
    return struct.unpack("<IIII", header)


def send_ack(serial_port, num):
    raw_data = make_raw_data_header(Transmition.ACK, 0, num)
    serial_port.write(raw_data)


def check_header_crc(header):
    code, length, num, received_crc = header
    a, b, c, computed_crc = parse_header(make_raw_data_header(code, length, num))
    return received_crc == computed_crc


def check_body_crc(body, crc):
    return zlib.crc32(body) == crc


def receive_raw_data_body(serial_port, length, timeout=1000):
    serial_port.timeout = timeout
    raw_data = serial_port.read(length + 4)
    result = struct.unpack(f"<{length}sI", raw_data)
    return result


def receive_raw_data(serial_port):
    while True:
        try:
            code, length, num, crc = header = receive_data_header(serial_port)
        except :
            print("wrong format header")
            continue
        if not check_header_crc(header):
            print("wrong crc header")
            continue
        send_ack(serial_port, num + 1)
        if length == 0:
            return code, length, bytes([])
        break

    while True:
        try:
            body, body_crc = receive_raw_data_body(serial_port, length)
        except:
            print("wrong format body")
            continue
        if not check_body_crc(body, body_crc):
            print("wrong crc body")
            continue
        send_ack(serial_port, num + 2)
        return code, length, body


def decode_data(params):
    transmition_code, length, body = params
    if transmition_code == Transmition.STRING_MESSAGE:
        body = body.decode("koi8-r")
    return transmition_code, length, body


def wait_for_data(serial_port):
    while 1:
        sleep(0.001)
        if serial_port.in_waiting > 0:
            return


class Transmition:
    START_CODE = 0xAE
    STRING_MESSAGE = 1
    ERROR_MESSAGE = 2
    PROGRAM = 3
    REQUEST = 4
    ACK = 5
    NEXT = 6
    BAUDRATE = 7
    TIMEOUT = 8
    RELEASE = 9
    SECRET_KEY = 10