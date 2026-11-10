"""Independent Ghidra controls for whole light-render record allocation/queue."""
import asyncio,json,hashlib,os,struct
from pathlib import Path
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
import emulate_room_torch_light as native
from emulate_hero_parties import compare_code

ROOT=Path(__file__).resolve().parent
CONTROLS=['point-128','other-255']

def build(name):
    case=next(c for c in native.cases() if c['name']==name)
    args,expected=native.build(case);saved=json.loads(native.path_for(args).read_text(encoding='utf8'))
    assert saved['input']==args and saved['case']==case
    native.verify(case,args,expected,saved['result'])
    memory=json.loads(args['memory'])['regions'];stack=next(r for r in memory if int(r['address'],16)==0x7FFEF000)
    first=args['before_calls'][0];b=bytearray.fromhex(stack['hex'])
    words=[native.OBS]+first['stack_args'];struct.pack_into('<'+'I'*len(words),b,4096,*words);stack['hex']=b.hex()
    shadow=0x19000000;observer=bytearray.fromhex('bd01000000bb00000000')
    for i,(address,blob) in enumerate(expected.items()):
        memory.append(native.region(shadow,blob));observer+=compare_code(address,shadow,len(blob))+bytes.fromhex('85c07505bb')+native.p32(i+1)+bytes.fromhex('21c5')
        shadow+=(len(blob)+255)//256*256
    observer+=bytes.fromhex('68efbeaddec3');memory.append(native.region(native.OBS,observer))
    out=dict(program='DKII-DX.exe',address=first['address'],registers=json.dumps(dict(first['registers'],DF='0')),
        memory=json.dumps(dict(regions=memory)),max_steps=100000,return_registers='EAX,EBP,EBX,ECX,EIP,ESP')
    return out,len(expected),sum(map(len,expected.values()))

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
                path=ROOT/'ghidra'/f'phase123-light-Ghidra-{name}-{tag}.json'
                if path.exists():saved=json.loads(path.read_text(encoding='utf8'));assert saved['input']==args
                else:
                    response=await session.call_tool('emulate_function',args)
                    raw=json.loads('\n'.join(x.text for x in response.content if x.type=='text'))
                    saved=dict(input=args,result=raw.get('data',raw),tool_is_error=response.isError)
                    path.write_text(json.dumps(saved,indent=2),encoding='utf8')
                r=saved['result'];assert not saved['tool_is_error'] and r['success'] and r['hit_return'],r
                assert all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items()),r
                rows.append(dict(case=name,file=str(path.relative_to(ROOT)),sha256=native.sha(path.read_bytes()),checked_regions=count,checked_bytes=size))
                print(name,'Ghidra passed',count,size,flush=True)
    (ROOT/'phase123-light-render-crosschecks.json').write_text(json.dumps(dict(scope=__doc__,replays=rows),indent=2),encoding='utf8')

if __name__=='__main__':asyncio.run(main())
