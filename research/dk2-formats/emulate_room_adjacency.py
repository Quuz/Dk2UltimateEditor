"""Raw Rooms -> original CMap/room dispatcher -> construction7 adjacency.

Prepared world/cells/room and native Creature fixture; no code/vtable patches.
The declared renderer/audio lookup returns failure; UI callback captures only
the original queued request. NAV4 prestate avoids a redundant mesh rebuild.
"""
from pathlib import Path
import json,struct,sys,hashlib
import emulate_temple_creature_entry as prior
from emulate_element_colors import region
from emulate_resistance import post_context
from build_effect_semantics import EXE
import native_unicorn_factorytrace as cpu
import dk2_formats as fmt

ROOT=Path(__file__).resolve().parent
TABLE,ROOMRAW,ROOMMID,NAV,BITS,PROVIDER,PVT,IO,LOOKUP,QUEUE=[0x18100000,0x18120000,0x18130000,0x18140000,0x18150000,0x18160000,0x18161000,0x18170000,0x18171000,0x18171100]
VFX=0x18171200
TERRAINRAW,TERRAINMID=0x18190000,0x181A0000
OTHERROOM=0x181B0000
DIRS=[(0,-1),(1,-1),(1,0),(1,1),(0,1),(-1,1),(-1,0),(-1,-1)]
put=prior.put
def sha(b):return hashlib.sha256(b).hexdigest()

def cases():
    base=dict(mask=255,mode=7,room_id=13,owner=2,neighbour_owner=2,neighbour_room=0,high_owner=0,xy=[2,2],entry=False,initial_nav=4)
    rows=[('all',{}),('none',dict(mask=0)),('missing-diagonal',dict(mask=253)),
          ('other-owner',dict(neighbour_owner=3)),('other-room',dict(neighbour_room=4)),
          ('same-room',dict(neighbour_room=1)),('owner-high-bits',dict(high_owner=0xF000)),
          ('corner',dict(xy=[0,0])),('edge',dict(xy=[2,0])),('far-corner',dict(xy=[4,4])),('queue-asymmetry',dict(xy=[3,3])),
          ('mode1',dict(mode=1)),('mode6',dict(mode=6)),('prison',dict(room_id=11)),('combat-pit',dict(room_id=16)),
          ('entry-all',dict(entry=True)),('entry-missing-diagonal',dict(entry=True,mask=253)),
          ('entry-other-owner',dict(entry=True,neighbour_owner=3))]
    rows += [('bit'+str(i),dict(mask=1<<i)) for i in range(8)]
    return [dict(base,**v,name=n) for n,v in rows]

def build(c,baseline=False):
    pc=dict(prior.cases()[1],name=c['name'],cell_kind=0xA5)
    memory,calls=prior.memory_and_calls(pc)
    memory={a:bytearray(b) for a,b in memory.items()}
    raw=next(r for s in fmt.parse((prior.SOURCE.parent/'Rooms.kwd').read_bytes()) if fmt.FAMILIES.get(s.signature)=='Rooms' for r in s.records if r[0x392]==c['room_id'])
    assert len(raw)==1055 and raw[0x395]==7
    terrain_id=raw[0x394]
    w=memory[prior.WORLD]
    for o,v in [(0x9C4,prior.WORLD),(0x9DA,0x66D3E4),(0x9E2,prior.WORLD),(0x9E6,5),(0x9EA,5),(0x9EE,prior.MAP),(0x9F2,NAV),(0x15A4,TABLE),(0x15A8,256),(0x15AC,256)]:put(w,o,v)
    session=memory[prior.SESSION];session.extend(bytes(0x300-len(session)));put(session,0x10,PROVIDER)
    cells=bytearray(5*5*16)
    for i in range(25):put(cells,i*16+8,2,2);cells[i*16+10]=1;cells[i*16+12]=0xA5
    x,y=c['xy'];center=16*(y*5+x)
    put(cells,center,100,2);put(cells,center+2,1,2);put(cells,center+8,c['owner'],2);cells[center+10]=terrain_id
    for i,(dx,dy) in enumerate(DIRS):
        xx,yy=x+dx,y+dy
        if not(0<=xx<5 and 0<=yy<5):continue
        at=16*(yy*5+xx)
        cells[at+10]=terrain_id if c['mask']&(1<<i) else 1
        put(cells,at+8,c['neighbour_owner']|c['high_owner'],2);put(cells,at+2,c['neighbour_room'],2)
    memory[prior.MAP]=cells
    put(memory[prior.ACTOR],0x1A,(x<<12)+0x800);put(memory[prior.ACTOR],0x1E,(y<<12)+0x800)
    memory[prior.ROOM][0x5B:0x5E]=bytes([255,x,y])
    memory[prior.ROOM][0x18]=c['room_id']
    memory[OTHERROOM]=bytearray(memory[prior.ROOM]);put(memory[OTHERROOM],4,4,2)
    put(memory[0x6EE540],4*4,OTHERROOM);memory[0x6ED540][4]=1
    table=bytearray(256*460)
    for i in range(256):table[i*460+0x1C4]=i
    memory[TABLE]=table
    cache=bytearray(1024)
    for i in range(256):put(cache,i*4,TABLE+i*460)
    memory[0x6DA4A8]=cache;memory[0x6DA498]=bytearray(struct.pack('<I',prior.WORLD+0x9DA))
    memory[0x6DA490]=bytearray(struct.pack('<I',5))
    raw=bytearray(raw);raw[0x395]=c['mode']
    memory[ROOMRAW]=raw+bytes([165])*64;memory[ROOMMID]=bytearray([165])*(1267+64)
    memory[prior.ROOMDEF]=bytearray([165])*(1158+64)
    tr=next(r for s in fmt.parse((prior.SOURCE.parent/'Terrain.kwd').read_bytes()) if fmt.FAMILIES.get(s.signature)=='Terrain' for r in s.records if r[0x1D6]==terrain_id)
    memory[TERRAINRAW]=bytearray(tr)+bytes([165])*64;memory[TERRAINMID]=bytearray([165])*(736+64)
    memory[0x6FCDCF]=bytearray(4)
    nav=bytearray(32);put(nav,8,5);put(nav,12,5);put(nav,16,BITS);memory[NAV]=nav;memory[BITS]=bytearray([c['initial_nav']])*25
    memory[PROVIDER]=bytearray(struct.pack('<I',PVT));vt=bytearray(0x94);put(vt,0x28,QUEUE);put(vt,0x54,LOOKUP);put(vt,0x90,VFX);memory[PVT]=vt
    memory[IO]=bytearray(28)
    # Session-provider slot54 has SIX arguments (Room4EBE75..89), unlike
    # the separate graphics resource-provider slot54 used with param4!=NULL.
    memory[LOOKUP]=bytearray(bytes.fromhex('ff05')+struct.pack('<I',IO)+bytes.fromhex('8b54241066c702000033c0c21800'))
    cb=bytearray(bytes.fromhex('ff05')+struct.pack('<I',IO+4)+bytes.fromhex('8b542404'))
    for o in range(0,16,4):cb+=bytes.fromhex('8b42')+bytes([o])+b'\xA3'+struct.pack('<I',IO+8+o)
    cb+=bytes.fromhex('b801000000c20400');memory[QUEUE]=cb
    memory[VFX]=bytearray(bytes.fromhex('ff05')+struct.pack('<I',IO+24)+bytes.fromhex('33c0c21800'))
    calls += [dict(address='0x56eb60',registers=dict(ECX=hex(TERRAINMID)),stack_args=[]),
              dict(address='0x56ed20',registers=dict(ECX=hex(TERRAINMID)),stack_args=[TERRAINRAW]),
              dict(address='0x4f66e0',registers=dict(ECX=hex(TABLE+terrain_id*460)),stack_args=[TERRAINMID,0]),
              dict(address='0x4eab70',registers=dict(ECX=hex(prior.ROOMDEF)),stack_args=[]),
              dict(address='0x56de40',registers=dict(ECX=hex(ROOMMID)),stack_args=[]),
              dict(address='0x56dfd0',registers=dict(ECX=hex(ROOMMID)),stack_args=[ROOMRAW]),
              dict(address='0x4eacc0',registers=dict(ECX=hex(prior.ROOMDEF)),stack_args=[ROOMMID,prior.WORLD,0])]
    if not baseline:calls.append(dict(address='0x450470',registers=dict(ECX=hex(prior.WORLD+0x9DA)),stack_args=[x,y,0]))
    args=dict(program='DKII-DX.exe',address='0x472930' if c['entry'] and not baseline else '0x508df0',
        registers=json.dumps(dict(ECX=hex(prior.CONTEXT if c['entry'] and not baseline else prior.WORLD))),
        before_calls=calls,memory=json.dumps(dict(regions=[region(a,b) for a,b in memory.items()])),max_steps=700000,
        observe_reads=[[ROOMRAW,1055],[ROOMMID,1267],[prior.ROOMDEF,1158],[prior.MAP,len(cells)],[0,4096]],
        observe_writes=[[prior.MAP,len(cells)],[BITS,25],[prior.ROOMDEF,1222],[prior.SESSION,0x300],[0,4096]],
        allowed_external_IO_code=[[a,len(memory[a])] for a in [prior.SCB,prior.PLAY,LOOKUP,QUEUE,VFX]])
    spans=sorted((a,len(b)) for a,b in memory.items());assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:]))
    checks={a:bytes(b) for a,b in memory.items() if a not in [prior.SCB,prior.PLAY,LOOKUP,QUEUE,VFX,0x7FFEE000]}
    return args,checks

def execute(c,baseline=False):
    args,checks=build(c,baseline)
    key=sha((json.dumps(args,sort_keys=True)+sha((ROOT/'native_unicorn_factorytrace.py').read_bytes())).encode())[:12]
    path=ROOT/'ghidra'/f'phase137-room-adjacency-native-{key}.json'
    if path.exists():saved=json.loads(path.read_text(encoding='utf8'));assert saved['input']==args
    else:
        result=cpu.run(EXE,args,list(checks.items()),prior.STOP,prior.ROOMDEF,1222)
        saved=dict(case=c,baseline=baseline,input=args,result=result);path.write_text(json.dumps(saved,indent=2),encoding='utf8')
    r=saved['result']
    if not r['success']:print(json.dumps(dict(case=c['name'],error=r['emulation_error'],registers=r['registers'],tail=r['trace_tail'],calls=r['before_calls']),ensure_ascii=False),flush=True)
    assert r['success'] and r['hit_return'] and not r['protected_write_or_execution_failures']
    return args,checks,saved,path

def main():
    selected=cases()
    if len(sys.argv)>1:selected=[c for c in selected if c['name']==sys.argv[1]];assert selected
    for c in selected:
        args,checks,saved,path=execute(c,baseline='--baseline' in sys.argv)
        post=post_context(checks,saved['result']);at=16*(c['xy'][1]*5+c['xy'][0])
        print(json.dumps(dict(case=c['name'],file=path.name,cellC=post[prior.MAP][at+12],mode=post[prior.ROOMDEF][0x452],id=post[prior.ROOMDEF][0x44D],io=list(struct.unpack('<7I',post[IO])),hp=int.from_bytes(post[prior.ACTOR][0x40:0x44],'little'),returnEAX=saved['result']['registers']['EAX'])),flush=True)

if __name__=='__main__':main()
