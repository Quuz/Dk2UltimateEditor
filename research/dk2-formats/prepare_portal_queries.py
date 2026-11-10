from pathlib import Path
import json
ROOT=Path(__file__).resolve().parent
q=[]
for a in [0x496d00,0x46f390]:
    for t,s in [('create_function','create'),('disassemble_function','assembly'),('force_decompile','decompile')]:
        args=dict(program='DKII-DX.exe',address=hex(a))
        if t=='create_function':args['disassemble_first']=True
        q.append(dict(id=f'phase91-portal-{a:x}-{s}',tool=t,args=args,print_limit=5))
q.append(dict(id='phase91-portal-459c20-xrefs',tool='get_xrefs_to',args=dict(program='DKII-DX.exe',address='0x459c20'),print_limit=5))
(ROOT/'phase91-portal-queries.json').write_text(json.dumps(q,indent=2),encoding='utf8')
