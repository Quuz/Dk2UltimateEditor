"""Inspect real slot60/70/74 and both complete Terrain-cache initializers."""
from pathlib import Path
import json
ROOT=Path(__file__).resolve().parent
q=[]
for profile,program,addresses in [
    ('DX','DKII-DX.exe',[0x50CEB0,0x508DF0]),
    ('old','DKII-local13.exe',[0x44E7E0,0x44B910,0x501900,0x4FD7F0,0x4FD810]),
]:
    for a in addresses:
        for label,tool in [('create','create_function'),('assembly','disassemble_function'),('decompile','force_decompile')]:
            args=dict(program=program,address=hex(a))
            if tool=='create_function':args['disassemble_first']=True
            q.append(dict(id=f'phase74-{profile}-{a:x}-{label}',tool=tool,args=args,print_limit=10))
(ROOT/'phase74-cache-helper-queries.json').write_text(json.dumps(q,indent=2),encoding='utf8')
