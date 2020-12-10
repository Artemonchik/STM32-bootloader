import re


def cond(string: str):
    if string.count('0') != string.count('1'):
        return False
    for i in range(len(string)):
        if string[:i + 1].count("0") - string[:i + 1].count("1") > 2:
            return False
        if string[:i + 1].count("1") - string[:i + 1].count("0") > 1:
            return False
    return True


for i in range(10000000):
    result = re.fullmatch(r'(01|10|0011)*', f"{i:b}")
    if cond(f"{i:b}"):
        if result == None:
            print(" :", f"{i:b}")
    else:
        if result != None:
            print(" :", f"{i:b}")
