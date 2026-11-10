"""Test post-return wide-register readout;never changes native program bytes."""
from pathlib import Path
import asyncio,json,os,struct
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
from emulate_element_colors import region,pack32
ROOT=Path(__file__).resolve().parent
async def main():
    params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    async with stdio_client(params) as (rd,wr):
      async with ClientSession(rd,wr) as session:
        await session.initialize()
        for name,prefix,width in [('ymm',bytes.fromhex('c5fe6f'),32),('zmm',bytes.fromhex('62f1fe486f'),64)]:
            data=bytes((i*7+3)&255 for i in range(width*8));observer=b''.join(prefix+bytes([5+8*i])+pack32(0x10010000+i*width) for i in range(8))+b'\x68'+pack32(0xDEADBEEF)+b'\xC3'
            args=dict(program='DKII-DX.exe',address='0x504d60',registers=json.dumps(dict(ECX='0x10020000')),memory=json.dumps(dict(regions=[region(0x10020000,bytes([165])*27),region(0x10010000,data),region(0x10000000,observer),region(0x7FFF0000,pack32(0x10000000))])),return_registers='EAX,ECX,EIP,'+','.join(name.upper()+str(i) for i in range(8)))
            response=await session.call_tool('emulate_function',args);r=json.loads('\n'.join(x.text for x in response.content if x.type=='text'));r=r.get('data',r)
            (ROOT/'ghidra'/f'phase85-wide-readout-{name}.json').write_text(json.dumps(dict(input=args,result=r),indent=2),encoding='utf8')
            success=r.get('success') and r.get('hit_return')
            if success:
                got=b''.join(int(r['registers'][name.upper()+str(i)],16).to_bytes(width,'little') for i in range(8));success=got==data
            print(name,'supported',bool(success),'native_return',r.get('registers',{}).get('EAX'),'error',r.get('error',''),flush=True)
if __name__=='__main__':asyncio.run(main())
