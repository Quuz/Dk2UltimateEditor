"""Independent Ghidra checks; reuse frozen driver's code with isolated globals."""
from types import FunctionType
import asyncio,json,os
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
import emulate_crypt_job_selection as native
import crosscheck_crypt_waiting as driver
ROOT=native.ROOT
build=FunctionType(driver.build.__code__,dict(vars(driver),native=native),'build')
CONTROLS=['preference-job28','preference-job0','preference-zero-weight','full-AI-preference28',
    'owner-flag4-blocks','preference-hate28','preference-high-job11c','source-happy-seed0','source-AI-seed17']

async def main():
    params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),
        args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    rows=[]
    async with stdio_client(params) as (rd,wr):
        async with ClientSession(rd,wr) as session:
            await session.initialize()
            for name in CONTROLS:
                a,n,size=build(name);tag=native.sha(json.dumps(a,sort_keys=True).encode())[:12]
                file=ROOT/'ghidra'/f'phase188-crypt-job-Ghidra-{name}-{tag}.json'
                if file.exists():saved=json.loads(file.read_text(encoding='utf-8'));assert saved['input']==a
                else:
                    response=await session.call_tool('emulate_function',a);v=json.loads('\n'.join(x.text for x in response.content if x.type=='text'))
                    saved=dict(input=a,result=v.get('data',v),tool_is_error=response.isError);file.write_text(json.dumps(saved,indent=2),encoding='utf-8')
                r=saved['result'];assert not saved['tool_is_error'] and r['success'] and r['hit_return'],(name,str(r)[:500])
                assert all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items()),(name,r['registers'])
                rows.append(dict(case=name,file=str(file.relative_to(ROOT)),sha256=native.sha(file.read_bytes()),checked_regions=n,checked_bytes=size))
                print(name,n,size,'Ghidra passed',flush=True)
    (ROOT/'phase188-crypt-job-crosschecks.json').write_text(json.dumps(dict(scope=__doc__,replays=rows),indent=2),encoding='utf-8')

if __name__=='__main__':asyncio.run(main())
