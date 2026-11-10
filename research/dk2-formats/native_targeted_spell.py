"""Low32 target prediction in native498870;not a universal aiming model."""
from math import isqrt
def u32(v):return v&0xFFFFFFFF
def s32(v):return (v&0x7FFFFFFF)-(v&0x80000000)
def trunc(v,n):return abs(v)//n*(-1 if v<0 else 1)
def prediction(actor,target,previous,height):
    dx,dy=[s32(u32(target[i]-actor[i])) for i in range(2)]
    distance_arg=u32(((s32(u32(dx*dx))>>12)+(s32(u32(dy*dy))>>12))<<4)
    factor=isqrt(distance_arg)>>8
    lead=[u32(target[i]+trunc(s32(u32(u32(target[i]-previous[i])*factor*3)),2)) for i in range(2)]
    return dict(distance_argument=distance_arg,integer_distance_factor=factor,
      aim=lead+[u32(target[2]+(u32(height)>>1))])

def patch_height(raw,value):
    if not isinstance(raw,(bytes,bytearray)) or len(raw) not in [5449,5537]:raise ValueError('Expected Creature raw variant5449/5537')
    if type(value) is not int or not 0<=value<=0xFFFFFFFF:raise ValueError('Expected raw height DWORD bits')
    return bytes(raw[:0xEA4])+value.to_bytes(4,'little')+bytes(raw[0xEA8:])

