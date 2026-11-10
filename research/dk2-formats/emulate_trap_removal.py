"""Complete original Trap removal after actual raw load and native pool init.

No World scheduler claim. The manager is invoked explicitly after creation and
optional activation/tick. Real cleanup/destructor/tag/owner/pool code runs;
unknown204 remains unresolved. All low-page accesses must be FS:[0] only.
"""
from pathlib import Path
import hashlib
import json
import struct
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
import probe_trap_removal as probe
from emulate_resistance import post_context
from build_effect_semantics import EXE,pe_read

ROOT=Path(__file__).resolve().parent


def cases():
    out=[]
    for ident in [1,9]:
        for active in [False,True]:
            for value in [0,0xFFF6,0xFFFF]:
                out.append(dict(name=f'trap{ident}-'+('active' if active else 'inactive')+f'-{value}',
                    ident=ident,active=active,unknown=value))
    out += [dict(name='upkeep-positive',unknown=0,upkeep=13),
            dict(name='upkeep-negative',unknown=0,upkeep=0xFFFF),
            dict(name='owner-mismatch-death',unknown=0,bad_cell=True),
            dict(name='definition-gate',unknown=0xFFF6,gate=1),
            dict(name='world-gate',unknown=0xFFF6,world_gate=1)]
    assert len(out)==17
    return out


def inputs(case):return {k:v for k,v in case.items() if k!='name'}
def sha(b):return hashlib.sha256(b).hexdigest()
def path_for(args):
    dependency=''.join(sha((ROOT/n).read_bytes()) for n in ['probe_trap_removal.py','probe_raw_trap_factory.py','native_unicorn_factorytrace.py'])
    return ROOT/'ghidra'/('phase120-trap-removal-'+sha((json.dumps(args,sort_keys=True)+dependency).encode())[:12]+'.json')


def verify_low_page_reads(result):
    image=EXE.read_bytes();md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True
    for row in result['observed_reads']:
        address,pc=int(row['address'],16),int(row['pc'],16)
        if address>=4096:continue
        assert address==0 and row['size']==4,('Invalid low-page access',row)
        ins=next(md.disasm(pe_read(image,pc,15),pc))
        assert 0x64 in ins.prefix and 'fs:[0]' in ins.op_str


def verify(case,args,checks,expected,result):
    assert result['success'] and result['hit_return'] and not result['protected_write_or_execution_failures']
    assert all(row['returned'] for row in result['before_calls'])
    assert len(result['before_calls'])==7+int(case.get('active',True))+int(case.get('bad_cell',False))
    state=post_context(checks,result)
    def value(address,offset=0,n=4,signed=False):return int.from_bytes(state[address][offset:offset+n],'little',signed=signed)
    for address,key in [(probe.creation.RAW,'raw'),(probe.creation.MID,'mid'),(probe.f.DEFINITION,'definition')]:
        assert bytes(state[address])==bytes.fromhex(expected[key])+bytes([0xA5])*64
    assert bytes(state[probe.ARENA][536:])==bytes([0xA5])*64
    assert list(struct.unpack_from('<III',state[probe.ARENA],0))==[0,probe.ARENA+12,0]
    assert list(struct.unpack_from('<III',state[probe.ARENA],12))==[512,probe.ARENA+524,probe.ARENA]
    assert list(struct.unpack_from('<III',state[probe.ARENA],524))==[0,0,probe.ARENA+12]
    assert value(probe.f.WORLD,0x76)==0 and value(probe.f.WORLD,0x32)==0
    assert value(probe.f.WORLD,0x6A)==probe.ARENA and value(probe.f.WORLD,0x72)==probe.ARENA+524
    assert list(struct.unpack_from('<III',state[probe.f.WORLD],0x52))==[0,probe.f.WORLD+0x5E,0]
    assert list(struct.unpack_from('<III',state[probe.f.WORLD],0x5E))==[0,0,probe.f.WORLD+0x52]
    assert value(probe.f.PLAYER,0xC,2)==0 and value(probe.f.PLAYER,0x22)==0
    assert value(probe.f.KINDS,6,2)==0
    assert value(0x6EE540,400)==0 and state[0x6ED540][100]==0
    assert value(0x6A4818)==4094 and value(0x6A4818,4)==101
    cell=(6*32+5)*16
    assert value(probe.f.MAP,cell,2)==0
    upkeep=int.from_bytes(bytes.fromhex(expected['raw'])[0x202:0x204],'little',signed=True)
    residual=upkeep if case.get('active',True) and case.get('world_gate',0) else 0
    assert value(probe.f.PLAYER,0x10D6,signed=True)==residual
    assert value(probe.f.PLAYER,0x10E2,signed=True)==-residual
    assert value(probe.creation.IO,0)==int(not case.get('gate',0))
    assert value(probe.creation.IO,4)==1
    visits=result['visited_instructions']
    for a in [0x554AD0,0x4B7C10,0x4A7940,0x4B7C90,0x4A7B20,0x4B7470,0x4B7490,0x4B2910,0x554BD0]:assert visits.get(hex(a),0)==1
    assert visits.get('0x4c1b90',0)==int(case.get('active',True) and not case.get('world_gate',0))
    verify_low_page_reads(result)
    image=EXE.read_bytes()
    unknown=[]
    for row in result['observed_reads']:
        a,pc=int(row['address'],16),int(row['pc'],16)
        if a<probe.f.DEFINITION+0x9A and a+row['size']>probe.f.DEFINITION+0x98:unknown.append(row)
    assert not unknown
    assert bytes(state[0x66D754])==pe_read(image,0x66D754,96)
    assert not [row for row in result['observed_writes'] if 0x66D754<=int(row['address'],16)<0x66D7B4]
    # Deallocation restores free-list metadata; it does not erase the object.
    assert bytes.fromhex(result['captured_hex'])==bytes(state[probe.ARENA][232:523])
    return state,dict(registered=False,pool_free_bytes=512,pool_used_bytes=0,
        owner_expense=residual,owner_income=-residual,unknown204=int.from_bytes(state[probe.f.DEFINITION][0x98:0x9A],'little'),
        unknown_reads=0,sound_releases=1,refund_calls=visits.get('0x4c1b90',0))


def main():
    rows=[];bases={};paired=[]
    for case in cases():
        args,checks,expected=probe.build(**inputs(case));path=path_for(args)
        if path.exists():saved=json.loads(path.read_text(encoding='utf8'));assert saved['input']==args and saved['case']==case and saved['expected']==expected
        else:
            result=probe.creation.base.cpu.run(EXE,args,list(checks.items()),probe.f.OBS,expected['node'],291)
            saved=dict(case=case,input=args,expected=expected,result=result)
            path.write_text(json.dumps(saved,ensure_ascii=False,indent=2),encoding='utf8')
        state,outcome=verify(case,args,checks,expected,saved['result'])
        if case['name'].startswith('trap'):
            key=case['ident'],case['active']
            if case['unknown']==0:bases[key]=state
            else:
                wanted={a:bytearray(b) for a,b in bases[key].items()}
                for a,k in [(probe.creation.RAW,'raw'),(probe.creation.MID,'mid'),(probe.f.DEFINITION,'definition')]:
                    blob=bytes.fromhex(expected[k]);wanted[a][:len(blob)]=blob
                assert state==wanted
                paired.append(case['name'])
        rows.append(dict(case=case['name'],file=path.name,sha256=sha(path.read_bytes()),outcome=outcome))
        print(case['name'],'verified',outcome,flush=True)
    (ROOT/'phase120-trap-removal-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows,
        paired_full_context_cases=paired,measured_opaque_baselines=4),ensure_ascii=False,indent=2),encoding='utf8')


if __name__=='__main__':main()
