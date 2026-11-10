"""Ghidra CPU controls against complete prepared outputs of original native paths.

Whole opaque buffers are measured Unicorn outputs, not independently decoded
geometry. The second CPU backend verifies those bytes. No original code patch.
"""
from pathlib import Path
import asyncio, hashlib, json, os, struct, sys
from mcp import ClientSession, StdioServerParameters
from mcp.client.stdio import stdio_client
import emulate_terrain_opaque_table as native
from emulate_resistance import post_context
from emulate_hero_parties import compare_code
from emulate_element_colors import region, pack32

ROOT=Path(__file__).resolve().parent
DRIVER, SHADOW=0x19000000,0x1A000000
CONTROLS=['DX-0-ascending','DX-1-highbit','old-0-ascending','old-0-highbit']


def build(name):
    case=next(c for c in native.cases() if c['name']==name)
    args,checks=native.build(case)
    saved=json.loads(native.path_for(args).read_text(encoding='utf8'))
    assert saved['input']==args
    native.verify(case,args,checks,saved['result'])
    wanted=post_context(checks,saved['result'])
    memory=json.loads(args['memory'])['regions']
    stack=next(r for r in memory if int(r['address'],16)==0x7FFEE000)
    data=bytearray.fromhex(stack['hex']);struct.pack_into('<I',data,0x2000,DRIVER);stack['hex']=data.hex()
    first=args['before_calls'][0]
    assert not first['stack_args']
    driver=bytearray()
    calls=args['before_calls'][1:]+[dict(address=args['address'],registers=json.loads(args['registers']),stack_args=[])]
    for call in calls:
        # Same explicit zero caller register inputs; preserve actual native callees.
        driver+=b'\xbc'+pack32(0x7FFF0004+4*len(call['stack_args']))+b'\xbd'+pack32(0x7FFF0000)
        driver+=bytes.fromhex('31c031db31c931d231f631ff')
        for value in reversed(call['stack_args']):driver+=b'\x68'+pack32(value)
        driver+=b'\xb9'+pack32(int(call['registers']['ECX'],0))+b'\xb8'+pack32(int(call['address'],0))+bytes.fromhex('ffd0')
    driver+=bytes.fromhex('bd01000000bb00000000')
    shadow=SHADOW
    for index,(address,blob) in enumerate(wanted.items()):
        memory.append(region(shadow,blob))
        driver+=compare_code(address,shadow,len(blob))+bytes.fromhex('85c07505bb')+pack32(index+1)+bytes.fromhex('21c5')
        shadow+=(len(blob)+255)//256*256
    driver+=bytes.fromhex('68efbeaddec3')
    memory.append(region(DRIVER,driver))
    params=dict(program=native.PROFILES[case['profile']]['program'],address=first['address'],registers=json.dumps(dict(first['registers'],DF='0',FPUControlWord='0x37f',FPUStatusWord='0')),
        memory=json.dumps(dict(regions=memory)),max_steps=6000000,return_registers='EAX,EBP,EBX,ECX,EIP,ESP')
    return params,len(wanted),sum(map(len,wanted.values()))


async def main():
    selected=[n for n in CONTROLS if len(sys.argv)==1 or n in sys.argv[1:]]
    rows=[]
    for profile,port in [('DX',8089),('old',8091)]:
        names=[n for n in selected if n.startswith(profile+'-')]
        if not names:continue
        server=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),
            args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],
            env={**os.environ,'GHIDRA_MCP_URL':f'http://127.0.0.1:{port}'})
        async with stdio_client(server) as (rd,wr):
            async with ClientSession(rd,wr) as session:
                await session.initialize()
                for name in names:
                    args,count,size=build(name)
                    tag=native.sha(json.dumps(args,sort_keys=True).encode())[:12]
                    path=ROOT/'ghidra'/f'phase134-Terrain-table-Ghidra-{name}-{tag}.json'
                    print('Checking',name,count,size,flush=True)
                    if path.exists():
                        saved=json.loads(path.read_text(encoding='utf8'));assert saved['input']==args
                    else:
                        result=await session.call_tool('emulate_function',args)
                        parsed=json.loads('\n'.join(c.text for c in result.content if c.type=='text'))
                        saved=dict(input=args,result=parsed.get('data',parsed),tool_is_error=result.isError)
                        path.write_text(json.dumps(saved,indent=2),encoding='utf8')
                    r=saved['result']
                    assert not saved['tool_is_error'] and r['success'] and r['hit_return'],(name,r)
                    assert all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items()),(name,r)
                    rows.append(dict(case=name,file=str(path.relative_to(ROOT)),sha256=native.sha(path.read_bytes()),checked_regions=count,checked_bytes=size))
                    print(name,'Ghidra passed',flush=True)
    if len(selected)==len(CONTROLS):
        (ROOT/'phase134-Terrain-table-crosschecks.json').write_text(json.dumps(dict(scope=__doc__,replays=rows),indent=2),encoding='utf8')


if __name__=='__main__':asyncio.run(main())
