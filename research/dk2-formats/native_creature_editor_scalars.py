"""Read-only views and pure byte patches for the35 verified primitive properties."""
from pathlib import Path
import json

SCHEMA=json.loads((Path(__file__).resolve().parent/'creature-editor-scalars.json').read_text(encoding='utf8'))
FIELDS={f['raw_offset']:f for f in SCHEMA['fields']}

def check(record):
    if not isinstance(record,(bytes,bytearray)) or len(record) not in SCHEMA['record_variants']:
        raise ValueError('Expected one5449- or5537-byte Creatures record')

def views(record):
    check(record);result=[]
    for f in SCHEMA['fields']:
        offset=f['raw_offset'];width=f['raw_bytes'];raw=int.from_bytes(record[offset:offset+width],'little')
        value=raw-(1<<(width*8)) if f['editor_display_kind'].startswith('s') and raw&(1<<(width*8-1)) else raw
        result.append(dict(raw_offset=offset,raw_bytes=width,raw=raw,native_label=f['native_label'],property_id=f['property_id'],
            native_editor_value=value,native_editor_double=float(value),native_editor_kind=f['editor_display_kind'],
            gui_validation_range=None))
    return result

def commit_converted_integer(raw_offset,value):
    """Only narrow an already-converted __ftol signed64 result. No float parsing."""
    if type(raw_offset) is not int or raw_offset not in FIELDS:raise ValueError('Unknown primitive field offset')
    if type(value) is not int or not -(1<<63)<=value<(1<<63):raise ValueError('Expected signed64 converted integer')
    f=FIELDS[raw_offset];bits=f['raw_bytes']*8;raw=value&((1<<bits)-1)
    display=raw-(1<<bits) if f['editor_display_kind'].startswith('s') and raw&(1<<(bits-1)) else raw
    return dict(raw_offset=raw_offset,raw_bytes=f['raw_bytes'],raw=raw,native_editor_value_after_reload=display)

def patch_raw(record,raw_offset,value):
    """Return a patched copy. Never writes a file, clamps values or rescales units."""
    check(record)
    if type(raw_offset) is not int or raw_offset not in FIELDS:raise ValueError('Unknown primitive field offset')
    width=FIELDS[raw_offset]['raw_bytes']
    if type(value) is not int or not 0<=value<(1<<(width*8)):raise ValueError('Expected unsigned raw value fitting physical width')
    return bytes(record[:raw_offset])+value.to_bytes(width,'little')+bytes(record[raw_offset+width:])
