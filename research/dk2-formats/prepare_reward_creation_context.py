from pathlib import Path
import json,struct
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent
data=Path('D:/Games/DK2/Dungeon Keeper 2/DKII-DX.exe').read_bytes()
entries=[0x4b7900,0x4b7190,0x49b1c0,0x4c6e70,0x4bab80]
slots={hex(s):hex(struct.unpack('<I',pe_read(data,0x66e3ec+s,4))[0]) for s in [0x184]}
entries+=[int(v,16) for v in slots.values()]
tasks=[]
for a in entries:
    for tool,suffix in [('create_function','create'),('disassemble_function','assembly'),('force_decompile','decompile')]:
        args=dict(program='DKII-DX.exe',address=hex(a))
        if tool=='create_function':args['disassemble_first']=True
        tasks.append(dict(id=f'phase89-creation-{a:x}-{suffix}',tool=tool,args=args,print_limit=5))
(ROOT/'phase89-creation-context-queries.json').write_text(json.dumps(tasks,indent=2),encoding='utf8')
print(slots)
