"""Native pre-allocation CreatureSpell request; not successful Shot creation."""
def u32(v):return v&0xFFFFFFFF
def s32(v):return (v&0x7FFFFFFF)-(v&0x80000000)
def rotated_origin(position,offsets,a,b,possessed=False):
    if not isinstance(position,(list,tuple)) or not isinstance(offsets,(list,tuple)) or len(position)!=3 or len(offsets)!=3:raise ValueError('Expected XYZ vectors')
    for value in [*position,*offsets,a,b]:
        if type(value) is not int or not 0<=value<=0xFFFFFFFF:raise ValueError('Expected DWORD bits')
    if type(possessed) is not bool:raise ValueError('Expected bool')
    x,y,z=offsets
    mul=lambda p,q:s32(u32(p*q))>>16
    dx=0 if possessed else u32(mul(x,b)-mul(y,a))
    dy=0 if possessed else u32(mul(x,a)+mul(y,b))
    return [u32(position[0]+dx),u32(position[1]+dy),u32(position[2]+z)]
def waiting(tick,last,delay,flags):
    for value in [tick,last,delay,flags]:
        if type(value) is not int or not 0<=value<=0xFFFFFFFF:raise ValueError('Expected DWORD bits')
    return not bool(flags&0x80) and tick<=u32(last+delay)

def patch_slot(raw,slot,field,value):
    widths={'x':(0,4),'y':(4,4),'z':(8,4),'play_animation':(13,1),'delay':(16,4),'spell_id':(22,1),'level':(23,1)}
    if not isinstance(raw,(bytes,bytearray)) or len(raw) not in [5449,5537]:raise ValueError('Expected native Creature record')
    if type(slot) is not int or not 0<=slot<3 or type(field) is not str or field not in widths:raise ValueError('Expected physical slot0..2 and known field')
    offset,n=widths[field]
    if type(value) is not int or not 0<=value<1<<(8*n):raise ValueError('Value outside physical width')
    at=0xDC4+24*slot+offset
    return bytes(raw[:at])+value.to_bytes(n,'little')+bytes(raw[at+n:])
