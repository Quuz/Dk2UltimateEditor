from pathlib import Path
import json
ROOT=Path(__file__).resolve().parent;rows=json.loads((ROOT/'phase100-old-object-boundary-contexts.json').read_text(encoding='utf8'));q=[]
for r in rows:
    a=r['nearest_calltarget_hint']
    for tool in ['create_function','disassemble_function','force_decompile']:
        args=dict(program='DKII-local13.exe',address=hex(a))
        if tool=='create_function':args['disassemble_first']=True
        q.append(dict(id=f'phase100-old-context-{a:x}-{tool}',tool=tool,args=args,print_limit=5))
(ROOT/'phase100-old-object-context-queries.json').write_text(json.dumps(q,indent=2),encoding='utf8')
