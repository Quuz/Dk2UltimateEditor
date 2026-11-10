"""Exact inspected Room editor-model projection, separate from lossless patches.

The original writer never assigns raw36F. Caller must pass its actual stack
byte; it must not be invented as zero by a supposedly exact writer model.
"""
import struct

DIRECT=[(0x392,0x168,1),(0x374,0x124,2),(0x376,0x126,2),
    (0x37C,0x128,2),(0x37E,0x12A,2),(0x36C,0x158,2),
    (0x378,0x4F2,2),(0x41D,0x4F0,2),(0x394,0x4E0,1),
    (0x396,0x4F7,1),(0x395,0x4F6,1),(0x37A,0x4F8,2),
    (0x393,0x512,1),(0x41B,0x513,1),(0x41C,0x514,1),
    (0x397,0x4C0,3),(0x36E,0x4D0,1),(0x3C2,0x15A,1),
    (0x39A,0x50A,8),(0x382,0x4FA,16)]
SCALED=[(0x368,0x4E8,4),(0x380,0x4C8,2),(0x3C3,0x4D8,4)]
RESOURCES=[(0x20,0x14),(0x74,0x70),(0xC8,0x170),(0x11C,0x1CC),
    (0x170,0x228),(0x1C4,0x284),(0x218,0x2E0),(0x26C,0x33C),
    (0x2C0,0x398),(0x3C7,0x3F4),(0x314,0x450)]
FLAG_FIELDS=[(1<<i,0x518+4*i) for i in range(9)]+[(0x1000,0x53C),(0x2000,0x540)]

def record(model,label=b'',sound=b'',*,unassigned_byte):
    if type(model) is not bytes or len(model)!=0x548:raise ValueError('Expected548hex immutable Room model fixture')
    if type(unassigned_byte) is not int or not 0<=unassigned_byte<=255:raise ValueError('Expected actual raw36F stack byte')
    if not isinstance(label,bytes) or not isinstance(sound,bytes) or max(len(label),len(sound))>32:raise ValueError('Bound strings to32bytes')
    out=bytearray(1055);covered=bytearray(1055)
    def put(a,b):out[a:a+len(b)]=b;covered[a:a+len(b)]=bytes([1])*len(b)
    put(0,label+bytes(32-len(label)));put(0x3A2,sound+bytes(32-len(sound)))
    for dst,src,n in DIRECT:put(dst,model[src:src+n])
    for dst,src,n in SCALED:
        value=int(struct.unpack_from('<d',model,src)[0]*4096)
        put(dst,(value&((1<<(8*n))-1)).to_bytes(n,'little'))
    for dst,src in RESOURCES:put(dst,model[src:src+84])
    flags=(struct.unpack_from('<I',model,0x4BC)[0]&0x3800)>>2
    for bit,off in FLAG_FIELDS:
        if struct.unpack_from('<I',model,off)[0]:flags|=bit
    put(0x370,struct.pack('<I',flags))
    assert [i for i,v in enumerate(covered) if not v]==[0x36F]
    put(0x36F,bytes([unassigned_byte]));return bytes(out)

def patch_unknown_resource(raw,resource):
    if type(raw) is not bytes or len(raw)!=1055 or type(resource) is not bytes or len(resource)!=84:
        raise ValueError('Expected one1055-byte Room and84explicit resource bytes')
    return raw[:0x1C4]+resource+raw[0x218:]

def patch_torch_intensity(raw,value):
    if type(raw) is not bytes or len(raw)!=1055 or type(value) is not int or not 0<=value<=255:
        raise ValueError('Expected one1055-byte Room and native unsigned8 intensity')
    return raw[:0x36E]+bytes([value])+raw[0x36F:]
