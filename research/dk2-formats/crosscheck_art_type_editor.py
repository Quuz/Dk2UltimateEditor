"""Independent Ghidra CPU checks of ArtResource type-change and actual Room save."""
from pathlib import Path
import asyncio,json,os,struct,sys
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
from emulate_hero_parties import compare_code
import emulate_art_type_editor as native

ROOT=Path(__file__).resolve().parent;DRIVER=0x18000000;SHADOW=0x19000000
CONTROLS=['type-0-to7-stack5a','type-7-to7-stacka5','Room-complete-resource-0-to7-stacka5']
def p32(v):return struct.pack('<I',v)


def build(name):
    case=next(c for c in native.cases() if c['name']==name);fixture=native.build(case)
    path=native.path_for(fixture);saved=json.loads(path.read_text(encoding='utf8'));assert saved['input']==fixture
    native.verify(fixture,saved['result'])
    first=fixture['calls'][0];memory=list(fixture['memory'])
    stack=next(r for r in memory if int(r['address'],16)==native.STACK-0x4000)
    data=bytearray.fromhex(stack['hex']);data[0x4000:0x4000+4+len(first['stack'])*4]=b''.join(p32(v) for v in [DRIVER]+first['stack']);stack['hex']=data.hex()
    driver=bytearray()
    for call in fixture['calls'][1:]:
        driver+=b'\xbc'+p32(native.STACK+4+len(call['stack'])*4)+b'\xbd'+p32(native.STACK)
        driver+=bytes.fromhex('31c031db31c931d231f631ff')
        for value in reversed(call['stack']):driver+=b'\x68'+p32(value)
        driver+=b'\xb9'+p32(call['ecx'])+b'\xb8'+p32(call['address'])+bytes.fromhex('ffd0')
    driver+=bytes.fromhex('bd01000000bb00000000')
    shadow=SHADOW;size=0;count=0
    for row in saved['result']['output']:
        address=int(row['address'],16);blob=bytes.fromhex(row['hex'])
        memory.append(native.region(shadow,blob))
        driver+=compare_code(address,shadow,len(blob))+bytes.fromhex('85c07505bb')+p32(count+1)+bytes.fromhex('21c5')
        size+=len(blob);count+=1;shadow+=(len(blob)+255)//256*256
    driver+=bytes.fromhex('68efbeaddec3');memory.append(native.region(DRIVER,driver))
    args=dict(program='DK2 Editor pro.exe',address=hex(first['address']),
        registers=json.dumps(dict(ECX=hex(first['ecx']),ESP=hex(native.STACK),EBP=hex(native.STACK),DF='0')),
        memory=json.dumps(dict(regions=memory)),max_steps=6000000,return_registers='EAX,EBP,EBX,ECX,EIP,ESP')
    return args,count,size


async def main():
    server=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),
        args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8090'})
    rows=[];selected=[c for c in CONTROLS if len(sys.argv)==1 or c in sys.argv[1:]]
    async with stdio_client(server) as (rd,wr):
        async with ClientSession(rd,wr) as session:
            await session.initialize()
            for name in selected:
                args,count,size=build(name);tag=native.sha(json.dumps(args,sort_keys=True).encode())[:12]
                path=ROOT/'ghidra'/f'phase130-art-type-Ghidra-{name}-{tag}.json';print('Checking',name,count,size,flush=True)
                if path.exists():saved=json.loads(path.read_text(encoding='utf8'));assert saved['input']==args
                else:
                    response=await session.call_tool('emulate_function',args)
                    value=json.loads('\n'.join(c.text for c in response.content if c.type=='text'))
                    saved=dict(input=args,result=value.get('data',value),tool_is_error=response.isError)
                    path.write_text(json.dumps(saved,indent=2),encoding='utf8')
                result=saved['result']
                assert not saved['tool_is_error'] and result['success'] and result['hit_return'],(name,result)
                assert all(int(result['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items()),(name,result)
                rows.append(dict(case=name,file=str(path.relative_to(ROOT)),sha256=native.sha(path.read_bytes()),checked_regions=count,checked_bytes=size))
                print(name,'Ghidra passed',flush=True)
    if len(selected)==len(CONTROLS):
        (ROOT/'phase130-art-type-crosschecks.json').write_text(json.dumps(dict(scope=__doc__,replays=rows),indent=2),encoding='utf8')


if __name__=='__main__':asyncio.run(main())
