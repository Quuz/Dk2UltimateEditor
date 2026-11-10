"""Raw research/manufacture rates -> loader quantization -> player contribution.

Real Crypt Wizard registration and full Creature update precede selected work
callbacks; queues are empty and clock101 avoids periodic experience branch.
No claim about autonomous work,animation frequency or completion rewards.
"""
import hashlib
import json
from pathlib import Path
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
import probe_work_points as probe
from emulate_resistance import post_context
from build_effect_semantics import EXE,pe_read

ROOT=Path(__file__).resolve().parent


def digest(b):return hashlib.sha256(b).hexdigest()


def cases():
    base=dict(base=100,level=1,coefficient=125,wizard=0,tick=101,initial=10,tps=20)
    changes=[('raw0',dict(base=0)),('raw1',dict(base=1)),('raw19',dict(base=19)),
        ('raw39',dict(base=39)),('raw40',dict(base=40)),('raw100',{}),('TPS25',dict(tps=25)),
        ('wizard-control',dict(base=20000)),('wizard-one',dict(base=20000,wizard=1)),
        ('wizard-two',dict(base=20000,wizard=2)),('level2',dict(level=2)),
        ('level2-negative',dict(level=2,coefficient=0xFFFF)),
        ('level2-cap',dict(base=0xFFFF,level=2,coefficient=0x7FFF)),
        ('counter-wrap',dict(base=40,initial=0xFFFFFFFF))]
    return [dict(base,**kw,mode=mode,name=mode+'-'+n) for mode in ['manufacture','research'] for n,kw in changes]


def inputs(c):return {k:v for k,v in c.items() if k!='name'}


def outcome(c):
    loaded=max(1,c['base']//c['tps']) if c['base'] else 0
    coeff=c['coefficient']&0xFFFF;coeff=coeff-65536 if coeff&0x8000 else coeff
    product=loaded*coeff;scaled=(abs(product)//100)*(1 if product>=0 else -1)
    points=loaded if c['level']==1 else min(65534,max(0,scaled))
    mask=0xFFFF if c['mode']=='manufacture' else 0xFFFFFFFF
    return dict(loaded_rate=loaded,contribution=points,progress=(c['initial']+points)&mask,
                work_enhancement=205*c['wizard'],callback_return=int(points!=0) if c['mode']=='manufacture' else 1,
                player_calls=int(points!=0) if c['mode']=='manufacture' else 1)


def path_for(args):
    deps=''.join(digest((ROOT/n).read_bytes()) for n in ['probe_work_points.py','native_unicorn_factorytrace.py'])
    tag=digest((json.dumps(args,sort_keys=True)+deps).encode())[:12]
    return ROOT/'ghidra'/f'phase116-work-points-{tag}.json'


def verify(c,args,checks,r):
    e=outcome(c);post=post_context(checks,r)
    assert r['success'] and r['hit_return'] and not r['protected_write_or_execution_failures']
    assert len(r['before_calls'])==6+int(bool(c['wizard'])) and all(x['returned'] for x in r['before_calls'])
    assert int(r['registers']['EAX'],16)==e['callback_return']
    definition=post[probe.target.attack.TARGET_DEF]
    for offset in [0x6BB,0x6BD]:assert int.from_bytes(definition[offset:offset+2],'little')==e['loaded_rate']
    actor=post[probe.target.attack.TARGET]
    assert int.from_bytes(actor[0x3B3:0x3B7],'little')==e['work_enhancement']
    assert int.from_bytes(actor[0x40:0x44],'little')==1000
    owner=post[probe.OWNER];assert owner.hex()==r['captured_hex']
    offset,width=(0x217,2) if c['mode']=='manufacture' else (0x20F,4)
    assert int.from_bytes(owner[offset:offset+width],'little')==e['progress'],(c['name'],e)
    other,ow=(0x20F,4) if c['mode']=='manufacture' else (0x217,2)
    assert owner[other:other+ow]==checks[probe.OWNER][other:other+ow]
    assert not owner[0x219+0x803] and int.from_bytes(owner[0x213:0x217],'little')==0
    v=r['visited_instructions']
    for a in [0x491DC0,0x568820,0x568E30,0x492320,0x51DC40,0x488930]:assert v.get(hex(a),0)==1
    handler=0x4C3200 if c['mode']=='manufacture' else 0x4C2DC0
    assert v.get(hex(handler),0)==e['player_calls']
    assert not v.get('0x4f2300',0) and not v.get('0x4f5240',0)
    if c['wizard']:
        assert v.get('0x4d8e60',0)==1 and owner[0x1094]==c['wizard']
    # 3B3 is watched in the full chain: numerical contribution functions never
    # read it,even when native registration/tick produced205/410.
    for row in r['observed_reads']:
        a,pc=int(row['address'],16),int(row['pc'],16)
        if a<probe.target.attack.TARGET+0x3B7 and a+row['size']>probe.target.attack.TARGET+0x3B3:
            raise AssertionError(('Unexpected WorkEnhancement read in selected chain',c['name'],row))
    md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;data=EXE.read_bytes()
    for row in r['observed_reads']:
        if int(row['address'],16)<4096:
            pc=int(row['pc'],16);i=next(md.disasm(pe_read(data,pc,15),pc))
            assert int(row['address'],16)==0 and row['size']==4 and 0x64 in i.prefix and 'fs:[0]' in i.op_str
    return post


def main():
    rows=[];posts={}
    for c in cases():
        args,checks=probe.build(**inputs(c));p=path_for(args)
        if p.exists():v=json.loads(p.read_text(encoding='utf8'));assert v['input']==args and v['case']==c
        else:
            r=probe.target.cpu.run(EXE,args,list(checks.items()),probe.f.OBS,probe.OWNER,0x1200)
            v=dict(case=c,input=args,result=r);p.write_text(json.dumps(v,indent=2),encoding='utf8')
        posts[c['name']]=verify(c,args,checks,v['result'])
        rows.append(dict(case=c['name'],file=p.name,sha256=digest(p.read_bytes()),outcome=outcome(c)))
        print(c['name'],outcome(c),'verified',flush=True)
    paired=[]
    for mode in ['manufacture','research']:
        for w in [1,2]:
            base=posts[mode+'-wizard-control'];actual=posts[mode+('-wizard-one' if w==1 else '-wizard-two')]
            # Complete unchanged common contexts,except explicit Crypt state.
            wanted={a:bytearray(b) for a,b in base.items()}
            probe.f.put(wanted[probe.target.attack.TARGET],0x3B3,205*w)
            probe.f.put(wanted[probe.OWNER],0x1094,w,1)
            for tag,a in [(200,probe.ROOM),(110,probe.MEMBER)]:
                probe.f.put(wanted[0x6EE540],4*tag,a);wanted[0x6ED540][tag]=1
            c=next(c for c in cases() if c['name']==mode+('-wizard-one' if w==1 else '-wizard-two'))
            _,checks=probe.build(**inputs(c))
            for a,b in checks.items():
                if a not in wanted:wanted[a]=bytearray(b)
            probe.f.put(wanted[probe.ROOM],0x53,110,2)
            assert actual==wanted,mode
            paired.append(mode+('-wizard-one' if w==1 else '-wizard-two'))
    (ROOT/'phase116-work-point-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows,
        paired_common_context_cases=paired,independent_Ghidra_CPU_controls=0),indent=2),encoding='utf8')


if __name__=='__main__':main()
