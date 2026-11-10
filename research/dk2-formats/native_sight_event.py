"""DX Sight-of-Evil process25 through Bridge event0D and region setup.

Describes native state/events up to explicit redraw/effect/sound providers.
Does not simulate renderer, final visibility shape, cast availability orC8.
"""
import struct
def u32(v):return v&0xFFFFFFFF
def s32(v):return (v&0x7FFFFFFF)-(v&0x80000000)
def describe_data1(bits):
    if type(bits) is not int or not 0<=bits<=0xFFFFFFFF:raise ValueError('Expected DWORD Data1')
    return dict(initial_event_enabled=bits!=0,event_radius_word=bits&65535,receiver_radius=min(bits&65535,7))
def packed_rectangle(bounds):
    a,b,c,d=bounds
    return [a&65535|(b&65535)<<16,(c-a)&65535|((d-b)&65535)<<16,0,6]
def region_state(x,y,radius,width,height,tick,time_word):
    if not 1<=width<=0x7FFFFFFF or not 1<=height<=0x7FFFFFFF:raise ValueError('Expected positive map dimensions')
    clamp=lambda v,maximum:min(max(s32(u32(v)),0),maximum-1)
    inside=[clamp(x-radius,width),clamp(y-radius,height),clamp(x+radius,width),clamp(y+radius,height)]
    a,b,c,d=inside;outside=[clamp(a-1,width),clamp(b-1,height),clamp(c+1,width),clamp(d+1,height)]
    return dict(start_tick=tick,end_tick=u32(tick+time_word),radius=min(radius,7),inside=inside,outside=outside)
