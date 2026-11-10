"""Lossless physical Shot damage fields; categories and algorithms are separate."""
FIELDS={'data1':(0x98,4),'data2':(0x9C,4),'damage_type':(0xC5,1),'attack_category':(0xC8,1)}


def check(raw):
    if type(raw) is not bytes or len(raw)!=239:raise ValueError('Expected exact239-byte Shot record')


def view(raw):
    check(raw)
    return {name:int.from_bytes(raw[o:o+n],'little') for name,(o,n) in FIELDS.items()}


def patch(raw,**changes):
    check(raw);b=bytearray(raw)
    for name,value in changes.items():
        if name not in FIELDS:raise ValueError('Unknown physical field')
        o,n=FIELDS[name]
        if type(value) is not int or not 0<=value<1<<(8*n):raise ValueError('Value outside physical bit width')
        b[o:o+n]=value.to_bytes(n,'little')
    return bytes(b)
