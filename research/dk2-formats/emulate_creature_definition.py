"""Full native2199-byte Creature ctor/loader with explicit partial intermediates.

All native code/vtables retained.9fresh deterministic replays expose all2199
bytes through YMM256 pages;not an atomic process dump. No raw loader claim
for these partial6457-byte intermediates;raw lineage is prior primary data.
"""
from pathlib import Path
import asyncio,json,os,struct,hashlib,sys
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
from emulate_element_colors import region,pack32
from emulate_hero_parties import compare_code
from native_keeper_array import put
import memory_readout_ymm as readout
ROOT=Path(__file__).resolve().parent
DEF=0x10000000;MID=0x10010000;WORLD=0x10020000;VARS=0x10030000;SESSION=0x10040000;GFX=0x10041000;GV=0x10042000;GFX_CB=0x10043000;SOUND=0x10044000;SV=0x10045000;SOUND_CB=0x10046000
OUT=0x10050000;DRIVER=0x10060000;OBS=0x10068000
def cases():
    base=dict(seed=165,load=True,overlay=0,tps=20,priority=0,jump=0,scalar_presence=False,portal=[0,0,0],offsets=[[0,0,0] for _ in range(8)],offset_presence=[False]*8,durations=None)
    variants=[('ctor-a5',dict(load=False)),('ctor-zero',dict(load=False,seed=0)),('empty-mid',{}),
      ('portal-without-presence',dict(portal=[0x11223344,0x80000000,0xFFFFFFFF])),
      ('seven-offsets',dict(portal=[11,12,13],offsets=[[i*3+101,i*3+102,i*3+103] for i in range(8)],offset_presence=[True]*8)),
      ('offsets-presence-off',dict(portal=[11,12,13],offsets=[[i*3+101,i*3+102,i*3+103] for i in range(8)])),
      ('mid-eighth-only',dict(offsets=[[0,0,0]]*7+[[0x11111111,0x22222222,0x33333333]],offset_presence=[False]*7+[True])),
      ('priority-jump-max',dict(priority=255,jump=0xFFFFFFFF,scalar_presence=True)),
      ('priority-jump-signed-bit',dict(priority=128,jump=0x80000000,scalar_presence=True)),
      ('priority-jump-presence-off',dict(priority=255,jump=0xFFFFFFFF)),
      ('overlay-checksum-retain',dict(overlay=1,priority=255,jump=0xFFFFFFFF,scalar_presence=True)),
      ('durations20',dict(durations=[0x12345,0xFFFFFFFF,0x80000000,1],jump=0x12345,scalar_presence=True)),
      ('durations60',dict(tps=60,durations=[0x12345,0xFFFFFFFF,0x80000000,1],jump=0x12345,scalar_presence=True))]
    return [dict(base,**kw,name=name) for name,kw in variants]
def call(at,target):return b'\xE8'+pack32(target-(at+5))
def build(c,page):
    mid=bytearray(6457);put(mid,0x1E8,int(c['scalar_presence']));mid[0x1345]=c['priority'];put(mid,0x204,int(c['scalar_presence']));put(mid,0x12D0,c['jump'])
    for i,v in enumerate(c['portal']):put(mid,0x1284+4*i,v)
    for i,xyz in enumerate(c['offsets']):
        put(mid,0x2EC+4*i,int(c['offset_presence'][i]))
        for j,v in enumerate(xyz):put(mid,0x166B+12*i+4*j,v)
    if c['durations'] is not None:
        for i,v in enumerate(c['durations']):put(mid,0x1F4+4*i,1);put(mid,0x12C0+4*i,v)
    world=bytearray(0xA3D0);put(world,0,0x66E3EC);put(world,4,SESSION);put(world,0xA3C7,c['overlay']);variables=bytearray(0x400);put(variables,0x40,c['tps']);session=bytearray(0x18);put(session,0x10,GFX);put(session,0x14,WORLD)
    gv=bytearray(0x58);put(gv,0x54,GFX_CB);sv=bytearray(32);put(sv,0x1C,SOUND_CB)
    output=bytearray(0x140);wanted=bytearray(output)
    driver=bytearray()
    if c['load']:
        driver+=b'\x6A\x00'+b'\x68'+pack32(WORLD)+b'\x68'+pack32(MID)+b'\xB9'+pack32(DEF);driver+=call(DRIVER+len(driver),0x492320)
    for i,index in enumerate([*range(8),8,0xFFFFFFFF]):
        driver+=b'\x68'+pack32(index)+b'\xB9'+pack32(DEF);driver+=call(DRIVER+len(driver),0x494650);driver+=b'\xA3'+pack32(OUT+4*i)
        xyz=(c['portal'] if i==0 and c['load'] else (c['offsets'][i-1] if c['load'] and 1<=i<=7 and c['offset_presence'][i-1] else [0,0,0]))
        if i>=8:xyz=[0,0,0]
        put(wanted,4*i,DEF+0x6EC+12*i if i<8 else 0x6E4148)
        for j,v in enumerate(xyz):put(wanted,0x40+12*i+4*j,v)
        for j in range(3):driver+=bytes.fromhex('8b50')+bytes([4*j])+bytes.fromhex('8915')+pack32(OUT+0x40+12*i+4*j)
    driver+=b'\x68'+pack32(OBS)+b'\xC3'
    sound=bytes.fromhex('ff05')+pack32(OUT+0x100)+b'\xB8'+pack32(0x11223344)+bytes.fromhex('c20800')
    gfx=bytes.fromhex('ff05')+pack32(OUT+0x104)+bytes.fromhex('8b44240cc7000000000033c0c21800')
    fixed=[(MID,mid,mid),(WORLD,world,world),(VARS,variables,variables),(SESSION,session,session),(GFX,pack32(GV),pack32(GV)),(GV,gv,gv),(SOUND,pack32(SV),pack32(SV)),(SV,sv,sv),
      (OUT,output,wanted),(0x6E4154,pack32(VARS),pack32(VARS)),(0x75B468,pack32(SOUND),pack32(SOUND)),(0x6E4144,b'\x01',b'\x01'),(0x6E4148,bytes(12),bytes(12)),(0,bytes(4),bytes(4))]
    mem=[region(DEF,bytes([c['seed']])*2560)]+[region(a,b) for a,b,_ in fixed]+[region(DRIVER,driver),region(SOUND_CB,sound),region(GFX_CB,gfx)]
    observer=b'\xBD'+pack32(1)+b'\x31\xDB';at=0x11000000
    checks=[(a,b) for a,_,b in fixed]+[(DEF+2199,bytes([c['seed']])*(2560-2199))]
    for i,(a,b) in enumerate(checks):mem.append(region(at,b));observer+=compare_code(a,at,len(b))+bytes.fromhex('85c07505bb')+pack32(i+1)+bytes.fromhex('21c5');at+=((len(b)+15)//16)*16
    observer+=readout.code(DEF+page*256)+b'\x68'+pack32(0xDEADBEEF)+b'\xC3';mem.append(region(OBS,observer));stack=bytearray([0xA5])*16384;put(stack,8192,DRIVER);mem.append(region(0x7FFEE000,stack))
    spans=sorted((int(r['address'],16),len(bytes.fromhex(r['hex']))) for r in mem);assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:])),spans
    args=dict(program='DKII-DX.exe',address='0x491dc0',max_steps=1000000,registers=json.dumps(dict(ECX=hex(DEF),EAX='0',EBX='0',EDX='0',ESI='0',EDI='0',DF='0')),memory=json.dumps(dict(regions=mem)),return_registers='EAX,EBX,ECX,EDX,ESI,EDI,ESP,EBP,EIP,'+readout.REGISTERS)
    return args,dict(priority=c['priority'] if c['load'] and c['scalar_presence'] else 0,jump=c['jump'] if c['load'] and c['scalar_presence'] else 0,
      checksum=sum(mid)&0xFFFFFFFF if c['load'] and not c['overlay'] else c['seed']*0x01010101,offsets_hex=bytes(wanted[0x40:0x40+96]).hex(),
      durations=[((v*c['tps'])&0xFFFFFFFF)>>12 for v in c['durations']] if c['load'] and c['durations'] is not None else [0]*4,
      checks=[dict(address=hex(a),bytes=len(b)) for a,b in checks])
def verify(c,b,e):
    assert len(b)==2199 and b[0x6EB]==e['priority'] and int.from_bytes(b[0x75C:0x760],'little')==e['jump']
    assert b[0x6EC:0x74C].hex()==e['offsets_hex'] and list(struct.unpack_from('<4I',b,0x74C))==e['durations']
    assert int.from_bytes(b[0x5AF:0x5B3],'little')==e['checksum']
async def main():
    selected=cases()
    if len(sys.argv)>1:selected=[c for c in selected if c['name']==sys.argv[1]];assert selected
    params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    rows=[]
    async with stdio_client(params) as (rd,wr):
      async with ClientSession(rd,wr) as session:
        await session.initialize()
        for c in selected:
            chunks=[]
            for page in range(9):
                args,e=build(c,page);p=ROOT/'ghidra'/f'phase85-emulation-creature-definition-{c["name"]}-page{page}.json'
                if p.exists():saved=json.loads(p.read_text());assert saved['input']==args and saved['expected']==e
                else:
                    response=await session.call_tool('emulate_function',args);r=json.loads('\n'.join(x.text for x in response.content if x.type=='text'))
                    if response.isError or 'error' in r:raise RuntimeError(str(r))
                    saved=dict(case=c,page=page,input=args,expected=e,result=r.get('data',r));p.write_text(json.dumps(saved,indent=2),encoding='utf8')
                r=saved['result'];assert r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,ECX=0,EBP=1,EBX=0).items()),(c['name'],page,r,e)
                chunks.append(readout.decode(r['registers']));rows.append(dict(file=p.name,sha256=hashlib.sha256(p.read_bytes()).hexdigest(),case=c['name'],page=page))
            b=b''.join(chunks)[:2199];verify(c,b,e);(ROOT/'ghidra'/f'phase85-creature-definition-{c["name"]}.bin').write_bytes(b);print(c['name'],'verified',flush=True)
    if len(selected)==len(cases()):(ROOT/'phase85-creature-definition-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows),indent=2),encoding='utf8')
if __name__=='__main__':asyncio.run(main())
