"""Full effective/raw Creature lookups with real494360 effective-key helper."""
from pathlib import Path
import asyncio,json,os,struct,hashlib
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
from emulate_element_colors import region,pack32
from emulate_hero_parties import compare_code
ROOT=Path(__file__).resolve().parent
ROUTES={'effective':0x50D390,'raw':0x50E3A0}
UNIQUE=0x08000000


def cases():
    ordinary=[(1,1,0),(3,3,0),(7,7,0)]
    unique_first=[(32,12,UNIQUE),(13,13,0)]
    unique_last=list(reversed(unique_first))
    variants=[
        ('empty',[],1,None,None),('first',ordinary,1,0,0),('sparse',ordinary,7,2,2),
        ('missing',ordinary,2,None,None),('zero',[(0,0,0),(1,1,0)],0,None,None),
        ('high-arg',ordinary,0x12340003,1,1),
        ('unique-first-own32',unique_first,32,None,None),
        ('unique-first-own13',unique_first,13,1,1),
        ('unique-first-clone12',unique_first,12,0,None),
        ('unique-last-own32',unique_last,32,None,1),
        ('unique-last-own13',unique_last,13,None,0),
        ('unique-last-clone12',unique_last,12,1,None),
        ('flag-off',[(32,12,0)],32,0,0),('single-unique-own',[(32,12,UNIQUE)],32,None,0),
        ('single-unique-clone',[(32,12,UNIQUE)],12,0,None),
        ('duplicate-key',[(32,1,UNIQUE),(1,1,0)],1,0,None),
        ('id0-hides1',[(0,0,0),(1,1,0),(2,2,0)],1,None,None),
    ]
    return [dict(name=route+'-'+name,route=route,rows=rows,key=key,wanted_index=effective if route=='effective' else raw)
        for route in ROUTES for name,rows,key,effective,raw in variants]


def build_case(c):
    world=bytearray((i*13+7)&255 for i in range(0x1700));struct.pack_into('<I',world,0x15C4,0x10010000);struct.pack_into('<I',world,0x15CC,len(c['rows']))
    table=bytearray((i*37+11)&255 for i in range(max(1,len(c['rows']))*2199))
    for i,(own,clone,flags) in enumerate(c['rows']):
        at=2199*i;table[at+0x770]=own;table[at+0x5B3]=clone;struct.pack_into('<I',table,at+0x686,flags)
        table[at+0x6EB]=(i*73+19)&255;struct.pack_into('<I',table,at+0x75C,(i*0x80000001)&0xFFFFFFFF)
    native=0 if c['wanted_index'] is None else 0x10010000+c['wanted_index']*2199
    observer=b'\xA3'+pack32(0x10030000)+b'\xBD'+pack32(1);mem=[]
    for a,b,initial in [(0x10000000,world,world),(0x10010000,table,table),(0x10030000,pack32(native),bytes(4))]:
        expected=a+0x40000;mem.extend([region(a,initial),region(expected,b)])
        observer+=compare_code(a,expected,len(b))+bytes.fromhex('21c5')
    observer+=b'\x68'+pack32(0xDEADBEEF)+b'\xC3';mem.extend([region(0x10080000,observer),region(0x7FFF0000,pack32(0x10080000)+pack32(c['key']))])
    spans=sorted((int(r['address'],16),len(bytes.fromhex(r['hex']))) for r in mem);assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:]))
    return dict(program='DKII-DX.exe',address=hex(ROUTES[c['route']]),max_steps=12000,registers=json.dumps(dict(ECX='0x10000000')),
        memory=json.dumps(dict(regions=mem)),return_registers='EAX,ECX,EBP,ESP,EIP'),dict(EAX=1,ECX=0,EBP=1),native


async def main():
    params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    rows=[]
    async with stdio_client(params) as (rd,wr):
        async with ClientSession(rd,wr) as session:
            await session.initialize()
            for c in cases():
                args,want,native=build_case(c);path=ROOT/'ghidra'/f'phase73-emulation-creature-lookup-{c["name"]}.json'
                if path.exists():saved=json.loads(path.read_text());assert saved['input']==args and saved['expected_native_return']==native
                else:
                    response=await session.call_tool('emulate_function',args);r=json.loads('\n'.join(x.text for x in response.content if x.type=='text'))
                    if response.isError or 'error' in r:raise RuntimeError(str(r))
                    saved=dict(case=c,input=args,expected_registers=want,expected_native_return=native,result=r.get('data',r));path.write_text(json.dumps(saved,indent=2),encoding='utf8')
                r=saved['result'];assert r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in want.items()),(c['name'],r,native)
                rows.append(dict(file=path.name,sha256=hashlib.sha256(path.read_bytes()).hexdigest(),case=c['name']));print(c['name'],'verified',flush=True)
    (ROOT/'phase73-creature-lookup-emulations.json').write_text(json.dumps(dict(cases=rows,scope=__doc__),indent=2),encoding='utf8')


if __name__=='__main__':asyncio.run(main())
