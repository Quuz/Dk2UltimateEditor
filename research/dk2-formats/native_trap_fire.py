"""Exact32-bit trap shot origin and conditional post-creation projections."""
from pathlib import Path
import json,struct
import dk2_formats as fmt

SCHEMA=json.loads((Path(__file__).resolve().parent/'trap-fire-semantics.json').read_text(encoding='utf8'))
U32=0xFFFFFFFF

def signed32(value):
    value&=U32
    return value if value<0x80000000 else value-0x100000000

def record_check(record):
    if not isinstance(record,(bytes,bytearray)) or len(record)!=579:raise ValueError('Expected one579-byte Traps record')

def trap_shot_origin(record,position_raw,heading):
    """Position/offsets use4096 units/tile; heading must be within one native turn."""
    record_check(record)
    if type(heading) is not int or not 0<=heading<2048:raise ValueError('Heading must be0..2047; native code has no local bounds check')
    if not isinstance(position_raw,(list,tuple)) or len(position_raw)!=3 or any(type(v) is not int or not -0x80000000<=v<=0x7FFFFFFF for v in position_raw):
        raise ValueError('Position must contain three signed DWORD integers')
    offsets=list(struct.unpack_from('<iii',record,0x231));tables=SCHEMA['origin']['trig_views']
    sine=tables['sine']['values'][heading];cosine=tables['cosine']['values'][heading]
    products=[offsets[0]*cosine,offsets[1]*sine,offsets[1]*cosine,offsets[0]*sine]
    rotated=[signed32(products[0])>>16,signed32(products[1])>>16,signed32(products[2])>>16,signed32(products[3])>>16]
    sums=[position_raw[0]+rotated[0]-rotated[1],position_raw[1]+rotated[2]+rotated[3],position_raw[2]+offsets[2]]
    out=[signed32(v) for v in sums]
    return dict(offset_raw=offsets,position_raw=list(position_raw),heading=heading,sine=sine,cosine=cosine,
        origin_raw=out,origin_tiles=[v/4096 for v in out],
        product_wrap_indices=[i for i,v in enumerate(products) if not -0x80000000<=v<=0x7FFFFFFF],
        coordinate_wrap_indices=[i for i,v in enumerate(sums) if not -0x80000000<=v<=0x7FFFFFFF],
        shot_type_id=record[0x20D],creator_skips_zero_shot_type=record[0x20D]==0,
        scope='Arithmetic only; no collision, map height or successful shot creation simulated.')

def trap_post_creation_projection(record,ticks_per_second,current_tick,*,target_tag=0,instance_flags=0):
    """Assignments only after successful shot creation, never a firing predictor."""
    record_check(record)
    for name,value in [('ticks_per_second',ticks_per_second),('current_tick',current_tick),('target_tag',target_tag)]:
        if type(value) is not int or not 0<=value<=U32 or (name=='ticks_per_second' and value==0):raise ValueError(name+' must be an explicit DWORD;TPS positive')
    if type(instance_flags) is not int or not 0<=instance_flags<=255:raise ValueError('Instance flags must be a byte')
    recharge_raw=fmt.u32(record,0x1C8);duration_raw=fmt.u32(record,0x1D0)
    recharge_ticks=((recharge_raw*ticks_per_second)&U32)>>12
    duration_ticks=((duration_raw*ticks_per_second)&U32)>>12
    threat=fmt.u16(record,0x1EA) if duration_ticks else 0
    flags=fmt.u32(record,0x1EC);target=target_tag&65535;trigger=record[0x20B]
    return dict(conditional_on_successful_shot_creation=True,creator_skips_zero_shot_type=record[0x20D]==0,
        shot_type_id=record[0x20D],target_tag_low16=target,
        copied_shot_data1=fmt.u32(record,0x1E0),copied_shot_data2=fmt.u32(record,0x1E4),
        normal_caller_data2_override=target if record[0x20C]==16 and target else None,
        pressure_target_marker_write=1 if trigger not in (1,3) and target else None,
        reset_charge_counter=0,recharge_ticks=recharge_ticks,recharge_deadline=(current_tick+recharge_ticks)&U32,
        recharge_product_wrapped=recharge_raw*ticks_per_second>U32,
        threat_duration_ticks=duration_ticks,threat_duration_byte=duration_ticks&255,threat_value=threat,
        threat_duration_narrowed=duration_ticks>255,
        threat_emission_gate_in_inspected_tick=threat!=0,
        duration_zero_with_nonzero_threat=duration_ticks>0 and duration_ticks&255==0 and threat!=0,
        reveal_on_fire=bool(flags&2),projected_instance_flags=instance_flags&~0x10 if flags&2 else instance_flags,
        scope='Selected post-creation assignments; no target validity, actual shot success, mana accounting, full visibility or threat lifecycle modeled.')

def advance_trap_threat_fields(duration_byte,threat_value):
    """Only the explicit countdown write and emission gate in the inspected tick."""
    if type(duration_byte) is not int or not 0<=duration_byte<=255:raise ValueError('Duration must be a byte')
    if type(threat_value) is not int or not 0<=threat_value<=U32:raise ValueError('Threat must be a DWORD')
    return dict(duration_byte=max(0,duration_byte-1),threat_value=threat_value,emission_gate=threat_value!=0)
