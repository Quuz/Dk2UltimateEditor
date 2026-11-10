"""Phase173 Guided clock gate with actual PE trig and ordinary-hit composition.

Whole original collision with rawShot35 and diagnostic optional nativevelocity
setter. Session time is prepared input; no World flight scheduler is executed.
"""
from pathlib import Path
import json,struct,sys
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
import probe_guided_clock as probe
from emulate_resistance import post_context
ROOT=Path(__file__).resolve().parent
p=probe.p
EXE=probe.EXE
sha=probe.sha

def cases():
    out=[dict(name='source-clock100',now=100),dict(name='source-clock104',now=104)]
    for now in [100,103,104,105,112,0xFFFFFFF8,0xFFFFFFFF]:
        out.append(dict(name=f'velocity-clock-{now}',now=now,initial_velocity=[123,456,789]))
    for name,kw in [('mask-miss',dict(mask=0)),('invulnerable445',dict(invulnerable=1)),('no-normal',dict(mode=0))]:
        out.append(dict(name=name,now=104,initial_velocity=[123,456,789],**kw))
    return out

def execute(c):
    args,checks=probe.build(c);deps=''.join(sha((ROOT/n).read_bytes()) for n in ['probe_guided_clock.py','native_shot_pair_trace.py'])
    tag=sha((json.dumps(args,sort_keys=True)+deps).encode())[:12]
    file=ROOT/'ghidra'/f'phase173-guided-clock-{c["name"]}-{tag}.json'
    if file.exists():saved=json.loads(file.read_text(encoding='utf-8'));assert saved['input']==args and saved['case']==c
    else:
        r=probe.old.cpu.run(EXE,args,list(checks.items()),p.f.OBS,p.shot.NODE,371)
        saved=dict(case=c,input=args,result=r);file.write_text(json.dumps(saved,indent=2),encoding='utf-8')
    return args,checks,saved,file

def verify(c,args,checks,r):
    assert r['success'] and r['hit_return'] and not r['emulation_error'],(c['name'],r['emulation_error'],r['trace_tail'])
    assert not r['protected_write_or_execution_failures'] and r['function_or_vtable_substitutions']==0
    assert len(r['before_calls'])==len(args['before_calls']) and all(x['returned'] for x in r['before_calls'])
    post=post_context(checks,r);raw=checks[p.shot.RAW]
    assert int.from_bytes(raw[0x94:0x98],'little')==2867 and int.from_bytes(raw[0x98:0x9C],'little')==100
    active=bool(int.from_bytes(raw[0xA0:0xA4],'little')&4);refresh=active and c['now']&7==0
    speed=(int.from_bytes(raw[0x94:0x98],'little')&65535)<<4
    wanted=[0,-speed,speed] if refresh else c.get('initial_velocity',[0,-speed,speed])
    hit=raw[0xC5]==7 and not c.get('invulnerable',0)
    HP=int.from_bytes(post[p.target.attack.TARGET][0x40:0x44],'little');assert HP==1000-int(hit)*100
    requests=[dict(delta=x['delta'],before=x['HP_before'],caller=x['caller_return']) for x in r['hp_requests']]
    assert requests==([dict(delta=-100,before=1000,caller='0x4ac58e')] if hit else [])
    node=post[p.f.BLOCK][p.shot.NODE-p.f.BLOCK:p.shot.NODE-p.f.BLOCK+371]
    velocity=list(struct.unpack_from('<3i',node,0x104));assert velocity==wanted,(c['name'],velocity,wanted)
    assert int.from_bytes(node[0x40:0x44],'little')==0 and int.from_bytes(node[0x147:0x149],'little')==101
    assert int.from_bytes(node[0xF4:0xF6],'little')==0
    assert r['visited_instructions'].get('0x4af3a0',0)==int(active)
    writes=[x for x in r['observed_writes'] if 0x4AF3A0<=int(x['pc'],16)<0x4AF610 and int(x['address'],16) in [p.shot.NODE+0x104,p.shot.NODE+0x108,p.shot.NODE+0x10C]]
    assert [int(x['pc'],16) for x in writes]==([0x4AF5E6,0x4AF5EA,0x4AF5ED] if refresh else [])
    table=probe.p.pe_read(EXE.read_bytes(),probe.TRIG,probe.TRIG_SIZE)
    assert checks[probe.TRIG]==post[probe.TRIG]==table
    session=int.from_bytes(checks[p.f.WORLD][4:8],'little')
    values=[int.from_bytes(bytes.fromhex(x['value_hex']),'little') for x in r['observed_reads'] if int(x['pc'],16)==0x517493 and int(x['address'],16)==session+4]
    assert values and all(x==c['now'] for x in values),(c['name'],values)
    for a in [p.shot.RAW,p.target.RAW,p.RAWSPELL,0x66D524,0x66D86C,0x66E3EC,0x670574]:assert post[a]==checks[a]
    md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;blob=EXE.read_bytes()
    for x in r['observed_reads']+r['observed_writes']:
        if int(x['address'],16)>=4096:continue
        pc=int(x['pc'],16);ins=next(md.disasm(p.pe_read(blob,pc,15),pc))
        assert int(x['address'],16)==0 and x['size']==4 and 0x64 in ins.prefix and 'fs:[0]' in ins.op_str,(c['name'],x)
    return post,dict(HP=HP,velocity=velocity,clock=c['now'],clock_reads=len(values),refresh=refresh,process_calls=int(active),
        native_velocity_writes=writes,requests=requests,ShotHP=0,full_regions=len(post),full_bytes=sum(map(len,post.values())),native_steps=r['instruction_count'])

def main():
    rows=[]
    for c in cases():
        args,checks,saved,file=execute(c);_,out=verify(c,args,checks,saved['result'])
        rows.append(dict(case=c,file=str(file.relative_to(ROOT)),sha256=sha(file.read_bytes()),outcome=out))
        print(c['name'],out['HP'],out['velocity'],'refresh',out['refresh'],flush=True)
    (ROOT/'phase173-guided-clock-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows),indent=2),encoding='utf-8')

if __name__=='__main__':main()
