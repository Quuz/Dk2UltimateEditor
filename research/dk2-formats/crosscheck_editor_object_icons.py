"""Independent Ghidra CPU controls for Pro raw conversion and atlas projection."""
import asyncio, hashlib, json, os, struct
from pathlib import Path
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
import emulate_editor_object_icons as native
from emulate_hero_parties import compare_code

ROOT=Path(__file__).resolve().parent
CONTROLS=[('converter',65535),('atlas',9)]

def build(kind,value):
    c=native.build(kind,value);regions=list(c['memory']);obs=0x13000000
    stack=bytearray(8192)
    words=[obs]+c['stack']
    struct.pack_into('<'+'I'*len(words),stack,4096,*words)
    regions.append(native.region(native.STACK-4096,stack))
    code=bytearray.fromhex('bd01000000bb00000000');shadow=0x14000000
    for i,r in enumerate(c['expected']):
        address=int(r['address'],16);blob=bytes.fromhex(r['hex'])
        regions.append(native.region(shadow,blob));code+=compare_code(address,shadow,len(blob))+bytes.fromhex('85c07505bb')+native.pack(i+1)+bytes.fromhex('21c5')
        shadow+=(len(blob)+255)//256*256
    code+=bytes.fromhex('68efbeaddec3');regions.append(native.region(obs,code))
    args=dict(program='DK2 Editor pro.exe',address=hex(c['entry']),
        registers=json.dumps(dict(ECX=hex(c['ecx']),ESP=hex(native.STACK),DF='0')),
        memory=json.dumps(dict(regions=regions)),max_steps=100000,
        return_registers='EAX,EBP,EBX,ECX,EIP,ESP')
    return args,len(c['expected']),sum(len(bytes.fromhex(r['hex'])) for r in c['expected'])

async def main():
    params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),
        args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],
        env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8090'})
    rows=[]
    async with stdio_client(params) as (rd,wr):
        async with ClientSession(rd,wr) as session:
            await session.initialize()
            for kind,value in CONTROLS:
                args,count,size=build(kind,value);tag=hashlib.sha256(json.dumps(args,sort_keys=True).encode()).hexdigest()[:12]
                path=ROOT/'ghidra'/f'phase121-icon-Ghidra-{kind}-{value}-{tag}.json'
                if path.exists():
                    saved=json.loads(path.read_text(encoding='utf8'));assert saved['input']==args
                else:
                    response=await session.call_tool('emulate_function',args)
                    raw=json.loads('\n'.join(x.text for x in response.content if x.type=='text'))
                    saved=dict(input=args,result=raw.get('data',raw),tool_is_error=response.isError)
                    path.write_text(json.dumps(saved,indent=2),encoding='utf8')
                r=saved['result'];assert not saved['tool_is_error'] and r['success'] and r['hit_return'],r
                assert all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items()),r
                rows.append(dict(kind=kind,value=value,file=str(path.relative_to(ROOT)),sha256=native.sha(path.read_bytes()),checked_regions=count,checked_bytes=size))
                print(kind,value,'Ghidra passed',count,size,flush=True)
    (ROOT/'phase121-editor-icon-crosschecks.json').write_text(json.dumps(dict(scope=__doc__,replays=rows),indent=2),encoding='utf8')

if __name__=='__main__':asyncio.run(main())
