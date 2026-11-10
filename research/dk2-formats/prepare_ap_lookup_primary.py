"""Re-read full AP getters, known call contracts and static patrol control."""
from pathlib import Path
import json,re,struct
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent
md=Cs(CS_ARCH_X86,CS_MODE_32);functions=[];blocks=[];queries=[]
for profile,program,exe,a,before,executed in [
 ('DX','DKII-DX.exe',ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe',0x50FAE0,'phase76-DX-50fae0-assembly.txt',True),
 ('old','DKII-local13.exe',ROOT/'source-binaries/DKII-local13.exe',0x504140,'phase76-old-504140-assembly.txt',True),
 ('DX','DKII-DX.exe',ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe',0x5558D0,'phase75-final-DX-5558d0-assembly.txt',True),
 ('DX','DKII-DX.exe',ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe',0x508FE0,'phase76-DX-508fe0-assembly.txt',True),
 ('DX','DKII-DX.exe',ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe',0x471EB0,'phase33-AP-patrol-assembly.txt',False),
]:
    aa=[int(line[:8],16) for line in (ROOT/'ghidra'/before).read_text().splitlines() if re.match(r'^[0-9a-fA-F]{8}:',line)]
    last=next(md.disasm(pe_read(exe.read_bytes(),aa[-1],15),aa[-1]));assert aa[0]==a and last.mnemonic=='ret'
    n=last.address+last.size-a;key=f'phase76-final-{profile}-{a:x}'
    functions.append(dict(program=program,exe=str(exe),address=a,length=n,before=before,after=key+'-assembly.txt',memory=key+'-memory.txt',executed=executed))
    queries.extend([dict(id=key+'-assembly',tool='disassemble_function',args=dict(program=program,address=hex(a)),print_limit=5),dict(id=key+'-memory',tool='read_memory',args=dict(program=program,address=hex(a),length=n),print_limit=5)])
for profile,program,exe,vtable,slot,expected in [
 ('DX','DKII-DX.exe',ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe',0x66E3EC,0x2C8,0x50FAE0),
 ('DX','DKII-DX.exe',ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe',0x66E3EC,0x1D4,0x508FE0),
 ('old','DKII-local13.exe',ROOT/'source-binaries/DKII-local13.exe',0x65F31C,0x2C8,0x504140),
]:
    a=vtable+slot;assert struct.unpack('<I',pe_read(exe.read_bytes(),a,4))[0]==expected;key=f'phase76-final-{profile}-vslot-{slot:x}'
    blocks.append(dict(program=program,exe=str(exe),address=a,length=4,memory=key+'-memory.txt',label=f'world-slot-{slot:x}',target=expected))
    queries.append(dict(id=key+'-memory',tool='read_memory',args=dict(program=program,address=hex(a),length=4),print_limit=5))
(ROOT/'phase76-primary-manifest.json').write_text(json.dumps(dict(functions=functions,blocks=blocks),indent=2),encoding='utf8')
(ROOT/'phase76-primary-queries.json').write_text(json.dumps(queries,indent=2),encoding='utf8')
print('5completefunctions(4executed)/3vslotranges')
