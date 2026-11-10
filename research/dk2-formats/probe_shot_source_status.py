"""Unchanged source Freeze/Poison records and actual Crystal children.

Prepared original class context from phase172; original loaders, targeted
Trap17 firing and whole Shot methods. Host selects subsequent object ticks.
No source files, gameplay instructions or vtables are changed.
"""
from pathlib import Path
import json,struct,sys
import probe_shot_instant_experience as old
from emulate_element_colors import region
from emulate_resistance import post_context
ROOT=Path(__file__).resolve().parent
EXE=old.EXE
sha=old.sha
p=old.target.p
DEFS,RAWS,MID=0x20400000,0x20500000,0x20600000
ARENAS=0x20700000
SOURCE=ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/maps/NeedsTestGlobals.kld'

def source(c):
    file=Path(c.get('source_file',str(SOURCE)))
    raws={r[0xBE]:r for s in old.target.fmt.parse(file.read_bytes()) if s.signature==260 for r in s.records}
    ids=sorted(c.get('IDs',[162,163,164]))
    return file,ids,{i:raws[i] for i in ids}

def build(c):
    args,_,node,actors=old.build(dict(process=34,mode=2))
    mem={int(r['address'],16):bytearray.fromhex(r['hex']) for r in json.loads(args['memory'])['regions']}
    file,ids,raws=source(c);parent=raws[c.get('parent',162)]
    mem[DEFS]=bytearray([165])*(len(ids)*118+64)
    mem[RAWS]=bytearray(b''.join(raws[i] for i in ids));mem[MID]=bytearray([165])*(375+64)
    mem[p.shot.RAW]=bytearray(parent);mem[old.trap.TRAW][0x20D]=parent[0xBE]
    for off,src in [(0x1E0,0x98),(0x1E4,0x9C)]:p.f.put(mem[old.trap.TRAW],off,int.from_bytes(parent[src:src+4],'little'))
    for name,off in [('trap_data1',0x1E0),('trap_data2',0x1E4)]:
        if name in c:p.f.put(mem[old.trap.TRAW],off,c[name])
    for off,value in [(0x1624,DEFS),(0x1628,len(ids)),(0x162C,len(ids))]:p.f.put(mem[p.f.WORLD],off,value)
    for actor in actors:
        definition=int.from_bytes(mem[actor][0x370:0x374],'little');p.f.put(mem[actor],0x6C,definition+27)
        p.f.put(mem[actor],0x40,c.get('HP',100000))
    if c.get('invulnerable'):p.f.put(mem[actors[1]],0x445,c['invulnerable'],2)
    previous=old.trap.TBLOCK
    for i in range(16):
        address=ARENAS+i*4096;mem[address]=bytearray([165])*768
        struct.pack_into('<III',mem[address],0,512,previous,p.f.FT)
        p.f.put(mem[previous],8,address);previous=address
    p.f.put(mem[p.f.FT],4,previous)
    calls=[];replaced=False
    for call in args['before_calls']:
        if call['address']=='0x4acd60':
            assert not replaced;replaced=True
            for i,ID in enumerate(ids):
                address=DEFS+118*i
                calls += [dict(address='0x4acd60',registers=dict(ECX=hex(address)),stack_args=[]),
                    dict(address='0x56e540',registers=dict(ECX=hex(MID)),stack_args=[]),
                    dict(address='0x56e690',registers=dict(ECX=hex(MID)),stack_args=[RAWS+239*i]),
                    dict(address='0x4acdf0',registers=dict(ECX=hex(address)),stack_args=[MID,old.trap.p.area.GFX])]
        elif call['address'] not in ['0x56e540','0x56e690','0x4acdf0']:calls.append(call)
    assert replaced
    planned=[node]*c.get('parent_ticks',1)
    for index,count in c.get('child_ticks',[]):planned += [ARENAS+index*4096+0x8C]*count
    assert planned
    calls += [dict(address='0x4ab180',registers=dict(ECX=hex(a)),stack_args=[]) for a in planned[:-1]]
    args['before_calls']=calls;args['address']='0x4ab180';args['registers']=json.dumps(dict(ECX=hex(planned[-1])))
    readable=old.readable;mem[readable.COUNTER]=bytearray(8)
    mem[readable.ADDRESS]=bytearray(readable.adapter(readable.allowed_ranges(EXE.read_bytes(),[region(a,b) for a,b in mem.items() if a!=readable.ADDRESS])))
    args['allowed_external_IO_code']=[r for r in args['allowed_external_IO_code'] if r[0]!=readable.ADDRESS]+[[readable.ADDRESS,len(mem[readable.ADDRESS])]]
    args['memory']=json.dumps(dict(regions=[region(a,b) for a,b in mem.items()]))
    args['observe_reads'] += [[DEFS,len(mem[DEFS])],[RAWS,len(mem[RAWS])]]
    args['observe_writes'] += [[a,len(b)] for a,b in mem.items() if ARENAS<=a<ARENAS+16*4096]+[[DEFS,len(mem[DEFS])]]
    excluded={0x7FFEE000,p.f.GFX_CB,p.f.SOUND_CB,p.target.attack.EVENT,p.target.LOAD_GFX,p.target.LOAD_SOUND,old.trap.p.SOUND_UPDATE,old.trap.LOOKUP,readable.ADDRESS,0x28CFA4}
    checks={a:bytes(b) for a,b in mem.items() if a not in excluded}
    for a,n in [(0x66D524,0x130),(0x66D754,0x100),(0x66D86C,0x100),(0x66E3EC,0x3A0),(0x670574,0x200)]:checks[a]=p.pe_read(EXE.read_bytes(),a,n)
    return args,checks,node,actors

def execute(c):
    args,checks,node,actors=build(c)
    tag=sha((json.dumps(args,sort_keys=True)+sha(Path(__file__).read_bytes())+sha((ROOT/'native_shot_pair_trace.py').read_bytes())).encode())[:12]
    file=ROOT/'ghidra'/f'phase175-source-status-discovery-{tag}.json'
    if file.exists():saved=json.loads(file.read_text(encoding='utf-8'));assert saved['input']==args
    else:
        r=old.target.cpu.run(EXE,args,list(checks.items()),p.f.OBS,node,371);saved=dict(case=c,input=args,result=r)
        file.write_text(json.dumps(saved,indent=2),encoding='utf-8')
    post=post_context(checks,saved['result']);return args,checks,saved,file,post,node,actors

def inspect(post,actors):
    u=lambda b,o,n=4:int.from_bytes(b[o:o+n],'little');shots=[];registry=post[0x6EE540]
    for tag in range(202,u(post[0x6A4818],4)):
        address=u(registry,tag*4)
        b=next((b[address-a:address-a+371] for a,b in post.items() if a<=address<address+371<=a+len(b)),None)
        if b is not None:shots.append(dict(tag=tag,address=address,ID=b[0x124],HP=u(b,0x40),tick=u(b,0x12D),Data1=u(b,0x13D),Data2=u(b,0x141),target=u(b,0x147,2)))
    return dict(HP=[u(post[a],0x40) for a in actors],freeze=[u(post[a],0x447,2) for a in actors],
        poison=[u(post[a],0x45D,2) for a in actors],severity=[u(post[a],0x45F,2) for a in actors],shots=shots)

def describe(c):
    args,ch,saved,file,post,node,actors=execute(c);r=saved['result']
    print(json.dumps(dict(case=c,success=r['success'],error=r['emulation_error'],outcome=inspect(post,actors),
        requests=r['hp_requests'],before=r['before_calls'][-5:],tail=r['trace_tail'][-12:],
        low=[x for x in r['observed_reads']+r['observed_writes'] if 0<int(x['address'],16)<4096][:8])),flush=True)

if __name__=='__main__':describe(json.loads(sys.argv[1]) if len(sys.argv)>1 else {})
