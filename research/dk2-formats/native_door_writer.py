"""Pro/RC3 model-to-raw Door writer projection;losslesspatching is separate."""
import struct
PROFILES={'Pro':dict(program='DK2 Editor pro.exe',writer=0x4A8BF0,array=0x4B8,count=0x4BC,date_global=0x55094A,date_helper=0x4AB320),
          'RC3':dict(program='DK2-Editor-rc3.exe',writer=0x4AE400,array=0x4B4,count=0x4B8,date_global=0x5680B2,date_helper=0x4B0B30)}
DIRECT=[(0x244,0x168,1),(0x23A,0x124,2),(0x23C,0x126,2),(0x23E,0x128,2),(0x240,0x12A,2),(0x242,0x12C,2),(0x22E,0x288,2),(0x236,0x28A,2),(0x238,0x28C,2),(0x22C,0x28E,2),(0x21C,0x290,2),(0x234,0x292,2),(0x226,0x298,1),(0x246,0x294,1),(0x247,0x295,1),(0x232,0x296,2),(0x227,0x299,1),(0x224,0x158,2),(0x245,0x15A,1)]
RESOURCES=[(0x20,0xCC),(0x74,0x14),(0xC8,0x70),(0x11C,0x170),(0x170,0x1CC),(0x1C4,0x228)]
GAPS=[(0x21E,2),(0x220,4),(0x230,2)]
FLAGS=[(0x20,0x29C),(0x40,0x2A0),(0x80,0x2A4)]

def record(model,prior_raw,label=b'',sound=b''):
    if len(model)!=0x300 or len(prior_raw)!=616 or len(label)>616 or len(sound)>32:raise ValueError('Expectedexplicit300hexmodel/616scratch/boundedoutputstrings')
    out=bytearray(prior_raw);coverage=bytearray(616)
    def copy(at,b):out[at:at+len(b)]=b;coverage[at:at+len(b)]=bytes([1])*len(b)
    for dst,src,n in DIRECT:copy(dst,model[src:src+n])
    copy(0x218,struct.pack('<I',int(struct.unpack_from('<d',model,0x280)[0]*4096)&0xFFFFFFFF))
    flags=sum(bit for bit,src in FLAGS if struct.unpack_from('<I',model,src)[0]);copy(0x228,struct.pack('<I',flags))
    for dst,src in RESOURCES:copy(dst,model[src:src+84])
    # ActualCStringhelpercopiesfulllength;count32onlypads shortstrings.
    copy(0,label+bytes(max(0,32-len(label))));copy(0x248,sound+bytes(32-len(sound)))
    untouched={i for off,n in GAPS for i in range(off,off+n)}
    if len(label)<=32:assert {i for i,v in enumerate(coverage) if not v}==untouched
    return bytes(out)

def patch_word236(raw,value):
    if len(raw)!=616 or type(value)is not int or not 0<=value<=65535:raise ValueError('Expected616-byteDoor andunsignedWORD')
    return bytes(raw[:0x236])+value.to_bytes(2,'little')+bytes(raw[0x238:])

def patch_untransferred(raw,offset,value):
    widths=dict(GAPS)
    if len(raw)!=616 or offset not in widths or type(value)is not int or not 0<=value<1<<(8*widths[offset]):raise ValueError('Expectedcompleteuntransferredspan')
    n=widths[offset];return bytes(raw[:offset])+value.to_bytes(n,'little')+bytes(raw[offset+n:])
