"""Raw extra flag bit1, kept separate from the long-record editor slot."""
import struct
OFFSETS={'game':0x1545,'editor':0x1599}

def flag_value(raw,slot='game'):
    if slot not in OFFSETS or len(raw)<OFFSETS[slot]+4:raise ValueError('Requested flag slot is not physically present')
    return struct.unpack_from('<I',raw,OFFSETS[slot])[0]

def set_immunity(raw,enabled,slot='game'):
    if type(enabled) is not bool:raise ValueError('Expected explicit bool')
    flags=flag_value(raw,slot);new=(flags|1) if enabled else (flags&~1);off=OFFSETS[slot]
    return bytes(raw[:off])+struct.pack('<I',new)+bytes(raw[off+4:])
