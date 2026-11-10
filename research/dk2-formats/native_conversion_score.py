"""Bit-preserving Variables233 view and exact observed DX score arithmetic."""
import struct
DX_SHA256='d7a8b854e4e4f02709eba271bead6fe2307dd6e332b5a96e70857e5b0e19fb8a'
TYPE=233


def view(raw):
    if len(raw)!=16 or int.from_bytes(raw[:4],'little')!=TYPE:raise ValueError('Expected a16-byte Variables233 record')
    kind,bits,extra1,extra2=struct.unpack('<4I',raw)
    return dict(type=kind,value_bits=bits,value_i32=(bits^0x80000000)-0x80000000,extra1=extra1,extra2=extra2,
                meaning='Conversion score delta; gameMode4 and receivingPlayerType>=3 in inspected DX consumer')


def patch_bits(raw,bits):
    view(raw)
    if type(bits) is not int or not 0<=bits<=0xFFFFFFFF:raise ValueError('Expected unsigned32 physical bits')
    return raw[:4]+struct.pack('<I',bits)+raw[8:]


def project(score_bits,delta_bits,mode,player_type):
    if not all(type(x) is int for x in [score_bits,delta_bits,mode,player_type]):raise ValueError('Integer inputs required')
    if not 0<=score_bits<=0xFFFFFFFF or not 0<=delta_bits<=0xFFFFFFFF or not 0<=player_type<=255:raise ValueError('Out of physical range')
    if mode!=4 or player_type<3:return score_bits
    summed=(score_bits+delta_bits)&0xFFFFFFFF
    return 0 if summed&0x80000000 else summed
