"""Mode18 request order only; successful creation and Next chains are separate."""
def record(value):
    if not isinstance(value,(bytes,bytearray)) or len(value)!=246:raise ValueError('246-byte Effects record required')
    if value[0xF2]!=18:raise ValueError('Only GenerationType18 supported')
    return bytes(value)

def requested_ids(raw,*,lifetime):
    raw=record(raw)
    if type(lifetime)is not int or not -(1<<31)<=lifetime<(1<<31):raise ValueError('Signed lifetime DWORD required')
    if lifetime<=0 or not int.from_bytes(raw[0xB4:0xB8],'little')&0x40:return []
    slots=[int.from_bytes(raw[0xCA+2*i:0xCC+2*i],'little') for i in range(4)]
    return [v for _ in range(raw[0xF3]//4) for v in reversed(slots) if v]

def patch(raw,*,elements_per_turn=None,first_four=None):
    out=bytearray(record(raw))
    if elements_per_turn is not None:
        if type(elements_per_turn)is not int or not 0<=elements_per_turn<=255:raise ValueError('ElementsPerTurn must beu8')
        out[0xF3]=elements_per_turn
    if first_four is not None:
        if not isinstance(first_four,(list,tuple)) or len(first_four)!=4 or any(type(v)is not int or not 0<=v<=65535 for v in first_four):raise ValueError('Four fullu16slot values required')
        out[0xCA:0xD2]=b''.join(v.to_bytes(2,'little') for v in first_four)
    return bytes(out)
