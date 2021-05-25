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


def send_data_header(serial_port, code, length, num, timeout=2.3):
    header = make_raw_data_header(code, length, num)
    print(f"Data header sended: {header} ")
    serial_port.write(header)


def send_raw_full_packet(header, serial_port, data):
    print(f"Full body sended: {header + data + zlib.crc32(data).to_bytes(length=4, byteorder='little')}")
    serial_port.write(header + data + zlib.crc32(data).to_bytes(length=4, byteorder="little"))


def send_raw_data(serial_port, transmition_code, length, body, timeout=2.3):
    global packet_counter
    serial_port.timeout = timeout
    while 1:
        sleep(0.01)
        send_data_header(serial_port, transmition_code, length, packet_counter, timeout)
        try:
            code, l, num, crc = header = receive_data_header(serial_port)
        except:
            serial_port.reset_input_buffer()
            print("Error occured, retring in send data header")
            continue
        if not check_header_crc(header):
            serial_port.reset_input_buffer()
            print("header crc not valid")
            continue
        if packet_counter == num + 1:
            send_ack(serial_port, packet_counter)
            serial_port.reset_input_buffer()
            print("num are invalid")
            continue
        break
    packet_counter += 1
    if length == 0:
        return True
    while 1:
        send_raw_full_packet(make_raw_data_header(transmition_code, length, packet_counter), serial_port, body)
        # wait_for_data(serial_port)
        try:
            code, l, num, crc = header = receive_data_header(serial_port)
        except:
            serial_port.reset_input_buffer()
            print("Error occured, retring in send full data")
            continue
        if not check_header_crc(header):
            print("when receiving full packege header crc is not valid")
            serial_port.reset_input_buffer()
            continue
        if packet_counter == num + 1:
            send_ack(serial_port, packet_counter)
            serial_port.reset_input_buffer()
            print(num + 1)
            print("num are invalid")
            continue
        break
    packet_counter += 1
    return True


def receive_raw_data_header(serial_port, timeout=1000):
    raw_data = serial_port.read(4 * 4)
    print(f"Received header {raw_data}")
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
    print("ack sended")
    raw_data = make_raw_data_header(Transmition.ACK, 0, num)
    serial_port.write(raw_data)


def check_header_crc(header):
    code, length, num, received_crc = header
    a, b, c, computed_crc = parse_header(make_raw_data_header(code, length, num))
    return received_crc == computed_crc


def check_body_crc(body, crc):
    return zlib.crc32(body) == crc


HEADER = 0
FULL_PACKET = 1
ERROR = 3


def receive_raw_full_packet(serial_port, length, timeout=1.2):
    serial_port.timeout = timeout
    raw_data = serial_port.read(4 + 4 + 4 + 4 + length + 4)
    if len(raw_data) == struct.calcsize(f"<IIII{length}sI"):
        print(f"Received full body as expected")
        result = struct.unpack(f"<IIII{length}sI", raw_data)
        return FULL_PACKET, result
    elif len(raw_data) == struct.calcsize(f"<IIII"):
        print(f"received header instead of body")
        result = struct.unpack(f"<IIII", raw_data)
        return HEADER, result
    else:
        print("unknown format received")
        return ERROR, raw_data


def receive_raw_data(serial_port):
    global packet_counter
    while True:
        try:
            code, length, num, crc = header = receive_data_header(serial_port)
        except:
            serial_port.reset_input_buffer()
            print("wrong format header")
            continue
        if not check_header_crc(header):
            serial_port.reset_input_buffer()
            print("wrong crc header")
            continue
        if packet_counter == num + 1:
            serial_port.reset_input_buffer()
            print("num are invalid")
            send_ack(serial_port, packet_counter)
            continue
        send_ack(serial_port, packet_counter + 1)
        packet_counter += 1
        if length == 0:
            return code, length, bytes([])
        break
    while True:
        packet_type, data = receive_raw_full_packet(serial_port, length)
        if packet_type == FULL_PACKET:
            code, length, num, crc, body, body_crc = data
        elif packet_type == HEADER:
            serial_port.reset_input_buffer()
            code, length, num, crc = data
        else:
            print(data)
            serial_port.reset_input_buffer()
            continue
        header = code, length, num, crc
        if not check_header_crc(header):
            serial_port.reset_input_buffer()
            continue
        if packet_counter == num + 1:
            serial_port.reset_input_buffer()
            print("num are invalid")
            send_ack(serial_port, packet_counter)
            continue
        if packet_type == FULL_PACKET and not check_body_crc(body, body_crc):
            serial_port.reset_input_buffer()
            print("wrong crc body")
            continue
        send_ack(serial_port, packet_counter + 1)
        packet_counter += 1
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
