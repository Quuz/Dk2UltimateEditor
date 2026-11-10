"""Bounded original DX object-sacrifice predicate and lossless Gold-bit edit.

This models an already invoked callback, not a physical drop/scheduler. Keep
other object-type/unknown flags; do not imply a complete Gold type conversion.
"""
import struct

def accepts(*,room_present,room_category,cell_byte_C,object_flags):
    return bool(room_present and room_category==13 and cell_byte_C==255 and not object_flags&0x40)

def patch_gold_bit(raw,enabled):
    if not isinstance(raw,bytes) or len(raw)!=894:raise ValueError('Expected894-byte Object record')
    if type(enabled) is not bool:raise ValueError('Expected Boolean')
    flags=struct.unpack_from('<I',raw,0x344)[0]
    return raw[:0x344]+struct.pack('<I',(flags&~0x40)|(0x40 if enabled else 0))+raw[0x348:]

def visual_helper_gate(object_z,surface_z):
    return ((object_z-surface_z)&0xFFFFFFFF)<0x1000
