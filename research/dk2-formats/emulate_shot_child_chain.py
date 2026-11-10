"""Phase174 selected original parent/hit-child/death-child/Inferno chain.

Whole Shot ticks selected explicitly from naturally created registered native
objects. Four unchanged source definitions, sorted native lookup and arenas.
No full World scheduler, Creature recovery ticks, terminal cleanup or live-game
balance claim; prepared HP100000 isolates negative-damage composition.
"""
from pathlib import Path
import json,struct,sys
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
import probe_shot_child_chain as probe
from emulate_resistance import post_context
ROOT=Path(__file__).resolve().parent
p=probe.p
EXE=probe.EXE
sha=probe.sha
u=lambda b,o,n=4:int.from_bytes(b[o:o+n],'little')

def cases():
    return [dict(name='parent-first',parent_ticks=1),dict(name='hit-child-first',parent_ticks=1,child_ticks=1),
        dict(name='parent-expired',parent_ticks=8),dict(name='wait-before-expiry',parent_ticks=8,wait_ticks=7),
        dict(name='wait-expired',parent_ticks=8,wait_ticks=8),dict(name='inferno-first',parent_ticks=8,wait_ticks=8,inferno_ticks=1),
        dict(name='full-selected-chain',parent_ticks=8,child_ticks=4,wait_ticks=8,inferno_ticks=1),
        dict(name='trap-overrides-through-chain',parent_ticks=8,child_ticks=4,wait_ticks=8,inferno_ticks=1,trap_data1=80,trap_data2=2),
        dict(name='invulnerable-chain',parent_ticks=8,child_ticks=4,wait_ticks=8,inferno_ticks=1,invulnerable=1)]

def execute(c):
    args,checks,node,actors=probe.build(c);deps=''.join(sha((ROOT/n).read_bytes()) for n in ['probe_shot_child_chain.py','native_shot_pair_trace.py'])
    tag=sha((json.dumps(args,sort_keys=True)+deps).encode())[:12]
    file=ROOT/'ghidra'/f'phase174-child-chain-{c["name"]}-{tag}.json'
    if file.exists():saved=json.loads(file.read_text(encoding='utf-8'));assert saved['input']==args and saved['case']==c
    else:
        r=probe.old.target.cpu.run(EXE,args,list(checks.items()),p.f.OBS,node,371);saved=dict(case=c,input=args,result=r)
        file.write_text(json.dumps(saved,indent=2),encoding='utf-8')
    return args,checks,saved,file

def reference(c,checks):
    damage=u(checks[probe.old.trap.TRAW],0x1E0);radius=u(checks[probe.old.trap.TRAW],0x1E4)
    hp=[c.get('HP',100000)]*3;actors=[probe.old.trap.p.base.cf.ACTOR]+probe.old.trap.p.area.TARGETS[:2];requests=[]
    def request(i,caller):
        requests.append(dict(target=actors[i],delta=-damage,before=hp[i],caller=caller));hp[i]-=damage*(2 if i==1 else 1)
    for _ in range(c['parent_ticks']+c.get('child_ticks',0)):
        if not c.get('invulnerable'):request(1,'0x4ac666')
        request(2,'0x4ac666')
    for _ in range(c.get('inferno_ticks',0)):
        for i in range(3):request(i,'0x4af83d')
        if not c.get('invulnerable'):request(1,'0x4ac666')
        request(2,'0x4ac666')
    shots=[dict(tag=202,address=probe.old.trap.FIRED_NODE,ID=152,HP=8-c['parent_ticks'],ticks=c['parent_ticks']),
        dict(tag=203,address=probe.ARENAS+0x8C,ID=153,HP=4-c.get('child_ticks',0),ticks=c.get('child_ticks',0))]
    if c['parent_ticks']==8:shots.append(dict(tag=204,address=probe.ARENAS+4096+0x8C,ID=158,HP=8-c.get('wait_ticks',0),ticks=c.get('wait_ticks',0)))
    if c.get('wait_ticks')==8:shots.append(dict(tag=205,address=probe.ARENAS+8192+0x8C,ID=49,HP=32-c.get('inferno_ticks',0),ticks=c.get('inferno_ticks',0)))
    for shot in shots:shot.update(Data1=damage,Data2=radius,target=2,owner=7,caster=0)
    return dict(HP=hp,requests=requests,shots=shots)

def verify(c,args,checks,r):
    assert r['success'] and r['hit_return'] and not r['emulation_error'],(c['name'],r['emulation_error'],r['trace_tail'])
    assert not r['protected_write_or_execution_failures'] and r['function_or_vtable_substitutions']==0
    assert len(r['before_calls'])==len(args['before_calls']) and all(x['returned'] for x in r['before_calls'])
    post=post_context(checks,r);e=reference(c,checks)
    actors=[probe.old.trap.p.base.cf.ACTOR]+probe.old.trap.p.area.TARGETS[:2]
    HP=[u(post[a],0x40) for a in actors]
    req=[dict(target=x['target'],delta=x['delta'],before=x['HP_before'],caller=x['caller_return']) for x in r['hp_requests']]
    assert HP==e['HP'] and req==e['requests'],(c['name'],HP,e['HP'],req,e['requests'])
    # Native registration and exact allocator addresses verify caller ECXs;
    # the host never writes a child definition pointer or child Shot payload.
    actual=[]
    for want in e['shots']:
        tag,address=want['tag'],want['address'];assert post[0x6ED540][tag]==1 and u(post[0x6EE540],tag*4)==address
        b=next(b[address-a:address-a+371] for a,b in post.items() if a<=address<address+371<=a+len(b))
        actual.append(dict(tag=tag,address=address,ID=b[0x124],HP=u(b,0x40),ticks=u(b,0x12D),Data1=u(b,0x13D),Data2=u(b,0x141),
            target=u(b,0x147,2),owner=u(b,0x28,2),caster=u(b,0x145,2)))
    assert actual==e['shots'],(c['name'],actual,e['shots'])
    copies=[]
    for shot in actual[1:]:
        pcs=[0x4ABF7D,0x4ABF83] if shot['ID']==153 else [0x4AB61B,0x4AB621]
        for pc,off,key in zip(pcs,[0x13D,0x141],['Data1','Data2']):
            found=[x for x in r['observed_writes'] if int(x['pc'],16)==pc and int(x['address'],16)==shot['address']+off]
            assert len(found)==1 and found[0]['value']==shot[key],(c['name'],shot,found)
            copies.append(dict(ShotID=shot['ID'],field=key,write=found[0]))
    assert u(post[0x6A4818],4)==202+len(actual)
    assert u(post[p.f.WORLD],0x36)==u(checks[p.f.WORLD],0x36)+len(actual)
    assert post[probe.RAWS]==checks[probe.RAWS] and post[probe.DEFS][4*118:]==checks[probe.DEFS][4*118:]
    for i,ID in enumerate(probe.IDS):
        raw=checks[probe.RAWS][239*i:239*(i+1)];d=post[probe.DEFS][118*i:118*(i+1)]
        assert raw[0xBE]==ID and u(d,0x37)==u(raw,0x98) and u(d,0x3B)==u(raw,0x9C) and d[0x58]==ID
        mid=bytearray(b'\1\0\0\0'*34+raw);mid[0xA7]=mid[0x170]=0
        assert u(d,0x33)==sum(mid)
    for a in [p.shot.RAW,p.target.RAW,probe.old.trap.JRAW,probe.old.trap.KRAW,probe.old.trap.TRAW,0x66D524,0x66D754,0x66D86C,0x66E3EC,0x670574]:
        if a in checks:assert post[a]==checks[a]
    assert post[probe.MID][375:]==checks[probe.MID][375:]
    for i in range(c.get('spare_blocks',16)):
        address=probe.ARENAS+4096*i;assert post[address][512:]==checks[address][512:]
    assert u(post[probe.old.readable.COUNTER],4)==0
    md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;blob=EXE.read_bytes()
    for x in r['observed_reads']+r['observed_writes']:
        if int(x['address'],16)>=4096:continue
        pc=int(x['pc'],16);ins=next(md.disasm(p.pe_read(blob,pc,15),pc))
        assert int(x['address'],16)==0 and x['size']==4 and 0x64 in ins.prefix and 'fs:[0]' in ins.op_str,(c['name'],x)
    return post,dict(HP=HP,requests=req,shots=actual,parameter_copy_writes=copies,full_regions=len(post),full_bytes=sum(map(len,post.values())),native_steps=r['instruction_count'],events=r['process_events'])

def main():
    rows=[]
    for c in cases():
        args,checks,saved,file=execute(c);_,out=verify(c,args,checks,saved['result'])
        rows.append(dict(case=c,file=str(file.relative_to(ROOT)),sha256=sha(file.read_bytes()),outcome=out))
        print(c['name'],out['HP'],[(x['ID'],x['Data1'],x['Data2'],x['ticks']) for x in out['shots']],flush=True)
    (ROOT/'phase174-child-chain-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows),indent=2),encoding='utf-8')

if __name__=='__main__':main()
