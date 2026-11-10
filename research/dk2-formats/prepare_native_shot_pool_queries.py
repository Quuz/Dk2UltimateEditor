"""Inspect real pool/base constructors for a Shot success path without code patches."""
from pathlib import Path
import json
ROOT=Path(__file__).resolve().parent
q=[]
for a in [0x554B30,0x507850,0x4B3610,0x4B1DF0,0x4B4C30,0x4B5560,0x4B5020,0x4B4E70,0x4B37D0,0x4B7D60,0x4AAC90,0x50E1E0]:
    for label,tool in [('create','create_function'),('assembly','disassemble_function'),('decompile','force_decompile')]:
        args=dict(program='DKII-DX.exe',address=hex(a))
        if tool=='create_function':args['disassemble_first']=True
        q.append(dict(id=f'phase80-DX-{a:x}-{label}',tool=tool,args=args,print_limit=5))
(ROOT/'phase80-native-shot-pool-queries.json').write_text(json.dumps(q,indent=2),encoding='utf8')
q=[]
for a in [0x4B28D0,0x4B4D40,0x4B4F60,0x4545C0,0x4B41E0,0x4B3AC0,0x41F900,0x4B5F30,0x4B7E30]:
    for label,tool in [('create','create_function'),('assembly','disassemble_function'),('decompile','force_decompile')]:
        args=dict(program='DKII-DX.exe',address=hex(a))
        if tool=='create_function':args['disassemble_first']=True
        q.append(dict(id=f'phase80-DX-{a:x}-{label}',tool=tool,args=args,print_limit=5))
(ROOT/'phase80-native-shot-dependencies-queries.json').write_text(json.dumps(q,indent=2),encoding='utf8')
q=[]
for a in [0x4B4A10,0x4B4120]:
    for label,tool in [('create','create_function'),('assembly','disassemble_function'),('decompile','force_decompile')]:
        args=dict(program='DKII-DX.exe',address=hex(a))
        if tool=='create_function':args['disassemble_first']=True
        q.append(dict(id=f'phase80-DX-{a:x}-{label}',tool=tool,args=args,print_limit=5))
(ROOT/'phase80-native-entity-registration-queries.json').write_text(json.dumps(q,indent=2),encoding='utf8')
q=[]
for a in [0x4AB830,0x4AB8A0]:
    for label,tool in [('create','create_function'),('assembly','disassemble_function'),('decompile','force_decompile')]:
        args=dict(program='DKII-DX.exe',address=hex(a))
        if tool=='create_function':args['disassemble_first']=True
        q.append(dict(id=f'phase80-DX-{a:x}-{label}',tool=tool,args=args,print_limit=5))
(ROOT/'phase80-native-shot-direction-queries.json').write_text(json.dumps(q,indent=2),encoding='utf8')
q=[]
for a in [0x65FB54,0x65FAE0,0x4ABA10]:
    for label,tool in [('create','create_function'),('assembly','disassemble_function'),('decompile','force_decompile')]:
        args=dict(program='DKII-DX.exe',address=hex(a))
        if tool=='create_function':args['disassemble_first']=True
        q.append(dict(id=f'phase80-DX-{a:x}-{label}',tool=tool,args=args,print_limit=5))
(ROOT/'phase80-native-shot-math-queries.json').write_text(json.dumps(q,indent=2),encoding='utf8')
