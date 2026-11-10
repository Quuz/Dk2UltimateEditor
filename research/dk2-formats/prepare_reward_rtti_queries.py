from pathlib import Path
import json
ROOT=Path(__file__).resolve().parent
tasks=[]
for a in [0x635120,0x634f30]:
    for tool,suffix in [('create_function','create'),('disassemble_function','assembly'),('force_decompile','decompile')]:
        args=dict(program='DKII-DX.exe',address=hex(a))
        if tool=='create_function':args['disassemble_first']=True
        tasks.append(dict(id=f'phase89-rtti-{a:x}-{suffix}',tool=tool,args=args,print_limit=5))
(ROOT/'phase89-rtti-queries.json').write_text(json.dumps(tasks,indent=2),encoding='utf8')
