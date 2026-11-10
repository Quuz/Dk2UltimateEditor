"""Verify original key creation/removal/pool instructions and gate vtable slot."""
from pathlib import Path
import hashlib
import json
from build_effect_semantics import EXE,pe_read
import prepare_raw_trap_primary as creation

ROOT=Path(__file__).resolve().parent
def sha(b):return hashlib.sha256(b).hexdigest()


def build():
    image=EXE.read_bytes()
    old=json.loads((ROOT/'phase119-primary-manifest.json').read_text(encoding='utf8'))
    assert old==creation.build()
    fresh=[]
    seed=json.loads((ROOT/'phase120-removal-primary-seed.json').read_text(encoding='utf8'))
    for key,row in seed['wanted'].items():
        address=int(key);length=row['length']
        name=f'ghidra/phase120-removal-{address:x}-read_memory.txt';path=ROOT/name
        saved=json.loads(path.read_text(encoding='utf8'));blob=bytes(saved['data'])
        assert int(saved['address'],16)==address and len(blob)==length and blob==pe_read(image,address,length)
        fresh.append(dict(program='DKII-DX.exe',address=address,length=length,
            symbol_hint_only=row['symbol_hint'],memory=name,memory_sha256=sha(path.read_bytes()),
            original_bytes_sha256=sha(blob)))
    path=ROOT/'ghidra/phase120-world-gate-vslot.txt'
    blob=bytes(json.loads(path.read_text(encoding='utf8'))['data'])
    assert blob==pe_read(image,0x66E438,4) and int.from_bytes(blob,'little')==0x509850
    assert len(old['functions'])==13 and len(fresh)==14
    return dict(phase=120,inherited_creation_functions=old['functions'],fresh_removal_functions=fresh,
        inherited_tables=old['blocks'],World_gate_slot=dict(address=0x66E438,length=4,target=0x509850,
            memory=str(path.relative_to(ROOT)),memory_sha256=sha(path.read_bytes())),
        scope='27keywholefunctionranges including5bytethunk4B2910 andactual160bytetarget4B4AB0. Source-map extentscanincludealignmentNOPs;notalltransitivehelpers/branches are newlyattested.')


if __name__=='__main__':
    value=build();(ROOT/'phase120-primary-manifest.json').write_text(json.dumps(value,ensure_ascii=False,indent=2),encoding='utf8')
    print('27keyfunctionranges/4tables verifiedagainstGhidra andoriginalPE.')
