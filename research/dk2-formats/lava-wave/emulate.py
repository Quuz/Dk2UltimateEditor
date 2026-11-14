from pathlib import Path
import sys,json,struct,math,hashlib,argparse
ap=argparse.ArgumentParser()
ap.add_argument('--research-root',type=Path,required=True)
ap.add_argument('--exe',type=Path,required=True)
ap.add_argument('--output',type=Path,required=True)
cli=ap.parse_args()
sys.path.insert(0,str(cli.research_root))
from native_unicorn_factorytrace import run
EXE=cli.exe.resolve()
fingerprint=hashlib.sha256(EXE.read_bytes()).hexdigest()
assert fingerprint=='d7a8b854e4e4f02709eba271bead6fe2307dd6e332b5a96e70857e5b0e19fb8a'
OUT=cli.output.resolve()
OUT.mkdir(parents=True,exist_ok=True)
GFX,GRID=0x18000000,0x18001000
STOP=0x5a2df8
ROWS=[]
fb=lambda x:struct.unpack('<f',struct.pack('<f',x))[0]
pi=fb(math.pi)
for x,y in [(2,3),(7,4)]:
 for phase in [0,8,16,32,64,96,127,128]:
  gfx=bytearray(0x100);struct.pack_into('<I',gfx,0xc9,phase)
  grid=bytearray(0x24);struct.pack_into('<I',grid,0x20,2)
  args=dict(address=hex(STOP),registers='{}',memory=json.dumps(dict(regions=[dict(address=hex(a),hex=b.hex()) for a,b in [(GFX,gfx),(GRID,grid),(0,bytes(4096)),(0x7ffec000,bytes(32768))]])),
    before_calls=[dict(address='0x5a2be0',registers={'ECX':hex(GFX)},stack_args=[x,y,0,GRID,0,0,0,0])],max_steps=50000,observe_reads=[],observe_writes=[])
  r=run(EXE,args,[(GFX,gfx),(GRID,grid)],STOP,0x7ffefae4,36)
  assert r['success'] and not r['context_mismatches'] and not r['protected_write_or_execution_failures'],(x,y,phase,r['emulation_error'],r['trace_tail'])
  values=list(struct.unpack('<9f',bytes.fromhex(r['captured_hex'])))
  expected=[]
  for iy in range(3):
   sy=fb(math.sin(((iy*32-y*64+phase)&127)*pi/64)*.04)
   for ix in range(3):
    sx=math.sin(((ix*32-x*64+phase)&127)*pi/64)*.04
    expected.append(fb(sx+sy-fb(.85)))
  assert values==expected,(phase,values,expected)
  ROWS.append(dict(x=x,y=y,phase=phase,segments=2,heights=values,grid_preserved=True,gfx_preserved=True,
    instructions=r['instruction_count'],substitutions=r['function_or_vtable_substitutions']))
assert hashlib.sha256(EXE.read_bytes()).hexdigest()==fingerprint
(OUT/'cases.json').write_text(json.dumps(dict(scope='Original lava renderer 5A2BE0 height sampling, prepared grid, before allocation/GPU',source_exe_sha256=hashlib.sha256(EXE.read_bytes()).hexdigest(),cases=ROWS),indent=2),encoding='utf8')
print(len(ROWS),'native lava grids,',len(ROWS)*9,'heights matched independently')

