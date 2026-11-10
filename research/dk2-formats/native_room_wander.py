"""Bounded room-list oracle: prepared CellC=0, no blocker chains, 32x32 map.

Does not infer raw WanderRadius units, normal room-list ordering or full AI.
"""
from native_wander_state import rand,JITTER
from native_wander_target import ring_point,nav_value
ELIGIBLE={1,2,4,6,7,9,10,11,12,13,14,15,16,26}
def choose(seed,pos,radius,c):
    seed,gate=rand(seed,8);trials=[];bounds=[8];target=None;selected=None
    good=[i for i,r in enumerate(c['rooms']) if r['type'] in ELIGIBLE]
    if gate==0 and good:
        # Native counter starts at count, not at a fresh random room index.
        for i in range(good[-1],len(c['rooms'])):
            row=c['rooms'][i];assert row.get('cellC',0)==0
            trials.append(i);cells=row.get('cells',[])
            if cells:
                seed,j=rand(seed,len(cells));bounds.append(len(cells));x,y=cells[j]
                target=[x*4096+2048,y*4096+2048,0];selected=i;break
    queries=[];start=None
    if target is None:
        seed,start=rand(seed,radius*8);bounds.append(radius*8)
        for idx in range(start,radius*8):
            x,y=ring_point([v>>12 for v in pos[:2]],radius,idx);queries.append([x,y])
            if not 0<=x<32 or not 0<=y<32 or nav_value(c,x,y)&8:continue
            if c.get('owner',1)!=1 and not c.get('actor_flags',0)&0x4000:continue
            # All prepared room cells are deliberately outside tested rings.
            assert [x,y] not in [p for r in c['rooms'] for p in r.get('cells',[])]
            target=[x*4096+2048,y*4096+2048,0];break
    return dict(seed=seed,room_gate=gate,room_trials=trials,selected_room=selected,target=target,queried_cells=queries,rng_bounds=bounds,start_index=start)
def expected(c):
    seed=c['seed'];pos=c.get('position',[0xA800,0xA800,0x2000])
    if not c.get('callback'):return choose(seed,pos,c.get('radius',4),c)
    seed,k=rand(seed,4);radius=k+4;runs=[];bounds=[4];target=None
    for r in range(radius,0,-1):
        h=choose(seed,pos,r,c);seed=h['seed'];runs.append(dict(radius=r,**h));bounds+=h['rng_bounds']
        if h['target'] is not None:target=h['target'];break
    if target is None:target=list(pos)
    seed,j=rand(seed,9);jx,jy=JITTER[j];bounds.append(9)
    target=[(target[0]&0xfffff000)|jx,(target[1]&0xfffff000)|jy,target[2]]
    return dict(seed=seed,radius=radius,helper_runs=runs,target=target,rng_bounds=bounds,room_trials=[i for h in runs for i in h['room_trials']])
