"""Full Shot factory+constructor+initializer under explicit base/resource fixtures.

Native4B7CD0,4B7D60,4AAC90 and simple402AE0/40CFD0/4AAF60/40D640/
4B49D0 execute. Allocator, base entity/movement/ownership, render attachment,
range setter and sound provider are controlled; resource success branches not run.
"""
from pathlib import Path
import asyncio,hashlib,json,os,struct,sys
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
from emulate_element_colors import region,pack32
from emulate_placed_objects import return_const
from emulate_hero_parties import compare_code
from native_shot_creation import projected_node
ROOT=Path(__file__).resolve().parent
END=b'\x68'+pack32(0xDEADBEEF)+b'\xC3'
def cases():
    rows=[]
    for ident in [1,21,255]:
        for seed in [0,165]:
            for sound in [0,1]:rows.append(dict(name=f'create-{ident}-{seed}-{sound}',ident=ident,seed=seed,sound=sound,exists=1,gate=1,allocate=1))
    for stop in ['exists','gate','allocate']:
        c=dict(name=f'fail-{stop}',ident=21,seed=165,sound=1,exists=1,gate=1,allocate=1);c[stop]=0;rows.append(c)
    rows.append(dict(name='zero-id-rejected',ident=0,seed=165,sound=1,exists=0,gate=1,allocate=1))
    from chain_keeper_shot_creation import cases as chain_cases
    return rows+chain_cases()
def build_case(c):
    if c.get('kind')=='chain':
        from chain_keeper_shot_creation import build_case as chain_case
        return chain_case(c)
    definition=bytearray(0x80);struct.pack_into('<II',definition,0x37,0x11223344,0x55667788);struct.pack_into('<HHHH',definition,0x47,65535,0,0xABCD,0xFEDC);definition[0x6C]=9;struct.pack_into('<I',definition,0x72,0x76543210)
    world=bytearray(0x50);struct.pack_into('<I',world,0,0x10001000);struct.pack_into('<I',world,4,0x10002000);struct.pack_into('<I',world,0x1C,0x10000000);struct.pack_into('<I',world,0x36,5)
    vt=bytearray(0x1B4);struct.pack_into('<I',vt,0x1AC,0x10010000);struct.pack_into('<I',vt,0x1B0,0x10010100)
    graphics=bytearray(0x14);struct.pack_into('<I',graphics,0x10,0x10002100);gfxvt=bytearray(0xA4);struct.pack_into('<I',gfxvt,0xA0,0x10010200)
    node=bytes([c['seed']])*0x173;success=bool(c['exists'] and c['gate'] and c['allocate']);expected=projected_node(c['seed'],c['ident'],definition,0x10003000,sound_enabled=c['sound']) if success else node
    capture=bytearray(0x30);struct.pack_into('<II',capture,0,1,c['ident']);allocation=bool(c['exists'] and c['gate'])
    if allocation:struct.pack_into('<II',capture,8,1,0x173)
    if success:
        struct.pack_into('<II',capture,0x10,1,c['ident']);struct.pack_into('<I',capture,0x18,0xABCD);struct.pack_into('<I',capture,0x1C,1+int(bool(c['sound'])));struct.pack_into('<I',capture,0x20,7);struct.pack_into('<I',capture,0x24,0xF2)
    exists=bytes.fromhex('ff05')+pack32(0x10008000)+bytes.fromhex('0fb6442404a3')+pack32(0x10008004)+return_const(c['exists'],4)
    lookup=bytes.fromhex('ff05')+pack32(0x10008010)+bytes.fromhex('0fb6442404a3')+pack32(0x10008014)+return_const(0x10003000,4)
    alloc=bytes.fromhex('ff05')+pack32(0x10008008)+bytes.fromhex('8b442404a3')+pack32(0x1000800C)+return_const(0x10005000 if c['allocate'] else 0,4)
    base=bytes.fromhex('66c7410434128bc1c3')
    owner=bytes.fromhex('0fb744240466894128a3')+pack32(0x10008020)+bytes.fromhex('c20400')
    position=bytes.fromhex('56578b74240c8d791ab903000000f3a55f5ec20400')
    speed=bytes.fromhex('8b442404a3')+pack32(0x10008018)+bytes.fromhex('c20400')
    sound=bytes.fromhex('ff05')+pack32(0x1000801C)+bytes.fromhex('8b44240ca3')+pack32(0x10008024)+return_const(0x13572468,16)
    soundvt=bytearray(0x24);struct.pack_into('<I',soundvt,0x20,0x10010300)
    wantedworld=bytearray(world);struct.pack_into('<I',wantedworld,0x36,5+int(success))
    readout=b'\xA3'+pack32(0x10008028)+b'\xBD'+pack32(1)
    # Include native factory result in capture; otherwise the compared arrays have no observer writes.
    struct.pack_into('<I',capture,0x28,int(success))
    for a,b,n in [(0x10005000,0x10009000,0x173),(0x10000000,0x1000A000,len(world)),(0x10008000,0x1000B000,len(capture)),(0x10007000,0x1000C000,4)]:readout+=compare_code(a,b,n)+bytes.fromhex('21c5')
    readout+=END
    mem=[region(0x10000000,world),region(0x10001000,vt),region(0x10002000,graphics),region(0x10002100,pack32(0x10002200)),region(0x10002200,gfxvt),region(0x10003000,definition),region(0x10004000,struct.pack('<3I',0x1200,0xFFFFDE00,0x3300)+struct.pack('<H',0x4321)),region(0x10005000,node),region(0x10006000,readout),region(0x10007000,pack32(0xCAFEBABE)),region(0x10008000,bytes(0x30)),region(0x10009000,expected),region(0x1000A000,wantedworld),region(0x1000B000,capture),region(0x1000C000,pack32(0x10005000 if success else 0xCAFEBABE)),region(0x10010000,exists),region(0x10010100,lookup),region(0x10010200,return_const(0,20)),region(0x10010300,sound),region(0x10010400,pack32(0x10010500)),region(0x10010500,soundvt),region(0x10010600,speed),region(0x554B30,alloc),region(0x507850,base),region(0x4B3610,b'\xC3'),region(0x4B1DF0,bytes.fromhex('c20400')),region(0x4B4C30,owner),region(0x4B5020,position),region(0x4B4E70,bytes.fromhex('c20400')),region(0x4B37D0,bytes.fromhex('c21000')),region(0x66D86C+0x8C,pack32(0x10010600)),region(0x6E5050,pack32(0x10000000)),region(0x6A4818,pack32(c['gate'])),region(0x75B468,pack32(0x10010400)),region(0x7FFF0000,pack32(0x10006000)+pack32(c['ident'])+pack32(7)+pack32(0x10004000)+pack32(0x1000400C)+pack32(0x10007000)+pack32(c['sound']))]
    spans=sorted((int(r['address'],16),len(bytes.fromhex(r['hex']))) for r in mem);assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:])),c['name']
    return dict(program='DKII-DX.exe',address='0x4b7cd0',max_steps=10000,registers=json.dumps(dict(ECX='0x10000008')),memory=json.dumps(dict(regions=mem)),return_registers='EAX,ECX,EBP,ESP,EIP'),dict(EAX=1,ECX=0,EBP=1)
async def main():
    selected=cases()
    if len(sys.argv)>1:selected=[c for c in selected if c['name']==sys.argv[1]];assert selected
    params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    manifest=[]
    async with stdio_client(params) as (rd,wr):
        async with ClientSession(rd,wr) as session:
            await session.initialize()
            for c in selected:
                args,want=build_case(c);p=ROOT/'ghidra'/('phase54-emulation-shot-create-v3-'+c['name']+'.json')
                if p.exists():saved=json.loads(p.read_text());assert saved['input']==args and saved['expected_registers']==want
                else:
                    response=await session.call_tool('emulate_function',args);r=json.loads('\n'.join(b.text for b in response.content if b.type=='text'))
                    if response.isError or 'error' in r:raise RuntimeError(str(r))
                    r=r.get('data',r);saved=dict(case=c,input=args,expected_registers=want,result=r);p.write_text(json.dumps(saved,indent=2),encoding='utf8')
                r=saved['result'];assert r['success'] and r['hit_return'],(c['name'],r);assert all(int(r['registers'][reg],16)==v for reg,v in want.items()),(c['name'],r,want)
                manifest.append(dict(file=p.name,sha256=hashlib.sha256(p.read_bytes()).hexdigest(),case=c['name']));print(c['name'],'verified',flush=True)
    if len(selected)==len(cases()):(ROOT/'phase54-shot-create-emulations.json').write_text(json.dumps(dict(cases=manifest,scope=__doc__),indent=2),encoding='utf8')
if __name__=='__main__':asyncio.run(main())
