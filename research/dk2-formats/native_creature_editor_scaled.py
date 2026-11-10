"""Read-only22-field fixed-point views; selected-property side effects are explicit."""
from pathlib import Path
from fractions import Fraction
import json

SCHEMA=json.loads((Path(__file__).resolve().parent/'creature-editor-scaled.json').read_text(encoding='utf8'))
FIELDS={f['raw_offset']:f for f in SCHEMA['fields']}

def check(record):
    if not isinstance(record,(bytes,bytearray)) or len(record) not in SCHEMA['record_variants']:
        raise ValueError('Expected one5449- or5537-byte Creatures record')

def field(raw_offset):
    if type(raw_offset) is not int or raw_offset not in FIELDS:raise ValueError('Unknown scaled field offset')
    return FIELDS[raw_offset]

def loaded_value(raw_offset,raw):
    f=field(raw_offset)
    if type(raw) is not int or not 0<=raw<=0xFFFFFFFF:raise ValueError('Expected raw unsigned DWORD')
    scale=f['raw_to_display']
    return Fraction(raw*scale['numerator'],scale['denominator'])

def views(record):
    check(record);result=[]
    for f in SCHEMA['fields']:
        offset=f['raw_offset'];raw=int.from_bytes(record[offset:offset+4],'little');value=loaded_value(offset,raw)
        result.append(dict(raw_offset=offset,raw=raw,native_label=f['native_label'],property_id=f['property_id'],
            native_editor_value=float(value),value_numerator=value.numerator,value_denominator=value.denominator,
            gui_validation_range=None,property_update=f['property_update']))
    return result

def project_loaded_property_update(raw_offset,raw):
    """Assume exact original value as update input and __ftol truncation.

    This is not a prediction that every file save touches the property.
    """
    f=field(raw_offset);before=loaded_value(raw_offset,raw);after=before
    if f['property_update']=='truncate_to_integer_then_low8_then_double':after=Fraction(int(before)&255)
    # For Scale, the writer constant is the binary64 representation of40.96,
    # not the exact rational inverse. Keep that visible rather than replacing it.
    constant=SCHEMA['constants'][hex(f['display_to_writer']['constant_address'])]
    writer_exact=after*Fraction(constant['numerator'],constant['denominator'])
    projected=int(writer_exact)&0xFFFFFFFF
    return dict(raw_offset=raw_offset,raw_before=raw,raw_after_projected=projected,
        display_before=float(before),display_after_update=float(after),changed=projected!=raw,
        property_update=f['property_update'],
        projection='Exact rational arithmetic on the loaded value and stored writer constant; __ftol truncation assumed. No x87 emulation or text parsing.')

def patch_raw(record,raw_offset,raw):
    check(record);loaded_value(raw_offset,raw)
    return bytes(record[:raw_offset])+raw.to_bytes(4,'little')+bytes(record[raw_offset+4:])
