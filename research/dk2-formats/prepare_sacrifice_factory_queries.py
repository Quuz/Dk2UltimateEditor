from pathlib import Path
import json
ROOT=Path(__file__).resolve().parent
tasks=[]
for a in [0x4b78d0,0x4b7a90,0x4ba420,0x48b120,0x48ad30]:
    for tool,suffix in [('create_function','create'),('disassemble_function','assembly'),('force_decompile','decompile')]:
        args=dict(program='DKII-DX.exe',address=hex(a))
        if tool=='create_function':args['disassemble_first']=True
        tasks.append(dict(id=f'phase89-sacrifice-{a:x}-{suffix}',tool=tool,args=args,print_limit=5))
(ROOT/'phase89-sacrifice-factory-queries.json').write_text(json.dumps(tasks,indent=2),encoding='utf8')
