"""Exact inspected Terrain flag selection for the CMap navigation-byte writer."""
import struct

def terrain_navigation_byte(flags):
    if type(flags) is not int or not 0<=flags<=0xffffffff:raise ValueError('Expected DWORD flags')
    return 8 if flags&1 else 1 if flags&0x80 else 3 if flags&0x100 else 0

def patch_terrain_navigation_flags(raw,mask,enabled):
    if not isinstance(raw,(bytes,bytearray)) or len(raw) not in [548,552]:raise ValueError('Expected548/552-byte Terrain record')
    if type(mask) is not int or not mask or mask&~0x181 or type(enabled) is not bool:raise ValueError('Expected selectedmask181h subset andboolean')
    out=bytearray(raw);flags=struct.unpack_from('<I',raw,0x190)[0]
    struct.pack_into('<I',out,0x190,(flags|mask) if enabled else (flags&~mask));return bytes(out)
