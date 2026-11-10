"""DK2 native KWD/KLD research reader. No third-party Python packages required.

Raw records, headers, unknown bits and trailing bytes are authoritative. Decoded
names use the accompanying evidence schemas; they are not a claim that every
gameplay meaning has been independently verified. Never writes game inputs.
"""
from __future__ import annotations
import argparse
from collections import Counter
from dataclasses import dataclass
import hashlib
import json
from pathlib import Path
import struct
from decimal import Decimal, ROUND_HALF_EVEN
from native_door_field_layout import effective_door_layout
from native_trap_field_layout import effective_trap_layout

ROOT = Path(__file__).resolve().parent
LAYOUTS = json.loads((ROOT/'layouts.json').read_text(encoding='utf-8'))
# Keep LAYOUTS as the historical source view. Current public decode/edit paths
# use native-confirmed Door WORD splitting and the single Trap WORD204 view.
CURRENT_DOOR_LAYOUT = effective_door_layout()
CURRENT_TRAP_LAYOUT = effective_trap_layout()

def catalogue_layout(name):
    """Current native width corrections, preserving LAYOUTS as provenance."""
    return {'Doors':CURRENT_DOOR_LAYOUT,'Traps':CURRENT_TRAP_LAYOUT}.get(name) or LAYOUTS.get(name)
THINGS = json.loads((ROOT/'thing-layouts.json').read_text(encoding='utf-8'))
ENUMS = json.loads((ROOT/'enums.json').read_text(encoding='utf-8'))
LEVEL_LAYOUTS = json.loads((ROOT/'level-layouts.json').read_text(encoding='utf-8'))
ENGINE = json.loads((ROOT/'verified-overrides.json').read_text(encoding='utf-8'))
VARIABLE_LAYOUTS = json.loads((ROOT/'variable-layouts.json').read_text(encoding='utf-8'))
VARIABLE_RUNTIME = json.loads((ROOT/'variable-runtime-map.json').read_text(encoding='utf-8'))
SHOT_SEMANTICS = json.loads((ROOT/'shot-semantics.json').read_text(encoding='utf-8'))
TRIGGERS = json.loads((ROOT/'trigger-layouts.json').read_text(encoding='utf-8'))
CREATURE_SUBRECORDS = json.loads((ROOT/'creature-subrecords.json').read_text(encoding='utf-8'))
CATALOGUE_CONSTRAINTS = json.loads((ROOT/'catalogue-loader-constraints.json').read_text(encoding='utf-8'))
EFFECT_SEMANTICS = json.loads((ROOT/'effect-semantics.json').read_text(encoding='utf-8'))
SPELL_SEMANTICS = json.loads((ROOT/'spell-semantics.json').read_text(encoding='utf-8'))
FAMILIES={0x6E:'Terrain',0x78:'Rooms',0x82:'Traps',0x8C:'Doors',0x96:'KeeperSpells',
          0xA0:'CreatureSpells',0xAA:'Creatures',0xF0:'Objects',0xFA:'EffectElements',0x104:'Shots',0x10E:'Effects'}
NAMES={**FAMILIES,0x64:'Map',0xB4:'Players',0xBE:'Things',0xD2:'Triggers',0xDC:'Level',0xE6:'Variables'}

def u32(b,o=0):return struct.unpack_from('<I',b,o)[0]
def u16(b,o=0):return struct.unpack_from('<H',b,o)[0]
def string(b):
    # Some shipped name buffers contain undefined ANSI bytes / stack residue.
    # This display view must never prevent preservation of the raw record.
    return b.split(b'\0',1)[0].decode('cp1252',errors='backslashreplace')

def enum_value(name,value,flags=False):
    entries=ENUMS.get(name,{})
    if not flags:return {'raw':value,'name':entries.get(str(value))}
    mask=0;labels=[]
    for number,label in entries.items():
        n=int(number)
        if n and value&n==n:labels.append(label);mask|=n
    return {'raw':value,'names':labels,'unmapped_bits':f'0x{value&~mask:X}'}

def art(b):
    assert len(b)==84
    flags=u32(b,64);kind=b[80]
    info={'name':string(b[:64]),'flags':enum_value('ArtResource.ArtResourceFlag',flags,True),
          'type':enum_value('ArtResource.ArtResourceType',kind),'union_hex':b[68:80].hex(),
          'type_attributes_hex':b[81:83].hex(),'sometimes_one':b[83]}
    if kind in (1,2,3):
        info['union']={'width':struct.unpack_from('<i',b,68)[0]/4096,'height':struct.unpack_from('<i',b,72)[0]/4096,'frames':u32(b,76)}
    elif kind==5:info['union']={'scale':struct.unpack_from('<i',b,68)[0]/4096,'frames':u32(b,72),'unknown':u32(b,76)}
    elif kind==6:info['union']={'frames':u32(b,68),'fps':u32(b,72),'start_distance':u16(b,76),'end_distance':u16(b,78),'start_animation':b[81],'end_animation':b[82]}
    return info

def decode_field(b,f):
    b=b[f['offset']:f['offset']+f['size']];k=f['kind']
    assert len(b)==f['size']
    if k=='string':return string(b)
    if k=='utf16le':return b.decode('utf-16-le',errors='surrogatepass').split('\0',1)[0]
    if k=='bytes':return {'hex':b.hex()}
    if k=='ArtResource':return art(b)
    if k=='RGB8':return list(b)
    if k=='StringId':return {'ids':list(struct.unpack_from('<5I',b)),'unknown':b[20:].hex()}
    if k=='Light':
        return {'position':[v/4096 for v in struct.unpack_from('<3i',b)],'radius':struct.unpack_from('<i',b,12)[0]/4096,
                'flags':enum_value('Light.LightFlag',u32(b,16),True),'rgba':list(b[20:24])}
    if k.startswith('JobPreference['):
        return [dict(job=enum_value('Creature.JobType',u32(b,i)),mood_change=u16(b,i+4),mana_change=u16(b,i+6),chance=b[i+8],unknown_hex=b[i+9:i+12].hex()) for i in range(0,len(b),12)]
    if k=='float32':return struct.unpack('<f',b)[0]
    signed=k.startswith('i')
    v=int.from_bytes(b,'little',signed=signed)
    if k.startswith('enum'):return enum_value(f['enum'],v)
    if k.startswith('flags'):return enum_value(f['enum'],v,True)
    if '/' in k:return {'raw':v,'value':v/int(k.split('/')[1])}
    return v

def decode_fields(b,fields):
    result={}
    for f in fields:
        if f['name'] in result:raise ValueError('Duplicate schema field '+f['name'])
        result[f['name']]=decode_field(b,f)
    return result

def decode_engine_fields(record):
    """Supplementary raw views with explicit evidence; baseline remains available."""
    result={f['name']:dict(raw=decode_field(record,f),offset=f['offset_hex'],
            confidence=f['confidence'],note=f['note'])
            for f in ENGINE['creature_fields'] if f['offset']+f['size']<=len(record)}
    for a in ENGINE.get('creature_arrays',[]):
        if a['offset']+a['size']>len(record):continue
        result[a['name']]=dict(offset=hex(a['offset']),note=a['note'],
            items=[dict(animation_slot=i,**decode_fields(record[a['offset']+i*a['element_size']:a['offset']+(i+1)*a['element_size']],a['element_fields'])) for i in range(a['count'])])
        if a['name']=='FootstepFrames':
            for item,slot in zip(result[a['name']]['items'],CREATURE_SUBRECORDS['animation_slots']):
                item.update(resource_field=slot['baseline_field'],resource_raw_offset=slot['raw_resource_offset_hex'],
                    loader_supports_resource_slot=slot['runtime_loader_has_slot_path'])
    spells=CREATURE_SUBRECORDS['spell_array']
    result['CreatureSpellSlots']=dict(offset=hex(spells['raw_offset']),count=spells['count'],element_size=spells['element_size'],
        note=spells['warning'],items=[dict(slot=i,**decode_fields(record[spells['raw_offset']+i*spells['element_size']:
            spells['raw_offset']+(i+1)*spells['element_size']],spells['fields'])) for i in range(spells['count'])])
    return result

def set_footstep_frames(record,animation_slot,left_frame,right_frame):
    """Patch one pair; frame validity depends on the actual loaded animation."""
    if len(record) not in (5449,5537):raise ValueError('Unknown creature variant')
    if not isinstance(animation_slot,int) or not 0<=animation_slot<48:raise ValueError('Animation slot must be 0..47')
    raw=[]
    for value in (left_frame,right_frame):
        exact=Decimal(str(value));integer=int(exact)
        if integer!=exact:raise ValueError('Frame must be an integer')
        raw.append(integer.to_bytes(2,'little',signed=False))
    offset=0x1323+4*animation_slot
    return record[:offset]+b''.join(raw)+record[offset+4:]


def decode_effect_engine_fields(record, family):
    """Read native low-width storage; the complete file values stay untouched."""
    expected = {'Effects':246, 'EffectElements':182}
    if family not in expected or len(record)!=expected[family]:
        raise ValueError('Unknown effect record variant')
    narrowed = {}
    for field in EFFECT_SEMANTICS['narrowed_fields'][family]:
        value = u32(record, field['raw_offset'])
        entry = dict(raw_u32=value, effective_u16=value&65535,
            raw_offset=hex(field['raw_offset']), runtime_offset=hex(field['runtime_offset']),
            consumer_interpretation=field['consumer_interpretation'])
        if field['consumer_interpretation']=='signed16':
            entry['effective_i16']=struct.unpack_from('<h',record,field['raw_offset'])[0]
        narrowed[field['field']]=entry
    result = dict(NarrowedScalars=narrowed,
        note='Native runtime storage view; source fields and all raw bytes remain authoritative.')
    if family=='Effects':
        value=record[0xF2]
        dispatch=next((x for x in EFFECT_SEMANTICS['generation_dispatch']['entries'] if x['value']==value),None)
        ids=[u16(record,0xCA+2*i) for i in range(8)]
        last=max((i for i,v in enumerate(ids) if v),default=0)
        result['GenerationDispatch']=dict(raw=value,defined=dispatch is not None,
            callback=dispatch['callback'] if dispatch else None,
            baseline_name=dispatch['baseline_name'] if dispatch else None)
        result['GenerateIdSlots']=dict(ids=ids,last_nonzero_index=last,
            random_slot_count=last+1,
            note='Physical slots, including holes and duplicates; selection depends on generation mode.')
        if value==6:
            result['SprayGeneration']=dict(secondary_effect_id=u16(record,0xDA),
                secondary_effect_period_ticks=3,element_id_range_inclusive=[ids[0],ids[last]],
                note='Raw+DA is SecondaryEffectId in this handler, despite the baseline name OuterOriginRange.')
        if value==14:
            result['DigitGeneration']=dict(base_element_id=ids[0],
                digit_element_ids=[(ids[0]+digit)&65535 for digit in range(10)],
                note='Type14 uses base+digit, including digits8 and9 beyond the eight GenerateIds slots. ElementsPerTurn does not gate this handler.')
        if value==18:
            result['FourSlotGeneration']=dict(effect_ids=ids[:4],batches=record[0xF3]//4,
                requires_flag=64,generation_enabled=bool(u32(record,0xB4)&64),
                note='Type18 visits slots3..0 per batch; slots4..7 are not generated by this handler.')
        if value==4:
            def signed16(offset):return struct.unpack_from('<h',record,offset)[0]
            effects=bool(u32(record,0xB4)&0x40)
            whirl=signed16(0xE4)
            if whirl>=128:whirl-=256
            circular=signed16(0xE8)
            if not effects and circular>=128:circular-=256
            result['CubeGeneration']=dict(target_family='Effects' if effects else 'EffectElements',
                whirlpool_allowed_in_this_branch=not effects,
                whirlpool_converted_rate=whirl,
                circular_converted_rate=max(-1024,min(1023,circular)),
                randomness_signed=signed16(0xEC),
                outer_radius_position_units=signed16(0xDA)*23,
                inner_radius_position_units=signed16(0xEA)*23,
                lower_height_position_units=signed16(0xDC)*16,
                upper_height_position_units=signed16(0xDE)*16,
                note='Incoming tail values; zero may retain an earlier runtime value in definition updates. Load order is needed for a complete runtime projection.')
    return result


def decode_spell_engine_fields(record,family,simulation_tps=None):
    """Native selector/timer views; TPS must be supplied explicitly for projection."""
    expected={'CreatureSpells':266,'KeeperSpells':406}
    if family not in expected or len(record)!=expected[family]:
        raise ValueError('Unknown spell record variant')
    if simulation_tps is not None and (not isinstance(simulation_tps,int) or not 0<=simulation_tps<=0xFFFFFFFF):
        raise ValueError('TPS projection requires an unsigned32 integer')
    def selector(key,value):
        table=SPELL_SEMANTICS['dispatch_tables'][key]
        row=next((x for x in table['entries'] if x['value']==value),None)
        return dict(raw=value,defined=row is not None,callback=row['callback'] if row else None,
            baseline_name=row['baseline_name'] if row else None,
            out_of_range_behavior=table['out_of_range_behavior'],
            **({'expects_entity_target':row['expects_entity_target']} if row and 'expects_entity_target' in row else {}))
    def timer(offset,bits):
        raw=u16(record,offset) if bits==16 else u32(record,offset)
        result=dict(raw=raw,file_offset=hex(offset),multiply_bits=bits,logical_shift=12,
            simulation_tps=simulation_tps,
            note='Definition-load projection; no effective load order or live TPS is inferred.')
        if simulation_tps is not None:result['runtime_value']=((raw*simulation_tps)&((1<<bits)-1))>>12
        return result
    if family=='CreatureSpells':
        return dict(AlternativeEligibility=selector('creature_alternative',record[0xEE]),
            UntransferredAlternativeReferences=dict(AlternativeShotId=record[0xE8],AlternativeRoomId=record[0xE9],
                note='Converter reads these fields, inspected runtime loader does not transfer them.'),
            RechargeTimeLoader=timer(0xEA,32),UnparsedTailHex=record[0xEF:0x10A].hex())
    def shot_tuple(id_offset,data_offset):
        return dict(ShotId=record[id_offset],Data1RawU32=u32(record,data_offset),Data2RawU32=u32(record,data_offset+4))
    return dict(CastRule=selector('keeper_cast',record[0xEF]),TargetRule=selector('keeper_target',record[0xDA]),
        RechargeTimeLoader=timer(0xCC,32),ResearchTimeLoader=timer(0xD8,16),BonusResearchTimeLoader=timer(0x111,16),
        ShotTuples=dict(normal=shot_tuple(0xF0,0xD0),upgraded=shot_tuple(0x113,0x114),
            note='Player spell state+0D selects the tuple; Data1/Data2 interpretation depends on the selected Shot.'),
        ResearchCostSource=dict(note='Native completion compares accumulated points with Variables type17 ResearchPointsRaw. Do not substitute ResearchTimeLoader as the point cost.'),
        UnparsedXe0Hex=record[0xE0:0xE2].hex())

def set_engine_scalar(record,field_name,raw_value):
    """Patch an engine overlay field using a raw integer; never mirrors flags."""
    if len(record) not in (5449,5537):raise ValueError('Unknown creature variant')
    f=next(f for f in ENGINE['creature_fields'] if f['name']==field_name)
    if f['offset']+f['size']>len(record):raise ValueError('Field absent in this variant')
    value=Decimal(str(raw_value));integer=int(value)
    if value!=integer:raise ValueError('Raw integer required')
    encoded=integer.to_bytes(f['size'],'little',signed=False)
    patched=bytearray(record);patched[f['offset']:f['offset']+f['size']]=encoded
    return bytes(patched)

def set_scalar(record:bytes,layout_name:str,field_name:str,value)->bytes:
    """Patch one scalar without recreating or zeroing any other bytes.

    For fixed point fields pass the displayed value; raw integer flag values
    remain raw. A flag UI must toggle only its selected bit, preserving others.
    """
    layout=catalogue_layout(layout_name) or LEVEL_LAYOUTS[layout_name]
    if len(record)!=layout['record_size']:raise ValueError('Record/schema size mismatch')
    f=next(f for f in layout['fields'] if f['name']==field_name)
    k=f['kind']
    if not k.startswith(('u','i','enum','flags')) or k in ('string','bytes'):
        raise ValueError('Only scalar integer/fixed-point fields can be patched')
    if '/' in k:
        raw=int((Decimal(str(value))*int(k.split('/')[1])).to_integral_value(rounding=ROUND_HALF_EVEN))
    else:
        exact=Decimal(str(value));raw=int(exact)
        if raw!=exact:raise ValueError('An integer field requires an integer value')
    encoded=raw.to_bytes(f['size'],'little',signed=k.startswith('i'))
    result=bytearray(record);result[f['offset']:f['offset']+f['size']]=encoded
    return bytes(result)

def rebuild_decoded(document):
    """Reassemble an exported dump from raw headers/records, preserving bytes.

    The decoded `fields` dictionaries are explanatory. To edit a typed scalar,
    use set_scalar, replace its raw_hex, then rebuild. References/properties keep
    their original raw data. All size/count changes are made by Section helpers.
    """
    output=[]
    for item in document['sections']:
        sig=int(item['signature'],16);head=bytes.fromhex(item['header_hex'])
        if sig in FAMILIES or sig in (0xBE,0xE6,0xD2):
            original_count=u32(head,20)
            records=[bytes.fromhex(r['raw_hex']) for r in item['records']]
            suffix=bytes.fromhex(item['suffix_hex'])
            section=Section(0,sig,head,records,suffix,item.get('stride',16 if sig==0xE6 else None))
            # Recompute lengths/counts even after the JSON record list changes.
            old_size=u32(head,8)-len(head)
            delta=len(section.data)-old_size
            edited=bytearray(head)
            struct.pack_into('<I',edited,8,u32(head,8)+delta)
            struct.pack_into('<I',edited,20,len(records))
            struct.pack_into('<I',edited,len(head)-4,u32(head,len(head)-4)+delta)
            if sig==0xD2:edited=bytearray(trigger_header(head,records,old_size,len(section.suffix)))
            section.header=bytes(edited)
            if sig in FAMILIES and section.stride is not None and any(len(r)!=section.stride for r in records):raise ValueError('Catalogue stride mismatch')
            if sig==0xE6 and any(len(r)!=16 for r in records):raise ValueError('Variable stride mismatch')
            if sig==0xBE and any(len(r)<8 or u32(r,4)+8!=len(r) for r in records):raise ValueError('Invalid Things record')
            output.append(section.pack())
        elif sig==0x64:output.append(head+bytes.fromhex(item['cells_hex']))
        else:output.append(head+bytes.fromhex(item['raw_data_hex']))
    result=b''.join(output)
    parse(result) # Bounds/envelope checks on reassembled output.
    return result

def trigger_header(header,records,old_data_bytes,suffix_bytes=0):
    if any(len(r)<8 or u32(r,4)+8!=len(r) or u32(r) not in (0xD5,0xD6) for r in records):
        raise ValueError('Invalid trigger node envelope/type')
    head=bytearray(header);new_size=sum(map(len,records))+suffix_bytes
    delta=new_size-old_data_bytes;old_declared=u32(head,len(head)-4)
    if old_declared==u32(head,20)*16:
        declared=sum(u32(r,4) for r in records if u32(r)==0xD5)
    elif old_declared==old_data_bytes:declared=new_size
    else:declared=old_declared+delta # Preserve an unrecognized legacy size convention.
    struct.pack_into('<I',head,8,u32(head,8)+delta)
    struct.pack_into('<I',head,20,sum(u32(r)==0xD5 for r in records))
    struct.pack_into('<I',head,24,sum(u32(r)==0xD6 for r in records))
    struct.pack_into('<I',head,len(head)-4,declared)
    return bytes(head)

def timestamp(b):
    return dict(year=u16(b),day=b[2],month=b[3],hour=b[6],minute=b[7],second=b[8],padding_hex=(b[4:6]+b[9:10]).hex())

@dataclass
class Section:
    offset:int
    signature:int
    header:bytes
    records:list[bytes]
    suffix:bytes=b''
    stride:int|None=None

    @property
    def data(self):return b''.join(self.records)+self.suffix
    def pack(self):return self.header+self.data

    def replace_records(self,records:list[bytes]):
        """For catalogues/Things/Variables only. Update this section's sizes.

        Index/ID allocation and referential integrity belong to the editor.
        Unknown records and suffix bytes are retained, not normalized away.
        """
        if self.signature==0xD2:
            self.header=trigger_header(self.header,records,len(self.data),len(self.suffix))
            self.records=list(records)
            return
        if self.signature not in FAMILIES and self.signature not in (0xBE,0xE6):
            raise ValueError('Only catalogue, Things and Variables replacement is implemented')
        if self.stride is not None and any(len(r)!=self.stride for r in records):raise ValueError('Record stride mismatch')
        if self.signature==0xBE and any(len(r)<8 or u32(r,4)+8!=len(r) for r in records):raise ValueError('Invalid Things record envelope')
        delta=sum(map(len,records))-sum(map(len,self.records))
        head=bytearray(self.header)
        struct.pack_into('<I',head,8,u32(head,8)+delta)
        struct.pack_into('<I',head,20,len(records))
        struct.pack_into('<I',head,len(head)-4,u32(head,len(head)-4)+delta)
        self.header=bytes(head);self.records=list(records)

def parse(data:bytes):
    if not data:raise ValueError('Empty KWD/KLD input')
    sections=[];p=0
    while p<len(data):
        if len(data)-p<20:raise ValueError(f'Truncated generic header at {p:#x}')
        sig,width,stored,tag,info=struct.unpack_from('<5I',data,p)
        if width!=4:raise ValueError(f'Unimplemented size-field width {width}; measured inputs all use 4')
        if tag!=sig+1:raise ValueError(f'Unexpected info marker at {p:#x}')
        if sig==0xD2 and info<32:raise ValueError('Incomplete trigger metadata')
        if sig in FAMILIES or sig in (0xBE,0xE6,0xDC):
            if info<28:raise ValueError('Incomplete catalogue/level metadata')
        elif sig==0x64 and info<8:raise ValueError('Incomplete map dimensions')
        dh=p+20+info;start=dh+8
        if start>len(data) or u32(data,dh)!=sig+2 and sig!=0xDC:raise ValueError('Invalid data header')
        size=u32(data,dh+4);end=start+size
        if sig==0xD2:
            # Original callback path ignores D and reads exactly both node counts.
            end=start
            for _ in range(u32(data,p+20)+u32(data,p+24)):
                if end+8>len(data):raise ValueError('Truncated trigger node header')
                if u32(data,end) not in (0xD5,0xD6):raise ValueError('Unknown trigger node tag')
                end+=8+u32(data,end+4)
                if end>len(data):raise ValueError('Truncated trigger node payload')
        elif sig==0xBE:
            # 0x56FA00 loops over N tagged records, independently of outer D.
            # Retain bytes inside the physical section after those records.
            physical_end=p+stored
            q=start
            for _ in range(u32(data,p+20)):
                if q+8>min(physical_end,len(data)):raise ValueError('Truncated Things node header')
                q+=8+u32(data,q+4)
                if q>min(physical_end,len(data)):raise ValueError('Truncated Things node payload')
            end=physical_end
        elif sig==0xE6:
            physical_end=p+stored
            # Count and complete-section length can be correct while D is stale.
            if physical_end<=len(data) and physical_end>=start+u32(data,p+20)*16:
                end=physical_end
        if end>len(data):raise ValueError('Data block extends past EOF')
        if sig==0xDC:
            if u32(data,dh)!=0xDF:raise ValueError('Invalid level properties tag')
            if end+8>len(data):raise ValueError('Truncated level references header')
            if u32(data,end)!=0xDE:raise ValueError('Invalid level references tag')
            end+=8+u32(data,end+4)
            if end>len(data):raise ValueError('Level references extend past EOF')
            if 72*u16(data,p+20)+4*u16(data,p+22)>end-start-size-8:
                raise ValueError('Truncated level reference/creature list')
        if stored!=end-p:raise ValueError(f'Section size disagrees with block lengths at {p:#x}')
        head=data[p:start];payload=data[start:end];records=[];suffix=b'';stride=None
        if sig in FAMILIES:
            count=u32(head,20)
            if count==0:
                if size:raise ValueError('Nonempty catalogue with zero count')
                stride=LAYOUTS[FAMILIES[sig]]['record_size']
            else:
                stride,rem=divmod(size,count)
                if rem:raise ValueError('Non-integral record size')
            records=[payload[i*stride:(i+1)*stride] for i in range(count)]
        elif sig==0xE6:
            count=u32(head,20);stride=16
            if count*16>len(payload):raise ValueError('Truncated variable records')
            records=[payload[i*16:(i+1)*16] for i in range(count)];suffix=payload[count*16:]
        elif sig in (0xBE,0xD2):
            q=0
            count=u32(head,20)+(u32(head,24) if sig==0xD2 else 0)
            for _ in range(count):
                if q+8>len(payload):raise ValueError('Truncated tagged record')
                length=u32(payload,q+4)+8
                if q+length>len(payload):raise ValueError('Truncated tagged payload')
                records.append(payload[q:q+length]);q+=length
            suffix=payload[q:]
        else:
            if sig==0x64 and size!=u32(head,20)*u32(head,24)*4:
                raise ValueError('Map dimensions and cell data disagree')
            records=[payload]
        sections.append(Section(p,sig,head,records,suffix,stride));p=end
    return sections

def decoded(section):
    s=section;sig=s.signature;head=s.header
    result=dict(offset=s.offset,signature=f'0x{sig:X}',family=NAMES.get(sig,'Unknown'),
                stored_size=u32(head,8),physical_size=len(s.pack()),data_offset=s.offset+len(head),
                data_size=u32(head,len(head)-4),header_hex=head.hex(),records=[],suffix_hex=s.suffix.hex())
    if sig==0xD2:
        result.update(condition_count=u32(head,20),action_count=u32(head,24),
                      count=u32(head,20)+u32(head,24),created=timestamp(head[32:42]),modified=timestamp(head[42:52]))
    elif sig!=0x64:
        result.update(count=u32(head,20),created=timestamp(head[28:38]),modified=timestamp(head[38:48]))
    if sig in FAMILIES:
        family=FAMILIES[sig];layout_name=family+('_5537' if sig==0xAA and s.stride==5537 else '')
        layout=catalogue_layout(layout_name)
        result['stride']=s.stride
        for index,rec in enumerate(s.records):
            fields=decode_fields(rec,layout['fields']) if layout and layout['record_size']==len(rec) else {}
            result['records'].append(dict(index=index,offset=s.offset+len(head)+index*s.stride,raw_hex=rec.hex(),fields=fields))
            if sig==0xAA and len(rec) in (5449,5537):
                result['records'][-1]['engine_fields']=decode_engine_fields(rec)
            if sig in (0x10E,0xFA) and layout and len(rec)==layout['record_size']:
                result['records'][-1]['engine_fields']=decode_effect_engine_fields(rec,family)
            if sig in (0x96,0xA0) and layout and len(rec)==layout['record_size']:
                result['records'][-1]['engine_fields']=decode_spell_engine_fields(rec,family)
            if sig==0x104 and fields:
                process=fields['ProcessType']['raw'];damage=fields['DamageType']['raw']
                p=next((p for p in SHOT_SEMANTICS['processes'] if p['process_type']==process),{})
                d=SHOT_SEMANTICS['damage_types'].get(str(damage),{})
                result['records'][-1]['parameter_interpretation']=dict(
                    process_type=process,process_name=p.get('name'),process_data1=p.get('data1'),process_data2=p.get('data2'),
                    damage_type=damage,damage_name=d.get('name'),damage_data1=d.get('data1'),damage_data2=d.get('data2'),
                    note='Both dispatch systems apply; meanings refer to runtime instance parameters unless noted')
    elif sig==0x64:
        b=s.data;w=u32(head,20);h=u32(head,24)
        if len(b)!=w*h*4:raise ValueError('Map dimensions and cell data disagree')
        result.update(width=w,height=h,cell_size=4,cells_hex=b.hex(),
                      terrain_counts=dict(Counter(b[0::4])),owner_counts=dict(Counter(b[1::4])),
                      bridge_values=sorted(set(b[2::4])),fourth_byte_values=sorted(set(b[3::4])))
    elif sig==0xD2:
        result['actual_data_size']=len(s.data)
        result['declared_size_convention']=('condition_payloads_only' if result['data_size']==u32(head,20)*16 else
                                           'all_node_bytes' if result['data_size']==len(s.data) else 'other_legacy_value')
        offset=s.offset+len(head)
        for i,b in enumerate(s.records):
            fields={}
            if len(b)==24:
                payload=b[8:]
                fields=dict(parameters_hex=payload[:8].hex(),Id=u16(payload,8),NextId=u16(payload,10),ChildId=u16(payload,12),
                    Type=payload[14],RepeatTimes=payload[15],
                    TypeName=ENUMS.get('TriggerGeneric.TargetType' if u32(b)==0xD5 else 'TriggerAction.ActionType',{}).get(str(payload[14])))
                layout=TRIGGERS.get(f'0x{u32(b):X}',{}).get(str(payload[14]))
                if layout:
                    fields['parameters']=decode_fields(payload[:8],layout['fields'])
                    fields['parameter_schema_confidence']=layout['confidence']
            result['records'].append(dict(index=i,offset=offset,tag=hex(u32(b)),payload_size=u32(b,4),raw_hex=b.hex(),fields=fields));offset+=len(b)
    elif sig==0xBE:
        result['type_counts']=dict(Counter(f'0x{u32(b):X}' for b in s.records))
        offset=s.offset+len(head)
        for i,b in enumerate(s.records):
            layout=THINGS.get(f'0x{u32(b):X}')
            fields=decode_fields(b[8:],layout['fields']) if layout and layout['payload_size']==len(b)-8 else {}
            result['records'].append(dict(index=i,offset=offset,tag=f'0x{u32(b):X}',payload_size=u32(b,4),raw_hex=b.hex(),fields=fields));offset+=len(b)
    elif sig==0xE6:
        result['type_counts']=dict(Counter(f'0x{u32(b):X}' for b in s.records))
        for i,b in enumerate(s.records):
            record=dict(index=i,offset=s.offset+len(head)+i*16,words_u32=list(struct.unpack('<4I',b)),words_i32=list(struct.unpack('<4i',b)),raw_hex=b.hex())
            variant=VARIABLE_LAYOUTS.get(str(u32(b)),VARIABLE_LAYOUTS['generic'])
            record['variant']=variant['name']
            record['fields']=decode_fields(b,variant['fields'])
            runtime=VARIABLE_RUNTIME['rows'].get(str(u32(b)))
            if runtime:
                record['runtime_storage']=dict(address=runtime['runtime_address'],bytes=runtime['runtime_bytes'],
                    transform=runtime['transform'],note='Runtime storage differs from 32-bit file payload; signed use depends on consumer')
            if u32(b)==17:
                # Discord DK2 Hex Editing, nsdr2025 2026-03-25;
                # independently matches variables_struct.bt and this input's IDs.
                record['research_order_interpretation']=dict(category=u32(b,4),
                    category_name={1:'Room',2:'Door',3:'Trap',4:'KeeperSpell'}.get(u32(b,4)),
                    definition_id=u32(b,8),research_cost_raw=u32(b,12),
                    note='Order is file order; physical units/gameplay effect not tested here')
            if u32(b)==65:
                record['typed']=dict(kind='CreatureLevelStat',stat_id=u32(b,4),value_raw=u32(b,8),level=u32(b,12),
                    note='0x51DC40 stores low 16 bits of value; current file levels 2..10')
            elif u32(b)==224:
                record['typed']=dict(kind='FirstPersonStat',stat_id=u32(b,4),value_raw=u32(b,8),unused_word=u32(b,12),
                    note='0x51DC70 ignores the third payload DWORD; not a level dimension')
            elif u32(b)==66:
                record['typed']=dict(kind='Alliance',player1=u32(b,4),player2=u32(b,8),unused_word=u32(b,12),
                    note='0x51DC90 writes a symmetric alliance using low bytes of player IDs')
            elif u32(b)==77:
                record['typed']=dict(kind='ModifyCreatureStat',creature_id=u32(b,4),stat_id=u32(b,8),runtime_value_raw=u32(b,12))
            result['records'].append(record)
    elif sig==0xDC:
        b=s.data;params=u32(head,len(head)-4);count=u16(head,20);unknown_count=u16(head,22)
        result['reference_count']=count;result['extra_word_count']=unknown_count;result['count']=count
        for name,off,size in [('name',0,128),('description',128,2048),('author',2176,128),('email',2304,128),('information',2432,2048)]:
            result[name]=b[off:off+size].decode('utf-16-le').split('\0')[0]
        result['properties_hex']=b[:params].hex()
        layout=LEVEL_LAYOUTS.get('Level_'+str(params))
        result['properties_fields']=decode_fields(b[:params],layout['fields']) if layout else {}
        result['reference_block_size']=u32(b,params+4)
        result['references']=[dict(type=f'0x{u32(b,params+8+i*72):X}',unknown=u32(b,params+12+i*72),path=string(b[params+16+i*72:params+80+i*72])) for i in range(count)]
        tail=params+8+72*count
        result['extra_words']=list(struct.unpack_from('<'+str(unknown_count)+'I',b,tail))
        result['creature_list']=[dict(raw_u32=v,creature_id_used_by_game=v&255) for v in result['extra_words']]
        result['after_reference_words_hex']=b[tail+4*unknown_count:].hex()
        result['raw_data_hex']=b.hex()
    else:result['raw_data_hex']=s.data.hex()
    return result

def compatibility_findings(document):
    """Version-specific diagnostics; never changes or rejects preserved input."""
    findings=[]
    def add(code,section,record_index,message):
        findings.append(dict(code=code,section_offset=section['offset'],record_index=record_index,message=message))
    for s in document['sections']:
        if s['signature'] in ('0xE6','0xBE') and s['data_size']!=s['physical_size']-len(bytes.fromhex(s['header_hex'])):
            add('stale_data_size',s,None,'D differs from full-section payload. Original record callback follows N records; raw header preserved')
        sig=int(s['signature'],16)
        if sig in FAMILIES and s.get('count')==0:
            constraint=CATALOGUE_CONSTRAINTS[hex(sig)]
            add('zero_creature_count' if sig==0xAA else 'zero_catalogue_count',s,None,
                f"{constraint['family']} parser {constraint['parser']} DIV at {constraint['zero_count_division']} precedes count guard; active record callback can divide by zero")
        if s['signature']=='0xAA':
            for r in s['records']:
                raw=bytes.fromhex(r['raw_hex'])
                if len(raw) not in (5449,5537):continue
                mass_class=(u32(raw,0xEA8)&0xFFFF)>>12
                if not 1<=mass_class<=5:
                    add('creature_mass_class_outside_table',s,r['index'],'Collision 0x4D2A60 indexes a 5x5 table by low16(MassRaw)>>12 without bounds clamp; supported classes1..5')
                if not u16(raw,0xCEC) and any(raw[o] for o in (0xF42,0xF43,0xF44,0xF45,0x1025,0x1026)):
                    add('zero_swipe_speed',s,r['index'],'Nonzero first-person swipe uses22000/SwipeSpeed in0x5075A0; zero reaches IDIV by zero at0x507615 when that swipe is activated')
        if s['signature']=='0x104':
            for r in s['records']:
                f=r['fields']
                if not f:continue
                if f['TimedEffectId'] and f['TimedDelay']==0:
                    add('zero_timed_effect_delay',s,r['index'],'Nonzero TimedEffectId with TimedDelay=0 reaches DIV by zero in original shot tick 0x4AB70C')
                if f['ProcessType']['raw']>39:
                    add('unverified_process_type',s,r['index'],'Process type is beyond original 0..39 table; verify runtime patches before using it')
        if sig==0x10E:
            for r in s['records']:
                raw=bytes.fromhex(r['raw_hex'])
                if len(raw)==246 and raw[0xF2]>=19:
                    add('effect_generation_type_outside_table',s,r['index'],
                        'GenerationType indexes a 19-entry table at0x697668 without a bounds check in0x4350EF; verify runtime patches before using values19..255')
        if sig in (0x96,0xA0):
            for r in s['records']:
                raw=bytes.fromhex(r['raw_hex'])
                if sig==0xA0 and len(raw)==266 and raw[0xEE]>=6:
                    add('creature_alternative_selector_rejected',s,r['index'],
                        'AlternativeShot>=6 is rejected with return0 by native eligibility dispatch0x49A7B0; bytes are preserved')
                if sig==0x96 and len(raw)==406:
                    for offset,limit,name in [(0xEF,6,'CastRule'),(0xDA,7,'TargetRule')]:
                        if raw[offset]>=limit:
                            add('keeper_spell_selector_rejected',s,r['index'],
                                f'{name}={raw[offset]} exceeds native selector table; dispatcher returns0 and rejects the operation')
        if s['signature']=='0xE6':
            for r in s['records']:
                tag,a,b,c=r['words_u32']
                if tag==19 and a==0:add('zero_tps',s,r['index'],'TPS zero is written directly and is later used as a divisor; later files may override it')
                if tag==65 and (a>=32 or not 2<=c<=10):
                    add('unverified_level_stat_index',s,r['index'],'Unchecked level-stat index outside measured stat 0..31 / level 2..10 table')
                if tag==224 and a>=32:
                    add('unverified_fp_stat_index',s,r['index'],'Unchecked first-person stat index outside measured 0..31 table')
    return findings

def inspect(path):
    b=Path(path).read_bytes();sections=parse(b)
    assert b==b''.join(s.pack() for s in sections)
    result=dict(source=str(Path(path).resolve()),size=len(b),sha256=hashlib.sha256(b).hexdigest(),sections=[decoded(s) for s in sections])
    result['compatibility_findings']=compatibility_findings(result)
    return result

def main():
    cli=argparse.ArgumentParser(description=__doc__)
    cli.add_argument('input',type=Path);cli.add_argument('--json',type=Path)
    args=cli.parse_args();result=inspect(args.input)
    if args.json:
        if args.json.resolve()==args.input.resolve():raise ValueError('Output must differ from source')
        args.json.parent.mkdir(exist_ok=True,parents=True)
        args.json.write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
    for s in result['sections']:
        print(f"{s['offset']:#x} {s['family']} count={s.get('count','-')} size={s['physical_size']} stride={s.get('stride','-')}")

if __name__=='__main__':main()
