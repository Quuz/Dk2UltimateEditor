"""Native Level projections with explicit scratch seed; preserve raw by default."""
import struct
from native_placed_structures import checked
SIZE=0x6453
MID_SIZE=0x6587
MID_BASE=0x134
OPAQUE=[(0x1184,520),(0x63C1,2)]
WRITER_UNASSIGNED=OPAQUE+[(0x63CA,1)]
WIDE_FIELDS=[(0,64),(0x80,1024),(0x880,64),(0x900,64),(0x980,1024)]+[(0x138C+0x400*i,512) for i in range(20)]+[(0x6413,32)]
ASCII_FIELDS=[(0x638E,32),(0x63E3,32)]

def bounded_wide_copy(raw,count):
    if len(raw)!=2*count:raise ValueError('Expected fixed wide buffer')
    out=bytearray(raw)
    for i in range(count):
        if raw[2*i:2*i+2]==b'\0\0':out[2*i:]=bytes(2*(count-i));break
    out[-2:]=b'\0\0'
    return bytes(out)

def intermediate(payload,seed=None):
    if len(payload)!=SIZE:raise ValueError('Expected modern25683-byte DF payload')
    seed=bytes(MID_SIZE) if seed is None else bytes(seed)
    if len(seed)!=MID_SIZE:raise ValueError('Expected25991-byte intermediate seed')
    out=bytearray(seed)
    for i in range(77):struct.pack_into('<I',out,4*i,1)
    out[MID_BASE:]=payload
    for off,n in OPAQUE:out[MID_BASE+off:MID_BASE+off+n]=seed[MID_BASE+off:MID_BASE+off+n]
    for off,count in WIDE_FIELDS:out[MID_BASE+off:MID_BASE+off+2*count]=bounded_wide_copy(payload[off:off+2*count],count)
    for off,count in ASCII_FIELDS:
        value=payload[off:off+count].split(b'\0',1)[0][:count-1];out[MID_BASE+off:MID_BASE+off+count]=value+bytes(count-len(value))
    flags=struct.unpack_from('<H',seed,MID_BASE+0x638C)[0]|struct.unpack_from('<H',payload,0x638C)[0];struct.pack_into('<H',out,MID_BASE+0x638C,flags)
    return bytes(out)

def ansi_to_wide(source,count):
    """48CF90: signed bytes widen to WORD, count pads but does not truncate."""
    if not isinstance(source,bytes):raise ValueError('Expected CString byte contents without terminator')
    checked(count,32)
    return b''.join(struct.pack('<H',b if b<128 else 0xFF00|b) for b in source)+bytes(2*max(0,count-len(source)))

def ansi_copy(source,count):
    checked(count,32)
    return bytes(source)+bytes(max(0,count-len(source)))

def writer_payload(seed,model,strings,player_nodes):
    """Controlled493200 projection; strings supply names/messages/CString bytes.

    Caller supplies full scratch seed, because native writer leaves gaps.
    Oversized strings reproduce only writes that stay inside this payload.
    """
    if len(seed)!=SIZE or len(model)<0xA6C:raise ValueError('Invalid payload seed/model')
    out=bytearray(seed)
    def put(off,value):
        if off<0 or off+len(value)>SIZE:raise ValueError('String would exceed whole DF scratch payload')
        out[off:off+len(value)]=value
    for off,count,name in [(0,64,'name'),(0x80,1024,'description'),(0x880,64,'author'),(0x900,64,'email'),(0x980,1024,'information')]:put(off,ansi_to_wide(strings[name],count))
    put(0x63E3,ansi_copy(strings['terrain_path'],32))
    put(0x1180,model[0x944:0x948])
    put(0x63AE,model[0x8E:0x8F]);put(0x6403,model[0x8F:0x90]);put(0x6407,model[0x90:0x92])
    out[0x6404]=(len(player_nodes)-2)&255
    put(0x63AF,model[0x92:0x96]);put(0x6405,model[0x96:0x97]);put(0x63B3,model[0x97:0x9B]);put(0x6406,model[0x9B:0x9C])
    put(0x63CB,model[0xC4:0xCC]);put(0x63D3,model[0xCC:0xDC]);put(0x6409,model[0x9C:0xA6])
    put(0x638E,ansi_copy(strings['sound_category'],32))
    out[0x63B8]=model[0x8C];out[0x63B7]=model[0x8D]
    for raw_off,model_off in [(0x63B9,0xA6),(0x63BB,0xA8),(0x63BD,0xAA),(0x63BF,0xAC),(0x63C5,0xAE),(0x63C7,0xB0),(0x63C3,0xB2)]:put(raw_off,model[model_off:model_off+2])
    out[0x63C9]=model[0xB4]
    flags=struct.unpack_from('<H',model,0xDC)[0]&~4
    if any(p[0x1C]>=3 and not p[0x40] for p in player_nodes):flags|=4
    struct.pack_into('<H',out,0x638C,flags)
    for i in range(20):put(0x138C+0x400*i,ansi_to_wide(strings['messages'][i],512))
    put(0x6413,ansi_to_wide(strings['hero_name'],32))
    return bytes(out)

def patch_raw(payload,offset,value):
    if len(payload) not in [25603,SIZE] or not isinstance(value,bytes) or offset<0 or offset+len(value)>len(payload):raise ValueError('Invalid raw DF patch')
    return payload[:offset]+value+payload[offset+len(value):]
