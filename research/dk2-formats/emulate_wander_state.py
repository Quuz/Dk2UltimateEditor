"""Full DX480750 with real clock, PRNG, move requester and navigation check.

World-vslot208 captures arguments and supplies a selected DWORD result. The
prebuilt navigation bitmap has bit8 set everywhere;actual4B35C0 rejects moves.
No native function is overwritten. Observer captures EAX and expired stack
locals after return;the latter are internal evidence,not public API outputs.
"""
from pathlib import Path
import asyncio,json,os,struct,hashlib
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
from emulate_element_colors import region,pack32
from emulate_hero_parties import compare_code
from native_wander_state import candidates
ROOT=Path(__file__).resolve().parent
CTX=0x10000000;WORLD=0x10010000;CREATURE=0x10020000;CLOCK=0x10030000;NAV=0x10031000;BITS=0x10032000;VTABLE=0x10040000;OUT=0x10050000;OBS=0x10080000;QUERY=0x10090000;STACK=0x7FFF0000

def cases():
    base=dict(seed=1,tick=100,deadline=101,position=[10*4096+0xFFE,10*4096+1],home=[10,10],radius=1,blocked=[0]*4,wander=0)
    changes=[('basic',{}),('placement255',dict(wander=255)),('radius0',dict(radius=0)),('radius2',dict(radius=2)),
      ('expired-equal',dict(deadline=100)),('expired-before',dict(deadline=99)),('unsigned-future',dict(deadline=0xFFFFFFFF,tick=0)),
      ('unsigned-expired',dict(deadline=0,tick=0xFFFFFFFF)),('home-north-radius0',dict(home=[10,9],radius=0)),
      ('home-north-radius1',dict(home=[10,9])),('query-blocked',dict(blocked=[1]*4)),('query-negative',dict(blocked=[0xFFFFFFFF]*4)),
      ('mixed-query',dict(blocked=[0,1,0,1])),('seed0',dict(seed=0)),('seedFFFF',dict(seed=0xFFFFFFFF)),
      ('signed-negative-position',dict(position=[0xFFFFFFFF,10*4096],home=[0xFFFFFFFF,10])),
      ('wide-coordinate',dict(position=[0x7FFFFFFF,0x80000000],home=[0,0],radius=0xFFFFFFFF)),
      ('wrapped-distance',dict(home=[0x80000000,0x7FFFFFFF],radius=0xFFFFFFFF)),
    ]
    return [dict(base,**change,name=name) for name,change in changes]

def build(c):
    expected=candidates(**{k:c[k] for k in ['seed','tick','deadline','position','home','radius','blocked']})
    ctx=bytearray((i*31+7)&255 for i in range(0x100));creature=bytearray((i*17+19)&255 for i in range(0x600));world=bytearray((i*23+11)&255 for i in range(0x1700))
    for a,v in [(8,CREATURE),(12,WORLD),(0xA7,c['home'][0]),(0xAB,c['home'][1]),(0xAF,c['radius']),(0xB3,c['deadline'])]:struct.pack_into('<I',ctx,a,v)
    for a,v in [(0x1A,c['position'][0]),(0x1E,c['position'][1])]:struct.pack_into('<I',creature,a,v)
    creature[0x418]=c['wander']
    for a,v in [(0,VTABLE),(4,CLOCK),(0x9E6,32),(0x9EA,32),(0x9F2,NAV),(0x15A0,c['seed'])]:struct.pack_into('<I',world,a,v)
    want_world=bytearray(world);struct.pack_into('<I',want_world,0x15A0,expected['seed'])
    clock=bytes(4)+pack32(c['tick']);nav=bytearray(32);struct.pack_into('<I',nav,8,32);struct.pack_into('<I',nav,16,BITS)
    bits=bytes([8])*1024;vtable=bytearray(0x20C);struct.pack_into('<I',vtable,0x208,QUERY)
    out=bytearray(256)
    for i,v in enumerate(c['blocked']):struct.pack_into('<I',out,0x80+4*i,v)
    want=bytearray(out)
    for a,v in [(4,expected['last_vector'][0]),(8,expected['last_vector'][1]),(12,expected['last_vector'][2]),(0x20,len(expected['queries']))]:struct.pack_into('<I',want,a,v)
    for i,(x,y) in enumerate(expected['queries']):struct.pack_into('<II',want,0x40+8*i,x,y)
    # EAX=index,EDX=byteoffset;record ECX-style callee's two DWORD arguments.
    adapter=b'\xA1'+pack32(OUT+0x20)+bytes.fromhex('8bd0c1e2038b4c2404898a')+pack32(OUT+0x40)+bytes.fromhex('8b4c2408898a')+pack32(OUT+0x44)
    adapter+=bytes.fromhex('ff05')+pack32(OUT+0x20)+bytes.fromhex('8b0485')+pack32(OUT+0x80)+bytes.fromhex('c20800')
    mem=[region(CTX,ctx),region(WORLD,world),region(CREATURE,creature),region(CLOCK,clock),region(NAV,nav),region(BITS,bits),region(VTABLE,vtable),region(OUT,out),region(QUERY,adapter),region(0x6E5050,pack32(WORLD)),region(0x75B2F4,bytes(4)),region(STACK-12,bytes(12)),region(STACK,pack32(OBS))]
    checks=[(CTX,ctx),(WORLD,want_world),(CREATURE,creature),(CLOCK,clock),(NAV,nav),(BITS,bits),(VTABLE,vtable),(OUT,want),(0x6E5050,pack32(WORLD)),(0x75B2F4,bytes(4))]
    observer=b'\xA3'+pack32(OUT)
    for i in range(3):observer+=b'\xA1'+pack32(STACK-12+4*i)+b'\xA3'+pack32(OUT+4+4*i)
    observer+=b'\xBD'+pack32(1);at=0x11000000
    for address,b in checks:mem.append(region(at,b));observer+=compare_code(address,at,len(b))+bytes.fromhex('21c5');at+=((len(b)+15)//16)*16
    observer+=b'\x68'+pack32(0xDEADBEEF)+b'\xC3';mem.append(region(OBS,observer))
    spans=sorted((int(r['address'],16),len(bytes.fromhex(r['hex']))) for r in mem);assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:]))
    args=dict(program='DKII-DX.exe',address='0x480750',max_steps=90000,registers=json.dumps(dict(ECX=hex(CTX))),memory=json.dumps(dict(regions=mem)),return_registers='EAX,ECX,EBP,ESP,EIP')
    return args,expected

async def main():
    params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    rows=[]
    async with stdio_client(params) as (rd,wr):
      async with ClientSession(rd,wr) as session:
        await session.initialize()
        for c in cases():
            args,expected=build(c);path=ROOT/'ghidra'/f'phase75-emulation-wander-{c["name"]}.json'
            if path.exists():saved=json.loads(path.read_text());assert saved['input']==args and saved['expected']==expected
            else:
                response=await session.call_tool('emulate_function',args);r=json.loads('\n'.join(x.text for x in response.content if x.type=='text'))
                if response.isError or 'error' in r:raise RuntimeError(str(r))
                saved=dict(case=c,input=args,expected=expected,result=r.get('data',r));path.write_text(json.dumps(saved,indent=2),encoding='utf8')
            r=saved['result'];assert r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,ECX=0,EBP=1).items()),(c['name'],r,expected)
            rows.append(dict(file=path.name,sha256=hashlib.sha256(path.read_bytes()).hexdigest(),case=c['name']));print(c['name'],'verified',expected,flush=True)
    (ROOT/'phase75-wander-state-emulations.json').write_text(json.dumps(dict(scope=__doc__,cases=rows),indent=2),encoding='utf8')

if __name__=='__main__':asyncio.run(main())
