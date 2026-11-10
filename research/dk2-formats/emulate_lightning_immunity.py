"""Both original lightning handlers: immunity gate with captured damage calls.

Event and health-application helpers are explicit fixtures. The full handler,
target lookup, definition flag check and damage arithmetic execute unchanged.
"""
from pathlib import Path
import asyncio,hashlib,json,os,struct,sys
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
from emulate_element_colors import region,pack32
from emulate_placed_structures import load_abs
ROOT=Path(__file__).resolve().parent
CONFIG={
 'current':dict(program='DKII-DX.exe',handler=0x4AF1E0,event=0x4B32A0,health=0x4B2CE0,entities=0x6EE540,valid=0x6ED540,world_ptr=0x6E5050,terrain=0x6DA4A8,type_ptr=0x370,shot_def=0x125,target=0x147,damage=0x13D,timer=0x4E3,skip=0x3CB),
 'local13':dict(program='DKII-local13.exe',handler=0x4A60E0,event=0x4A9E20,health=0x4A98B0,entities=0x6D95C8,valid=0x6D85C8,world_ptr=0x6CFEE0,terrain=0x6CD1A0,type_ptr=0x36F,shot_def=0x124,target=0x146,damage=0x13C,timer=0x4D6,skip=0x3CA)}

def cases():
    rows=[]
    for version in CONFIG:
        for flags in [0,1,2,0x80000001]:
            for terrain in [0,0x80]:rows.append(dict(name=f'{version}-{flags}-{terrain}',version=version,flags=flags,terrain=terrain,damage=100,creature=21))
        rows.append(dict(name=f'{version}-negative-damage',version=version,flags=0,terrain=0,damage=-100,creature=21))
        for flags in [0,1]:rows.append(dict(name=f'{version}-other-creature-{flags}',version=version,flags=flags,terrain=0,damage=100,creature=7))
    return rows

def build_case(c):
    cfg=CONFIG[c['version']];shot=bytearray(0x160);node=bytearray(0x600);definition=bytearray(0x800);world=bytearray(0xA00);tile=bytearray(16);terrain=bytearray(0x190);shotdef=bytearray(0x60)
    struct.pack_into('<I',shot,cfg['shot_def'],0x10006000);struct.pack_into('<H',shot,0x28,3);struct.pack_into('<H',shot,cfg['target'],1);struct.pack_into('<i',shot,cfg['damage'],c['damage'])
    struct.pack_into('<H',node,4,1);struct.pack_into('<I',node,cfg['type_ptr'],0x10002000);struct.pack_into('<H',node,cfg['skip'],1)
    struct.pack_into('<I',definition,0x68A,c['flags']);definition[0x770]=c['creature'];struct.pack_into('<I',world,0x9E6,1);struct.pack_into('<I',world,0x9EE,0x10004000);tile[10]=1;terrain[0x17B]=c['terrain']
    # Capture [damage,source,zero] from actual stack arguments and targetECX.
    health=bytes.fromhex('ff05')+pack32(0x10007000)+bytes.fromhex('8b442404a3')+pack32(0x10007004)+bytes.fromhex('890d')+pack32(0x10007008)+bytes.fromhex('8b442408a3')+pack32(0x10007010)+bytes.fromhex('8b44240ca3')+pack32(0x10007014)+bytes.fromhex('c20c00')
    event=bytes.fromhex('ff05')+pack32(0x1000700C)+bytes.fromhex('c20c00')
    readout=load_abs(0,0x10007000)+load_abs(3,0x10007004)+load_abs(2,0x10007008)+load_abs(1,0x1000700C)+bytes.fromhex('0fb735')+pack32(0x10001000+cfg['timer'])+bytes.fromhex('0fb73d')+pack32(0x10007010)+load_abs(5,0x10007014)+b'\x68'+pack32(0xDEADBEEF)+b'\xC3'
    mem=[region(0x10000000,shot),region(0x10001000,node),region(0x10002000,definition),region(0x10003000,world),region(0x10004000,tile),region(0x10005000,terrain),region(0x10006000,shotdef),region(0x10007000,bytes(24)),region(0x10008000,readout),region(cfg['entities']+4,pack32(0x10001000)),region(cfg['valid'],bytes([0,1])),region(cfg['world_ptr'],pack32(0x10003000)),region(cfg['terrain']+4,pack32(0x10005000)),region(cfg['health'],health),region(cfg['event'],event),region(0x7FFF0000,pack32(0x10008000))]
    spans=sorted((int(r['address'],16),len(bytes.fromhex(r['hex']))) for r in mem);assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:]))
    immune=bool(c['flags']&1);delta=-abs(c['damage']*(2 if c['terrain']&0x80 else 1))
    expected=dict(EAX=int(not immune),EBX=0 if immune else delta&0xFFFFFFFF,ECX=1,EDX=0 if immune else 0x10001000,ESI=0 if immune else 20,EDI=0 if immune else 3,EBP=0)
    return dict(program=cfg['program'],address=hex(cfg['handler']),max_steps=10000,registers=json.dumps(dict(ECX='0x10000000')),memory=json.dumps(dict(regions=mem)),return_registers=','.join([*expected,'ESP','EIP'])),expected

async def main():
    selected=cases()
    if len(sys.argv)>1:selected=[c for c in selected if c['name']==sys.argv[1]];assert selected
    params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    manifest=[]
    async with stdio_client(params) as (rd,wr):
        async with ClientSession(rd,wr) as session:
            await session.initialize()
            for c in selected:
                args,want=build_case(c);p=ROOT/'ghidra'/('phase36-emulation-lightning-'+c['name']+'.json')
                if p.exists():saved=json.loads(p.read_text());assert saved['input']==args and saved['expected_registers']==want
                else:
                    response=await session.call_tool('emulate_function',args);r=json.loads('\n'.join(b.text for b in response.content if b.type=='text'))
                    if response.isError or 'error' in r:raise RuntimeError(str(r))
                    r=r.get('data',r);saved=dict(case=c,input=args,expected_registers=want,result=r);p.write_text(json.dumps(saved,indent=2),encoding='utf8')
                r=saved['result'];assert r['success'] and r['hit_return'],(c['name'],r);assert all(int(r['registers'][reg],16)==v for reg,v in want.items()),(c['name'],r,want)
                manifest.append(dict(file=p.name,sha256=hashlib.sha256(p.read_bytes()).hexdigest(),case=c['name']));print(c['name'],'verified',flush=True)
    if len(selected)==len(cases()):(ROOT/'phase36-lightning-emulations.json').write_text(json.dumps(dict(cases=manifest,scope=__doc__),indent=2),encoding='utf8')
if __name__=='__main__':asyncio.run(main())
