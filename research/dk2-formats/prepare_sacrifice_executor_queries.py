from pathlib import Path
import json
ROOT=Path(__file__).resolve().parent
tasks=[]
for a in [0x454110,0x5558d0,0x4c19c0,0x4c4600]:
    for tool,suffix in [('create_function','create'),('disassemble_function','assembly'),('force_decompile','decompile')]:
        args=dict(program='DKII-DX.exe',address=hex(a))
        if tool=='create_function':args['disassemble_first']=True
        tasks.append(dict(id=f'phase88-sacrifice-executor-{a:x}-{suffix}',tool=tool,args=args,print_limit=5))
(ROOT/'phase88-sacrifice-executor-queries.json').write_text(json.dumps(tasks,indent=2),encoding='utf8')
