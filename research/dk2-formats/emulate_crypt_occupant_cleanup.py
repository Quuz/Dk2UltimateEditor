"""Phase178 accepted Crypt registration cleanup with actual Creature occupants.

Prepared room/member records, full initialized original Creature snapshots,
original AI methods and room list unlinking. No room object destruction,
automatic Crypt entry, full World scheduler or new group-bonus assignment.
"""
from pathlib import Path
import json
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
import probe_crypt_occupant_cleanup as probe
from emulate_resistance import post_context
ROOT=Path(__file__).resolve().parent
p=probe.p
EXE=probe.EXE
sha=probe.sha
u=lambda b,o,n=2:int.from_bytes(b[o:o+n],'little')

def cases():
    out=[dict(name=f'clear-group{slot:x}',old_species=ids[0],occupants=2) for slot,ids in sorted(probe.counters.GROUPS.items())]
    out += [dict(name='empty-clear',occupants=0),dict(name='replace',occupants=2,replace=True),
        dict(name='replace-other-owner',occupants=2,replace=True,room_owner=8),dict(name='reset255',occupants=2,enter_state=255),
        dict(name='insert-no-old',occupants=2,old_registered=False,replace=True,enter_state=255),
        dict(name='no-op-no-old',occupants=2,old_registered=False,enter_state=255)]
    return out

def execute(c):
    args,checks,actors,owner=probe.build(c)
    deps=''.join(sha((ROOT/n).read_bytes()) for n in ['probe_crypt_occupant_cleanup.py','native_shot_pair_trace.py'])
    tag=sha((json.dumps(args,sort_keys=True)+deps).encode())[:12]
    file=ROOT/'ghidra'/f'phase178-crypt-cleanup-{c["name"]}-{tag}.json'
    if file.exists():saved=json.loads(file.read_text(encoding='utf-8'));assert saved['input']==args and saved['case']==c
    else:
        r=probe.base.cpu.run(EXE,args,list(checks.items()),p.f.OBS,probe.ROOM,0xC0)
        saved=dict(case=c,input=args,result=r);file.write_text(json.dumps(saved,indent=2),encoding='utf-8')
    return args,checks,saved,file

def data_at(post,address,n):
    base=max(a for a,b in post.items() if a<=address and address+n<=a+len(b))
    return post[base][address-base:address-base+n]

def verify(c,args,checks,r):
    assert r['success'] and r['hit_return'] and not r['emulation_error'],(c['name'],r['emulation_error'],r['trace_tail'])
    assert not r['protected_write_or_execution_failures'] and r['function_or_vtable_substitutions']==0
    assert len(r['before_calls'])==len(args['before_calls']) and all(x['returned'] for x in r['before_calls'])
    assert args['before_calls'][-1]['address']=='0x4d8e60' and args['before_calls'][-1]['stack_args']==[302 if c.get('replace') else 0]
    actors=[p.target.attack.TARGET] if c.get('occupants',1) else []
    if c.get('occupants',1)==2:actors.append(p.cf.ACTOR)
    owner=u(checks[0x6EE540],c.get('room_owner',7)*4,4);post=post_context(checks,r)
    assert int(r['registers']['EAX'],16)==owner
    registered=c.get('old_registered',True);values=[0]*18
    if c.get('replace'):values[probe.counters.counter_slot(c.get('new_species',5))-0x108A]=1
    assert list(post[owner][0x108A:0x109C])==values
    for address in [u(checks[0x6EE540],7*4,4),u(checks[0x6EE540],8*4,4)]:
        if address!=owner:assert post[address][0x108A:0x109C]==checks[address][0x108A:0x109C]
    room=post[probe.ROOM]
    assert u(room,0x53)==(302 if c.get('replace') else 0)
    assert u(room,0x55,4)==(0 if registered else 1234)
    assert u(room,0xA)==(0 if registered or not actors else u(checks[actors[0]],4))
    cap=100;other=100;outputs=[]
    for a in actors:
        b=post[a];definition=u(b,0x370,4);usage=data_at(post,definition+0x672,1)[0]
        if registered and usage<=cap:cap-=usage;other=(other-usage)&65535
        wanted=dict(state=1 if registered else c.get('enter_state',u(checks[a],0x25E,4)),room=0 if registered else 300,
            previous=0 if registered else u(checks[a],0x236),next=0 if registered else u(checks[a],0x238))
        actual=dict(state=u(b,0x25E,4),room=u(b,0x3B9),previous=u(b,0x236),next=u(b,0x238))
        assert actual==wanted,(c['name'],hex(a),actual,wanted)
        assert u(b,0x40,4)==u(checks[a],0x40,4) and post[0x6ED540][u(b,4)]==1
        writes=[x for x in r['observed_writes'] if int(x['address'],16)==a+0x25E]
        if c.get('enter_state'):
            v=[x['value'] for x in writes]
            assert c['enter_state'] in v and (not registered or 1 in v[v.index(c['enter_state'])+1:]),(c['name'],hex(a),v)
        outputs.append(dict(address=a,tag=u(b,4),runtime_usage_byte=usage,**actual))
    assert u(room,0x6B)==cap and u(room,0x6D)==other
    assert room[0xC0:]==checks[probe.ROOM][0xC0:]
    for a in [probe.MEMBER,probe.NEW_MEMBER,p.shot.RAW,p.target.RAW,0x66D524,0x66D754,0x66D86C,0x66E3EC,0x670574]:assert post[a]==checks[a]
    assert not r['hp_requests']
    assert r['visited_instructions'].get('0x4d9570',0)==int(registered)
    assert r['visited_instructions'].get('0x48ad30',0)==len(actors)*int(registered)
    assert r['visited_instructions'].get('0x4e5e00',0)==len(actors)*int(registered)
    md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;blob=EXE.read_bytes()
    for x in r['observed_reads']+r['observed_writes']:
        if int(x['address'],16)>=4096:continue
        pc=int(x['pc'],16);ins=next(md.disasm(p.pe_read(blob,pc,15),pc))
        assert int(x['address'],16)==0 and x['size']==4 and 0x64 in ins.prefix and 'fs:[0]' in ins.op_str,(c['name'],x)
    return post,dict(owner=owner,counters=values,room_head=u(room,0xA),registered_tag=u(room,0x53),room55=u(room,0x55,4),room6B=cap,room6D=other,actors=outputs,
        full_regions=len(post),full_bytes=sum(map(len,post.values())),native_steps=r['instruction_count'])

def main():
    rows=[]
    for c in cases():
        args,checks,saved,file=execute(c);_,out=verify(c,args,checks,saved['result'])
        rows.append(dict(case=c,file=str(file.relative_to(ROOT)),sha256=sha(file.read_bytes()),outcome=out))
        print(c['name'],out['registered_tag'],out['room_head'],out['room6B'],out['counters'],flush=True)
    (ROOT/'phase178-crypt-cleanup-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows),indent=2),encoding='utf-8')

if __name__=='__main__':main()
