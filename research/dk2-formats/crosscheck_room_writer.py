"""Independent Ghidra replay of complete Room writer with native date helpers."""
import asyncio,hashlib,json,os,struct
from pathlib import Path
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
import emulate_room_writer as native
from emulate_hero_parties import compare_code

ROOT=Path(__file__).resolve().parent
CONTROLS=[dict(seed=165,unknown='ones'),dict(slots=[1,0,2],seed='pattern',flags=0xFFFFFFFF)]

def build(case):
    c=native.build(**case);regions=json.loads(json.dumps(c['memory']));obs=0x16000000;shadow=0x17000000
    stack=next(r for r in regions if int(r['address'],16)==native.STACK-0x4000)
    b=bytearray.fromhex(stack['hex']);struct.pack_into('<I',b,0x4000,obs);stack['hex']=b.hex()
    code=bytearray.fromhex('bd01000000bb00000000')
    for i,r in enumerate(c['expected']):
        blob=bytes.fromhex(r['hex']);regions.append(native.region(shadow,blob))
        code+=compare_code(int(r['address'],16),shadow,len(blob))+bytes.fromhex('85c07505bb')+native.p32(i+1)+bytes.fromhex('21c5')
        shadow+=(len(blob)+255)//256*256
    code+=bytes.fromhex('68efbeaddec3');regions.append(native.region(obs,code))
    return dict(program='DK2 Editor pro.exe',address='0x4a7ac0',
        registers=json.dumps(dict(ECX=hex(native.CTX),ESP=hex(native.STACK),DF='0')),
        memory=json.dumps(dict(regions=regions)),max_steps=200000,
        return_registers='EAX,EBP,EBX,ECX,EIP,ESP'),len(c['expected']),sum(len(bytes.fromhex(r['hex'])) for r in c['expected'])

async def main():
    params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),
        args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],
        env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8090'})
    rows=[]
    async with stdio_client(params) as (rd,wr):
        async with ClientSession(rd,wr) as session:
            await session.initialize()
            for case in CONTROLS:
                args,count,size=build(case);tag=hashlib.sha256(json.dumps(args,sort_keys=True).encode()).hexdigest()[:12]
                path=ROOT/'ghidra'/f'phase122-Room-Ghidra-{tag}.json'
                if path.exists():saved=json.loads(path.read_text(encoding='utf8'));assert saved['input']==args
                else:
                    response=await session.call_tool('emulate_function',args)
                    raw=json.loads('\n'.join(x.text for x in response.content if x.type=='text'))
                    saved=dict(input=args,result=raw.get('data',raw),tool_is_error=response.isError)
                    path.write_text(json.dumps(saved,indent=2),encoding='utf8')
                r=saved['result'];assert not saved['tool_is_error'] and r['success'] and r['hit_return'],r
                assert all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items()),r
                rows.append(dict(case=case,file=str(path.relative_to(ROOT)),sha256=native.sha(path.read_bytes()),checked_regions=count,checked_bytes=size))
                print('Ghidra passed',case,count,size,flush=True)
    (ROOT/'phase122-room-writer-crosschecks.json').write_text(json.dumps(dict(scope=__doc__,replays=rows),indent=2),encoding='utf8')

if __name__=='__main__':asyncio.run(main())
