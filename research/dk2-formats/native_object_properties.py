"""Read-only native Objects GUI and selected runtime projections."""
from pathlib import Path
import json
import dk2_formats as fmt

SCHEMA=json.loads((Path(__file__).resolve().parent/'object-property-semantics.json').read_text(encoding='utf8'))

def record_check(record):
    if not isinstance(record,(bytes,bytearray)) or len(record)!=894:raise ValueError('Expected one894-byte Objects record')

def native_object_property_views(record):
    record_check(record);result=[]
    for f in SCHEMA['fields']:
        raw=int.from_bytes(record[f['raw_offset']:f['raw_offset']+f['raw_bytes']],'little')
        value=raw/f['divisor'] if f['native_editor_kind']=='scaled_u32' else raw-65536 if f['native_editor_kind']=='s16' and raw>=32768 else raw
        result.append(dict(name=f['name'],raw=raw,native_editor_value=value,gui_value_in_range=f['gui_range'][0]<=value<=f['gui_range'][1],
            runtime_copy_bits=(raw&65535) if f['runtime_load_rule']=='copy low16' else None,
            meaning_status=f['meaning_status']))
    return result

def native_object_speed_projection(record,ticks_per_second):
    record_check(record)
    if type(ticks_per_second) is not int or not 1<=ticks_per_second<=0xFFFFFFFF:raise ValueError('Pass the positive unsigned DWORD TPS')
    raw=fmt.u32(record,0x338);quotient=raw//ticks_per_second;bits=quotient&65535
    clamped=bits==0 and raw!=0
    return dict(raw=raw,ticks_per_second=ticks_per_second,quotient=quotient,runtime_bits=1 if clamped else bits,
        quotient_narrowed=quotient>65535,zero_replaced_with_one=clamped)

def native_object_flags_projection(record):
    record_check(record);raw=fmt.u32(record,0x344);spec=SCHEMA['flags'];basic=raw&spec['supported_mask']
    choices=spec['object_type_radio']['choices'];selected=next((c for c in choices[1:] if raw&c['mask']),choices[0])
    panel=(basic&~spec['object_type_radio']['type_mask'])|selected['mask']
    return dict(raw=raw,native_decode_encode=basic,native_panel_accept=panel,unsupported_bits=raw&~spec['supported_mask'],
        selected_type=selected['label'],selected_type_index=selected['index'],
        type_bits_cleared=(raw&spec['object_type_radio']['type_mask'])&~selected['mask'],
        decode_encode_would_change=raw!=basic,panel_accept_would_change=raw!=panel)
