"""Physical Objects resource slots; native UI labels do not replace baseline names."""
from pathlib import Path
import json
from dk2_formats import art

SCHEMA=json.loads((Path(__file__).resolve().parent/'object-resource-bindings.json').read_text(encoding='utf8'))
OFFSETS=frozenset(s['raw_offset'] for s in SCHEMA['slots'])

def _record(record):
    if not isinstance(record,(bytes,bytearray)) or len(record)!=894:
        raise ValueError('Expected one894-byte Objects record')

def object_resources(record):
    _record(record)
    return [dict(raw_offset=s['raw_offset'],baseline_field=s['baseline_field'],
        native_editor_label=s['native_editor']['label'] if s['native_editor'] else None,
        runtime_entry_offset=s['runtime_entry_offset'],
        raw_hex=record[s['raw_offset']:s['raw_offset']+84].hex(),
        resource=art(record[s['raw_offset']:s['raw_offset']+84])) for s in SCHEMA['slots']]

def set_object_resource(record,raw_offset,resource_bytes):
    """Replace exactly one explicitly selected84-byte slot, without alias copying."""
    _record(record)
    if type(raw_offset) is not int or raw_offset not in OFFSETS:
        raise ValueError('Select a physical ArtResource offset from the Objects schema')
    if not isinstance(resource_bytes,(bytes,bytearray)) or len(resource_bytes)!=84:
        raise ValueError('Resource replacement must contain exactly84 bytes')
    return bytes(record[:raw_offset])+bytes(resource_bytes)+bytes(record[raw_offset+84:])
