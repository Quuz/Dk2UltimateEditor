"""Lossless eight physical Terrain deformation bytes; no game normalization."""
OFFSETS={'wibble_h':0x1CE,'lean_h':0x1CF,'wibble_v':0x1D2,'lean_v':0x1D3}

def check(raw):
    if type(raw) is not bytes or len(raw)!=552:raise ValueError('Expected exact552-byte Terrain record')

def view(raw):
    check(raw)
    return dict(wibble_h=raw[0x1CE],lean_h=list(raw[0x1CF:0x1D2]),wibble_v=raw[0x1D2],lean_v=list(raw[0x1D3:0x1D6]))

def patch(raw,**changes):
    check(raw);out=bytearray(raw)
    for name,value in changes.items():
        if name not in OFFSETS:raise ValueError('Unknown deformation field')
        o=OFFSETS[name]
        if name.startswith('lean_'):
            if type(value) not in [list,tuple] or len(value)!=3 or any(type(v) is not int or not 0<=v<=255 for v in value):raise ValueError('Expected3 physical bytes')
            out[o:o+3]=bytes(value)
        else:
            if type(value) is not int or not 0<=value<=255:raise ValueError('Expected physical byte')
            out[o]=value
    return bytes(out)
