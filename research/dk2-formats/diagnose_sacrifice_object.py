"""Provider-only snapshots of the failing temple fixture; native code intact."""
from pathlib import Path
import asyncio,json,os
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
import emulate_sacrifice_objects as t
import emulate_object_factory as f
from emulate_element_colors import pack32
ROOT=Path(__file__).resolve().parent
def vload(reg,address):return bytes.fromhex('c5fe6f')+bytes([5+8*reg])+pack32(address)
async def main():
    args,e,_=t.build(t.cases()[0],0);memory=json.loads(args['memory']);by={int(r['address'],16):r for r in memory['regions']};node=e['node']
    g=by[f.GFX_CB];g['hex']=(vload(0,node)+vload(1,node+32)+vload(2,node+0x120)+bytes.fromhex('c5fe6f1c24')+vload(4,f.WORLD)+bytes.fromhex(g['hex'])).hex()
    sound=by[f.SOUND_CB];sound['hex']=(vload(6,node+0x120)+bytes.fromhex('c5fe6f3c24')+bytes.fromhex(sound['hex'])).hex()
    args['memory']=json.dumps(memory);path=ROOT/'ghidra/phase89-temple-provider-diagnostic.json'
    if path.exists():saved=json.loads(path.read_text());assert saved['input']==args
    else:
        p=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
        async with stdio_client(p) as (rd,wr):
            async with ClientSession(rd,wr) as session:
                await session.initialize();response=await session.call_tool('emulate_function',args);r=json.loads('\n'.join(x.text for x in response.content if x.type=='text'))
                assert not response.isError and 'error' not in r;saved=dict(input=args,result=r.get('data',r));path.write_text(json.dumps(saved,indent=2),encoding='utf8')
    r=saved['result'];print('Final',r['final_pc'],r.get('emulation_error'))
    for i in range(8):print('YMM'+str(i),int(r['registers']['YMM'+str(i)],16).to_bytes(32,'little').hex())
if __name__=='__main__':asyncio.run(main())
