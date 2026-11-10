"""Full native Terrain parser/init/converter/callback/loader/getter compositions.

Only low-level file read and sound handle provider are bounded memory fixtures.
All256 definition slots preallocated; graphics provider null; resources absent.
No live engine, GUI, OS file I/O or original-file modifications.
"""
from pathlib import Path
import asyncio,hashlib,json,os,struct,sys
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
from emulate_element_colors import region,pack32
from emulate_hero_parties import compare_code
from native_terrain_catalogue import intermediate,definition,put,u32,lookup_address
ROOT=Path(__file__).resolve().parent
END=b'\x68'+pack32(0xDEADBEEF)+b'\xC3'
IDS=[0,1,3,7,22,255,256,511,0xFFFFFFFF]

def read_fixture(end):
 b=bytearray(bytes.fromhex('5356578b7c24188b4c241c8b35')+pack32(0x10003000))
 b+=bytes.fromhex('81f928020000');j1=len(b);b+=bytes.fromhex('7700')
 b+=bytes.fromhex('81fe')+pack32(0x10300000);j2=len(b);b+=bytes.fromhex('7200')
 b+=bytes.fromhex('89f201ca81fa')+pack32(end);j3=len(b);b+=bytes.fromhex('7700')
 b+=bytes.fromhex('89cbf3a48935')+pack32(0x10003000)+bytes.fromhex('ff05')+pack32(0x10003004)+bytes.fromhex('8b44242089188b442410c700000000005f5e5bc3')
 fail=len(b);b+=bytes.fromhex('8b442410c700ffffffff5f5e5bc3')
 for j in [j1,j2,j3]:assert fail-j-2<128;b[j+1]=fail-j-2
 return bytes(b)

def cases():
 out=[]
 for gate in [0,1]:
  for update in [False,True]:
   for word,dword in [(0,0),(65535,0xFFFFFFFF),(508,4628)]:
    out.append(dict(name=f'pipeline-{gate}-{int(update)}-{word}-{dword}',kind='pipeline',gate=gate,update=update,word=word,dword=dword,ids=[7,3,255],stride=552,header=True))
 for gate in [0,1]:
  out.append(dict(name=f'special-id22-{gate}',kind='pipeline',gate=gate,update=False,word=504,dword=0x80000000,ids=[22,0,255],stride=552,header=True))
  for update in [False,True]:
   out.append(dict(name=f'duplicate-{gate}-{int(update)}',kind='pipeline',gate=gate,update=update,word=0x8000,dword=0x80000000,ids=[7,7,3],stride=552,header=False))
  out.append(dict(name=f'short548-{gate}',kind='pipeline',gate=gate,update=False,word=65535,dword=0xFFFFFFFF,ids=[7,3,255],stride=548,header=True))
  out.append(dict(name=f'stale-water-{gate}',kind='pipeline',gate=gate,update=False,word=508,dword=4628,ids=[7,3,7],flags=[0x80,0x100,0],stride=552,header=True))
 for gate in [0,1]:
  for presence in [0,1,2,0xFFFFFFFF]:
   out.append(dict(name=f'partial-{gate}-{presence}',kind='partial',gate=gate,presence=presence,word=65535,dword=0xFFFFFFFF))
 for name,word,dword in [('a',508,0x01000000),('b',763,0x00010000)]:
  out.append(dict(name='checksum-collision-'+name,kind='pipeline',gate=0,update=False,word=word,dword=dword,ids=[7],stride=552,header=True))
 return out

def records(c):
 out=[]
 for j,ident in enumerate(c['ids']):
  r=bytearray(552);r[:8]=b'terrain\0';r[0x1D6]=ident;r[0x1D9]=ident+1 if ident<255 else 1
  struct.pack_into('<H',r,0x1D7,10+j)
  r[0x1D9]=ident+1 if ident<255 else 1;struct.pack_into('<H',r,0x1FF,20+j)
  put(r,0x188,0xFFFF1234+j);put(r,0x18C,0x11223344+j)
  put(r,0x190,c.get('flags',[0x80,0x100,0x180])[j]);r[0x1DB:0x1DE]=bytes([j,127,255]);r[0x201:0x204]=bytes([255,127,j])
  struct.pack_into('<H',r,0x198,c['word']^j);put(r,0x224,c['dword']^j)
  r[0x1DF:0x1E4]=b'base\0';r[0x204:0x208]=b'alt\0'
  r[0x170:0x188]=bytes(range(24))
  for k in range(16):struct.pack_into('<H',r,0x1AE+2*k,0x8000+k)
  out.append(bytes(r))
 return out

def build_case(c):
 world=bytearray(0xA3CC);put(world,0,0x10001000);put(world,0x15A4,0x10100000)
 put(world,0x15A8,5);put(world,0x15AC,9);put(world,0x15B0,0x1122);put(world,0xA3C7,int(c.get('update',False)))
 world[0x9BAE:0x9BB0]=bytes([201,202]);expectedworld=bytearray(world)
 initial=bytes([165])*(460*256);expectedarray=bytearray(initial);calls=0
 control=bytearray(16);expectedcontrol=bytearray(16);vt=bytearray(0x74);put(vt,0x70,0x508DD0)
 callbacks=bytearray(0x14);put(callbacks,8,0x51EB30 if c.get('header') else 0);put(callbacks,12,0x51EB50);put(callbacks,16,0x10010000)
 extra=[];stream=b'';mids=[]
 if c['kind']=='pipeline':
  rr=records(c);actual=[r[:c['stride']]+bytes(552-c['stride']) for r in rr]
  stream=bytearray(44);struct.pack_into('<II',stream,0,0x6F,28);put(stream,8,len(rr));struct.pack_into('<II',stream,36,0x70,c['stride']*len(rr));stream+=b''.join(r[:c['stride']] for r in rr)
  put(control,0,0x10300000)
  for r in actual:
   mid=intermediate(r);mids.append(mid);ident=r[0x1D6];i=460*ident
   new,calls=definition(mid,expectedarray[i:i+460],c['gate'],calls);expectedarray[i:i+460]=new
   if not c['update']:
    put(expectedworld,0x15AC,u32(expectedworld,0x15AC)+1);put(expectedworld,0x15B0,u32(expectedworld,0x15B0)+u32(new,0x84))
    if u32(new,0x17B)&0x80:expectedworld[0x9BAE]=ident
    if u32(new,0x17B)&0x100:expectedworld[0x9BAF]=ident
  if c['header']:put(expectedworld,0x15A8,5+len(rr))
  struct.pack_into('<4I',expectedcontrol,0,0x10300000+len(stream),2+len(rr),1,calls)
  fixture=read_fixture(0x10300000+len(stream))
  extra=[region(0x5B3EB0,fixture),region(0x10300000,stream),region(0x10310000,stream)]
  entry=0x56F090;args=[0x10000000,0x10002000];regs={}
 else:
  mid=bytearray(736);put(mid,0x30,c['presence']);put(mid,0x98,c['presence']);struct.pack_into('<H',mid,0x250,c['word']);put(mid,0x2DC,c['dword']);mids=[bytes(mid)]
  initial=bytearray(initial);i=460*7
  # Only required final derived fields are seeded; no graphical flags/resources.
  put(initial,i+0x17B,0);initial[i+0x1C4]=7;initial=bytes(initial);expectedarray=bytearray(initial)
  new,calls=definition(mid,initial[i:i+460],c['gate']);expectedarray[i:i+460]=new
  # Void loader has no claimed EAX return. Readout will normalize parser capture.
  put(expectedcontrol,8,1);put(expectedcontrol,12,calls)
  extra=[region(0x7470E8,mid)];entry=0x4F66E0;args=[0x7470E8,0];regs={'ECX':hex(0x10100000+i)}
 # Native constructor is exercised after every path on a separate sentinel buffer.
 ctor=bytearray([165])*460;expectedctor=bytearray(460);expectedctor[0x84:0x88]=bytes([165])*4;expectedctor[0x1CB]=164
 readout=(b'\xA3'+pack32(0x10003008)) if c['kind']=='pipeline' else (b'\xC7\x05'+pack32(0x10003008)+pack32(1))
 readout+=b'\xB9'+pack32(0x10324000)+b'\xB8'+pack32(0x4F6570)+bytes.fromhex('ffd0bd')+pack32(1)
 for a,b,n in [(0x10100000,0x10200000,len(initial)),(0x10010000,0x10020000,len(world)),(0x7470E8,0x10320000,736),(0x10003000,0x10321000,16),(0x10324000,0x10325000,460)]:readout+=compare_code(a,b,n)+bytes.fromhex('21c5')
 if stream:readout+=compare_code(0x10300000,0x10310000,len(stream))+bytes.fromhex('21c5')
 for k,ident in enumerate(IDS):readout+=b'\xB9'+pack32(0x10010000)+b'\x68'+pack32(ident)+b'\xB8'+pack32(0x508DD0)+bytes.fromhex('ffd0a3')+pack32(0x10322000+4*k)
 readout+=compare_code(0x10322000,0x10323000,4*len(IDS))+bytes.fromhex('21c5')+END
 sound=bytes.fromhex('ff05')+pack32(0x1000300C)+b'\xA1'+pack32(0x1000300C)+bytes.fromhex('0500a10000c20800')
 soundvt=bytearray(0x20);put(soundvt,0x1C,0x10004200)
 mem=[region(0x10000000,pack32(123)),region(0x10001000,vt),region(0x10002000,callbacks),region(0x10003000,control),region(0x10004000,soundvt),region(0x10004100,pack32(0x10004000)),region(0x10004200,sound),region(0x10006000,readout),region(0x10010000,world),region(0x10020000,expectedworld),region(0x10100000,initial),region(0x10200000,expectedarray),region(0x10320000,mids[-1]),region(0x10321000,expectedcontrol),region(0x10322000,bytes(4*len(IDS))),region(0x10323000,b''.join(pack32(lookup_address(0x10100000,i)) for i in IDS)),region(0x10324000,ctor),region(0x10325000,expectedctor),region(0x6FCDCF,pack32(c['gate'])),region(0x75B468,pack32(0x10004100)),region(0x7FFF0000,pack32(0x10006000)+b''.join(pack32(v) for v in args)),*extra]
 spans=sorted((int(r['address'],16),len(bytes.fromhex(r['hex']))) for r in mem);assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:])),c['name']
 return dict(program='DKII-DX.exe',address=hex(entry),max_steps=350000,registers=json.dumps(regs),memory=json.dumps(dict(regions=mem)),return_registers='EAX,ECX,EBP,ESP,EIP'),dict(EAX=1,ECX=0,EBP=1)

async def main():
 selected=cases()
 if len(sys.argv)>1:selected=[c for c in selected if c['name']==sys.argv[1]];assert selected
 params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
 manifest=[]
 async with stdio_client(params) as (rd,wr):
  async with ClientSession(rd,wr) as session:
   await session.initialize()
   for c in selected:
    args,want=build_case(c);p=ROOT/'ghidra'/('phase58-emulation-terrain-v2-'+c['name']+'.json')
    if p.exists():saved=json.loads(p.read_text());assert saved['input']==args and saved['expected_registers']==want
    else:
     response=await session.call_tool('emulate_function',args);r=json.loads('\n'.join(b.text for b in response.content if b.type=='text'))
     if response.isError or 'error' in r:raise RuntimeError(str(r))
     r=r.get('data',r);saved=dict(case=c,input=args,expected_registers=want,result=r);p.write_text(json.dumps(saved,indent=2),encoding='utf8')
    r=saved['result'];assert r['success'] and r['hit_return'],(c['name'],r);assert all(int(r['registers'][reg],16)==v for reg,v in want.items()),(c['name'],r,want)
    manifest.append(dict(file=p.name,sha256=hashlib.sha256(p.read_bytes()).hexdigest(),case=c['name']));print(c['name'],'verified',flush=True)
 if len(selected)==len(cases()):(ROOT/'phase58-terrain-emulations.json').write_text(json.dumps(dict(cases=manifest,scope=__doc__),indent=2),encoding='utf8')
if __name__=='__main__':asyncio.run(main())
