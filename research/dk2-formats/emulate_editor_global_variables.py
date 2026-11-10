"""Whole Pro numerical defaults and registered Variables callback, prepared MID envelope."""
from pathlib import Path
import json,struct,re
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from capstone.x86 import X86_OP_REG,X86_OP_MEM,X86_OP_IMM
from build_effect_semantics import pe_read
import probe_editor_global_defaults as probe
import native_editor_text_executor as executor
import dk2_formats as fmt
ROOT=Path(__file__).resolve().parent
engine=probe.old.engine
sha=probe.old.sha
CTX=probe.CTX
RECORD=0x17600000

def cases():
    return [dict(name=n,records=[list(pair) for pair in records],reinitialize=again) for n,records,again in [
        ('defaults',[],False),('integer42',[(100,42)],False),('negative80',[(100,-80)],False),
        ('signed-max',[(100,0x7fffffff)],False),('signed-min',[(100,0x80000000)],False),
        ('unsigned-bits',[(100,0xffffffff)],False),('scaled-all',[(t,(i+1)*4096) for i,t in enumerate([67,155,158,185,220,221])],False),
        ('scaled-negative',[(67,-1)],False),('unknown233',[(233,99)],False),
        ('unknown19',[(19,99)],False),('unknown29',[(29,99)],False),('unknown186',[(186,99)],False),
        ('not-supported242',[(242,99)],False),('temple43',[(43,0)],False),
        ('reinitialize-retains',[(100,42)],True),('possession-zero',[(113,0),(114,0)],False)]]

def literal_table(start,end,member,size):
    blob=engine.EXE.read_bytes();md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True
    regs={'ebx':('context',0),'ebp':0};out=bytearray(size);assigned=[]
    for ins in md.disasm(pe_read(blob,start,end-start),start):
        assert ins.mnemonic=='mov',(hex(ins.address),ins.mnemonic)
        dst,src=ins.operands
        if dst.type==X86_OP_REG:
            name=ins.reg_name(dst.reg)
            if src.type==X86_OP_IMM:regs[name]=src.imm&0xffffffff
            elif src.type==X86_OP_MEM:
                assert ins.reg_name(src.mem.base)=='ebx' and src.mem.disp==member and src.mem.index==0
                regs[name]=('table',0)
            else:raise AssertionError(ins.op_str)
        else:
            assert dst.type==X86_OP_MEM and dst.size==4 and dst.mem.index==0
            assert regs[ins.reg_name(dst.mem.base)]==('table',0)
            value=src.imm&0xffffffff if src.type==X86_OP_IMM else regs[ins.reg_name(src.reg)]
            assert isinstance(value,int)
            offset=dst.mem.disp;assert 0<=offset<=size-4
            struct.pack_into('<I',out,offset,value);assigned.append(offset)
    return bytes(out),assigned

def static_defaults():
    ids,ip=literal_table(0x4800E7,0x480C96,0x6B4,193*4)
    values,vp=literal_table(0x480DCA,0x481CC4,0x6A0,248*8)
    assert sorted(ip)==list(range(0,193*4,4))
    return list(struct.unpack('<193I',ids)),values,vp

def build(c):
    f=probe.build();mem={int(v['address'],16):bytes.fromhex(v['hex']) for v in f['memory']}
    for i,(typ,value) in enumerate(c['records']):
        raw=struct.pack('<4I',typ,value&0xffffffff,0,0)
        if typ==43:
            source=ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/GlobalVariables.kwd'
            raw=next(r for s in fmt.parse(source.read_bytes()) if s.signature==0xE6 for r in s.records
                     if len(r)==16 and int.from_bytes(r[:4],'little')==43)
        address=RECORD+i*0x100
        mem[address]=bytes([0x5a])*16+raw+bytes([0xa5])*64
        f['calls'].append(dict(address=0x49EC90,ecx=0,stack=[address]))
    if c['reinitialize']:f['calls'].append(dict(address=0x47FFB0,ecx=CTX,stack=[]))
    f['memory']=[engine.region(a,b) for a,b in sorted(mem.items())]
    return f

def execute(c):
    f=build(c);backend=sha(Path(executor.__file__).read_bytes());tag=sha(json.dumps(f,sort_keys=True).encode()+backend.encode())[:12]
    path=ROOT/'ghidra'/f'phase166-editor-variables-{tag}.json'
    if path.exists():
        d=json.loads(path.read_text(encoding='utf-8'));assert d['input']==f and d['backend_sha256']==backend
    else:
        d=dict(input=f,backend_sha256=backend,result=executor.run(f));path.write_text(json.dumps(d,indent=2),encoding='utf-8')
    return f,d,path

def verify(c,f,r):
    assert r['success'] and r['error'] is None and not r['failures']
    assert r['code_substitutions']==0 and not r['low_reads'] and not r['low_writes']
    post={int(v['address'],16):bytes.fromhex(v['hex']) for v in r['output']}
    inputs={int(v['address'],16):bytes.fromhex(v['hex']) for v in f['memory']}
    keys,default,_=static_defaults();wanted=bytearray(default);accepted=[]
    scaled={67,155,158,185,220,221}
    for i,(typ,value) in enumerate(c['records']):
        assert post[RECORD+i*0x100]==inputs[RECORD+i*0x100]
        okay=typ in keys;accepted.append(okay)
        if okay:
            signed=struct.unpack('<i',struct.pack('<I',value&0xffffffff))[0]
            struct.pack_into('<d',wanted,typ*8,signed/(4096 if typ in scaled else 1))
    pointer,count,cap,grow,unused,ids,icount,icap,igrow=struct.unpack('<9I',post[CTX+0x6A0]);heap=post[engine.HEAP]
    assert (count,cap,grow,unused,icount,icap,igrow)==(248,248,0,0,193,193,0)
    assert heap[pointer-engine.HEAP:pointer-engine.HEAP+len(wanted)]==wanted
    assert struct.unpack_from('<193I',heap,ids-engine.HEAP)==tuple(keys)
    assert post[0]==inputs[0]==bytes(4096)
    assert all(v['returned'] for v in r['calls']) and len(r['calls'])==1+len(c['records'])+int(c['reinitialize'])
    assert r['visited_instructions'].get('0x47ffb0')==1+int(c['reinitialize'])
    assert r['visited_instructions'].get('0x49ec90',0)==len(c['records'])
    assert struct.unpack_from('<I',post[engine.ALLOC],8)[0]==0
    return dict(native_steps=r['instruction_count'],all248_doubles_match_literal_oracle=True,all193_IDs_match_literal_oracle=True,
        accepted=accepted,records_unchanged=True,reinitialize_retains=c['reinitialize'],
        values=[dict(type=typ,input_bits=int.from_bytes(inputs[RECORD+i*0x100][20:24],'little'),
                     model=struct.unpack_from('<d',wanted,typ*8)[0]) for i,(typ,value) in enumerate(c['records'])])

def main():
    rows=[];keys,default,assigned=static_defaults()
    for c in cases():
        f,d,path=execute(c);out=verify(c,f,d['result'])
        rows.append(dict(case=c,file=str(path.relative_to(ROOT)),sha256=sha(path.read_bytes()),outcome=out))
        print(c['name'],out['accepted'],'verified',flush=True)
    data=dict(scope=__doc__,replays=rows,literal_IDs=keys,literal_defaults=[struct.unpack_from('<d',default,i*8)[0] for i in range(248)],
        literal_default_assigned_DWORD_offsets=assigned,limits=['Prepared16-byteMIDprefix+actual16-byteVariablepayload,not wholefileparser.',
            'Baseline twoarrays independentlydecodedfromattestedstraightlineMOVstores;otherpreparedmemorynotallsemanticallydecoded.',
            'Editor defaults arenotcurrent loadedgame settings orproof of gameplaymeaning.'])
    (ROOT/'phase166-editor-variables-emulations.json').write_text(json.dumps(data,indent=2),encoding='utf-8')

if __name__=='__main__':main()
