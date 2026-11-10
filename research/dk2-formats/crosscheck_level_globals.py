"""Independent Ghidra full-context controls of rawLevel/Level/TPS/Freeze."""
from types import FunctionType,SimpleNamespace
import asyncio,json,os
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
import emulate_level_globals as native
import crosscheck_conversion_score as frozen
from emulate_element_colors import region
ROOT=native.ROOT
CONTROLS=['common30-level4','level4-common30','sound-source4']
def execute_for_Ghidra(c):
    args,ch,saved,file=native.execute(c);args=dict(args)
    regions=json.loads(args['memory'])['regions'];source=[(int(r['address'],16),bytes.fromhex(r['hex'])) for r in regions]
    groups=[]
    for a,b in sorted(source):
        end=a+len(b)
        if groups and a<groups[-1][1]:groups[-1][1]=max(groups[-1][1],end)
        else:groups.append([a,end])
    merged=[]
    for lo,hi in groups:
        data=bytearray(hi-lo)
        for a,b in source:
            if lo<=a and a+len(b)<=hi:data[a-lo:a-lo+len(b)]=b
        merged.append(region(lo,data))
    args['memory']=json.dumps(dict(regions=merged))
    return args,ch,saved,file

adapter=SimpleNamespace(cases=native.cases,execute=execute_for_Ghidra,verify=native.verify)
build=FunctionType(frozen.build.__code__,dict(vars(frozen),native=adapter,RESULT=0x1F400000,DRIVER=0x1F410000,SHADOW=0x21000000),'build')


async def main():
    params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    rows=[]
    async with stdio_client(params) as (rd,wr):
        async with ClientSession(rd,wr) as session:
            await session.initialize()
            for name in CONTROLS:
                args,n,size=build(name);tag=native.sha(json.dumps(args,sort_keys=True).encode())[:12];file=ROOT/'ghidra'/f'phase196-Level-Ghidra-{name}-{tag}.json'
                if file.exists():saved=native.load(str(file.relative_to(ROOT)));assert saved['input']==args
                else:
                    response=await session.call_tool('emulate_function',args);value=json.loads('\n'.join(x.text for x in response.content if x.type=='text'));saved=dict(input=args,result=value.get('data',value),tool_is_error=response.isError);native.dump(str(file.relative_to(ROOT)),saved)
                r=saved['result'];assert not saved['tool_is_error'] and r['success'] and r['hit_return'],(name,str(r)[:700])
                assert all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items()),(name,r['registers'])
                rows.append(dict(case=name,file=str(file.relative_to(ROOT)),sha256=native.sha(file.read_bytes()),checked_regions=n,checked_bytes=size));print(name,n,size,'Ghidra passed',flush=True)
    native.dump('phase196-Level-crosschecks.json',dict(scope=__doc__,replays=rows))


if __name__=='__main__':asyncio.run(main())
