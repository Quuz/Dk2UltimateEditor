"""CB EffectGenerator storage and separately scoped native projections.

Lossless patching never applies the original editor's coordinate normalization.
"""
import struct
from native_placed_structures import checked,center_q12

def payload(raw):
    if not isinstance(raw,(bytes,bytearray)) or len(raw)!=44 or struct.unpack_from('<II',raw)!=(203,36):
        raise ValueError('Expected CB envelope and exactly36 payload bytes')
    return bytes(raw[8:])

def record_view(raw):
    p=payload(raw);x,y,z,flags,w,h,*rest=struct.unpack_from('<4I6H2B',p)
    return dict(x=x,y=y,z_bits=z,flags_bits=flags,width=w,height=h,effects=rest[:4],frequency=rest[4],id=rest[5],opaque_tail_hex=p[30:36].hex())

def intermediate(raw,seed=None):
    p=payload(raw)
    if seed is None:seed=bytes(76)
    if len(seed)!=76:raise ValueError('Expected76 intermediate bytes')
    b=bytearray(seed)
    for o in range(0,40,4):struct.pack_into('<I',b,o,1)
    b[40:52]=p[:12];struct.pack_into('<I',b,52,struct.unpack_from('<I',b,52)[0]|struct.unpack_from('<I',p,12)[0]);b[56:70]=p[16:30]
    return bytes(b)

def runtime_projection(raw):
    v=record_view(raw)
    return dict(position=[center_q12(v['x']),center_q12(v['y']),0],width=v['width'],height=v['height'],effects=v['effects'],frequency=v['frequency'],id=v['id'],deadline=0,active=1,runtime_flags=0)

def editor_extent(origin_bits,map_dimension_bits,requested):
    checked(origin_bits,32);checked(map_dimension_bits,32);checked(requested,16)
    total=(origin_bits+requested)&0xFFFFFFFF
    signed=total if total<0x80000000 else total-0x100000000
    return requested if signed<(map_dimension_bits&65535) else (map_dimension_bits-(origin_bits&65535)-1)&65535

def writer_payload(model,tail):
    if len(tail)!=6:raise ValueError('Expected6 scratch tail bytes')
    for k in ['x','y']:checked(model[k],32)
    for k in ['width','height']:checked(model[k],16)
    for k in ['frequency','id']:checked(model[k],8)
    if len(model['effects'])!=4:raise ValueError('Expected4 effect slots')
    for n in model['effects']:checked(n,16)
    return struct.pack('<4I6H2B',model['x'],model['y'],0,0,model['width'],model['height'],*model['effects'],model['frequency'],model['id'])+bytes(tail)

def tick_projection(node,now,tps,random_values,rejected=False):
    """Controlled provider outcomes, not a whole world or visual simulation."""
    for n in [now,tps,node['deadline']]:checked(n,32)
    checked(node['frequency'],8)
    out=dict(deadline=node['deadline'],clock_calls=0,random_bounds=[],position=None,effect=None,angle=None)
    if not node['active']:return out
    out['clock_calls']=1
    if now<=node['deadline']:return out
    it=iter(random_values)
    def rnd(bound):
        value=next(it)
        if type(value) is not int or not (0<=value<bound if bound else value==0):raise ValueError('Expected bounded provider value')
        out['random_bounds'].append(bound);return value
    slot=rnd(4);effect=node['effects'][slot]
    if not effect:return out
    x,y,z=node['position'];x=(x+rnd(node['width']<<12))&0xFFFFFFFF;y=(y+rnd(node['height']<<12))&0xFFFFFFFF;out['position']=[x,y,z]
    if rejected:return out
    out['angle']=rnd(2048)&2047;out['effect']=effect;out['deadline']=(now+node['frequency']*tps)&0xFFFFFFFF;out['clock_calls']=2
    return out

def random_step(bound,seed):
    checked(bound,32);checked(seed,32)
    if not bound:return 0,seed
    new=(seed*1664525+1013904223)&0xFFFFFFFF;rot=((new>>13)|(new<<19))&0xFFFFFFFF
    return rot%bound,new

FIELD_WIDTHS={0:4,4:4,8:4,12:4,16:2,18:2,20:2,22:2,24:2,26:2,28:1,29:1,**{i:1 for i in range(30,36)}}
def patch_raw(raw,offset,value):
    payload(raw)
    if offset not in FIELD_WIDTHS:raise ValueError('Expected complete field or opaque byte offset')
    n=FIELD_WIDTHS[offset];checked(value,n*8);o=offset+8
    return bytes(raw[:o])+value.to_bytes(n,'little')+bytes(raw[o+n:])
