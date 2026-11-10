"""CreatureSpells storage views; keep raw authoritative when editing."""
import struct
FLAG_MODEL=((1,0x184),(2,0x188),(4,0x18C),(8,0x190),(32,0x194),(64,0x198),(128,0x19C),(256,0x1A0))
FIELD_WIDTHS={0xC8:4,0xCC:4,0xD0:4,0xD4:4,**{o:2 for o in range(0xD8,0xE6,2)},**{o:1 for o in range(0xE6,0xEA)},0xEA:4,0xEE:1}

def check(raw):
    if not isinstance(raw,(bytes,bytearray)) or len(raw)!=266:raise ValueError('Expected266-byte CreatureSpells record')

def intermediate(raw,seed=0):
    """Compatibility320-byte prefix view; full native size346, see phase42 API."""
    check(raw)
    if type(seed) is not int or not 0<=seed<=255:raise ValueError('Expected byte seed')
    out=bytearray([seed])*320
    for o in range(0,80,4):struct.pack_into('<I',out,o,1)
    out[0x50:0x70]=raw[:32].split(b'\0',1)[0].ljust(32,b'\0');out[0x6F]=0
    out[0x70:0xC4]=raw[0x20:0x74];out[0xC4:0x118]=raw[0x74:0xC8]
    for dst,src,n in [(0x118,0xC8,4),(0x11C,0xCC,4),(0x120,0xD0,4),(0x128,0xD8,2),(0x12A,0xDA,2),(0x12C,0xDC,2),(0x12E,0xDE,2),(0x130,0xE0,2),(0x132,0xE2,2),(0x134,0xE4,2),(0x136,0xE6,4),(0x13A,0xEA,4),(0x13E,0xEE,1)]:out[dst:dst+n]=raw[src:src+n]
    struct.pack_into('<I',out,0x124,int.from_bytes(raw[0xD4:0xD8],'little') | seed*0x01010101)
    return bytes(out)

def flags_model_projection(raw_flags):
    if type(raw_flags) is not int or not 0<=raw_flags<=0xFFFFFFFF:raise ValueError('Expected DWORD flags')
    return raw_flags&0x1EF

def patch_raw(raw,offset,value):
    check(raw)
    if type(offset) is not int or offset not in FIELD_WIDTHS:raise ValueError('Unknown scalar offset')
    n=FIELD_WIDTHS[offset]
    if type(value) is not int or not 0<=value<1<<(8*n):raise ValueError('Value outside raw width')
    return bytes(raw[:offset])+value.to_bytes(n,'little')+bytes(raw[offset+n:])

def patch_flags(raw,mask,enabled):
    check(raw)
    if type(mask) is not int or not 0<=mask<=0xFFFFFFFF or type(enabled) is not bool:raise ValueError('Expected DWORD mask and bool')
    old=int.from_bytes(raw[0xD4:0xD8],'little');return patch_raw(raw,0xD4,old|mask if enabled else old&(~mask&0xFFFFFFFF))
