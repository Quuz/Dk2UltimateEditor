"""Whole-function Ghidra/PE attestation of raw EffectElement loader, spawn, full update and render queue."""
from pathlib import Path
from functools import cache
import json,re,sys,struct
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import EXE,pe_read
import emulate_element_palette_chain as native

ROOT=Path(__file__).resolve().parent
BLOCKS=[(0x66CC94,0x164,'Original CBridge vtable'),(0x66CBDC,0x20,'Original EffectElement vtable'),(0x66D0C4,0x40,'Original inherited client-entity vtable'),(0x66FCCC,0x28,'Original sprite vtable'),(0x66FCF4,0x28,'Original quad vtable')]

def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def sha(b):return native.sha(b)

@cache
def traces():
    v=load('phase140-element-palette-chain-emulations.json');out=[]
    for row in v['replays']:
        assert sha((ROOT/row['file']).read_bytes())==row['sha256'];out.append(load(row['file']))
    return out

def visited():return {int(a,16) for s in traces() for a in s['result']['visited_instructions'] if 0x400000<=int(a,16)<0x670000}

def entries():
    blob=EXE.read_bytes();md=Cs(CS_ARCH_X86,CS_MODE_32);pcs=visited();roots=set()
    for s in traces():
        roots.add(int(s['input']['address'],16));roots.update(int(c['address'],16) for c in s['input']['before_calls'])
    for pc in pcs:
        ins=next(md.disasm(pe_read(blob,pc,15),pc))
        if ins.mnemonic=='call' and ins.op_str.startswith('0x') and 0x400000<=int(ins.op_str,16)<0x670000:roots.add(int(ins.op_str,16))
    for a,n,_ in BLOCKS:roots.update(p for p in struct.unpack('<'+str(n//4)+'I',pe_read(blob,a,n)) if p in pcs)
    return sorted(roots)

def spans():
    blob=EXE.read_bytes();md=Cs(CS_ARCH_X86,CS_MODE_32);rows=[]
    for address in entries():
        name=f'ghidra/phase140-primary-{address:06x}-assembly.txt';text=(ROOT/name).read_text(encoding='utf8')
        pcs=[int(s,16) for s in re.findall(r'^([0-9A-Fa-f]{8}):',text,re.M)]
        assert pcs and pcs[0]==address,(hex(address),text[:160])
        last=next(md.disasm(pe_read(blob,pcs[-1],15),pcs[-1]));size=last.address+last.size-address
        cap=[i.address for i in md.disasm(pe_read(blob,address,size),address)];split=[p for p in cap if p not in pcs]
        assert all(p-1 in pcs and pe_read(blob,p-1,3)==bytes.fromhex('9bd97d') for p in split),(hex(address),[hex(p) for p in split[:30]])
        assert [p for p in cap if p not in split]==pcs,hex(address)
        rows.append(dict(address=address,length=size,assembly=name,assembly_sha256=sha((ROOT/name).read_bytes()),
            explicit_FWAIT_split_addresses=split,instruction_addresses=sorted(set(pcs+split))))
    return rows

def build():
    blob=EXE.read_bytes();rows=spans();covered=set()
    for row in rows:
        name=f'ghidra/phase140-primary-{row["address"]:06x}-memory.txt';saved=load(name);data=bytes(saved['data'])
        assert int(saved['address'],16)==row['address'] and len(data)==row['length'] and data==pe_read(blob,row['address'],row['length'])
        row.update(memory=name,memory_sha256=sha((ROOT/name).read_bytes()),bytes_sha256=sha(data));covered.update(row.pop('instruction_addresses'))
    pcs=visited();assert pcs<=covered,[hex(p) for p in sorted(pcs-covered)[:30]]
    blocks=[]
    for a,n,meaning in BLOCKS:
        name=f'ghidra/phase140-primary-block-{a:06x}-memory.txt';saved=load(name);data=bytes(saved['data'])
        assert int(saved['address'],16)==a and len(data)==n and data==pe_read(blob,a,n)
        blocks.append(dict(address=a,length=n,meaning=meaning,memory=name,memory_sha256=sha((ROOT/name).read_bytes()),bytes_sha256=sha(data)))
    return dict(phase=140,profile=dict(file=str(EXE),sha256=sha(blob)),functions=rows,blocks=blocks,
        executed_function_spans=len(entries()),unique_executed_original_PCs=len(pcs),all_executed_original_PCs_covered=True,
        scope=__doc__,limitations='Whole-function byte extents are not all-branch or whole-game coverage. Includes rawloader baseline and original native initializers; no substitutions.')

if __name__=='__main__':
    mode=sys.argv[1] if len(sys.argv)>1 else ''
    if mode in ['--queries','--memory']:
        if mode=='--queries':
            qs=[]
            for a in entries():
                for tool,suffix in [('create_function','create'),('disassemble_function','assembly')]:
                    args=dict(program='DKII-DX.exe',address=hex(a))
                    if tool=='create_function':args['disassemble_first']=True
                    qs.append(dict(id=f'phase140-primary-{a:06x}-{suffix}',tool=tool,args=args,print_limit=0))
            name='phase140-primary-assembly-queries.json'
        else:
            qs=[dict(id=f'phase140-primary-{r["address"]:06x}-memory',tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(r['address']),length=r['length']),print_limit=0) for r in spans()]
            qs += [dict(id=f'phase140-primary-block-{a:06x}-memory',tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(a),length=n),print_limit=0) for a,n,_ in BLOCKS]
            name='phase140-primary-memory-queries.json'
        (ROOT/name).write_text(json.dumps(qs,indent=2),encoding='utf8');print(len(qs),'queries',name)
    else:
        v=build();(ROOT/'phase140-element-palette-chain-primary.json').write_text(json.dumps(v,indent=2),encoding='utf8')
        print('Attested',len(v['functions']),'functions',v['unique_executed_original_PCs'],'PCs')
