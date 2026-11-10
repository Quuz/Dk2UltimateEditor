"""Ghidra CPU controls for direct hit, both AoE branches and Boulder filtering."""
import asyncio
import hashlib
import json
import os
import struct
from pathlib import Path
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
import emulate_shot_damage as native
from emulate_element_colors import region,pack32
from emulate_hero_parties import compare_code

ROOT=Path(__file__).resolve().parent
CONTROLS=['hit-res100','area-res100','range-half','boulder-fire','boulder-projectile']


def build(name):
    c=next(c for c in native.probe.cases() if c['name']==name)
    args,_=native.probe.build(c);wanted=native.expected(c);calls=args['before_calls'];first=calls[0]
    assert not first['stack_args'];driver=bytearray()
    for call in calls[1:]+[dict(address=args['address'],registers=json.loads(args['registers']),stack_args=[])]:
        for v in reversed(call['stack_args']):driver+=b'\x68'+pack32(v)
        driver+=b'\xB9'+pack32(int(call['registers']['ECX'],0))+b'\xB8'+pack32(int(call['address'],0))+bytes.fromhex('ffd0')
    driver+=b'\xBD'+pack32(1)+b'\xBB'+pack32(0)
    regions=json.loads(args['memory'])['regions']
    stack=next(r for r in regions if int(r['address'],16)==0x7FFEE000);b=bytearray.fromhex(stack['hex'])
    struct.pack_into('<I',b,8192,native.probe.f.OBS);stack['hex']=b.hex()
    shadow=0x11000000
    for i,(a,b) in enumerate(wanted.items()):
        regions.append(region(shadow,b));driver+=compare_code(a,shadow,len(b))+bytes.fromhex('85c07505bb')+pack32(i+1)+bytes.fromhex('21c5')
        shadow+=((len(b)+255)//256)*256
    driver+=b'\x68'+pack32(0xDEADBEEF)+b'\xC3';regions.append(region(native.probe.f.OBS,driver))
    for key in ['before_calls','observe_reads','observe_writes','allowed_external_IO_code']:args.pop(key,None)
    args.update(address=first['address'],registers=json.dumps(dict(first['registers'],DF='0')),
                memory=json.dumps(dict(regions=regions)),max_steps=1000000,return_registers='EAX,EBP,EBX,ECX,EIP,ESP')
    return args


async def main():
    params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),
        args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],
        env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    rows=[]
    async with stdio_client(params) as (rd,wr):
        async with ClientSession(rd,wr) as session:
            await session.initialize()
            for name in CONTROLS:
                args=build(name);tag=hashlib.sha256(json.dumps(args,sort_keys=True).encode()).hexdigest()[:12]
                p=ROOT/'ghidra'/f'phase114-shot-Ghidra-{name}-{tag}.json'
                if p.exists():v=json.loads(p.read_text(encoding='utf8'));assert v['input']==args
                else:
                    response=await session.call_tool('emulate_function',args)
                    raw=json.loads('\n'.join(c.text for c in response.content if c.type=='text'))
                    v=dict(input=args,result=raw.get('data',raw),tool_is_error=response.isError)
                    p.write_text(json.dumps(v,indent=2),encoding='utf8')
                r=v['result'];assert not v['tool_is_error'] and r['success'] and r['hit_return'],(name,r)
                assert all(int(r['registers'][k],16)==n for k,n in dict(EAX=1,EBP=1,EBX=0,ECX=0).items()),(name,r)
                rows.append(dict(case=name,file=p.name,sha256=hashlib.sha256(p.read_bytes()).hexdigest()))
                print(name,'Ghidra whole-context verified',flush=True)
    (ROOT/'phase114-shot-damage-crosschecks.json').write_text(json.dumps(dict(scope=__doc__,replays=rows),indent=2),encoding='utf8')


if __name__=='__main__':asyncio.run(main())
