"""Phase181 accepted room-entry/registration/bell activation/exit compositions."""
from pathlib import Path
import json
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
import probe_crypt_participant_chain as probe
from emulate_resistance import post_context
from native_room_counters import counter_slot
ROOT=Path(__file__).resolve().parent
p,EXE,sha,u=probe.p,probe.EXE,probe.sha,probe.u

def cases():
    return [dict(name=n,species=species,**kw) for n,species,kw in [
        ('warlock-cycle',5,{}),('unique-warlock-cycle',34,{}),('knight-cycle',13,{}),('king-cycle',21,{}),
        ('fairy-registration-cycle',20,{}),('wizard-registration-cycle',16,{}),('monk-registration-cycle',19,{}),
        ('no-bell-reference',5,dict(bell=False)),('wrong-facing',5,dict(direction=0)),
        ('other-action-busy',5,dict(initial_action=17,tick=False)),
        ('not-a-room-member',5,dict(entry=False,member=False,register=False,exit=False)),
        ('different-room-type',5,dict(room_type=14,register=False,exit=False)),
        ('without-registration',5,dict(register=False)),
        ('after-exit-action-tick',5,dict(tick_after_exit=True)),
        ('after-exit-AI-then-action',5,dict(ai_after_exit=True,tick_after_exit=True)),
        ('after-exit-whole-Creature',5,dict(full_tick_after_exit=True)),
        ('after-exit-original-action-clear',5,dict(full_tick_after_exit=True,stop_action_after_exit=True)),
        ('original-AI-registration',5,dict(registration='ai',entry_state=274,native_nav=True,activate=False,tick=False,exit=False)),
        ('AI-registration-no-bell',5,dict(registration='ai',entry_state=274,native_nav=True,bell=False,activate=False,tick=False,exit=False)),
    ]]

def execute(c):
    args,ch,s,file,offsets=probe.execute(c)
    return args,ch,s,file

def snapshots(c,post):
    offsets=probe.build(c)[2];rows=[]
    for index in range(6):
        b=post[probe.LOG][index*probe.STRIDE:(index+1)*probe.STRIDE]
        assert u(b,0)==index
        row={name:b[off:off+n] for name,(off,n,a) in offsets.items()}
        end=max(off+n for off,n,a in offsets.values());assert b[end:]==bytes([165])*(probe.STRIDE-end)
        rows.append(row)
    return rows

def verify(c,args,checks,r):
    assert r['success'] and r['hit_return'] and not r['emulation_error'],(c['name'],r['emulation_error'],r['trace_tail'])
    assert not r['protected_write_or_execution_failures'] and r['function_or_vtable_substitutions']==0
    assert len(r['before_calls'])==len(args['before_calls']) and all(x['returned'] for x in r['before_calls'])
    md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;blob=EXE.read_bytes()
    for x in r['observed_reads']+r['observed_writes']:
        if int(x['address'],16)>=4096:continue
        ins=next(md.disasm(p.pe_read(blob,int(x['pc'],16),15),int(x['pc'],16)))
        assert int(x['address'],16)==0 and x['size']==4 and 0x64 in ins.prefix and 'fs:[0]' in ins.op_str,(c['name'],x)
    post=post_context(checks,r);rows=snapshots(c,post)
    owner=u(checks[probe.ACTOR],0x2A)
    assert int(r['registers']['EAX'],16)==owner
    assert u(post[probe.old.prior.readable.COUNTER],4)==0
    assert post[probe.old.UNRELATED]==checks[probe.old.UNRELATED]
    other=u(checks[probe.old.UNRELATED],0x2A)
    assert post[other][0x108A:0x109C]==checks[other][0x108A:0x109C]
    def at(a,n):
        base=max(b for b,v in post.items() if b<=a and a+n<=b+len(v));return post[base][a-base:a-base+n]
    bell=u(post[probe.OUT],0);b=at(bell,393);belltag=u(b,4,2)
    assert bell and b[0x12C]==142 and u(post[0x6EE540],4*belltag)==bell and post[0x6ED540][belltag]==1
    assert belltag==102 and u(rows[0]['room'],0xE,2)==102
    assert u(rows[0]['room'],0x49,2)==(belltag if c.get('bell',True) else 0)
    member=c.get('entry',True) or c.get('member',True)
    state0=c.get('entry_state',275) if c.get('entry',True) else 275
    assert u(rows[0]['actor'],0x25E)==state0
    assert u(rows[0]['actor'],0x3B9,2)==(300 if member else 0)
    assert u(rows[0]['room'],0xA,2)==(101 if member else 0)
    assert r['visited_instructions'].get('0x47afb0',0)==int(c.get('entry',True))
    assert r['visited_instructions'].get('0x4e5a70',0)==int(c.get('entry',True))
    slot=counter_slot(c['species'])-0x108A
    registered=c.get('register',True) if c.get('registration')!='ai' else c.get('bell',True)
    values=[0]*18
    if registered:values[slot]=1
    assert list(rows[0]['owner'][0x108A:0x109C])==[0]*18
    assert list(rows[1]['owner'][0x108A:0x109C])==values
    assert u(rows[1]['room'],0x53,2)==(101 if registered else 0)
    if c.get('registration')=='ai':
        assert u(rows[1]['actor'],0x25E)==(6 if c.get('bell',True) else 274)
        assert r['visited_instructions'].get('0x483790',0)==1
    else:assert not r['visited_instructions'].get('0x483790',0)
    started=c.get('activate',True) and member and c.get('room_type',26)==26 and c.get('bell',True) and c.get('direction',0x600)==0x600 and not c.get('initial_action',0)
    action=80 if started else c.get('initial_action',0)
    assert u(rows[2]['actor'],0x3A9)==action
    for row in rows[2:4]:assert list(row['owner'][0x108A:0x109C])==values
    dancers=({13:1,21:40}.get(c['species'],0)) if started else 0
    assert u(rows[2]['room'],0x55)==dancers
    if dancers:assert u(rows[2]['room'],0xE,2)==102+dancers
    healed=started and c.get('tick',True) and c['species'] in [5,34]
    delta={5:5,34:8}.get(c['species'],0) if healed else 0
    assert u(rows[3]['actor'],0x40)==100+delta and u(rows[3]['recipient'],0x40)==100+delta
    if started and c.get('tick',True) and c['species'] in [13,21]:
        wanted=945 if c['species']==13 else 863
        for name in ['actor','recipient']:assert [u(rows[3][name],0x26E+4*i) for i in range(6)]==[wanted]*6
    exited=c.get('exit',True)
    finalvalues=[0]*18 if exited and registered and c.get('room_type',26)==26 else values
    for index in [4,5]:
        row=rows[index];assert list(row['owner'][0x108A:0x109C])==finalvalues
        assert u(row['room'],0x53,2)==(0 if exited and registered and c.get('room_type',26)==26 else (101 if registered else 0))
        assert u(row['actor'],0x3B9,2)==(0 if exited else (300 if member else 0))
        assert u(row['room'],0xA,2)==(0 if exited else (101 if member else 0))
    if exited:assert u(rows[4]['actor'],0x25E)==1
    assert u(rows[4]['room'],0x55)==(0 if exited and registered else dancers)
    if exited and registered:assert u(rows[4]['room'],0xE,2)==102
    nexttick=c.get('tick_after_exit') or c.get('full_tick_after_exit')
    second=delta if nexttick and not c.get('stop_action_after_exit') else 0
    assert u(rows[5]['actor'],0x40)==100+delta+second
    assert u(rows[5]['recipient'],0x40)==100+delta+second
    assert u(rows[5]['actor'],0x3A9)==(0 if c.get('stop_action_after_exit') else action)
    if c.get('ai_after_exit') or c.get('full_tick_after_exit'):assert u(rows[5]['actor'],0x25E)==89
    for row in rows:
        assert u(row['actor'],0x40)>=100 and row['actor'][1343:]==checks[probe.ACTOR][1343:]
    assert post[0x6ED540][101]==1
    assert len(r['hp_requests'])==(2*int(healed)+2*int(bool(second)))
    assert r['visited_instructions'].get('0x4d8e60',0)==int(registered)*(1+int(exited))
    assert r['visited_instructions'].get('0x4d9570',0)==int(exited and registered)
    assert r['visited_instructions'].get('0x4b7b10',0)==(dancers if exited and registered else 0)
    # Complete allocator byte balance is retained; the only alwayslive
    # allocation is Bell142, with no unrelated Shot spawned in this scene.
    count=1+(0 if exited and registered else dancers)
    assert u(post[p.f.WORLD],0x2A)==count and u(post[p.f.WORLD],0x76)==408*count
    for a in [p.target.RAW,probe.old.prior.RAWS,probe.old.VARIABLE,0x66D524,0x66D654,0x66D754,0x66D86C,0x66E3EC,0x670574]:assert post[a]==checks[a]
    if probe.TRIANGLES in checks:assert post[probe.TRIANGLES][-64:]==bytes([165])*64
    simple=[]
    for i,row in enumerate(rows):
        a,room= row['actor'],row['room']
        simple.append(dict(checkpoint=i,state=u(a,0x25E),action=u(a,0x3A9),HP=u(a,0x40),recipientHP=u(row['recipient'],0x40),
            roomtag=u(a,0x3B9,2),registered_tag=u(room,0x53,2),roomhead=u(room,0xA,2),objecthead=u(room,0xE,2),room55=u(room,0x55),
            counters=list(row['owner'][0x108A:0x109C])))
    return post,dict(checkpoints=simple,started=started,registered=registered,dancers_created=dancers,
        participant_alive=True,lower_page_only_FS0=True,native_steps=r['instruction_count'],
        full_Creature_ticks=r['visited_instructions'].get('0x488930',0))

def main():
    rows=[]
    for c in cases():
        args,ch,s,file=execute(c);_,out=verify(c,args,ch,s['result'])
        rows.append(dict(case=c,file=str(file.relative_to(ROOT)),sha256=sha(file.read_bytes()),outcome=out))
        print(c['name'],out['registered'],out['started'],out['checkpoints'][-1]['HP'],out['dancers_created'],flush=True)
    (ROOT/'phase181-crypt-chain-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows),indent=2),encoding='utf-8')

if __name__=='__main__':main()
