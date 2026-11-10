from pathlib import Path
import json
ROOT=Path(__file__).resolve().parent
q=[]
for a in [0x517490,0x50ceb0,0x508df0,0x56eb60,0x56ed20,0x4f66e0]:
    for tool in ['create_function','disassemble_function','force_decompile']:
        args=dict(program='DKII-DX.exe',address=hex(a))
        if tool=='create_function':args['disassemble_first']=True
        q.append(dict(id=f'phase99-loader-{a:x}-{tool}',tool=tool,args=args,print_limit=5))
(ROOT/'phase99-loader-queries.json').write_text(json.dumps(q,indent=2),encoding='utf8')
