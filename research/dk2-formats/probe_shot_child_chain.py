"""Phase174 discovery: unchanged Shot152 ->153 and158 ->49 parameter chain.

Original raw definition loaders, original Trap targeted firing and whole Shot
ticks. Four sorted source definitions only; explicit spare512B arena blocks.
No gameplay functions/vtables changed; this is not the whole World scheduler.
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
DEFS,RAWS,MID=0x20000000,0x20100000,0x20200000
ARENAS=0x20300000
IDS=[49,152,153,158]
SOURCE=ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/maps/NeedsTestGlobals.kld'

def build(c):
    args,_,node,actors=old.build(dict(process=34,mode=2))
    mem={int(r['address'],16):bytearray.fromhex(r['hex']) for r in json.loads(args['memory'])['regions']}
    raws={r[0xBE]:r for s in old.target.fmt.parse(SOURCE.read_bytes()) if s.signature==260 for r in s.records}
    mem[DEFS]=bytearray([165])*(len(IDS)*118+64);mem[RAWS]=bytearray(b''.join(raws[i] for i in IDS));mem[MID]=bytearray([165])*(375+64)
    parent=raws[152]
    mem[p.shot.RAW]=bytearray(parent)
    mem[old.trap.TRAW][0x20D]=152
    for off,src in [(0x1E0,0x98),(0x1E4,0x9C)]:p.f.put(mem[old.trap.TRAW],off,int.from_bytes(parent[src:src+4],'little'))
    for name,off in [('trap_data1',0x1E0),('trap_data2',0x1E4)]:
        if name in c:p.f.put(mem[old.trap.TRAW],off,c[name])
    for off,value in [(0x1624,DEFS),(0x1628,4),(0x162C,4)]:p.f.put(mem[p.f.WORLD],off,value)
    # Complete original class snapshots need currentArt when Deckem calls the
    # real animation setter; no rawCreature or runtimevtable field changes.
    for actor in actors:
        definition=int.from_bytes(mem[actor][0x370:0x374],'little');p.f.put(mem[actor],0x6C,definition+27)
        p.f.put(mem[actor],0x40,c.get('HP',100000))
    if c.get('invulnerable'):p.f.put(mem[actors[1]],0x445,c['invulnerable'],2)
    previous=old.trap.TBLOCK
    for i in range(c.get('spare_blocks',16)):
        address=ARENAS+i*4096;mem[address]=bytearray([165])*768
        struct.pack_into('<III',mem[address],0,512,previous,p.f.FT)
        p.f.put(mem[previous],8,address);previous=address
    p.f.put(mem[p.f.FT],4,previous)
    calls=[];replaced=False
    for call in args['before_calls']:
        if call['address']=='0x4acd60':
            assert not replaced;replaced=True
            for i,ID in enumerate(IDS):
                address=DEFS+118*i
                calls += [dict(address='0x4acd60',registers=dict(ECX=hex(address)),stack_args=[]),
                    dict(address='0x56e540',registers=dict(ECX=hex(MID)),stack_args=[]),
                    dict(address='0x56e690',registers=dict(ECX=hex(MID)),stack_args=[RAWS+239*i]),
                    dict(address='0x4acdf0',registers=dict(ECX=hex(address)),stack_args=[MID,old.trap.p.area.GFX])]
        elif call['address'] not in ['0x56e540','0x56e690','0x4acdf0']:calls.append(call)
    assert replaced
    assert not c.get('wait_ticks') or c.get('parent_ticks',1)==8
    assert not c.get('inferno_ticks') or c.get('wait_ticks',0)==8
    planned=[node]*c.get('parent_ticks',1)
    planned += [ARENAS+0x8C]*c.get('child_ticks',0)
    planned += [ARENAS+4096+0x8C]*c.get('wait_ticks',0)
    planned += [ARENAS+8192+0x8C]*c.get('inferno_ticks',0)
    calls += [dict(address='0x4ab180',registers=dict(ECX=hex(a)),stack_args=[]) for a in planned[:-1]]
    args['before_calls']=calls
    args['address']='0x4ab180';args['registers']=json.dumps(dict(ECX=hex(planned[-1])))
    # Range-checked original IsBadReadPtr import provider needs enlargedranges.
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
    args,checks,node,actors=build(c);tag=sha((json.dumps(args,sort_keys=True)+sha(Path(__file__).read_bytes())+sha((ROOT/'native_shot_pair_trace.py').read_bytes())).encode())[:12]
    file=ROOT/'ghidra'/f'phase174-child-chain-discovery-{tag}.json'
    if file.exists():saved=json.loads(file.read_text(encoding='utf-8'));assert saved['input']==args
    else:
        r=old.target.cpu.run(EXE,args,list(checks.items()),p.f.OBS,node,371);saved=dict(case=c,input=args,result=r)
        file.write_text(json.dumps(saved,indent=2),encoding='utf-8')
    post=post_context(checks,saved['result']);return args,checks,saved,file,post,node,actors

def describe(c):
    args,ch,saved,file,post,node,actors=execute(c);r=saved['result'];u=lambda b,o,n=4:int.from_bytes(b[o:o+n],'little');shots=[]
    registry=post[0x6EE540]
    for tag in range(202,u(post[0x6A4818],4)):
        address=u(registry,tag*4)
        b=next((b[address-a:address-a+371] for a,b in post.items() if a<=address<address+371<=a+len(b)),None)
        if b is not None:shots.append(dict(tag=tag,address=hex(address),ID=b[0x124],HP=u(b,0x40),tick=u(b,0x12D),Data1=u(b,0x13D),Data2=u(b,0x141),target=u(b,0x147,2)))
    print(json.dumps(dict(case=c,success=r['success'],error=r['emulation_error'],HP=[u(post[a],0x40) for a in actors],
        requests=r['hp_requests'],shots=shots,before=r['before_calls'][-8:],tail=r['trace_tail'],
        low=[x for x in r['observed_reads']+r['observed_writes'] if 0<int(x['address'],16)<4096][:8])),flush=True)

if __name__=='__main__':describe(dict(parent_ticks=int(sys.argv[1]) if len(sys.argv)>1 else 1))
