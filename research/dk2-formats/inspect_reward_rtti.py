from pathlib import Path
import json,struct,pefile
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent
data=Path('D:/Games/DK2/Dungeon Keeper 2/DKII-DX.exe').read_bytes();rows=[];md=Cs(CS_ARCH_X86,CS_MODE_32)
print('635120:',[(hex(i.address),i.mnemonic,i.op_str) for i in md.disasm(pe_read(data,0x635120,32),0x635120)])
for dll in pefile.PE(data=data).DIRECTORY_ENTRY_IMPORT:
    for imp in dll.imports:
        raw=struct.unpack('<I',pe_read(data,imp.address,4))[0]
        if raw==0x28cfa4 or (imp.name and b'RTDynamicCast' in imp.name):rows.append(dict(iat=hex(imp.address),dll=dll.dll.decode(),name=imp.name.decode() if imp.name else None,value=hex(raw)))
print(rows)
print('Gold scale double66D428:',struct.unpack('<d',pe_read(data,0x66d428,8))[0])
(ROOT/'phase89-reward-rtti-import.json').write_text(json.dumps(rows,indent=2),encoding='utf8')
