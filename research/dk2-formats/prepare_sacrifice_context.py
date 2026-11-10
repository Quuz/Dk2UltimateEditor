from pathlib import Path
import json,struct
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent
data=Path('D:/Games/DK2/Dungeon Keeper 2/DKII-DX.exe').read_bytes()
entries=[0x4e4d90,0x4e52f0,0x4559b0,0x453a20,0x4539b0]
slots={hex(s):hex(struct.unpack('<I',pe_read(data,0x66e3ec+s,4))[0]) for s in [0x1d4,0x1fc,0x264]}
entries += [int(v,16) for v in slots.values()]
tasks=[]
for a in sorted(set(entries)):
    for tool,suffix in [('create_function','create'),('disassemble_function','assembly'),('force_decompile','decompile')]:
        args=dict(program='DKII-DX.exe',address=hex(a))
        if tool=='create_function':args['disassemble_first']=True
        tasks.append(dict(id=f'phase88-sacrifice-context-{a:x}-{suffix}',tool=tool,args=args,print_limit=5))
(ROOT/'phase88-sacrifice-context-queries.json').write_text(json.dumps(tasks,indent=2),encoding='utf8')
(ROOT/'phase88-sacrifice-context-slots.json').write_text(json.dumps(slots,indent=2),encoding='utf8')
print(slots)
