"""Trace actual CreatureSpell execution toward the native Shot factory."""
from pathlib import Path
import json
ROOT=Path(__file__).resolve().parent
q=[]
for a in [0x45EB20,0x45F030,0x461130,0x475660]:
    for label,tool in [('create','create_function'),('assembly','disassemble_function'),('decompile','force_decompile')]:
        args=dict(program='DKII-DX.exe',address=hex(a))
        if tool=='create_function':args['disassemble_first']=True
        q.append(dict(id=f'phase79-DX-{a:x}-{label}',tool=tool,args=args,print_limit=5))
for a in [0x4B7CD0,0x50DFA0]:
    q.append(dict(id=f'phase79-DX-{a:x}-xrefs',tool='get_xrefs_to',args=dict(program='DKII-DX.exe',address=hex(a)),print_limit=5))
(ROOT/'phase79-creature-cast-queries.json').write_text(json.dumps(q,indent=2),encoding='utf8')
q=[]
for a in [0x4776C0,0x4982B0,0x4984D0,0x498870]:
    for label,tool in [('create','create_function'),('assembly','disassemble_function'),('decompile','force_decompile')]:
        args=dict(program='DKII-DX.exe',address=hex(a))
        if tool=='create_function':args['disassemble_first']=True
        q.append(dict(id=f'phase79-DX-{a:x}-{label}',tool=tool,args=args,print_limit=5))
for a in [0x4982B0,0x4984D0,0x498870]:
    q.append(dict(id=f'phase79-DX-{a:x}-xrefs',tool='get_xrefs_to',args=dict(program='DKII-DX.exe',address=hex(a)),print_limit=5))
(ROOT/'phase79-creature-shot-callers-queries.json').write_text(json.dumps(q,indent=2),encoding='utf8')
q=[]
for a in [0x4943F0,0x50E080,0x50E2D0,0x634DB0,0x517490]:
    for label,tool in [('create','create_function'),('assembly','disassemble_function'),('decompile','force_decompile')]:
        args=dict(program='DKII-DX.exe',address=hex(a))
        if tool=='create_function':args['disassemble_first']=True
        q.append(dict(id=f'phase79-DX-{a:x}-{label}',tool=tool,args=args,print_limit=5))
(ROOT/'phase79-creature-shot-helper-queries.json').write_text(json.dumps(q,indent=2),encoding='utf8')
