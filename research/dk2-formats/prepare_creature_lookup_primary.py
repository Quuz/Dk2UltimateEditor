"""Re-read full native Creature getters/key helper after34 emulations."""
from pathlib import Path
import json,re
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent
exe=ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe';md=Cs(CS_ARCH_X86,CS_MODE_32);rows=[];queries=[];blocks=[]
for a in [0x50D390,0x50E3A0,0x494360]:
    before=f'phase73-DX-{a:x}-assembly.txt'
    aa=[int(line[:8],16) for line in (ROOT/'ghidra'/before).read_text().splitlines() if re.match(r'^[0-9a-fA-F]{8}:',line)]
    last=next(md.disasm(pe_read(exe.read_bytes(),aa[-1],15),aa[-1]));assert aa[0]==a and last.mnemonic=='ret'
    n=last.address+last.size-a;key=f'phase73-final-DX-{a:x}'
    rows.append(dict(program='DKII-DX.exe',exe=str(exe),address=a,length=n,before=before,after=key+'-assembly.txt',memory=key+'-memory.txt',executed=True))
    queries.extend([dict(id=key+'-assembly',tool='disassemble_function',args=dict(program='DKII-DX.exe',address=hex(a)),print_limit=5),dict(id=key+'-memory',tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(a),length=n),print_limit=5)])
for slot in [0xC0,0x1C8]:
    key=f'phase73-final-DX-world-slot-{slot:x}'
    blocks.append(dict(program='DKII-DX.exe',exe=str(exe),address=0x66E3EC+slot,length=4,memory=key+'-memory.txt',label=f'world-vslot-{slot:x}'))
    queries.append(dict(id=key+'-memory',tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(0x66E3EC+slot),length=4),print_limit=5))
(ROOT/'phase73-primary-manifest.json').write_text(json.dumps(dict(functions=rows,blocks=blocks),indent=2),encoding='utf8')
(ROOT/'phase73-primary-queries.json').write_text(json.dumps(queries,indent=2),encoding='utf8')
print('3wholeexecutedfunctions/2vslotranges')
