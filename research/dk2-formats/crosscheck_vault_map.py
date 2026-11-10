"""Ghidra controls of complete prepared CMap vault replacement and mana chains."""
import asyncio
import hashlib
import json
import os
from pathlib import Path
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
import emulate_vault_map as native
from emulate_element_colors import region,pack32
from emulate_hero_parties import compare_code

ROOT=Path(__file__).resolve().parent
CONTROLS=['claim-existing','same-owner-cap80']


def build(name):
    c=next(c for c in native.cases() if c['name']==name)
    args,e,_,wanted=native.build(c);calls=args['before_calls'];first=calls[0]
    assert not first['stack_args']
    driver=bytearray()
    for call in calls[1:]+[dict(address=args['address'],registers=json.loads(args['registers']),stack_args=[])]:
        for value in reversed(call['stack_args']):driver+=b'\x68'+pack32(value)
        driver+=b'\xB9'+pack32(int(call['registers']['ECX'],0))
        driver+=b'\xB8'+pack32(int(call['address'],0))+bytes.fromhex('ffd0')
    driver+=b'\xBD'+pack32(1)+b'\xBB'+pack32(0)
    regions=json.loads(args['memory'])['regions'];shadow=0x11000000
    for i,(a,b) in enumerate(wanted.items()):
        regions.append(region(shadow,b));driver+=compare_code(a,shadow,len(b))+bytes.fromhex('85c07505bb')+pack32(i+1)+bytes.fromhex('21c5')
        shadow+=((len(b)+255)//256)*256
    driver+=b'\x68'+pack32(0xDEADBEEF)+b'\xC3';regions.append(region(native.f.OBS,driver))
    for key in ['before_calls','observe_reads','observe_writes','allowed_external_IO_code']:args.pop(key,None)
    args.update(address=first['address'],registers=json.dumps(dict(first['registers'],DF='0')),
                memory=json.dumps(dict(regions=regions)),max_steps=800000,return_registers='EAX,EBP,EBX,ECX,EIP,ESP')
    return args,e


async def main():
    params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),
        args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],
        env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    rows=[]
    async with stdio_client(params) as (rd,wr):
        async with ClientSession(rd,wr) as session:
            await session.initialize()
            for name in CONTROLS:
                args,e=build(name);path=ROOT/'ghidra'/f'phase112-map-Ghidra-{name}.json'
                if path.exists():
                    value=json.loads(path.read_text(encoding='utf8'));assert value['input']==args and value['expected']==e
                else:
                    response=await session.call_tool('emulate_function',args)
                    raw=json.loads('\n'.join(c.text for c in response.content if c.type=='text'))
                    value=dict(input=args,expected=e,result=raw.get('data',raw),tool_is_error=response.isError)
                    path.write_text(json.dumps(value,indent=2),encoding='utf8')
                r=value['result'];assert not value['tool_is_error'] and r['success'] and r['hit_return'],(name,r)
                assert all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items()),(name,r)
                rows.append(dict(case=name,file=path.name,sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
                print(name,'Ghidra verified',flush=True)
    (ROOT/'phase112-vault-map-crosschecks.json').write_text(json.dumps(dict(scope=__doc__,replays=rows),indent=2),encoding='utf8')


if __name__=='__main__':asyncio.run(main())
