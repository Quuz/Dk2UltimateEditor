"""Independent Ghidra controls for rawTrap destruction and pool coalescing."""
from pathlib import Path
import asyncio
import hashlib
import json
import os
import struct
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
import emulate_trap_removal as native
from emulate_element_colors import region,pack32
from emulate_hero_parties import compare_code

ROOT=Path(__file__).resolve().parent
CONTROLS=['trap1-active-65535','trap9-active-65526','world-gate']


def build(name):
    case=next(c for c in native.cases() if c['name']==name)
    args,checks,expected=native.probe.build(**native.inputs(case))
    saved=json.loads(native.path_for(args).read_text(encoding='utf8'))
    assert saved['input']==args and saved['case']==case and saved['expected']==expected
    wanted,_=native.verify(case,args,checks,expected,saved['result'])
    calls=args['before_calls'];first=calls[0];assert not first['stack_args']
    driver=bytearray()
    for call in calls[1:]+[dict(address=args['address'],registers=json.loads(args['registers']),stack_args=[])]:
        for v in reversed(call['stack_args']):driver+=b'\x68'+pack32(v)
        driver+=b'\xB9'+pack32(int(call['registers']['ECX'],0))+b'\xB8'+pack32(int(call['address'],0))+bytes.fromhex('ffd0')
    driver+=b'\xBD'+pack32(1)+b'\xBB'+pack32(0)
    regions=json.loads(args['memory'])['regions']
    stack=next(r for r in regions if int(r['address'],16)==0x7FFEF000);b=bytearray.fromhex(stack['hex'])
    struct.pack_into('<I',b,4096,native.probe.f.OBS);stack['hex']=b.hex()
    shadow=0x11000000
    for i,(address,blob) in enumerate(wanted.items()):
        regions.append(region(shadow,blob));driver+=compare_code(address,shadow,len(blob))+bytes.fromhex('85c07505bb')+pack32(i+1)+bytes.fromhex('21c5')
        shadow+=((len(blob)+255)//256)*256
    driver+=b'\x68'+pack32(0xDEADBEEF)+b'\xC3';regions.append(region(native.probe.f.OBS,driver))
    for k in ['before_calls','observe_reads','observe_writes','allowed_external_IO_code']:args.pop(k,None)
    args.update(address=first['address'],registers=json.dumps(dict(first['registers'],DF='0')),
        memory=json.dumps(dict(regions=regions)),max_steps=1000000,return_registers='EAX,EBP,EBX,ECX,EIP,ESP')
    return args,len(wanted),sum(len(b) for b in wanted.values())


async def main():
    params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),
        args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],
        env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    rows=[]
    async with stdio_client(params) as (rd,wr):
        async with ClientSession(rd,wr) as session:
            await session.initialize()
            for name in CONTROLS:
                args,count,size=build(name);tag=hashlib.sha256(json.dumps(args,sort_keys=True).encode()).hexdigest()[:12]
                path=ROOT/'ghidra'/f'phase120-Trap-Ghidra-{name}-{tag}.json'
                if path.exists():saved=json.loads(path.read_text(encoding='utf8'));assert saved['input']==args
                else:
                    response=await session.call_tool('emulate_function',args)
                    raw=json.loads('\n'.join(c.text for c in response.content if c.type=='text'))
                    saved=dict(input=args,result=raw.get('data',raw),tool_is_error=response.isError)
                    path.write_text(json.dumps(saved,indent=2),encoding='utf8')
                result=saved['result']
                assert not saved['tool_is_error'] and result['success'] and result['hit_return'],(name,result)
                assert all(int(result['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items()),(name,result)
                rows.append(dict(case=name,file=path.name,sha256=hashlib.sha256(path.read_bytes()).hexdigest(),checked_regions=count,checked_bytes=size))
                print(name,'Ghidra complete prepared contexts verified',flush=True)
    (ROOT/'phase120-trap-removal-crosschecks.json').write_text(json.dumps(dict(scope=__doc__,replays=rows),indent=2),encoding='utf8')


if __name__=='__main__':asyncio.run(main())
