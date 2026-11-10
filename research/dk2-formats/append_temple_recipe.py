"""Append one rawtype43 to a canonical Variables section; preserve all opaque bytes.

Refuse forensic count/size disagreements instead of silently repairing them.
Native full-file parser is not executed by this helper. It only rebuilds bytes.
"""
import struct
import dk2_formats as fmt
from native_sacrifice import validate_raw

def append(data,recipe):
    validate_raw(recipe)
    sections=fmt.parse(data);hits=[(i,s) for i,s in enumerate(sections) if s.signature==0xE6]
    if len(hits)!=1:raise ValueError('Expected one Variables section')
    index,s=hits[0]
    if len(s.header)!=56 or any(len(r)!=16 for r in s.records):raise ValueError('Expected16byte Variable records')
    count=len(s.records)
    if (struct.unpack_from('<I',s.header,20)[0]!=count or struct.unpack_from('<I',s.header,52)[0]!=16*count
        or struct.unpack_from('<I',s.header,8)[0]!=56+16*count+len(s.suffix)):
        raise ValueError('Noncanonical source header; forensic bytes require explicit handling')
    clone=fmt.Section(s.offset,s.signature,s.header,list(s.records),s.suffix)
    clone.replace_records(list(s.records)+[recipe])
    return b''.join(clone.pack() if i==index else x.pack() for i,x in enumerate(sections))

def recipes(data):
    return [r for s in fmt.parse(data) if s.signature==0xE6 for r in s.records if struct.unpack_from('<I',r)[0]==43]
