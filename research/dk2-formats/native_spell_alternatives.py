"""Native CreatureSpells references, complete346-byte intermediate and loader.

AlternativeShotId E8 and AlternativeRoomId E9 are native-editor references;
this does not assign them an effective gameplay replacement rule.
"""
import struct
from native_spell_writer import check,intermediate as prefix320,patch_raw

def full_intermediate(raw,seed=0):
    """Direct converter projection; call with seed0 after native initializer."""
    return prefix320(raw,seed)+bytes([seed])*26

def references(raw):
    check(raw);return dict(alternative_shot_id=raw[232],alternative_room_id=raw[233],alternative_rule=raw[238])

def model_reference_projection(shot_id,room_id,presence28,presence2c,previous_shot=165,previous_room=165):
    for value in [shot_id,room_id,previous_shot,previous_room]:
        if type(value) is not int or not 0<=value<=255:raise ValueError('Expected byte IDs')
    # The model callback crosses presence checks, not the values or writer slots.
    return dict(alternative_shot_id=shot_id if presence2c else previous_shot,
                alternative_room_id=room_id if presence28 else previous_room)

def lookup(ids,value):
    if type(value) is not int or not 0<=value<=255:raise ValueError('Expected byte ID')
    if not isinstance(ids,(bytes,bytearray)):raise ValueError('Expected byte ID array')
    return ids.find(bytes([value]))

def checked_selection(ids,index):
    """Diagnostic safe API; native commit performs unchecked array[index]."""
    if type(index) is not int or not isinstance(ids,(bytes,bytearray)) or not 0<=index<len(ids):raise ValueError('Selection outside ID array; native commit would read outside it')
    return ids[index]

def runtime_projection(mid,previous=bytes(70),tps=20,checksum_gate=0):
    if len(mid)!=346 or len(previous)!=70:raise ValueError('Expected346-byte intermediate and70-byte definition')
    if type(tps) is not int or not 0<=tps<=0xFFFFFFFF:raise ValueError('Expected DWORD TPS')
    out=bytearray(previous)
    if checksum_gate==0:struct.pack_into('<I',out,27,sum(mid)&0xFFFFFFFF)
    for presence,dst,src,n in [(4,0x43,0x136,1),(0x24,0x2B,0x128,2),(0x40,0x31,0x120,4),(0x10,0x35,0x12E,2),(0x14,0x37,0x12C,2),(0x18,0x39,0x130,2),(0x1C,0x3B,0x132,2),(0x20,0x3D,0x134,2),(0x48,0x45,0x13E,1),(0x30,0x44,0x137,1),(0x34,0x1F,0x118,4),(0x38,0x23,0x11C,4),(0x4C,0x27,0x124,4)]:
        if struct.unpack_from('<I',mid,presence)[0]:out[dst:dst+n]=mid[src:src+n]
    if struct.unpack_from('<I',mid,0x44)[0]:struct.pack_into('<I',out,0x2D,((struct.unpack_from('<I',mid,0x13A)[0]*tps)&0xFFFFFFFF)>>12)
    if struct.unpack_from('<I',mid,0x3C)[0]:struct.pack_into('<I',out,0x3F,struct.unpack_from('<H',mid,0x12A)[0])
    return bytes(out)
