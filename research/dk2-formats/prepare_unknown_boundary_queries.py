from pathlib import Path
import json
ROOT=Path(__file__).resolve().parent;tasks=[]
for a in [0x42da1a,0x492495,0x497c62,0x49806b,0x4a9004,0x5642b3]:
    tasks.append(dict(id=f'phase100-old-boundary-{a:x}',tool='get_function_by_address',args=dict(program='DKII-local13.exe',address=hex(a)),print_limit=400))
for a in [0x4a7d10,0x4a7d90,0x4a7e70,0x4a80e0,0x4a8e70,0x4a9a00]:
    for tool in ['create_function','disassemble_function','force_decompile']:
        args=dict(program='DKII-DX.exe',address=hex(a))
        if tool=='create_function':args['disassemble_first']=True
        tasks.append(dict(id=f'phase100-door-context-{a:x}-{tool}',tool=tool,args=args,print_limit=5))
(ROOT/'phase100-unknown-boundary-queries.json').write_text(json.dumps(tasks,indent=2),encoding='utf8')
(ROOT/'phase100-DX-door-context-queries.json').write_text(json.dumps([t for t in tasks if t['args']['program']=='DKII-DX.exe'],indent=2),encoding='utf8')
