"""Phase170 accepted Jack39 hostile creation and original Trap16 firing chains.

Exact raw Creature46/Keeper4/optionalTrap16/Shot64 records, original loaders,
constructor/factories/AI initializers, complete firing wrapper and Shot tick.
Explicit prepared pools/map/targets/resources; no gameplay replacements.
"""
from pathlib import Path
import json,struct,sys
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
import probe_jack_in_box as p
from emulate_resistance import post_context
ROOT=Path(__file__).resolve().parent
EXE=p.EXE
sha=p.sha

def cases():
    seed=dict(mask=64,mode=2,data1=80,data2=2,trap=True,trap_active=True,owners=[8,8],HP=[1000,1000],invulnerable=[0,0])
    rows=[('hostile-start',{}),('hostile-tick',dict(mask=2)),('hostile-no-process',dict(mask=0)),
        ('hostile-no-normal',dict(mode=0)),('friendly-start',dict(owners=[7,8])),
        ('hostile-invulnerable',dict(invulnerable=[1,0])),('hostile-capacity448',dict(capacity=448)),
        ('hostile-capacity447',dict(capacity=447)),('hostile-tick-capacity448',dict(mask=2,capacity=448)),
        ('hostile-zero-data1',dict(data1=0)),('hostile-negative-no-normal',dict(data1=0xFFFFFFB0,mode=0)),
        ('source64-direct',dict(source_shot=True)),('source64-high-HP',dict(source_shot=True,HP=[100000,100000])),
        ('source64-capacity448',dict(source_shot=True,capacity=448)),
        ('fired-source64-tag2',dict(trap_fire=True,source_shot=True)),
        ('fired-source64-tag3',dict(trap_fire=True,source_shot=True,target_tag=3)),
        ('fired-source64-high-HP',dict(trap_fire=True,source_shot=True,HP=[100000,100000])),
        ('fired-source64-capacity448',dict(trap_fire=True,source_shot=True,capacity=448)),
        ('fired-trap-overrides-defaults',dict(trap_fire=True,source_shot=True,shot_data1=123,shot_data2=17,trap_data1=80)),
        ('fired-tag-overrides-trap',dict(trap_fire=True,source_shot=True,trap_data1=80,trap_data2=1)),
        ('fired-tick-return-zero',dict(trap_fire=True,source_shot=True,shot_mask=2,trap_data1=80)),
        ('wind-real-AI',dict(process=17,mask=2,data1=2)),
        ('wind-without-AI-tables',dict(process=17,mask=2,data1=2,ai_tables=False)),
        ('jack-without-AI-tables',dict(ai_tables=False))]
    return [dict(seed,**kw,name=name) for name,kw in rows]

def execute(c):
    args,checks=p.build(c)
    deps=''.join(sha((ROOT/n).read_bytes()) for n in ['probe_jack_in_box.py','native_shot_pair_trace.py'])
    tag=sha((json.dumps(args,sort_keys=True)+deps).encode())[:12]
    file=ROOT/'ghidra'/f'phase170-jack-{c["name"]}-{tag}.json'
    if file.exists():saved=json.loads(file.read_text(encoding='utf-8'));assert saved['input']==args and saved['case']==c
    else:
        r=p.old.cpu.run(EXE,args,list(checks.items()),p.p.f.OBS,p.shot_node(c),371)
        saved=dict(case=c,input=args,result=r);file.write_text(json.dumps(saved,indent=2),encoding='utf-8')
    return args,checks,saved,file

def reference(c,checks):
    process=c.get('process',39);base=p.p.base;f=p.p.f
    raw=checks[base.RAW];d1=int.from_bytes(raw[0x98:0x9C],'little');d2=int.from_bytes(raw[0x9C:0xA0],'little')
    mode=raw[0xC5];mask=int.from_bytes(raw[0xA0:0xA4],'little')
    fired=c.get('trap_fire',False)
    if fired:
        d1=int.from_bytes(checks[p.TRAW][0x1E0:0x1E4],'little');d2=c.get('target_tag',2)
    signed=lambda v:v if v<0x80000000 else v-0x100000000
    amount=signed(d1);HP=list(c['HP']);requests=[]
    events=[];inv=c['invulnerable'];created=False;factory=0
    def request(i,damage,caller):
        requests.append(dict(target=p.p.area.TARGETS[i],delta=-damage,before=HP[i],caller=caller));HP[i]-=damage
    calls=0
    def event(flag):
        nonlocal created,factory,calls
        ret=1
        if mask&flag and process:
            calls+=1
            if process==39:
                ret=0
                if c['owners'][0]!=7:
                    request(0,amount,'0x4b039f');factory+=1
                    created=c.get('capacity',1)<448
            elif process==17:
                request(0,25,'0x4aeb40');request(1,28,'0x4aeb40')
        events.append(dict(event_mask=flag,return_EAX=ret))
        return ret
    event(64);allowed=bool(event(2))
    if allowed:
        if fired:event(4) # trackingShot originalcollisionpath; mask64/2 here
        if mode==2 and d1:
            working=d1
            for i,dist in [(0,0),(1,1024)]:
                if c['owners'][i]==7 or inv[i]:continue
                working=(d2*4096-dist)*working//(d2*4096)
                request(i,working,'0x4ac8d2')
        if not fired:event(8)
    shot_hp=int.from_bytes(raw[0xBC:0xBE],'little')-int(allowed and int.from_bytes(raw[0xA8:0xAC],'little')&4!=0)
    if allowed and shot_hp<=0:event(128)
    return dict(Data1=d1,Data2=d2,mode=mode,mask=mask,HP=HP,requests=requests,events=events,created=created,factory=factory,
        callback_calls=calls,normal_entries=int(allowed),shotHP=shot_hp,
        actor_tag=200 if fired else 201,shot_tag=202 if fired else 200,trap_tag=201 if fired else 202,
        new_state=(273 if c.get('ai_tables',True) else 1) if created else 0)

def verify(c,args,checks,r):
    assert r['success'] and r['hit_return'] and not r['emulation_error'],(c['name'],r['emulation_error'],r['trace_tail'])
    assert not r['protected_write_or_execution_failures'] and r['function_or_vtable_substitutions']==0
    assert len(r['before_calls'])==len(args['before_calls']) and all(x['returned'] for x in r['before_calls'])
    e=reference(c,checks);post=post_context(checks,r);f=p.p.f;base=p.p.base;v=r['visited_instructions']
    actual=[dict(target=x['target'],delta=x['delta'],before=x['HP_before'],caller=x['caller_return']) for x in r['hp_requests']]
    assert actual==e['requests'],(c['name'],actual,e['requests'])
    assert [int.from_bytes(post[a][0x40:0x44],'little',signed=True) for a in p.p.area.TARGETS[:2]]==e['HP']
    ev=[{k:x[k] for k in ['event_mask','return_EAX']} for x in r['process_events']]
    assert ev==e['events'],(c['name'],ev,e['events'])
    assert all(x['owner_u16']==7 and x['mode']==0 for x in r['hp_requests'])
    assert v.get('0x4b0270',0)==0
    assert v.get('0x4b02e0',0)==(e['callback_calls'] if c.get('process',39)==39 else 0)
    assert v.get('0x4ae990',0)==(e['callback_calls'] if c.get('process',39)==17 else 0)
    assert v.get('0x4b78d0',0)==e['factory'] and v.get('0x4872a0',0)==int(e['created'])
    assert v.get('0x4a7fd0',0)==v.get('0x4a80e0',0)==int(c.get('trap_fire',False))
    assert v.get('0x4ac6d0',0)==(e['normal_entries'] if e['mode']==2 else 0)
    assert v.get('0x4ac3c0',0)==e['normal_entries']
    for a in [base.RAW,base.target.RAW,p.JRAW,p.KRAW,p.TRAW]:assert post[a]==checks[a]
    assert post[p.ACTOR][1343:]==checks[p.ACTOR][1343:] and post[p.TABLE][-64:]==checks[p.TABLE][-64:]
    actor=post[p.ACTOR];u=lambda b,o,n=4:int.from_bytes(b[o:o+n],'little')
    assert u(actor,4,2)==e['actor_tag'] and u(actor,0)==0x66D524
    if e['created']:
        assert actor[0x36F]==46 and actor[0x403]==1 and u(actor,0x370)==p.JDEF
        assert u(actor,0x40)==240 and u(actor,0x28,2)==7 and u(actor,0x2A)==f.PLAYER
        assert u(actor,0x2F5,2)==c['owners'][0] and u(actor,0x25E)==e['new_state']
        assert u(post[p.STATS],13+9)==1
    else:
        assert actor[0x36F]==0 and u(actor,0x40)==0 and u(actor,0x25E)==0
        assert u(post[p.STATS],13+9)==0
    assert u(post[f.WORLD],0x26)==c.get('capacity',1)+int(e['created'])
    assert u(post[f.WORLD],0x24,2)==(0 if e['created'] else e['actor_tag'])
    assert u(post[0x6EE540],e['actor_tag']*4)==p.ACTOR and post[0x6ED540][e['actor_tag']]==1
    trap=p.trap_node(c);trap_region=f.BLOCK if c.get('trap_fire') else p.TBLOCK;trap_offset=trap-trap_region
    tb=post[trap_region][trap_offset:trap_offset+291]
    assert u(tb,0)==0x66D754 and u(tb,4,2)==e['trap_tag'] and tb[0xF6]==16
    trapHP=u(post[p.TDEF],0xA2,2)
    assert u(tb,0x40)==(0 if e['created'] else trapHP)
    # Its registry allocation remains aftertheHP-zero request; noWorldpoolfree.
    assert u(post[0x6EE540],e['trap_tag']*4)==trap and post[0x6ED540][e['trap_tag']]==1
    nodeaddr=p.shot_node(c);block=p.TBLOCK if c.get('trap_fire') else f.BLOCK;off=nodeaddr-block
    node=post[block][off:off+371];assert node.hex()==r['captured_hex']
    assert u(node,4,2)==e['shot_tag'] and u(node,0x13D)==e['Data1'] and u(node,0x141)==e['Data2']
    assert u(node,0x40)==e['shotHP'] and u(node,0x12D)==1 and u(node,0x149)==1
    if c.get('trap_fire'):
        assert u(node,0x147,2)==c.get('target_tag',2)
        writes={x['pc']:x['value'] for x in r['observed_writes'] if int(x['address'],16)==nodeaddr+0x141}
        assert writes['0x4a823b']==int.from_bytes(checks[p.TRAW][0x1E4:0x1E8],'little')
        assert writes['0x4a804c']==e['Data2']
    # Actual rawconvertercomplete375-byteShot projection andruntimechecksum.
    mid=bytearray(b'\1\0\0\0'*34+checks[base.RAW]);mid[0xA7]=mid[0x170]=0
    assert post[base.MID]==mid and u(post[base.DEFINITION],0x33)==sum(mid)
    assert u(post[p.TABLE],2199+0x692,2)==240
    for entry in [0x47BBE0,0x4798F0,0x4950C0]:assert v.get(hex(entry),0)==int(c.get('ai_tables',True))
    if c.get('process',39)==17:
        for a in [base.cf.ACTOR]+p.p.area.TARGETS[:2]:
            assert u(post[a],0x25E)==(210 if c.get('ai_tables',True) else 1)
    md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;blob=EXE.read_bytes()
    for x in r['observed_reads']:
        if int(x['address'],16)>=4096:continue
        ins=next(md.disasm(p.p.area.pe_read(blob,int(x['pc'],16),15),int(x['pc'],16)))
        assert int(x['address'],16)==0 and x['size']==4 and 0x64 in ins.prefix and 'fs:[0]' in ins.op_str,(c['name'],x)
    assert u(post[p.readable.COUNTER],4)==0
    return post,dict(HP=e['HP'],requests=e['requests'],events=ev,created=e['created'],newActorTag=e['actor_tag'],newActorHP=u(actor,0x40),newActorState=u(actor,0x25E),
        remembered_victim_owner=u(actor,0x2F5,2),trapHP=u(tb,0x40),Data1=e['Data1'],Data2=e['Data2'],ShotHP=e['shotHP'],
        capacity_before=c.get('capacity',1),capacity_after=u(post[f.WORLD],0x26),full_regions=len(post),full_bytes=sum(map(len,post.values())),native_steps=r['instruction_count'])

def main():
    selected=[c for c in cases() if len(sys.argv)==1 or c['name'] in sys.argv[1:]];rows=[]
    for c in selected:
        args,checks,saved,file=execute(c)
        if not saved['result']['success']:print(c['name'],saved['result']['emulation_error'],saved['result']['trace_tail'],flush=True)
        _,out=verify(c,args,checks,saved['result'])
        rows.append(dict(case=c,file=str(file.relative_to(ROOT)),sha256=sha(file.read_bytes()),outcome=out))
        print(c['name'],out['HP'],'created/state',out['created'],out['newActorState'],'D1/D2',out['Data1'],out['Data2'],flush=True)
    if len(selected)==len(cases()):
        (ROOT/'phase170-jack-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows),indent=2),encoding='utf-8')

if __name__=='__main__':main()
