"""Rebuild byte attestation for thirteen Trap path bodies and three tables."""
from pathlib import Path
import json
import hashlib
from build_effect_semantics import EXE,pe_read
from scan_editor_variants import instructions

ROOT=Path(__file__).resolve().parent
SCOPE='13wholekeyfunctionranges and3tables matchedtoGhidra+originalPE;31byteoriginalresourcectorhelper. Not all transitivelyexecutedhelpers/allbranches are newlyattested.'


def digest(b):return hashlib.sha256(b).hexdigest()


def build():
    seed=json.loads((ROOT/'phase119-trap-primary-seed.json').read_text(encoding='utf8'))
    image=EXE.read_bytes();rows=[]
    for key,n in seed['wanted'].items():
        a=int(key);old=seed['inherited'].get(key)
        name=old['memory'] if old else f'ghidra/phase119-trap-primary-{a:x}-read_memory.txt'
        path=ROOT/name;j=json.loads(path.read_text(encoding='utf8'));raw=bytes(j['data'])
        assert len(raw)==n and raw==pe_read(image,a,n)
        rows.append(dict(program='DKII-DX.exe',exe=str(EXE),address=a,length=n,
            memory=str(Path(name)),memory_sha256=digest(path.read_bytes()),original_bytes_sha256=digest(raw),
            fresh_Ghidra=(old is None or a in [0x4B7C10,0x4B7400])))
    blocks=[]
    for a,n,label in [(0x66D754,96,'trap-vtable'),(0x66E548,8,'world-trap-lookups'),(0x670604,4,'native-null-effect-slot')]:
        name=f'ghidra/phase119-block-{label}.txt';path=ROOT/name
        raw=bytes(json.loads(path.read_text(encoding='utf8'))['data'])
        assert len(raw)==n and raw==pe_read(image,a,n)
        blocks.append(dict(program='DKII-DX.exe',address=a,length=n,label=label,
            memory=str(Path(name)),memory_sha256=digest(path.read_bytes()),hex=raw.hex()))
    helper=pe_read(image,0x504D60,31)
    return dict(phase=119,functions=rows,blocks=blocks,
        additional_original_PE_helper=dict(address=0x504D60,length=31,hex=helper.hex(),instructions=instructions(helper,0x504D60)),scope=SCOPE)


if __name__=='__main__':
    result=build();(ROOT/'phase119-primary-manifest.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8')
    print('13keyfunctionranges/3tables/1PEhelper verified;8freshGhidrafunctionranges.')
