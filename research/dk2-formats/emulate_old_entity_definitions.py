"""Whole old native lookups and structure overlay helper, without renderer calls.

Class/initialization bindings are separate static evidence, not emulated here.
All world/definition/entity/context input bytes compared or explicitly projected.
"""
from pathlib import Path
import asyncio,hashlib,json,os,struct,sys
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
from emulate_element_colors import region,pack32
from emulate_hero_parties import compare_code
from native_structure_overlay import project
from native_entity_definitions import FAMILIES,lookup_index
ROOT=Path(__file__).resolve().parent
END=b'\x68'+pack32(0xDEADBEEF)+b'\xC3'
def cases():
 rows=[]
 for f in FAMILIES:
  for name,ids,key in [('empty',[],1),('zero',[0],0),('sparse',[1,3,7],7),('missing',[1,3,7],5),('duplicate',[1,3,3],3),('upperbits',[1,3,7],0x107),('unsorted',[7,1,3],7)]:rows.append(dict(name=f'lookup-{f}-{name}',kind='lookup',family=f,ids=ids,key=key))
 for f,h,state,unknown in [('Traps',0,0,65526),('Traps',32768,0,0),('Traps',65535,0,65535),('Doors',0,0,0),('Doors',65535,0,0),('Doors',65535,1,0),('Doors',0,2,0),('Doors',1,0xFFFFFFFF,0),('Traps',100,0,0),('Traps',100,0,65535)]:rows.append(dict(name=f'overlay-{f}-{h}-{state}-{unknown}',kind='overlay',family=f,height=h,state=state,unknown=unknown))
 return rows
def build_case(c):
 f=FAMILIES[c['family']];mem=[];readout=b'\xBD'+pack32(1)
 if c['kind']=='lookup':
  table=bytearray((i*37+5)&255 for i in range(max(1,len(c['ids']))*f['definition_bytes']))
  for i,n in enumerate(c['ids']):table[i*f['definition_bytes']+f['definition_id']]=n
  world=bytearray((i*7+11)&255 for i in range(0x1700));struct.pack_into('<I',world,f['world_array'],0x10002000);struct.pack_into('<I',world,f['world_count'],len(c['ids']));index=lookup_index(c['ids'],c['key']);value=0 if index is None else 0x10002000+index*f['definition_bytes']
  readout+=b'\x3D'+pack32(value)+bytes.fromhex('0f94c00fb6c021c5')
  for a,b,n in [(0x10000000,0x10004000,len(world)),(0x10002000,0x10006000,len(table))]:readout+=compare_code(a,b,n)+bytes.fromhex('21c5')
  entry=f['lookup_old'];regs=dict(ECX='0x10000000');mem=[region(0x10000000,world),region(0x10004000,world),region(0x10002000,table),region(0x10006000,table)];stack=pack32(0x10008000)+pack32(c['key'])
 else:
  definition=bytearray((i*37+5)&255 for i in range(f['definition_bytes']));height=0xA0 if c['family']=='Traps' else 0x97;struct.pack_into('<H',definition,height,c['height'])
  if c['family']=='Traps':struct.pack_into('<H',definition,0x98,c['unknown'])
  entity=bytearray((i*29+7)&255 for i in range(512));struct.pack_into('<I',entity,0x12,f['kind']);struct.pack_into('<I',entity,0xF6,0x10002000);struct.pack_into('<I',entity,0x10C,c['state']);context=bytes((i*73+19)&255 for i in range(300));expected=project(context,f['kind'],c['height'],c['state'])
  for a,b,n in [(0x10000000,0x10004000,300),(0x10001000,0x10005000,512),(0x10002000,0x10006000,len(definition))]:readout+=compare_code(a,b,n)+bytes.fromhex('21c5')
  mem=[region(0x10000000,context),region(0x10004000,expected),region(0x10001000,entity),region(0x10005000,entity),region(0x10002000,definition),region(0x10006000,definition)];entry=0x58ACA0;regs=dict(ECX='0x10000000');stack=pack32(0x10008000)+pack32(0x10001000)
 readout+=b'\x89\xE8'+END;mem.extend([region(0x10008000,readout),region(0x7FFF0000,stack)]);spans=sorted((int(r['address'],16),len(bytes.fromhex(r['hex']))) for r in mem);assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:])),c['name']
 return dict(program='DKII-local13.exe',address=hex(entry),max_steps=30000,registers=json.dumps(regs),memory=json.dumps(dict(regions=mem)),return_registers='EAX,ECX,EBP,ESP,EIP'),dict(EAX=1,ECX=0,EBP=1)
async def main():
 selected=cases()
 if len(sys.argv)>1:selected=[c for c in selected if c['name']==sys.argv[1] or c['kind']==sys.argv[1]];assert selected
 params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'});manifest=[]
 async with stdio_client(params) as (rd,wr):
  async with ClientSession(rd,wr) as session:
   await session.initialize()
   for c in selected:
    args,want=build_case(c);path=ROOT/'ghidra'/('phase62-emulation-old-entity-'+c['name']+'.json')
    if path.exists():saved=json.loads(path.read_text());assert saved['input']==args and saved['expected_registers']==want
    else:
     response=await session.call_tool('emulate_function',args);r=json.loads('\n'.join(b.text for b in response.content if b.type=='text'))
     if response.isError or 'error' in r:raise RuntimeError(str(r))
     saved=dict(case=c,input=args,expected_registers=want,result=r.get('data',r));path.write_text(json.dumps(saved,indent=2))
    r=saved['result'];assert r['success'] and r['hit_return'],(c['name'],r);assert all(int(r['registers'][k],16)==v for k,v in want.items()),(c['name'],r,want);manifest.append(dict(file=path.name,sha256=hashlib.sha256(path.read_bytes()).hexdigest(),case=c['name']));print(c['name'],'verified',flush=True)
 if len(selected)==len(cases()):(ROOT/'phase62-entity-emulations.json').write_text(json.dumps(dict(cases=manifest,scope=__doc__),indent=2))
if __name__=='__main__':asyncio.run(main())
