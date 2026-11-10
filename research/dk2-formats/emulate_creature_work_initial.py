"""ActualrawCEE -> nativeCreatureconstructor/factory/initializer; hardcodedinitialWorkModifier.

Initial ActorWorkModifier is not prepared. WholememoryCEE-only pairs preserve
all other outputs; no globalnonuse/normalWorldbootstrap proof.
"""
from pathlib import Path
import json,struct
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
import probe_creature_work_initial as p
from emulate_resistance import post_context
ROOT=Path(__file__).resolve().parent
sha=p.sha
def u(b,o,n=4):return int.from_bytes(b[o:o+n],'little')

def cases():
    return [dict(name='base'),dict(name='default-one',default=1),dict(name='default4096',default=4096),
        dict(name='default8192',default=8192),dict(name='default-all-bits',default=0xFFFFFFFF),
        dict(name='increment1024',increment=1024),dict(name='Imp',species=1),dict(name='Imp-default8192',species=1,default=8192)]

def execute(c):return p.execute(c)

def verify(c,args,ch,r):
    assert r['success'] and r['hit_return'] and not r['protected_write_or_execution_failures'],(c,r['emulation_error'],r['trace_tail'])
    assert len(r['before_calls'])==8 and all(x['returned'] for x in r['before_calls'])
    assert int(r['registers']['EAX'],16)==1
    post=post_context(ch,r);raw=ch[p.RAW][:5449];mid=post[p.MID][:6457];d=post[p.cf.DEFINITION][:2199];actor=post[p.cf.ACTOR]
    p.cf.verify(dict(p.cf.cases()[0],ident=c.get('species',8)),actor[:1343],dict(health=1000,position=p.cf.cases()[0]['position']))
    assert post[p.RAW]==ch[p.RAW] and post[p.MID][6457:]==ch[p.MID][6457:] and post[p.cf.DEFINITION][2199:]==ch[p.cf.DEFINITION][2199:]
    assert actor[1343:]==ch[p.cf.ACTOR][1343:]
    assert post[p.KRAW]==ch[p.KRAW] and post[p.KMID][526:]==ch[p.KMID][526:] and post[p.KDEF][122:]==ch[p.KDEF][122:]
    assert post[p.KDEF][0x75]==4
    assert u(mid,0x10DE)==u(d,0x793)==u(raw,0xCEE)==c.get('default',0)
    assert u(mid,0x10E2)==u(d,0x797)==u(raw,0xCF2)==c.get('increment',0) and u(d,0x5AF)==sum(mid)
    assert u(actor,0x3AF)==0xCCC and u(actor,0x3B3)==0
    writes=[x for x in r['observed_writes'] if int(x['address'],16)<p.cf.ACTOR+0x3B7 and int(x['address'],16)+x['size']>p.cf.ACTOR+0x3AF]
    assert writes==[dict(pc='0x48746c',address=hex(p.cf.ACTOR+0x3AF),size=4,value=0xCCC),dict(pc='0x487476',address=hex(p.cf.ACTOR+0x3B3),size=4,value=0)],writes
    assert r['visited_instructions'].get('0x4872a0',0)==r['visited_instructions'].get('0x4b78d0',0)==r['visited_instructions'].get('0x4b5e10',0)==1
    assert post[p.cf.probe.COUNTER][4:]==bytes(4)
    md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;blob=p.EXE.read_bytes()
    for row in r['observed_reads']+r['observed_writes']:
        if int(row['address'],16)>=4096:continue
        pc=int(row['pc'],16);assert 0x400000<=pc<0x670000,row
        ins=next(md.disasm(p.pe_read(blob,pc,15),pc));assert int(row['address'],16)==0 and row['size']==4 and 0x64 in ins.prefix and 'fs:[0]' in ins.op_str,row
    return post,dict(species=c.get('species',8),raw_CEE=c.get('default',0),raw_CF2=c.get('increment',0),Actor_WorkModifier=u(actor,0x3AF),
        Actor_WorkEnhancement=u(actor,0x3B3),initial_Q12_factor=u(actor,0x3AF)/4096,HP=u(actor,0x40),level=actor[0x403],
        exact_WorkModifier_writer=0x48746C,exact_WorkEnhancement_writer=0x487476,compared_regions=len(post),compared_bytes=sum(map(len,post.values())),native_steps=r['instruction_count'])

def compare_pair(left,right,names,*,increment=False):
    off,mid,definition=(0xCF2,0x10E2,0x797) if increment else (0xCEE,0x10DE,0x793)
    allowed={p.RAW:[(off,4)],p.MID:[(mid,4)],p.cf.DEFINITION:[(definition,4),(0x5AF,4)]};diffs=[]
    assert left.keys()==right.keys()
    for a,b in left.items():
        c=right[a];assert len(b)==len(c);indices=[i for i,(x,y) in enumerate(zip(b,c)) if x!=y]
        assert all(any(o<=i<o+n for o,n in allowed.get(a,[])) for i in indices),(names,hex(a),indices[:20])
        if indices:diffs.append(dict(region=hex(a),changed_byte_offsets=indices))
    assert {int(x['region'],16) for x in diffs}==set(allowed)
    return dict(cases=names,compared_regions=len(left),compared_bytes=sum(map(len,left.values())),actual_differences=diffs,
        all_Actor_and_other_outputs_identical=True,global_nonuse_proved=False,scope='Selectedfullnativecreation; onlyraw/MID/definitionstorageanddefinitionchecksumdiffer.')

def pairs(posts):
    return [compare_pair(posts['base'],posts[name],['base',name]) for name in ['default-one','default4096','default8192','default-all-bits']]+[
        compare_pair(posts['base'],posts['increment1024'],['base','increment1024'],increment=True),compare_pair(posts['Imp'],posts['Imp-default8192'],['Imp','Imp-default8192'])]

def main():
    rows=[];posts={}
    for c in cases():
        args,ch,s,path=execute(c);post,out=verify(c,args,ch,s['result']);posts[c['name']]=post
        rows.append(dict(case=c,outcome=out,file=str(path.relative_to(ROOT)),sha256=sha(path.read_bytes())))
        print(c['name'],'initialWorkModifier',out['Actor_WorkModifier'],'verified',flush=True)
    (ROOT/'phase164-creature-work-initial-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows,paired_controls=pairs(posts)),indent=2),encoding='utf8')

if __name__=='__main__':main()
