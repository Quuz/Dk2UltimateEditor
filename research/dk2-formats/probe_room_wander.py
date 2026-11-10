"""Prepared native room/cell lists -> original480A00/4E8430, optional real NAV callback.

No original code, switch table, vtable or import substitutions. Room objects
and their cell/owner linked lists are explicit runtime fixtures, not generated
by the original map bootstrap. Phase144 executor is extended only to observe.
"""
from pathlib import Path
import json,struct
import probe_wander_target as base
import probe_wander_path as path
import native_unicorn_roomwandertrace as cpu
from emulate_element_colors import region,pack32
ROOT=Path(__file__).resolve().parent
ROOMBASE=0x10D00000
ROOMSIZE=169
TERRAINDEF=0x10D10000
def room_address(index):return ROOMBASE+0x1000*index
def build(c):
    args,_=(path if c.get('callback') else base).build(c)
    mem={int(r['address'],16):bytearray.fromhex(r['hex']) for r in json.loads(args['memory'])['regions']}
    def put(a,o,v,n=4):base.f.put(mem[a],o,v,n)
    mem[0x6DA4A4]=bytearray(pack32(base.f.WORLD))
    mem[0x6DA498]=bytearray(pack32(base.f.WORLD+0x9DA))
    # Real454230 checks neighboring NAV8 cells via the Terrain cache. The
    # scene declares a prepared non-corner-blocking floor definition for ID1.
    # Never let a missing cache entry silently read mapped low-page memory.
    mem[TERRAINDEF]=bytearray(460)+bytearray([165])*64
    mem[TERRAINDEF][0x1C4]=1
    mem[0x6DA4A8]=bytearray(1024);put(0x6DA4A8,4,TERRAINDEF)
    rooms=c['rooms'];put(base.f.PLAYER,0x36,200 if rooms else 0,2)
    points=set()
    for i,row in enumerate(rooms):
        a=room_address(i);mem[a]=bytearray(ROOMSIZE)+bytearray([165])*64
        put(a,4,200+i,2);put(a,8,201+i if i+1<len(rooms) else 0,2)
        put(a,0x18,row['type'],1)
        cells=row.get('cells',[]);anchor=cells[0] if cells else row.get('anchor',[28,28])
        put(a,0x2D,anchor[0],1);put(a,0x2E,anchor[1],1)
        put(0x6EE540,(200+i)*4,a)
        for j,(x,y) in enumerate(cells):
            assert (x,y) not in points and 1<=x<31 and 1<=y<31;points.add((x,y))
            at=16*(y*32+x);put(base.f.MAP,at+2,200+i,2)
            nextxy=cells[j+1] if j+1<len(cells) else [0,0]
            put(base.f.MAP,at+4,nextxy[0]+256*nextxy[1],2)
            put(base.f.MAP,at+12,row.get('cellC',0),1)
    args['memory']=json.dumps(dict(regions=[region(a,b) for a,b in mem.items()]))
    args['observe_reads'] += [[room_address(i),ROOMSIZE+64] for i in range(len(rooms))]
    checks={a:bytes(b) for a,b in mem.items() if a!=0x7FFEF000}
    for a,n in [(0x66E3EC,0x3A0),(0x66D524,0x130)]:checks[a]=base.pe_read(base.EXE.read_bytes(),a,n)
    spans=sorted((a,len(b)) for a,b in mem.items());assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:]))
    return args,checks
def execute(c,verbose=True):
    args,checks=build(c);key=cpu.sha((json.dumps(args,sort_keys=True)+cpu.sha((ROOT/'native_unicorn_roomwandertrace.py').read_bytes())+json.dumps({hex(a):cpu.sha(b) for a,b in checks.items()},sort_keys=True)).encode())[:12]
    p=ROOT/'ghidra'/f'phase145-room-wander-native-{key}.json'
    if p.exists():saved=json.loads(p.read_text(encoding='utf8'));assert saved['input']==args
    else:
        result=cpu.run(base.EXE,args,list(checks.items()),base.STOP,base.cf.ACTOR if c.get('callback') else base.TARGET,len(checks[base.cf.ACTOR]) if c.get('callback') else 76)
        saved=dict(case=c,input=args,result=result);p.write_text(json.dumps(saved,indent=2),encoding='utf8')
    if verbose:
        r=saved['result'];print(json.dumps(dict(case=c,success=r['success'],error=r['emulation_error'],registers=r['registers'],tail=r['trace_tail'],room_calls=r['room_target_calls'],rng=r['wander_rng_calls'],helper=r['wander_target_calls'])),flush=True)
    return args,checks,saved,p
if __name__=='__main__':
    from native_wander_state import rand
    seed=next(s for s in range(1000) if rand(s,8)[1]==0)
    execute(dict(name='two-rooms',radius=1,seed=seed,rooms=[dict(type=1,cells=[[3,3]]),dict(type=2,cells=[[20,20]])]))
