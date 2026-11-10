"""Find the old complete kind/owner filter by exact relocated bytes."""
from pathlib import Path
import json
from capstone import Cs, CS_ARCH_X86, CS_MODE_32
from capstone.x86 import X86_OP_MEM
from compare_rc3_converters import code_region
from build_effect_semantics import pe_read

ROOT = Path(__file__).resolve().parent


def main():
    dx = (ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe').read_bytes()
    old = (ROOT/'source-binaries/DKII-local13.exe').read_bytes()
    md = Cs(CS_ARCH_X86, CS_MODE_32)
    md.detail = True
    current = pe_read(dx,0x453700,91)
    expected = bytearray(current)
    relocations = []
    for x in md.disasm(current,0x453700):
        if any(o.type==X86_OP_MEM and o.mem.disp==0x6EE540 for o in x.operands):
            at = x.address-0x453700+x.disp_offset
            assert x.disp_size == 4
            expected[at:at+4] = (0x6D95C8).to_bytes(4,'little')
            relocations.append(dict(offset=at,bytes=4,DX=0x6EE540,old=0x6D95C8))
    assert len(relocations) == 2
    base,code = code_region(old)
    matches = []
    at = code.find(expected)
    while at >= 0:
        matches.append(base+at)
        at = code.find(expected,at+1)
    assert len(matches) == 1,matches
    start = matches[0]
    previous = pe_read(old,start-16,16)
    assert previous[-1] == 0x90
    match = dict(DX=0x453700,old=start,length=91,relocations=relocations,
                 scope='Whole91 bytes, including final RET imm16 operands, match after replacing only two verified global tag-table displacements;old boundary also ends in NOP padding.')
    (ROOT/'phase69-kind-filter-match.json').write_text(json.dumps(match,indent=2),encoding='utf8')
    queries = [
        dict(id=f'phase69-old-{start:x}-create',tool='create_function',args=dict(program='DKII-local13.exe',address=hex(start),disassemble_first=True),print_limit=30),
        dict(id=f'phase69-old-{start:x}-assembly',tool='disassemble_function',args=dict(program='DKII-local13.exe',address=hex(start)),print_limit=30),
        dict(id='phase69-DX-513730-create',tool='create_function',args=dict(program='DKII-DX.exe',address='0x513730',disassemble_first=True),print_limit=30),
        dict(id='phase69-DX-513730-assembly',tool='disassemble_function',args=dict(program='DKII-DX.exe',address='0x513730'),print_limit=30),
        dict(id='phase69-DX-kind-switch',tool='read_memory',args=dict(program='DKII-DX.exe',address='0x4a0e14',length=20),print_limit=30),
    ]
    (ROOT/'phase69-followup-queries.json').write_text(json.dumps(queries,indent=2),encoding='utf8')
    print('Old full kind/owner filter',hex(start),'91 bytes;two exact table relocations')


if __name__=='__main__':
    main()
