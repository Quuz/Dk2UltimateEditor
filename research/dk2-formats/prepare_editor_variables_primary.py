"""Attest exact functions and tables for original Pro numerical Variables loader."""
from pathlib import Path
from functools import cache
import json,re,struct,sys
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from audit_targeted_spell import executable_ranges
from build_effect_semantics import pe_read
import emulate_editor_global_variables as native
import prepare_terrain_text_primary as prior
ROOT=Path(__file__).resolve().parent
sha=native.sha
EXE=native.engine.EXE
FRESH=[0x409250,0x4242C0,0x47D5F0,0x47FFB0,0x481D80,0x49EC50,0x49EC90]
STATIC=[0x47D5F0,0x49EC50]

def load(n):return json.loads((ROOT/n).read_text(encoding='utf-8'))
@cache
def visits():
    ranges=executable_ranges(EXE.read_bytes())
    return {int(pc,16) for row in load('phase166-editor-variables-emulations.json')['replays']
            for pc in load(row['file'])['result']['visited_instructions'] if any(a<=int(pc,16)<a+n for a,n in ranges)}

def fresh_rows():
    md=Cs(CS_ARCH_X86,CS_MODE_32);blob=EXE.read_bytes();out=[]
    for entry in FRESH:
        file=f'ghidra/phase166-primary-Pro-{entry:06x}-assembly.txt';txt=(ROOT/file).read_text(encoding='utf-8')
        pcs=[int(v,16) for v in re.findall(r'^([0-9a-fA-F]{8}):',txt,re.M)];assert pcs[0]==entry
        last=next(md.disasm(pe_read(blob,pcs[-1],15),pcs[-1]));length=last.address+last.size-entry
        out.append(dict(address=entry,length=length,assembly=file,assembly_sha256=sha((ROOT/file).read_bytes()),PCs=pcs))
    return out

def build():
    blob=EXE.read_bytes();md=Cs(CS_ARCH_X86,CS_MODE_32);covered=set();old=prior.build()
    assert old==load('phase165-terrain-text-primary.json')
    inherited={row['address']:row for row in old['functions']+prior.inherited_rows()};reused=[]
    for row in inherited.values():
        pcs={i.address for i in md.disasm(pe_read(blob,row['address'],row['length']),row['address'])}
        if pcs&visits():covered|=pcs;reused.append(row['address'])
    rows=fresh_rows()
    for row in rows:
        covered.update(row.pop('PCs'));f=f'ghidra/phase166-primary-Pro-{row["address"]:06x}-memory.txt'
        saved=load(f);data=bytes(saved['data']);assert data==pe_read(blob,row['address'],row['length'])
        row.update(memory=f,memory_sha256=sha((ROOT/f).read_bytes()),bytes_sha256=sha(data))
    assert not visits()-covered,[hex(x) for x in sorted(visits()-covered)[:20]]
    blocks=[]
    for a,n,label in [(0x49F380,459,'OriginalVariables switch tables/byte selectors/padding,not oneinstructionstream.'),
                      (0x50AC48,8,'Originalbinary64 1/4096 scale constant.')]:
        f=f'ghidra/phase166-primary-Pro-block-{a:06x}-memory.txt';saved=load(f);data=bytes(saved['data'])
        assert data==pe_read(blob,a,n)
        blocks.append(dict(address=a,length=n,memory=f,memory_sha256=sha((ROOT/f).read_bytes()),bytes_sha256=sha(data),scope=label))
    proofs=[]
    for pc,target in [(0x47D6E4,0x49EC50),(0x47D6F1,0x49EC90)]:
        b=pe_read(blob,pc,11);assert b[:3]==bytes.fromhex('c78424') and int.from_bytes(b[-4:],'little')==target
        proofs.append(dict(original_registration_PC=pc,callback=target,bytes_hex=b.hex(),registration_executed=False))
    assert struct.unpack('<d',pe_read(blob,0x50AC48,8))[0]==1/4096
    return dict(profile=dict(file=str(EXE),sha256=sha(blob)),functions=rows,static_only_functions=STATIC,
        inherited=dict(file='phase165-terrain-text-primary.json',sha256=sha((ROOT/'phase165-terrain-text-primary.json').read_bytes()),reused_functions=sorted(reused)),
        blocks=blocks,callback_entry_proofs=proofs,unique_executed_original_PCs=len(visits()),all_executed_original_PCs_covered=True)

def main():
    mode=sys.argv[1] if len(sys.argv)>1 else ''
    if mode=='--queries':
        rows=[]
        for a in FRESH:
            # All fresh entries are canonical functions, actual direct CALLs,
            # or explicit registered callback addresses attested before create.
            rows.extend(dict(id=f'phase166-primary-Pro-{a:06x}-{suffix}',tool=tool,args=dict(program='DK2 Editor pro.exe',address=hex(a)),print_limit=0)
                        for tool,suffix in [('create_function','create'),('disassemble_function','assembly')])
        name='phase166-primary-assembly-queries.json'
    elif mode=='--memory':
        rows=[dict(id=f'phase166-primary-Pro-{r["address"]:06x}-memory',tool='read_memory',
                   args=dict(program='DK2 Editor pro.exe',address=hex(r['address']),length=r['length']),print_limit=0) for r in fresh_rows()]
        rows.extend(dict(id=f'phase166-primary-Pro-block-{a:06x}-memory',tool='read_memory',
                         args=dict(program='DK2 Editor pro.exe',address=hex(a),length=n),print_limit=0) for a,n in [(0x49F380,459),(0x50AC48,8)])
        name='phase166-primary-memory-queries.json'
    else:
        d=build();(ROOT/'phase166-editor-variables-primary.json').write_text(json.dumps(d,indent=2),encoding='utf-8')
        print('verified',len(d['functions']),'fresh',len(d['inherited']['reused_functions']),'reused',d['unique_executed_original_PCs'],'PCs');return
    (ROOT/name).write_text(json.dumps(rows,indent=2),encoding='utf-8');print(name,len(rows))

if __name__=='__main__':main()
