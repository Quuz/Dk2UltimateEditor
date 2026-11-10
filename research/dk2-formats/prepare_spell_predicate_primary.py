"""Re-read complete native functions after emulation and original data slots."""
from pathlib import Path
import json,re
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent
exe=ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe';data=exe.read_bytes();md=Cs(CS_ARCH_X86,CS_MODE_32)
functions=[];blocks=[];queries=[]
executed=[0x47BBE0,0x49A6C0,0x49A7A0,0x49A7B0,0x49A830,0x49A840,0x49A8C0,0x49A8F0,0x49A920,0x478050,0x50DFA0]
for a in executed+[0x45CB00,0x4633D0,0x463500,0x463760,0x463BE0]:
    before=f'phase78-DX-{a:x}-assembly.txt'
    aa=[int(line[:8],16) for line in (ROOT/'ghidra'/before).read_text().splitlines() if re.match(r'^[0-9a-fA-F]{8}:',line)]
    last=next(md.disasm(pe_read(data,aa[-1],15),aa[-1]));assert aa[0]==a and (last.mnemonic=='ret' or (a==0x45CB00 and last.mnemonic=='jmp'))
    n=last.address+last.size-a;key=f'phase78-final-DX-{a:x}'
    functions.append(dict(program='DKII-DX.exe',exe=str(exe),address=a,length=n,before=before,after=key+'-assembly.txt',memory=key+'-memory.txt',executed=a in executed))
    queries.extend([dict(id=key+'-assembly',tool='disassemble_function',args=dict(program='DKII-DX.exe',address=hex(a)),print_limit=5),dict(id=key+'-memory',tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(a),length=n),print_limit=5)])
for a,n,label in [(0x66E3EC+0xE8,4,'World-vslotE8-actual-CreatureSpell-getter'),(0x6E4AC8,102,'Predicate-table-before-native-initialization'),(0x6DA8B0,7025,'AI-table-before-native-initialization')]:
    key=f'phase78-final-DX-data-{a:x}';blocks.append(dict(program='DKII-DX.exe',exe=str(exe),address=a,length=n,memory=key+'-memory.txt',label=label))
    queries.append(dict(id=key+'-memory',tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(a),length=n),print_limit=5))
(ROOT/'phase78-primary-manifest.json').write_text(json.dumps(dict(functions=functions,blocks=blocks),indent=2),encoding='utf8')
(ROOT/'phase78-primary-queries.json').write_text(json.dumps(queries,indent=2),encoding='utf8')
print('16whole native functions(11executed),3data ranges')
