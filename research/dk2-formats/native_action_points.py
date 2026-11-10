"""Native C5 field projections; raw bits and native editor save are separate."""
import struct
from native_placed_structures import checked,center_q12

def payload(raw):
    if not isinstance(raw,(bytes,bytearray)) or len(raw)<8:raise ValueError('Expected Things envelope')
    tag,n=struct.unpack_from('<II',raw)
    if tag!=197 or n>56 or len(raw)!=n+8:raise ValueError('Expected bounded C5 payload0..56')
    return bytes(raw[8:]).ljust(56,b'\0')

def bounds(words):
    if len(words)!=4:raise ValueError('Expected4 DWORD bit patterns')
    for n in words:checked(n,32)
    x,y,end_x,last=words
    if end_x==0:return [(x-last)&0xFFFFFFFF,(y-last)&0xFFFFFFFF,(x+last)&0xFFFFFFFF,(y+last)&0xFFFFFFFF]
    return list(words)

def record_view(raw):
    p=payload(raw);xy=struct.unpack_from('<4I',p);wait,flags,trigger,ident,nxt=struct.unpack_from('<HHHBB',p,16)
    return dict(tag=197,payload_bytes=len(raw)-8,coordinate_bits=list(xy),coordinate_mode='center_radius' if xy[2]==0 else 'corners',bounds_bits=bounds(xy),wait=wait,flags=flags,trigger=trigger,id=ident,next_id=nxt,name_raw_hex=p[24:56].hex())

def intermediate(raw):
    p=payload(raw);v=record_view(raw);b=bytearray(92)
    for off in range(0,36,4):struct.pack_into('<I',b,off,1)
    struct.pack_into('<4I',b,36,*v['bounds_bits']);b[52:60]=p[16:24]
    name=p[24:56].split(b'\0',1)[0][:31];b[60:60+len(name)]=name
    return bytes(b)

def runtime_projection(raw,tps,initial_flags=0):
    v=record_view(raw);checked(tps,32);checked(initial_flags,8)
    return dict(bounds_words=[n&65535 for n in v['bounds_bits']],wait_ticks=(v['wait']*tps)&65535,flags=(initial_flags&~0x3D)|(v['flags']&0x3D),next_id=v['next_id'],id=v['id'],trigger=v['trigger'])

def inclusive_area(bounds_words):
    if len(bounds_words)!=4:raise ValueError('Expected4 coordinate WORDs')
    for n in bounds_words:checked(n,16)
    a,b,c,d=bounds_words;return ((c-a+1)*(d-b+1))&65535

def patrol_candidate(bounds_words,random_x=0,random_y=0):
    for n in bounds_words:checked(n,16)
    x,y,ex,ey=bounds_words;dx=ex-x;dy=ey-y
    if dx<0 or dy<0:raise ValueError('Diagnostic API requires ordered bounds; native path does not validate')
    for r,span in [(random_x,dx),(random_y,dy)]:
        if type(r) is not int or not (0<=r<span if span else r==0):raise ValueError('Expected random value below endpoint difference')
    return [center_q12(x+random_x),center_q12(y+random_y),0]

def editor_saved_flags(flags,next_ids):
    checked(flags,16)
    for n in next_ids:checked(n,8)
    return (flags&255)|(4 if any(next_ids) else 0)

def writer_payload(model,seed_name,next_ids):
    if not isinstance(seed_name,(bytes,bytearray)) or len(seed_name)!=32:raise ValueError('Expected32-byte scratch name seed')
    for n in model['bounds']:checked(n,32)
    for k in ['wait','trigger']:checked(model[k],16)
    for k in ['id','next_id']:checked(model[k],8)
    return struct.pack('<4IHHHBB',*model['bounds'],model['wait'],editor_saved_flags(model['flags'],next_ids),model['trigger'],model['id'],model['next_id'])+bytes(seed_name)

def advance(current_objective_bits,next_id,now,wait_ticks,previous_deadline=0):
    for n in [current_objective_bits,now,previous_deadline]:checked(n,32)
    checked(next_id,8);checked(wait_ticks,16)
    if not current_objective_bits&255:return dict(objective_bits=current_objective_bits,deadline=previous_deadline,state_requested=False)
    return dict(objective_bits=next_id,deadline=(now+wait_ticks)&0xFFFFFFFF,state_requested=True)

def patch_raw(raw,offset,value):
    record_view(raw);n={0:4,4:4,8:4,12:4,16:2,18:2,20:2,22:1,23:1}.get(offset)
    if not n or offset+n>len(raw)-8:raise ValueError('Field not physically present')
    checked(value,8*n);o=8+offset;return bytes(raw[:o])+value.to_bytes(n,'little')+bytes(raw[o+n:])
