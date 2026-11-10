"""Whole canonical Ghidra function evidence for the new procedural resource paths."""
from pathlib import Path
import hashlib,json,re,sys
from capstone import Cs,CS_ARCH_X86,CS_MODE_32,CS_OP_IMM
from build_effect_semantics import EXE,pe_read
import emulate_procedural_resources as native

ROOT=Path(__file__).resolve().parent


def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def sha(b):return hashlib.sha256(b).hexdigest()


def visited():
    pcs=set()
    for row in load('phase129-procedural-native-emulations.json')['replays']:
        saved=load(row['file']);assert sha((ROOT/row['file']).read_bytes())==row['sha256']
        pcs.update(int(a,16) for a in saved['result']['visited_instructions'] if 0x400000<=int(a,16)<0x670000)
    return pcs


def entries():
    pcs=visited();blob=EXE.read_bytes();md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True
    roots={0x59A6A0,0x504D80}
    for c in native.cases():
        args,_,_=native.build(c);roots.add(int(args['address'],16))
        roots.update(int(x['address'],16) for x in args['before_calls'])
    for pc in pcs:
        ins=next(md.disasm(pe_read(blob,pc,15),pc))
        if ins.mnemonic=='call' and ins.operands[0].type==CS_OP_IMM and ins.operands[0].imm in pcs:
            roots.add(ins.operands[0].imm)
    return sorted(roots)


def spans():
    blob=EXE.read_bytes();md=Cs(CS_ARCH_X86,CS_MODE_32)
    rows=[]
    for address in entries():
        name=f'ghidra/phase129-primary-{address:06x}-assembly.txt'
        text=(ROOT/name).read_text(encoding='utf8')
        pcs=[int(s,16) for s in re.findall(r'^([0-9A-Fa-f]{8}):',text,re.M)]
        assert pcs and pcs[0]==address,(hex(address),pcs[:1])
        last=next(md.disasm(pe_read(blob,pcs[-1],15),pcs[-1]))
        size=last.address+last.size-address
        capstone_pcs=[i.address for i in md.disasm(pe_read(blob,address,size),address)]
        split=[p for p in capstone_pcs if p not in pcs]
        # Ghidra fuses 9B+FNSTCW into FSTCW; Capstone reports WAIT/FNSTCW.
        assert all(p-1 in pcs and pe_read(blob,p-1,3)[:3]==bytes.fromhex('9bd97d') for p in split),(hex(address),split)
        assert [p for p in capstone_pcs if p not in split]==pcs,hex(address)
        rows.append(dict(address=address,length=size,assembly=name,assembly_sha256=sha((ROOT/name).read_bytes()),
            explicit_FWAIT_split_addresses=split,instruction_addresses=sorted(set(pcs+split))))
    return rows


def build():
    blob=EXE.read_bytes();rows=spans();covered=set()
    for row in rows:
        n=f'ghidra/phase129-primary-{row["address"]:06x}-memory.txt'
        saved=load(n);data=bytes(saved['data'])
        assert int(saved['address'],16)==row['address'] and len(data)==row['length']
        assert data==pe_read(blob,row['address'],row['length'])
        row.update(memory=n,memory_sha256=sha((ROOT/n).read_bytes()),bytes_sha256=sha(data))
        covered.update(row.pop('instruction_addresses'))
    pcs=visited();assert pcs<=covered,[hex(p) for p in sorted(pcs-covered)[:30]]
    blocks=[]
    for a,n,label in [(0x66FE04,64,'original procedural mesh vtable'),(0x670574,0x200,'original graphics interface vtable'),
        (0x504E9C,32,'resource initializer type dispatch'),(0x670690,0x100,'procedural float constants')]:
        path=f'ghidra/phase129-primary-block-{a:06x}.txt';v=load(path);data=bytes(v['data'])
        assert int(v['address'],16)==a and len(data)==n and data==pe_read(blob,a,n)
        blocks.append(dict(address=a,length=n,label=label,memory=path,sha256=sha((ROOT/path).read_bytes()),bytes_sha256=sha(data)))
    return dict(phase=129,profile=dict(executable=str(EXE),sha256=sha(blob)),functions=rows,blocks=blocks,
        unique_executed_original_PCs=len(pcs),all_executed_original_PCs_covered=True,
        scope='Whole canonical function spans; instruction boundary list and every byte checked. Executed paths are recorded, not all-branch coverage.')


if __name__=='__main__':
    if '--queries' in sys.argv:
        qs=[]
        for a in entries():
            qs.append(dict(id=f'phase129-primary-{a:06x}-assembly',tool='disassemble_function',args=dict(program='DKII-DX.exe',address=hex(a)),print_limit=0))
        (ROOT/'phase129-primary-assembly-queries.json').write_text(json.dumps(qs,indent=2),encoding='utf8')
        print('Canonical function queries',len(qs))
    elif '--memory' in sys.argv:
        qs=[dict(id=f'phase129-primary-{r["address"]:06x}-memory',tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(r['address']),length=r['length']),print_limit=0) for r in spans()]
        for a,n in [(0x66FE04,64),(0x670574,0x200),(0x504E9C,32),(0x670690,0x100)]:
            qs.append(dict(id=f'phase129-primary-block-{a:06x}',tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(a),length=n),print_limit=0))
        (ROOT/'phase129-primary-memory-queries.json').write_text(json.dumps(qs,indent=2),encoding='utf8')
        print('Full span/block memory queries',len(qs))
    else:
        v=build();(ROOT/'phase129-primary-manifest.json').write_text(json.dumps(v,indent=2),encoding='utf8')
        print('Attested',len(v['functions']),'whole functions',len(v['blocks']),'blocks',v['unique_executed_original_PCs'],'visited PCs')
