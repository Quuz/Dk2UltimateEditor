"""Lossless physical four-slot resistance edits; no unproved damage formula."""
RAW_OFFSET=0xE0C


def check(raw):
    if type(raw) is not bytes or len(raw) not in [5449,5537]:
        raise ValueError('Expected exact5449/5537-byte Creature record')


def view(raw):
    check(raw)
    return [dict(slot=i,attack_type=raw[RAW_OFFSET+2*i],value_bits=raw[RAW_OFFSET+2*i+1]) for i in range(4)]


def patch(raw,slot,*,attack_type=None,value=None):
    check(raw)
    if type(slot) is not int or not 0<=slot<4:raise ValueError('Expected slot0..3')
    for v in [attack_type,value]:
        if v is not None and (type(v) is not int or not 0<=v<=255):raise ValueError('Expected u8 bit pattern')
    b=bytearray(raw)
    if attack_type is not None:b[RAW_OFFSET+2*slot]=attack_type
    if value is not None:b[RAW_OFFSET+2*slot+1]=value
    return bytes(b)
