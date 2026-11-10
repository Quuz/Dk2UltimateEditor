"""CC Room overlays: raw TerrainId selector, native projections and patching."""
import struct
from native_placed_structures import checked,center_q12

def payload(raw):
    if not isinstance(raw,(bytes,bytearray)) or len(raw)<8:raise ValueError('Expected CC envelope')
    tag,n=struct.unpack_from('<II',raw)
    if tag!=204 or n>20 or len(raw)!=n+8:raise ValueError('Expected bounded CC payload0..20')
    return bytes(raw[8:]).ljust(20,b'\0')

def record_view(raw):
    x,y,z,flags,direction,opaque,health,terrain,owner=struct.unpack('<3IHBBHBB',payload(raw))
    return dict(payload_bytes=len(raw)-8,x=x,y=y,z_bits=z,opaque_flags=flags,direction=direction,opaque_byte=opaque,health=health,terrain_id=terrain,player_id=owner)

def intermediate(raw,seed=None):
    p=payload(raw)
    if seed is None:seed=bytes(44)
    if len(seed)!=44:raise ValueError('Expected44 intermediate bytes')
    b=bytearray(seed)
    for o in range(0,24,4):struct.pack_into('<I',b,o,1)
    b[24:36]=p[:12];struct.pack_into('<H',b,36,struct.unpack_from('<H',b,36)[0]|struct.unpack_from('<H',p,12)[0]);b[38]=p[14];b[40:44]=p[16:20]
    return bytes(b)

def signed32(value):
    value&=0xFFFFFFFF;return value if value<0x80000000 else value-0x100000000

def portal_delta(current_health,percent):
    checked(current_health,32);checked(percent,16)
    product=signed32(current_health*((100-percent)&0xFFFFFFFF));hi=(product*-1374389535)>>32;q=hi>>5
    return (q+int(q<0))&0xFFFFFFFF

def game_projection(raw,current_health=10000,previous_orientation=0xA5A5A5A5,valid_position=True,room_id=1):
    v=record_view(raw);checked(current_health,32);checked(previous_orientation,32);checked(room_id,12)
    out=dict(position=None,position_checks=0,tile_lookups=0,checksum_calls=0,orientation=previous_orientation,refresh_calls=0,heart_health=None,portal_delta=None)
    if v['player_id']==0:return out
    out['position']=[center_q12(v['x']),center_q12(v['y']),0];out['position_checks']=1
    if not valid_position:return out
    out['tile_lookups']=1;out['checksum_calls']=1
    if not room_id:return out
    if v['terrain_id']==37:
        if v['direction'] in [0,2,4,6]:out['orientation']=v['direction']//2
        out['refresh_calls']=1
    elif v['terrain_id']==14:out['heart_health']=v['health']
    elif v['terrain_id']==12 and v['health']!=100:out['portal_delta']=portal_delta(current_health,v['health'])
    return out

def editor_kind(terrain_id):
    checked(terrain_id,8)
    return 'portal' if terrain_id in [12,40] else 'heart' if terrain_id==14 else 'gate' if terrain_id in [33,37] else None

def editor_projection(raw,major=3,tile_terrain=None,health_present=True):
    v=record_view(raw);checked(major,32)
    if tile_terrain is None:tile_terrain=v['terrain_id']
    checked(tile_terrain,8)
    if tile_terrain!=v['terrain_id']:return None
    kind=editor_kind(v['terrain_id'])
    if kind is None:return None
    out=dict(kind=kind,x=v['x'],y=v['y'],terrain_id=v['terrain_id'],player_id=v['player_id'])
    if kind=='heart':out['health']=v['health'] if health_present else None
    elif kind=='portal':out['health']=100 if major==1 or not health_present else v['health']&255;out['direction']=0
    else:out.update(health=100,direction=v['direction'],player_id=1)
    return out

def writer_heart(player_id,x,y,health,opaque_byte):
    checked(player_id,8);checked(x,32);checked(y,32);checked(health,16);checked(opaque_byte,8)
    return struct.pack('<3IHBBHBB',x,y,0,0,0,opaque_byte,health,14,player_id)

def writer_portal(raw_payload):
    if len(raw_payload)!=20:raise ValueError('Expected20-byte model record')
    b=bytearray(raw_payload)
    if struct.unpack_from('<H',b,16)[0]==0:struct.pack_into('<H',b,16,100)
    return bytes(b)

FIELD_WIDTHS={0:4,4:4,8:4,12:2,14:1,15:1,16:2,18:1,19:1}
def patch_raw(raw,offset,value):
    record_view(raw)
    if offset not in FIELD_WIDTHS:raise ValueError('Expected complete field offset')
    n=FIELD_WIDTHS[offset]
    if offset+n>len(raw)-8:raise ValueError('Field not physically present')
    checked(value,n*8);o=8+offset
    return bytes(raw[:o])+value.to_bytes(n,'little')+bytes(raw[o+n:])
