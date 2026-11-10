"""Complete old/DX Keeper definition lookups;whole world/table compared unchanged."""
from pathlib import Path
import asyncio,json,os,struct,hashlib
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
from native_trap_loading import DX,OLD
from emulate_element_colors import region,pack32
from emulate_hero_parties import compare_code

ROOT=Path(__file__).resolve().parent
PROFILES={DX:dict(program='DKII-DX.exe',getter=0x50DD60),OLD:dict(program='DKII-local13.exe',getter=0x5027A0)}


def cases():
    variants=[('empty',[],1,None),('zero-existing',[0,1,2],0,None),('first',[1,3,7],1,0),
        ('middle',[1,3,7],3,1),('last-high-id',[1,3,7],7,2),('missing-between',[1,3,7],2,None),
        ('missing-above',[1,3,7],8,None),('high-argument',[1,3,7],0x12340003,1),
        ('high-argument-zero',[1,3,7],0x100,None),('unsorted-hidden',[1,7,3],7,None),
        ('duplicate-index-limit',[1,1,3],1,0),('single-id255',[255],255,0),('id0-first-hides-1',[0,1,2],1,None)]
    return [dict(name=f'{sha[:3]}-{name}',sha=sha,ids=ids,key=key,wanted_index=wanted) for sha in PROFILES for name,ids,key,wanted in variants]


def build_case(c):
    p=PROFILES[c['sha']];world=bytearray((i*13+7)&255 for i in range(0x1700))
    struct.pack_into('<I',world,0x1604,0x10010000);struct.pack_into('<I',world,0x160C,len(c['ids']))
    table=bytearray((i*37+11)&255 for i in range(max(1,len(c['ids']))*122))
    for i,key in enumerate(c['ids']):table[122*i+0x75]=key;struct.pack_into('<I',table,122*i+0x3A,(i*0x80000001)&0xFFFFFFFF)
    native=0 if c['wanted_index'] is None else 0x10010000+c['wanted_index']*122
    observer=b'\xA3'+pack32(0x10030000)+b'\xBD'+pack32(1)
    mem=[]
    for a,b,initial in [(0x10000000,world,world),(0x10010000,table,table),(0x10030000,pack32(native),bytes(4))]:
        expected=a+0x40000;mem.extend([region(a,initial),region(expected,b)])
        observer+=compare_code(a,expected,len(b))+bytes.fromhex('21c5')
    observer+=b'\x68'+pack32(0xDEADBEEF)+b'\xC3'
    mem.extend([region(0x10080000,observer),region(0x7FFF0000,pack32(0x10080000)+pack32(c['key']))])
    spans=sorted((int(r['address'],16),len(bytes.fromhex(r['hex']))) for r in mem)
    assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:]))
    return dict(program=p['program'],address=hex(p['getter']),max_steps=8000,registers=json.dumps(dict(ECX='0x10000000')),
        memory=json.dumps(dict(regions=mem)),return_registers='EAX,ECX,EBP,ESP,EIP'),dict(EAX=1,ECX=0,EBP=1),native


async def main():
    params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),
        args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    rows=[]
    async with stdio_client(params) as (rd,wr):
        async with ClientSession(rd,wr) as session:
            await session.initialize()
            for c in cases():
                args,want,native=build_case(c);path=ROOT/'ghidra'/f'phase72-emulation-keeper-lookup-{c["name"]}.json'
                if path.exists():saved=json.loads(path.read_text());assert saved['input']==args and saved['expected_native_return']==native
                else:
                    response=await session.call_tool('emulate_function',args);r=json.loads('\n'.join(x.text for x in response.content if x.type=='text'))
                    if response.isError or 'error' in r:raise RuntimeError(str(r))
                    saved=dict(case=c,input=args,expected_registers=want,expected_native_return=native,result=r.get('data',r));path.write_text(json.dumps(saved,indent=2),encoding='utf8')
                r=saved['result'];assert r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in want.items()),(c['name'],r,native)
                rows.append(dict(file=path.name,sha256=hashlib.sha256(path.read_bytes()).hexdigest(),case=c['name']));print(c['name'],'verified',flush=True)
    (ROOT/'phase72-keeper-lookup-emulations.json').write_text(json.dumps(dict(cases=rows,scope=__doc__),indent=2),encoding='utf8')


if __name__=='__main__':asyncio.run(main())
