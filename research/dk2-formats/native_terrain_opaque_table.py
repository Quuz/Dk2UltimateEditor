"""Lossless access to Terrain's unknown16WORD array; no semantic naming.

No sorting, checksum-field invention, index relabeling or zero normalization.
The array affects the selected engine intermediate-byte sum. That sum is a
runtime signature, not a separately stored checksum at this raw offset.
"""
import struct

OFFSET,WORDS,RECORD_BYTES=0x1AE,16,552

def view(raw):
    if not isinstance(raw,(bytes,bytearray)) or len(raw)!=RECORD_BYTES:
        raise ValueError('Expected552-byte Terrain record')
    return list(struct.unpack_from('<16H',raw,OFFSET))

def patch(raw,values):
    view(raw)
    values=list(values)
    if len(values)!=WORDS or any(type(v) is not int or not 0<=v<=65535 for v in values):
        raise ValueError('Expected exactly16opaque unsigned16-bit values')
    out=bytearray(raw);struct.pack_into('<16H',out,OFFSET,*values)
    return bytes(out)
