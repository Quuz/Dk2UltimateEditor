"""Independent Ghidra full-context controls of original automatic file traversal."""
from types import FunctionType
import asyncio,json,os
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
import emulate_reference_loading as native
import crosscheck_variables_reader as frozen
ROOT=native.ROOT
CONTROLS=['common4-mapTPS30','mapTPS30-common4','missing-first','partial-child']
build=FunctionType(frozen.build.__code__,dict(vars(frozen),native=native,RESULT=0x1F600000,DRIVER=0x1F610000,SHADOW=0x24000000),'build')


async def main():
    params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    rows=[]
    async with stdio_client(params) as (rd,wr):
        async with ClientSession(rd,wr) as session:
            await session.initialize()
            for name in CONTROLS:
                args,n,size=build(name);tag=native.sha(json.dumps(args,sort_keys=True).encode())[:12];file=ROOT/'ghidra'/f'phase198-reference-Ghidra-{name}-{tag}.json'
                if file.exists():saved=native.load(str(file.relative_to(ROOT)));assert saved['input']==args
                else:
                    response=await session.call_tool('emulate_function',args);value=json.loads('\n'.join(x.text for x in response.content if x.type=='text'));saved=dict(input=args,result=value.get('data',value),tool_is_error=response.isError);native.dump(str(file.relative_to(ROOT)),saved)
                r=saved['result'];assert not saved['tool_is_error'] and r['success'] and r['hit_return'],(name,str(r)[:700])
                assert all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items()),(name,r['registers'])
                rows.append(dict(case=name,file=str(file.relative_to(ROOT)),sha256=native.sha(file.read_bytes()),checked_regions=n,checked_bytes=size));print(name,n,size,'Ghidra passed',flush=True)
    native.dump('phase198-reference-crosschecks.json',dict(scope=__doc__,replays=rows))


if __name__=='__main__':asyncio.run(main())
