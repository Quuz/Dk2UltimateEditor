"""Physicalscalar112/113/114 view; names do not establish bonus effects."""
import struct
def view(raw):
    if not isinstance(raw,(bytes,bytearray)) or len(raw)!=16:raise ValueError('Expected16-byte Variables record')
    kind,value,a,b=struct.unpack('<4I',raw)
    if kind not in [112,113,114]:raise ValueError('Expectedscalar112/113/114')
    return dict(type=kind,value_bits=value,loaded_word=value&65535,opaque_dwords=[a,b])
def patch_value_bits(raw,value):
    view(raw)
    if type(value) is not int or not 0<=value<=0xffffffff:raise ValueError('Expected physical DWORD value')
    out=bytearray(raw);struct.pack_into('<I',out,4,value);return bytes(out)
