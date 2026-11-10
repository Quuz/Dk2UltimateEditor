"""Independent Ghidra register controls for the actual three-read helper bounds."""
from pathlib import Path
import asyncio,json,os
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
import emulate_crypt_counter_boundary as native
ROOT=Path(__file__).resolve().parent
CONTROLS=['crypt108b-1','crypt108b-255','slot2-active','slot0-highbit']

def build(name):
    c=next(c for c in native.cases() if c['name']==name);args,checks,saved,_=native.execute(c);native.verify(c,args,checks,saved['result'])
    args={k:v for k,v in args.items() if k not in ['observe_reads','observe_writes']}
    # Ghidra recognizes the DEADBEEF return sentinel. Unicorn stops before
    # STOP; Ghidra needs this explicit return driver, which preserves ESP.
    memory=json.loads(args['memory'])
    next(r for r in memory['regions'] if int(r['address'],16)==native.STOP)['hex']='68efbeaddec3'
    args['memory']=json.dumps(memory)
    return args,saved['result']['registers']

async def main():
    server=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    rows=[]
    async with stdio_client(server) as (rd,wr):
        async with ClientSession(rd,wr) as session:
            await session.initialize()
            for name in CONTROLS:
                args,wanted=build(name);tag=native.sha(json.dumps(args,sort_keys=True).encode())[:12]
                p=ROOT/'ghidra'/f'phase149-boundary-Ghidra-{name}-{tag}.json'
                if p.exists():out=json.loads(p.read_text(encoding='utf8'));assert out['input']==args
                else:
                    response=await session.call_tool('emulate_function',args);value=json.loads('\n'.join(x.text for x in response.content if x.type=='text'))
                    out=dict(input=args,result=value.get('data',value),tool_is_error=response.isError);p.write_text(json.dumps(out,indent=2),encoding='utf8')
                r=out['result'];assert not out['tool_is_error'] and r['success'] and r['hit_return']
                for key in ['EAX','ECX','ESP']:assert int(r['registers'][key],16)==int(wanted[key],16),(name,key,r['registers'])
                rows.append(dict(case=name,file=str(p.relative_to(ROOT)),sha256=native.sha(p.read_bytes()),checked_registers=['EAX','ECX','ESP']));print(name,'Ghidra passed',flush=True)
    (ROOT/'phase149-crypt-boundary-crosschecks.json').write_text(json.dumps(dict(scope=__doc__,replays=rows),indent=2),encoding='utf8')

if __name__=='__main__':asyncio.run(main())
