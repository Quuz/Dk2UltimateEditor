"""EffectElement initializer range arithmetic under explicit coefficient data."""
from native_wander_state import rand
def u32(v):return v&0xFFFFFFFF
def s16(v):return (v&32767)-(v&32768)
def plan(c):
    seed=c['seed'];draws=[]
    def draw(bound,label):
        nonlocal seed
        seed,value=rand(seed,u32(bound));draws.append(dict(label=label,bound=u32(bound),value=value));return value
    angles=[draw(2048,'constructor-angle'+str(i)) for i in range(3)]
    def choose(pair,signed,inclusive,label):
        low,high=[s16(v) if signed else v for v in pair];delta=high-low
        return u32(low+(draw(delta+int(inclusive),label) if delta else 0))
    xy=choose(c['xy'],True,True,'speedXY');z=choose(c['z'],True,True,'speedZ');angle=draw(2048,'initial-direction')
    hp=choose(c['hp'],False,True,'lifetime')
    product=u32(hp*c['fade']);product=(product&0x7FFFFFFF)-(product&0x80000000)
    quotient=abs(product)//100*(-1 if product<0 else 1);threshold=u32(hp-quotient)
    scale=choose(c['scale'],False,False,'scale') if c['resource_flags']&1 else None
    return dict(seed=seed,draws=draws,constructor_angles=angles,initial_direction=angle,velocity=[xy,u32(-xy),z],lifetime=hp,alpha_threshold=threshold,scale=scale)
def patch_raw(raw,offset,value):
    fields={0x80:4,0x84:4,0x88:4,0x8C:4,0x90:4,0x94:4,0xA2:2,0xA4:2,0xB1:1,0xB2:1}
    if not isinstance(raw,(bytes,bytearray)) or len(raw)!=182 or type(offset) is not int or offset not in fields:raise ValueError('Expected182-byte EffectElement and known range/index field')
    n=fields[offset]
    if type(value) is not int or not 0<=value<1<<(8*n):raise ValueError('Value outside raw width')
    return bytes(raw[:offset])+value.to_bytes(n,'little')+bytes(raw[offset+n:])
