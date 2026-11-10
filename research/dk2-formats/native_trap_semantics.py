"""Read-only native trap projections; no implicit editor flag cleanup."""
from pathlib import Path
import json
import dk2_formats as fmt

SCHEMA=json.loads((Path(__file__).resolve().parent/'trap-semantics.json').read_text(encoding='utf8'))
U32=0xFFFFFFFF

def record_check(record):
    if not isinstance(record,(bytes,bytearray)) or len(record)!=579:
        raise ValueError('Expected one579-byte Traps record')

def trap_editor_flags_projection(record):
    record_check(record);value=fmt.u32(record,0x1EC);mask=SCHEMA['editor_flags']['supported_mask']
    return dict(raw=value,native_editor_serialized=value&mask,dropped_bits=value&~mask,
        would_change=(value&mask)!=value,
        fields=[dict(mask=f['mask'],baseline_name=f['baseline_name'],
            native_checkbox=f['checkbox']['label'] if f['checkbox'] else None,
            set=bool(value&f['mask'])) for f in SCHEMA['editor_flags']['flags']])

def trap_charge_projection(record,ticks_per_second):
    record_check(record)
    if type(ticks_per_second) is not int or not 1<=ticks_per_second<=U32:
        raise ValueError('Pass the actual positive DWORD ticks-per-second value')
    raw=fmt.u32(record,0x1CC);product=(raw*ticks_per_second)&U32;threshold=product>>12
    return dict(raw_charge_time=raw,ticks_per_second=ticks_per_second,wrapped_product=product,
        multiplication_wrapped=raw*ticks_per_second>U32,threshold=threshold,
        reachable_by_byte_counter=threshold<=255,
        earliest_positive_polls=max(1,threshold) if threshold<=255 else None,
        scope='Threshold gate only, from initial counter0 with repeated positive callbacks; not full firing/lifecycle simulation.')

def advance_trap_charge_counter(counter,condition_met,threshold):
    if type(counter) is not int or not 0<=counter<=255:raise ValueError('Counter must be a byte')
    if type(condition_met) is not bool:raise ValueError('Callback result must be boolean')
    if type(threshold) is not int or not 0<=threshold<=U32:raise ValueError('Threshold must be DWORD')
    next_counter=(counter+1)&255 if condition_met else 0
    return dict(counter=next_counter,threshold_reached=condition_met and next_counter>=threshold)

def trap_los_range_projection(record,*,tracking=False):
    record_check(record)
    if type(tracking) is not bool:raise ValueError('Tracking switch must be boolean')
    raw=fmt.u32(record,0x1DC);radius=(raw+(2 if tracking else 0))&U32
    unwrapped=radius*radius*4096;threshold=unwrapped&U32
    return dict(trigger_type=record[0x20B],trigger_data_raw=raw,tracking=tracking,radius_operand=radius,
        squared_threshold_fixed12=threshold,multiplication_wrapped=unwrapped>U32,
        fresh_candidate_comparison='distance < threshold',cached_previous_tick_comparison='distance <= threshold',
        scope='Radius arithmetic in0x4A9400 only; cached target, visibility, ownership and immunity predicates are not simulated.')
