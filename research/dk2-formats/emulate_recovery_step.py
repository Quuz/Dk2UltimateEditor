"""Phase184 accepted raw Creature recovery displacement controls."""
from pathlib import Path
import json
import probe_recovery_step as probe
import check_recovery_step_discovery as discovery
from emulate_resistance import post_context
ROOT=Path(__file__).resolve().parent
p,EXE,sha,u=probe.p,probe.EXE,probe.sha,probe.u
NAMES=['source-west','zero-distance','negative-distance','source-north','source-east','source-south','one-cell-distance','other-components']

def cases():return [dict(c,name=n) for n,c in zip(NAMES,discovery.CASES)]

def execute(c):
    a,ch,s,f,_=probe.execute({k:v for k,v in c.items() if k!='name'})
    return a,ch,s,f

def verify(c,args,ch,r):
    plain={k:v for k,v in c.items() if k!='name'};ofs=probe.build(plain)[2]
    out=discovery.verify(plain,args,ch,r,ofs);post=post_context(ch,r)
    actor=probe.old.ACTOR;owner=u(ch[actor],0x2A);unrelated=probe.old.old.old.UNRELATED;other=u(ch[unrelated],0x2A)
    assert int(r['registers']['EAX'],16)==owner
    assert post[other][0x108A:0x109C]==ch[other][0x108A:0x109C]
    assert u(post[probe.old.old.old.prior.readable.COUNTER],4)==0
    assert post[probe.old.ENTRY_RESULT]==bytes(4)+ch[probe.old.ENTRY_RESULT][4:]
    def at(a,n):
        base=next(b for b,v in post.items() if b<=a and a+n<=b+len(v));return post[base][a-base:a-base+n]
    bell=u(post[probe.old.old.OUT],0);b=at(bell,393)
    assert u(b,4,2)==102 and b[0x12C]==142
    assert post[0x6ED540][100]==post[0x6ED540][101]==post[0x6ED540][102]==1
    present=set();tile=post[p.f.MAP]
    for off in range(0,len(tile),16):
        tag=u(tile,off,2);prev=0
        while tag:
            assert tag not in present;present.add(tag)
            node=at(u(post[0x6EE540],tag*4),42)
            assert u(node,0xA,2)==prev and u(node,0x16)==0
            assert (u(node,0x1E)>>12)*32+(u(node,0x1A)>>12)==off//16
            prev,tag=tag,u(node,0xC,2)
    assert present=={100,101,102}
    room=post[probe.old.ROOM]
    assert u(room,0x53,2)==0 and u(room,0xA,2)==101 and u(room,0xE,2)==102 and u(room,0x55)==0
    assert u(post[actor],0x3B9,2)==300
    assert u(post[p.f.WORLD],0x2A)==1 and u(post[p.f.WORLD],0x76)==408
    out.update(map_lists_consistent=True,source_scenes_have_zero_Crypt_registration=True)
    return post,out

def main():
    rows=[]
    for c in cases():
        a,ch,s,f=execute(c);_,out=verify(c,a,ch,s['result'])
        rows.append(dict(case=c,file=str(f.relative_to(ROOT)),sha256=sha(f.read_bytes()),outcome=out))
        print(c['name'],out['final_position'],flush=True)
    (ROOT/'phase184-recovery-step-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows),indent=2),encoding='utf-8')

if __name__=='__main__':main()
