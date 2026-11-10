"""Inspect the real CreatureSpell predicate dispatcher and its native callees."""
from pathlib import Path
import json
ROOT=Path(__file__).resolve().parent
q=[]
for a in [0x49A6C0,0x49A7A0,0x49A7B0,0x49A830,0x49A840,0x49A8C0,0x49A8F0,0x49A920,0x478050,0x50DFA0]:
    for label,tool in [('create','create_function'),('assembly','disassemble_function'),('decompile','force_decompile')]:
        args=dict(program='DKII-DX.exe',address=hex(a))
        if tool=='create_function':args['disassemble_first']=True
        q.append(dict(id=f'phase78-DX-{a:x}-{label}',tool=tool,args=args,print_limit=5))
for a in [0x49A7A0,0x49A7B0]:
    q.append(dict(id=f'phase78-DX-{a:x}-xrefs',tool='get_xrefs_to',args=dict(program='DKII-DX.exe',address=hex(a)),print_limit=5))
for a,n in [(0x66D524,0x98),(0x6E4AC8,102)]:
    q.append(dict(id=f'phase78-DX-{a:x}-data',tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(a),length=n),print_limit=5))
(ROOT/'phase78-predicate-queries.json').write_text(json.dumps(q,indent=2),encoding='utf8')
q=[]
for a in [0x45CB00,0x47BBE0]:
    for label,tool in [('create','create_function'),('assembly','disassemble_function'),('decompile','force_decompile')]:
        args=dict(program='DKII-DX.exe',address=hex(a))
        if tool=='create_function':args['disassemble_first']=True
        q.append(dict(id=f'phase78-DX-{a:x}-{label}',tool=tool,args=args,print_limit=5))
for a in [0x45CB00,0x6DA8C7,0x6DA8C0]:
    q.append(dict(id=f'phase78-DX-{a:x}-xrefs',tool='get_xrefs_to',args=dict(program='DKII-DX.exe',address=hex(a)),print_limit=5))
q.append(dict(id='phase78-DX-6da8c0-data',tool='read_memory',args=dict(program='DKII-DX.exe',address='0x6da8c0',length=512),print_limit=5))
(ROOT/'phase78-predicate-callers-queries.json').write_text(json.dumps(q,indent=2),encoding='utf8')
