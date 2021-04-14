from matplotlib import pyplot
from security import encrypt
import sys

code_path = sys.argv[1]
with open(code_path, 'rb') as code_file:
    code = bytearray(code_file.read())

key = b"11111ghtr11111111111111111111111"
iv = b"11111acedA111111"

pyplot.hist(list(map(lambda x: x, code)), bins=256)
pyplot.show()
code = encrypt(code, key, iv)
pyplot.hist(list(map(lambda x: x, code)), bins=256)
pyplot.show()