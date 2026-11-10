"""Read-only effective/raw Creature lookup;does not reorder file records."""
from native_trap_loading import u32


def lookup_index(rows,argument,route):
    u32(argument)
    if route not in ['effective','raw'] or not isinstance(rows,(list,tuple)):raise ValueError('Expectedexplicitruntime rows androute')
    for row in rows:
        if not isinstance(row,(list,tuple)) or len(row)!=3:raise ValueError('ExpectedownID,cloneID,flags')
        own,clone,flags=row;u32(own);u32(clone);u32(flags)
        if own>255 or clone>255:raise ValueError('ExpectedBYTEIDs')
    key=argument&255;index=min(key-1,len(rows)-1)
    while index>=0:
        own,clone,flags=rows[index];found=clone if route=='effective' and flags&0x08000000 else own
        if found==key:return index
        if found<key:return None
        index-=1
    return None
