from pathlib import Path
import json,struct
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent;data=(ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe').read_bytes();tasks=[]
addresses=[0x4d1970,0x455b90,0x4559d0,0x452d80,0x454b80,0x455d40]
for slot in [0x64,0x70,0x1d8,0x264]:
    a=struct.unpack('<I',pe_read(data,0x66e3ec+slot,4))[0];print('Worldslot',hex(slot),hex(a));addresses.append(a)
for address in dict.fromkeys(addresses):
    for tool in ['create_function','disassemble_function','force_decompile']:
        args=dict(program='DKII-DX.exe',address=hex(address))
        if tool=='create_function':args['disassemble_first']=True
        tasks.append(dict(id=f'phase99-placement-{address:x}-{tool}',tool=tool,args=args,print_limit=5))
(ROOT/'phase99-placement-queries.json').write_text(json.dumps(tasks,indent=2),encoding='utf8')
