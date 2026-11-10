"""Discovery: realrawCF2/level-up -> completeworkcallback -> originalTerrainHP.

World/map/Actor prepared, nativeCMap and actualTerrain loader/getters. Raw
GlobalVariables action-health callbacks run. Externalresources/audio/visuals
declared as in priorlevel-up fixture. No ordinaryAI scheduling or GPU.
"""
from pathlib import Path
import json,struct
import dk2_formats as fmt
import probe_creature_work_level as old
import native_unicorn_keepertrace as cpu
from build_effect_semantics import EXE,pe_read
from emulate_element_colors import region
from emulate_resistance import post_context
ROOT=Path(__file__).resolve().parent
f,cf=old.f,old.cf
TABLE,TRAW,TMID,SVC,VT,CB,IO=0x1D000000,0x1D100000,0x1D110000,0x1D120000,0x1D121000,0x1D122000,0x1D123000
VRAW=0x1D130000
STOP=0x1D140000
sha=cpu.sha

def build(c):
    args,_=old.build(dict(name='seed',increment=c.get('increment',1024),modifier=c.get('modifier',4096),level=c.get('level',1)))
    mem={int(r['address'],16):bytearray.fromhex(r['hex']) for r in json.loads(args['memory'])['regions']}
    species=c.get('species',1);raw=bytearray(next(r for s in fmt.parse((ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/Creatures.kwd').read_bytes()) if s.signature==170 for r in s.records if r[0xF4B]==species))
    f.put(raw,0xCF2,c.get('increment',1024));f.put(raw,0xCEE,4096);f.put(raw,0xF0C,1000,2);mem[old.RAW]=raw+bytes([165])*64
    mem[cf.ACTOR][0x36F]=species;f.put(mem[cf.ACTOR],0x3B3,c.get('enhancement',0))
    # Actualworktarget is adjacent to preparedActor5.5/6.5, at6.5/6.5.
    struct.pack_into('<3I',mem[cf.ACTOR],0x38D,0x6800,0x6800,0)
    table=bytearray(256*460);cache=bytearray(1024)
    for i in range(256):table[i*460+0x1C4]=i;f.put(cache,4*i,TABLE+460*i)
    mem[TABLE]=table;mem[0x6DA4A8]=cache
    r=next(r for s in fmt.parse((ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/Terrain.kwd').read_bytes()) if s.signature==110 for r in s.records if r[0x1D6]==2)
    mem[TRAW]=bytearray(r)+bytes([165])*64;mem[TMID]=bytearray([165])*800
    mem[SVC]=bytearray(struct.pack('<I',VT));mem[VT]=bytearray(0x58);f.put(mem[VT],0x54,CB)
    cb=bytes.fromhex('ff05')+struct.pack('<I',IO)+bytes.fromhex('8b54241066c702000033c0c21800');mem[CB]=bytearray(cb);mem[IO]=bytearray(4)
    for off,v in [(0x9DA,0x66D3E4),(0x9E2,f.WORLD),(0x9E6,32),(0x9EA,32),(0x9EE,f.MAP),(0x15A4,TABLE),(0x15A8,256),(0x15AC,256)]:f.put(mem[f.WORLD],off,v)
    # NonlethalRock-onlypath: allcacheentriesexplicit, targetHPbelowrawmax660.
    cell=(6*32+6)*16;mem[f.MAP][cell+10]=2;f.put(mem[f.MAP],cell+6,c.get('HP',600),2);f.put(mem[f.MAP],cell+8,c.get('owner',7),2)
    for a,v in [(0x6DA490,32),(0x6DA49C,f.MAP),(0x6DA498,f.WORLD+0x9DA),(0x6DA4A4,f.WORLD),(0x6F2548,8),(0x6F2590,20),(0x6F295C,0),(0x75AE93,0)]:
        found=next((base for base,b in mem.items() if base<=a and a+4<=base+len(b)),None)
        if found is None:mem[a]=bytearray(struct.pack('<I',v))
        else:f.put(mem[found],a-found,v)
    variables=fmt.parse((ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/GlobalVariables.kwd').read_bytes())[0].records
    action_calls=[]
    for i,typ in enumerate([8,9,10]):
        rawvar=bytearray(next(r for r in variables if len(r)==16 and int.from_bytes(r[:4],'little')==typ))
        if 'action' in c:f.put(rawvar,4,c['action'])
        at=VRAW+4096*i;mem[at]=bytearray(16)+rawvar;action_calls.append(dict(address='0x51cb80',registers=dict(ECX='0'),stack_args=[at]))
    calls=[dict(address='0x56eb60',registers=dict(ECX=hex(TMID)),stack_args=[]),dict(address='0x56ed20',registers=dict(ECX=hex(TMID)),stack_args=[TRAW]),
        dict(address='0x4f66e0',registers=dict(ECX=hex(TABLE+2*460)),stack_args=[TMID,SVC])]+args['before_calls']+action_calls
    if c.get('level_up',True):calls.append(dict(address='0x48b120',registers=dict(ECX=hex(cf.ACTOR)),stack_args=[]))
    args.update(address='0x496e00',registers=json.dumps(dict(ECX=hex(cf.ACTOR))),stack_args=[],before_calls=calls,
        memory=json.dumps(dict(regions=[region(a,b) for a,b in mem.items()])),max_steps=3000000,
        allowed_external_IO_code=args['allowed_external_IO_code']+[[CB,len(cb)]],
        observe_reads=[[cf.DEFINITION,2199],[cf.ACTOR+0x3AF,8],[f.MAP,len(mem[f.MAP])],[0,4096]],
        observe_writes=[[cf.ACTOR,len(mem[cf.ACTOR])],[f.MAP,len(mem[f.MAP])],[0,4096]])
    excluded={old.sp.DRIVER,f.GFX_CB,f.SOUND_CB,old.LOOKUP,old.FXCB,CB,0x7FFEE000};checks={a:bytes(b) for a,b in mem.items() if a not in excluded}
    for a,n in [(0x66D524,0x130),(0x66E3EC,0x3A0),(0x66D3E4,0x10)]:checks[a]=pe_read(EXE.read_bytes(),a,n)
    spans=sorted((a,len(b)) for a,b in mem.items());assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:]))
    return args,checks

def execute(c):
    args,ch=build(c);backend=sha(Path(cpu.__file__).read_bytes());tag=sha((json.dumps(args,sort_keys=True)+backend+json.dumps({hex(a):sha(b) for a,b in ch.items()},sort_keys=True)).encode())[:12]
    path=ROOT/'ghidra'/f'phase163-creature-work-cell-probe-{tag}.json'
    if path.exists():saved=json.loads(path.read_text(encoding='utf8'));assert saved['input']==args
    else:
        saved=dict(case=c,input=args,backend_sha256=backend,result=cpu.run(EXE,args,list(ch.items()),STOP,cf.ACTOR,1343));path.write_text(json.dumps(saved,indent=2),encoding='utf8')
    return args,ch,saved,path

if __name__=='__main__':
    a,ch,s,p=execute(dict(name='base'));r=s['result'];print(p.name);print(json.dumps({k:r[k] for k in ['success','emulation_error','registers','trace_tail','before_calls']}))
    if r['success']:
        post=post_context(ch,r);at=(6*32+6)*16;print('CellHP',int.from_bytes(post[f.MAP][at+6:at+8],'little'),'Requests',r['terrain_requests'])
