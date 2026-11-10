"""Inspect the native food-related path suggested by Alternative36/Room4 data."""
from pathlib import Path
import json
ROOT=Path(__file__).resolve().parent
q=[]
for a in [0x4633D0,0x463500,0x463760,0x463BE0]:
    for label,tool in [('create','create_function'),('assembly','disassemble_function'),('decompile','force_decompile')]:
        args=dict(program='DKII-DX.exe',address=hex(a))
        if tool=='create_function':args['disassemble_first']=True
        q.append(dict(id=f'phase78-DX-{a:x}-{label}',tool=tool,args=args,print_limit=10))
q.append(dict(id='phase78-DX-49a7b0-xrefs',tool='get_xrefs_to',args=dict(program='DKII-DX.exe',address='0x49a7b0'),print_limit=10))
(ROOT/'phase78-food-cast-queries.json').write_text(json.dumps(q,indent=2),encoding='utf8')
