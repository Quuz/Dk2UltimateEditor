"""Phase171: Lightning/Deckem/Guided composed with ordinary damage.

Original complete collision and selected complete Shot ticks, valid native
AI tables and prepared Creature/World/map context. The scene is bounded;
it is not a natural combat scheduler or a complete child-Shot simulation.
"""
from pathlib import Path
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
import json,sys
import probe_shot_target_combinations as probe
from emulate_resistance import post_context
ROOT=Path(__file__).resolve().parent
p=probe.p
EXE=probe.EXE
sha=probe.sha

def cases():
    seed=dict(process=9,mode=7,data1=80,data2=2,mask=4,root='collision',HP=1000)
    rows=[('lightning-hit',{}),('lightning-process-only',dict(mode=0)),
        ('lightning-mask-miss',dict(mask=2)),('lightning-immune-extra1',dict(extra_flags=1)),
        ('lightning-invulnerable445',dict(invulnerable=1)),('lightning-conductive',dict(conductive=True)),
        ('lightning-conductive-immune',dict(conductive=True,extra_flags=1)),
        ('lightning-armoured',dict(armour=2)),('lightning-no-AI-tables',dict(ai_tables=False)),
        ('lightning-zero',dict(data1=0)),('lightning-negative',dict(data1=0xFFFFFFB0)),
        ('lightning-immune-and445',dict(extra_flags=1,invulnerable=1)),
        ('lightning-primary4000',dict(creature_flags=0x4000)),
        ('deckem-hit',dict(process=29)),('deckem-process-only',dict(process=29,mode=0)),
        ('deckem-mask-miss',dict(process=29,mask=2)),('deckem-invulnerable445',dict(process=29,invulnerable=1)),
        ('deckem-no-AI-tables',dict(process=29,ai_tables=False)),('deckem-primary4000',dict(process=29,creature_flags=0x4000)),
        ('deckem-zero',dict(process=29,data1=0)),
        ('deckem-area-collision',dict(process=29,mode=1,root='tick',extra_target=True)),
        ('deckem-area-start',dict(process=29,mode=1,root='tick',extra_target=True,mask=64)),
        ('deckem-area-tick-abort',dict(process=29,mode=1,root='tick',extra_target=True,mask=2)),
        ('deckem-area-no-process',dict(process=29,mode=1,root='tick',extra_target=True,mask=0)),
        ('deckem-area-invulnerable',dict(process=29,mode=1,root='tick',extra_target=True,invulnerable=1)),
        ('deckem-area-armoured',dict(process=29,mode=1,root='tick',extra_target=True,armour=2)),
        ('guided-hit',dict(process=28)),('guided-mask-miss',dict(process=28,mask=2)),
        ('guided-process-only',dict(process=28,mode=0))]
    out=[dict(seed,**kw,name=name) for name,kw in rows]
    inventory=json.loads((ROOT/'phase168-shot-combinations-storage.json').read_text(encoding='utf-8'))['observed_pairs']
    for name,pair in [('source-stun97',(9,7)),('source-guided35',(28,7)),('source-reaper52',(29,7))]:
        v=next(x for x in inventory if (x['ProcessType'],x['DamageType'])==pair)['variants'][0]
        out.append(dict(seed,process=pair[0],mode=pair[1],name=name,source_record=dict(file=v['first_source'],record_index=v['first_record_index'])))
    return out

def execute(c):
    args,checks=probe.build(c)
    deps=''.join(sha((ROOT/n).read_bytes()) for n in ['probe_shot_target_combinations.py','native_shot_pair_trace.py'])
    tag=sha((json.dumps(args,sort_keys=True)+deps).encode())[:12]
    file=ROOT/'ghidra'/f'phase171-target-{c["name"]}-{tag}.json'
    if file.exists():saved=json.loads(file.read_text(encoding='utf-8'));assert saved['input']==args and saved['case']==c
    else:
        r=probe.cpu.run(EXE,args,list(checks.items()),p.f.OBS,p.shot.NODE,371)
        saved=dict(case=c,input=args,result=r);file.write_text(json.dumps(saved,indent=2),encoding='utf-8')
    return args,checks,saved,file

def reference(c,checks):
    raw=checks[p.shot.RAW];process=raw[0xC7];mode=raw[0xC5]
    data=int.from_bytes(raw[0x98:0x9C],'little');mask=int.from_bytes(raw[0xA0:0xA4],'little')
    signed=lambda n:n if n<0x80000000 else n-0x100000000
    amount=signed(data);HP=[c['HP'],c['HP']];state=[1,1];reaction=0;requests=[];callbacks=0
    primary=int.from_bytes(checks[p.target.RAW][0xEFC:0xF00],'little')
    extra=int.from_bytes(checks[p.target.RAW][0x1545:0x1549],'little')
    def request(i,damage,caller):
        requests.append(dict(target=p.target.attack.TARGET if i==0 else probe.EXTRA,delta=-damage,before=HP[i],caller=caller))
        if primary&0x4000:return
        coefficient=128-(64 if c.get('armour',0) else 0)+(128 if state[i]==255 else 0)
        HP[i]-=damage*coefficient//128
    def event(flag,has_target):
        nonlocal callbacks,reaction
        if not mask&flag:return 1
        callbacks+=1
        if has_target and process==9 and not extra&1:
            request(0,abs(amount*(2 if c.get('conductive') else 1)),'0x4af2d6')
            reaction=20
            if not primary&0x4000 and c.get('ai_tables',True):state[0]=255
        if has_target and process==29 and not primary&0x4000 and c.get('ai_tables',True):state[0]=255
        return int(process==28)
    if c['root']=='collision':
        event(4,True)
        if mode==7 and amount>0 and not c.get('invulnerable',0):request(0,amount,'0x4ac58e')
    else:
        event(64,False)
        if event(2,False):
            event(4,True)
            if not c.get('invulnerable',0):request(0,amount,'0x4ac666')
            # Copy of the prepared Creature includes its armour/445 fields.
            if not c.get('invulnerable',0):request(1,amount,'0x4ac666')
    return dict(HP=HP[:2 if c.get('extra_target') else 1],state=state[:2 if c.get('extra_target') else 1],reaction=reaction,
        requests=requests,callbacks=callbacks,Data1=data,Data2=int.from_bytes(raw[0x9C:0xA0],'little'))

def verify(c,args,checks,r):
    assert r['success'] and r['hit_return'] and not r['emulation_error'],(c['name'],r['emulation_error'],r['trace_tail'])
    assert not r['protected_write_or_execution_failures'] and r['function_or_vtable_substitutions']==0
    assert len(r['before_calls'])==len(args['before_calls']) and all(x['returned'] for x in r['before_calls'])
    post=post_context(checks,r);e=reference(c,checks);v=r['visited_instructions']
    targets=[p.target.attack.TARGET]+([probe.EXTRA] if c.get('extra_target') else [])
    actual_hp=[int.from_bytes(post[a][0x40:0x44],'little') for a in targets]
    actual_state=[int.from_bytes(post[a][0x25E:0x262],'little') for a in targets]
    actual_requests=[dict(target=x['target'],delta=x['delta'],before=x['HP_before'],caller=x['caller_return']) for x in r['hp_requests']]
    assert actual_hp==e['HP'] and actual_state==e['state'],(c['name'],actual_hp,actual_state,e)
    assert actual_requests==e['requests'],(c['name'],actual_requests,e['requests'])
    assert int.from_bytes(post[targets[0]][0x4E3:0x4E5],'little')==e['reaction']
    callback={9:0x4AF1E0,28:0x4AF3A0,29:0x4AF610}[c['process']]
    assert v.get(hex(callback),0)==e['callbacks'],(c['name'],v.get(hex(callback),0),e)
    for a in [p.shot.RAW,p.target.RAW,p.RAWSPELL,p.shot.SHOT_VTABLE]:assert post[a]==checks[a]
    raw=checks[p.shot.RAW];node=post[p.f.BLOCK][p.shot.NODE-p.f.BLOCK:p.shot.NODE-p.f.BLOCK+371]
    assert int.from_bytes(node[0x13D:0x141],'little')==e['Data1'] and int.from_bytes(node[0x141:0x145],'little')==e['Data2']
    assert int.from_bytes(node[0x145:0x147],'little')==100
    # Genuine conversion bytes, including the native two cleared Art bytes.
    mid=bytearray(b'\1\0\0\0'*34+raw);mid[0xA7]=mid[0x170]=0
    assert post[p.shot.MID]==mid
    assert int.from_bytes(post[p.shot.DEFINITION][0x33:0x37],'little')==sum(mid)
    for entry in [0x47BBE0,0x4798F0,0x4950C0]:assert v.get(hex(entry),0)==int(c.get('ai_tables',True))
    md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;blob=EXE.read_bytes()
    for x in r['observed_reads']+r['observed_writes']:
        if int(x['address'],16)>=4096:continue
        pc=int(x['pc'],16);ins=next(md.disasm(p.pe_read(blob,pc,15),pc))
        assert int(x['address'],16)==0 and x['size']==4 and 0x64 in ins.prefix and 'fs:[0]' in ins.op_str,(c['name'],x)
    return post,dict(HP=actual_hp,state=actual_state,reaction=e['reaction'],requests=actual_requests,
        callback_calls=e['callbacks'],Data1=e['Data1'],Data2=e['Data2'],events=r['process_events'],
        ShotHP=int.from_bytes(node[0x40:0x44],'little'),ShotTarget=int.from_bytes(node[0x147:0x149],'little'),
        full_regions=len(post),full_bytes=sum(map(len,post.values())),native_steps=r['instruction_count'])

def main():
    selected=[c for c in cases() if len(sys.argv)==1 or c['name'] in sys.argv[1:]];rows=[]
    for c in selected:
        args,checks,saved,file=execute(c);_,out=verify(c,args,checks,saved['result'])
        rows.append(dict(case=c,file=str(file.relative_to(ROOT)),sha256=sha(file.read_bytes()),outcome=out))
        print(c['name'],out['HP'],out['state'],flush=True)
    if len(selected)==len(cases()):(ROOT/'phase171-target-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows),indent=2),encoding='utf-8')

if __name__=='__main__':main()
