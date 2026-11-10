"""CD camera raw storage, low-byte preset IDs and scoped active-camera math."""
import struct
from native_placed_structures import checked
from native_placed_rooms import signed32

def payload(raw):
    if not isinstance(raw,(bytes,bytearray)) or len(raw)<8:raise ValueError('Expected CD envelope')
    tag,n=struct.unpack_from('<II',raw)
    if tag!=205 or n>84 or len(raw)!=n+8:raise ValueError('Expected bounded CD payload0..84')
    return bytes(raw[8:]).ljust(84,b'\0')

def record_view(raw):
    p=payload(raw)
    return dict(payload_bytes=len(raw)-8,position_bits=list(struct.unpack_from('<3I',p)),min_clip_bits=list(struct.unpack_from('<3I',p,12)),max_clip_bits=list(struct.unpack_from('<3I',p,24)),view_bits=list(struct.unpack_from('<3I',p,36)),zoom_bits=list(struct.unpack_from('<3I',p,48)),lens_bits=list(struct.unpack_from('<3I',p,60)),flags=struct.unpack_from('<I',p,72)[0],angles=list(struct.unpack_from('<3H',p,76)),id=p[82],opaque_tail=p[83])

def intermediate(raw,seed=None):
    p=payload(raw)
    if seed is None:seed=bytes(152)
    if len(seed)!=152:raise ValueError('Expected152 intermediate bytes')
    b=bytearray(seed)
    for off in range(0,68,4):struct.pack_into('<I',b,off,1)
    b[68:140]=p[:72];struct.pack_into('<I',b,140,struct.unpack_from('<I',b,140)[0]|struct.unpack_from('<I',p,72)[0]);b[144:151]=p[76:83]
    return bytes(b)

def preset_projection(raw,global_setting=5):
    """Native store projection; global_setting is Machine Spec Level (phase46)."""
    checked(global_setting,32);mid=bytearray(intermediate(raw));ident=mid[150]
    if not 1<=ident<=18:return dict(id=ident,accepted=False,slot_offset=None,slot=None)
    if global_setting<5:
        lo=signed32(int.from_bytes(mid[120:124],'little'));hi=signed32(int.from_bytes(mid[124:128],'little'));d=signed32(hi-lo);third=(abs(d)//3)*(1 if d>=0 else -1)
        struct.pack_into('<I',mid,124,(lo+third)&0xFFFFFFFF);struct.pack_into('<I',mid,32,1)
    return dict(id=ident,accepted=True,slot_offset=40+152*ident,slot=bytes(mid))

def preset_for_machine_spec(raw,machine_spec_level):
    """Explicit named setting; preserve raw input instead of saving projection."""
    return preset_projection(raw,machine_spec_level)

def clamp_exclusive(value,lower,upper):
    for x in [value,lower,upper]:checked(x,32)
    v,lo,hi=signed32(value),signed32(lower),signed32((upper-1)&0xFFFFFFFF)
    return (hi if max(v,lo)>=hi else max(v,lo))&0xFFFFFFFF

def active_delta(field,current,delta,lower,upper,flags=0,busy=0):
    for x in [current,delta,lower,upper,flags,busy]:checked(x,32)
    masks={'zoom':8,'view':16,'lens':32}
    if field not in masks:raise ValueError('Expected zoom/view/lens')
    if flags&masks[field] or field=='zoom' and busy:return current
    return clamp_exclusive((current+delta)&0xFFFFFFFF,lower,upper)

def relative_angles(current,deltas,flags=0,busy=0):
    if len(current)!=3 or len(deltas)!=3:raise ValueError('Expected yaw, roll, pitch')
    for a in [*current,*deltas]:checked(a,16)
    checked(flags,32);checked(busy,32)
    # Pure projection only for modes whose jump-table branch permits all angles.
    if busy:return list(current)
    return [a if flags&(1<<i) else (a+d)&2047 for i,(a,d) in enumerate(zip(current,deltas))]

FIELD_WIDTHS={**{i:4 for i in range(0,76,4)},76:2,78:2,80:2,82:1,83:1}
def patch_raw(raw,offset,value):
    payload(raw)
    if offset not in FIELD_WIDTHS:raise ValueError('Expected complete field offset')
    n=FIELD_WIDTHS[offset]
    if offset+n>len(raw)-8:raise ValueError('Field not physically present')
    checked(value,n*8);o=8+offset
    return bytes(raw[:o])+value.to_bytes(n,'little')+bytes(raw[o+n:])
