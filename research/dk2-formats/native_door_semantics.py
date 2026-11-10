"""Read-only projections of selected native door assignments, never a serializer."""
from pathlib import Path
import json
import dk2_formats as fmt

SCHEMA=json.loads((Path(__file__).resolve().parent/'door-semantics.json').read_text(encoding='utf8'))
U32=0xFFFFFFFF

def s32(v):
    v&=U32
    return v-(1<<32) if v&0x80000000 else v

def s16(v):
    v&=65535
    return v-65536 if v&32768 else v

def record_check(r,n=616):
    if not isinstance(r,(bytes,bytearray)) or len(r)!=n:raise ValueError(f'Expected one {n}-byte record')

def uint(v,name,max_value=U32):
    if type(v) is not int or not 0<=v<=max_value:raise ValueError(f'{name} must be an unsigned integer <= {max_value}')

def door_scalar_projection(record):
    record_check(record)
    return dict(door_id=record[0x244],manufacture_required_u16=fmt.u16(record,0x234),
        unresolved_236_u16=fmt.u16(record,0x236),health_max_u16=fmt.u16(record,0x22C),
        health_gain_s16=s16(fmt.u16(record,0x21C)),mana_cost_gui_s16=s16(fmt.u16(record,0x238)),
        untransferred_hex={hex(o):record[o:o+n].hex() for o,n in [(0x21E,2),(0x220,4),(0x230,2)]})

def door_editor_flags_projection(record):
    record_check(record);raw=fmt.u32(record,0x228);mask=SCHEMA['editor_flags']['supported_mask']
    return dict(raw=raw,native_editor_serialized=raw&mask,dropped_bits=raw&~mask,would_change=bool(raw&~mask))

def door_health_tick_projection(record,health,current_tick,next_health_tick,ticks_per_second,*,is_dead=False):
    record_check(record)
    if type(health) is not int or not -(1<<31)<=health<(1<<31):raise ValueError('Health must be signed DWORD')
    for v,name in [(current_tick,'tick'),(next_health_tick,'next tick'),(ticks_per_second,'TPS')]:uint(v,name)
    if ticks_per_second==0 or type(is_dead) is not bool:raise ValueError('Positive TPS and boolean death gate required')
    gain=s16(fmt.u16(record,0x21C));active=not is_dead and current_tick>next_health_tick and gain!=0
    delta=s16(min(s32(fmt.u16(record,0x22C)-health),gain)) if active else 0
    return dict(gate_passed=active,delta_to_apply=delta,health_helper_called=active and delta!=0,
        next_health_tick=(next_health_tick+ticks_per_second)&U32 if active else next_health_tick,
        scope='Local gate, helper argument and deadline only; not the full entity lifecycle.')

def door_shot_projection(door,trap,ticks_per_second,current_tick,position_raw,*,door_tag=0,target_tag=0,factory_succeeded=True):
    record_check(door);record_check(trap,579)
    for v,name in [(ticks_per_second,'TPS'),(current_tick,'tick'),(door_tag,'door tag'),(target_tag,'target tag')]:uint(v,name)
    if ticks_per_second==0 or type(factory_succeeded) is not bool:raise ValueError('Positive TPS and boolean factory result required')
    if not isinstance(position_raw,(tuple,list)) or len(position_raw)!=3 or any(type(v) is not int or not -(1<<31)<=v<(1<<31) for v in position_raw):
        raise ValueError('Position must contain three signed DWORD values')
    if door[0x227]==0:raise ValueError('Door has no linked trap type')
    if door[0x227]!=trap[0x20C]:raise ValueError('Trap record ID does not match door TrapTypeId')
    recharge=((fmt.u32(trap,0x1C8)*ticks_per_second)&U32)>>12
    return dict(trap_id=door[0x227],shot_type_id=trap[0x20D],
        initial_origin=[position_raw[0],position_raw[1],s32(position_raw[2]+2048)],
        shot_data1=fmt.u32(trap,0x1E0) if factory_succeeded else None,
        shot_data2=fmt.u32(trap,0x1E4) if factory_succeeded else None,
        source_tag=door_tag&65535 if factory_succeeded else None,target_tag=target_tag&65535 if factory_succeeded else None,
        mana_cost_s32=s32(fmt.u32(trap,0x1D4)),recharge_ticks=recharge,next_shot_tick=(current_tick+recharge)&U32,
        factory_succeeded=factory_succeeded,scope='Conditional on reaching the selected target, closed-door, mana-allowed attempt path. Deadline/mana accounting also run on factory failure.')
