"""Finite post-return byte-diff observer for the prepared owner expectation."""
from pathlib import Path
import asyncio,json,os,struct
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
import emulate_creature_factory as f
import memory_readout_ymm as readout
from emulate_element_colors import region,pack32
ROOT=Path(__file__).resolve().parent;DIAG=0x100b0000
async def main():
    args,e=f.build(f.cases()[0],0);memory=json.loads(args['memory']);code=bytearray(b'\xbe'+pack32(f.f.PLAYER)+b'\xbf'+pack32(0x11008000)+b'\xb9'+pack32(0x1200)+b'\xbd'+pack32(DIAG));labels={};fix=[]
    def jump(op,label):code.extend(op);fix.append((len(code),label));code.extend(bytes(4))
    labels['loop']=len(code);code.extend(bytes.fromhex('f3a6'));jump(bytes.fromhex('0f84'),'done')
    code.extend(b'\xb8'+pack32(0x1200)+bytes.fromhex('29c8480fb656ffc1e21009d00fb657ffc1e21809d089450083c50481fd')+pack32(DIAG+252))
    jump(bytes.fromhex('0f83'),'done');code.extend(bytes.fromhex('85c9'));jump(bytes.fromhex('0f85'),'loop');labels['done']=len(code)
    code.extend(readout.code(DIAG)+b'\x68'+pack32(0xdeadbeef)+b'\xc3')
    for off,label in fix:struct.pack_into('<i',code,off,labels[label]-(off+4))
    next(r for r in memory['regions'] if int(r['address'],16)==f.f.OBS)['hex']=code.hex();memory['regions'].append(region(DIAG,bytes(256)));args['memory']=json.dumps(memory)
    path=ROOT/'ghidra/phase90-owner-differences-diagnostic.json'
    if path.exists():saved=json.loads(path.read_text());assert saved['input']==args
    else:
        p=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
        async with stdio_client(p) as (rd,wr):
            async with ClientSession(rd,wr) as session:
                await session.initialize();response=await session.call_tool('emulate_function',args);r=json.loads('\n'.join(x.text for x in response.content if x.type=='text'));assert not response.isError and 'error' not in r
                saved=dict(input=args,result=r.get('data',r));path.write_text(json.dumps(saved,indent=2),encoding='utf8')
    r=saved['result'];assert r['success'] and r['hit_return'],r
    b=readout.decode(r['registers']);rows=[]
    for i in range(64):
        offset,actual,expected=struct.unpack_from('<HBB',b,4*i)
        if not (offset or actual or expected):break
        rows.append(dict(offset=hex(offset),actual=actual,expected=expected))
    (ROOT/'phase90-owner-differences.json').write_text(json.dumps(rows,indent=2),encoding='utf8');print(json.dumps(rows))
if __name__=='__main__':asyncio.run(main())
