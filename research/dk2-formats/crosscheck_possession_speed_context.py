"""Independent Ghidra full-context replay of accepted Variables file scenes."""
import asyncio,json,os,struct
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
import emulate_possession_speed_context as native
from emulate_element_colors import region,pack32
from emulate_hero_parties import compare_code
ROOT=native.ROOT
CONTROLS=['wounded-water-rounding','at-health-boundary-water','level2-File-growth125-wounded','wounded-other-flag','definition-bit0-status100','status100-without-definition-bit0','walk-raw-quotient65536-minimum','negative-coefficient-wrap']
RESULT,DRIVER,SHADOW=0x1F500000,0x1F510000,0x22000000


def build(name):
    c=next(c for c in native.cases() if c['name']==name)
    args,ch,meta,saved,_=native.execute(c);wanted,_=native.verify(c,args,ch,meta,saved['result'])
    calls=args['before_calls'];first=calls[0];assert not first['stack_args']
    regions=json.loads(args['memory'])['regions'];stack=next(r for r in regions if int(r['address'],16)==0x7FFEE000)
    blob=bytearray.fromhex(stack['hex']);struct.pack_into('<I',blob,8192,DRIVER);stack['hex']=blob.hex();driver=bytearray()
    root=dict(address=args['address'],registers=json.loads(args['registers']),stack_args=args.get('stack_args',[]))
    for call in calls[1:]+[root]:
        driver+=b'\xbc'+pack32(0x7FFF0004+4*len(call['stack_args']))+b'\xbd'+pack32(0x7FFF0000)
        driver+=bytes.fromhex('31c031db31c931d231f631ff6a029d')
        for v in reversed(call['stack_args']):driver+=b'\x68'+pack32(v)
        for reg,op in [('EAX',0xB8),('ECX',0xB9),('EDX',0xBA),('EBX',0xBB),('ESI',0xBE),('EDI',0xBF)]:
            if reg in call['registers']:driver+=bytes([op])+pack32(int(call['registers'][reg],0))
        # CALL rel32 preserves every prepared general register.
        pc=DRIVER+len(driver);driver+=b'\xE8'+pack32(int(call['address'],0)-pc-5)
    driver+=b'\xa3'+pack32(RESULT);regions.append(region(RESULT,bytes([165])*4));wanted[RESULT]=pack32(int(saved['result']['registers']['EAX'],16))
    driver+=bytes.fromhex('bd01000000bb00000000');shadow=SHADOW
    for i,(a,b) in enumerate(wanted.items()):
        regions.append(region(shadow,b));driver+=compare_code(a,shadow,len(b))+bytes.fromhex('85c07505bb')+pack32(i+1)+bytes.fromhex('21c5')
        shadow+=(len(b)+255)//256*256
    driver+=bytes.fromhex('68efbeaddec3');regions.append(region(DRIVER,driver))
    spans=sorted((int(r['address'],16),len(bytes.fromhex(r['hex']))) for r in regions);assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:]))
    return dict(program='DKII-DX.exe',address=first['address'],registers=json.dumps(dict(first['registers'],DF='0',FPUControlWord='0x37f',FPUStatusWord='0')),
        memory=json.dumps(dict(regions=regions)),max_steps=4000000,return_registers='EAX,EBP,EBX,ECX,EIP,ESP'),len(wanted),sum(map(len,wanted.values()))


async def main():
    params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    rows=[]
    async with stdio_client(params) as (rd,wr):
        async with ClientSession(rd,wr) as session:
            await session.initialize()
            for name in CONTROLS:
                args,n,size=build(name);tag=native.sha(json.dumps(args,sort_keys=True).encode())[:12];file=ROOT/'ghidra'/f'phase201-speed-context-Ghidra-{name}-{tag}.json'
                if file.exists():saved=native.load(str(file.relative_to(ROOT)));assert saved['input']==args
                else:
                    response=await session.call_tool('emulate_function',args);value=json.loads('\n'.join(x.text for x in response.content if x.type=='text'));saved=dict(input=args,result=value.get('data',value),tool_is_error=response.isError);native.dump(str(file.relative_to(ROOT)),saved)
                r=saved['result'];assert not saved['tool_is_error'] and r['success'] and r['hit_return'],(name,str(r)[:700])
                assert all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items()),(name,r['registers'])
                rows.append(dict(case=name,file=str(file.relative_to(ROOT)),sha256=native.sha(file.read_bytes()),checked_regions=n,checked_bytes=size));print(name,n,size,'Ghidra passed',flush=True)
    native.dump('phase201-speed-context-crosschecks.json',dict(scope=__doc__,replays=rows))


if __name__=='__main__':asyncio.run(main())
