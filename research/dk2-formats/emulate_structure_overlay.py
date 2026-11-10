"""Full converters→explicit height-loader fragments→native overlay consumers.

5955C0 complete;24 full5953D0 compositions also execute5956E0/5958F0/40CFD0/
4B5C10. Render submission/resource conversion are captured fixtures,not draws.
Only loader entry/stop are memory patched; other loader code is not executed.
Full intermediate snapshot checks later immutability,not all converter meanings.
"""
from pathlib import Path
import asyncio,hashlib,json,os,struct,sys
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
from emulate_element_colors import region,pack32,jump
from emulate_hero_parties import compare_code
from native_structure_overlay import FAMILIES,project,height_offset
ROOT=Path(__file__).resolve().parent
END=b'\x68'+pack32(0xDEADBEEF)+b'\xC3'
def put(b,o,v):struct.pack_into('<I',b,o,v&0xFFFFFFFF)

def cases():
 out=[]
 for mode,values in [('helper',[0,32768,65535,65536]),('caller',[0,4096,32767,32768,65535,65536,0x1234FFFF,0xFFFFFFFF])]:
  for family in FAMILIES:
   for state in ([0,1] if family=='Doors' else [0]):
    for height in values:out.append(dict(name=f'{mode}-{family}-{state}-{height}',mode=mode,family=family,state=state,height=height,presence=1,ready=1,trapid=7))
 for family in FAMILIES:
  for presence in [0,2,0xFFFFFFFF]:out.append(dict(name=f'partial-{family}-{presence}',mode='helper',family=family,state=0,height=0xFFFF0001,presence=presence,ready=1,trapid=7))
 for state in [2,0xFFFFFFFF]:
  for height in [32768,65536]:out.append(dict(name=f'nonzero-door-state-{state}-{height}',mode='helper',family='Doors',state=state,height=height,presence=1,ready=1,trapid=7))
 for family in FAMILIES:out.append(dict(name='not-ready-'+family,mode='caller',family=family,state=0,height=65535,presence=1,ready=0,trapid=7))
 out.append(dict(name='trap16-no-submission',mode='caller',family='Traps',state=0,height=65535,presence=1,ready=1,trapid=16))
 return out

def const(value,cleanup=0):return b'\xB8'+pack32(value)+(b'\xC2'+struct.pack('<H',cleanup) if cleanup else b'\xC3')

def capture_submission():
 # One caller submits at most twice. Each row records info/subject pointers,
 # both positions and27-byte resource descriptor. Explicit guard bounds REP.
 b=bytearray(bytes.fromhex('535657a1')+pack32(0x10008000)+bytes.fromhex('ff05')+pack32(0x10008000)+bytes.fromhex('83f802'));j=len(b);b+=bytes.fromhex('7300')
 b+=bytes.fromhex('89c3c1e30681c3')+pack32(0x10008008)
 b+=bytes.fromhex('8b44241089038b4424248943048d7b088b742414b903000000f3a58b742418b903000000f3a5be')+pack32(0x100010ED)+bytes.fromhex('b91b000000f3a4')
 end=len(b);b+=bytes.fromhex('5f5e5bc21800');assert end-j-2<128;b[j+1]=end-j-2
 return bytes(b)

def build_case(c):
 f=FAMILIES[c['family']];raw=bytearray(f['raw_bytes']);put(raw,f['raw_height'],c['height']);mid=bytes(f['mid_bytes']);definition=bytearray([165])*f['definition_bytes'];definition[0x1B:0x36]=bytes([199])*27
 entity=bytearray([165])*0x600;put(entity,0,0x10018000);put(entity,0x12,f['kind']);put(entity,0xF7,0x10002000);put(entity,0x10D,c['state']);struct.pack_into('<H',entity,0x28,7);entity[0xF6]=c['trapid'];struct.pack_into('<3I',entity,0x1A,0x11223344,0x55667788,0xFFFFFF00)
 ctx=bytearray([165])*300;put(ctx,4,0x10010000);put(ctx,0x14,0x333);struct.pack_into('<3I',ctx,0x25,100,200,0xFFFFFF00);struct.pack_into('<3I',ctx,0x31,101,201,0x80000000)
 word=(c['height']&65535) if c['presence'] else 0xA5A5;expecteddef=bytearray(definition);struct.pack_into('<H',expecteddef,f['definition_height'],word);expectedctx=bytearray(ctx);capture=bytearray(136)
 if c['mode']=='helper':expectedctx[:]=project(ctx,f['kind'],word,c['state'])
 elif c['ready']:
  put(expectedctx,8,0x10012000);put(expectedctx,12,0x10014000)
  for o in [0x41,0x45]:put(expectedctx,o,0x400)
  for o in [0x51,0x55]:put(expectedctx,o,0)
  expectedctx[0x25:0x31]=entity[0x1A:0x26];expectedctx[0x31:0x3D]=entity[0x1A:0x26];expectedctx[:]=project(expectedctx,f['kind'],word,c['state']);put(expectedctx,0x10,0x11223344);put(expectedctx,0x85,0x11223344);put(expectedctx,0x98,4)
  if not (f['kind']==3 and c['trapid']==16):
   expectedctx[0xED:0x108]=definition[0x1B:0x36];struct.pack_into('<II',capture,0,2,2)
   for i,res in enumerate([bytes([180])*27,bytes([199])*27]):
    o=8+64*i;struct.pack_into('<II',capture,o,0x1000103D,0x10003000);capture[o+8:o+20]=expectedctx[0x31:0x3D];capture[o+20:o+32]=expectedctx[0x25:0x31];capture[o+32:o+59]=res
 # Native converter returns to a driver,which first independently checks raw
 # DWORD→intermediate DWORD then snapshots other fields before scoped loader.
 driver=compare_code(0x10004000+f['mid_height'],0x10000000+f['raw_height'],4)+b'\xA3'+pack32(0x10008100)
 driver+=bytes.fromhex('c705')+pack32(0x10004000+f['presence'])+pack32(c['presence'])
 driver+=b'\xBE'+pack32(0x10004000)+b'\xBF'+pack32(0x10020000)+b'\xB9'+pack32(f['mid_bytes'])+bytes.fromhex('f3a4')
 driver+=b'\xBD'+pack32(0x10002000)+b'\xBE'+pack32(0x10004000)+b'\xB8'+pack32(f['loader'])+bytes.fromhex('ffd0b9')+pack32(0x10001000)
 if c['mode']=='caller':driver+=bytes.fromhex('6a00')
 driver+=b'\x68'+pack32(0x10003000)+b'\xB8'+pack32(0x5953D0 if c['mode']=='caller' else 0x5955C0)+bytes.fromhex('ffd0bd')+pack32(1)
 for a,b,n in [(0x10000000,0x10021000,len(raw)),(0x10001000,0x10022000,len(ctx)),(0x10002000,0x10023000,len(definition)),(0x10003000,0x10024000,len(entity)),(0x10004000,0x10020000,len(mid)),(0x10008000,0x10025000,len(capture)),(0x10008100,0x10026000,4)]:driver+=compare_code(a,b,n)+bytes.fromhex('21c5')
 driver+=END
 evt=bytearray(0x60);put(evt,0x2C,0x4B5C10);put(evt,0x5C,0x10019000)
 worldvt=bytearray(0x294)
 for o,a in [(0x10,0x10019100),(0x264,0x10019200),(0x260,0x10019300),(0x290,0x10019400)]:put(worldvt,o,a)
 cpcen=bytearray(8);put(cpcen,4,0x10011000);r4=bytearray(0x18);put(r4,0x14,0x10012000);world=pack32(0x10013000)
 # Fixture4B37D0 records resource conversion count;5958F0 remains real.
 resource=bytes.fromhex('ff05')+pack32(0x10008004)+bytes.fromhex('c21000')
 mem=[region(0x10000000,raw),region(0x10001000,ctx),region(0x10002000,definition),region(0x10003000,entity),region(0x10004000,mid),region(0x10006000,driver),region(0x10008000,bytes(136)),region(0x10008100,bytes(4)),region(0x10010000,cpcen),region(0x10011000,r4),region(0x10012000,world),region(0x10013000,worldvt),region(0x10014000,bytes(4)),region(0x10015000,bytes(0x136)),region(0x10016000,pack32(0x11223344)),region(0x10017000,bytes([180])*27),region(0x10018000,evt),region(0x10019000,const(c['ready'])),region(0x10019100,const(0x10014000)),region(0x10019200,const(7)),region(0x10019300,const(0x10015000,4)),region(0x10019400,const(0x10016000,8)),region(0x10020000,bytes(f['mid_bytes'])),region(0x10021000,raw),region(0x10022000,expectedctx),region(0x10023000,expecteddef),region(0x10024000,entity),region(0x10025000,capture),region(0x10026000,pack32(1)),region(f['loader'],jump(f['loader'],f['start'])),region(f['stop'],b'\xC3'),region(0x4B37D0,resource),region(0x59B880,capture_submission()),region(0x596EB0,const(0x10017000,4)),region(0x5B5E90,const(1000)),region(0x7FFF0000,pack32(0x10006000)+pack32(0x10000000))]
 spans=sorted((int(r['address'],16),len(bytes.fromhex(r['hex']))) for r in mem);assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:])),c['name']
 return dict(program='DKII-DX.exe',address=hex(f['converter']),max_steps=50000,registers=json.dumps(dict(ECX='0x10004000')),memory=json.dumps(dict(regions=mem)),return_registers='EAX,ECX,EBP,ESP,EIP'),dict(EAX=1,ECX=0,EBP=1)

async def main():
 selected=cases()
 if len(sys.argv)>1:selected=[c for c in selected if c['name']==sys.argv[1]];assert selected
 params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
 manifest=[]
 async with stdio_client(params) as (rd,wr):
  async with ClientSession(rd,wr) as session:
   await session.initialize()
   for c in selected:
    args,want=build_case(c);p=ROOT/'ghidra'/('phase59-emulation-overlay-v2-'+c['name']+'.json')
    if p.exists():saved=json.loads(p.read_text());assert saved['input']==args and saved['expected_registers']==want
    else:
     response=await session.call_tool('emulate_function',args);r=json.loads('\n'.join(b.text for b in response.content if b.type=='text'))
     if response.isError or 'error' in r:raise RuntimeError(str(r))
     r=r.get('data',r);saved=dict(case=c,input=args,expected_registers=want,result=r);p.write_text(json.dumps(saved,indent=2),encoding='utf8')
    r=saved['result'];assert r['success'] and r['hit_return'],(c['name'],r);assert all(int(r['registers'][reg],16)==v for reg,v in want.items()),(c['name'],r,want)
    manifest.append(dict(file=p.name,sha256=hashlib.sha256(p.read_bytes()).hexdigest(),case=c['name']));print(c['name'],'verified',flush=True)
 if len(selected)==len(cases()):(ROOT/'phase59-overlay-emulations.json').write_text(json.dumps(dict(cases=manifest,scope=__doc__),indent=2),encoding='utf8')
if __name__=='__main__':asyncio.run(main())
