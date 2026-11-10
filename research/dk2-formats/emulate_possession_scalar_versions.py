"""Full originalscalar callbacks inexactold/DXprofiles;no gameplay bonus claim."""
from pathlib import Path
import asyncio,json,os,struct,hashlib,sys
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
from emulate_element_colors import region,pack32
from emulate_hero_parties import compare_code
ROOT=Path(__file__).resolve().parent
PROFILES={'old':dict(program='DKII-local13.exe',callback=0x510af0,variables=0x6dd5d8),
 'DX':dict(program='DKII-DX.exe',callback=0x51cd90,variables=0x6f2550)}
RAW=0x10900000;OUT=0x10910000;DRIVER=0x10920000;OBS=0x10930000
def cases():
    out=[]
    for profile in PROFILES:
        for kind in [112,113,114]:
            for value in [0,6,0xffff,0xabcd0006,0xffffffff]:out.append(dict(name=f'{profile}-{kind}-{value:x}',profile=profile,records=[[kind,value]]))
        out.append(dict(name=profile+'-overlapping-neighbours',profile=profile,records=[[113,0x89abcdef],[112,0x12345678],[114,0xfedcba98],[113,6]]))
    return out
def build(c):
    p=PROFILES[c['profile']];variables=bytearray([0xa5])*4096;wanted=bytearray(variables);raw=bytearray()
    for i,(kind,value) in enumerate(c['records']):
        raw+=bytes([0x5a])*16+struct.pack('<4I',kind,value,0x11223344,0x55667788)
        struct.pack_into('<H',wanted,0x3a0+2*(kind-112),value&65535)
    driver=bytearray(b'\xa3'+pack32(OUT))
    def call(a):driver.extend(b'\xe8'+pack32(a-(DRIVER+len(driver)+5)))
    for i in range(1,len(c['records'])):
        driver.extend(b'\x68'+pack32(RAW+32*i));call(p['callback']);driver.extend(b'\xa3'+pack32(OUT+4*i))
    driver.extend(b'\x68'+pack32(OBS)+b'\xc3')
    out=bytes(4*len(c['records']))+bytes([0xa5])*64;expected=b''.join(pack32(1) for _ in c['records'])+bytes([0xa5])*64
    buffers=[(p['variables'],variables,wanted),(RAW,raw+bytes([0xa5])*64,raw+bytes([0xa5])*64),(OUT,out,expected)]
    mem=[region(a,b) for a,b,_ in buffers]+[region(DRIVER,driver),region(0x7fff0000,pack32(DRIVER)+pack32(RAW))]
    observer=bytearray(b'\xbd'+pack32(1)+b'\xbb'+pack32(0));at=0x11000000
    for i,(a,_,wanted) in enumerate(buffers):
        mem.append(region(at,wanted));observer+=compare_code(a,at,len(wanted))+bytes.fromhex('85c07505bb')+pack32(i+1)+bytes.fromhex('21c5');at+=((len(wanted)+255)//256)*256
    observer+=b'\x68'+pack32(0xdeadbeef)+b'\xc3';mem.append(region(OBS,observer))
    args=dict(program=p['program'],address=hex(p['callback']),max_steps=100000,registers='{}',memory=json.dumps(dict(regions=mem)),return_registers='EAX,EBP,EBX,ECX,ESP,EIP')
    return args,dict(values=[v&65535 for _,v in c['records']],offsets=[0x3a0+2*(k-112) for k,_ in c['records']],checks=[dict(address=hex(a),bytes=len(w)) for a,_,w in buffers])
async def main():
    selected=cases()
    if len(sys.argv)>1:selected=[c for c in selected if c['name']==sys.argv[1]];assert selected
    params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'});rows=[]
    async with stdio_client(params) as (rd,wr):
        async with ClientSession(rd,wr) as session:
            await session.initialize()
            for c in selected:
                args,e=build(c);key=hashlib.sha256(json.dumps(args,sort_keys=True).encode()).hexdigest()[:10];path=ROOT/'ghidra'/f'phase101-scalars-{c["name"]}-{key}.json'
                if path.exists():saved=json.loads(path.read_text(encoding='utf8'));assert saved['input']==args and saved['expected']==e
                else:
                    response=await session.call_tool('emulate_function',args);value='\n'.join(x.text for x in response.content if x.type=='text')
                    if response.isError or not value.startswith('{'):
                        (ROOT/'ghidra'/f'phase101-scalar-error-{key}.txt').write_text(value,encoding='utf8');raise RuntimeError(value[:700])
                    r=json.loads(value);assert 'error' not in r,r
                    saved=dict(case=c,input=args,expected=e,result=r.get('data',r));path.write_text(json.dumps(saved,indent=2),encoding='utf8')
                r=saved['result'];assert r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items()),(c,r)
                rows.append(dict(case=c['name'],file=path.name,sha256=hashlib.sha256(path.read_bytes()).hexdigest()));print(c['name'],'verified',flush=True)
    if len(selected)==len(cases()):(ROOT/'phase101-scalar-version-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows),indent=2),encoding='utf8')
if __name__=='__main__':asyncio.run(main())
