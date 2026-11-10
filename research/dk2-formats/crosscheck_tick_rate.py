"""Independent full-context Ghidra controls of Variables19 and Freeze."""
from pathlib import Path
from types import FunctionType,SimpleNamespace
import asyncio,json,os
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
import emulate_tick_rate as native
import crosscheck_conversion_score as frozen
from emulate_element_colors import region
from probe_melee_animation_flag import coalesce_world
ROOT=Path(__file__).resolve().parent
CONTROLS=['zero','four','twenty','all-ones']
def execute_for_Ghidra(c):
    args,ch,saved,file=native.execute(c);args=dict(args)
    mem={int(r['address'],16):bytearray.fromhex(r['hex']) for r in json.loads(args['memory'])['regions']}
    # Preserve the native backend's ordered last-write-wins World input in
    # one physical region; no overlapping Ghidra fixture views remain.
    coalesce_world(mem)
    args['memory']=json.dumps(dict(regions=[region(a,b) for a,b in mem.items()]))
    return args,ch,saved,file

adapter=SimpleNamespace(cases=native.cases,execute=execute_for_Ghidra,verify=native.verify)
build=FunctionType(frozen.build.__code__,dict(vars(frozen),native=adapter,RESULT=0x1F200000,DRIVER=0x1F210000,SHADOW=0x20000000),'build')


async def main():
    rows=[]
    params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    async with stdio_client(params) as (rd,wr):
        async with ClientSession(rd,wr) as session:
            await session.initialize()
            for name in CONTROLS:
                args,n,size=build(name);tag=native.sha(json.dumps(args,sort_keys=True).encode())[:12]
                file=ROOT/'ghidra'/f'phase195-tps-Ghidra-{name}-{tag}.json'
                if file.exists():saved=native.load(str(file.relative_to(ROOT)));assert saved['input']==args
                else:
                    response=await session.call_tool('emulate_function',args);v=json.loads('\n'.join(x.text for x in response.content if x.type=='text'))
                    saved=dict(input=args,result=v.get('data',v),tool_is_error=response.isError);native.dump(str(file.relative_to(ROOT)),saved)
                r=saved['result'];assert not saved['tool_is_error'] and r['success'] and r['hit_return'],(name,str(r)[:400])
                assert all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items()),(name,r['registers'])
                rows.append(dict(case=name,file=str(file.relative_to(ROOT)),sha256=native.sha(file.read_bytes()),checked_regions=n,checked_bytes=size))
                print(name,n,size,'Ghidra passed',flush=True)
    native.dump('phase195-tps-crosschecks.json',dict(scope=__doc__,replays=rows))


if __name__=='__main__':asyncio.run(main())
