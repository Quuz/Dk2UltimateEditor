"""Raw Trap definition->real creation/activation/tick; unknown204 remains open.

Whole747/209-byte loader expectations are independent existing projections.
Opaque complete scene baselines are measured; unknown-value variants predict
all differences in raw, intermediate, definition and checksum only. No live
assets, complete World scheduler, target hits or autonomous placement claimed.
"""
import hashlib
import json
from pathlib import Path
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
import probe_raw_trap_factory as probe
from emulate_resistance import post_context
from build_effect_semantics import EXE,pe_read

ROOT=Path(__file__).resolve().parent


def cases():
    out=[]
    for ident in [1,9]:
        for mode in ['create','inactive-tick','active-tick']:
            for value in [0,0xFFF6,0xFFFF]:
                out.append(dict(name=f'trap{ident}-{mode}-{value}',ident=ident,unknown=value,
                    activation=mode=='active-tick',tick=mode!='create'))
    out += [dict(name='health-control',ident=1,unknown=0,activation=True,tick=True,health=1234),
        dict(name='upkeep-positive',ident=1,unknown=0,activation=True,tick=True,upkeep=13),
        dict(name='upkeep-negative',ident=1,unknown=0,activation=True,tick=True,upkeep=0xFFFF),
        dict(name='owner-mismatch',ident=1,unknown=0,activation=True,tick=True,cell_owner=0),
        dict(name='loading-gate',ident=1,unknown=0xFFF6,activation=True,tick=True,gate=1)]
    assert len(out)==23
    return out


def inputs(case):return {k:v for k,v in case.items() if k!='name'}
def digest(b):return hashlib.sha256(b).hexdigest()
def path_for(args):
    frozen=''.join(digest((ROOT/n).read_bytes()) for n in ['probe_raw_trap_factory.py','probe_trap_factory.py','native_unicorn_factorytrace.py'])
    return ROOT/'ghidra'/('phase119-raw-trap-'+digest((json.dumps(args,sort_keys=True)+frozen).encode())[:12]+'.json')


def verify(case,args,checks,expected,result):
    assert result['success'] and result['hit_return'] and not result['protected_write_or_execution_failures']
    assert len(result['before_calls'])==5+int(case.get('activation',False))+int(case.get('tick',False))
    assert all(r['returned'] for r in result['before_calls'])
    post=post_context(checks,result)
    for address,key,n in [(probe.RAW,'raw',579),(probe.MID,'mid',747),(probe.f.DEFINITION,'definition',209)]:
        assert bytes(post[address])==bytes.fromhex(expected[key])+bytes([0xA5])*64
    blob=bytes.fromhex(result['captured_hex']);assert len(blob)==291
    u=lambda o,n=4:int.from_bytes(blob[o:o+n],'little')
    assert u(0)==0x66D754 and u(4,2)==100 and u(0x12)==3
    assert u(0xF6,1)==case['ident'] and u(0xF7)==probe.f.DEFINITION
    assert u(0x28,2)==7 and u(0x2A)==probe.f.PLAYER
    assert u(0x1A)==0x5800 and u(0x1E)==0x6800 and u(0x22)==0x2000
    raw=bytes.fromhex(expected['raw']);maxhp=int.from_bytes(raw[0x1F0:0x1F2],'little')
    mismatch=case.get('cell_owner',7)!=7 and case.get('tick',False)
    assert u(0x40)==(0 if mismatch else maxhp)
    assert bool(u(0x10C,1)&0x20)==(not case.get('activation',False))
    upkeep=int.from_bytes(raw[0x202:0x204],'little',signed=True) if case.get('activation',False) else 0
    owner=post[probe.f.PLAYER]
    assert int.from_bytes(owner[0x10D6:0x10DA],'little',signed=True)==upkeep
    assert int.from_bytes(owner[0x10E2:0x10E6],'little',signed=True)==-upkeep
    assert int.from_bytes(post[probe.f.OUT][4:8],'little')==expected['node']
    assert int.from_bytes(post[probe.f.WORLD][0x32:0x36],'little')==1
    def word(address,offset=0,n=4):return int.from_bytes(post[address][offset:offset+n],'little')
    assert word(probe.f.WORLD,0x76)==304
    assert word(probe.f.BLOCK,0)==208 and word(probe.f.BLOCK,208)==304
    assert word(probe.f.BLOCK,212)==probe.f.UH and word(probe.f.BLOCK,216)==probe.f.UT
    assert word(probe.f.UH,8)==probe.f.BLOCK+208 and word(probe.f.UT,4)==probe.f.BLOCK+208
    assert word(0x6EE540,100*4)==expected['node'] and post[0x6ED540][100]==1
    assert word(0x6A4818,0)==4093 and word(0x6A4818,4)==101
    assert word(probe.f.KINDS,6,2)==100 and word(probe.f.PLAYER,0xC,2)==100
    assert word(probe.f.PLAYER,0x22)==1
    # One definition sound lookup only when the original loader gate is clear.
    assert int.from_bytes(post[probe.IO],'little')==int(not case.get('gate',0))
    unknown_reads=[];md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;image=EXE.read_bytes()
    for row in result['observed_reads']:
        address,pc=int(row['address'],16),int(row['pc'],16)
        if address<4096:
            assert address==0 and row['size']==4
            ins=next(md.disasm(pe_read(image,pc,15),pc));assert 0x64 in ins.prefix and 'fs:[0]' in ins.op_str
        if address<probe.f.DEFINITION+0x9A and address+row['size']>probe.f.DEFINITION+0x98:unknown_reads.append(row)
    assert not unknown_reads
    assert not [r for r in result['observed_writes'] if 0x66D754<=int(r['address'],16)<0x66D7B4]
    assert bytes(post[0x66D754])==pe_read(image,0x66D754,96)
    visits=result['visited_instructions']
    for address in [0x4AA330,0x570B60,0x570D10,0x4AA440,0x4B7C10,0x4B7400,0x4A7940]:assert visits.get(hex(address),0)==1
    assert visits.get('0x4a96d0',0)==int(case.get('activation',False))
    assert visits.get('0x4a84e0',0)==int(case.get('tick',False))
    return post,dict(HP=u(0x40),flags10C=u(0x10C,1),owner_upkeep=upkeep,
        owner_income=-upkeep,definition_unknown=int.from_bytes(post[probe.f.DEFINITION][0x98:0x9A],'little'),
        definition_checksum=int.from_bytes(post[probe.f.DEFINITION][0x6C:0x70],'little'),unknown_reads=0,
        sound_lookups=int(not case.get('gate',0)),readiness_calls=visits.get('0x4a7d10',0))


def main():
    rows=[];baselines={};paired=[]
    for case in cases():
        args,checks,expected=probe.build(**inputs(case));path=path_for(args)
        if path.exists():saved=json.loads(path.read_text(encoding='utf8'));assert saved['input']==args and saved['case']==case and saved['expected']==expected
        else:
            result=probe.base.cpu.run(EXE,args,list(checks.items()),probe.f.OBS,expected['node'],291)
            saved=dict(case=case,input=args,expected=expected,result=result)
            path.write_text(json.dumps(saved,ensure_ascii=False,indent=2),encoding='utf8')
        post,outcome=verify(case,args,checks,expected,saved['result'])
        if case['name'].startswith('trap'):
            key=case['ident'],case['activation'],case['tick']
            if case['unknown']==0:baselines[key]=post
            else:
                wanted={a:bytearray(b) for a,b in baselines[key].items()}
                for a,k in [(probe.RAW,'raw'),(probe.MID,'mid'),(probe.f.DEFINITION,'definition')]:
                    blob=bytes.fromhex(expected[k]);wanted[a][:len(blob)]=blob
                assert post==wanted,[(hex(a),[i for i,(x,y) in enumerate(zip(post[a],b)) if x!=y][:16]) for a,b in wanted.items() if post[a]!=b]
                paired.append(case['name'])
        rows.append(dict(case=case['name'],file=path.name,sha256=digest(path.read_bytes()),outcome=outcome,
            checked_regions=len(checks),checked_bytes=sum(len(b) for b in checks.values())))
        print(case['name'],'verified',outcome,flush=True)
    (ROOT/'phase119-raw-trap-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows,
        paired_full_context_cases=paired,measured_baselines=6),ensure_ascii=False,indent=2),encoding='utf8')


if __name__=='__main__':main()
