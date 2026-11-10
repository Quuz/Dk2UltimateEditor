"""RawCreatureCF2/level-up and rawVariables -> wholeworkcallback -> actualRockHP.

PreparedActor/World/CMap and rawRockdefinition; nativevariablehandlers/getters,
Q12arithmetic and complete452F60 execute. Nonlethalpreparedcases, noAIcaller/GPU.
"""
from pathlib import Path
import json,struct
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
import probe_creature_work_cell as p
from emulate_resistance import post_context
ROOT=Path(__file__).resolve().parent
sha=p.sha
def u(b,o,n=4):return int.from_bytes(b[o:o+n],'little')
def s32(n):n&=0xFFFFFFFF;return n-(1<<32) if n&0x80000000 else n
def tz(n,d):return (abs(n)//abs(d))*(-1 if (n<0)!=(d<0) else 1)
def cases():
    return [dict(name='base',increment=0),dict(name='quarter',increment=1024),dict(name='double',increment=4096),
        dict(name='no-level-up',increment=4096,level_up=False),dict(name='enhancement205',increment=0,enhancement=205),
        dict(name='both',increment=1024,enhancement=205),dict(name='zero-modifier',increment=0,modifier=0),
        dict(name='negative-modifier',increment=0,modifier=0xFFFFF000),dict(name='neutral',increment=1024,owner=8),
        dict(name='enemy',increment=1024,owner=9),dict(name='non-Imp',increment=1024,species=8),
        dict(name='level10',increment=1024,level=10),dict(name='action-minus100',increment=1024,action=0xFFFFFF9C)]
def execute(c):return p.execute(c)

def reference(c,ch):
    increment=c.get('increment',1024);level=c.get('level',1);eligible=c.get('level_up',True) and level!=10
    modifier=(c.get('modifier',4096)+(increment if eligible else 0))&0xFFFFFFFF;enhancement=c.get('enhancement',0)
    factor=s32(modifier+enhancement);species=c.get('species',1);owner=c.get('owner',7)
    typ=9 if species!=1 or owner==7 else 8 if owner==8 else 10
    index=[8,9,10].index(typ);raw=ch[p.VRAW+4096*index][16:32];action=tz(s32(u(raw,4)),20)
    # Non-Imp actualbranch multiplies DIG_OWN_WALL baseby6 beforeQ12.
    base=action*(6 if species!=1 else 1);amount=s32(((base<<12)*factor)>>12)>>12
    before=c.get('HP',600);after=before-((-amount)&65535) if amount<0 else min(32000,before+(amount&65535))
    assert 0<after<660,(c['name'],after)
    return dict(modifier=modifier,enhancement=enhancement,factor=factor,action_type=typ,action_per_tick=action,base=base,delta=amount,HP_before=before,HP_after=after,level_after=level+int(eligible))

def verify(c,args,ch,r):
    assert r['success'] and r['hit_return'] and not r['protected_write_or_execution_failures'],(c['name'],r['emulation_error'],r['trace_tail'])
    assert len(r['before_calls'])==10+int(c.get('level_up',True)) and all(x['returned'] for x in r['before_calls'])
    post=post_context(ch,r);e=reference(c,ch);cell=(6*32+6)*16;map_=bytearray(ch[p.f.MAP]);p.f.put(map_,cell+6,e['HP_after'],2)
    for index,address in enumerate([0x6F2564,0x6F2584,0x6F2588]):
        base=next(a for a,b in post.items() if a<=address and address+4<=a+len(b))
        raw=ch[p.VRAW+4096*index][16:32]
        assert s32(u(post[base],address-base))==tz(s32(u(raw,4)),20)
    assert post[p.f.MAP]==map_ and post[p.f.MAP][cell+10]==2
    req=r['terrain_requests'];assert len(req)==1
    assert {k:req[0][k] for k in ['x','y','delta','HP_before','terrain_id','caller_return']}==dict(x=6,y=6,delta=e['delta'],HP_before=e['HP_before'],terrain_id=2,caller_return='0x4508f9')
    assert req[0]['owner']&65535==7
    levelup=c.get('level_up',True) and c.get('level',1)!=10
    assert len(r['hp_requests'])==int(levelup)
    if levelup:assert r['hp_requests'][0]['target']==p.cf.ACTOR and r['hp_requests'][0]['delta']==500 and r['hp_requests'][0]['caller_return']=='0x48b294'
    actor=post[p.cf.ACTOR];assert u(actor,0x3AF)==e['modifier'] and u(actor,0x3B3)==e['enhancement'] and actor[0x403]==e['level_after']
    assert r['visited_instructions'].get('0x496e00',0)==r['visited_instructions'].get('0x452f60',0)==1 and not r['visited_instructions'].get('0x44fc40',0)
    assert int(r['registers']['EAX'],16)&255==0
    assert post[p.old.RAW]==ch[p.old.RAW] and post[p.TRAW]==ch[p.TRAW]
    for i in range(3):assert post[p.VRAW+4096*i]==ch[p.VRAW+4096*i]
    for a in [p.old.MID,p.TMID,p.cf.DEFINITION]:
        n={p.old.MID:6457,p.TMID:736,p.cf.DEFINITION:2199}[a];assert post[a][n:]==ch[a][n:]
    assert post[p.TABLE][-460:]==ch[p.TABLE][-460:]
    md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;blob=p.EXE.read_bytes()
    for x in r['observed_reads']+r['observed_writes']:
        if int(x['address'],16)>=4096:continue
        pc=int(x['pc'],16);assert 0x400000<=pc<0x670000,x
        ins=next(md.disasm(p.pe_read(blob,pc,15),pc));assert int(x['address'],16)==0 and x['size']==4 and 0x64 in ins.prefix and 'fs:[0]' in ins.op_str,x
    return post,dict(**e,source_species=c.get('species',1),target_owner=c.get('owner',7),Terrain_calls=1,returned_AL=0,
        request_owner_low16=req[0]['owner']&65535,request_owner_raw_u32=req[0]['owner'],compared_regions=len(post),compared_bytes=sum(map(len,post.values())),native_steps=r['instruction_count'])

def main():
    rows=[]
    for c in cases():
        args,ch,d,path=execute(c);post,out=verify(c,args,ch,d['result'])
        rows.append(dict(case=c,outcome=out,file=str(path.relative_to(ROOT)),sha256=sha(path.read_bytes())))
        print(c['name'],'delta',out['delta'],'HP',out['HP_after'],'verified',flush=True)
    (ROOT/'phase163-creature-work-cell-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows),indent=2),encoding='utf8')
if __name__=='__main__':main()
