from pathlib import Path
import json,struct
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent
data=Path('D:/Games/DK2/Dungeon Keeper 2/DKII-DX.exe').read_bytes()
slots={f'Object+{s:x}':struct.unpack('<I',pe_read(data,0x66d654+s,4))[0] for s in [0x70,0x7c,0x8c]}
slots.update({f'World+{s:x}':struct.unpack('<I',pe_read(data,0x66e3ec+s,4))[0] for s in [0x188,0xbc,0x1c8]})
entries=[0x4b5ef0,0x4872a0,0x49c760]+list(slots.values());tasks=[]
for a in sorted(set(entries)):
    for tool,suffix in [('create_function','create'),('disassemble_function','assembly'),('force_decompile','decompile')]:
        args=dict(program='DKII-DX.exe',address=hex(a))
        if tool=='create_function':args['disassemble_first']=True
        tasks.append(dict(id=f'phase89-object-{a:x}-{suffix}',tool=tool,args=args,print_limit=5))
(ROOT/'phase89-object-queries.json').write_text(json.dumps(tasks,indent=2),encoding='utf8')
(ROOT/'phase89-object-vslots.json').write_text(json.dumps(slots,indent=2),encoding='utf8')
print({k:hex(v) for k,v in slots.items()})
