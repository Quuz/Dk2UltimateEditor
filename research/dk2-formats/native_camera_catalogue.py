"""Camera catalogue inside Variables files; raw records stay authoritative."""
import struct
from native_cameras import payload,record_view,preset_projection

def model_from_raw(raw):
    """Pro parser+callback projection:raw53 is skipped and zeroed by mid init."""
    p=bytearray(payload(raw));p[83]=0
    return bytes(p)

def editor_scaled_view(raw):
    p=payload(raw);out={}
    for name,off in [('position',0),('min_clip',12),('max_clip',24),('view',36),('zoom',48),('lens',60)]:
        code='<3I' if name=='lens' else '<3i';out[name]=[n/4096 for n in struct.unpack_from(code,p,off)]
    out.update(angles=list(struct.unpack_from('<3H',p,76)),id=p[82],flags=struct.unpack_from('<I',p,72)[0],opaque_tail=p[83]);return out

def apply_checkboxes(raw,values):
    """RC3 seven BOOL setters:nonzero=true, preserve flags beyond7F."""
    payload(raw)
    if len(raw)!=92 or len(values)!=7 or any(type(v) is not int or not 0<=v<=0xFFFFFFFF for v in values):raise ValueError('Expected fullCD84 and7 DWORD BOOLs')
    b=bytearray(raw);flags=struct.unpack_from('<I',b,80)[0]&~127
    for i,v in enumerate(values):
        if v:flags|=1<<i
    struct.pack_into('<I',b,80,flags);return bytes(b)

def bank_projection(records,machine_spec_level,initial_bank):
    """Apply ordered camera records to the inspected game's preset bank."""
    if len(initial_bank)!=0xC00:raise ValueError('Expected0xC00 camera-bank comparison region')
    b=bytearray(initial_bank)
    for raw in records:
        v=preset_projection(raw,machine_spec_level)
        if v['accepted']:b[v['slot_offset']:v['slot_offset']+152]=v['slot']
    return bytes(b)

def writer_section(models,header_seed,date_bytes):
    """Exact scoped native writer projection with external date/stack fixtures.

    models are84-byte raw-layout model data, excluding vtable. No filtering,
    renumbering, default filling, limit1..18, or duplicate-ID removal.
    """
    if len(header_seed)!=56 or len(date_bytes)!=9 or any(not isinstance(v,(bytes,bytearray)) or len(v)!=84 for v in models):raise ValueError('Expected raw model84,header56,date9')
    n=len(models);b=bytearray(header_seed)
    for off,v in [(0,190),(4,4),(8,56+92*n),(12,191),(16,28),(20,n),(24,1),(48,192),(52,92*n)]:struct.pack_into('<I',b,off,v)
    b[28:37]=date_bytes;b[38:47]=date_bytes
    return bytes(b)+b''.join(struct.pack('<II',205,84)+bytes(v) for v in models)
