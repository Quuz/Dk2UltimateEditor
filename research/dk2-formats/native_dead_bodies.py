"""CA storage: native Level, editor health WORD and lossless patching.

The inspected game placement ignores rawWORD0C after its checksum input.
"""
import struct
from native_placed_structures import checked,center_q12

def payload(raw):
    if not isinstance(raw,(bytes,bytearray)) or len(raw)!=24 or struct.unpack_from('<II',raw)!=(202,16):raise ValueError('Expected CA envelope and16-byte payload')
    return bytes(raw[8:])

def record_view(raw):
    x,y,z,value,creature,level=struct.unpack('<3IHBB',payload(raw))
    return dict(x=x,y=y,z_bits=z,editor_health_word=value,creature_id=creature,level=level)

def intermediate(raw):
    p=payload(raw);return struct.pack('<4I',1,1,1,1)+p

def runtime_projection(raw,tps,decay_seconds,angle=0):
    v=record_view(raw);checked(tps,32);checked(decay_seconds,32);checked(angle,16)
    return dict(position=[center_q12(v['x']),center_q12(v['y']),0],creature_id=v['creature_id'],level=v['level'],angle=angle&2047,decay_counter=(tps*decay_seconds)&0xFFFFFFFF)

def editor_health(value,major=3,legacy_integer=None):
    """Exact byte/clamp stage; legacy x87 integer conversion must be explicit."""
    checked(value,16);checked(major,32)
    if major==1:
        if type(legacy_integer) is not int or not -(1<<63)<=legacy_integer<1<<63:raise ValueError('Supply explicit legacy x87 converted signed64 integer')
        converted=legacy_integer
    else:converted=value
    return min(converted&255,100)

def writer_payload(model):
    for k in ['x','y']:checked(model[k],32)
    for k in ['health_byte','creature_id','level']:checked(model[k],8)
    return struct.pack('<3IHBB',model['x'],model['y'],0,model['health_byte'],model['creature_id'],model['level'])

def body_scale_bits(level,base,growth):
    checked(level,8);checked(base,16)
    if type(growth) is not int or not -32768<=growth<=32767:raise ValueError('Expected signed16 growth')
    product=base*growth;adjusted=base if level==1 else (abs(product)//100)*(1 if product>=0 else -1)
    return (((level+15)*256*adjusted)>>12)&0xFFFFFFFF

FIELD_WIDTHS={0:4,4:4,8:4,12:2,14:1,15:1}
def patch_raw(raw,offset,value):
    payload(raw)
    if offset not in FIELD_WIDTHS:raise ValueError('Expected complete field offset')
    n=FIELD_WIDTHS[offset];checked(value,n*8);o=8+offset
    return bytes(raw[:o])+value.to_bytes(n,'little')+bytes(raw[o+n:])
