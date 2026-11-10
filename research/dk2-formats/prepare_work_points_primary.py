"""Original work contribution instructions plus earlier Ghidra byte evidence."""
import hashlib
import json
from pathlib import Path
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import EXE,pe_read

ROOT=Path(__file__).resolve().parent


def build():
    data=EXE.read_bytes();md=Cs(CS_ARCH_X86,CS_MODE_32);rows=[]
    for a,n in [(0x47B000,145),(0x47B190,150),(0x4C2DC0,569),(0x4C3200,319),(0x506B50,29)]:
        raw=pe_read(data,a,n);ins=list(md.disasm(raw,a));assert ins[-1].address+ins[-1].size==a+n
        rows.append(dict(address=a,length=n,hex=raw.hex(),sha256=hashlib.sha256(raw).hexdigest(),
            instructions=[dict(address=i.address,hex=i.bytes.hex(),text=i.mnemonic+' '+i.op_str) for i in ins]))
    inherited=[]
    for manifest,wanted in [('phase113-primary-manifest.json',[0x491DC0,0x568820,0x568E30,0x492320,0x51DC40]),
                            ('phase107-primary-manifest.json',[0x488930,0x4D8E60])]:
        for r in json.loads((ROOT/manifest).read_text(encoding='utf8'))['functions']:
            if r['address'] not in wanted:continue
            p=ROOT/'ghidra'/r['memory'];o=json.loads(p.read_text(encoding='utf8'));raw=bytes(o['data'])
            assert raw==pe_read(data,r['address'],r['length'])
            inherited.append(dict(address=r['address'],length=r['length'],file=str(p.relative_to(ROOT)),
                                  evidence_sha256=hashlib.sha256(p.read_bytes()).hexdigest(),program_sha256=hashlib.sha256(raw).hexdigest()))
    assert len(inherited)==7
    return dict(executable=str(EXE),sha256=hashlib.sha256(data).hexdigest(),original_PE_ranges=rows,
                inherited_Ghidra_ranges=inherited,independent_Ghidra_CPU_controls=0,
                scope='Original byte ranges and earlier Ghidra results; no new Ghidra request while prior automatic approval review remains unavailable.')


if __name__=='__main__':
    v=build();(ROOT/'phase116-primary-manifest.json').write_text(json.dumps(v,indent=2),encoding='utf8')
    print(len(v['original_PE_ranges']),'original PE ranges;',len(v['inherited_Ghidra_ranges']),'Ghidra byte ranges')
