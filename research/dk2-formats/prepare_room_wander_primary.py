"""Extend phase144 whole-function attestation only where new room-branch PCs require it."""
from pathlib import Path
import json,re,struct,sys
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import EXE,pe_read
import emulate_room_wander as native
ROOT=Path(__file__).resolve().parent
BLOCKS=[(0x480EC4,0x68,'Room-type count and retry switch/index tables'),
        (0x6A11D0,4,'Original NAV query callback pointer to canonical4C8BD0')]
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
sha=native.sha
def traces():
    out=[]
    for row in load('phase145-room-wander-emulations.json')['replays']:
        p=ROOT/row['file'];assert sha(p.read_bytes())==row['sha256'];out.append(load(row['file']))
    return out
def visited():return {int(pc,16) for s in traces() for pc in s['result']['visited_instructions']}
def prior():
    v=load('phase144-wander-target-path-primary.json');blob=EXE.read_bytes()
    assert sha(blob)==v['profile']['sha256']
    for row in v['functions']+v['blocks']:
        data=bytes(load(row['memory'])['data']);assert data==pe_read(blob,row['address'],row['length'])
        assert sha(data)==row['bytes_sha256'] and sha((ROOT/row['memory']).read_bytes())==row['memory_sha256']
        if 'assembly' in row:assert sha((ROOT/row['assembly']).read_bytes())==row['assembly_sha256']
    return v
def entries():
    old={r['address'] for r in prior()['functions']};blob=EXE.read_bytes();md=Cs(CS_ARCH_X86,CS_MODE_32);pcs=visited();roots=set()
    for s in traces():
        roots.add(int(s['input']['address'],16));roots.update(int(x['address'],16) for x in s['input'].get('before_calls',[]))
    for pc in pcs:
        ins=next(md.disasm(pe_read(blob,pc,15),pc))
        if ins.mnemonic=='call' and ins.op_str.startswith('0x'):roots.add(int(ins.op_str,16))
    for a,n in [(0x66E3EC,0x3A0),(0x66D524,0x130)]:roots.update(p for p in struct.unpack('<'+str(n//4)+'I',pe_read(blob,a,n)) if p in pcs)
    nav_callback=struct.unpack('<I',pe_read(blob,0x6A11D0,4))[0]
    assert nav_callback==0x4C8BD0
    if nav_callback in pcs:roots.add(nav_callback)
    return sorted(roots-old)
def spans():
    md=Cs(CS_ARCH_X86,CS_MODE_32);blob=EXE.read_bytes();rows=[]
    for a in entries():
        name=f'ghidra/phase145-primary-{a:06x}-assembly.txt';text=(ROOT/name).read_text(encoding='utf8');pcs=[int(s,16) for s in re.findall(r'^([0-9A-Fa-f]{8}):',text,re.M)]
        assert pcs and pcs[0]==a,(hex(a),text[:100]);last=next(md.disasm(pe_read(blob,pcs[-1],15),pcs[-1]));n=last.address+last.size-a
        cap=[i.address for i in md.disasm(pe_read(blob,a,n),a)];split=[p for p in cap if p not in pcs]
        assert all(p-1 in pcs and pe_read(blob,p-1,3)==bytes.fromhex('9bd97d') for p in split)
        assert [p for p in cap if p not in split]==pcs,hex(a)
        rows.append(dict(address=a,length=n,assembly=name,assembly_sha256=sha((ROOT/name).read_bytes()),instruction_addresses=cap))
    return rows
def build():
    old=prior();blob=EXE.read_bytes();md=Cs(CS_ARCH_X86,CS_MODE_32);covered=set();reused=[]
    for row in old['functions']:
        pcs={i.address for i in md.disasm(pe_read(blob,row['address'],row['length']),row['address'])}
        if pcs&visited():covered|=pcs;reused.append(row['address'])
    rows=spans()
    for row in rows:
        covered.update(row.pop('instruction_addresses'));n=f'ghidra/phase145-primary-{row["address"]:06x}-memory.txt';data=bytes(load(n)['data'])
        assert data==pe_read(blob,row['address'],row['length']);row.update(memory=n,memory_sha256=sha((ROOT/n).read_bytes()),bytes_sha256=sha(data))
    assert visited()<=covered,[hex(p) for p in sorted(visited()-covered)[:20]]
    blocks=[]
    for a,n,meaning in BLOCKS:
        file=f'ghidra/phase145-primary-block-{a:06x}-memory.txt';d=load(file);data=bytes(d['data'])
        assert int(d['address'],16)==a and len(data)==n and data==pe_read(blob,a,n)
        blocks.append(dict(address=a,length=n,meaning=meaning,memory=file,memory_sha256=sha((ROOT/file).read_bytes()),bytes_sha256=sha(data)))
    return dict(phase=145,profile=old['profile'],inherited_attestation=dict(file='phase144-wander-target-path-primary.json',sha256=sha((ROOT/'phase144-wander-target-path-primary.json').read_bytes()),reused_function_addresses=reused),
        functions=rows,blocks=blocks,unique_executed_original_PCs=len(visited()),all_executed_original_PCs_covered=True,source_code_substitutions=0)
if __name__=='__main__':
    mode=sys.argv[1] if len(sys.argv)>1 else ''
    if mode in ['--queries','--memory']:
        if mode=='--queries':
            qs=[dict(id=f'phase145-primary-{a:06x}-{suffix}',tool=tool,args=dict(program='DKII-DX.exe',address=hex(a)),print_limit=0) for a in entries() for tool,suffix in [('create_function','create'),('disassemble_function','assembly')]]
            name='phase145-room-primary-assembly-queries.json'
        else:
            qs=[dict(id=f'phase145-primary-{r["address"]:06x}-memory',tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(r['address']),length=r['length']),print_limit=0) for r in spans()]
            qs += [dict(id=f'phase145-primary-block-{a:06x}-memory',tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(a),length=n),print_limit=0) for a,n,_ in BLOCKS]
            name='phase145-room-primary-memory-queries.json'
        (ROOT/name).write_text(json.dumps(qs,indent=2),encoding='utf8');print(name,len(qs))
    else:
        v=build();(ROOT/'phase145-room-wander-primary.json').write_text(json.dumps(v,indent=2),encoding='utf8');print('Attested',len(v['functions']),'new spans;',len(v['inherited_attestation']['reused_function_addresses']),'inherited;',v['unique_executed_original_PCs'],'PCs')
