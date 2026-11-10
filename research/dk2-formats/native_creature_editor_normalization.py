"""Explicit read-only native editor normalization; never normalize on codec load."""
from pathlib import Path
from fractions import Fraction
import json

SCHEMA=json.loads((Path(__file__).resolve().parent/'creature-editor-normalization.json').read_text(encoding='utf8'))
FIELDS={f['raw_offset']:f for f in SCHEMA['fields']}

def check(record):
    if not isinstance(record,(bytes,bytearray)) or len(record) not in SCHEMA['record_variants']:
        raise ValueError('Expected one5449- or5537-byte Creatures record')

def project_raw(raw_offset,raw):
    if type(raw_offset) is not int or raw_offset not in FIELDS:raise ValueError('Unknown normalized field offset')
    f=FIELDS[raw_offset]
    if type(raw) is not int or not 0<=raw<(1<<(f['raw_bytes']*8)):raise ValueError('Expected unsigned raw integer of physical width')
    rule=f['normalization'];changed=raw>rule['maximum_preserved_raw'];normalized=rule['replacement_raw'] if changed else raw
    within=None
    if f['native_dialog']:
        minimum,maximum=f['native_dialog']['validation_range'];within=minimum*f['raw_divisor']<=normalized<=maximum*f['raw_divisor']
    return dict(raw_offset=raw_offset,raw_bytes=f['raw_bytes'],name=f['baseline_name'],raw_before=raw,
        raw_after_model_callback=normalized,native_writer_raw_if_model_unedited=normalized,
        value_before=float(Fraction(raw,f['raw_divisor'])),model_value=float(Fraction(normalized,f['raw_divisor'])),
        callback_changed_value=changed,within_declared_ddx_range=within,
        note='Numeric range check only; does not predict full dialog validation failure handling.')

def views(record):
    check(record)
    return [project_raw(f['raw_offset'],int.from_bytes(record[f['raw_offset']:f['raw_offset']+f['raw_bytes']],'little')) for f in SCHEMA['fields']]

def patch_raw(record,raw_offset,raw):
    check(record);project_raw(raw_offset,raw);width=FIELDS[raw_offset]['raw_bytes']
    return bytes(record[:raw_offset])+raw.to_bytes(width,'little')+bytes(record[raw_offset+width:])

def normalized_copy_scenario(record):
    """Return a pure copy showing the four callback/write effects, when explicitly requested."""
    check(record);result=bytes(record)
    for v in views(record):result=patch_raw(result,v['raw_offset'],v['native_writer_raw_if_model_unedited'])
    return result
