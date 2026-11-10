"""Complete camera activation kind6 branch with real5558D0 PRNG.

Native PRNG arithmetic executes unchanged and trig values are loaded PE data.
Existing explicit clock/world/ground/hook/event providers are retained. No body
creation,renderer,actualterrain,eventqueue or per-frame interpolation executes.
"""
from pathlib import Path
import asyncio,hashlib,json,os,struct,sys
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
from emulate_element_colors import region,pack32,jump
from emulate_hero_parties import compare_code
from emulate_camera_activation import build_case as common_case,initial,BASE,CAP,END
from native_camera_activation import put,setw
from native_body_camera import project,random_step
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent

def table():return pe_read((ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe').read_bytes(),0x6C1488,0x2800)
def cases():
    rows=[]
    for angle in [0,512,1024,1536,2047]:
        for eye in [0,4096,65535]:rows.append(dict(name=f'body-angle-{angle}-eye-{eye}',kind='body',angle=angle,raw_eye=eye))
    rows.extend([dict(name='body-eye-high-word-loader',kind='body',angle=0,raw_eye=0xFFFF1000,loader=True),dict(name='body-eye-low-word-loader',kind='body',angle=0,raw_eye=0x1000,loader=True),dict(name='body-eye-absent-loader',kind='body',angle=0,raw_eye=0xFFFFFFFF,loader=True,presence=0,prior_eye=8192),dict(name='body-negative-positions',kind='body',angle=1729,raw_eye=65535,x=0xFFFFE123,y=0x80001234),dict(name='body-flag-bit-already-clear',kind='body',angle=17,raw_eye=8192,flags=0xFE),dict(name='body-invalid-node-fallback',kind='body',angle=17,raw_eye=8192,valid=False),dict(name='body-high-node-word',kind='body',angle=1024,raw_eye=4096,node=0x10001),dict(name='body-force-still-transition',kind='body',angle=512,raw_eye=4096,force=True),dict(name='body-clock-wrap',kind='body',angle=0,raw_eye=4096,now=0xFFFFFF00)])
    for seed in [0,1,0x80000000,0xFFFFFFFF]:
        for bound in [1,256,512,0x77758,0xFFFFFFFF]:rows.append(dict(name=f'rng-{seed}-{bound}',kind='rng',seed=seed,bound=bound))
    return rows

def build_case(c):
    if c['kind']=='rng':return rng_case(c)
    common=dict(name=c['name'],mode=11,node=c.get('node',1),entity_kind=4,valid=c.get('valid',True),force=c.get('force',False),now=c.get('now',123456))
    args,wantreg=common_case(common);mem={int(r['address'],16):bytes.fromhex(r['hex']) for r in json.loads(args['memory'])['regions']}
    ent=bytearray(mem[BASE+0x16000]);definition=bytearray(mem[BASE+0x18000]);put(ent,0x12,6);put(ent,0x129,BASE+0x18000);ent[0x128]=4;ent[0x12D]=1
    runtime_eye=(c['raw_eye']&65535) if c.get('presence',1) else c.get('prior_eye',8192)
    struct.pack_into('<3I',ent,0x1A,c.get('x',11111),c.get('y',22222),33333);setw(ent,0xF4,c['angle']);ent[0xF3]=c.get('flags',255);setw(definition,0x65C,runtime_eye)
    projection=project(initial(common),ent,definition,table(),node=c.get('node',1),valid=c.get('valid',True),now=c.get('now',123456))
    mem[BASE+0x16000]=bytes(ent);mem[BASE+0x17000]=projection['entity'];mem[BASE+0x18000]=mem[BASE+0x19000]=bytes(definition);mem[BASE+0x2000]=projection['manager']
    mem[0x75B2F4]=pack32(0)
    # Capture the actual position supplied to terrain vslot204,not only count.
    ground=bytes.fromhex('ff05')+pack32(CAP+28)+bytes.fromhex('8b442404')
    for off in [0,4,8]:ground+=bytes.fromhex('8b50')+bytes([off])+bytes.fromhex('8915')+pack32(BASE+0x21000+off)
    ground+=bytes.fromhex('890d')+pack32(BASE+0x2100C)+b'\xB8'+pack32(768)+bytes.fromhex('c20400')
    mem[BASE+0xD000]=ground
    if c.get('valid',True):
        v=projection['body_position'];groundpoint=struct.pack('<4I',v[0],v[1],33333,BASE+0x9000)
    else:groundpoint=struct.pack('<4I',5000,7000,9000,BASE+0x9000)
    mem[BASE+0x21000]=bytes(16);mem[BASE+0x22000]=groundpoint
    old=mem[BASE+0x4000];assert old.endswith(bytes.fromhex('89e8')+END)
    mem[BASE+0x4000]=old[:-len(END)-2]+compare_code(BASE+0x21000,BASE+0x22000,16)+bytes.fromhex('21c589e8')+END
    if c.get('loader'):
        # Explicit21-byte native EyeHeight assignment fragment,then wholecamera.
        # Entry/exit localmemoryjumps bypass the rest of Creatureloader492320;
        # this is not counted as a complete loader or a full rawparser chain.
        original=bytearray(definition);setw(original,0x65C,c.get('prior_eye',0xA5A5));mem[BASE+0x18000]=bytes(original)
        mid=bytearray([165])*0x12A4;put(mid,0xF4,c.get('presence',1));put(mid,0x12A0,c['raw_eye']);mem[BASE+0x25000]=bytes(mid)
        adapter=b'\xB9'+pack32(BASE)+b'\x68'+pack32(c.get('node',1))+b'\x68'+pack32(11)+b'\x68'+pack32(BASE+0x4000)
        adapter+=jump(BASE+0x24000+len(adapter),0x44ACE0)
        mem[BASE+0x24000]=adapter;mem[0x492320]=jump(0x492320,0x4923C1);mem[0x4923D6]=jump(0x4923D6,BASE+0x24000)
        args['address']='0x492320';args['registers']=json.dumps(dict(EBX=hex(BASE+0x18000),EBP=hex(BASE+0x25000),EDI='0x0'))
    args['memory']=json.dumps(dict(regions=[region(a,b) for a,b in mem.items()]));return args,wantreg

def rng_case(c):
    seed,value=random_step(c['seed'],c['bound']);before=struct.pack('<4I',c['seed'],0x12345678,0x87654321,0xA5A5A5A5);after=pack32(seed)+before[4:]
    capture=struct.pack('<I',value);readout=b'\xA3'+pack32(BASE+0x2000)+compare_code(BASE,BASE+0x1000,16)+bytes.fromhex('89c5')+compare_code(BASE+0x2000,BASE+0x3000,4)+bytes.fromhex('21c589e8')+END
    mem=[region(BASE,before),region(BASE+0x1000,after),region(BASE+0x2000,bytes(4)),region(BASE+0x3000,capture),region(BASE+0x4000,readout),region(0x75B2F4,pack32(0)),region(0x7FFF0000,pack32(BASE+0x4000)+pack32(c['bound'])+pack32(BASE)+pack32(0)+pack32(0))]
    return dict(program='DKII-DX.exe',address='0x5558D0',max_steps=10000,registers=json.dumps({}),memory=json.dumps(dict(regions=mem)),return_registers='EAX,EBP,ESP,EIP'),dict(EAX=1,EBP=1)

async def main():
    selected=cases()
    if len(sys.argv)>1:selected=[c for c in selected if c['name']==sys.argv[1]];assert selected
    params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    rows=[]
    async with stdio_client(params) as (rd,wr):
        async with ClientSession(rd,wr) as s:
            await s.initialize()
            for c in selected:
                args,want=build_case(c);p=ROOT/'ghidra'/('phase65-emulation-body-camera-'+c['name']+'.json')
                if p.exists():saved=json.loads(p.read_text());assert saved['input']==args and saved['expected_registers']==want
                else:
                    response=await s.call_tool('emulate_function',args);r=json.loads('\n'.join(x.text for x in response.content if x.type=='text'))
                    if response.isError or 'error' in r:raise RuntimeError(str(r))
                    saved=dict(case=c,input=args,expected_registers=want,result=r.get('data',r));p.write_text(json.dumps(saved,indent=2),encoding='utf8')
                r=saved['result'];assert r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in want.items()),(c['name'],r,want)
                rows.append(dict(file=p.name,sha256=hashlib.sha256(p.read_bytes()).hexdigest(),case=c['name']));print(c['name'],'verified',flush=True)
    if len(selected)==len(cases()):(ROOT/'phase65-body-camera-emulations.json').write_text(json.dumps(dict(cases=rows,scope=__doc__),indent=2),encoding='utf8')
if __name__=='__main__':asyncio.run(main())
