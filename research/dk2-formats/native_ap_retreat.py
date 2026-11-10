"""Scoped projection of DX4611D0 ActionPoint branch; not a navigator.

Only entity owner==explicit good-player WORD fixture branch is projected.
Reachability,state/movement and sound operations remain external providers.
"""
import struct
U32=0xFFFFFFFF
def d(b,o):return struct.unpack_from('<I',b,o)[0]
def w(b,o):return struct.unpack_from('<H',b,o)[0]
def setd(b,o,n):struct.pack_into('<I',b,o,n&U32)
def sw(b,o,n):struct.pack_into('<H',b,o,n&65535)

def project(entity,nodes,head,*,good_player=4,nav=(1,),state=22,state_ok=1,moves=(1,),now=123456,sound_handle=0x777):
    if w(entity,0x28)!=good_player:raise ValueError('Non-good-player branches are not projected')
    b=bytearray(entity);calls=dict(nav=[],state_query=0,state_change=[],moves=[],post=[],audio=[]);chosen=None;tag=head;seen=set()
    if b[0x46F]&16:
        while tag:
            if tag in seen:raise ValueError('Native linked-list cycle not executed')
            seen.add(tag);ap=nodes[tag]
            if ap[0x32]&1:
                point=((w(ap,0x38)<<12)+2048,(w(ap,0x3A)<<12)+2048,0)
                idx=len(calls['nav']);calls['nav'].append(point)
                if idx<len(nav) and nav[idx]:chosen=point;break
            tag=w(ap,8)
    if chosen is None:return dict(entity=bytes(b),calls=calls,result=0,chosen=None)
    calls['state_query']=1
    ready=state==22
    if not ready:calls['state_change'].append((199,0));ready=bool(state_ok)
    moved=False
    if ready:
        for i,action in enumerate([0xE8,0x68]):
            calls['moves'].append((chosen,0x10A,action))
            if i<len(moves) and moves[i]:moved=True;break
    if moved:
        calls['post']=[(1,0)];calls['audio']=[(d(b,0x2E),0x1234,0xDC)]
        setd(b,0x2E,sound_handle)
        if b[0x36F]!=1:
            x=d(b,0x1A);x=x if x<0x80000000 else x-0x100000000
            # Both native loads read entity+1A; second is not entityY+1E.
            sw(b,0x52A,x>>12);sw(b,0x52C,x>>12);setd(b,0x52E,now)
    return dict(entity=bytes(b),calls=calls,result=int(moved),chosen=chosen)
