"""Original function bytes and exact instruction coverage for collision pairs."""
from pathlib import Path
from functools import cache
import json,re,sys
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
import emulate_shot_area_combinations as native
from audit_targeted_spell import executable_ranges
ROOT=Path(__file__).resolve().parent
sha=native.sha
INHERITED=['phase168-shot-combinations-primary.json','phase131-area-primary.json','phase148-shot-lifecycle-primary.json','phase107-primary-manifest.json',
    'phase96-primary-manifest.json','phase80-primary-manifest.json','phase113-primary-manifest.json',
    'phase151-tremor-primary.json','phase164-creature-work-initial-primary.json',
    'phase162-creature-work-level-primary.json','phase147-melee-animation-primary.json']
BLOCKS=[(0x66D524,0x130),(0x66D86C,0x100),(0x66E3EC,0x3A0),(0x670574,0x200)]
load=lambda n:json.loads((ROOT/n).read_text(encoding='utf-8'))

@cache
def traces():
    rows=load('phase169-area-combinations-emulations.json')['replays'];out=[]
    for row in rows:
        assert sha((ROOT/row['file']).read_bytes())==row['sha256']
        out.append(load(row['file']))
    return out

@cache
def visited():
    ranges=executable_ranges(native.EXE.read_bytes())
    return {int(pc,16) for d in traces() for pc in d['result']['visited_instructions'] if any(a<=int(pc,16)<a+n for a,n in ranges)}

def path(n):return n if n.startswith('ghidra/') or n.startswith('ghidra\\') else 'ghidra/'+n

def instructions(row):
    md=Cs(CS_ARCH_X86,CS_MODE_32);blob=native.EXE.read_bytes()
    if row.get('assembly'):
        txt=(ROOT/path(row['assembly'])).read_text(encoding='utf-8')
        pcs=[int(v,16) for v in re.findall(r'^([0-9a-fA-F]{8}):',txt,re.M)]
        assert pcs and pcs[0]==row['address'],row
        out=set(pcs)
        for pc in pcs:
            ins=next(md.disasm(native.p.p.area.pe_read(blob,pc,15),pc))
            if ins.mnemonic=='wait':
                follow=next(md.disasm(native.p.p.area.pe_read(blob,pc+1,15),pc+1))
                if follow.mnemonic.startswith('fn'):out.add(pc+1)
        return out
    return {i.address for i in md.disasm(native.p.p.area.pe_read(blob,row['address'],row['length']),row['address'])}

@cache
def inherited_rows():
    blob=native.EXE.read_bytes();out={}
    for manifest in INHERITED:
        for x in load(manifest)['functions']:
            if x.get('program','DKII-DX.exe')!='DKII-DX.exe':continue
            if x['address'] in out:continue
            f=path(x['memory']);raw=bytes(load(f)['data'])
            assert len(raw)==x['length'] and raw==native.p.p.area.pe_read(blob,x['address'],x['length']),f
            row=dict(x,memory=f,source_manifest=manifest,memory_sha256=sha((ROOT/f).read_bytes()),bytes_sha256=sha(raw))
            if x.get('assembly'):row['assembly']=path(x['assembly'])
            out[x['address']]=row
    return list(out.values())

def inherited_coverage():return set().union(*(instructions(x) for x in inherited_rows()))

def fresh_entries():
    covered=inherited_coverage();roots=set()
    for d in traces():
        roots.add(int(d['input']['address'],16));roots.update(int(x['address'],16) for x in d['input']['before_calls'])
        # Captured at actual original CALL instructions, with their actual
        # destination. These are entry proofs, never arbitrary interior PCs.
        roots.update(target for source,target in d['result']['observed_call_edges'] if source in visited() and target in visited())
    # Executed native48F380 adjustsECX then tail-jumps to canonical477B40.
    # Ghidra independently reports477B40 as the function entry, not an
    # arbitrary conditional/intra-function branch target.
    b=native.p.p.area.pe_read(native.EXE.read_bytes(),0x48F386,5)
    assert b[0]==0xE9 and 0x48F38B+int.from_bytes(b[1:],'little',signed=True)==0x477B40
    if 0x48F386 in visited():roots.add(0x477B40)
    return sorted((roots&visited())-covered)

def fresh_rows():
    md=Cs(CS_ARCH_X86,CS_MODE_32);out=[];blob=native.EXE.read_bytes()
    for a in fresh_entries():
        f=f'ghidra/phase169-primary-{a:06x}-assembly.txt';txt=(ROOT/f).read_text(encoding='utf-8')
        pcs=[int(v,16) for v in re.findall(r'^([0-9a-fA-F]{8}):',txt,re.M)];assert pcs[0]==a
        last=next(md.disasm(native.p.p.area.pe_read(blob,pcs[-1],15),pcs[-1]))
        out.append(dict(address=a,length=last.address+last.size-a,assembly=f,assembly_sha256=sha((ROOT/f).read_bytes())))
    return out

def build():
    covered=set();inherited=[];blob=native.EXE.read_bytes()
    for row in inherited_rows():
        pcs=instructions(row)
        if pcs&visited():covered|=pcs;inherited.append(row)
    fresh=fresh_rows()
    for row in fresh:
        f=f'ghidra/phase169-primary-{row["address"]:06x}-memory.txt';data=bytes(load(f)['data'])
        assert data==native.p.p.area.pe_read(blob,row['address'],row['length'])
        row.update(memory=f,memory_sha256=sha((ROOT/f).read_bytes()),bytes_sha256=sha(data))
        covered|=instructions(row)
    assert visited()<=covered,[hex(p) for p in sorted(visited()-covered)[:30]]
    blocks=[]
    for a,n in BLOCKS:
        f=f'ghidra/phase169-primary-block-{a:06x}-memory.txt';data=bytes(load(f)['data'])
        assert data==native.p.p.area.pe_read(blob,a,n)
        blocks.append(dict(address=a,length=n,memory=f,memory_sha256=sha((ROOT/f).read_bytes()),bytes_sha256=sha(data)))
    # Critical original order: callback return is not tested at collision.
    expected={0x4ABFBD:'6a04',0x4ABFC1:'e86a170000',0x4ABFCC:'80786b07',0x4ABFDE:'e83d040000'}
    for a,h in expected.items():assert native.p.p.area.pe_read(blob,a,len(bytes.fromhex(h))).hex()==h
    for a,h in {0x4AB27D:'6a40',0x4AB286:'c7864901000001000000',0x4AB30B:'6a02',0x4AB314:'85c07508'}.items():
        assert native.p.p.area.pe_read(blob,a,len(bytes.fromhex(h))).hex()==h
    return dict(profile=dict(file=str(native.EXE),sha256=sha(blob)),functions=fresh,inherited=inherited,blocks=blocks,
        unique_executed_original_PCs=len(visited()),all_executed_original_PCs_covered=True,
        area_tick_order=dict(start_call=0x4AB281,start_return_ignored=True,tick_call=0x4AB30F,tick_return_test=0x4AB314,normal_dispatch=0x4AB329),
        collision_order=dict(event4_call=0x4ABFC1,damage7_test=0x4ABFCC,hit_call=0x4ABFDE,process_return_ignored=True))

def main():
    mode=sys.argv[1] if len(sys.argv)>1 else ''
    if mode=='--queries':
        rows=[dict(id=f'phase169-primary-{a:06x}-{s}',tool=t,args=dict(program='DKII-DX.exe',address=hex(a)),print_limit=0)
              for a in fresh_entries() for t,s in [('create_function','create'),('disassemble_function','assembly')]]
        name='phase169-primary-assembly-queries.json'
    elif mode=='--memory':
        rows=[dict(id=f'phase169-primary-{r["address"]:06x}-memory',tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(r['address']),length=r['length']),print_limit=0) for r in fresh_rows()]
        rows += [dict(id=f'phase169-primary-block-{a:06x}-memory',tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(a),length=n),print_limit=0) for a,n in BLOCKS]
        name='phase169-primary-memory-queries.json'
    else:
        d=build();(ROOT/'phase169-area-combinations-primary.json').write_text(json.dumps(d,indent=2),encoding='utf-8')
        print(len(d['functions']),'fresh',len(d['inherited']),'inherited',d['unique_executed_original_PCs'],'PCs');return
    (ROOT/name).write_text(json.dumps(rows,indent=2),encoding='utf-8');print(name,len(rows))

if __name__=='__main__':main()
