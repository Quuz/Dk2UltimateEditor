"""Read-only native floor resource requests from fully converted Rooms records."""
from pathlib import Path
import json
import dk2_formats as fmt

SCHEMA=json.loads((Path(__file__).resolve().parent/'room-resource-semantics.json').read_text(encoding='utf8'))
MODES={r['value']:r for r in SCHEMA['construction']}

def room_floor_resource_plan(record,*,update_mode=False,resource_service_present=True):
    """Project resource-manager requests, not load success or final map geometry.

    Names are formed from original ANSI bytes. The native converter is assumed
    to have supplied all presence flags. No input bytes are written.
    """
    if not isinstance(record,(bytes,bytearray)) or len(record)!=1055:
        raise ValueError('Expected one1055-byte Rooms record')
    if type(update_mode) is not bool or type(resource_service_present) is not bool:
        raise ValueError('Pass explicit boolean loading-mode/service switches')
    mode=record[0x395];rule=MODES.get(mode)
    art=record[0xC8:0x11C];name_buffer=art[:64]
    terminated=0 in name_buffer;base=name_buffer.split(b'\0',1)[0]
    flags=fmt.u32(art,64);kind=art[80]
    raw_flags=fmt.u32(record,0x370)
    effective_flags=(raw_flags&65535)|(8 if mode in SCHEMA['runtime_flags']['forced_for_construction'] else 0)
    result=dict(construction=mode,native_editor_label=rule['native_editor_label'] if rule else None,
        base_name=fmt.string(base),base_name_hex=base.hex(),base_resource_type=kind,
        runtime_flags_projection=effective_flags,requests=[],findings=[],
        scope='Floor/base/destroyed/Water resource requests only; excludes Start/End loads, common GUI/wall/cap/torch/ceiling loads and geometry placement.')
    if update_mode or not resource_service_present:
        result['skipped_by_outer_gate']=True
        return result
    result['skipped_by_outer_gate']=False
    if not terminated:
        result['findings'].append(dict(code='unterminated_resource_name',
            explanation='Native C-string access can escape the64-byte name buffer; request projection is withheld.'))
        return result
    def add(name,role,runtime,number=None,resource_type=kind,service_flags=flags):
        row=dict(name=fmt.string(name),name_hex=name.hex(),role=role,runtime_entry_offset=runtime,
            suffix=number,resource_type=resource_type,service_flags=service_flags)
        result['requests'].append(row)
        if role!='exact_base' and len(name)>=64:
            result['findings'].append(dict(code='generated_name_exceeds_local_buffer',name_hex=name.hex(),
                explanation='Generated name plus NUL exceeds the64-byte stack buffer used by the inspected sprintf path.'))
    if rule is None or mode==6:
        result['findings'].append(dict(code='no_floor_generation_branch',
            explanation='No floor-resource generation branch for this value in the inspected loader; not a verdict on all rendering support.'))
        return result
    # Mode1 exact resource has a type gate but no nonempty-name gate.
    if mode==1 and kind:
        add(base,'exact_base',0x1B)
    if mode==10 and base:
        add(base,'exact_base',0xC1)
    if base:
        prefix=base[:-1] if mode==1 else base
        for position,number in enumerate(rule['indexed_suffixes']):
            entry_index=number if rule['indexed_runtime_uses_suffix_as_index'] else position
            add(prefix+str(number).encode('ascii'),'indexed_floor',0xC1+27*entry_index,number)
    destroyed=rule.get('destroyed_resources')
    if destroyed and (base or not destroyed['requires_nonempty_base']):
        for number in destroyed['indices']:
            suffix=str(number).encode('ascii')
            if mode==8:name=b'Dungeon_Destroyed'+suffix
            elif mode==11:name=base+b'Destroyed'+suffix
            else:name=base+suffix+b'_DESTROYED'
            add(name,'destroyed_floor',0x271+number*27,number)
    if mode==7 and record[0x392]==13 and base:
        for water in SCHEMA['water_special']['requests']:
            add(water['name'].encode('ascii'),'special_water',water['runtime_offset'],
                resource_type=water['resource_type'],service_flags=water['service_flags'])
    if mode in (11,13):
        result['requests'].sort(key=lambda row:(row['suffix'],row['role']=='destroyed_floor'))
    if any(r['resource_type']==0 for r in result['requests']):
        result['findings'].append(dict(code='requests_with_zero_resource_type',
            explanation='These generated branches test a nonempty name, not a nonzero ArtResource type. Resource-manager success is not projected.'))
    return result
