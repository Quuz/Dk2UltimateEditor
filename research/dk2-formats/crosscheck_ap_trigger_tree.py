"""Independent Ghidra execution of complete bind plus three AP ticks, whole memory."""
from pathlib import Path
import asyncio,json,os,struct
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
import emulate_ap_trigger_tree as native
from emulate_element_colors import region,pack32
from emulate_hero_parties import compare_code
ROOT=Path(__file__).resolve().parent
DRIVER,RESULT,SHADOW=0x11000000,0x11010000,0x20000000
SELECT=['flags00-repeat1','flags04-repeat2','flags08-repeat255','flags0c-repeat0','false-condition',
    'missing-root-falls-back','missing-action-falls-back','nested-actions','nested-condition-repeat2','direct-negative-root',
    'original-3DLevel-clock1','original-3DLevel-clock2147483648']

def build(c):
    args,ch,d,_=native.execute(c);wanted,out=native.verify(c,args,ch,d['result']);regions=json.loads(args['memory'])['regions']
    first=args['before_calls'][0]
    stack=next(r for r in regions if int(r['address'],16)==0x7FFEE000);b=bytearray.fromhex(stack['hex']);values=[DRIVER]+first['stack_args'];struct.pack_into('<'+'I'*len(values),b,0x2000,*values);stack['hex']=b.hex()
    driver=b''
    for call in args['before_calls'][1:]+[dict(address=args['address'],registers=json.loads(args['registers']),stack_args=[])]:
        driver+=b'\xb9'+pack32(int(call['registers']['ECX'],16))
        for value in reversed(call['stack_args']):driver+=b'\x68'+pack32(value)
        driver+=b'\xb8'+pack32(int(call['address'],16))+bytes.fromhex('ffd0')
    driver+=b'\xa3'+pack32(RESULT)+bytes.fromhex('bd01000000bb00000000');regions.append(region(RESULT,bytes([165])*4));wanted[RESULT]=pack32(1)
    shadow=SHADOW
    for i,(a,b) in enumerate(wanted.items()):
        regions.append(region(shadow,b));driver+=compare_code(a,shadow,len(b))+bytes.fromhex('85c07505bb')+pack32(i+1)+bytes.fromhex('21c5');shadow+=(len(b)+255)//256*256
    driver+=bytes.fromhex('68efbeaddec3');regions.append(region(DRIVER,driver))
    return dict(program='DKII-DX.exe',address=first['address'],registers=json.dumps(dict(first['registers'],DF='0')),
        memory=json.dumps(dict(regions=regions)),max_steps=1000000,return_registers='EAX,EBP,EBX,ECX,EIP,ESP'),len(wanted),sum(map(len,wanted.values()))

async def main():
    params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    rows=[]
    async with stdio_client(params) as (rd,wr):
        async with ClientSession(rd,wr) as session:
            await session.initialize()
            for c in native.cases():
                if c['name'] not in SELECT:continue
                args,n,size=build(c);tag=native.sha(json.dumps(args,sort_keys=True).encode())[:12];file=ROOT/'ghidra'/f'phase156-AP-tree-Ghidra-{c["name"]}-{tag}.json'
                if file.exists():d=json.loads(file.read_text(encoding='utf8'));assert d['input']==args
                else:
                    response=await session.call_tool('emulate_function',args);v=json.loads('\n'.join(x.text for x in response.content if x.type=='text'));d=dict(input=args,result=v.get('data',v),tool_is_error=response.isError);file.write_text(json.dumps(d,indent=2),encoding='utf8')
                r=d['result'];assert not d['tool_is_error'] and r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items()),(c,r)
                rows.append(dict(case=c['name'],file=str(file.relative_to(ROOT)),sha256=native.sha(file.read_bytes()),checked_regions=n,checked_bytes=size));print(c['name'],n,size,'Ghidra passed',flush=True)
    assert len(rows)==len(SELECT)
    (ROOT/'phase156-AP-tree-crosschecks.json').write_text(json.dumps(dict(scope=__doc__,replays=rows),indent=2),encoding='utf8')
if __name__=='__main__':asyncio.run(main())
