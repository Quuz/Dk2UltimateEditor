from pathlib import Path
import json
ROOT=Path(__file__).resolve().parent;tasks=[]
for address in [0x4d1930,0x4d1820,0x4d1840]:
    for tool in ['create_function','disassemble_function','force_decompile','get_xrefs_to']:
        args=dict(program='DKII-DX.exe',address=hex(address))
        if tool=='create_function':args['disassemble_first']=True
        tasks.append(dict(id=f'phase99-navwrite-{address:x}-{tool}',tool=tool,args=args,print_limit=5))
(ROOT/'phase99-navwrite-queries.json').write_text(json.dumps(tasks,indent=2),encoding='utf8')
