"""KeeperSpells physical storage and bounded native-loader projection.

C8 is intentionally unnamed. The unsigned editor double is a storage view,
not evidence of a gameplay unit. Preserve complete original records on save.
"""
import struct

WIDTHS={0xC8:4,0xCC:4,0xD0:4,0xD4:4,0xD8:2,0xDA:1,0xDB:1,0xDC:4,
        0xE0:2,**{o:2 for o in range(0xE2,0xEE,2)},0xEE:1,0xEF:1,0xF0:1,
        0x111:2,0x113:1,0x114:4,0x118:4,0x11C:4,0x194:1,0x195:1}
FLAGS=[(2,0x204),(4,0x208),(8,0x20C),(16,0x210)]

def check(raw):
    if not isinstance(raw,(bytes,bytearray)) or len(raw)!=406:raise ValueError('Expected406-byte KeeperSpells record')

def string32(raw):
    return raw.split(b'\0',1)[0].ljust(32,b'\0')

def intermediate(raw,seed=0):
    check(raw)
    if type(seed) is not int or not 0<=seed<=255:raise ValueError('Expected byte seed')
    out=bytearray([seed])*526
    for o in range(0,120,4):struct.pack_into('<I',out,o,1)
    out[0x78:0x98]=string32(raw[:32]);out[0x97]=0
    for dst,src,n in [(0x98,0x20,84),(0xEC,0x74,84),(0x140,0xC8,4),(0x144,0xCC,4),
                      (0x148,0xD0,4),(0x14C,0xD4,4),(0x150,0xD8,2),(0x152,0xDA,2),
                      (0x15A,0xE2,2),(0x15C,0xE4,10),(0x166,0xEE,3),(0x189,0x111,2),
                      (0x18B,0x113,1),(0x18C,0x114,12),(0x198,0x120,84),(0x20C,0x194,2)]:out[dst:dst+n]=raw[src:src+n]
    struct.pack_into('<I',out,0x154,struct.unpack_from('<I',raw,0xDC)[0]|seed*0x01010101)
    for dst,src in [(0x169,0xF1),(0x1EC,0x174)]:out[dst:dst+32]=string32(raw[src:src+32]);out[dst+31]=0
    return bytes(out)

def runtime_projection(mid,previous=bytes(122),tps=20,checksum_gate=0,sound_handle=0x11223344):
    """Full scalar122-byte output with null resource provider and sound stub."""
    if len(mid)!=526 or len(previous)!=122:raise ValueError('Expected526-byte intermediate and122-byte definition')
    if type(tps) is not int or not 0<=tps<=0xFFFFFFFF:raise ValueError('Expected DWORD TPS')
    out=bytearray(previous)
    if checksum_gate==0:struct.pack_into('<I',out,0x36,sum(mid))
    for p,d,s,n in [(4,0x75,0x166,1),(0x40,0x58,0x194,4),(0x44,0x5C,0x15A,2),
                    (0x18,0x3A,0x140,4),(0x48,0x62,0x15C,2),(0x4C,0x64,0x15E,2),
                    (0x50,0x66,0x160,2),(0x54,0x68,0x162,2),(0x58,0x6A,0x164,2),
                    (0x60,0x76,0x167,1),(0x64,0x77,0x152,1),(0x20,0x42,0x168,1),
                    (0x24,0x43,0x148,4),(0x28,0x47,0x14C,4),(0x34,0x4B,0x18B,1),
                    (0x38,0x4C,0x18C,4),(0x3C,0x50,0x190,4),(0x70,0x6C,0x153,1),
                    (0x68,0x78,0x20C,1),(0x6C,0x79,0x20D,1),(0x74,0x54,0x154,4)]:
        if struct.unpack_from('<I',mid,p)[0]:out[d:d+n]=mid[s:s+n]
    if struct.unpack_from('<I',mid,0x1C)[0]:struct.pack_into('<I',out,0x3E,((struct.unpack_from('<I',mid,0x144)[0]*tps)&0xFFFFFFFF)>>12)
    for p,d,s in [(0x5C,0x5E,0x150),(0x30,0x60,0x189)]:
        if struct.unpack_from('<I',mid,p)[0]:struct.pack_into('<H',out,d,((struct.unpack_from('<H',mid,s)[0]*tps)&0xFFFF)>>12)
    if checksum_gate==0:
        for p,d in [(8,0x6D),(12,0x71)]:
            if struct.unpack_from('<I',mid,p)[0]:struct.pack_into('<I',out,d,sound_handle)
    return bytes(out)

def view(raw):
    check(raw);v=struct.unpack_from('<I',raw,0xC8)[0]
    return dict(unknown_c8_bits=v,editor_c8_double=v/4096,opaque_e0_hex=raw[0xE0:0xE2].hex(),flags=struct.unpack_from('<I',raw,0xDC)[0])

def patch_raw(raw,offset,value):
    check(raw)
    if type(offset) is not int or offset not in WIDTHS:raise ValueError('Unknown scalar offset')
    n=WIDTHS[offset]
    if type(value) is not int or not 0<=value<1<<(8*n):raise ValueError('Value outside raw width')
    return bytes(raw[:offset])+value.to_bytes(n,'little')+bytes(raw[offset+n:])

def patch_flags(raw,mask,enabled):
    check(raw)
    if type(mask) is not int or not 0<=mask<=0xFFFFFFFF or type(enabled) is not bool:raise ValueError('Expected DWORD mask and bool')
    old=struct.unpack_from('<I',raw,0xDC)[0]
    return patch_raw(raw,0xDC,old|mask if enabled else old&(~mask&0xFFFFFFFF))
