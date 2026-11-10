"""Inspect real vslot getter code before classifying Creature pointer seeds."""
from pathlib import Path
import json,struct
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent
data=(ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe').read_bytes();md=Cs(CS_ARCH_X86,CS_MODE_32);qs=[]
for a in [0x50D390,0x50E3A0]:
    previous=pe_read(data,a-16,16)
    assert previous[-1] in [0x90,0xCC,0xC3] or previous[-3]==0xC2
    print(hex(a),'; '.join(f'{x.mnemonic} {x.op_str}' for x in md.disasm(pe_read(data,a,112),a)))
    qs.extend([dict(id=f'phase73-DX-{a:x}-create',tool='create_function',args=dict(program='DKII-DX.exe',address=hex(a),disassemble_first=True),print_limit=5),
        dict(id=f'phase73-DX-{a:x}-assembly',tool='disassemble_function',args=dict(program='DKII-DX.exe',address=hex(a)),print_limit=5)])
for slot in [0xC0,0x1C8]:qs.append(dict(id=f'phase73-DX-world-slot-{slot:x}',tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(0x66E3EC+slot),length=4),print_limit=5))
(ROOT/'phase73-creature-getter-queries.json').write_text(json.dumps(qs,indent=2),encoding='utf8')
