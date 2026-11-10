"""Phase176 bounded original Chainfire graph and causal event8 controls.

Native methods and factories on explicit prepared context; host-selected next
objects. Does not assert perpetual lifetime, World scheduling or live DPS.
"""
from pathlib import Path
import json,math
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
import probe_shot_chainfire as probe
from emulate_resistance import post_context
ROOT=Path(__file__).resolve().parent
p=probe.p
EXE=probe.EXE
sha=probe.sha
u=lambda b,o,n=4:int.from_bytes(b[o:o+n],'little')
si=lambda b,o:int.from_bytes(b[o:o+4],'little',signed=True)

def cases():
    return [dict(name='source50'),dict(name='source36',parent=36),dict(name='source14',parent=14),dict(name='source155',parent=155),
        dict(name='child36-first',child_ticks=[[0,1]]),dict(name='child36-two',child_ticks=[[0,2]]),dict(name='child36-four',child_ticks=[[0,4]]),
        dict(name='death75-area',child_ticks=[[1,1]]),dict(name='whole-selected-chain',child_ticks=[[0,1],[2,1],[3,1],[4,1]]),
        dict(name='whole-chain-invulnerable',invulnerable=1,child_ticks=[[0,1],[2,1],[3,1],[4,1]]),
        dict(name='clear8-first',clear_bits=8,child_ticks=[[0,1]]),dict(name='clear8-two',clear_bits=8,child_ticks=[[0,2]]),
        dict(name='clear256-two',clear_bits=256,child_ticks=[[0,2]])]

def execute(c):
    args,checks,node,actors=probe.build(c)
    deps=''.join(sha((ROOT/n).read_bytes()) for n in ['probe_shot_chainfire.py','probe_shot_source_status.py','native_shot_pair_trace.py'])
    tag=sha((json.dumps(args,sort_keys=True)+deps).encode())[:12]
    file=ROOT/'ghidra'/f'phase176-chainfire-{c["name"]}-{tag}.json'
    if file.exists():saved=json.loads(file.read_text(encoding='utf-8'));assert saved['input']==args and saved['case']==c
    else:
        r=probe.base.old.target.cpu.run(EXE,args,list(checks.items()),p.f.OBS,node,371)
        saved=dict(case=c,input=args,result=r);file.write_text(json.dumps(saved,indent=2),encoding='utf-8')
    return args,checks,saved,file

def view(post,address):return next(b[address-a:address-a+371] for a,b in post.items() if a<=address<address+371<=a+len(b))

def reference(c,checks,post):
    parent=c.get('parent',50);cfg=probe.config(c);mapping={50:36,36:14,14:155,155:75}
    damage=u(checks[probe.base.old.trap.TRAW],0x1E0);radius=u(checks[probe.base.old.trap.TRAW],0x1E4)
    assert damage==250 and radius==3
    shots=[dict(ID=parent,HP=0xFFFFFFFF,tick=1),dict(ID=mapping[parent],HP=1 if parent==155 else 128 if parent==14 else 40,tick=0)]
    if parent!=155:shots.append(dict(ID=75,HP=1,tick=0))
    solid_parents=[]
    for index,count in c.get('child_ticks',[]):
        shot=shots[index+1]
        for _ in range(count):
            shot['tick']+=1
            if shot['HP']==0:continue
            if shot['ID']==75:shot['HP']=0;continue
            child=mapping[shot['ID']];shots.append(dict(ID=child,HP=1 if child==75 else 128 if child==155 else 40,tick=0));solid_parents.append(index+1)
            if c.get('clear_bits',0)&8 and shot['ID'] in c.get('mask_ids',[36]):shot['HP']=0
    for i,shot in enumerate(shots):shot.update(tag=202+i,address=probe.base.old.trap.FIRED_NODE if i==0 else probe.ARENAS+(i-1)*4096+0x8C,Data1=damage,Data2=radius,target=2)
    actors=[probe.base.old.trap.p.base.cf.ACTOR]+probe.base.old.trap.p.area.TARGETS[:2]
    hp=[c.get('HP',100000)]*3;requests=[]
    if not c.get('invulnerable'):
        requests.append(dict(target=actors[1],delta=-250,before=hp[1],caller='0x4ac58e'));hp[1]-=500
    # Independent phase131 bounded D2 formula, on the actual stationary
    # explosion's native position; this does not replace motion validation.
    for shot in shots:
        if shot['ID']!=75 or not shot['tick']:continue
        origin=view(post,shot['address']);work=damage
        for index in [1,2]:
            if index==1 and c.get('invulnerable'):continue
            actor=post[actors[index]];dx=si(actor,0x1A)-si(origin,0x1A);dy=si(actor,0x1E)-si(origin,0x1E)
            dist2=(dx*dx>>12)+(dy*dy>>12);R=radius*4096
            if dist2>=R*R>>12:continue
            distance=math.isqrt(dist2*4096);work=(R-distance)*work//R
            requests.append(dict(target=actors[index],delta=-work,before=hp[index],caller='0x4ac8d2'));hp[index]-=work*(2 if index==1 else 1)
    return dict(HP=hp,freeze=[0]*3,poison=[0]*3,severity=[0]*3,shots=shots),requests,solid_parents

def verify(c,args,checks,r):
    assert r['success'] and r['hit_return'] and not r['emulation_error'],(c['name'],r['emulation_error'],r['trace_tail'])
    assert not r['protected_write_or_execution_failures'] and r['function_or_vtable_substitutions']==0
    assert len(r['before_calls'])==len(args['before_calls']) and all(x['returned'] for x in r['before_calls'])
    post=post_context(checks,r);actors=[probe.base.old.trap.p.base.cf.ACTOR]+probe.base.old.trap.p.area.TARGETS[:2]
    out=probe.base.inspect(post,actors);expected,requests,solid_parents=reference(c,checks,post)
    actual_req=[dict(target=x['target'],delta=x['delta'],before=x['HP_before'],caller=x['caller_return']) for x in r['hp_requests']]
    assert out==expected and actual_req==requests,(c['name'],out,expected,actual_req,requests)
    cfg=probe.config(c);file,ids,raws=probe.base.source(cfg);changed=[]
    assert post[probe.RAWS]==checks[probe.RAWS] and post[probe.DEFS][len(ids)*118:]==checks[probe.DEFS][len(ids)*118:]
    for i,ID in enumerate(ids):
        raw=bytearray(raws[ID]);mask=u(raw,0xA0)
        if c.get('clear_bits') and ID in c.get('mask_ids',[36]):p.f.put(raw,0xA0,mask&~c['clear_bits']);changed.append(ID)
        assert checks[probe.RAWS][239*i:239*(i+1)]==raw
        d=post[probe.DEFS][118*i:118*(i+1)]
        assert d[0x58]==ID and u(d,0x37)==u(raw,0x98) and u(d,0x3B)==u(raw,0x9C) and u(d,0x3F)==u(raw,0xA0)
        mid=bytearray(b'\1\0\0\0'*34+raw);mid[0xA7]=mid[0x170]=0;assert u(d,0x33)==sum(mid)
    copies=[]
    for i,shot in enumerate(out['shots']):
        address=shot['address'];tag=shot['tag'];assert post[0x6ED540][tag]==1 and u(post[0x6EE540],tag*4)==address
        b=view(post,address);assert u(b,0x28,2)==7 and u(b,0x145,2)==0
        if not i:continue
        pcs=([0x4ABF7D,0x4ABF83] if i==1 else [0x4AB61B,0x4AB621] if i==2 and cfg['parent']!=155 else [0x4AB43F,0x4AB445])
        for pc,off,key in zip(pcs,[0x13D,0x141],['Data1','Data2']):
            found=[x for x in r['observed_writes'] if int(x['pc'],16)==pc and int(x['address'],16)==address+off]
            assert len(found)==1 and found[0]['value']==shot[key],(c['name'],shot,found)
            copies.append(dict(ShotID=shot['ID'],field=key,write=found[0]))
    assert u(post[0x6A4818],4)==202+len(out['shots'])
    assert u(post[p.f.WORLD],0x36)==u(checks[p.f.WORLD],0x36)+len(out['shots'])
    for a in [p.shot.RAW,p.target.RAW,probe.base.old.trap.JRAW,probe.base.old.trap.KRAW,probe.base.old.trap.TRAW,0x66D524,0x66D754,0x66D86C,0x66E3EC,0x670574]:assert post[a]==checks[a]
    assert post[probe.MID][375:]==checks[probe.MID][375:]
    for i in range(16):
        address=probe.ARENAS+4096*i;assert post[address][512:]==checks[address][512:]
    assert u(post[probe.base.old.readable.COUNTER],4)==0
    md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;blob=EXE.read_bytes()
    for x in r['observed_reads']+r['observed_writes']:
        if int(x['address'],16)>=4096:continue
        pc=int(x['pc'],16);ins=next(md.disasm(p.pe_read(blob,pc,15),pc))
        assert int(x['address'],16)==0 and x['size']==4 and 0x64 in ins.prefix and 'fs:[0]' in ins.op_str,(c['name'],x)
    events=r['process_events'];e8=[e for e in events if e['event_mask']==8]
    zeros=[e for e in e8 if e['return_EAX']==0]
    expected_zeros=sum(not(c.get('clear_bits',0)&8 and out['shots'][i]['ID'] in c.get('mask_ids',[36])) for i in solid_parents)
    assert len(zeros)==expected_zeros
    assert r['visited_instructions'].get('0x4ab4df',0)==1+sum(s['ID']==75 and s['tick']>0 for s in out['shots'])
    assert r['visited_instructions'].get('0x4af610',0)==1+expected_zeros
    return post,dict(out,requests=actual_req,parameter_copy_writes=copies,events=events,event8_zero_returns=len(zeros),diagnostic_mask_copies=changed,
        full_regions=len(post),full_bytes=sum(map(len,post.values())),native_steps=r['instruction_count'])

def main():
    rows=[]
    for c in cases():
        args,checks,saved,file=execute(c);_,out=verify(c,args,checks,saved['result'])
        rows.append(dict(case=c,file=str(file.relative_to(ROOT)),sha256=sha(file.read_bytes()),outcome=out))
        print(c['name'],out['HP'],out['event8_zero_returns'],[(s['ID'],s['HP'],s['tick']) for s in out['shots']],flush=True)
    (ROOT/'phase176-chainfire-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows),indent=2),encoding='utf-8')

if __name__=='__main__':main()
