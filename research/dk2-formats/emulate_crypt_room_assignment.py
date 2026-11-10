"""Phase183 strict generic room entry, raw Hate Jobs, drop and recovery boundaries."""
from pathlib import Path
import json
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
import probe_crypt_room_assignment as probe
import emulate_crypt_movement as prior
from emulate_resistance import post_context
ROOT=Path(__file__).resolve().parent
p,EXE,sha,u=probe.p,probe.EXE,probe.sha,probe.u

def cases():
    base=dict(species=5,owner=7,entry_state=274,register=False,activate=False,tick=False,exit=False,native_nav=True,HP=900)
    walk=dict(registration='ai',register_dispatch=True,movement_steps=72,whole_movement_ticks=True,tick=True,exit=True,
        full_tick_after_exit=True,x=22528,room_area=True,native_forces=True,source_trig=True,native_speed=256,null_renderer_cache=True)
    return [dict(base,**kw,name=name) for name,kw in [
        ('generic-room-entry',{}),('generic-Imp',dict(species=1)),('generic-King',dict(species=21)),
        ('generic-Wizard',dict(species=16)),('generic-Fairy',dict(species=20)),('generic-last-species',dict(species=46)),
        ('hate1-one',dict(raw_hate=1)),('hate1-17',dict(raw_hate=17)),('hate1-255',dict(raw_hate=255)),('hate1-allbits',dict(raw_hate=0xFFFFFFFF)),
        ('mode1-query',dict(assignment_mode=1)),('blocked-status',dict(blocked_status=1)),
        ('hand-drop',dict(entry_path='hand_drop')),
        ('drop-one-AI',dict(entry_path='hand_drop',registration='ai',register_dispatch=True)),
        ('generic-walk-cycle',walk),('generic-walk-clear',dict(walk,stop_action_after_exit=True)),
        ('after-recovery-walk-cycle',dict(walk,entry_path='post_recovery')),
    ]]

def execute(c):
    a,ch,s,f,_=probe.execute(c);return a,ch,s,f

def snapshots(c,post):
    offsets=probe.build(c)[2];rows=[]
    for i in range(6):
        b=post[probe.LOG][i*probe.STRIDE:(i+1)*probe.STRIDE];assert u(b,0)==i
        end=max(o+n for o,n,a in offsets.values());assert b[end:]==bytes([165])*(probe.STRIDE-end)
        rows.append({k:b[o:o+n] for k,(o,n,a) in offsets.items()})
    return rows

def verify(c,args,checks,r):
    assert r['success'] and r['hit_return'] and not r['emulation_error'],(c['name'],r['emulation_error'],r['trace_tail'])
    assert not r['protected_write_or_execution_failures'] and r['function_or_vtable_substitutions']==0
    assert len(r['before_calls'])==len(args['before_calls']) and all(x['returned'] for x in r['before_calls'])
    md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;blob=EXE.read_bytes()
    for x in r['observed_reads']+r['observed_writes']:
        if int(x['address'],16)>=4096:continue
        pc=int(x['pc'],16);ins=next(md.disasm(p.pe_read(blob,pc,15),pc))
        assert int(x['address'],16)==0 and x['size']==4 and 0x64 in ins.prefix and 'fs:[0]' in ins.op_str,(c['name'],x)
    post=post_context(checks,r);rows=snapshots(c,post)
    owner=u(checks[probe.ACTOR],0x2A);other=u(checks[probe.old.old.UNRELATED],0x2A)
    # The getter returns owner-pointer, not the owner's numeric roomID.
    assert int(r['registers']['EAX'],16)==owner
    assert u(post[probe.old.old.prior.readable.COUNTER],4)==0
    assert post[probe.old.old.UNRELATED]==checks[probe.old.old.UNRELATED]
    assert post[other][0x108A:0x109C]==checks[other][0x108A:0x109C]
    def at(a,n):
        base=next(b for b,v in post.items() if b<=a and a+n<=b+len(v));return post[base][a-base:a-base+n]
    bell=u(post[probe.old.OUT],0);b=at(bell,393)
    assert u(b,4,2)==102 and b[0x12C]==142 and u(post[0x6EE540],102*4)==bell and post[0x6ED540][102]==1
    assert u(post[p.f.WORLD],0x2A)==1 and u(post[p.f.WORLD],0x76)==408
    result=post[probe.ENTRY_RESULT];assert result[4:]==checks[probe.ENTRY_RESULT][4:]
    assert u(result,0)==(0 if c.get('blocked_status') or c.get('entry_path')=='post_recovery' else 1)
    hand=c.get('entry_path')=='hand_drop';query=c.get('assignment_mode')==1;blocked=bool(c.get('blocked_status'))
    entered=not (hand or query or blocked);registered=entered and c.get('registration')=='ai'
    walked=bool(c.get('movement_steps'));state0=76 if hand else 1 if query or blocked else 274
    assert u(rows[0]['actor'],0x25E)==state0
    if hand:
        assert post[owner][0x1BF]==0 and u(post[owner],0xB2,2)==101
        assert not (u(rows[0]['actor'],0x3E6)&1) and u(rows[0]['actor'],0x16)==0
    assert u(rows[0]['actor'],0x3B9,2)==(300 if entered else 0)
    assert u(rows[0]['room'],0x53,2)==0
    counters=[int(registered)]+[0]*17
    assert list(rows[0]['owner'][0x108A:0x109C])==[0]*18
    for row in rows[1:4]:assert list(row['owner'][0x108A:0x109C])==counters
    state1=6 if registered else 255 if hand and c.get('registration')=='ai' else state0
    assert u(rows[1]['actor'],0x25E)==state1
    for row in rows[1:4]:assert u(row['room'],0x53,2)==(101 if registered else 0)
    started=walked
    assert u(rows[2]['actor'],0x25E)==(275 if walked else state1)
    action=80 if walked else 52 if state1==255 else 0
    assert u(rows[2]['actor'],0x3A9)==action
    pos=lambda row:[u(row['actor'],o) for o in [0x1A,0x1E,0x22]]
    if walked:
        expected=[30644,26617,4096] if c.get('entry_path')=='post_recovery' else [30469,26615,4096]
        assert pos(rows[2])==expected and u(rows[2]['actor'],0xF4,2)==0x600
        assert pos(rows[0])==([24018,26624,4096] if c.get('entry_path')=='post_recovery' else [22528,26624,0])
    hp2=910 if walked and c.get('entry_path')=='post_recovery' else 905 if walked else 900
    hp3=hp2+(5 if walked else 0);hp5=hp3+(5 if walked and not c.get('stop_action_after_exit') else 0)
    for i,wanted in [(0,900),(1,900),(2,hp2),(3,hp3),(4,hp3),(5,hp5)]:
        for k in ['actor','recipient']:assert u(rows[i][k],0x40)==wanted,(c['name'],i,k,u(rows[i][k],0x40),wanted)
    exited=c.get('exit',False)
    for i in [4,5]:
        assert list(rows[i]['owner'][0x108A:0x109C])==([0]*18 if exited else counters)
        assert u(rows[i]['actor'],0x3B9,2)==(300 if entered and not exited else 0)
        assert u(rows[i]['room'],0xA,2)==(101 if entered and not exited else 0)
    assert u(rows[5]['actor'],0x3A9)==(0 if c.get('stop_action_after_exit') else action)
    if walked:assert u(rows[4]['actor'],0x25E)==1 and u(rows[5]['actor'],0x25E)==89
    for row in rows:
        assert u(row['actor'],0x2BC)==0 and u(row['room'],0xE,2)==102 and u(row['room'],0x55)==0
        assert row['actor'][1343:]==checks[probe.ACTOR][1343:]
    assert post[0x6ED540][101]==post[0x6ED540][100]==1
    visit=lambda a:r['visited_instructions'].get(hex(a),0)
    assert visit(0x4E8DA0)==(0 if hand and not c.get('registration') else 1)
    assert visit(0x494480)==int(entered) and visit(0x48AD30)==2*int(entered)+int(hand)
    # The hate check's actual request is0 at original4E9018. All stored
    # HateJobs must be ignored here, rather than inferred from output alone.
    if entered:
        assert visit(0x494480)==visit(0x4944BD)==1
        assert visit(0x49448E)==0 and visit(0x49449C)==0
    assert visit(0x4D8E60)==int(registered)*(1+int(exited)) and visit(0x4D9570)==int(registered and exited)
    assert visit(0x483790)==int(registered) and visit(0x496700)==int(walked)+int(state1==255)
    assert len(r['hp_requests'])==2*((hp5-900)//5)
    assert not ({int(x['address'],16) for x in args['before_calls']} & {0x483790,0x483870,0x4D8E60,0x496700})
    definition=u(post[probe.ACTOR],0x370)
    if 'raw_hate' in c:assert post[definition][0x66E]==(c['raw_hate']&255) and post[p.target.RAW]==checks[p.target.RAW]
    for a in [p.target.RAW,probe.old.old.prior.RAWS,probe.old.old.VARIABLE,probe.old.SPELL_RAW,0x66D524,0x66D654,0x66D754,0x66D86C,0x66DB34,0x66E3EC,0x670574]:
        if a in checks:assert post[a]==checks[a],(c['name'],hex(a))
    # Check ordinary list0. Actor+16=2 is the not-in-map sentinel; held
    # Creatures are absent from map lists, not linked through cellWORD4.
    present={};tilebytes=post[p.f.MAP]
    for off in range(0,len(tilebytes),16):
        for layer in [0]:
            tag=u(tilebytes,off+2*layer,2);previous=0
            while tag:
                assert tag not in present,(c['name'],'duplicate map tag',tag)
                present[tag]=(off//16,layer);node=at(u(post[0x6EE540],tag*4),42)
                assert u(node,0xA,2)==previous and u(node,0x16)==layer
                assert ((u(node,0x1E)>>12)*32+(u(node,0x1A)>>12))==off//16
                previous,tag=tag,u(node,0xC,2)
    assert set(present)=={100,101,102} and all(layer==0 for cell,layer in present.values())
    if c.get('source_trig'):assert post[0x6C1488]==p.pe_read(blob,0x6C1488,0x2800)
    if probe.old.TRIANGLES in checks:assert post[probe.old.TRIANGLES][-64:]==bytes([165])*64
    if c.get('native_forces'):
        vector=at(0x6ECAE0,12);assert [u(vector,4*j) for j in range(3)]==[probe.old.FORCES,probe.old.FORCES+32,probe.old.FORCES+256]
        assert post[probe.old.FORCES][-64:]==bytes([165])*64
    simple=[]
    for i,row in enumerate(rows):
        a=row['actor'];simple.append(dict(checkpoint=i,state=u(a,0x25E),action=u(a,0x3A9),HP=u(a,0x40),recipientHP=u(row['recipient'],0x40),
            position=pos(row),roomtag=u(a,0x3B9,2),registered_tag=u(row['room'],0x53,2),job=u(a,0x2BC),counters=list(row['owner'][0x108A:0x109C])))
    return post,dict(checkpoints=simple,entry_return=u(result,0),entered=entered,registered=registered,started=started,
        participant_alive=True,lower_page_only_FS0=True,map_lists_consistent=True,raw_hate_loaded=c.get('raw_hate'),
        native_steps=r['instruction_count'],whole_Creature_ticks=visit(0x488930))

def main():
    rows=[]
    for c in cases():
        args,ch,s,file=execute(c);_,out=verify(c,args,ch,s['result'])
        rows.append(dict(case=c,file=str(file.relative_to(ROOT)),sha256=sha(file.read_bytes()),outcome=out))
        print(c['name'],out['entry_return'],out['checkpoints'][0]['state'],out['checkpoints'][-1]['HP'],flush=True)
    (ROOT/'phase183-crypt-room-assignment-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows),indent=2),encoding='utf-8')

if __name__=='__main__':main()
