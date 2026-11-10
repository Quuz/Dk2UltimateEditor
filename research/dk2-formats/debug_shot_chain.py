import json
from pathlib import Path
from chain_keeper_shot_creation import build_case,cases,END
from emulate_hero_parties import compare_code
from emulate_element_colors import pack32
queries=[]
c=next(c for c in cases() if c['name']=='chain-4bc290-1-20-40')
for name,a,b,n in [('oldcap',0x10008000,0x1000E200,64),('factorycap',0x11008000,0x1100B000,48)]:
    args,_=build_case(c);mem=json.loads(args['memory'])
    code=compare_code(a,b,n)+bytes.fromhex('89f281ea')+pack32(a)+bytes.fromhex('0fb646ff0fb65fff')+END
    next(r for r in mem['regions'] if r['address']=='0x1000f000')['hex']=code.hex()
    args['memory']=json.dumps(mem);args['return_registers']='EAX,EBX,ECX,EDX,ESI,EDI,ESP'
    queries.append(dict(id='phase54-chain-debug-v2-'+name,tool='emulate_function',args=args,print_limit=1000))
Path('phase54-chain-debug-v2-queries.json').write_text(json.dumps(queries))
