"""Complete old/current Trap initializers, converters and loaders with bounded providers.

Null visual-resource provider; explicit sound lookup captures prefix and32-byte
name and returns a token. Real bounded strncpy executes. No Windows/game run.
All747 intermediate/209 definition/579 raw bytes are compared independently.
"""
from pathlib import Path
import asyncio,hashlib,json,os,struct,sys
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
from emulate_element_colors import region,pack32
from emulate_hero_parties import compare_code
from native_trap_loading import *
ROOT=Path(__file__).resolve().parent
END=b'\x68'+pack32(0xDEADBEEF)+b'\xC3'
TOKEN=0xA1B2C3D4

def cases():
 rows=[]
 for sha in PROFILES:
  for seed in [0,165,255]:rows.append(dict(name=f'init-{sha[:3]}-{seed}',kind='init',sha=sha,seed=seed))
  for seed in [0,165,255]:
   for terminated in [False,True]:rows.append(dict(name=f'convert-{sha[:3]}-{seed}-{int(terminated)}',kind='converter',sha=sha,seed=seed,terminated=terminated))
  for v,tps in zip([0,1,32767,32768,65526,65535],[0,1,20,4095,4096,0xFFFFFFFF]):rows.append(dict(name=f'load-{sha[:3]}-{v}',kind='loader',sha=sha,value=v,tps=tps,gate=0,variant='full'))
  for variant in ['missing','present2','presentFF','allmissing']:rows.append(dict(name=f'load-{sha[:3]}-{variant}',kind='loader',sha=sha,value=65526,tps=20,gate=1,variant=variant))
  for value in [0,65535]:rows.append(dict(name=f'load-{sha[:3]}-gate1-{value}',kind='loader',sha=sha,value=value,tps=0xFFFFFFFF,gate=1,variant='full'))
  for value,seed,gate in [(0,0,0),(1,165,0),(32767,255,0),(32768,0,1),(65526,165,1),(65535,255,1),(255,0,0),(65280,0,0)]:rows.append(dict(name=f'chain-{sha[:3]}-{value}',kind='chain',sha=sha,value=value,seed=seed,tps=20,gate=gate,terminated=False))
  rows.append(dict(name=f'initialized-chain-{sha[:3]}',kind='initialized_chain',sha=sha,value=65526,seed=165,tps=20,gate=1,terminated=False))
 return rows

def raw_record(c):
 raw=bytearray((i*47+13)&255 for i in range(579));struct.pack_into('<H',raw,0x204,c.get('value',0xFFF6));struct.pack_into('<I',raw,0x1EC,0x11223344)
 if c.get('terminated',True):raw[:32]=b'Trap\0'+bytes([173])*27;raw[0x20F:0x22F]=b'Sound\0'+bytes([191])*26
 else:raw[:32]=b'ABCDEFGHIJKLMNOPQRSTUVWXYZ123456';raw[0x20F:0x22F]=b'abcdefghijklmnopqrstuvwxyz123456'
 return bytes(raw)

def build_case(c):
 p=profile(c['sha']);raw=raw_record(c);prior=bytes([c.get('seed',165)])*747;mid=bytearray(intermediate(raw,prior));kind=c['kind'];gate=c.get('gate',0);tps=c.get('tps',20)
 if kind=='loader':
  variant=c['variant']
  if variant=='allmissing':mid[:0xA8]=bytes(0xA8)
  elif variant=='missing':struct.pack_into('<I',mid,0x5C,0);struct.pack_into('<I',mid,8,0);struct.pack_into('<I',mid,0x9C,0)
  elif variant in ['present2','presentFF']:struct.pack_into('<I',mid,0x5C,2 if variant=='present2' else 0xFFFFFFFF)
 if kind=='init':mid=bytearray(747)
 elif kind=='initialized_chain':mid=bytearray(intermediate(raw,bytes(747)))
 previous=bytes((i*29+7)&255 for i in range(209));expected=definition(mid,previous,tps,c['sha'],gate,TOKEN) if kind not in ['converter','init'] else previous
 called=kind not in ['converter','init'] and sound_requested(mid,c['sha'],gate)
 capture_expected=pack32(1)+pack32(p['sound_prefix'])+string32(bytes(mid[0x2B7:0x2D7])) if called else bytes(40)
 bufprior=bytes([165])*32;bufexpected=string32(bytes(mid[0x2B7:0x2D7])) if called else bufprior
 # Preserve callee-saved registers; copy a fixed32-byte native lookup name.
 sound=bytes.fromhex('56578b44240ca3')+pack32(0x10006304)+bytes.fromhex('8b742410bf')+pack32(0x10006308)+b'\xB9'+pack32(32)+bytes.fromhex('f3a4ff05')+pack32(0x10006300)+b'\xB8'+pack32(TOKEN)+bytes.fromhex('5f5ec20800')
 vt=bytearray(32);struct.pack_into('<I',vt,0x1C,0x10006200)
 checks=[(0x10000000,0x10001000,579),(0x10002000,0x10003000,747),(0x10004000,0x10005000,209),(0x10006300,0x10006400,40),(p['sound_buffer'],0x10006500,32)]
 readout=b'\xBD'+pack32(1)
 for a,b,n in checks:readout+=compare_code(a,b,n)+bytes.fromhex('21c5')
 readout+=END
 mem=[region(0x10000000,raw),region(0x10001000,raw),region(0x10002000,prior if kind in ['converter','chain','init','initialized_chain'] else mid),region(0x10003000,mid),region(0x10004000,previous),region(0x10005000,expected),region(0x10006000,pack32(0x10006100)),region(0x10006100,vt),region(0x10006200,sound),region(0x10006300,bytes(40)),region(0x10006400,capture_expected),region(0x10006500,bufexpected),region(0x10008000,readout),region(p['sound_global'],pack32(0x10006000)),region(p['sound_buffer'],bufprior),region(p['tps'],pack32(tps)),region(0x6FCDCF,pack32(gate))]
 if kind=='loader':entry=p['loader'];regs=dict(ECX='0x10004000');stack=pack32(0x10008000)+pack32(0x10002000)+pack32(0)
 elif kind=='converter':entry=p['converter'];regs=dict(ECX='0x10002000');stack=pack32(0x10008000)+pack32(0x10000000)
 elif kind=='init':entry=p['init'];regs=dict(ECX='0x10002000');stack=pack32(0x10008000)
 else:
  entry=p['converter'];regs=dict(ECX='0x10002000');stack=pack32(0x10007000)+pack32(0x10000000)
  driver=b'\xB9'+pack32(0x10004000)+bytes.fromhex('6a0068')+pack32(0x10002000)+b'\xB8'+pack32(p['loader'])+bytes.fromhex('ffd068')+pack32(0x10008000)+b'\xC3'
  if kind=='initialized_chain':
   entry=p['init'];stack=pack32(0x10007000);driver=b'\xB9'+pack32(0x10002000)+b'\x68'+pack32(0x10000000)+b'\xB8'+pack32(p['converter'])+bytes.fromhex('ffd0')+driver
  mem.append(region(0x10007000,driver))
 mem.append(region(0x7FFF0000,stack));spans=sorted((int(r['address'],16),len(bytes.fromhex(r['hex']))) for r in mem);assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:])),c['name']
 return dict(program=p['program'],address=hex(entry),max_steps=30000,registers=json.dumps(regs),memory=json.dumps(dict(regions=mem)),return_registers='EAX,ECX,EBP,ESP,EIP'),dict(EAX=1,ECX=0,EBP=1)

async def main():
 selected=cases()
 if len(sys.argv)>1:selected=[c for c in selected if c['name']==sys.argv[1] or c['kind']==sys.argv[1]];assert selected
 params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'});manifest=[]
 async with stdio_client(params) as (rd,wr):
  async with ClientSession(rd,wr) as session:
   await session.initialize()
   for c in selected:
    args,want=build_case(c);path=ROOT/'ghidra'/('phase61-emulation-trap-loading-'+c['name']+'.json')
    if path.exists():saved=json.loads(path.read_text());assert saved['input']==args and saved['expected_registers']==want
    else:
     response=await session.call_tool('emulate_function',args);r=json.loads('\n'.join(b.text for b in response.content if b.type=='text'))
     if response.isError or 'error' in r:raise RuntimeError(str(r))
     saved=dict(case=c,input=args,expected_registers=want,result=r.get('data',r));path.write_text(json.dumps(saved,indent=2))
    r=saved['result'];assert r['success'] and r['hit_return'],(c['name'],r);assert all(int(r['registers'][reg],16)==v for reg,v in want.items()),(c['name'],r,want)
    manifest.append(dict(file=path.name,sha256=hashlib.sha256(path.read_bytes()).hexdigest(),case=c['name']));print(c['name'],'verified',flush=True)
 if len(selected)==len(cases()):(ROOT/'phase61-trap-loading-emulations.json').write_text(json.dumps(dict(cases=manifest,scope=__doc__),indent=2))
if __name__=='__main__':asyncio.run(main())
