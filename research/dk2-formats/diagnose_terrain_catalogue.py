"""Diagnostic native buffer comparisons; not successful validation evidence."""
import asyncio,json
from emulate_terrain_catalogue import *

async def diagnostic():
 c=cases()[0];args,_=build_case(c);mm=json.loads(args['memory']);rs=mm['regions']
 params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
 async with stdio_client(params) as (rd,wr):
  async with ClientSession(rd,wr) as s:
   await s.initialize()
   for name,a,b,n in [('array',0x10100000,0x10200000,460*256),('world',0x10010000,0x10020000,0xA3CC),('mid',0x7470E8,0x10320000,736),('control',0x10003000,0x10321000,16),('ctor',0x10324000,0x10325000,460)]:
    out=bytes.fromhex('a3')+pack32(0x10003008)+b'\xB9'+pack32(0x10324000)+b'\xB8'+pack32(0x4F6570)+bytes.fromhex('ffd0')+compare_code(a,b,n)+bytes.fromhex('8b5efc8b57fc')+END
    regions=[region(int(r['address'],16),out) if int(r['address'],16)==0x10006000 else r for r in rs]
    aa={**args,'memory':json.dumps(dict(regions=regions)),'return_registers':'EAX,EBX,ECX,EDX,ESI,EDI,EIP'}
    response=await s.call_tool('emulate_function',aa);print(name,'\n'.join(x.text for x in response.content if x.type=='text'),flush=True)
asyncio.run(diagnostic())
