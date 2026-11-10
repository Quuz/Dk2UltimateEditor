from pathlib import Path
import json
ROOT=Path(__file__).resolve().parent
addresses=[0x5090c0,0x509100,0x509160,0x509200,0x510370]
tasks=[]
for address in addresses:
    for tool in ['create_function','disassemble_function','force_decompile','get_xrefs_to']:
        args=dict(program='DKII-DX.exe',address=hex(address))
        if tool=='create_function':args['disassemble_first']=True
        tasks.append(dict(id=f'phase99-navmap-{address:x}-{tool}',tool=tool,args=args,print_limit=5))
(ROOT/'phase99-navmap-queries.json').write_text(json.dumps(tasks,indent=2),encoding='utf8')
print(len(tasks),'queries')
