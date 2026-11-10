"""Strict phase184 discovery controls; not yet published as a format component.

Only bounded native recovery boundary evidence. Independent Ghidra controls,
machine schema/API publication and package integration remain required.
"""
from pathlib import Path
import json,struct
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
import probe_recovery_step as probe
from emulate_resistance import post_context
ROOT=Path(__file__).resolve().parent
u,sha,p=probe.u,probe.sha,probe.p
CASES=[{},dict(raw_y=0),dict(raw_y=-1490),dict(direction=0),dict(direction=512),dict(direction=1024),
       dict(raw_y=4096),dict(raw_x=30001,raw_z=-30002)]

def verify(c,args,ch,r,ofs):
    assert r['success'] and r['hit_return'] and not r['emulation_error']
    assert not r['protected_write_or_execution_failures'] and r['function_or_vtable_substitutions']==0
    assert all(x['returned'] for x in r['before_calls']) and len(r['before_calls'])==len(args['before_calls'])
    md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;blob=probe.EXE.read_bytes()
    for x in r['observed_reads']+r['observed_writes']:
        if int(x['address'],16)>=4096:continue
        pc=int(x['pc'],16);ins=next(md.disasm(p.pe_read(blob,pc,15),pc))
        assert int(x['address'],16)==0 and x['size']==4 and 0x64 in ins.prefix and 'fs:[0]' in ins.op_str,(c,x)
    post=post_context(ch,r);actor=probe.old.ACTOR;definition=u(post[actor],0x370)
    xyz=list(struct.unpack_from('<3i',post[definition],0x704))
    assert xyz==[c.get('raw_x',0),c.get('raw_y',1490),c.get('raw_z',0)]
    assert list(struct.unpack_from('<3i',post[p.target.RAW],0x1287))==xyz
    table=struct.unpack('<2560i',p.pe_read(blob,0x6C1488,0x2800));direction=c.get('direction',0x600)
    signed=lambda n:((n&0xFFFFFFFF)^0x80000000)-0x80000000
    mul=lambda a,b:signed(a*b)>>16
    initial=[u(ch[actor],o) for o in [0x1A,0x1E,0x22]]
    expected=[initial[0]+mul(table[direction],-xyz[1]),initial[1]-mul(table[direction+512],-xyz[1])]
    position=[u(post[actor],o) for o in [0x1A,0x1E,0x22]];assert position[:2]==expected,(c,position,expected)
    assert position[2] in [3584,4096]  # native floor adjustment, no new Z formula
    for i in range(6):
        b=post[probe.old.LOG][i*probe.old.STRIDE:(i+1)*probe.old.STRIDE];assert u(b,0)==i
        o,n,_=ofs['actor'];a=b[o:o+n]
        assert u(a,0x25E)==274 and u(a,0x3A9)==0 and u(a,0x40)==900
        assert [u(a,o) for o in [0x1A,0x1E,0x22]]==position and a[1343:]==ch[actor][1343:]
        o,n,_=ofs['owner'];owner=b[o:o+n];assert owner[0x108A:0x109C]==bytes(18)
    assert post[p.target.RAW]==ch[p.target.RAW]
    for a in [0x66D524,0x66D654,0x66D754,0x66D86C,0x66DB34,0x66E3EC,0x670574,0x6C1488,0x6E4144,0x6E4148]:assert post[a]==ch[a]
    unrelated=probe.old.old.old.UNRELATED;assert post[unrelated]==ch[unrelated]
    assert post[0x6ED540][101]==1 and not r['hp_requests']
    visits=r['visited_instructions']
    for address in [0x481200,0x494650,0x4E8DA0,0x494480]:assert visits.get(hex(address),0)==1
    assert not visits.get('0x483790',0) and not visits.get('0x496700',0)
    return dict(native_slot2=xyz,direction=direction,initial_position=initial,final_position=position,
        predicted_XY=expected,lower_page_only_FS0=True,participant_alive=True,raw_unchanged_by_native=True,
        instruction_count=r['instruction_count'])

def main():
    rows=[]
    for c in CASES:
        a,ch,s,f,ofs=probe.execute(c);out=verify(c,a,ch,s['result'],ofs)
        rows.append(dict(case=c,file=str(f.relative_to(ROOT)),sha256=sha(f.read_bytes()),outcome=out))
        print(c,out['final_position'],flush=True)
    result=dict(phase=184,status='strict_native_discovery_passed_unpublished',replays=rows,
        conclusion='Source rawY128B causally controls facing-relative recovery XY displacement in exact481200. Diagnostic rawX/Z changes preserve XY in this consumer.',
        required_next=['IndependentGhidraCPUcontrols','Exactcontaineredits/API/PEevidence','Publishschema/Russiandocs/packagepreservingprior161'],
        goal_status='active',source_writes=0)
    (ROOT/'phase184-recovery-step-discovery.json').write_text(json.dumps(result,indent=2),encoding='utf-8')

if __name__=='__main__':main()
