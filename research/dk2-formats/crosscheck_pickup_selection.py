"""Independent Ghidra CPU and whole prepared-memory controls of cursor fallback."""
from pathlib import Path
import asyncio,json,os,struct
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
import emulate_pickup_selection as native
from emulate_element_colors import region,pack32
from emulate_hero_parties import compare_code
ROOT=Path(__file__).resolve().parent
CONTROLS=['first-farther-creature','reversed-creatures','near-object-before-creature','objects-nearest','object-only-nearest','priority-255-0','object-priority-255','Y-outer-X-inner-order']
DRIVER,RESULT,SHADOW=0x10800000,0x10810000,0x20000000
def build(name):
    c=next(c for c in native.cases() if c['name']==name);args,checks,e,saved,_=native.execute(c);wanted,_=native.verify(c,args,checks,e,saved['result'])
    regions=json.loads(args['memory'])['regions'];first=args['before_calls'][0]
    stack=next(r for r in regions if int(r['address'],16)==0x7FFEE000);b=bytearray.fromhex(stack['hex']);struct.pack_into('<I',b,0x2000,DRIVER);stack['hex']=b.hex()
    driver=bytearray()
    for call in args['before_calls'][1:]+[dict(address=args['address'],registers=json.loads(args['registers']),stack_args=args['stack_args'])]:
        driver+=b'\xbc'+pack32(0x7FFF0004+4*len(call['stack_args']))+b'\xbd'+pack32(0x7FFF0000)+bytes.fromhex('31c031db31c931d231f631ff6a029d')
        for v in reversed(call['stack_args']):driver+=b'\x68'+pack32(v)
        driver+=b'\xb9'+pack32(int(call['registers']['ECX'],0))+b'\xb8'+pack32(int(call['address'],0))+bytes.fromhex('ffd0')
    driver+=b'\xa3'+pack32(RESULT);regions.append(region(RESULT,bytes([165])*4));wanted[RESULT]=pack32(int(saved['result']['registers']['EAX'],16))
    driver+=bytes.fromhex('bd01000000bb00000000');shadow=SHADOW
    for i,(a,b) in enumerate(wanted.items()):
        regions.append(region(shadow,b));driver+=compare_code(a,shadow,len(b))+bytes.fromhex('85c07505bb')+pack32(i+1)+bytes.fromhex('21c5');shadow+=(len(b)+255)//256*256
    driver+=bytes.fromhex('68efbeaddec3');regions.append(region(DRIVER,driver))
    return dict(program='DKII-DX.exe',address=first['address'],registers=json.dumps(dict(first['registers'],DF='0',FPUControlWord='0x37f',FPUStatusWord='0')),
        memory=json.dumps(dict(regions=regions)),max_steps=2000000,return_registers='EAX,EBP,EBX,ECX,EIP,ESP'),len(wanted),sum(map(len,wanted.values()))
async def main():
    p=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    rows=[]
    async with stdio_client(p) as (rd,wr):
        async with ClientSession(rd,wr) as session:
            await session.initialize()
            for name in CONTROLS:
                args,n,size=build(name);tag=native.sha(json.dumps(args,sort_keys=True).encode())[:12];file=ROOT/'ghidra'/f'phase153-pickup-Ghidra-{name}-{tag}.json'
                if file.exists():saved=json.loads(file.read_text(encoding='utf8'));assert saved['input']==args
                else:
                    response=await session.call_tool('emulate_function',args);value=json.loads('\n'.join(x.text for x in response.content if x.type=='text'))
                    saved=dict(input=args,result=value.get('data',value),tool_is_error=response.isError);file.write_text(json.dumps(saved,indent=2),encoding='utf8')
                r=saved['result'];assert not saved['tool_is_error'] and r['success'] and r['hit_return'],(name,str(r)[:500])
                assert all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items()),(name,r['registers'])
                rows.append(dict(case=name,file=str(file.relative_to(ROOT)),sha256=native.sha(file.read_bytes()),checked_regions=n,checked_bytes=size));print(name,n,size,'Ghidra passed',flush=True)
    (ROOT/'phase153-pickup-crosschecks.json').write_text(json.dumps(dict(scope=__doc__,replays=rows),indent=2),encoding='utf8')
if __name__=='__main__':asyncio.run(main())
