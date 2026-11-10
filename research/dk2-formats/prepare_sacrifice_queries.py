from pathlib import Path
import json
ROOT=Path(__file__).resolve().parent
tasks=[]
for a in [0x4f1600,0x4f1660,0x4f1780,0x4728e0,0x49f600,0x5133a0,0x51db30]:
    for tool,suffix in [('create_function','create'),('disassemble_function','assembly'),('force_decompile','decompile')]:
        args=dict(program='DKII-DX.exe',address=hex(a))
        if tool=='create_function':args['disassemble_first']=True
        tasks.append(dict(id=f'phase88-sacrifice-DX-{a:x}-{suffix}',tool=tool,args=args,print_limit=5))
for a in [0x4f1600,0x4f1660,0x4f1780]:
    tasks.append(dict(id=f'phase88-sacrifice-DX-{a:x}-xrefs',tool='get_xrefs_to',args=dict(program='DKII-DX.exe',address=hex(a)),print_limit=5))
(ROOT/'phase88-sacrifice-queries.json').write_text(json.dumps(tasks,indent=2),encoding='utf8')
print(len(tasks),'queries prepared')
