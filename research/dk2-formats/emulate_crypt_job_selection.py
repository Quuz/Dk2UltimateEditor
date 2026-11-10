"""Strict phase188 raw-job selectors and unchanged-source Firefly controls."""
from pathlib import Path
import json
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
import probe_crypt_job_selection as probe
import probe_crypt_source_job as source
from emulate_resistance import post_context
ROOT=Path(__file__).resolve().parent
p,EXE,sha,u=probe.p,probe.EXE,probe.sha,probe.u

def cases():
    return probe.cases()+[dict(name=f'source-{method}-seed{seed}',source=True,method=method,seed=seed)
        for method,seed in [('happy',0),('happy',2),('happy',17),('AI',17)]]

def execute(c):
    a,ch,s,f,_=source.execute(c['method'],c['seed']) if c.get('source') else probe.execute(c)
    return a,ch,s,f

def snapshots(c,post):
    offsets=source.build(c['method'],c['seed'])[2] if c.get('source') else probe.build(c)[2]
    rows=[]
    for i in range(6):
        b=post[probe.old.LOG][i*probe.old.STRIDE:(i+1)*probe.old.STRIDE];assert u(b,0)==i
        end=max(o+n for o,n,a in offsets.values());assert b[end:]==bytes([165])*(probe.old.STRIDE-end)
        rows.append({k:b[o:o+n] for k,(o,n,a) in offsets.items()})
    return rows

def verify(c,a,ch,r):
    assert r['success'] and r['hit_return'] and not r['emulation_error'],(c['name'],r['emulation_error'])
    assert not r['protected_write_or_execution_failures'] and r['function_or_vtable_substitutions']==0
    assert len(r['before_calls'])==len(a['before_calls']) and all(x['returned'] for x in r['before_calls'])
    md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;blob=EXE.read_bytes()
    for x in r['observed_reads']+r['observed_writes']:
        if int(x['address'],16)>=4096:continue
        ins=next(md.disasm(p.pe_read(blob,int(x['pc'],16),15),int(x['pc'],16)))
        assert int(x['address'],16)==0 and x['size']==4 and 0x64 in ins.prefix and 'fs:[0]' in ins.op_str
    post=post_context(ch,r);rows=snapshots(c,post);actor=probe.old.ACTOR
    owner=u(ch[actor],0x2A);foreign=u(ch[probe.old.old.UNRELATED],0x2A)
    assert int(r['registers']['EAX'],16)==owner and u(post[probe.old.old.prior.readable.COUNTER],4)==0
    assert post[probe.old.old.UNRELATED]==ch[probe.old.old.UNRELATED]
    assert post[foreign][0x108A:0x109C]==ch[foreign][0x108A:0x109C]
    source_case=c.get('source',False);crypt=c['seed']==17 if source_case else c.get('continue_ai',True)
    delayed=source_case and c['method']=='AI';rat=source_case and crypt
    wanted=[89,274,275,275,1,1] if delayed else [274,6,275,275,1,1] if crypt else [6,6,6,6,1,1] if source_case else [1]*6
    simple=[]
    for i,row in enumerate(rows):
        cr,room,ow=row['actor'],row['room'],row['owner'];registered=crypt and ((i in [2,3]) if delayed else (i in [1,2,3]))
        assert u(cr,0x25E)==wanted[i] and u(cr,0x3A9)==(80 if crypt and i>=2 else 0),(c['name'],i)
        assert u(room,0x53,2)==(101 if registered else 0)
        counters=[0]*18
        if registered:counters[17 if source_case else 0]=1
        assert list(ow[0x108A:0x109C])==counters
        hp=905 if crypt and not source_case and i>=3 else 900
        for key in ['actor','recipient']:assert u(row[key],0x40)==hp and u(row[key],0x12)==0
        job=0 if delayed and i==0 else 28 if crypt else 24 if source_case else 0
        assert u(cr,0x24E+0x64)==job
        member=crypt and i<4 and not (delayed and i==0)
        assert u(cr,0x3B9,2)==(300 if member else 0) and u(room,0xA,2)==(101 if member else 0)
        assert u(room,0x55)==0 and u(room,0xE,2)==102
        assert cr[1343:]==ch[actor][1343:]
        simple.append(dict(checkpoint=i,state=u(cr,0x25E),action=u(cr,0x3A9),HP=hp,selected_job=job,
            registered_tag=u(room,0x53,2),counters=counters))
    assert len(r['hp_requests'])==(2 if crypt and not source_case else 0)
    assert post[p.target.RAW]==ch[p.target.RAW]
    definition=post[u(post[actor],0x370)]
    if source_case:
        original=next(r for s in probe.old.old.prior.fmt.parse(source.SOURCE.read_bytes()) if s.signature==170 for r in s.records if r[0xF4B]==32)
        assert ch[p.target.RAW]==original and definition[0x78B]==3
        assert [(definition[0x628+6*i+4],definition[0x628+6*i+5]) for i in range(3)]==[(55,28),(45,24),(0,0)]
    else:
        assert definition[0x78C]==2
        if 'raw_job' in c:assert definition[0x641]==(c['raw_job']&255) and definition[0x640]==c.get('weight',100)
        assert u(post[probe.RESULT],0)==(0 if c['method']=='AI' else int(crypt))
        assert post[probe.RESULT][4:]==ch[probe.RESULT][4:]
    def at(a,n):
        b=next(b for b,v in post.items() if b<=a and a+n<=b+len(v));return post[b][a-b:a-b+n]
    seen=set()
    for off in range(0,len(post[p.f.MAP]),16):
        tag=u(post[p.f.MAP],off,2);previous=0
        while tag:
            assert tag not in seen;seen.add(tag);node=at(u(post[0x6EE540],tag*4),42)
            assert u(node,0xA,2)==previous and ((u(node,0x1E)>>12)*32+(u(node,0x1A)>>12))*16==off
            previous,tag=tag,u(node,0xC,2)
    assert seen==({100,101,102,103} if rat else {100,101,102})
    if rat:assert at(u(post[0x6EE540],103*4),393)[0x12C]==11
    assert u(post[p.f.WORLD],0x2A)==1+int(rat) and u(post[p.f.WORLD],0x76)==408*(1+int(rat))
    for tag in [100,101,102]:assert post[0x6ED540][tag]==1
    for address in [probe.old.old.prior.RAWS,probe.old.old.VARIABLE,0x66D524,0x66D654,0x66D754,0x66D86C,0x66DB34,0x66E3EC,0x670574]:
        if address in ch:assert post[address]==ch[address]
    # Initial274/registration/activation is never explicitly called by host.
    assert not {0x483AC0,0x483790,0x483870,0x4D8E60,0x496700}&{int(x['address'],16) for x in a['before_calls']}
    assert not any(x['address']=='0x476d30' and x['stack_args']==[274,0] for x in a['before_calls'])
    visits=lambda pc:r['visited_instructions'].get(hex(pc),0)
    assert visits(0x483AC0)==int(crypt) and visits(0x494480)==0
    return post,dict(checkpoints=simple,selected_Crypt=bool(crypt),unchanged_source_record=bool(source_case),both_Creatures_alive=True,
        lower_page_only_FS0=True,lists_consistent=True,raw_and_vtables_preserved=True,host_requests_Crypt_state=0,
        source_Rat_created=bool(rat),native_steps=r['instruction_count'],job28_dispatches=visits(0x483AC0))

def main():
    rows=[]
    for c in cases():
        a,ch,s,f=execute(c);_,out=verify(c,a,ch,s['result'])
        rows.append(dict(case=c,file=str(f.relative_to(ROOT)),sha256=sha(f.read_bytes()),outcome=out))
        print(c['name'],out['selected_Crypt'],out['checkpoints'][2]['action'],flush=True)
    (ROOT/'phase188-crypt-job-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows),indent=2),encoding='utf-8')

if __name__=='__main__':main()
