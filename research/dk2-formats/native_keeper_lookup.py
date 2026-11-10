"""Read-only runtime Keeper lookup index; never renumbers serialized records."""
from native_trap_loading import u32


def lookup_index(ids,argument):
    u32(argument)
    if not isinstance(ids,(list,tuple)) or any(type(n)is not int or not 0<=n<=255 for n in ids):
        raise ValueError('Expected explicit sequence of runtime BYTE IDs')
    key=argument&255
    index=min(key-1,len(ids)-1)
    while index>=0:
        found=ids[index]
        if found==key:return index
        if found<key:return None
        index-=1
    return None
