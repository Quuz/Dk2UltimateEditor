"""Original rawShot/native factory and rawTerrain loaders -> originalmode8damage.

Preparedmap has actualTerrain IDs1/2/3,sourceRockHP165,ownerbits andnativecache.
No replacement damage code. ExtraCPUhook onlyrecords452F60 arguments.
"""
from pathlib import Path
import json,struct,sys
import probe_shot_lifecycle as life
import native_unicorn_tremortrace as cpu
from emulate_element_colors import region
from emulate_resistance import post_context
ROOT=Path(__file__).resolve().parent
f,base,area=life.f,life.base,life.area
TABLE=0x1E000000
MID0,RAW0=0x1E100000,0x1E110000
SVC,VT,CB,IO=0x1E200000,0x1E201000,0x1E202000,0x1E203000
NAV,BITS=0x1E300000,0x1E301000
TRIANGLES,NTRIANGLES=0x1E500000,2048
NAVGLOBALS,NAVSIZE=0x6E5AC0,0x7520
sha=area.sha

def build(c):
    args,_=life.build(dict(mode=8,root='tick' if c.get('tick') else 'factory',damage=c.get('damage',80),radius=c.get('data2',4096),pre_ticks=c.get('pre_ticks',0)))
    mem={int(r['address'],16):bytearray.fromhex(r['hex']) for r in json.loads(args['memory'])['regions']}
    area.put(mem[f.GV],0xD4,int.from_bytes(area.pe_read(area.EXE.read_bytes(),0x670574+0xD4,4),'little'))
    source=life.base.target.fmt.parse((ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/Terrain.kwd').read_bytes())
    records=next(s.records for s in source if s.signature==0x6E)
    table=bytearray(256*460)
    for i in range(256):table[i*460+0x1C4]=i
    mem[TABLE]=table;cache=bytearray(1024)
    for i in range(256):area.put(cache,4*i,TABLE+460*i)
    mem[0x6DA4A8]=cache;mem[0x6DA490]=bytearray(struct.pack('<I',32));mem[0x6DA49C]=bytearray(struct.pack('<I',f.MAP))
    for off,v in [(0x15A4,TABLE),(0x15A8,256),(0x15AC,256)]:area.put(mem[f.WORLD],off,v)
    # SourceRock has a resource branch that dereferences the supplied
    # service without checkingNULL. Declare its six-argument IO boundary.
    mem[SVC]=bytearray(struct.pack('<I',VT));mem[VT]=bytearray(0x58);area.put(mem[VT],0x54,CB)
    mem[IO]=bytearray(4);mem[CB]=bytearray(bytes.fromhex('ff05')+struct.pack('<I',IO)+bytes.fromhex('8b54241066c702000033c0c21800'))
    calls=[]
    for index,tid in enumerate([1,2,3]):
        raw=next(r for r in records if r[0x1D6]==tid)
        if tid==2 and 'destroyed_type' in c:raw=life.base.target.fmt.set_scalar(raw,'Terrain','DestroyedTypeTerrainId',c['destroyed_type'])
        mid= MID0+index*0x20000;at=RAW0+index*0x20000
        mem[at]=bytearray(raw)+bytes([165])*64;mem[mid]=bytearray([165])*(736+64)
        calls += [dict(address='0x56eb60',registers=dict(ECX=hex(mid)),stack_args=[]),dict(address='0x56ed20',registers=dict(ECX=hex(mid)),stack_args=[at]),dict(address='0x4f66e0',registers=dict(ECX=hex(TABLE+tid*460)),stack_args=[mid,SVC])]
    if 'origin_tile' in c:
        x,y=c['origin_tile'];struct.pack_into('<2i',mem[f.ARGS],0,(x<<12)+0x800,(y<<12)+0x800)
    origin=struct.unpack_from('<3i',mem[f.ARGS]);cx,cy=origin[0]>>12,origin[1]>>12
    for at in range(0,len(mem[f.MAP]),16):
        mem[f.MAP][at+10]=3;area.put(mem[f.MAP],at+6,1,2);area.put(mem[f.MAP],at+8,c.get('owner',0)|c.get('owner_high',0),2)
    for dx,dy,tid in c.get('tiles',[[-1,-1,2],[-1,0,2],[0,-1,2]]):
        x,y=cx+dx,cy+dy
        if not(0<=x<32 and 0<=y<32):continue
        at=(y*32+x)*16;mem[f.MAP][at+10]=tid;area.put(mem[f.MAP],at+6,c.get('health',165) if tid==2 else 1,2)
    # Lethal Terrain damage invokes the real CMap change route; aNULLCMap
    # is not a valid scene even when nonlethal directHPchanges can run.
    for off,v in [(0x9C4,f.WORLD),(0x9DA,0x66D3E4),(0x9E2,f.WORLD),(0x9E6,32),(0x9EA,32),(0x9EE,f.MAP),(0x9F2,NAV)]:area.put(mem[f.WORLD],off,v)
    mem[0x6DA498]=bytearray(struct.pack('<I',f.WORLD+0x9DA));mem[0x6DA4A4]=bytearray(struct.pack('<I',f.WORLD))
    mem[NAV]=bytearray(32)
    for off,v in [(8,32),(12,32),(16,BITS)]:area.put(mem[NAV],off,v)
    mem[BITS]=bytearray(1024)
    for i in range(1024):
        if mem[f.MAP][16*i+10] in [1,2]:mem[BITS][i]=8
    # Keep tag0 a NULL list terminator. SourceRock/Dirt are unclaimed;
    # owner0 is the coherent default for the full destruction route.
    assert int.from_bytes(mem[0x6EE540][:4],'little')==0
    if c.get('full_nav',True):
        pe_base,pe_image=cpu.pe_image(area.EXE.read_bytes())
        globals_=bytearray(pe_image[NAVGLOBALS-pe_base:NAVGLOBALS-pe_base+NAVSIZE])
        for a in list(mem):
            if NAVGLOBALS<=a<NAVGLOBALS+NAVSIZE:
                b=mem.pop(a);globals_[a-NAVGLOBALS:a-NAVGLOBALS+len(b)]=b
        mem[NAVGLOBALS]=globals_;size=4+29*NTRIANGLES;mem[TRIANGLES]=bytearray(size)+bytes([165])*64
        calls += [dict(address='0x4ce690',registers=dict(ECX='0'),stack_args=[TRIANGLES,size]),
            dict(address='0x4ce740',registers=dict(ECX='0'),stack_args=[BITS,32,32,32]),
            dict(address='0x4c8f30',registers=dict(ECX='0'),stack_args=[]),dict(address='0x4cef20',registers=dict(ECX='0'),stack_args=[])]
    if 0 not in mem:mem[0]=bytearray(4096);area.put(mem[0],0,0xFFFFFFFF)
    args['before_calls']=calls+args['before_calls']
    if c.get('long_run'):
        args['max_steps']=20000000;args['timeout_us']=45000000
    args['allowed_external_IO_code'].append([CB,len(mem[CB])])
    args['address']='0x4ab180' if c.get('tick') else '0x4ac3c0';args['registers']=json.dumps(dict(ECX=hex(base.NODE)))
    args['memory']=json.dumps(dict(regions=[region(a,b) for a,b in mem.items()]))
    args['observe_reads'] += [[f.MAP,len(mem[f.MAP])],[TABLE,len(table)],[0,4096]]
    args['observe_writes'] += [[f.MAP,len(mem[f.MAP])],[TABLE,len(table)],[0,4096]]
    spans=sorted((a,len(b)) for a,b in mem.items())
    assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:])),[(hex(a),n,hex(b)) for (a,n),(b,_) in zip(spans,spans[1:]) if a+n>b]
    excluded=[0x7FFEE000,f.GFX_CB,f.SOUND_CB,base.target.attack.EVENT,base.target.LOAD_GFX,base.target.LOAD_SOUND,life.SOUND_UPDATE,CB]
    checks={a:bytes(b) for a,b in mem.items() if a not in excluded}
    return args,checks

def execute(c,verbose=True):
    args,checks=build(c);backend=sha(Path(cpu.__file__).read_bytes());tag=sha((json.dumps(args,sort_keys=True)+backend).encode())[:12];p=ROOT/'ghidra'/f'phase151-tremor-native-{tag}.json'
    if p.exists():saved=json.loads(p.read_text(encoding='utf8'));assert saved['input']==args and saved['backend_sha256']==backend
    else:
        r=cpu.run(area.EXE,args,list(checks.items()),f.OBS,base.NODE,371);saved=dict(case=c,input=args,backend_sha256=backend,result=r);p.write_text(json.dumps(saved,indent=2),encoding='utf8')
    if verbose:
        r=saved['result'];post=post_context(checks,r)
        print(json.dumps(dict(case=c,success=r['success'],error=r['emulation_error'],before_calls=r['before_calls'],terrain=r['terrain_requests'],hp_requests=r['hp_requests'],tail=r['trace_tail'],
            flags=[hex(int.from_bytes(post[TABLE][i*460+0x17B:i*460+0x17F],'little')) for i in [1,2,3]])),flush=True)
    return args,checks,saved,p

if __name__=='__main__':execute(dict(tick='tick' in sys.argv))
