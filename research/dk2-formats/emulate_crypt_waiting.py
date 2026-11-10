"""Strict phase187 checks for actual Crypt state276 and living waiting visitors."""
from pathlib import Path
import json
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
import probe_crypt_waiting as probe
import probe_crypt_crowd as crowd
from emulate_resistance import post_context
ROOT=Path(__file__).resolve().parent
p,EXE,sha,u=probe.p,probe.EXE,probe.sha,probe.u
def cases():return probe.cases()+[dict(name='King-crowd',king_crowd=True)]

def execute(c):
    a,ch,s,f,_=crowd.execute() if c.get('king_crowd') else probe.execute(c)
    return a,ch,s,f

def snapshots(c,post):
    offsets=crowd.build()[2] if c.get('king_crowd') else probe.build(c)[2];rows=[]
    for i in range(6):
        b=post[probe.LOG][i*probe.STRIDE:(i+1)*probe.STRIDE];assert u(b,0)==i
        end=max(o+n for o,n,a in offsets.values());assert b[end:]==bytes([165])*(probe.STRIDE-end)
        rows.append({k:b[o:o+n] for k,(o,n,a) in offsets.items()})
    return rows

def verify(c,args,checks,r):
    assert r['success'] and r['hit_return'] and not r['emulation_error']
    assert not r['protected_write_or_execution_failures'] and r['function_or_vtable_substitutions']==0
    assert len(r['before_calls'])==len(args['before_calls']) and all(x['returned'] for x in r['before_calls'])
    md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;blob=EXE.read_bytes()
    for x in r['observed_reads']+r['observed_writes']:
        if int(x['address'],16)>=4096:continue
        pc=int(x['pc'],16);ins=next(md.disasm(p.pe_read(blob,pc,15),pc))
        assert int(x['address'],16)==0 and x['size']==4 and 0x64 in ins.prefix and 'fs:[0]' in ins.op_str,(c['name'],x)
    post=post_context(checks,r);rows=snapshots(c,post)
    owner=u(checks[probe.ACTOR],0x2A);foreign=u(checks[probe.old.old.UNRELATED],0x2A)
    assert int(r['registers']['EAX'],16)==owner
    assert u(post[probe.old.old.prior.readable.COUNTER],4)==0
    assert post[probe.old.old.UNRELATED]==checks[probe.old.old.UNRELATED]
    assert post[foreign][0x108A:0x109C]==checks[foreign][0x108A:0x109C]
    assert not r['hp_requests'] and not r['aoe_entries']
    if c.get('king_crowd'):return verify_crowd(c,args,checks,r,post,rows)
    occupied=c['occupied'];dancers=c['dancers'];released=c.get('release_other',False)
    wanted=[c.get('entry_state',274)]+([276]*5 if occupied and dancers else [1]*5 if occupied else [6]*5)
    if released:wanted[4:]=[1,1]
    simplified=[]
    for i,row in enumerate(rows):
        a,other,room,ow=row['actor'],row['recipient'],row['room'],row['owner']
        exited=released and i>=4;rejected=occupied and not dancers and i>=1
        registered=0 if exited else 100 if occupied else 101 if i>=1 else 0
        counters=[0]*18
        if registered:counters[4 if occupied else 0]=1
        assert u(a,0x25E)==wanted[i] and u(a,0x3A9)==(76 if occupied and dancers and i>=2 else 0),(c['name'],i)
        assert u(room,0x53,2)==registered and list(ow[0x108A:0x109C])==counters
        assert u(room,0x55)==(0 if exited else dancers)
        assert u(a,0x3B9,2)==(0 if exited or rejected else 300)
        assert u(other,0x3B9,2)==(300 if occupied and not exited else 0)
        assert u(other,0x25E)==(275 if occupied and not exited else 1)
        for key,node,at in [('actor',a,probe.ACTOR),('recipient',other,probe.OTHER)]:
            assert u(node,0x40)==900 and u(node,0x12)==0
            assert node[1343:]==checks[at][1343:]
        # Native room insertion/unlink, including the second actual Creature.
        expected=[] if exited else [100] if rejected else [101,100] if occupied else [101]
        tag=u(room,0xA,2);seen=[];previous=0
        bytag={101:a,100:other}
        while tag:
            assert tag not in seen and tag in bytag
            node=bytag[tag];assert u(node,0x236,2)==previous and u(node,0x3B9,2)==300
            seen.append(tag);previous=tag;tag=u(node,0x238,2)
        assert seen==expected,(c['name'],i,seen,expected)
        simplified.append(dict(checkpoint=i,state=u(a,0x25E),action=u(a,0x3A9),HP=u(a,0x40),otherHP=u(other,0x40),
            registered_tag=registered,counters=counters,room_members=seen,dancers=u(room,0x55)))
    def at(a,n):
        base=next(b for b,v in post.items() if b<=a and a+n<=b+len(v));return post[base][a-base:a-base+n]
    # Verify all actual map lists and room Object lists, not just raw counts.
    live_dancers=dancers and not released;expected_tags={100,101,102}|({103} if live_dancers else set())
    seen=set()
    for off in range(0,len(post[p.f.MAP]),16):
        tag=u(post[p.f.MAP],off,2);previous=0
        while tag:
            assert tag not in seen;seen.add(tag);node=at(u(post[0x6EE540],tag*4),42)
            assert u(node,0xA,2)==previous and ((u(node,0x1E)>>12)*32+(u(node,0x1A)>>12))*16==off
            previous,tag=tag,u(node,0xC,2)
    assert seen==expected_tags,(c['name'],seen,expected_tags)
    tag=u(post[probe.ROOM],0xE,2);previous=0;object_tags=[]
    while tag:
        assert tag not in object_tags;node=at(u(post[0x6EE540],tag*4),393)
        assert u(node,0x128,2)==previous and u(node,0x138,2)==300
        assert node[0x12C]==(142 if tag==102 else 143)
        object_tags.append(tag);previous,tag=tag,u(node,0x12A,2)
    assert object_tags==([103,102] if live_dancers else [102])
    for tag in [100,101,102]:assert post[0x6ED540][tag]==1
    if dancers:assert post[0x6ED540][103]==int(not released)
    assert u(post[p.f.WORLD],0x2A)==1+int(bool(live_dancers))
    assert u(post[p.f.WORLD],0x76)==408*(1+int(bool(live_dancers)))
    visits=lambda a:r['visited_instructions'].get(hex(a),0)
    assert visits(0x483790)==int(occupied) and visits(0x4839E0)==(c['ai_steps']-1 if occupied else 1)
    assert visits(0x496700)==int(occupied and bool(dancers)) and visits(0x499CA0)==0
    assert visits(0x4D9570)==int(released) and visits(0x4D9410)==int(bool(dancers))
    assert visits(0x4E62D0)==visits(0x4B7B10)==int(released)
    # No direct waiting/registration/activation callback is supplied for101.
    forbidden={0x483790,0x4839E0,0x483870,0x496700,0x499CA0}
    assert not forbidden&{int(x['address'],16) for x in args['before_calls']}
    for address in [p.target.RAW,probe.old.old.prior.RAWS,probe.old.old.VARIABLE,0x66D524,0x66D654,0x66D754,0x66D86C,0x66DB34,0x66E3EC,0x670574]:
        if address in checks:assert post[address]==checks[address]
    return post,dict(checkpoints=simplified,both_Creatures_alive=True,waiting_adds_no_counter=True,
        map_and_room_lists_consistent=True,source_and_vtables_preserved=True,lower_page_only_FS0=True,
        AI_dispatches=visits(0x476FC0),waiting_callbacks=visits(0x4839E0),action76_starts=visits(0x496700),
        active80_callbacks=visits(0x499CA0),cleanup_calls=visits(0x4D9570),native_steps=r['instruction_count'])

def verify_crowd(c,args,checks,r,post,rows):
    simplified=[]
    for i,row in enumerate(rows):
        king,visitor,room,owner=row['actor'],row['recipient'],row['room'],row['owner']
        assert king[0x36F]==21 and visitor[0x36F]==8
        assert u(king,0x25E)==[274,6,275,275,1,1][i] and u(king,0x3A9)==(80 if i>=2 else 0)
        assert u(visitor,0x25E)==(276 if i==3 else 1) and u(visitor,0x3A9)==(76 if i>=3 else 0)
        expected=[] if i>=4 else [100,101] if i==3 else [101]
        counters=[0]*18
        if 1<=i<=3:counters[9]=1
        assert list(owner[0x108A:0x109C])==counters and u(room,0x53,2)==(101 if 1<=i<=3 else 0)
        dancers=40 if i in [2,3] else 0;assert u(room,0x55)==dancers
        nodes={101:king,100:visitor};tag=u(room,0xA,2);seen=[];previous=0
        while tag:
            assert tag not in seen and tag in nodes;node=nodes[tag]
            assert u(node,0x236,2)==previous and u(node,0x3B9,2)==300
            seen.append(tag);previous,tag=tag,u(node,0x238,2)
        assert seen==expected
        for node,addr in [(king,probe.ACTOR),(visitor,probe.OTHER)]:
            assert u(node,0x40)==900 and u(node,0x12)==0 and node[1343:]==checks[addr][1343:]
        simplified.append(dict(checkpoint=i,state=u(king,0x25E),action=u(king,0x3A9),HP=900,otherHP=900,
            visitor_state=u(visitor,0x25E),visitor_action=u(visitor,0x3A9),registered_tag=u(room,0x53,2),
            counters=counters,room_members=seen,dancers=dancers))
    def at(a,n):
        b=next(b for b,v in post.items() if b<=a and a+n<=b+len(v));return post[b][a-b:a-b+n]
    assert u(post[probe.ROOM],0xE,2)==102
    bell=at(u(post[0x6EE540],102*4),393);assert bell[0x12C]==142 and u(bell,0x138,2)==300
    for tag in [100,101,102]:assert post[0x6ED540][tag]==1
    for tag in range(103,143):assert post[0x6ED540][tag]==0 and u(post[0x6EE540],tag*4)==0
    seen=set()
    for off in range(0,len(post[p.f.MAP]),16):
        tag=u(post[p.f.MAP],off,2);previous=0
        while tag:
            assert tag not in seen;seen.add(tag);node=at(u(post[0x6EE540],tag*4),42)
            assert u(node,0xA,2)==previous and ((u(node,0x1E)>>12)*32+(u(node,0x1A)>>12))*16==off
            previous,tag=tag,u(node,0xC,2)
    assert seen=={100,101,102}
    assert u(post[p.f.WORLD],0x2A)==1 and u(post[p.f.WORLD],0x76)==408
    for a in [p.target.RAW,probe.old.old.prior.RAWS,probe.old.old.VARIABLE,0x66D524,0x66D654,0x66D754,0x66D86C,0x66DB34,0x66E3EC,0x670574]:
        if a in checks:assert post[a]==checks[a]
    visits=lambda a:r['visited_instructions'].get(hex(a),0)
    assert visits(0x483790)==2 and visits(0x483870)==visits(0x4839E0)==visits(0x4E8DA0)==1
    assert visits(0x4D9410)==1 and visits(0x4D9570)==1 and visits(0x4E62D0)==visits(0x4B7B10)==40
    assert visits(0x496700)==2 and visits(0x499CA0)==0
    assert not {0x4D9410,0x483790,0x483870,0x4839E0,0x496700}&{int(x['address'],16) for x in args['before_calls']}
    return post,dict(checkpoints=simplified,both_Creatures_alive=True,waiting_adds_no_counter=True,
        map_and_room_lists_consistent=True,source_and_vtables_preserved=True,lower_page_only_FS0=True,
        AI_dispatches=visits(0x476FC0),waiting_callbacks=visits(0x4839E0),action76_starts=1,active80_callbacks=0,
        cleanup_calls=1,native_steps=r['instruction_count'],native_King_Dancer_count=40,host_Dancer_creation_calls=0)

def main():
    rows=[]
    for c in cases():
        args,ch,s,file=execute(c);_,out=verify(c,args,ch,s['result'])
        rows.append(dict(case=c,file=str(file.relative_to(ROOT)),sha256=sha(file.read_bytes()),outcome=out))
        print(c['name'],out['checkpoints'][-1],flush=True)
    (ROOT/'phase187-crypt-waiting-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows),indent=2),encoding='utf-8')

if __name__=='__main__':main()
