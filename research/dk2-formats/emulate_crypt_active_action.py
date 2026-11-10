"""Accepted phase180 original Crypt callback/dispatcher and source102 checks."""
from pathlib import Path
import json
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
import probe_crypt_active_action as probe
from emulate_resistance import post_context
ROOT=Path(__file__).resolve().parent
p,EXE,sha=probe.p,probe.EXE,probe.sha
u=probe.u
canonical=lambda x:json.dumps(x,sort_keys=True)

def cases():
    # Source species cover every actual switch case, including all fifteen
    # common Rat creators and the differing lord/prince/king divisions.
    ids=sorted(r[0xF4B] for s in probe.prior.fmt.parse(probe.SOURCE.read_bytes()) if s.signature==170 for r in s.records)
    rows=[dict(name=f'source-species-{i}',species=i,clock=100,dispatcher=True) for i in ids]
    rows += [dict(name='heal-boundary',species=5,clock=4),dict(name='heal-after-boundary',species=5,clock=5),
        dict(name='heal-same-clock-three',species=5,clock=5,repeat=3,dispatcher=True),
        dict(name='heal-owner-list-empty',species=5,clock=5,listed=False),
        dict(name='anger-disabled',species=13,clock=7,accumulation_enabled=False),
        dict(name='anger-clamp',species=13,clock=7,initial_anger=10),
        dict(name='anger-positive-diagnostic',species=13,clock=7,unit=100),
        dict(name='anger-zero-diagnostic',species=21,clock=9,unit=0),
        dict(name='rat-allocation-failure',species=4,clock=3,spare_blocks=0),
        dict(name='chicken-allocation-failure',species=17,clock=13,spare_blocks=0),
        dict(name='rat-same-clock',species=4,clock=3,repeat=3,dispatcher=True),
        dict(name='gold-same-clock',species=40,clock=9,repeat=3,dispatcher=True),
        dict(name='clock-wrap-diagnostic',species=5,clock=3,last=0xFFFFFFFE)]
    return rows

def execute(c):return probe.execute(c)

def rule(i):
    if i==5:return 'heal',4,5
    if i==34:return 'heal',4,8
    if i in [13,37]:return 'anger',6,5
    if i in [2,26,31]:return 'anger',4,4
    if i==29:return 'anger',6,4
    if i==21:return 'anger',8,2
    if i in [3,15]:return 'quake',12,(3,0x1000)
    if i==41:return 'quake',12,(5,0x3000)
    if i in [18,24]:return 'gold',8,0
    if i==40:return 'gold',8,10
    if i in [17,25,30]:return 'object',12,9
    if i in [4,6,7,9,10,11,12,22,32,33,35,36,38,39,43]:return 'object',2,11
    return 'none',0,None

def verify(c,args,checks,r):
    assert r['success'] and r['hit_return'] and not r['emulation_error'],(c['name'],r['emulation_error'],r['trace_tail'])
    assert not r['protected_write_or_execution_failures'] and r['function_or_vtable_substitutions']==0
    assert len(r['before_calls'])==len(args['before_calls']) and all(x['returned'] for x in r['before_calls'])
    assert int(r['registers']['EAX'],16)==1
    md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;blob=EXE.read_bytes()
    for x in r['observed_reads']+r['observed_writes']:
        if int(x['address'],16)>=4096:continue
        ins=next(md.disasm(p.pe_read(blob,int(x['pc'],16),15),int(x['pc'],16)))
        assert int(x['address'],16)==0 and x['size']==4 and 0x64 in ins.prefix and 'fs:[0]' in ins.op_str,(c['name'],x)
    post=post_context(checks,r)
    assert u(post[probe.prior.readable.COUNTER],4)==0
    participant,recipient,outside=probe.PARTICIPANT,probe.RECIPIENT,probe.UNRELATED
    owner=u(checks[participant],0x2A);other=u(checks[outside],0x2A)
    kind,delay,amount=rule(c['species']);clock=c['clock'];last=c.get('last',0)
    due=kind!='none' and clock>((last+delay)&0xFFFFFFFF)
    assert u(post[participant],0x3A5)==(clock if due else last)
    assert u(post[participant],0x3A9)==80
    assert r['visited_instructions'].get('0x499ca0',0)==c.get('repeat',1)
    assert r['visited_instructions'].get('0x496aa0',0)==int(c.get('dispatcher',False))
    assert post[outside]==checks[outside]
    assert post[owner][0x108A:0x109C]==checks[owner][0x108A:0x109C]==bytes(18)
    assert post[other][0x108A:0x109C]==checks[other][0x108A:0x109C]
    assert post[p.target.RAW]==checks[p.target.RAW] and post[probe.VARIABLE]==checks[probe.VARIABLE]
    unit=c.get('unit',-275);signed=((unit&65535)^32768)-32768
    assert int.from_bytes(post[0x6F28E8],'little',signed=True)==signed
    listed=c.get('listed',True)
    for a in [participant,recipient]:
        hp=c.get('HP',100)+(amount if due and kind=='heal' and listed else 0)
        assert u(post[a],0x40)==hp,(c['name'],hex(a),u(post[a],0x40),hp)
        assert post[0x6ED540][u(post[a],4,2)]==1
        expected=c.get('initial_anger',1000)
        definition=u(post[a],0x370)
        if due and kind=='anger' and listed and c.get('accumulation_enabled',True) and not(post[definition][0x68A]&4):
            delta=(abs(signed)//amount)*(-1 if signed<0 else 1)
            expected=max(0,min(65533,expected+delta))
        actual=[u(post[a],0x26E+4*i) for i in range(6)]
        assert actual==[expected]*6,(c['name'],hex(a),actual,expected)
    requests=r['hp_requests']
    assert len(requests)==(2 if kind=='heal' and due and listed else 0)
    if requests:assert [(x['target'],x['delta'],x['owner_u16']) for x in requests]==[(participant,amount,8),(recipient,amount,8)]
    # First original world RNG operation in this callback; the genuine native
    # LCG rotates the updated seed right13, then applies unsigned remainder.
    seed=(1664525*1+1013904223)&0xFFFFFFFF
    rnd=((seed>>13)|(seed<<19))&0xFFFFFFFF
    money=(rnd%10+amount) if due and kind=='gold' else 0
    assert u(post[owner],0x7E)==money and u(post[other],0x7E)==u(checks[other],0x7E)
    assert u(post[probe.UILOG],0)==int(due and kind=='gold')
    objects=[]
    def at(a,n):
        base=max(b for b,v in post.items() if b<=a and a+n<=b+len(v));return post[base][a-base:a-base+n]
    for tag in range(103,u(post[0x6A4818],4)):
        address=u(post[0x6EE540],4*tag)
        if not address:continue
        b=at(address,393);assert u(b,0x12)==2
        objects.append(dict(tag=tag,address=address,ID=b[0x12C],state=u(b,0x13A),HP=u(b,0x40),position=[u(b,0x1A+4*i) for i in range(3)]))
    created=bool(kind=='object' and due and c.get('spare_blocks',4)>0)
    assert len(objects)==int(created) and u(post[p.f.WORLD],0x2A)==int(created)
    if created:
        assert objects[0]['ID']==amount
        assert objects[0]['position'][:2]==[(u(checks[participant],0x1A)>>12)*4096-2048,(u(checks[participant],0x1E)>>12)*4096+2048]
        if amount==9:assert objects[0]['state']==10
    world=post[p.f.WORLD]
    shake=world[0x9BB0:0x9C50]
    if due and kind=='quake':
        assert [u(shake,o) for o in [0,4,8]]==[(u(checks[participant],0x1A)>>12)*4096-2048,(u(checks[participant],0x1E)>>12)*4096+2048,clock]
        assert u(shake,12,2)==20*amount[0] and u(shake,14,2)==amount[1]
    else:assert shake==checks[p.f.WORLD][0x9BB0:0x9C50]
    for a,n in [(probe.prior.DEFS,5*285),(probe.prior.RAWS,5*894),(probe.prior.MID,1022)]:assert post[a][n:]==checks[a][n:]
    for a,n in [(probe.KEEPER_RAW,406),(probe.KEEPER_MID,526),(probe.KEEPER_DEF,122)]:assert post[a][n:]==checks[a][n:]
    for a in [0x66D524,0x66D654,0x66D754,0x66D86C,0x66E3EC,0x670574]:assert post[a]==checks[a]
    return post,dict(kind=kind,threshold=delay,due=due,last=u(post[participant],0x3A5),action=80,
        HP=[u(post[a],0x40) for a in [participant,recipient,outside]],anger=[[u(post[a],0x26E+4*i) for i in range(6)] for a in [participant,recipient,outside]],
        money=money,objects=objects,quake=shake.hex() if kind=='quake' else None,all_18_owner_counters_remain_zero=True,
        unrelated_owner_Creature_unchanged=True,native_steps=r['instruction_count'])

def main():
    rows=[]
    for c in cases():
        args,ch,s,file=execute(c);_,out=verify(c,args,ch,s['result'])
        rows.append(dict(case=c,file=str(file.relative_to(ROOT)),sha256=sha(file.read_bytes()),outcome=out))
        print(c['name'],out['kind'],out['due'],out['HP'],out['money'],len(out['objects']),flush=True)
    (ROOT/'phase180-crypt-action-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows),indent=2),encoding='utf-8')

if __name__=='__main__':main()
