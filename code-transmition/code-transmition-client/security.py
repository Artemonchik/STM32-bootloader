from Crypto.Cipher import AES
from Crypto.Util.Padding import pad


def encrypt(code, key, iv):
    cipher = AES.new(key, AES.MODE_CBC, iv=iv[:16])
    ct_bytes = cipher.encrypt(code)
    return ct_bytes
def decrypt(code, key, iv):
    cipher = AES.new(key, AES.MODE_CBC, iv=iv[:16])
    ct_bytes = cipher.decrypt(code)
    return ct_bytes
