"""DX neutral-force controller projection for documented bounded fixtures."""
import math,struct
from native_navigation_speed import u32,s32,truncate_vector,instance_cap
from native_possession_stats import speed_modifier

def patch_traversal_flags(raw,*,water=None,lava=None):
    from native_creature_extension import check
    check(raw)
    if any(x is not None and type(x) is not bool for x in [water,lava]):raise ValueError('ExpectedbooleanorNone')
    out=bytearray(raw);flags=struct.unpack_from('<I',raw,0xefc)[0]
    for value,mask in [(water,0x10),(lava,0x20)]:
        if value is not None:flags=(flags&~mask)|(mask if value else 0)
    struct.pack_into('<I',out,0xefc,flags);return bytes(out)

def normalize(vector):
    x,y=map(s32,vector);ax,ay=abs(x),abs(y)
    if (ax|ay)&0xfffff000==0:
        length=math.isqrt(u32((ax*ax+ay*ay)*64));factor=0 if not length else 0x80000//length
        return [u32(x*factor),u32(y*factor)]
    if (ax|ay)&0xffc00000==0:
        length=math.isqrt((ax>>8)**2+(ay>>8)**2);factor=0 if not length else 0x1000000//length
        return [u32(x*factor>>16),u32(y*factor>>16)]
    raise ValueError('Projection onlycoversfixturevelocitiesbelow400000h')

def walk_cost(old,new,*,owner=0,bit17=False,bit18=False,bit19=False,definition_bit5=False,extra=False):
    if new&8:return 0
    if new&0xf0 and (new>>4&15)!=owner and new&2:return 0
    if not new&0xf0:
        if not old&0xf0:
            if not definition_bit5 and new&3==3 and old&3!=3:return 0
            if not extra and new&3==1 and old&3!=1:return 0
            if not bit18 and (old^new)&4:return 0
        elif not bit19:
            if not bit17 and old>>4&15==owner and old&4:return 0
        elif old>>4&15!=owner or old&4:return 0
    elif not bit19:
        if not bit17 and new>>4&15==owner and new&4:return 0
    elif new>>4&15!=owner or new&4:return 0
    return (new&3)+1

def outcome(c):
    raw=speed_modifier(c['base'],c['percent'],c['state'] in ['possessed','both'],c['state'] in ['secondary','both'])
    cap=instance_cap(raw);pos=[u32(x<<4) for x in c['position'][:2]];vel=[u32(x<<4) for x in c['velocity']]
    e=dict(getter=raw,**cap,skipped=not cap['controller_max_speed'])
    if e['skipped']:e.update(position=pos,velocity=vel,normal=[0,0],history=0,progress=0)
    else:
        vel=truncate_vector(vel,cap['controller_max_speed']);candidate=[u32(p+s32(v)) for p,v in zip(pos,vel)];nav={(x,y):value for x,y,value in c['nav']}
        def cell(p):
            x,y=(u32(v)>>16 for v in p)
            return nav.get((x,y),0) if x<32 and y<32 else 8
        old=cell(pos)
        def allows(p):return walk_cost(old,cell(p),owner=c.get('owner_index',0),bit17=c.get('bit17',False),bit18=c.get('bit18',False),bit19=c.get('bit19',False),definition_bit5=c.get('definition_bit5',False),extra=c.get('extra',False) or (c.get('preflight',False) and c.get('definition_bit4',False)))
        if allows(candidate):pos=candidate
        elif allows([candidate[0],pos[1]]):pos=[candidate[0],pos[1]];vel[1]=0
        elif allows([pos[0],candidate[1]]):pos=[pos[0],candidate[1]];vel[0]=0
        else:raise ValueError('Fixture reaches unprojected rotated-collision alternatives')
        e.update(position=pos,velocity=vel,normal=normalize(vel),history=0,progress=0x10000)
    e['perpendicular']=[u32(-s32(e['normal'][1])),e['normal'][0]]
    e['xy']=[u32(s32(x)>>4) for x in e['position']];e['velocity_q12']=[u32(s32(x)>>4) for x in e['velocity']]
    if c.get('apply'):
        x,y=(v>>12 for v in e['xy']);nav={(x,y):v for x,y,v in c['nav']}
        e['apply_accepted']=int(x<32 and y<32 and not nav.get((x,y),0)&8)
        e['actor_position']=[*e['xy'],c['position'][2]] if e['apply_accepted'] else list(c['position'])
    if c.get('preflight'):e['preflight']=int(c.get('definition_bit4',False)) # fixtures use sourceNav0,targetNav1,no flight
    return e
