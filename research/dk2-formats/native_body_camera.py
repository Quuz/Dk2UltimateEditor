"""Mode11 body-camera arithmetic using exact PE trig data and native PRNG rule.

Projection of a newly executed branch,not rendering or camera interpolation.
CA payload has no angle; its runtime angle originates in native creation.
"""
import struct
from native_camera_activation import activate,get,put,word,setw
U32=0xFFFFFFFF
def signed(n):return n if n<0x80000000 else n-0x100000000

def random_step(seed,bound):
    if not isinstance(seed,int) or not 0<=seed<=U32 or not isinstance(bound,int) or not 1<=bound<=U32:raise ValueError('Expected DWORD seed and nonzeroDWORD bound')
    updated=(seed*0x19660D+0x3C6EF35F)&U32
    rotated=((updated>>13)|(updated<<19))&U32
    return updated,rotated%bound

def project(manager,entity,definition,trig,*,node=1,valid=True,now=123456,ground=768):
    if len(trig)!=0x2800 or len(entity)<0x12E or len(definition)<0x65E:raise ValueError('Expected explicit body/definition and10240 trig bytes')
    if get(entity,0x12)!=6:raise ValueError('Expected native kind6 body fixture')
    if not valid:return activate(manager,11,node,entity=entity,definition=definition,node_valid=False,now=now,ground=ground)
    angle=word(entity,0xF4)
    if angle>=2048:raise ValueError('Native camera does not mask tableindex;only validruntimeangles0..2047 projected')
    # Reuse previously measured common mode11 setup; replace only the newly
    # investigated kind6 branch's position,angles and explicit entity sideeffect.
    fallback=bytearray(entity);put(fallback,0x12,4)
    v=activate(manager,11,node,entity=fallback,definition=definition,node_valid=True,now=now,ground=ground)
    b=bytearray(v['manager']);body=bytearray(entity);height=word(definition,0x65C)
    tx=struct.unpack_from('<i',trig,angle*4)[0];ty=struct.unpack_from('<i',trig,0x800+angle*4)[0]
    dx=signed((tx*height)&U32)>>16;dy=signed((ty*height)&U32)>>16
    position=((get(entity,0x1A)-dx)&U32,(get(entity,0x1E)+dy)&U32,(ground+256)&U32)
    struct.pack_into('<3I',b,0xDB6,*position)
    sx,r1=random_step(get(entity,0x1A),0x77758)
    sy,r2=random_step(get(entity,0x1E),0x2278)
    seed=r1^r2;seed,r3=random_step(seed,256);seed,r4=random_step(seed,512);seed,r5=random_step(seed,256)
    setw(b,0xDC6,(word(manager,0xD84)+r5-128)&2047)
    setw(b,0xDC8,(r4+768)&2047);setw(b,0xDCA,(r3-128)&2047)
    body[0xF3]&=254
    v.update(manager=bytes(b),entity=bytes(body),body_position=position,random_trace=dict(first_x_seed=sx,first_y_seed=sy,first_returns=[r1,r2],final_seed=seed,angle_returns=[r3,r4,r5]))
    return v
