"""Unpublished discovery of complete Shot ticks with area-process combinations.

Reuse accepted phase148 prepared scene and original loaders/factory/tick.
Original process table initializer is now composed before selected ticks.
No game instructions, vtables or source files are replaced or changed.
"""
from pathlib import Path
import hashlib,json,struct,sys
import probe_shot_lifecycle as p
import native_shot_pair_trace as cpu
import dk2_formats as fmt
from emulate_element_colors import region
from emulate_resistance import post_context
ROOT=Path(__file__).resolve().parent
sha=lambda b:hashlib.sha256(b).hexdigest()
EXE=p.area.EXE
NAV,NAVDATA,ALERTS=0x1B050000,0x1B060000,0x1B070000

def build(c):
    args,_=p.build(dict(mode=c.get('mode',2),root='tick',damage=c.get('data1',80),radius=c.get('data2',2),pre_ticks=0,process_none=False))
    mem={int(x['address'],16):bytearray.fromhex(x['hex']) for x in json.loads(args['memory'])['regions']}
    mem[p.base.RAW][0xC7]=c.get('process',30)
    p.area.put(mem[p.base.RAW],0xA0,c.get('mask',2))
    if c.get('source_record'):
        q=c['source_record'];source=bytes(next(s.records[q['record_index']] for s in fmt.parse(Path(q['file']).read_bytes()) if s.signature==0x104))
        assert (source[0xC7],source[0xC5])==(c['process'],c['mode'])
        mem[p.base.RAW]=bytearray(source)
        args['before_calls'][8]['stack_args'][0]=source[0xBE]
    if 'target_flags' in c:p.area.put(mem[p.base.target.RAW],0xEFC,c['target_flags'])
    if 'target_flag_set' in c:
        flags=int.from_bytes(mem[p.base.target.RAW][0xEFC:0xF00],'little')
        p.area.put(mem[p.base.target.RAW],0xEFC,(flags|c['target_flag_set'])&~c.get('target_flag_clear',0))
    # Prepared consistent player tags; original relation lookup reads these.
    p.area.put(mem[p.f.PLAYER],4,7,2);p.area.put(mem[p.base.OTHEROWNER],4,8,2)
    mem[ALERTS]=bytearray(2)
    for player in [p.f.PLAYER,p.base.OTHEROWNER]:p.area.put(mem[player],0x10FD,ALERTS)
    origin=struct.unpack_from('<3i',mem[p.f.ARGS]);tags=[2 if c.get('jack_target') else 101,103]
    if c.get('jack_target'):
        p.area.put(mem[0x6EE540],101*4,0);mem[0x6ED540][101]=0
    for i,a in enumerate(p.area.TARGETS[:2]):
        dx,dy=c.get('positions',[[0,0],[1024,0]])[i]
        struct.pack_into('<3i',mem[a],0x1A,origin[0]+dx,origin[1]+dy,origin[2])
        owner=c.get('owners',[8,8])[i]
        p.area.put(mem[a],4,tags[i],2);p.area.put(mem[a],0x28,owner,2)
        p.area.put(mem[a],0x2A,p.f.PLAYER if owner==7 else p.base.OTHEROWNER)
        p.area.put(mem[a],0x445,c.get('invulnerable',[0,0])[i],2)
        p.area.put(mem[0x6EE540],tags[i]*4,a);mem[0x6ED540][tags[i]]=1
    for o in range(0,len(mem[p.f.MAP]),16):p.area.put(mem[p.f.MAP],o,0,2)
    tails={}
    for a,tag in [(p.base.cf.ACTOR,100)]+[(p.area.TARGETS[i],tags[i]) for i in c.get('order',[0,1])]:
        p.area.put(mem[a],0xA,0,2);p.area.put(mem[a],0xC,0,2)
        x,y=struct.unpack_from('<2i',mem[a],0x1A);cell=((y>>12)*32+(x>>12))*16
        assert 0<=cell<len(mem[p.f.MAP])
        if cell in tails:
            prev,pt=tails[cell];p.area.put(mem[prev],0xC,tag,2);p.area.put(mem[a],0xA,pt,2)
        else:p.area.put(mem[p.f.MAP],cell,tag,2)
        tails[cell]=(a,tag)
    # OriginalWorld509200 reads a separate navigation byte grid. The frozen
    # phase148 NONE scene did not need it. A real prepared object is required;
    # reads of low-address zero padding are never accepted as a valid grid.
    mem[NAV]=bytearray(24);mem[NAVDATA]=bytearray(32*32)
    p.area.put(mem[NAV],8,32);p.area.put(mem[NAV],16,NAVDATA)
    p.area.put(mem[p.f.WORLD],0x9F2,NAV)
    if c.get('blocked_cells'):
        nav=int.from_bytes(mem[p.f.WORLD][0x9F2:0x9F6],'little');grid=mem[nav]
        width=int.from_bytes(grid[8:12],'little');data=int.from_bytes(grid[16:20],'little')
        for dx,dy in c['blocked_cells']:
            x,y=(origin[0]+dx)>>12,(origin[1]+dy)>>12
            mem[data][y*width+x]|=8
    args['before_calls'].append(dict(address='0x4ad1b0',registers=dict(ECX='0'),stack_args=[]))
    args['before_calls'] += [dict(address='0x4ab180',registers=dict(ECX=hex(p.base.NODE)),stack_args=[]) for _ in range(c.get('pre_ticks',0))]
    args['memory']=json.dumps(dict(regions=[region(a,b) for a,b in mem.items()]))
    args['observe_reads'] += [[0x69E860,39*17]]
    args['observe_writes'] += [[0x69E860,39*17],[p.base.cf.ACTOR,len(mem[p.base.cf.ACTOR])]]
    excluded=[0x7FFEE000,p.f.GFX_CB,p.f.SOUND_CB,p.base.target.attack.EVENT,p.base.target.LOAD_GFX,p.base.target.LOAD_SOUND,p.SOUND_UPDATE]
    checks={a:bytes(b) for a,b in mem.items() if a not in excluded}
    checks[p.base.SHOT_VTABLE]=p.area.pe_read(EXE.read_bytes(),p.base.SHOT_VTABLE,0x100)
    checks[0x69E860]=p.area.pe_read(EXE.read_bytes(),0x69E860,39*17)
    return args,checks

def execute(c):
    args,checks=build(c);tag=sha((json.dumps(args,sort_keys=True)+sha(Path(__file__).read_bytes())+sha((ROOT/'native_shot_pair_trace.py').read_bytes())).encode())[:12]
    file=ROOT/'ghidra'/f'phase169-area-pair-discovery-{c.get("process",30)}-{c.get("mode",2)}-{tag}.json'
    if file.exists():saved=json.loads(file.read_text(encoding='utf-8'));assert saved['input']==args
    else:
        r=cpu.run(EXE,args,list(checks.items()),p.f.OBS,p.base.NODE,371)
        saved=dict(case=c,input=args,result=r);file.write_text(json.dumps(saved,indent=2),encoding='utf-8')
    r=saved['result'];post=post_context(checks,r)
    out=dict(case=c,success=r['success'],error=r['emulation_error'],before=r['before_calls'],
        requests=r['hp_requests'],HP={hex(a):int.from_bytes(post[a][0x40:0x44],'little') for a in [p.base.cf.ACTOR]+p.area.TARGETS[:2]},
        calls={hex(a):r['visited_instructions'].get(hex(a),0) for a in [0x4AE990,0x4AF660,0x4AF8E0,0x4B02E0,0x4AD730,0x4ABD60,0x4AC3C0,0x4AC5A0,0x4AC6D0]},tail=r['trace_tail'])
    return args,checks,saved,file,out

if __name__=='__main__':
    c=dict(process=int(sys.argv[1]) if len(sys.argv)>1 else 30,mode=int(sys.argv[2]) if len(sys.argv)>2 else 2)
    print(json.dumps(execute(c)[-1]),flush=True)
