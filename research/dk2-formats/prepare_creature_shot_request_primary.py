"""Re-read full request functions and real callee bytes after native execution."""
from pathlib import Path
import json,re
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent
exe=ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe';data=exe.read_bytes();md=Cs(CS_ARCH_X86,CS_MODE_32);functions=[];blocks=[];queries=[]
for a,before,executed in [(0x4982B0,'phase79-DX-4982b0-assembly.txt',True),(0x4984D0,'phase79-DX-4984d0-assembly.txt',True),
 (0x4943F0,'phase79-DX-4943f0-assembly.txt',True),(0x517490,'phase79-DX-517490-assembly.txt',True),(0x50E080,'phase79-DX-50e080-assembly.txt',True),
 (0x50DFA0,'phase78-final-DX-50dfa0-assembly.txt',True),(0x4B7CD0,'phase54-final-game-4b7cd0-assembly.txt',True),(0x50E2D0,'phase79-DX-50e2d0-assembly.txt',True),
 (0x498870,'phase79-DX-498870-assembly.txt',False),(0x634DB0,'phase79-DX-634db0-assembly.txt',False)]:
    aa=[int(line[:8],16) for line in (ROOT/'ghidra'/before).read_text().splitlines() if re.match(r'^[0-9a-fA-F]{8}:',line)];last=next(md.disasm(pe_read(data,aa[-1],15),aa[-1]));assert aa[0]==a and last.mnemonic=='ret'
    n=last.address+last.size-a;key=f'phase79-final-DX-{a:x}';functions.append(dict(program='DKII-DX.exe',exe=str(exe),address=a,length=n,before=before,after=key+'-assembly.txt',memory=key+'-memory.txt',executed=executed))
    queries.extend([dict(id=key+'-assembly',tool='disassemble_function',args=dict(program='DKII-DX.exe',address=hex(a)),print_limit=5),dict(id=key+'-memory',tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(a),length=n),print_limit=5)])
for slot in [0xE4,0xE8,0x1AC]:
    a=0x66E3EC+slot;key=f'phase79-final-DX-data-{a:x}';blocks.append(dict(program='DKII-DX.exe',exe=str(exe),address=a,length=4,memory=key+'-memory.txt',label=f'World-vslot{slot:X}'))
    queries.append(dict(id=key+'-memory',tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(a),length=4),print_limit=5))
(ROOT/'phase79-primary-manifest.json').write_text(json.dumps(dict(functions=functions,blocks=blocks),indent=2),encoding='utf8');(ROOT/'phase79-primary-queries.json').write_text(json.dumps(queries,indent=2),encoding='utf8')
print('10whole native functions(8executed),3file-backed vslots')
