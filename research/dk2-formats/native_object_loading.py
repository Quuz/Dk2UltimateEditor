"""Exact raw/intermediate/Object loader projections with null visual provider."""
import struct
from native_trap_loading import DX,OLD,sized,u32,string32

PROFILES = {
    DX:dict(program='DKII-DX.exe',init=0x56D4F0,converter=0x56D620,loader=0x49DD60,
            tps=0x6F2590,gate=0x6FCDCF,sound_buffer=0x740C20,sound_global=0x75B468,sound_prefix=0x68ED34,string=0x6361A0,divide_address=0x49DE1F),
    OLD:dict(program='DKII-local13.exe',init=0x5637F0,converter=0x563920,loader=0x494E80,
             tps=0x6DD618,gate=None,sound_buffer=0x730A80,sound_global=0x74B100,sound_prefix=0x67FCCC,string=0x627CD0,divide_address=0x494F36),
}
CONVERT = [(4,0x35A,1),(0x6C,0x35B,1),(0x60,0x354,2),(0x64,0x358,2),(0x68,0x356,2),
           (0x34,0x32C,4),(0x38,0x330,4),(0x3C,0x334,4),(0x44,0x338,4),(0x40,0x33C,4),
           (0x48,0x34A,2),(0x4C,0x348,2),(0x50,0x34C,2),(0x54,0x34E,2),(0x58,0x350,2),(0x5C,0x352,2),
           (0x70,0x35C,1),(0x74,0x35D,1),(0x78,0x340,1)]
RESOURCES = [(0xC,0x20),(0x10,0x74),(0x14,0xC8),(0x18,0x170),(0x1C,0x11C)] + [(0x20+4*i,0x1C4+84*i) for i in range(4)]
LOAD = [(4,0x3DA,0x113,1),(0x34,0x3AC,0xF8,2),(0x38,0x3B0,0xFA,2),(0x3C,0x3B4,0xFC,2),(0x40,0x3BC,0xFE,2),
        (0x48,0x3CA,0x102,2),(0x4C,0x3C8,0x104,2),(0x58,0x3D0,0x10A,2),(0x5C,0x3D2,0x10C,2),
        (0x70,0x3DC,0x114,1),(0x74,0x3DD,0x112,1),(0x60,0x3D4,0x115,2),(0x64,0x3D8,0x117,2),
        (0x68,0x3D6,0x119,2),(0x6C,0x3DB,0x11B,1),(0x50,0x3CC,0x106,2),(0x54,0x3CE,0x108,2),
        (0x78,0x3C0,0x11C,1),(0x7C,0x3C4,0xF4,4)]


def profile(sha):
    if not isinstance(sha,str) or sha not in PROFILES:
        raise ValueError('Expected exact inspected EXE SHA')
    return PROFILES[sha]


def intermediate(raw,previous):
    raw = sized(raw,894,'Object raw')
    b = bytearray(sized(previous,1022,'Object intermediate'))
    for pres,src,n in CONVERT:
        struct.pack_into('<I',b,pres,1)
        b[src+128:src+128+n] = raw[src:src+n]
    for pres,src in RESOURCES:
        struct.pack_into('<I',b,pres,1)
        b[src+128:src+128+84] = raw[src:src+84]
    for pres,src in [(0,0),(8,0x35E)]:
        struct.pack_into('<I',b,pres,1)
        b[src+128:src+160] = string32(raw[src:src+32])
    struct.pack_into('<I',b,0x30,1)
    b[0x394:0x3AC] = raw[0x314:0x32C]
    struct.pack_into('<I',b,0x7C,1)
    struct.pack_into('<I',b,0x3C4,struct.unpack_from('<I',b,0x3C4)[0]|struct.unpack_from('<I',raw,0x344)[0])
    return bytes(b)


def sound_requested(mid,sha,gate):
    mid = sized(mid,1022,'Object intermediate')
    u32(gate)
    return bool(struct.unpack_from('<I',mid,8)[0] and (profile(sha)['gate'] is None or not gate))


def definition(mid,previous,sha,tps,gate=0,sound_token=0):
    mid = sized(mid,1022,'Object intermediate')
    b = bytearray(sized(previous,285,'Object definition'))
    p = profile(sha)
    u32(tps);u32(gate);u32(sound_token)
    if p['gate'] is None or not gate:
        struct.pack_into('<I',b,0xF0,sum(mid))
    for pres,src,dst,n in LOAD:
        if struct.unpack_from('<I',mid,pres)[0]:
            b[dst:dst+n] = mid[src:src+n]
    if struct.unpack_from('<I',mid,0x44)[0]:
        if tps == 0:
            raise ValueError('Native loader divides by zero when Speed is present and TPS is zero')
        raw = struct.unpack_from('<I',mid,0x3B8)[0]
        low = (raw//tps)&65535
        struct.pack_into('<H',b,0x100,1 if low==0 and raw!=0 else low)
    if struct.unpack_from('<I',mid,0x30)[0]:
        b[0xD8:0xF0] = mid[0x394:0x3AC]
        flags = struct.unpack_from('<I',b,0xE8)[0] | int(any(mid[0x3A8:0x3AB]))
        struct.pack_into('<I',b,0xE8,flags)
    if sound_requested(mid,sha,gate):
        struct.pack_into('<I',b,0x10E,sound_token)
    return bytes(b)


def patch_light_color_bytes(raw,color):
    raw = sized(raw,894,'Object raw')
    color = sized(color,3,'light color bytes')
    return raw[:0x328]+color+raw[0x32B:]


def patch_light_flags(raw,value):
    raw = sized(raw,894,'Object raw')
    u32(value)
    return raw[:0x324]+value.to_bytes(4,'little')+raw[0x328:]
