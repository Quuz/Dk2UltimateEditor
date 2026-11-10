"""Exact candidate generation for DX480750 under blocked-navigation fixtures.

The StateUtils DWORD+AF is separate from placed-creature BYTE418. This oracle
does not assign units/effect to that placement byte and does not move creatures.
"""
DIRS=[(0,-1),(1,0),(0,1),(-1,0)]
JITTER=[(682,682),(2047,682),(3412,682),(682,2047),(2047,2047),(3412,2047),(682,3412),(2047,3412),(3412,3412)]
def u32(x):return x&0xFFFFFFFF
def s32(x):return (x&0x7FFFFFFF)-(x&0x80000000)
def rand(seed,bound):
    seed=u32(seed*1664525+1013904223)
    return seed,((seed>>13)|(seed<<19)&0xFFFFFFFF)%bound
def abs32(x):
    x=s32(x);return u32(-x if x<0 else x)
def candidates(seed,tick,deadline,position,home,radius,blocked):
    for v in [seed,tick,deadline,*position,*home,radius]:
        if type(v) is not int or not 0<=v<=0xFFFFFFFF:raise ValueError('Expected DWORD fixture')
    if len(position)!=2 or len(home)!=2 or len(blocked)!=4 or any(type(v) is not int or not 0<=v<=0xFFFFFFFF for v in blocked):raise ValueError('Invalid fixture width')
    trace=[];vector=[0,0,0];moves=0
    if deadline<=tick:return dict(seed=seed,queries=trace,last_vector=vector,movement_attempts=0)
    seed,start=rand(seed,4);gx,gy=[s32(v)>>12 for v in position]
    for n in range(4):
        dx,dy=DIRS[(start+n)&3];x,y=u32(gx+dx),u32(gy+dy)
        ax,ay=abs32(x-home[0]),abs32(home[1]-y)
        distance=ax if s32(ax)>s32(ay) else ay
        if distance>radius:continue
        blocked_result=blocked[len(trace)];trace.append([x,y])
        if blocked_result:continue
        seed,index=rand(seed,9);jx,jy=JITTER[index];vector=[u32((x<<12)|jx),u32((y<<12)|jy),0];moves+=1
    return dict(seed=seed,queries=trace,last_vector=vector,movement_attempts=moves)
