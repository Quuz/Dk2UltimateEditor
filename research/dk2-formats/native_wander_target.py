"""Selected empty-room-list32x32 wander target oracle; not a game AI simulator.

Only normal coordinate fixtures with nonzero final ring corner, no room tags,
no blocking object chains, and nav/ownership gates. Does not infer placed-byte
WanderRadius meaning or predict the pathfinder/controller internals.
"""
from native_wander_state import rand,JITTER

def ring_point(origin,radius,index):
    x,y=origin;side,step=divmod(index,2*radius)
    if side==0:return x+step-radius,y-radius
    if side==1:return x+radius,y+step-radius
    if side==2:return x+step-radius,y+radius
    if side==3:return x-radius,y+step-radius
    raise ValueError('Ring index')

def nav_value(c,x,y):
    v=8 if c.get('blocked',False) else 0
    for nx,ny,flag in c.get('nav',[]):
        if (nx,ny)==(x,y):v=flag
    return v

def choose(seed,position,radius,c):
    if type(radius)is not int or not 1<=radius<=7:raise ValueError('Only selected native callback radii1..7')
    origin=tuple(v>>12 for v in position[:2]);assert ring_point(origin,radius,8*radius-1)!=(0,0)
    # First RNG controls the optional room branch, whose list is empty here.
    seed,room_gate=rand(seed,8);seed,start=rand(seed,8*radius);attempted=[];target=None
    for index in range(start,8*radius):
        x,y=ring_point(origin,radius,index);attempted.append([x,y])
        if not 0<=x<32 or not 0<=y<32 or nav_value(c,x,y)&8:continue
        if c.get('owner',1)!=1 and not c.get('actor_flags',0)&0x4000:continue
        target=[x*4096+2048,y*4096+2048,0];break
    return dict(seed=seed,start_index=start,room_gate=room_gate,queried_cells=attempted,target=target)

def callback(c):
    seed=c.get('seed',1);pos=c.get('position',[0xA800,0xA800,0x2000]);action=c.get('current_action',0)
    if action not in [0,0x15]:return dict(seed=seed,radius=None,helper_runs=[],target=None,path_requested=False)
    seed,r=rand(seed,4);radius=r+4;runs=[];chosen=None
    for size in range(radius,0,-1):
        out=choose(seed,pos,size,c);seed=out['seed'];runs.append(dict(radius=size,**out))
        if out['target'] is not None:chosen=out['target'];break
    if chosen is None:chosen=list(pos)
    seed,j=rand(seed,9);jx,jy=JITTER[j]
    target=[(chosen[0]&0xFFFFF000)|jx,(chosen[1]&0xFFFFF000)|jy,chosen[2]]
    return dict(seed=seed,radius=radius,helper_runs=runs,target=target,path_requested=True)
