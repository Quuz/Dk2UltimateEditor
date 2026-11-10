"""Exploration: complete original51EC60 callback, native room creation/join.

No accepted specification until independent oracles/attestation. Runtime world,
free room-slot list and terrain/room catalogues prepared; original constructors
and placement routines execute. Original code/vtables remain unchanged.
"""
from pathlib import Path
import json,struct,sys
import emulate_room_adjacency as old
from emulate_element_colors import region
from emulate_resistance import post_context
import native_unicorn_factorytrace as cpu
from build_effect_semantics import EXE
import dk2_formats as fmt
from native_map_cells import intermediate_cell
from object_state_tables import BASE as OBJECT_STATES,SIZE as OBJECT_STATE_BYTES

ROOT=Path(__file__).resolve().parent
PLAYERS,CLEARANCE,CELLS_MID,KINDS=0x18200000,0x18220000,0x18230000,0x18240000
ROOM2=0x18250000
ROOMRAW2,ROOMMID2=0x18260000,0x18270000
TERRAW2,TERMID2=0x18280000,0x18290000
OVERLAY=0x182A0000
OBJDEF,OBJRAW,OBJMID=0x18300000,0x18310000,0x18320000
BLOCK,FH,FT,UH,UT=0x18400000,0x18410000,0x18411000,0x18412000,0x18413000
GFXNULL=0x18420000
SOUND_RELEASE=0x18421000
put=old.put

def build(points=None,flags=0):
    p=old.prior
    c=next(c for c in old.cases() if c['name']=='all')
    args,_=old.build(c)
    memory={int(r['address'],16):bytearray.fromhex(r['hex']) for r in json.loads(args['memory'])['regions']}
    points=[(2,2)] if points is None else points
    memory[old.PVT].extend(bytes(0xD8-len(memory[old.PVT])));put(memory[old.PVT],0xD4,OVERLAY)
    memory[OVERLAY]=bytearray(bytes.fromhex('33c0c20800'))
    args['allowed_external_IO_code'].append([OVERLAY,5])
    memory[GFXNULL]=bytearray(bytes.fromhex('33c0c21400'));put(memory[old.PVT],0xA0,GFXNULL)
    args['allowed_external_IO_code'].append([GFXNULL,5])
    memory[p.SV].extend(bytes(0x3C-len(memory[p.SV])));put(memory[p.SV],0x38,SOUND_RELEASE)
    memory[SOUND_RELEASE]=bytearray(bytes.fromhex('33c0c20800'));args['allowed_external_IO_code'].append([SOUND_RELEASE,5])
    calls=args['before_calls'][:-1]
    # Two real Room constructors obtain tags1000/1001. Manager's bounded
    # free-list input selects1000 first. This does not execute 672-slot malloc.
    memory[p.ROOM]=bytearray([165])*(169+64);memory[ROOM2]=bytearray([165])*(169+64)
    memory[0x6A4818]=bytearray(struct.pack('<3I',3094,1000,200))
    memory[0x6F2540]=bytearray(4);memory[0x6E504C]=bytearray(struct.pack('<I',KINDS));memory[KINDS]=bytearray(20)
    for tag in [1,4,100]:put(memory[0x6EE540],tag*4,0);memory[0x6ED540][tag]=0
    world=memory[p.WORLD]
    for o,v,n in [(0x1C,p.WORLD,4),(0x36,5,4),(0x6E,UT,4),(0x72,FH,4),(0x76,0,4),
        (0x84,PLAYERS,4),(0x15B4,p.ROOMDEF,4),(0x15B8,2,4),(0x15BC,2,4),
        (0x9CC,0,2),(0x9CE,1000,2),(0x9D0,0,4),(0x9D4,0,4),(0x9D8,0,2),
        (0x9FA,CLEARANCE,4),(0x7239,0x40,1),(0x7243,0xFFFF,2)]:put(world,o,v,n)
    memory[p.ROOMDEF].extend(bytes(1158*2-len(memory[p.ROOMDEF])+64))
    memory[BLOCK]=bytearray([165])*(16384+24+64)
    put(world,0x4A,BLOCK);put(world,0x4E,16384+24)
    calls.append(dict(address='0x554ad0',registers=dict(ECX=hex(p.WORLD+0x46)),stack_args=[]))
    objects=sorted([r for s in fmt.parse((p.SOURCE.parent/'Objects.kwd').read_bytes()) if fmt.FAMILIES.get(s.signature)=='Objects' for r in s.records if r[0x35A] in [72,73,97,98,111,132]],key=lambda r:r[0x35A])
    assert len(objects)==6
    memory[OBJDEF]=bytearray(285*6);memory[OBJRAW]=bytearray().join(objects);memory[OBJMID]=bytearray([165])*(1022*6)
    memory[OBJECT_STATES]=bytearray(OBJECT_STATE_BYTES)
    memory[0x6F2550]=bytearray(4096);put(memory[0x6F2550],0x40,20)
    for o,v in [(0x15F4,OBJDEF),(0x15F8,6),(0x15FC,6)]:put(world,o,v)
    calls.append(dict(address='0x49e600',registers=dict(ECX='0'),stack_args=[]))
    for i in range(6):
        calls += [dict(address='0x56d4f0',registers=dict(ECX=hex(OBJMID+i*1022)),stack_args=[]),
            dict(address='0x56d620',registers=dict(ECX=hex(OBJMID+i*1022)),stack_args=[OBJRAW+i*894]),
            dict(address='0x49dd60',registers=dict(ECX=hex(OBJDEF+i*285)),stack_args=[OBJMID+i*1022,0])]
    # A canonical second catalogue definition lets the native sorted lookup
    # scan forward/backward correctly: Temple13 then CombatPit16.
    raw=next(r for s in fmt.parse((p.SOURCE.parent/'Rooms.kwd').read_bytes()) if fmt.FAMILIES.get(s.signature)=='Rooms' for r in s.records if r[0x392]==16)
    memory[ROOMRAW2]=bytearray(raw);memory[ROOMMID2]=bytearray([165])*1267
    calls += [dict(address='0x4eab70',registers=dict(ECX=hex(p.ROOMDEF+1158)),stack_args=[]),
        dict(address='0x56de40',registers=dict(ECX=hex(ROOMMID2)),stack_args=[]),
        dict(address='0x56dfd0',registers=dict(ECX=hex(ROOMMID2)),stack_args=[ROOMRAW2]),
        dict(address='0x4eacc0',registers=dict(ECX=hex(p.ROOMDEF+1158)),stack_args=[ROOMMID2,p.WORLD,0])]
    # Ground3 is the initial map terrain; raw Temple22 is inherited above.
    raw=next(r for s in fmt.parse((p.SOURCE.parent/'Terrain.kwd').read_bytes()) if fmt.FAMILIES.get(s.signature)=='Terrain' for r in s.records if r[0x1D6]==3)
    memory[TERRAW2]=bytearray(raw);memory[TERMID2]=bytearray([165])*736
    calls += [dict(address='0x56eb60',registers=dict(ECX=hex(TERMID2)),stack_args=[]),
        dict(address='0x56ed20',registers=dict(ECX=hex(TERMID2)),stack_args=[TERRAW2]),
        dict(address='0x4f66e0',registers=dict(ECX=hex(old.TABLE+3*460)),stack_args=[TERMID2,0])]
    # Slot2 means PlayerId2; the Player WORD tag2 drives actual owner lists.
    players=bytearray(8*0x1125)
    for ident in range(1,9):put(players,(ident-1)*0x1125+4,ident,2)
    put(players,0x1125+0x19A,1);memory[PLAYERS]=players
    put(memory[0x6EE540],2*4,PLAYERS+0x1125)
    for g in [0x6E5050,0x6E5054,0x6ED484]:memory[g]=bytearray(struct.pack('<I',p.WORLD))
    cells=bytearray(25*16)
    for i in range(25):put(cells,i*16+8,2,2);cells[i*16+10]=3
    memory[p.MAP]=cells;memory[CLEARANCE]=bytearray(25*2)
    memory[old.BITS]=bytearray(25)
    # Full selected NAV globals plus native-sized storage for 2*25 triangles.
    # Use original PE/BSS prestate and preserve all inherited explicit inputs.
    base,image=cpu.pe_image(EXE.read_bytes());navstart,navsize=0x6E5AC0,0x7520
    globals_=bytearray(image[navstart-base:navstart-base+navsize])
    for a in list(memory):
        if navstart<=a<navstart+navsize:
            payload=memory.pop(a);globals_[a-navstart:a-navstart+len(payload)]=payload
    memory[navstart]=globals_
    memory[p.TRIANGLES]=bytearray(1454)+bytes([165])*64
    calls=[call for call in calls if call['address']!='0x4ce690']
    calls += [dict(address='0x4ce690',registers=dict(ECX='0'),stack_args=[p.TRIANGLES,1454]),
        dict(address='0x4ce740',registers=dict(ECX='0'),stack_args=[old.BITS,5,5,5]),
        dict(address='0x4c8f30',registers=dict(ECX='0'),stack_args=[]),
        dict(address='0x4cef20',registers=dict(ECX='0'),stack_args=[])]
    # Original constructor registers exact native Room vtable and tag.
    calls += [dict(address='0x4e3790',registers=dict(ECX=hex(p.ROOM)),stack_args=[]),
        dict(address='0x4e3790',registers=dict(ECX=hex(ROOM2)),stack_args=[])]
    # Constructor-produced free slots have no list links until manager setup.
    # Explicit singleton input avoids patching instructions after constructors.
    # First slot's next=0 is sufficient to create one connected room.
    memory[CELLS_MID]=bytearray().join(intermediate_cell(bytes([22,2,flags,0])) for _ in points)+bytes([165])*64
    for i,(x,y) in enumerate(points):calls.append(dict(address='0x51ec60',registers=dict(ECX='0'),stack_args=[x,y,CELLS_MID+16*i,p.WORLD]))
    args.update(address='0x508df0',registers=json.dumps(dict(ECX=hex(p.WORLD))),before_calls=calls,max_steps=1500000,
        observe_reads=[[old.ROOMRAW,1055],[p.ROOMDEF,len(memory[p.ROOMDEF])],[CELLS_MID,len(memory[CELLS_MID])],[p.MAP,len(cells)],[0,4096]],
        observe_writes=[[p.MAP,len(cells)],[p.ROOM,233],[ROOM2,233],[p.WORLD,len(world)],[0,4096]])
    args['memory']=json.dumps(dict(regions=[region(a,b) for a,b in memory.items()]))
    checks={a:bytes(b) for a,b in memory.items() if a not in [p.SCB,p.PLAY,old.LOOKUP,old.QUEUE,old.VFX,OVERLAY,GFXNULL,SOUND_RELEASE,0x7FFEE000]}
    spans=sorted((a,len(b)) for a,b in memory.items());assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:]))
    return args,checks

def execute(points=None,flags=0,verbose=True):
    args,checks=build(points,flags);key=old.sha(json.dumps(args,sort_keys=True).encode())[:12]
    path=ROOT/'ghidra'/f'phase138-map-room-discovery-{key}.json'
    if path.exists():saved=json.loads(path.read_text(encoding='utf8'));assert saved['input']==args
    else:
        r=cpu.run(EXE,args,list(checks.items()),old.prior.STOP,old.prior.ROOM,233)
        saved=dict(input=args,points=points,flags=flags,result=r);path.write_text(json.dumps(saved,indent=2),encoding='utf8')
    r=saved['result']
    if verbose:print(json.dumps({k:r[k] for k in ['success','emulation_error','registers','trace_tail','before_calls','protected_write_or_execution_failures']})[:3600],flush=True)
    if r['success'] and verbose:
        post=post_context(checks,r);print('ROOM',post[old.prior.ROOM][:169].hex(),'CELLS',post[old.prior.MAP].hex(),flush=True)
    return args,checks,saved,path

if __name__=='__main__':execute([(x,y) for y in range(1,4) for x in range(1,4)] if 'square' in sys.argv else [(2,2),(3,2)] if 'pair' in sys.argv else None)
