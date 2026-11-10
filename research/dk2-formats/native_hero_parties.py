"""C9 raw-preserving records, bounded legacy projections and native editor fields.

Unknown names/reserved bytes stay raw. Loader projections never rewrite inputs.
These APIs do not emulate hero spawn, native allocation or full GUI model state.
"""
import struct
from native_placed_structures import checked
from native_placed_creatures import record_view as creature_view
SIZES={264,268,300,555}

def checked_record(raw):
    if not isinstance(raw,(bytes,bytearray)) or len(raw)<8:raise ValueError('Expected C9 envelope')
    tag,n=struct.unpack_from('<II',raw)
    if tag!=201 or n not in SIZES or len(raw)!=n+8:raise ValueError('Expected C9 payload264/268/300/555')
    return bytes(raw[8:])

def canonical_payload(raw,legacy_tail_cstring=None):
    p=checked_record(raw);n=len(p)
    if n==555:return p
    out=bytearray(555);out[32:35]=p[:3]
    start=8 if n==264 else 12;out[43:299]=p[start:start+256]
    if n!=264:
        name=p[268:300] if n==300 else legacy_tail_cstring
        if not isinstance(name,(bytes,bytearray)) or b'\0' not in name:raise ValueError('Legacy268 needs explicit outside-input CString;300 needs a bounded NUL-terminated name')
        name=bytes(name).split(b'\0',1)[0]
        if len(name)>31:raise ValueError('Only bounded legacy names<=31 supported; native copy is unbounded')
        out[:len(name)]=name
    return bytes(out)

def record_view(raw):
    p=checked_record(raw)
    if len(p)!=555:return dict(tag=201,payload_bytes=len(p),legacy=True,trigger_id=int.from_bytes(p[:2],'little'),party_id=p[2],member_capacity=8)
    return dict(tag=201,payload_bytes=555,legacy=False,name_raw_hex=p[:32].hex(),trigger_id=int.from_bytes(p[32:34],'little'),party_id=p[34],opaque_header_hex=p[35:43].hex(),
                member_capacity=16,occupied_slots=[i for i in range(16) if p[43+32*i+28]],
                members=[creature_view(struct.pack('<II',199,32)+p[43+32*i:75+32*i]) for i in range(16)])

def loader_intermediate(raw,legacy_tail_cstring=None):
    p=canonical_payload(raw,legacy_tail_cstring);out=bytearray(631)
    for i in range(19):struct.pack_into('<I',out,4*i,1)
    # strncpy(name,32) pads after first NUL; native loader then forces byte31=0.
    name=p[:32].split(b'\0',1)[0][:31];out[76:76+len(name)]=name
    out[108:111]=p[32:35];out[119:631]=p[43:555]
    return bytes(out)

def editor_member_fields(raw,major=3,legacy_tail_cstring=None):
    checked(major,32)
    if major==1:raise ValueError('Legacy absolute-to-percent model conversion is separate')
    p=canonical_payload(raw,legacy_tail_cstring);result=[]
    for i in range(16):
        slot=p[43+32*i:75+32*i]
        if not slot[28]:continue
        flags=slot[15]
        if flags&6==0:flags|=2 if not result else 4
        result.append(dict(source_slot=i,creature_id=slot[28],level=slot[14],gold=min(slot[12],100),health=min(slot[20],100),
            flags=flags,flags2=slot[31]&15,wander=slot[29],party_id=p[34],objective=slot[27],target_player=slot[26],ap=slot[16],trigger_id=int.from_bytes(slot[24:26],'little')))
    return result

def writer_overlay(seed_payload,party_id,trigger_id,serialized_members):
    if not isinstance(seed_payload,(bytes,bytearray)) or len(seed_payload)!=555:raise ValueError('Expected555-byte scratch seed')
    checked(party_id,8);checked(trigger_id,16)
    if len(serialized_members)!=16:raise ValueError('Exactly16 serializer results required')
    out=bytearray(seed_payload);struct.pack_into('<HB',out,32,trigger_id,party_id)
    for i,m in enumerate(serialized_members):
        if m is None:m=bytes(32)
        if not isinstance(m,(bytes,bytearray)) or len(m)!=32:raise ValueError('Each member is32 bytes orNone')
        out[43+32*i:75+32*i]=m
    return bytes(out)

def world_store_projection(initial_runtime_slot,raw,legacy_tail_cstring=None):
    if not isinstance(initial_runtime_slot,(bytes,bytearray)) or len(initial_runtime_slot)!=520:raise ValueError('Expected520-byte runtime slot seed')
    p=canonical_payload(raw,legacy_tail_cstring);out=bytearray(initial_runtime_slot);out[:3]=p[32:35];out[8:520]=p[43:555]
    return dict(party_id=p[34],world_offset=0x4030+520*p[34],slot=bytes(out),native_editor_slot_range=p[34]<24)

def patch_raw(raw,offset,value):
    p=checked_record(raw)
    if len(p)!=555:raise ValueError('Raw field patch helper currently targets555-byte records; preserve legacy envelopes separately')
    if offset not in [32,34,35,39]:raise ValueError('Expected trigger/party ID/opaque DWORD offset')
    n={32:2,34:1,35:4,39:4}[offset];checked(value,n*8);o=8+offset
    return bytes(raw[:o])+value.to_bytes(n,'little')+bytes(raw[o+n:])

def patch_member(raw,slot,member_offset,value):
    p=checked_record(raw)
    if len(p)!=555:raise ValueError('Expected555-byte C9 record')
    if type(slot) is not int or not 0<=slot<16:raise ValueError('Expected slot0..15')
    from native_placed_creatures import patch_raw as patch_creature
    start=43+32*slot;member=struct.pack('<II',199,32)+p[start:start+32];new=patch_creature(member,member_offset,value)[8:]
    return bytes(raw[:8+start])+new+bytes(raw[8+start+32:])
