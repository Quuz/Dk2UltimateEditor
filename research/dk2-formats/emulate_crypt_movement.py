"""Phase182: original AI registration, force-driven walk, turning, ritual and exit.

The state274 request, room, clock and optional speed256 are explicit fixture
conditions. No callback/action, desired velocity or post-entry XY is supplied.
"""
from pathlib import Path
import json
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
import probe_crypt_movement as probe
from emulate_resistance import post_context
ROOT=Path(__file__).resolve().parent
p,EXE,sha,u=probe.p,probe.EXE,probe.sha,probe.u

def cases():
    base=dict(species=5,owner=7,registration='ai',register_dispatch=True,entry_state=274,activate=False,HP=900)
    walk=dict(x=22528,room_area=True,whole_movement_ticks=True,native_forces=True,source_trig=True,native_speed=256,null_renderer_cache=True)
    return [dict(base,**kw,name=name) for name,kw in [
        ('ready-one-AI',dict(movement_steps=1,tick=False,exit=False)),
        ('ready-two-AI-cycle',dict(movement_steps=2,full_tick_after_exit=True)),
        ('ready-owner8-cycle',dict(movement_steps=2,full_tick_after_exit=True,owner=8)),
        ('walk64-turning',dict(walk,movement_steps=64,tick=False,exit=False)),
        ('walk72-ritual',dict(walk,movement_steps=72,tick=False,exit=False)),
        ('walk72-cycle',dict(walk,movement_steps=72,full_tick_after_exit=True)),
        ('walk72-clear-action',dict(walk,movement_steps=72,full_tick_after_exit=True,stop_action_after_exit=True)),
        ('walk72-no-forces',dict(walk,movement_steps=72,native_forces=False,tick=False,exit=False)),
        ('walk32-fast',dict(walk,movement_steps=32,native_speed=4095,tick=False,exit=False)),
    ]]

def execute(c):
    args,ch,s,file,_=probe.execute(c)
    return args,ch,s,file

def snapshots(c,post):
    offsets=probe.build(c)[2];rows=[]
    for i in range(6):
        b=post[probe.LOG][i*probe.STRIDE:(i+1)*probe.STRIDE]
        assert u(b,0)==i
        end=max(o+n for o,n,a in offsets.values())
        assert b[end:]==bytes([165])*(probe.STRIDE-end)
        rows.append({k:b[o:o+n] for k,(o,n,a) in offsets.items()})
    return rows

def verify(c,args,checks,r):
    assert r['success'] and r['hit_return'] and not r['emulation_error'],(c['name'],r['emulation_error'],r['trace_tail'])
    assert not r['protected_write_or_execution_failures'] and r['function_or_vtable_substitutions']==0
    assert len(r['before_calls'])==len(args['before_calls']) and all(x['returned'] for x in r['before_calls'])
    # Low-page successes are accepted only for the actual x86 SEH chain.
    md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;blob=EXE.read_bytes()
    for x in r['observed_reads']+r['observed_writes']:
        if int(x['address'],16)>=4096:continue
        pc=int(x['pc'],16);ins=next(md.disasm(p.pe_read(blob,pc,15),pc))
        assert int(x['address'],16)==0 and x['size']==4 and 0x64 in ins.prefix and 'fs:[0]' in ins.op_str,(c['name'],x)
    post=post_context(checks,r);rows=snapshots(c,post)
    owner=u(checks[probe.ACTOR],0x2A);other=u(checks[probe.old.UNRELATED],0x2A)
    assert int(r['registers']['EAX'],16)==owner
    assert u(post[probe.old.prior.readable.COUNTER],4)==0
    assert post[probe.old.UNRELATED]==checks[probe.old.UNRELATED]
    assert post[other][0x108A:0x109C]==checks[other][0x108A:0x109C]
    def at(a,n):
        base=next(b for b,v in post.items() if b<=a and a+n<=b+len(v))
        return post[base][a-base:a-base+n]
    bell=u(post[probe.OUT],0);b=at(bell,393)
    assert u(b,4,2)==102 and b[0x12C]==142 and u(post[0x6EE540],102*4)==bell and post[0x6ED540][102]==1
    assert u(post[p.f.WORLD],0x2A)==1 and u(post[p.f.WORLD],0x76)==408
    # Every actual map node is present once, in its coordinate cell, with
    # consistent previous/next links, including the real Bell allocation.
    present=set();tilebytes=post[p.f.MAP]
    for off in range(0,len(tilebytes),16):
        tag=u(tilebytes,off,2);previous=0
        while tag:
            assert tag not in present,(c['name'],'duplicate map tag',tag)
            present.add(tag);address=u(post[0x6EE540],tag*4);node=at(address,42)
            assert u(node,0xA,2)==previous and ((u(node,0x1E)>>12)*32+(u(node,0x1A)>>12))*16==off,(c['name'],tag,off)
            previous,tag=tag,u(node,0xC,2)
    assert present=={100,101,102},(c['name'],present)
    assert [u(rows[i]['actor'],0x25E) for i in [0,1]]==[274,6]
    assert [u(rows[i]['room'],0x53,2) for i in [0,1]]==[0,101]
    counters=[1]+[0]*17
    assert list(rows[0]['owner'][0x108A:0x109C])==[0]*18
    for row in rows[1:4]:assert list(row['owner'][0x108A:0x109C])==counters
    name=c['name'];walk=name.startswith('walk');started=name in ['ready-two-AI-cycle','ready-owner8-cycle','walk72-ritual','walk72-cycle','walk72-clear-action']
    state=6 if name in ['walk72-no-forces','walk32-fast'] else 275
    assert u(rows[2]['actor'],0x25E)==state and u(rows[2]['actor'],0x3A9)==(80 if started else 0),(name,state)
    pos=lambda row:[u(row['actor'],o) for o in [0x1A,0x1E,0x22]]
    if walk:
        assert pos(rows[0])==[22528,26624,0] and pos(rows[1])==pos(rows[0])
        if name=='walk72-no-forces':assert pos(rows[2])[:2]==[22528,26624]
        elif name=='walk32-fast':assert pos(rows[2])[0]>32768
        else:assert pos(rows[2])==[30469,26615,4096]
    else:assert pos(rows[2])[:2]==[30720,26624]
    if started:assert u(rows[2]['actor'],0xF4,2)==0x600
    hp2=905 if walk and started else 900
    hp3=hp2+(5 if started and c.get('tick',True) else 0)
    hp5=hp3+(5 if started and c.get('full_tick_after_exit') and not c.get('stop_action_after_exit') else 0)
    for i,wanted in [(0,900),(1,900),(2,hp2),(3,hp3),(4,hp3),(5,hp5)]:
        for k in ['actor','recipient']:assert u(rows[i][k],0x40)==wanted,(name,i,k,u(rows[i][k],0x40),wanted)
    exited=c.get('exit',True)
    for i in [4,5]:
        assert list(rows[i]['owner'][0x108A:0x109C])==([0]*18 if exited else counters)
        assert u(rows[i]['room'],0x53,2)==(0 if exited else 101)
        assert u(rows[i]['room'],0xA,2)==(0 if exited else 101)
        assert u(rows[i]['actor'],0x3B9,2)==(0 if exited else 300)
    if exited:assert u(rows[4]['actor'],0x25E)==1
    if c.get('full_tick_after_exit'):assert u(rows[5]['actor'],0x25E)==89
    assert u(rows[5]['actor'],0x3A9)==(0 if c.get('stop_action_after_exit') else (80 if started else 0))
    for row in rows:
        assert u(row['room'],0xE,2)==102 and u(row['room'],0x49,2)==102 and u(row['room'],0x55)==0
        assert row['actor'][1343:]==checks[probe.ACTOR][1343:]
    assert post[0x6ED540][101]==post[0x6ED540][100]==1
    assert len(r['hp_requests'])==2*((hp5-900)//5)
    visit=lambda a:r['visited_instructions'].get(hex(a),0)
    assert visit(0x4D8E60)==1+int(exited) and visit(0x4D9570)==int(exited)
    assert visit(0x496700)==int(started) and visit(0x47AFB0)==visit(0x4E5A70)==1
    # Registration, arrival and action callbacks occur through original AI.
    assert not ({int(x['address'],16) for x in args['before_calls']} & {0x483790,0x483870,0x4D8E60,0x496700})
    assert visit(0x483790)==1
    if state==6 or name=='ready-one-AI':assert visit(0x483870)==0
    else:assert visit(0x483870)>0
    assert visit(0x488930)==(c['movement_steps'] if c.get('whole_movement_ticks') else 0)+int(bool(c.get('full_tick_after_exit')))
    for a in [p.target.RAW,probe.old.prior.RAWS,probe.old.VARIABLE,probe.SPELL_RAW,0x66D524,0x66D654,0x66D754,0x66D86C,0x66DB34,0x66E3EC,0x670574]:
        if a in checks:assert post[a]==checks[a],(name,hex(a))
    if probe.TRIANGLES in checks:assert post[probe.TRIANGLES][-64:]==bytes([165])*64
    if c.get('source_trig'):assert post[0x6C1488]==p.pe_read(blob,0x6C1488,0x2800)
    force_rows=[]
    if c.get('native_forces'):
        vector=at(0x6ECAE0,12)
        assert [u(vector,4*j) for j in range(3)]==[probe.FORCES,probe.FORCES+32,probe.FORCES+256]
        force_rows=[[u(post[probe.FORCES],8*j+k) for k in [0,4]] for j in range(4)]
        assert force_rows==[[0x9999,0x6ECAF0],[0x6666,0x6ED3B0],[0x5999,0x6ED388],[0x2147,0x6ED358]]
        assert post[probe.FORCES][32:256]==bytes(224) and post[probe.FORCES][-64:]==bytes([165])*64
        assert visit(0x4D56E0)==1 and visit(0x4D3C40)>0
    simple=[]
    for i,row in enumerate(rows):
        a=row['actor'];simple.append(dict(checkpoint=i,state=u(a,0x25E),action=u(a,0x3A9),HP=u(a,0x40),recipientHP=u(row['recipient'],0x40),
            position=pos(row),direction=u(a,0xF4,2),roomtag=u(a,0x3B9,2),registered_tag=u(row['room'],0x53,2),
            counters=list(row['owner'][0x108A:0x109C])))
    return post,dict(checkpoints=simple,started=started,participant_alive=True,lower_page_only_FS0=True,map_lists_consistent=True,
        native_steps=r['instruction_count'],full_Creature_ticks=visit(0x488930),force_rows=force_rows,
        direct_Crypt_callback_or_action_calls=0)

def main():
    rows=[]
    for c in cases():
        args,ch,s,file=execute(c);_,out=verify(c,args,ch,s['result'])
        rows.append(dict(case=c,file=str(file.relative_to(ROOT)),sha256=sha(file.read_bytes()),outcome=out))
        print(c['name'],out['started'],out['checkpoints'][2]['position'],out['checkpoints'][-1]['HP'],flush=True)
    (ROOT/'phase182-crypt-movement-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows),indent=2),encoding='utf-8')

if __name__=='__main__':main()
