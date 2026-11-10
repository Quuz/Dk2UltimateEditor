"""Full original Pro/RC3 Trap writers with bounded memory-output/date fixtures.

Every579-byte record independently projected and compared. All56 header bytes
checked in26 direct writers;12 DDX/commit chains exclude unassigned bytes46/47.
Two unassigned timestamp padding bytes come from the seeded stack at1B2/1B3;
their observed origin is not a claim of meaningful on-disk timestamp fields.
No GUI/game execution. Native helper/string/resource/flags/ftol bodies run.
"""
from pathlib import Path
import asyncio,hashlib,json,os,struct,sys
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
from emulate_element_colors import region,pack32
from emulate_hero_parties import compare_code
from native_trap_writer import PROFILES,DIRECT,SCALED,RESOURCES,record,commit_projection,COMMIT_COPY
ROOT=Path(__file__).resolve().parent
FLAGS=[(r['mask'],r['editor_model_offset']) for r in json.loads((ROOT/'trap-semantics.json').read_text())['editor_flags']['flags']]
END=b'\x68'+pack32(0xDEADBEEF)+b'\xC3'
DATE=bytes.fromhex('ea070a1b000c2238');STAMP=bytes.fromhex('112233445566778899aa')

def cases():
 rows=[]
 for profile in PROFILES:
  for value in [0,1,32767,32768,65526,65535]:rows.append(dict(name=f'{profile}-word-{value}',profile=profile,value=value,seed=165,slots=[1],flags=0xFFFFFFFF))
  for seed in [0,255,'pattern']:rows.append(dict(name=f'{profile}-seed-{seed}',profile=profile,value=65526,seed=seed,slots=[1,2],flags=0xFFFFFFFF))
  for slots in [[],[0],[1,0,2],[2,1]]:rows.append(dict(name=f'{profile}-slots-'+''.join(map(str,slots)),profile=profile,value=0x8000,seed='pattern',slots=slots,flags=0))
  for require in [0,1,2]:
   for unknown in [0,65526]:rows.append(dict(name=f'{profile}-ddx-commit-{require}-{unknown}',profile=profile,value=unknown,seed='pattern',slots=[1],flags=0xFFFFFFFF,commit=True,require=require))
 return rows

def model(n,c):
 b=bytearray(0x300);struct.pack_into('<I',b,0,0x10008000)
 struct.pack_into('<I',b,8,0x1000B008+(n-1)*0x200);struct.pack_into('<I',b,0x154,0x1000B108+(n-1)*0x200)
 for i,(dst,src,count) in enumerate(DIRECT):b[src:src+count]=((n*257+i*31)&((1<<(count*8))-1)).to_bytes(count,'little')
 for i,(_,src) in enumerate(SCALED):struct.pack_into('<d',b,src,n+i/4)
 for i,(_,src) in enumerate(RESOURCES):b[src:src+84]=bytes((j*7+i+n)&255 for j in range(84))
 struct.pack_into('<H',b,0x238,c['value']^ (0xFFFF if n==2 else 0));struct.pack_into('<I',b,0x2E4,0x12340000+n)
 for bit,off in FLAGS:struct.pack_into('<I',b,off,0x80000000 if c['flags']&bit else 0)
 return b

def capture(limit):
 # thiscall Write(pointer,length);count every call,guard length and output end.
 b=bytearray(bytes.fromhex('56578b74240c8b4c2410ff05')+pack32(0x10011000)+bytes.fromhex('8b3d')+pack32(0x10011004)+bytes.fromhex('81f943020000'));j1=len(b);b+=bytes.fromhex('7700')
 b+=bytes.fromhex('89f801c881f8')+pack32(limit);j2=len(b);b+=bytes.fromhex('7700')
 b+=bytes.fromhex('a3')+pack32(0x10011004)+bytes.fromhex('f3a45f5eb801000000c20800');fail=len(b);b+=bytes.fromhex('5f5e31c0c20800')
 for j in [j1,j2]:b[j+1]=fail-j-2
 return bytes(b)

def build_case(c):
 f=PROFILES[c['profile']];ctx=bytearray(0x600);struct.pack_into('<II',ctx,f['array'],0x10001000,len(c['slots']));ptrs=b''.join(pack32(0x10002000+(n-1)*0x1000 if n else 0) for n in c['slots']) or bytes(4)
 models={n:model(n,c) for n in [1,2]};label={n:bytes([64+n])*5 for n in [1,2]};sound={n:bytes([96+n])*7 for n in [1,2]}
 wantedmodels={n:bytes(b) for n,b in models.items()};extra=[]
 if c.get('commit'):
  panel=bytearray(0x200)
  for dst,src,n in COMMIT_COPY:panel[src:src+n]=models[1][dst:dst+n]
  struct.pack_into('<I',panel,0x148,0x10002000);struct.pack_into('<I',panel,0x120,c['require']);struct.pack_into('<I',panel,0x84,0x12345678);struct.pack_into('<I',panel,0xD8,0x24681357)
  for off in [0xB8,0xBC,0xC0,0xC4,0x198,0xD4,0xDC]:struct.pack_into('<I',panel,off,0)
  for off in [0x170,0x184,0xC8,0x1AC,0x1C0]:struct.pack_into('<I',panel,off,0x1001D000)
  wantedmodels[1],wantedpanel=commit_projection(models[1],panel,12345)
  # DDX adapter is an input provider,not a Windows/UI validation emulator.
  # Capture exactly the queried control12861 and its target panel DWORD.
  ddx=bytes.fromhex('817c24083d32000075108b44240cc70039300000ff05')+pack32(0x1001F000)+bytes.fromhex('c20c00')
  from capstone import Cs,CS_ARCH_X86,CS_MODE_32
  from capstone.x86 import X86_OP_IMM
  from build_effect_semantics import pe_read
  exe=ROOT/'source-binaries/DK2-Editor-rc3.exe' if c['profile']=='RC3' else ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/DK2 Editor pro.exe';md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True
  ins=list(md.disasm(pe_read(exe.read_bytes(),f['ddx'],1198),f['ddx']));targets={i.operands[0].imm for i in ins if i.mnemonic=='call' and i.operands[0].type==X86_OP_IMM}
  val16=0x4F1A30 if c['profile']=='RC3' else 0x4E4B31;val24=0x4F1F37 if c['profile']=='RC3' else 0x4E5038
  for t in targets:extra.append(region(t,(b'\xC2\x10\x00' if t==val16 else b'\xC2\x18\x00' if t==val24 else ddx)))
  # UpdateData, CString-copy and virtual setters do not modify fixtures.
  # Derive relocated targets from the matched native commit body.
  cc=list(md.disasm(pe_read(exe.read_bytes(),f['commit'],1026),f['commit']));direct=[i.operands[0].imm for i in cc if i.mnemonic=='call' and i.operands[0].type==X86_OP_IMM];assert len(direct)==3;update,copy,setter=direct
  extra.extend([region(update,b'\xB8'+pack32(1)+b'\xC2\x04\x00'),region(copy,b'\xC2\x04\x00')])
  # Actual setter writes model+2E4; retain its original native instructions.
  extra.extend([region(0x1001B000,panel),region(0x1001C000,wantedpanel),region(0x1001D000,bytes.fromhex('34120700000000000000')),region(0x1001F000,bytes(4)),region(0x1001F100,pack32(1))])
  # Selection byte7,trigger3,material9 require independent one-byte tables.
  for off,a,val in [(0x184,0x1001D100,7),(0xC8,0x1001D200,1),(0x1AC,0x1001D300,3),(0x1C0,0x1001D400,9)]:
   struct.pack_into('<I',panel,off,a);extra.append(region(a,bytes([val])))
  wantedmodels[1],wantedpanel=commit_projection(models[1],panel,12345)
  extra=[region(int(r['address'],16),panel if int(r['address'],16)==0x1001B000 else wantedpanel) if int(r['address'],16) in [0x1001B000,0x1001C000] else r for r in extra]
 expected=b''.join(record(wantedmodels[n],label[n],sound[n],flag_fields=FLAGS) for n in c['slots'] if n)
 stack=bytes((i*73+19)&255 for i in range(0x400)) if c['seed']=='pattern' else bytes([c['seed']])*0x400
 # Independent fixed header constraints,including N counting null slots even
 # though native writer skips their records. Padding/timestamps compared too.
 header=struct.pack('<7I',130,4,56+579*len(c['slots']),131,28,len(c['slots']),1)+STAMP+DATE+stack[0x1B2:0x1B4]+struct.pack('<II',132,579*len(c['slots']))
 assert len(header)==56
 vt=bytearray(0x48)
 for o,a in [(0x2C,0x10009000),(0x38,0x10009020),(0x40,0x10009040)]:struct.pack_into('<I',vt,o,a)
 if c.get('commit'):
  for o in [8,0x34,0x3C,0x44]:struct.pack_into('<I',vt,o,0x10009060)
 filevt=bytearray(0x40);struct.pack_into('<I',filevt,0x38,0x1000A000)
 ret=lambda v:b'\xB8'+pack32(v)+b'\xC3'
 date=bytes.fromhex('8b442404c700')+DATE[:4]+bytes.fromhex('c74004')+DATE[4:]+bytes.fromhex('c20400')
 readout=bytes.fromhex('0fb6c0a3')+pack32(0x10011008)+b'\xBD'+pack32(1)
 checks=[(0x10012000,0x10014000,56+len(expected))] if not c.get('commit') else [(0x10012000,0x10014000,46),(0x10012030,0x10014030,8+len(expected))]
 checks += [(0x10000000,0x10015000,len(ctx)),(0x10002000,0x10016000,0x300),(0x10003000,0x10017000,0x300),(0x10001000,0x10018000,len(ptrs)),(0x10011000,0x10019000,12)]
 for a,b,n in checks:readout+=compare_code(a,b,n)+bytes.fromhex('21c5')
 if c.get('commit'):
  for a,b,n in [(0x1001B000,0x1001C000,0x200),(0x1001F000,0x1001F100,4)]:readout+=compare_code(a,b,n)+bytes.fromhex('21c5')
 readout+=END
 mem=[region(0x10000000,ctx),region(0x10001000,ptrs),region(0x10002000,models[1]),region(0x10003000,models[2]),region(0x10008000,vt),region(0x10009000,ret(21)),region(0x10009020,ret(0x12345678)),region(0x10009040,ret(0x89ABCDEF)),region(0x10009060,b'\xC2\x04\x00'),region(0x1000A000,capture(0x10013000)),region(0x1000C000,pack32(0x1000D000)),region(0x1000D000,filevt),region(0x10011000,struct.pack('<3I',0,0x10012000,0)),region(0x10012000,bytes(0x1000)),region(0x10014000,header+expected),region(0x10015000,ctx),region(0x10016000,wantedmodels[1]),region(0x10017000,wantedmodels[2]),region(0x10018000,ptrs),region(0x10019000,struct.pack('<3I',3+sum(bool(n) for n in c['slots']),0x10012000+56+len(expected),1)),region(0x1001A000,readout),region(f['date_global'],STAMP),region(f['date_helper'],date),region(0x7FFEFC00,stack),region(0x7FFF0000,pack32(0x1001A000)+pack32(0x1000C000)),*extra]
 entry=f['writer'];regs=dict(ECX='0x10000000')
 if c.get('commit'):
  driver=b'\xB9'+pack32(0x1001B000)+b'\xB8'+pack32(f['commit'])+bytes.fromhex('ffd0b9')+pack32(0x10000000)+b'\x68'+pack32(0x1000C000)+b'\xB8'+pack32(f['writer'])+bytes.fromhex('ffd0')+b'\x68'+pack32(0x1001A000)+b'\xC3'
  mem=[region(0x7FFF0000,pack32(0x1001E000)+pack32(0x1001F000)) if int(r['address'],16)==0x7FFF0000 else r for r in mem]
  mem.append(region(0x1001E000,driver));entry=f['ddx'];regs=dict(ECX='0x1001b000')
 for n in [1,2]:
  for delta,value in [(0,label[n]),(0x100,sound[n])]:mem.append(region(0x1000B000+(n-1)*0x200+delta,pack32(len(value))+pack32(0)+value+b'\0'))
 spans=sorted((int(r['address'],16),len(bytes.fromhex(r['hex']))) for r in mem);assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:])),c['name']
 return dict(program=f['program'],address=hex(entry),max_steps=50000,registers=json.dumps(regs),memory=json.dumps(dict(regions=mem)),return_registers='EAX,ECX,EBP,ESP,EIP'),dict(EAX=1,ECX=0,EBP=1)

async def main():
 selected=cases()
 if len(sys.argv)>1:selected=[c for c in selected if c['name']==sys.argv[1]];assert selected
 params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
 manifest=[]
 async with stdio_client(params) as (rd,wr):
  async with ClientSession(rd,wr) as session:
   await session.initialize()
   for c in selected:
    args,want=build_case(c);p=ROOT/'ghidra'/('phase60-emulation-trap-writer-v4-'+c['name']+'.json')
    if p.exists():saved=json.loads(p.read_text());assert saved['input']==args and saved['expected_registers']==want
    else:
     response=await session.call_tool('emulate_function',args);r=json.loads('\n'.join(b.text for b in response.content if b.type=='text'))
     if response.isError or 'error' in r:raise RuntimeError(str(r))
     r=r.get('data',r);saved=dict(case=c,input=args,expected_registers=want,result=r);p.write_text(json.dumps(saved,indent=2),encoding='utf8')
    r=saved['result'];assert r['success'] and r['hit_return'],(c['name'],r);assert all(int(r['registers'][reg],16)==v for reg,v in want.items()),(c['name'],r,want)
    manifest.append(dict(file=p.name,sha256=hashlib.sha256(p.read_bytes()).hexdigest(),case=c['name']));print(c['name'],'verified',flush=True)
 if len(selected)==len(cases()):(ROOT/'phase60-trap-writer-emulations.json').write_text(json.dumps(dict(cases=manifest,scope=__doc__),indent=2),encoding='utf8')
if __name__=='__main__':asyncio.run(main())
