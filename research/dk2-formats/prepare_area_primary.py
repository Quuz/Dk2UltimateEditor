"""Fresh whole-function Ghidra/PE attestation of actual AoE trace callees."""
from pathlib import Path
import hashlib,json,re,sys
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import EXE,pe_read
import emulate_area_damage as native

ROOT=Path(__file__).resolve().parent
BLOCKS=[(0x65FB14,64,'integer square-root BSR seed table'),
    (0x66D86C,0x100,'original Shot vtable'),(0x66D524,0x130,'original Creature vtable'),
    (0x66E3EC,0x3A0,'original World vtable'),(0x670574,0x200,'original graphics vtable'),
    (0x69E834,96,'area handler diagnostic formats')]

def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def sha(b):return hashlib.sha256(b).hexdigest()


def traces():
    out=[]
    for row in load('phase131-area-native-emulations.json')['replays']:
        saved=load(row['file']);assert sha((ROOT/row['file']).read_bytes())==row['sha256']
        out.append(saved)
    return out


def visited():
    return {int(a,16) for saved in traces() for a in saved['result']['visited_instructions'] if 0x400000<=int(a,16)<0x670000}


def entries():
    # Four dispatch targets are reached through real tail JMPs, not CALLs.
    roots={0x4AC5A0,0x4AC6D0,0x4AC940,0x4ACAF0}
    for saved in traces():
        roots.update(target for caller,target in saved['result']['observed_call_edges'] if 0x400000<=target<0x670000)
        roots.add(int(saved['input']['address'],16))
        roots.update(int(c['address'],16) for c in saved['input']['before_calls'])
    return sorted(roots)


def spans():
    blob=EXE.read_bytes();md=Cs(CS_ARCH_X86,CS_MODE_32);rows=[]
    for address in entries():
        name=f'ghidra/phase131-primary-{address:06x}-assembly.txt'
        text=(ROOT/name).read_text(encoding='utf8')
        pcs=[int(s,16) for s in re.findall(r'^([0-9A-Fa-f]{8}):',text,re.M)]
        assert pcs and pcs[0]==address,(hex(address),pcs[:1],text[:160])
        last=next(md.disasm(pe_read(blob,pcs[-1],15),pcs[-1]));size=last.address+last.size-address
        capstone_pcs=[i.address for i in md.disasm(pe_read(blob,address,size),address)]
        split=[p for p in capstone_pcs if p not in pcs]
        assert all(p-1 in pcs and pe_read(blob,p-1,3)==bytes.fromhex('9bd97d') for p in split),(hex(address),split)
        assert [p for p in capstone_pcs if p not in split]==pcs,hex(address)
        rows.append(dict(address=address,length=size,assembly=name,assembly_sha256=sha((ROOT/name).read_bytes()),
            explicit_FWAIT_split_addresses=split,instruction_addresses=sorted(set(pcs+split))))
    return rows


def build():
    blob=EXE.read_bytes();rows=spans();covered=set()
    for row in rows:
        name=f'ghidra/phase131-primary-{row["address"]:06x}-memory.txt';saved=load(name);data=bytes(saved['data'])
        assert int(saved['address'],16)==row['address'] and len(data)==row['length'] and data==pe_read(blob,row['address'],row['length'])
        row.update(memory=name,memory_sha256=sha((ROOT/name).read_bytes()),bytes_sha256=sha(data))
        covered.update(row.pop('instruction_addresses'))
    pcs=visited();assert pcs<=covered,[hex(p) for p in sorted(pcs-covered)[:40]]
    blocks=[]
    for a,n,label in BLOCKS:
        name=f'ghidra/phase131-primary-block-{a:06x}.txt';saved=load(name);data=bytes(saved['data'])
        assert int(saved['address'],16)==a and len(data)==n and data==pe_read(blob,a,n)
        blocks.append(dict(address=a,length=n,label=label,memory=name,sha256=sha((ROOT/name).read_bytes()),bytes_sha256=sha(data)))
    return dict(phase=131,profile=dict(executable=str(EXE),sha256=sha(blob)),functions=rows,blocks=blocks,
        unique_executed_original_PCs=len(pcs),all_executed_original_PCs_covered=True,
        scope='Actual call-edge targets plus explicit roots, whole canonical function spans and every byte. Instruction lists agree with Capstone, except explicitly recorded fused FWAIT. Not all-branch or whole-game coverage.')


if __name__=='__main__':
    if '--queries' in sys.argv:
        qs=[dict(id=f'phase131-primary-{a:06x}-assembly',tool='disassemble_function',args=dict(program='DKII-DX.exe',address=hex(a)),print_limit=0) for a in entries()]
        (ROOT/'phase131-primary-assembly-queries.json').write_text(json.dumps(qs,indent=2),encoding='utf8')
        print('Actual root/callee function queries',len(qs))
    elif '--memory' in sys.argv:
        qs=[dict(id=f'phase131-primary-{r["address"]:06x}-memory',tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(r['address']),length=r['length']),print_limit=0) for r in spans()]
        qs += [dict(id=f'phase131-primary-block-{a:06x}',tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(a),length=n),print_limit=0) for a,n,_ in BLOCKS]
        (ROOT/'phase131-primary-memory-queries.json').write_text(json.dumps(qs,indent=2),encoding='utf8');print('Memory queries',len(qs))
    else:
        value=build();(ROOT/'phase131-area-primary.json').write_text(json.dumps(value,indent=2),encoding='utf8')
        print('Attested',len(value['functions']),'functions',len(value['blocks']),'blocks',value['unique_executed_original_PCs'],'PCs')
