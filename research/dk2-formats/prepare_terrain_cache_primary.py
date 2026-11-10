"""Re-read executed native code, actual virtual slots and static light caller."""
from pathlib import Path
import json,re,struct
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import pe_read
from emulate_terrain_cache import PROFILES
ROOT=Path(__file__).resolve().parent
md=Cs(CS_ARCH_X86,CS_MODE_32);functions=[];blocks=[];queries=[]
for profile,p in PROFILES.items():
    exe=ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe' if profile=='DX' else ROOT/'source-binaries/DKII-local13.exe'
    for role in ['init','select','enum','getter','count']:
        a=p[role];before=f'phase74-{profile}-{a:x}-assembly.txt'
        if profile=='DX' and role=='getter':before='phase58-final-game-508dd0-assembly.txt'
        aa=[int(line[:8],16) for line in (ROOT/'ghidra'/before).read_text().splitlines() if re.match(r'^[0-9a-fA-F]{8}:',line)]
        last=next(md.disasm(pe_read(exe.read_bytes(),aa[-1],15),aa[-1]));assert aa[0]==a and last.mnemonic=='ret'
        n=last.address+last.size-a;key=f'phase74-final-{profile}-{a:x}'
        functions.append(dict(program=p['program'],exe=str(exe),address=a,length=n,before=before,after=key+'-assembly.txt',memory=key+'-memory.txt',executed=True,role=role))
        queries.extend([dict(id=key+'-assembly',tool='disassemble_function',args=dict(program=p['program'],address=hex(a)),print_limit=5),dict(id=key+'-memory',tool='read_memory',args=dict(program=p['program'],address=hex(a),length=n),print_limit=5)])
    for slot,role in [(0x60,'enum'),(0x70,'getter'),(0x74,'count')]:
        a=p['vtable']+slot;assert struct.unpack('<I',pe_read(exe.read_bytes(),a,4))[0]==p[role]
        key=f'phase74-final-{profile}-vslot-{slot:x}'
        blocks.append(dict(program=p['program'],exe=str(exe),address=a,length=4,memory=key+'-memory.txt',label=f'world-{role}'))
        queries.append(dict(id=key+'-memory',tool='read_memory',args=dict(program=p['program'],address=hex(a),length=4),print_limit=5))
exe=ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe';a=0x441CD0;before='phase48-reader-441cd0-disassemble_function.txt'
aa=[int(line[:8],16) for line in (ROOT/'ghidra'/before).read_text().splitlines() if re.match(r'^[0-9a-fA-F]{8}:',line)];last=next(md.disasm(pe_read(exe.read_bytes(),aa[-1],15),aa[-1]));assert aa[0]==a and last.mnemonic=='ret'
n=last.address+last.size-a;key='phase74-final-DX-441cd0'
functions.append(dict(program='DKII-DX.exe',exe=str(exe),address=a,length=n,before=before,after=key+'-assembly.txt',memory=key+'-memory.txt',executed=False,role='static-light-caller'))
queries.extend([dict(id=key+'-assembly',tool='disassemble_function',args=dict(program='DKII-DX.exe',address=hex(a)),print_limit=5),dict(id=key+'-memory',tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(a),length=n),print_limit=5)])
(ROOT/'phase74-primary-manifest.json').write_text(json.dumps(dict(functions=functions,blocks=blocks),indent=2),encoding='utf8')
(ROOT/'phase74-primary-queries.json').write_text(json.dumps(queries,indent=2),encoding='utf8')
print('11completefunctions(10executed)/6vslotranges')
