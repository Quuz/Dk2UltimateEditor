"""Native44DEC0 distance only; not the complete game picking pipeline."""
def patch_priority(raw,family,value):
    variants={'Creatures':([5449,5537],0xF55),'Objects':([894],0x35D)}
    if family not in variants or not isinstance(raw,(bytes,bytearray)):raise ValueError('Expected supported family and raw record')
    sizes,offset=variants[family]
    if len(raw) not in sizes or type(value) is not int or not 0<=value<=255:raise ValueError('Expected known record length and unsigned byte')
    return bytes(raw[:offset])+bytes([value])+bytes(raw[offset+1:])

def distance(dx,dy):
    if type(dx) is not int or type(dy) is not int or not -(1<<31)<dx<(1<<31) or not -(1<<31)<dy<(1<<31):
        raise ValueError('Expected signed32 differences excluding INT_MIN overflow boundary')
    a,b=abs(dx),abs(dy)
    return (max(a,b)+(min(a,b)>>1))&0xFFFFFFFF
