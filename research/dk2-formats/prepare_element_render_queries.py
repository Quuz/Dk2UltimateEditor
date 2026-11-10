from pathlib import Path
import json
ROOT=Path(__file__).resolve().parent;q=[]
for a in [0x435ec0,0x435ed0,0x435ee0,0x435eb0,0x4b3ad0,0x4476e0]:
    for tool in ['create_function','disassemble_function','force_decompile','get_xrefs_to']:
        args=dict(program='DKII-DX.exe',address=hex(a))
        if tool=='create_function':args['disassemble_first']=True
        q.append(dict(id=f'phase102-render-{a:x}-{tool}',tool=tool,args=args,print_limit=5))
(ROOT/'phase102-render-queries.json').write_text(json.dumps(q,indent=2),encoding='utf8')
