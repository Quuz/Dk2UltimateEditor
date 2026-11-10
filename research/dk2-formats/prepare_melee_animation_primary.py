"""Native original byte/instruction coverage, bit29 diagnostic string and formatter."""
from pathlib import Path
from functools import cache
import json,re,struct,sys
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import EXE,pe_read
from audit_targeted_spell import executable_ranges
import emulate_melee_animation_flag as native
ROOT=Path(__file__).resolve().parent
sha=native.sha
BLOCKS=[(0x66D524,0x130,'Original Creature vtable'),(0x670574,0x200,'Original CPCEngineInterface vtable'),
        (0x66E3EC,0x3A0,'Original CWorld vtable'),(0x69D1C0,32,'Native PlayInstanceAnimation diagnostic format string')]
STATIC=[0x490240]
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
@cache
def traces():
    out=[]
    for row in load('phase147-melee-animation-emulations.json')['replays']:
        p=ROOT/row['file'];assert sha(p.read_bytes())==row['sha256'];out.append(load(row['file']))
    return out
@cache
def visited():
    ranges=executable_ranges(EXE.read_bytes());return {int(pc,16) for d in traces() for pc in d['result']['visited_instructions'] if any(a<=int(pc,16)<a+n for a,n in ranges)}
def entries():
    blob=EXE.read_bytes();md=Cs(CS_ARCH_X86,CS_MODE_32);pcs=visited();roots=set(STATIC)
    for d in traces():roots.add(int(d['input']['address'],16));roots.update(int(c['address'],16) for c in d['input']['before_calls'])
    for pc in pcs:
        ins=next(md.disasm(pe_read(blob,pc,15),pc))
        if ins.mnemonic in ['call','push'] and ins.op_str.startswith('0x') and int(ins.op_str,16) in pcs:roots.add(int(ins.op_str,16))
    for a,n,meaning in BLOCKS:
        if 'vtable' in meaning.lower():roots.update(p for p in struct.unpack('<'+str(n//4)+'I',pe_read(blob,a,n)) if p in pcs)
    return sorted(roots)
def spans():
    md=Cs(CS_ARCH_X86,CS_MODE_32);blob=EXE.read_bytes();out=[]
    for a in entries():
        file=f'ghidra/phase147-primary-{a:06x}-assembly.txt';text=(ROOT/file).read_text(encoding='utf8');pcs=[int(x,16) for x in re.findall(r'^([0-9a-fA-F]{8}):',text,re.M)]
        assert pcs and pcs[0]==a,(hex(a),text[:100]);last=next(md.disasm(pe_read(blob,pcs[-1],15),pcs[-1]));n=last.address+last.size-a
        decoded=[];gaps=[]
        for j,pc in enumerate(pcs):
            ins=next(md.disasm(pe_read(blob,pc,15),pc));decoded.append(pc);end=pc+ins.size
            if ins.mnemonic=='wait' and j+1<len(pcs) and pcs[j+1]>end:
                follow=next(md.disasm(pe_read(blob,end,15),end))
                if follow.mnemonic.startswith('fn'):decoded.append(end);end+=follow.size
            nxt=pcs[j+1] if j+1<len(pcs) else a+n;assert end<=nxt,(hex(a),hex(pc))
            if end<nxt:gaps.append(dict(address=end,length=nxt-end))
        out.append(dict(address=a,length=n,assembly=file,assembly_sha256=sha((ROOT/file).read_bytes()),static_only=a in STATIC,unlisted_intervals=gaps,PCs=decoded))
    return out
def build():
    blob=EXE.read_bytes();rows=spans();covered=set()
    for row in rows:
        covered.update(row.pop('PCs'));file=f'ghidra/phase147-primary-{row["address"]:06x}-memory.txt';d=load(file);data=bytes(d['data'])
        assert int(d['address'],16)==row['address'] and len(data)==row['length'] and data==pe_read(blob,row['address'],row['length'])
        row.update(memory=file,memory_sha256=sha((ROOT/file).read_bytes()),bytes_sha256=sha(data))
    assert visited()<=covered,[hex(pc) for pc in sorted(visited()-covered)[:25]]
    blocks=[]
    for a,n,meaning in BLOCKS:
        file=f'ghidra/phase147-primary-block-{a:06x}-memory.txt';d=load(file);data=bytes(d['data']);assert data==pe_read(blob,a,n)
        blocks.append(dict(address=a,length=n,meaning=meaning,memory=file,memory_sha256=sha((ROOT/file).read_bytes()),bytes_sha256=sha(data)))
    assert pe_read(blob,0x69D1C0,32).split(b'\0')[0]==b'    PlayInstanceAnimation %d\n'
    return dict(phase=147,profile=dict(file=str(EXE),sha256=sha(blob)),functions=rows,blocks=blocks,unique_executed_original_PCs=len(visited()),all_observed_original_PCs_covered=True,
        native_flag_name=dict(formatter=0x490240,read=0x490A11,shift=0x490A1A,mask=0x490A1D,string_push=0x490A21,string=0x69D1C0,source='ActorDWORD3EA bit12, not a direct rawfield string label.'))
if __name__=='__main__':
    mode=sys.argv[1] if len(sys.argv)>1 else ''
    if mode in ['--queries','--memory']:
        if mode=='--queries':qs=[dict(id=f'phase147-primary-{a:06x}-{suffix}',tool=tool,args=dict(program='DKII-DX.exe',address=hex(a)),print_limit=0) for a in entries() for tool,suffix in [('create_function','create'),('disassemble_function','assembly')]];name='phase147-primary-assembly-queries.json'
        else:
            qs=[dict(id=f'phase147-primary-{r["address"]:06x}-memory',tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(r['address']),length=r['length']),print_limit=0) for r in spans()]
            qs += [dict(id=f'phase147-primary-block-{a:06x}-memory',tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(a),length=n),print_limit=0) for a,n,_ in BLOCKS];name='phase147-primary-memory-queries.json'
        (ROOT/name).write_text(json.dumps(qs,indent=2),encoding='utf8');print(name,len(qs))
    else:
        d=build();(ROOT/'phase147-melee-animation-primary.json').write_text(json.dumps(d,indent=2),encoding='utf8');print(len(d['functions']),'function extents',d['unique_executed_original_PCs'],'PCs')
