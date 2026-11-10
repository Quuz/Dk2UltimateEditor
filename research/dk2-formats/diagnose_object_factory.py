"""Read-only post-return diagnostic of the new factory fixture expectation."""
from pathlib import Path
import asyncio,json,os,struct
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
import emulate_object_factory as f
from emulate_element_colors import pack32
ROOT=Path(__file__).resolve().parent
async def main():
    c=f.cases()[0];args,e=f.build(c,0);memory=json.loads(args['memory'])
    addresses=[f.WORLD+32*i for i in range(4)]+[f.OUT,f.PLAYER,f.WORLD+0x7300,f.WORLD+0x15f0]
    observer=b'\xa3'+pack32(f.OUT)+b''.join(bytes.fromhex('c5fe6f')+bytes([5+8*i])+pack32(a) for i,a in enumerate(addresses))+b'\x68'+pack32(0xdeadbeef)+b'\xc3'
    next(r for r in memory['regions'] if int(r['address'],16)==f.OBS)['hex']=observer.hex();args['memory']=json.dumps(memory)
    path=ROOT/'ghidra/phase89-object-state-diagnostic.json'
    if path.exists():saved=json.loads(path.read_text());assert saved['input']==args
    else:
        p=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
        async with stdio_client(p) as (rd,wr):
            async with ClientSession(rd,wr) as session:
                await session.initialize();response=await session.call_tool('emulate_function',args);r=json.loads('\n'.join(x.text for x in response.content if x.type=='text'))
                assert not response.isError and 'error' not in r
                saved=dict(input=args,addresses=addresses,result=r.get('data',r));path.write_text(json.dumps(saved,indent=2),encoding='utf8')
    r=saved['result'];assert r['success'] and r['hit_return'];actual={a:int(r['registers']['YMM'+str(i)],16).to_bytes(32,'little') for i,a in enumerate(addresses)}
    print('OUT',struct.unpack('<4I',actual[f.OUT][:16]))
    world=b''.join(actual[f.WORLD+32*i] for i in range(4))
    for o in range(0,128):
        desired=bytes.fromhex(next(x for x in memory['regions'] if int(x['address'],16)==0x11000000)['hex'])
        if world[o]!=desired[o]:print('World mismatch',hex(o),'actual',world[o],'expected',desired[o])
if __name__=='__main__':asyncio.run(main())
