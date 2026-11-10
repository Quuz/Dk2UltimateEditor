"""Phase185 strict uninterrupted held/drop/stun/RenderInfo/Crypt compositions."""
from pathlib import Path
import json
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
import probe_crypt_drop_recovery as probe
from emulate_resistance import post_context
ROOT=Path(__file__).resolve().parent
p,EXE,sha,u=probe.p,probe.EXE,probe.sha,probe.u

def cases():
    return [dict(kw,name=n) for n,kw in [
        ('no-animation',{}),('short34',dict(animation_steps=34)),('endpoint35',dict(animation_steps=35)),
        ('complete36',dict(animation_steps=36)),('extra40',dict(animation_steps=40)),
        ('no-action-dispatch',dict(animation_steps=36,action_tick=False)),
        ('full-cycle',dict(animation_steps=36,walk_steps=72)),
        ('full-cycle-residual',dict(animation_steps=35,walk_steps=72)),
        ('full-cycle-clear',dict(animation_steps=36,walk_steps=72,clear_after_exit=True)),
    ]]

def execute(c):
    a,ch,s,f,_=probe.execute(c);return a,ch,s,f

def snapshots(c,post):
    ofs=probe.build(c)[2];out=[]
    for base,count in [(probe.old.LOG,6)]+([(0x29000000,3)] if c.get('walk_steps') else []):
        for i in range(count):
            b=post[base][i*probe.old.STRIDE:(i+1)*probe.old.STRIDE];assert u(b,0)==i
            end=max(o+n for o,n,at in ofs.values());assert b[end:]==bytes([165])*(probe.old.STRIDE-end)
            out.append({name:b[o:o+n] for name,(o,n,at) in ofs.items()})
    return out

def verify(c,a,ch,r):
    assert r['success'] and r['hit_return'] and not r['emulation_error'],(c['name'],r['emulation_error'])
    assert not r['protected_write_or_execution_failures'] and r['function_or_vtable_substitutions']==0
    assert len(r['before_calls'])==len(a['before_calls']) and all(x['returned'] for x in r['before_calls'])
    md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;blob=EXE.read_bytes()
    for x in r['observed_reads']+r['observed_writes']:
        if int(x['address'],16)>=4096:continue
        pc=int(x['pc'],16);ins=next(md.disasm(p.pe_read(blob,pc,15),pc))
        assert int(x['address'],16)==0 and x['size']==4 and 0x64 in ins.prefix and 'fs:[0]' in ins.op_str,(c['name'],x)
    post=post_context(ch,r);rows=snapshots(c,post);actor=probe.old.ACTOR;core=probe.old.old.old
    owner=u(ch[actor],0x2A);other=u(ch[core.UNRELATED],0x2A)
    assert int(r['registers']['EAX'],16)==owner and u(post[core.prior.readable.COUNTER],4)==0
    assert post[core.UNRELATED]==ch[core.UNRELATED] and post[other][0x108A:0x109C]==ch[other][0x108A:0x109C]
    assert u(post[probe.old.ENTRY_RESULT],0)==1 and post[probe.old.ENTRY_RESULT][4:]==ch[probe.old.ENTRY_RESULT][4:]
    assert post[owner][0x1BF]==0 and u(post[owner],0xB2,2)==101
    completed=c.get('animation_steps',0)>=36 and c.get('action_tick',True)
    wanted_states=[76,255,257,257,274 if completed else 257,6 if completed else 257]
    wanted_actions=[0,52,53,0 if completed else 53,0 if completed else 53,0 if completed else 53]
    for i,row in enumerate(rows[:6]):
        n=row['actor'];assert u(n,0x25E)==wanted_states[i] and u(n,0x3A9)==wanted_actions[i],(c['name'],i)
        assert u(n,0x40)==900 and u(row['recipient'],0x40)==900
        member=completed and i>=4;registered=completed and i==5
        assert u(n,0x3B9,2)==(300 if member else 0) and u(row['room'],0xA,2)==(101 if member else 0)
        assert u(row['room'],0x53,2)==(101 if registered else 0)
        assert list(row['owner'][0x108A:0x109C])==[int(registered)]+[0]*17
        position=[u(n,o) for o in [0x1A,0x1E,0x22]]
        assert position==([24018,26624,4096] if member else [22528,26624,3584])
    steps=c.get('animation_steps',0);n=rows[3]['actor']
    assert u(n,0xD8)==min(4096,120*steps)
    assert u(n,0xDC)==(0 if steps>=36 else 120)
    assert n[0xF1]==(2 if steps>=35 else 0)
    assert n[0x3AE]==(0 if completed else 1 if steps==35 and c.get('action_tick',True) else 2)
    assert u(rows[1]['actor'],0xDC)==236 and u(rows[2]['actor'],0xDC)==120
    assert u(post[u(post[actor],0x370)],0x74C)==40
    walked=bool(c.get('walk_steps'))
    if walked:
        assert len(rows)==9
        for i,row in enumerate(rows[6:],6):
            n=row['actor'];assert [u(n,o) for o in [0x1A,0x1E,0x22]]==[30644,26617,4096]
            assert u(n,0x25E)==[275,1,89][i-6]
            assert u(n,0x3A9)==(0 if i==8 and c.get('clear_after_exit') else 80)
            hp=915 if i==8 and not c.get('clear_after_exit') else 910
            for key in ['actor','recipient']:assert u(row[key],0x40)==hp
            assert u(n,0x3B9,2)==(300 if i==6 else 0)
            assert u(row['room'],0x53,2)==(101 if i==6 else 0)
            assert list(row['owner'][0x108A:0x109C])==[int(i==6)]+[0]*17
    for row in rows:
        assert row['actor'][1343:]==ch[actor][1343:] and u(row['room'],0xE,2)==102 and u(row['room'],0x55)==0
    def at(addr,n):
        base=next(b for b,v in post.items() if b<=addr and addr+n<=b+len(v));return post[base][addr-base:addr-base+n]
    tile=post[p.f.MAP];present=set()
    for off in range(0,len(tile),16):
        tag=u(tile,off,2);prev=0
        while tag:
            assert tag not in present;present.add(tag);n=at(u(post[0x6EE540],tag*4),42)
            assert u(n,0xA,2)==prev and u(n,0x16)==0 and (u(n,0x1E)>>12)*32+(u(n,0x1A)>>12)==off//16
            prev,tag=tag,u(n,0xC,2)
    assert present=={100,101,102}
    bell=at(u(post[probe.old.old.OUT],0),393);assert bell[0x12C]==142 and u(bell,4,2)==102
    assert u(post[p.f.WORLD],0x2A)==1 and u(post[p.f.WORLD],0x76)==408
    for tag in [100,101,102]:assert post[0x6ED540][tag]==1
    for addr in [p.target.RAW,core.prior.RAWS,core.VARIABLE,probe.old.old.SPELL_RAW,0x66D524,0x66D654,0x66D754,0x66D86C,0x66DB34,0x66E3EC,0x670574,0x6E4144,0x6E4148,0x6C1488]:
        if addr in ch:assert post[addr]==ch[addr],(c['name'],hex(addr))
    for x in a['before_calls']:
        assert x['address'] not in ['0x483790','0x483870','0x4d8e60','0x496700','0x481200','0x4e8da0']
        if x['address']=='0x476d30':assert x['stack_args']==[1,0]
    visit=lambda addr:r['visited_instructions'].get(hex(addr),0)
    assert visit(0x4BC710)==1 and visit(0x46EAB0)==1
    assert visit(0x4B3C10)>=steps and visit(0x488930)==(73 if walked else 0)
    assert visit(0x481200)>=1 and visit(0x496700)==(3 if walked else 2)
    assert len(r['hp_requests'])==(6 if walked and not c.get('clear_after_exit') else 4 if walked else 0)
    simple=[]
    for i,row in enumerate(rows):
        n=row['actor'];simple.append(dict(index=i,state=u(n,0x25E),action=u(n,0x3A9),HP=u(n,0x40),recipientHP=u(row['recipient'],0x40),
            position=[u(n,o) for o in [0x1A,0x1E,0x22]],roomtag=u(n,0x3B9,2),registered=u(row['room'],0x53,2),
            animation_progress=u(n,0xD8),animation_increment=u(n,0xDC),animation_flags=n[0xF1],action_count=n[0x3AE]))
    return post,dict(checkpoints=simple,recovered_before_walk=completed,walked=walked,participant_alive=True,
        lower_page_only_FS0=True,map_lists_consistent=True,whole_Creature_ticks=visit(0x488930),native_steps=r['instruction_count'])

def main():
    rows=[]
    for c in cases():
        a,ch,s,f=execute(c);_,out=verify(c,a,ch,s['result'])
        rows.append(dict(case=c,file=str(f.relative_to(ROOT)),sha256=sha(f.read_bytes()),outcome=out))
        print(c['name'],out['recovered_before_walk'],out['checkpoints'][-1]['state'],out['checkpoints'][-1]['HP'],flush=True)
    (ROOT/'phase185-crypt-drop-recovery-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows),indent=2),encoding='utf-8')

if __name__=='__main__':main()
