"""Whole original editor function spans for the accepted Trap model/serializer paths."""
from pathlib import Path
from functools import cache
import json,re,sys,struct
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import pe_read
from audit_targeted_spell import executable_ranges
import emulate_trap_model_chain as native
import prepare_room_model_primary as common
ROOT=Path(__file__).resolve().parent
EXE=native.engine.EXE
sha=native.sha
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
@cache
def traces():
    out=[]
    for row in load('phase146-Traps-model-emulations.json')['replays']:
        p=ROOT/row['file'];assert sha(p.read_bytes())==row['sha256'];out.append(load(row['file']))
    return out
@cache
def visited():
    ranges=executable_ranges(EXE.read_bytes())
    return {int(pc,16) for d in traces() for pc in d['result']['visited_instructions'] if any(a<=int(pc,16)<a+n for a,n in ranges)}
def inherited():
    v=common.build();assert v==load('phase127-Room-model-primary-manifest.json');return v
def entries():
    old={r['address'] for r in inherited()['fresh_functions']+inherited()['inherited_functions']}
    blob=EXE.read_bytes();md=Cs(CS_ARCH_X86,CS_MODE_32);roots={c['address'] for d in traces() for c in d['input']['calls']}
    for pc in visited():
        ins=next(md.disasm(pe_read(blob,pc,15),pc))
        if ins.mnemonic=='call' and ins.op_str.startswith('0x'):
            target=int(ins.op_str,16)
            if target in visited():roots.add(target)
    # Actual original resource dispatch table; only targets reached by the
    # accepted CPU cases are roots. No interior basic blocks are functions.
    roots.update(p for p in struct.unpack('<33I',pe_read(blob,0x50AC60,132)) if p in visited())
    # Canonical Ghidra owner of the executed shared strchr tail, independently
    # queried at4AD61B; entry4AD560 (tail target4AD566 is not a new function).
    roots.add(0x4AD560)
    return sorted(roots-old)
def spans():
    blob=EXE.read_bytes();md=Cs(CS_ARCH_X86,CS_MODE_32);out=[]
    for a in entries():
        file=f'ghidra/phase146-primary-Pro-{a:06x}-assembly.txt';text=(ROOT/file).read_text(encoding='utf8')
        pcs=[int(x,16) for x in re.findall(r'^([0-9a-fA-F]{8}):',text,re.M)];assert pcs and pcs[0]==a,(hex(a),text[:100])
        last=next(md.disasm(pe_read(blob,pcs[-1],15),pcs[-1]));n=last.address+last.size-a
        # CRT functions may have disjoint shared tails. The bridge listing
        # includes embedded jump tables between code blocks; do not linear-
        # decode those DWORDs as if they were instructions. Each listed PC is
        # decoded independently, while the entire byte extent is attested.
        decoded=[];gaps=[]
        for j,pc in enumerate(pcs):
            ins=next(md.disasm(pe_read(blob,pc,15),pc));decoded.append(pc)
            end=pc+ins.size
            if ins.mnemonic=='wait' and j+1<len(pcs) and pcs[j+1]>end:
                follow=next(md.disasm(pe_read(blob,end,15),end))
                if follow.mnemonic.startswith('fn'):decoded.append(end);end+=follow.size
            nxt=pcs[j+1] if j+1<len(pcs) else a+n
            assert end<=nxt,(hex(a),hex(pc),hex(end),hex(nxt))
            if end<nxt:gaps.append(dict(address=end,length=nxt-end))
        out.append(dict(address=a,length=n,assembly=file,assembly_sha256=sha((ROOT/file).read_bytes()),unlisted_intervals=gaps,PCs=decoded))
    return out
def build():
    old=inherited();blob=EXE.read_bytes();md=Cs(CS_ARCH_X86,CS_MODE_32);covered=set();reused=[]
    for row in old['fresh_functions']+old['inherited_functions']:
        pcs={i.address for i in md.disasm(pe_read(blob,row['address'],row['length']),row['address'])}
        if pcs&visited():covered|=pcs;reused.append(row['address'])
    out=spans()
    for row in out:
        covered.update(row.pop('PCs'));file=f'ghidra/phase146-primary-Pro-{row["address"]:06x}-memory.txt';data=bytes(load(file)['data'])
        assert data==pe_read(blob,row['address'],row['length']);row.update(memory=file,memory_sha256=sha((ROOT/file).read_bytes()),bytes_sha256=sha(data))
    left=sorted(visited()-covered)
    assert not left,[hex(pc) for pc in left[:30]]
    block='ghidra/phase146-Pro-resource-vtable-memory.txt';saved=load(block);data=bytes(saved['data'])
    assert int(saved['address'],16)==0x50AC44 and len(data)==160 and data==pe_read(blob,0x50AC44,160)
    return dict(phase=146,profile=dict(file=str(EXE),sha256=sha(blob)),functions=out,
        blocks=[dict(address=0x50AC44,length=160,memory=block,memory_sha256=sha((ROOT/block).read_bytes()),bytes_sha256=sha(data),
            scope='Constant/table region: resource destructor cell, constants and resource dispatch table starting50AC60; not160bytes ofonevtable.')],
        inherited_common=dict(file='phase127-Room-model-primary-manifest.json',sha256=sha((ROOT/'phase127-Room-model-primary-manifest.json').read_bytes()),reused_function_addresses=reused),
        unique_executed_original_PCs=len(visited()),all_executed_original_PCs_covered=True,
        external_providers='Declared bounded CFile/Win32 IAT adapters only; no original function substitutions.')
if __name__=='__main__':
    mode=sys.argv[1] if len(sys.argv)>1 else ''
    if mode in ['--queries','--memory']:
        if mode=='--queries':
            qs=[dict(id=f'phase146-primary-Pro-{a:06x}-{suffix}',tool=tool,args=dict(program='DK2 Editor pro.exe',address=hex(a)),print_limit=0) for a in entries() for tool,suffix in [('create_function','create'),('disassemble_function','assembly')]];name='phase146-primary-assembly-queries.json'
        else:
            qs=[dict(id=f'phase146-primary-Pro-{r["address"]:06x}-memory',tool='read_memory',args=dict(program='DK2 Editor pro.exe',address=hex(r['address']),length=r['length']),print_limit=0) for r in spans()];name='phase146-primary-memory-queries.json'
        (ROOT/name).write_text(json.dumps(qs,indent=2),encoding='utf8');print(name,len(qs))
    else:
        d=build();(ROOT/'phase146-Traps-model-primary.json').write_text(json.dumps(d,indent=2),encoding='utf8');print(len(d['functions']),'newfunctions',len(d['inherited_common']['reused_function_addresses']),'inherited',d['unique_executed_original_PCs'],'PCs')
