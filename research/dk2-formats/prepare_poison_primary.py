"""Original PE poison/collision instructions and inherited Ghidra byte evidence.

No Ghidra request is sent while automatic approval review is unavailable.
Symbol-map extents only locate ranges; exact instruction bytes are preserved.
"""
import hashlib
import json
from pathlib import Path
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import EXE,pe_read

ROOT=Path(__file__).resolve().parent


def build():
    data=EXE.read_bytes();md=Cs(CS_ARCH_X86,CS_MODE_32);ranges=[]
    for a,n in [(0x451570,676),(0x48CC50,27),(0x4ABCA0,177),(0x4AC300,33),(0x4AD1B0,1394),
                (0x4AD730,110),(0x4B0270,105),(0x4B32A0,579),(0x4B5590,259),(0x4B5800,217),(0x4BF390,54)]:
        raw=pe_read(data,a,n);ins=list(md.disasm(raw,a))
        assert ins[-1].address+ins[-1].size==a+n
        ranges.append(dict(address=a,length=n,hex=raw.hex(),sha256=hashlib.sha256(raw).hexdigest(),
            instructions=[dict(address=i.address,bytes=i.bytes.hex(),text=i.mnemonic+' '+i.op_str) for i in ins]))
    saved=[]
    groups=[('phase107-primary-manifest.json',[0x488930,0x48E160,0x59C140,0x594E10,0x575D70]),
            ('phase96-primary-manifest.json',[0x56A490,0x56A560,0x4F7860]),
            ('phase80-primary-manifest.json',[0x4984D0,0x4B7CD0,0x4AAC90]),
            ('phase113-primary-manifest.json',[0x4B2CE0])]
    for manifest,wanted in groups:
        o=json.loads((ROOT/manifest).read_text(encoding='utf8'))
        for r in o['functions']:
            if r['address'] not in wanted:continue
            assert r.get('program','DKII-DX.exe')=='DKII-DX.exe'
            filename=r['memory'];p=ROOT/'ghidra'/filename;v=json.loads(p.read_text(encoding='utf8'));raw=bytes(v['data'])
            assert raw==pe_read(data,r['address'],r['length'])
            saved.append(dict(address=r['address'],length=r['length'],file=str(p.relative_to(ROOT)),
                              evidence_sha256=hashlib.sha256(p.read_bytes()).hexdigest(),program_bytes_sha256=hashlib.sha256(raw).hexdigest()))
    return dict(executable=str(EXE),sha256=hashlib.sha256(data).hexdigest(),new_original_PE_ranges=ranges,
                inherited_Ghidra_ranges=saved,independent_Ghidra_CPU_controls=0,
                primary_stack_contract=dict(process_pushes_at=[0x4B02C0,0x4B02C1],
                    first_stack_argument='Data2 low16; read by48CC55 fromESP+4 -> DX -> Creature45F.',
                    second_stack_argument='Data1 low16; read by48CC50 fromESP+8 -> AX -> Creature45D.'),
                inherited_phase114='Six Ghidra instruction ranges for rawShot loading/factory setup remain in phase114-primary-manifest.json unchanged.')


if __name__=='__main__':
    v=build();(ROOT/'phase115-primary-manifest.json').write_text(json.dumps(v,indent=2),encoding='utf8')
    print(len(v['new_original_PE_ranges']),'original PE ranges,',len(v['inherited_Ghidra_ranges']),'inherited Ghidra byte ranges')
