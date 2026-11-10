from pathlib import Path
import json
ROOT=Path(__file__).resolve().parent
queries=[]
for a in [0x43AAD0,0x43AD70,0x43B0B0,0x43B180]:
    for label,tool in [('create','create_function'),('assembly','disassemble_function'),('decompile','force_decompile')]:
        args=dict(program='DKII-DX.exe',address=hex(a))
        if tool=='create_function':args['disassemble_first']=True
        queries.append(dict(id=f'phase77-DX-{a:x}-{label}',tool=tool,args=args,print_limit=10))
(ROOT/'phase77-sight-dispatch-queries.json').write_text(json.dumps(queries,indent=2),encoding='utf8')
