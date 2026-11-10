"""Physical Variables19 scalar and the checked16-bit duration consumer."""
import struct


def view(raw):
    if len(raw)!=16 or int.from_bytes(raw[:4],'little')!=19:raise ValueError('Expected Variables19 record')
    kind,value,a,b=struct.unpack('<4I',raw)
    return dict(type=kind,value_bits=value,extra1=a,extra2=b,confirmed_alias='SIMULATION_TICKS_PER_SECOND')


def patch_bits(raw,value):
    view(raw)
    if type(value)is not int or not 0<=value<=0xFFFFFFFF:raise ValueError('Expected physical DWORD bits')
    return raw[:4]+struct.pack('<I',value)+raw[8:]


def duration_ticks(duration_bits,tps_bits):
    for value in [duration_bits,tps_bits]:
        if type(value)is not int or not 0<=value<=0xFFFFFFFF:raise ValueError('Expected physical DWORD bits')
    return (duration_bits*tps_bits)&65535
