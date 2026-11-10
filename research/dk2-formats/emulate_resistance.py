"""Three matched-type rawCreature->loader->whole melee chains, paired whole contexts.

Baseline whole native output is measured, not an independent interpretation of
all2199definition bytes. Variant expectations modify exactly raw/mid/loaded
resistance value bytes; HP980 and no damage-path reads are separately checked.
"""
import hashlib
import json
from pathlib import Path
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
import probe_creature_resistance as probe
from build_effect_semantics import EXE,pe_read

ROOT=Path(__file__).resolve().parent
VALUES=[0,100,255]


def digest(b):return hashlib.sha256(b).hexdigest()


def post_context(checks,r):
    post={a:bytearray(b) for a,b in checks.items()}
    for row in r['context_mismatches']:
        a=int(row['address'],16);assert len(post[a])==row['length']
        for d in row['differences']:
            assert post[a][d['offset']]==d['expected'];post[a][d['offset']]=d['actual']
    return post


def path_for(args):
    backend=''.join(digest((ROOT/n).read_bytes()) for n in ['native_unicorn_factorytrace.py','probe_creature_resistance.py'])
    tag=digest((json.dumps(args,sort_keys=True)+backend).encode())[:10]
    return ROOT/'ghidra'/f'phase113-resistance-{tag}.json'


def expected(value):
    args,e,checks=probe.build(0);p=path_for(args);v=json.loads(p.read_text(encoding='utf8'))
    assert v['input']==args
    wanted=post_context(checks,v['result'])
    for i in range(4):
        wanted[probe.RAW][0xE0D+2*i]=value
        wanted[probe.MID][0x11FD+2*i]=value
        wanted[probe.attack.TARGET_DEF][0x61D+2*i]=value
    return wanted


def verify(value,args,checks,wanted,r):
    assert r['success'] and r['hit_return'] and not r['protected_write_or_execution_failures']
    assert all(x['returned'] for x in r['before_calls']) and len(r['before_calls'])==6
    post=post_context(checks,r);assert post==wanted
    assert int.from_bytes(post[probe.attack.TARGET][0x40:0x44],'little')==980
    assert int.from_bytes(checks[probe.attack.TARGET][0x40:0x44],'little')==1000
    definition=post[probe.attack.TARGET_DEF]
    assert definition[0x61C:0x624]==bytes([1,value])*4 and definition[0x78A]==4
    assert post[probe.cf.DEFINITION][0x77A]==1
    assert definition.hex()==r['captured_hex']
    visits=r['visited_instructions']
    assert all(visits.get(hex(a),0)==1 for a in [0x491DC0,0x568820,0x568E30,0x492320,0x498E30,0x4B2CE0,0x4430D0])
    watched=[];md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;data=EXE.read_bytes()
    for x in r['observed_reads']:
        a,pc=int(x['address'],16),int(x['pc'],16)
        if a<4096:
            assert a==0 and x['size']==4
            ins=next(md.disasm(pe_read(data,pc,15),pc));assert 0x64 in ins.prefix and 'fs:[0]' in ins.op_str
        if any(a<probe.attack.TARGET_DEF+o+n and a+x['size']>probe.attack.TARGET_DEF+o for o,n in [(0x61C,8),(0x78A,1)]):
            assert pc in [0x4936B4,0x4936C8] and a==probe.attack.TARGET_DEF+0x78A
            watched.append(x)
    assert len(watched)==8
    hp=[x for x in r['observed_writes'] if int(x['address'],16)==probe.attack.TARGET+0x40]
    assert len(hp)==1 and int(hp[0]['pc'],16)==0x4B2F03 and hp[0]['value']==980


def main():
    rows=[]
    for value in VALUES:
        args,e,checks=probe.build(value);p=path_for(args)
        if p.exists():v=json.loads(p.read_text(encoding='utf8'));assert v['input']==args and v['expected_attack']==e
        else:
            r=probe.cpu.run(EXE,args,list(checks.items()),probe.f.OBS,probe.attack.TARGET_DEF,2199)
            v=dict(input=args,expected_attack=e,result=r);p.write_text(json.dumps(v,indent=2),encoding='utf8')
        wanted=expected(value);verify(value,args,checks,wanted,v['result'])
        rows.append(dict(value=value,file=p.name,sha256=digest(p.read_bytes()),compared_regions=len(wanted),compared_bytes=sum(map(len,wanted.values())),HP=980))
        print('matched type1;value',value,'HP1000->980;whole paired contexts verified',flush=True)
    (ROOT/'phase113-resistance-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows),indent=2),encoding='utf8')


if __name__=='__main__':main()
