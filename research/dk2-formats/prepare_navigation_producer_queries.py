from pathlib import Path
import json
ROOT=Path(__file__).resolve().parent;tasks=[]
for address in [0x454fd0,0x44fc40,0x4ce770,0x4ce740,0x4cef20]:
    for tool in ['create_function','disassemble_function','force_decompile','get_xrefs_to']:
        args=dict(program='DKII-DX.exe',address=hex(address))
        if tool=='create_function':args['disassemble_first']=True
        tasks.append(dict(id=f'phase99-navproducer-{address:x}-{tool}',tool=tool,args=args,print_limit=5))
(ROOT/'phase99-navproducer-queries.json').write_text(json.dumps(tasks,indent=2),encoding='utf8')
