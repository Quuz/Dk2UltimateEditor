"""Bounded ActionPoint reveal-loop projection; no renderer/navigation model."""
import struct

def reveal_projection(width,height,cells,points,world_flags=0,room_types=None):
    if type(width) is not int or type(height) is not int or not 1<=width<=65535 or not 1<=height<=65535 or len(cells)!=width*height*16:raise ValueError('Expected bounded16-byte runtime tiles')
    room_types={} if room_types is None else room_types
    out=bytearray(cells);events=[];refresh=[];rooms=[]
    if world_flags&0x40:return bytes(out),events,refresh,rooms
    for p in points:
        if not 0<=p['flags']<=255:raise ValueError('Expected runtime flags byte')
        if not p['flags']&0x10:continue
        x0,y0,x1,y1=p['bounds']
        if any(type(v) is not int or not 0<=v<=65535 for v in p['bounds']):raise ValueError('Expected WORD bounds')
        if x0<=x1 and y0<=y1 and (x1>=width or y1>=height):raise ValueError('Native loop lacks coordinate bounds checks; refuse out-of-map projection')
        for y in range(y0,y1+1):
            for x in range(x0,x1+1):
                off=(y*width+x)*16;flags=struct.unpack_from('<H',out,off+8)[0]
                if flags&0x2000:continue
                room=struct.unpack_from('<H',out,off+2)[0]&0xFFF
                if room and room_types.get(room) in [3,4,8]:rooms.append(room);continue
                struct.pack_into('<H',out,off+8,flags|0x2000);events.append((x|(y<<16),1,0,3));refresh.append((x,y))
        events.append((x0|(y0<<16),((x1-x0+1)&65535)|(((y1-y0+1)&65535)<<16),0,6))
    return bytes(out),events,refresh,rooms

def toolbox_id(points):
    """Native traversal result: last matching runtime AP ID, including0."""
    result=0
    for p in points:
        if not 0<=p['flags']<=255 or not 0<=p['id']<=255:raise ValueError('Expected byte flags/ID')
        if p['flags']&0x20:result=p['id']
    return result
