"""Attest original bytes and every executed PC of primary graphical picking and actual raw definition loaders."""
from pathlib import Path
from functools import cache
import json,re,struct,sys
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import EXE,pe_read
from audit_targeted_spell import executable_ranges
import emulate_primary_pick as native
ROOT=Path(__file__).resolve().parent
sha=native.sha
BLOCKS=[(0x66E3EC,0x3A0,'Original World vtable'),(0x66FD1C,0x68,'Original dynamic/base/animated model vtables'),(0x66FC24,0x74,'Original picking floating constants')]
STATIC=[0x5992B0,0x580EC0,0x584900]
SPECIAL={}
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
@cache
def traces():
    out=[]
    for row in load('phase154-primary-pick-emulations.json')['replays']:
        assert sha((ROOT/row['file']).read_bytes())==row['sha256'];out.append(load(row['file']))
    return out
@cache
def visited():
    ranges=executable_ranges(EXE.read_bytes())
    return {int(p,16) for d in traces() for p in d['result']['visited_instructions'] if any(a<=int(p,16)<a+n for a,n in ranges)}
def entries():
    pcs=visited();roots=set(STATIC);md=Cs(CS_ARCH_X86,CS_MODE_32);blob=EXE.read_bytes()
    for d in traces():
        roots.add(int(d['input']['address'],16));roots.update(int(c['address'],16) for c in d['input']['before_calls'])
        roots.update(b for a,b in d['result']['observed_call_edges'] if b in pcs)
    pending=list(roots)
    while pending:
        a=pending.pop();ins=next(md.disasm(pe_read(blob,a,15),a))
        if ins.mnemonic=='jmp' and ins.op_str.startswith('0x'):
            t=int(ins.op_str,16)
            if t in pcs and t not in roots:roots.add(t);pending.append(t)
    return sorted(roots)
def spans():
    md=Cs(CS_ARCH_X86,CS_MODE_32);blob=EXE.read_bytes();out=[]
    for a in entries():
        assembly=f'ghidra/phase154-primary-{a:06x}-assembly.txt'
        text=(ROOT/assembly).read_text(encoding='utf8');pcs=[int(p,16) for p in re.findall(r'^([0-9a-fA-F]{8}):',text,re.M)]
        assert pcs and pcs[0]==a,(hex(a),text[:80]);last=next(md.disasm(pe_read(blob,pcs[-1],15),pcs[-1]));n=last.address+last.size-a;note='Original Ghidra function listing and byte extent'
        out.append(dict(address=a,length=n,assembly=assembly,assembly_sha256=sha((ROOT/assembly).read_bytes()),boundary_evidence=note,static_only=a in STATIC,PCs=pcs))
    return out
def build():
    rows=spans();covered=set();blob=EXE.read_bytes()
    for r in rows:
        covered.update(r.pop('PCs'));f=f'ghidra/phase154-primary-{r["address"]:06x}-memory.txt';d=load(f);b=bytes(d['data'])
        assert int(d['address'],16)==r['address'] and len(b)==r['length'] and b==pe_read(blob,r['address'],r['length'])
        r.update(memory=f,memory_sha256=sha((ROOT/f).read_bytes()),bytes_sha256=sha(b))
    assert visited()<=covered,[hex(p) for p in sorted(visited()-covered)[:30]]
    blocks=[];base,image=native.cpu.pe_image(blob)
    for a,n,note in BLOCKS:
        f=f'ghidra/phase154-primary-block-{a:06x}-memory.txt';d=load(f);b=bytes(d['data']);assert b==bytes(image[a-base:a-base+n])
        blocks.append(dict(address=a,length=n,meaning=note,memory=f,memory_sha256=sha((ROOT/f).read_bytes()),bytes_sha256=sha(b)))
    return dict(phase=154,profile=dict(file=str(EXE),sha256=sha(blob)),functions=rows,blocks=blocks,
        unique_executed_original_PCs=len(visited()),all_executed_original_PCs_covered=True,
        static_scope='5992B0 caller and580EC0/584900 registryproducers attested statically only. Complete572820 registry/two-pass/modelcallback path executes with preparedgeometry; no fullrender/GPU/Actor-to-primitivecreation.')

if __name__=='__main__':
    mode=sys.argv[1] if len(sys.argv)>1 else ''
    if mode in ['--queries','--memory']:
        if mode=='--queries':
            qs=[dict(id=f'phase154-primary-{a:06x}-{suffix}',tool=tool,args=dict(program='DKII-DX.exe',address=hex(a)),print_limit=0) for a in entries() for tool,suffix in [('create_function','create'),('disassemble_function','assembly')]];name='phase154-primary-assembly-queries.json'
        else:
            qs=[dict(id=f'phase154-primary-{r["address"]:06x}-memory',tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(r['address']),length=r['length']),print_limit=0) for r in spans()]
            qs += [dict(id=f'phase154-primary-block-{a:06x}-memory',tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(a),length=n),print_limit=0) for a,n,_ in BLOCKS];name='phase154-primary-memory-queries.json'
        (ROOT/name).write_text(json.dumps(qs,indent=2),encoding='utf8');print(name,len(qs))
    else:
        d=build();(ROOT/'phase154-primary-pick-primary.json').write_text(json.dumps(d,indent=2),encoding='utf8');print(len(d['functions']),'extents',d['unique_executed_original_PCs'],'PCs')
