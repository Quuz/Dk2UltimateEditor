"""Scoped projections of Terrain enumeration, cache fill and light selection.

Inputs are prebuilt runtime definitions/worlds. These are not raw-file sorting,
allocator, parser, renderer, or GUI policies. Offsets are hexadecimal in docs.
"""
def enumerate_id(ids,capacity,ordinal):
    if type(capacity) is not int or not 0<=capacity<=len(ids):raise ValueError('Invalid capacity')
    if type(ordinal) is not int or not 0<=ordinal<=0xFFFFFFFF:raise ValueError('Invalid DWORD ordinal')
    if any(type(i) is not int or not 0<=i<=255 for i in ids):raise ValueError('Invalid BYTE ID')
    selected=[i for i in ids[1:capacity] if i]
    return selected[ordinal] if ordinal<len(selected) else 0

def cache_ids(ids,capacity,loaded):
    if type(loaded) is not int or not 0<=loaded<=0xFFFFFFFF:raise ValueError('Invalid loaded count')
    enumerate_id(ids,capacity,0) # Validate the complete fixture even when empty.
    selected=[i for i in ids[1:capacity] if i]
    out=set(selected[:loaded])
    if loaded>len(selected):out.add(0)
    return out

def light_selection(tile,flags,fallback_flags,query_result):
    """Return (native DWORD bool, output selector or None, query called).

    query_result is an explicit world-vslot21C fixture, not reconstructed terrain
    geometry. A false return can still write the output pointer.
    """
    if len(tile)!=16:raise ValueError('Expected 16-byte tile fixture')
    for x in (flags,fallback_flags,query_result):
        if type(x) is not int or not 0<=x<=0xFFFFFFFF:raise ValueError('Expected DWORD')
    if int.from_bytes(tile[2:4],'little')&0xFFF:
        return (1,'current',False) if tile[12]==255 and flags&0x10000000 else (0,None,False)
    if not tile[9]&0x20 and tile[11]&0x40:
        if flags&0x01000000:return ((flags>>28)&1,'current',False)
        return ((fallback_flags>>28)&1,'fallback38',False)
    if flags&0x10000000:
        return (1,'current',True) if query_result==0 else (0,None,True)
    return 0,None,False
