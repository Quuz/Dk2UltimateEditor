"""Inspect complete client EffectElement construction and range consumers."""
from pathlib import Path
import json
ROOT=Path(__file__).resolve().parent
q=[]
for a in [0x435E70,0x4468E0,0x435F20,0x446C70,0x434E50,0x4363E0,0x4363F0,0x4B4160,0x4B41A0]:
    for label,tool in [('create','create_function'),('assembly','disassemble_function'),('decompile','force_decompile')]:
        args=dict(program='DKII-DX.exe',address=hex(a))
        if tool=='create_function':args['disassemble_first']=True
        q.append(dict(id=f'phase81-DX-{a:x}-{label}',tool=tool,args=args,print_limit=5))
for a in [0x6DA458,0x6DA45C]:q.append(dict(id=f'phase81-DX-{a:x}-xrefs',tool='get_xrefs_to',args=dict(program='DKII-DX.exe',address=hex(a)),print_limit=5))
(ROOT/'phase81-element-initialization-queries.json').write_text(json.dumps(q,indent=2),encoding='utf8')
