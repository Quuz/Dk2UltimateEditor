"""Objects+C8 editor-map icon, independent of game GUI/in-hand resources.

Only canonical decimal suffixes are interpreted here; unusual strings remain
opaque. Patches preserve every resource attribute and bytes after the new NUL.
"""
import re

OFFSET = 0xC8

def _record(record):
    if type(record) is not bytes or len(record) != 894:
        raise ValueError('Expected one immutable894-byte Object record')

def view(record):
    _record(record)
    field = record[OFFSET:OFFSET+64]
    end = field.find(b'\0')
    name = field if end < 0 else field[:end]
    parts = name.split(b'#', 1)
    number = 0 if len(parts) == 1 else None
    if len(parts) == 2 and re.fullmatch(rb'[0-9]{1,9}', parts[1]):
        number = int(parts[1]) & 0xFFFF
    return dict(raw_offset=OFFSET, resource_hex=record[OFFSET:OFFSET+84].hex(),
        name_bytes_hex=name.hex(), name_terminated=end >= 0,
        resource_prefix_bytes_hex=parts[0].hex(), canonical_icon_number=number)

def patch_icon_index(record, index):
    _record(record)
    if type(index) is not int or not 0 <= index <= 65535:
        raise ValueError('Expected explicit unsigned16 icon index')
    state = view(record)
    if not state['name_terminated']:
        raise ValueError('Preserve unterminated resource name as opaque')
    prefix = bytes.fromhex(state['resource_prefix_bytes_hex'])
    name = prefix + b'#' + str(index).encode('ascii')
    if len(name) >= 64:
        raise ValueError('Resource name and NUL must fit64 bytes')
    old = bytes.fromhex(state['name_bytes_hex'])
    if name == old:
        return record
    result = bytearray(record)
    result[OFFSET:OFFSET+len(name)+1] = name + b'\0'
    return bytes(result)

def icon_rect(index, columns):
    if type(index) is not int or not 0 <= index <= 65535:
        raise ValueError('Expected unsigned16 index')
    if type(columns) is not int or not 1 <= columns <= 0xFFFFFFFF:
        raise ValueError('Expected positive unsigned32 column count')
    x, y = (index % columns)*64, (index // columns)*64
    return [x, y, x+64, y+64]
