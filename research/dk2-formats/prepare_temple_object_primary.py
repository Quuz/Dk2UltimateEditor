"""Attest whole original functions for rawObject->Temple entry, plus static registration."""
from pathlib import Path
from functools import cache
import json,re,sys,struct
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import EXE,pe_read
import emulate_temple_object_entry as native

ROOT=Path(__file__).resolve().parent
STATIC=[0x49E600]
BLOCKS=[(0x66E3EC,0x3A0,'World original vtable'),(0x66D654,0x80,'Object original vtable')]
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def sha(b):return native.sha(b)

@cache
def traces():
    out=[]
    for row in load('phase135-temple-object-emulations.json')['replays']:
        assert sha((ROOT/row['file']).read_bytes())==row['sha256'];out.append(load(row['file']))
    return out

def visited():return {int(a,16) for s in traces() for a in s['result']['visited_instructions'] if 0x400000<=int(a,16)<0x670000}

def entries():
    blob=EXE.read_bytes();md=Cs(CS_ARCH_X86,CS_MODE_32)
    assert struct.unpack('<I',pe_read(blob,0x66E3EC+0x1D4,4))[0]==0x508FE0
    assert struct.unpack('<I',pe_read(blob,0x66D654+0x20,4))[0]==0x4B2AD0
    roots={0x508FE0,0x4B2AD0}
    for s in traces():
        roots.add(int(s['input']['address'],16));roots.update(int(c['address'],16) for c in s['input']['before_calls'])
    for pc in visited():
        ins=next(md.disasm(pe_read(blob,pc,15),pc))
        if ins.mnemonic=='call' and ins.op_str.startswith('0x') and 0x400000<=int(ins.op_str,16)<0x670000:roots.add(int(ins.op_str,16))
    return sorted(roots)

def spans():
    blob=EXE.read_bytes();md=Cs(CS_ARCH_X86,CS_MODE_32);rows=[]
    for address in sorted(set(entries()+STATIC)):
        name=f'ghidra/phase135-primary-{address:06x}-assembly.txt';text=(ROOT/name).read_text(encoding='utf8')
        pcs=[int(s,16) for s in re.findall(r'^([0-9A-Fa-f]{8}):',text,re.M)]
        assert pcs and pcs[0]==address,(hex(address),text[:160])
        last=next(md.disasm(pe_read(blob,pcs[-1],15),pcs[-1]));size=last.address+last.size-address
        cap=[i.address for i in md.disasm(pe_read(blob,address,size),address)];split=[p for p in cap if p not in pcs]
        assert all(p-1 in pcs and pe_read(blob,p-1,3)==bytes.fromhex('9bd97d') for p in split),(hex(address),[hex(p) for p in split[:30]])
        assert [p for p in cap if p not in split]==pcs,hex(address)
        rows.append(dict(address=address,length=size,assembly=name,assembly_sha256=sha((ROOT/name).read_bytes()),
            static_only=address in STATIC,explicit_FWAIT_split_addresses=split,instruction_addresses=sorted(set(pcs+split))))
    return rows

def build():
    blob=EXE.read_bytes();rows=spans();covered=set()
    for row in rows:
        name=f'ghidra/phase135-primary-{row["address"]:06x}-memory.txt';saved=load(name);data=bytes(saved['data'])
        assert int(saved['address'],16)==row['address'] and len(data)==row['length'] and data==pe_read(blob,row['address'],row['length'])
        row.update(memory=name,memory_sha256=sha((ROOT/name).read_bytes()),bytes_sha256=sha(data));covered.update(row.pop('instruction_addresses'))
    pcs=visited();assert pcs<=covered,[hex(p) for p in sorted(pcs-covered)[:25]]
    blocks=[]
    for a,n,meaning in BLOCKS:
        name=f'ghidra/phase135-primary-block-{a:06x}-memory.txt';saved=load(name);data=bytes(saved['data'])
        assert int(saved['address'],16)==a and len(data)==n and data==pe_read(blob,a,n)
        blocks.append(dict(address=a,length=n,meaning=meaning,memory=name,memory_sha256=sha((ROOT/name).read_bytes()),bytes_sha256=sha(data)))
    return dict(phase=135,profile=dict(file=str(EXE),sha256=sha(blob)),functions=rows,blocks=blocks,
        executed_function_spans=len(entries()),static_only_function_spans=len(STATIC),unique_executed_original_PCs=len(pcs),
        all_executed_original_PCs_covered=True,scope=__doc__)

if __name__=='__main__':
    mode=sys.argv[1] if len(sys.argv)>1 else ''
    if mode in ['--queries','--memory']:
        if mode=='--queries':
            qs=[]
            for a in sorted(set(entries()+STATIC)):
                for tool,suffix in [('create_function','create'),('disassemble_function','assembly')]:
                    args=dict(program='DKII-DX.exe',address=hex(a))
                    if tool=='create_function':args['disassemble_first']=True
                    qs.append(dict(id=f'phase135-primary-{a:06x}-{suffix}',tool=tool,args=args,print_limit=0))
            name='phase135-primary-assembly-queries.json'
        else:
            qs=[dict(id=f'phase135-primary-{r["address"]:06x}-memory',tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(r['address']),length=r['length']),print_limit=0) for r in spans()]
            qs += [dict(id=f'phase135-primary-block-{a:06x}-memory',tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(a),length=n),print_limit=0) for a,n,_ in BLOCKS]
            name='phase135-primary-memory-queries.json'
        (ROOT/name).write_text(json.dumps(qs,indent=2),encoding='utf8');print(len(qs),'queries',name)
    else:
        value=build();(ROOT/'phase135-temple-object-primary.json').write_text(json.dumps(value,indent=2),encoding='utf8')
        print('Attested',len(value['functions']),'functions',value['unique_executed_original_PCs'],'PCs')
