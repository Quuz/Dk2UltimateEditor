from pathlib import Path
import json,struct
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent
data=(ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe').read_bytes()
entries=[0x4b5e10,0x4d0e40]
slots={hex(o):struct.unpack('<I',pe_read(data,0x66d524+o,4))[0] for o in [0x28,0x3c,0x90]}
entries+=list(slots.values());q=[]
for a in sorted(set(entries)):
    for tool,suffix in [('create_function','create'),('disassemble_function','assembly'),('force_decompile','decompile')]:
        args=dict(program='DKII-DX.exe',address=hex(a))
        if tool=='create_function':args['disassemble_first']=True
        q.append(dict(id=f'phase90-creature-{a:x}-{suffix}',tool=tool,args=args,print_limit=5))
(ROOT/'phase90-creature-factory-queries.json').write_text(json.dumps(q,indent=2),encoding='utf8')
(ROOT/'phase90-creature-vslots.json').write_text(json.dumps({k:hex(v) for k,v in slots.items()},indent=2),encoding='utf8')
print(len(q),'queries; slots', {k:hex(v) for k,v in slots.items()})
