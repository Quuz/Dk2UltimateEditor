"""RawCreatureCF2 -> whole48B120 level-up -> actualWorkModifier andHP.

Prepared Actor fromverifiednativefactorysnapshot, originalWorld/Actorvtables,
fullrawconstructor/converter/loader. Level-upvisual request explicitlydeclined;
soundlookup0/playtoken aredeclaredexternalproviders. No realAI scheduler.
"""
from pathlib import Path
import json,struct
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
import probe_creature_work_level as p
from emulate_resistance import post_context
ROOT=Path(__file__).resolve().parent
sha=p.sha
def u(b,o,n=4):return int.from_bytes(b[o:o+n],'little')
def cases():
    return [dict(name='source-zero',increment=0),dict(name='quarter',increment=1024),dict(name='one',increment=4096),
        dict(name='minus-one-bitpattern',increment=0xFFFFFFFF),dict(name='highbit',increment=0x80000000),
        dict(name='wrap',increment=1,modifier=0xFFFFFFFF),dict(name='level9',level=9,increment=1024),dict(name='level10',level=10,increment=1024),
        dict(name='default-only',increment=1024,default=0x89ABCDEF),dict(name='negative-result',increment=0xFFFFE000),
        dict(name='dead-gate',increment=1024,dead=True)]
def execute(c):return p.execute(c)

def verify(c,args,ch,r):
    assert r['success'] and r['hit_return'] and not r['protected_write_or_execution_failures'],(c,r['emulation_error'],r['trace_tail'])
    assert len(r['before_calls'])==4 and all(x['returned'] for x in r['before_calls'])
    post=post_context(ch,r);raw=ch[p.RAW][:5449];mid=post[p.MID][:6457];d=post[p.cf.DEFINITION][:2199]
    assert post[p.RAW]==ch[p.RAW] and post[p.MID][6457:]==ch[p.MID][6457:] and post[p.cf.DEFINITION][2199:]==ch[p.cf.DEFINITION][2199:]
    assert u(mid,0x10DE)==u(raw,0xCEE) and u(mid,0x10E2)==u(raw,0xCF2)
    assert u(d,0x793)==u(raw,0xCEE) and u(d,0x797)==u(raw,0xCF2) and u(d,0x5AF)==sum(mid)
    level=c.get('level',1);active=level!=10 and not c.get('dead');increment=c.get('increment',1024)
    desired=(c.get('modifier',4096)+(increment if active else 0))&0xFFFFFFFF
    actor=bytearray(ch[p.cf.ACTOR]);actor[0x403]=level+int(active);p.f.put(actor,0x3AF,desired)
    if active:
        p.f.put(actor,0x40,1000*(100+50*level)//100);p.f.put(actor,0x38,100);p.f.put(actor,0x3C,100);p.f.put(actor,0x2E,0x13572468)
    assert post[p.cf.ACTOR]==actor,(c['name'],[(hex(i),a,b) for i,(a,b) in enumerate(zip(post[p.cf.ACTOR],actor)) if a!=b][:20])
    assert int(r['registers']['EAX'],16)==level+int(active)
    out=bytearray(ch[p.f.OUT]);p.f.put(out,8,int(active));assert post[p.f.OUT]==out
    assert post[p.FXIO]==struct.pack('<I',int(active)) and post[p.IO]==struct.pack('<I',2)
    md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;blob=p.EXE.read_bytes()
    for row in r['observed_reads']+r['observed_writes']:
        if int(row['address'],16)>=4096:continue
        pc=int(row['pc'],16);assert 0x400000<=pc<0x670000,row
        ins=next(md.disasm(p.pe_read(blob,pc,15),pc));assert int(row['address'],16)==0 and row['size']==4 and 0x64 in ins.prefix and 'fs:[0]' in ins.op_str,row
    direct=[]
    for row in r['observed_reads']:
        if int(row['address'],16)==p.cf.DEFINITION+0x797:direct.append(row)
    assert len(direct)==int(active) and all(int(row['pc'],16)==0x48B335 and row['size']==4 for row in direct)
    assert r['visited_instructions'].get('0x48b330',0)==int(active) and r['visited_instructions'].get('0x48b120',0)==1
    allowed={p.cf.ACTOR,p.cf.DEFINITION,p.MID,p.f.OUT,p.IO,p.FXIO,0x740C20}
    for a in post:
        if a not in allowed:assert post[a]==ch[a],(c['name'],hex(a))
    return post,dict(level_before=level,level_after=level+int(active),active=active,WorkModifier_before=c.get('modifier',4096),
        raw_increment_bits=increment,definition_increment_bits=u(d,0x797),WorkModifier_after=desired,
        default_raw_loaded=u(d,0x793),HP_after=u(actor,0x40),helper_definition_reads=len(direct),visual_requests=int(active),
        compared_regions=len(post),compared_bytes=sum(map(len,post.values())),native_steps=r['instruction_count'])

def compare_default_pair(a,b):
    assert a.keys()==b.keys();allowed={p.RAW:[(0xCEE,4)],p.MID:[(0x10DE,4)],p.cf.DEFINITION:[(0x793,4),(0x5AF,4)]};diffs=[]
    for address,left in a.items():
        right=b[address];assert len(left)==len(right);indices=[i for i,(x,y) in enumerate(zip(left,right)) if x!=y]
        assert all(any(off<=i<off+n for off,n in allowed.get(address,[])) for i in indices),(hex(address),indices[:20])
        if indices:diffs.append(dict(region=hex(address),changed_byte_offsets=indices))
    assert {int(x['region'],16) for x in diffs}==set(allowed)
    return dict(cases=['quarter','default-only'],compared_regions=len(a),compared_bytes=sum(map(len,a.values())),actual_differences=diffs,
        level_up_result_identical=True,global_nonuse_proved=False,scope='rawCEEloadpreservedbutnotreadinselectedlevel-up; noinitialdefaultconsumerproved.')

def main():
    rows=[];posts={}
    for c in cases():
        args,ch,d,path=execute(c);post,out=verify(c,args,ch,d['result']);posts[c['name']]=post
        rows.append(dict(case=c,outcome=out,file=str(path.relative_to(ROOT)),sha256=sha(path.read_bytes())));print(c['name'],out['level_after'],hex(out['WorkModifier_after']),out['HP_after'],'verified',flush=True)
    pair=compare_default_pair(posts['quarter'],posts['default-only'])
    (ROOT/'phase162-creature-work-level-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows,paired_controls=[pair]),indent=2),encoding='utf8')
if __name__=='__main__':main()
