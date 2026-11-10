"""Verified C6/C7/C8 projections; raw files remain authoritative."""
import struct
from native_placed_structures import checked,center_q12
SIZES={198:24,199:32,200:28}
CONVERTERS={198:0x56F330,199:0x56F430,200:0x56F540}
EDITOR_CONVERTERS={198:0x42F410,199:0x42F510,200:0x42F620}
MID={198:dict(size=56,xyz=0x20,gold=0x2C,level=0x2E,flags=0x2F,health=0x30,trigger=0x34,creature=0x36,wander=0x37),
     199:dict(size=84,xyz=0x34,gold=0x40,level=0x42,flags=0x43,ap=0x44,health=0x48,trigger=0x4C,target=0x4E,objective=0x4F,creature=0x50,wander=0x51,party=0x52,flags2=0x53),
     200:dict(size=64,xyz=0x24,gold=0x30,level=0x32,flags=0x33,health=0x34,ap=0x38,trigger=0x3C,creature=0x3E,owner=0x3F)}

def record_view(raw):
    if not isinstance(raw,(bytes,bytearray)) or len(raw)<8:raise ValueError('Expected Things envelope')
    tag,n=struct.unpack_from('<II',raw)
    if tag not in SIZES or n!=SIZES[tag] or len(raw)!=n+8:raise ValueError('Expected C6/C7/C8 creature payload')
    p=raw[8:];x,y,z=struct.unpack_from('<3I',p);gold,level,flags=struct.unpack_from('<HBB',p,12)
    health=struct.unpack_from('<I',p,20 if tag==199 else 16)[0];trigoff=20 if tag==198 else 24
    out=dict(tag=tag,x_bits=x,y_bits=y,z_bits=z,gold_percent_bits=gold,health_percent_bits=health,level=level,flags=flags,trigger_id=struct.unpack_from('<H',p,trigoff)[0],
             creature_id=p[{198:22,199:28,200:26}[tag]],position_q12=[center_q12(x),center_q12(y),0])
    if tag in [198,199]:out['wander_radius_byte']=p[23 if tag==198 else 29]
    if tag==199:out.update(ap_bits=struct.unpack_from('<I',p,16)[0],target_player=p[26],objective=p[27],party_id=p[30],flags2=p[31])
    if tag==200:out.update(ap_bits=struct.unpack_from('<I',p,20)[0],player_id=p[27])
    return out

def to_intermediate(raw):
    v=record_view(raw);tag=v['tag'];m=MID[tag];b=bytearray(m['size']);payload=raw[8:]
    for off in range(0,m['xyz'],4):struct.pack_into('<I',b,off,1)
    struct.pack_into('<3I',b,m['xyz'],v['x_bits'],v['y_bits'],v['z_bits'])
    for name,key,width in [('gold','gold_percent_bits',2),('level','level',1),('flags','flags',1),('health','health_percent_bits',4),('trigger','trigger_id',2),('creature','creature_id',1),('wander','wander_radius_byte',1),('party','party_id',1),('flags2','flags2',1),('ap','ap_bits',4),('target','target_player',1),('objective','objective',1),('owner','player_id',1)]:
        if name in m:b[m[name]:m[name]+width]=v[key].to_bytes(width,'little')
    return bytes(b)

def scaled_base_bits(base_word,level_byte,growth_s16):
    checked(base_word,16);checked(level_byte,8)
    if type(growth_s16) is not int or not -32768<=growth_s16<=32767:raise ValueError('Expected signed16 growth')
    p=base_word*growth_s16;q=abs(p)//100*(-1 if p<0 else 1)
    return (base_word if level_byte==1 else q)&0xFFFFFFFF

def gold_add_cap(initial,delta_bits,capacity_bits):
    for n in [initial,delta_bits,capacity_bits]:checked(n,32)
    return min((initial+delta_bits)&0xFFFFFFFF,capacity_bits)

def modern_editor_projection(raw,major=3,minor=50):
    v=record_view(raw);checked(major,32);checked(minor,32)
    if major==1:raise ValueError('Legacy major1 conversion has a separate floating-point path')
    out=dict(level=v['level'] or 1,gold=min(v['gold_percent_bits']&255,100),health=min(v['health_percent_bits']&255,100),flags=v['flags'],wander=v.get('wander_radius_byte',0),party=255)
    if v['tag']==199:
        out.update(flags=v['flags']&~6,flags2=v['flags2']&15,objective=v['objective'] or 19,ap=3 if not v['objective'] else v['ap_bits']&255,target=v['target_player'])
        if major<=2 and minor<50:out['flags']&=~16
    if v['tag']==200:out['ap']=v['ap_bits']
    return out

def runtime_flags(tag,flags,flags2=0,initial_flags=0,initial_extra=0,initial_f0=0):
    if tag not in SIZES:raise ValueError('Expected C6/C7/C8 tag')
    for n in [flags,flags2,initial_flags,initial_f0]:checked(n,8)
    checked(initial_extra,32)
    mask=255 if tag==199 else 0xF9
    out=dict(flags=(initial_flags&~mask)|(flags&mask),extra=initial_extra,f0=initial_f0)
    if tag==199:
        extra_mask=0xB80;bits=((flags2&1)<<7)|((flags2&2)<<7)|((flags2&8)<<6)|((flags2&4)<<9)
        out.update(extra=(initial_extra&~extra_mask)|bits,f0=int(bool(flags2&8)))
    return out

def patch_raw(raw,offset,value):
    v=record_view(raw);tag=v['tag'];widths={0:4,4:4,8:4,12:2,14:1,15:1}
    widths.update({198:{16:4,20:2,22:1,23:1},199:{16:4,20:4,24:2,26:1,27:1,28:1,29:1,30:1,31:1},200:{16:4,20:4,24:2,26:1,27:1}}[tag])
    if offset not in widths:raise ValueError('Unsupported creature field')
    n=widths[offset];checked(value,8*n);o=offset+8
    return bytes(raw[:o])+value.to_bytes(n,'little')+bytes(raw[o+n:])
