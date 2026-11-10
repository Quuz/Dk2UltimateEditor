"""Terrain raw RGB+flags, native editor projection and game-loader arithmetic."""
import struct
GROUPS={'terrain':dict(offset=0x1DB,masks=[0x80000,0x100000,0x200000],enable=0x10000000,model=0x2E0,runtime=[0x1AF,0x1B1,0x1B3],mid=0x293,presence=0x8C),
        'ambient':dict(offset=0x201,masks=[0x2000000,0x4000000,0x8000000],enable=0x20000000,model=0x2FC,runtime=[0x1B9,0x1BB,0x1BD],mid=0x2B9,presence=0x9C)}
DIRECT_MASK=0x3187FFFF
ALL_ENCODABLE_MASK=0x3FBFFFFF

def check(raw):
    if not isinstance(raw,(bytes,bytearray)) or len(raw)!=552:raise ValueError('Expected552-byte Terrain record')

def flags_projection(flags):
    if type(flags) is not int or not 0<=flags<=0xFFFFFFFF:raise ValueError('Expected DWORD flags')
    result=flags&DIRECT_MASK
    for g in GROUPS.values():
        if flags&g['enable']:result|=flags&sum(g['masks'])
    return result

def view(raw):
    check(raw);flags=struct.unpack_from('<I',raw,0x190)[0];out=dict(flags=flags,native_decode_encode_flags=flags_projection(flags))
    for name,g in GROUPS.items():
        rgb=list(raw[g['offset']:g['offset']+3]);out[name]=dict(raw_bytes=rgb,enabled=bool(flags&g['enable']),editor_values=[v+(256 if flags&m else 0) for v,m in zip(rgb,g['masks'])],game_loaded_words=[v+(255 if flags&m else 0) for v,m in zip(rgb,g['masks'])])
    return out

def patch_channel(raw,group,channel,value):
    """Explicit UI value0..511 patch, preserving enable and all unrelated bits."""
    check(raw)
    if not isinstance(group,str) or group not in GROUPS or type(channel) is not int or not 0<=channel<3 or type(value) is not int or not 0<=value<=511:raise ValueError('Expected group,channel0..2 and UI value0..511')
    g=GROUPS[group];mask=g['masks'][channel];flags=struct.unpack_from('<I',raw,0x190)[0];out=bytearray(raw);out[g['offset']+channel]=value&255;struct.pack_into('<I',out,0x190,flags|mask if value>255 else flags&~mask);return bytes(out)

def game_colour_fragment(mid,previous):
    if len(mid)!=736 or len(previous)!=460:raise ValueError('Expected736-byte intermediate and460-byte definition')
    out=bytearray(previous)
    if struct.unpack_from('<I',mid,0xB0)[0]:out[0x17B:0x17F]=mid[0x248:0x24C]
    flags=struct.unpack_from('<I',out,0x17B)[0]
    for g in GROUPS.values():
        for i,(off,mask) in enumerate(zip(g['runtime'],g['masks'])):
            if struct.unpack_from('<I',mid,g['presence']+i*4)[0]:struct.pack_into('<H',out,off,mid[g['mid']+i])
            if flags&mask:struct.pack_into('<H',out,off,(struct.unpack_from('<H',out,off)[0]+255)&65535)
    if struct.unpack_from('<I',mid,0x98)[0]:out[0x1B5:0x1B9]=mid[0x2DC:0x2E0]
    return bytes(out)
