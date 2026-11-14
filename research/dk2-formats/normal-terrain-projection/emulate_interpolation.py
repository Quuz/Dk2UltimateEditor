from pathlib import Path
import sys,json,struct,math,hashlib,argparse
ap=argparse.ArgumentParser()
ap.add_argument('--research-root',type=Path,required=True)
ap.add_argument('--output',type=Path,required=True)
ap.add_argument('--exe',type=Path,required=True)
cli=ap.parse_args()
ROOT=cli.research_root.resolve()
sys.path.insert(0,str(ROOT))
from native_unicorn_factorytrace import run
import emulate_terrain_lean as setup
import native_terrain_lean as lean
sys.path.insert(0,str(Path(__file__).parent))
from normal_terrain_model import grid
OUT=cli.output.resolve()
OUT.mkdir(parents=True,exist_ok=True)
raw=json.loads((Path(__file__).with_name('wall-positions.json')).read_text())
EXE=cli.exe.resolve()
fingerprint=hashlib.sha256(EXE.read_bytes()).hexdigest()
assert fingerprint=='d7a8b854e4e4f02709eba271bead6fe2307dd6e332b5a96e70857e5b0e19fb8a'
CTX,TABLE,GRIDS,OUTPUT=0x18000000,0x18001000,0x18002000,0x18010000
fb=lambda v:struct.unpack('<I',struct.pack('<f',v))[0]
rows=[]
for topology in ['isolated','checker']:
 for mode in ['zero','HV','W']:
  h=[128 if mode=='HV' else 0]*3;v=h
  wh=wv=128 if mode=='W' else 0
  # Independent lattice projection supplies the prepared interpolation grids.
  blocks=[]
  for x in range(1,5):
   for y in range(2,6):
    occ=[int(setup.occupied(dict(topology=topology),xx,yy)) for yy in range(y-1,y+3) for xx in range(x-1,x+3)]
    block=bytearray(grid(x,y,occ,h,v,wh,wv))
    for ix in range(2):
     for iy in range(2):
      for z in range(5):
       o=(ix*10+iy*5+z)*12+8
       p=struct.unpack_from('<f',block,o)[0]
       struct.pack_into('<f',block,o,lean.f32(p+(-.75+z*.25)))
    blocks.append(bytes(block))
  pointers=struct.pack('<16I',*[GRIDS+i*240 for i in range(16)])
  context=struct.pack('<I2f',TABLE,1,2)
  inputs=[]
  for mesh in raw:
   for turn in range(4):
    sin,cos=[(0,1),(1,0),(0,-1),(-1,0)][turn]
    for p in mesh['Positions']:
     inputs.append(dict(mesh=mesh['Name'],turn=turn,point=[lean.f32(2.5+p[0]*cos-p[1]*sin),lean.f32(3.5+p[0]*sin+p[1]*cos),p[2]]))
  inputs += [dict(mesh='interior-control',turn=0,point=p) for p in [[2.125,3.375,-1.375],[2.625,3.875,-2.125],[2.4999,3.5001,-1.001],[2.5001,3.4999,-1.999],[2.25,3.25,-.4],[2.25,3.25,-2.6]]]
  output=bytes([165])*(12*len(inputs))
  memory=[(CTX,context),(TABLE,pointers),(GRIDS,b''.join(blocks)),(OUTPUT,output),(0x7ffec000,bytes(32768))]
  calls=[dict(address='0x58ee60',registers={},stack_args=[])]
  calls += [dict(address='0x58f030',registers={'ECX':hex(CTX)},stack_args=[*map(fb,p['point']),OUTPUT+i*12]) for i,p in enumerate(inputs)]
  args=dict(address='0x4aa310',registers='{}',memory=json.dumps(dict(regions=[dict(address=hex(a),hex=b.hex()) for a,b in memory])),
      before_calls=calls,max_steps=20000,observe_reads=[],observe_writes=[])
  result=run(EXE,args,[(a,b) for a,b in memory if a in [CTX,TABLE,GRIDS]],0x4aa310,OUTPUT,len(output))
  assert result['success'] and not result['context_mismatches'] and not result['protected_write_or_execution_failures'],(topology,mode,result['emulation_error'],result['trace_tail'])
  for p,q in zip(inputs,struct.iter_unpack('<3f',bytes.fromhex(result['captured_hex']))):p['output']=list(q)
  rows.append(dict(topology=topology,mode=mode,points=inputs,source_sha256=result['source_exe_sha256'],
      before_calls_returned=all(c['returned'] for c in result['before_calls']),
      instructions=result['instruction_count'],code_substitutions=result['function_or_vtable_substitutions']))
  print(topology,mode,len(inputs),'native vertex interpolation samples',flush=True)
assert hashlib.sha256(EXE.read_bytes()).hexdigest()==fingerprint
(OUT/'vertex-interpolation.json').write_text(json.dumps(dict(scope='Original 58EE60 table init + 58F030 interpolation; prepared independent full-height lattices; retail ROCK WALL and Rock Corner coordinates in four rotations',cases=rows),indent=2),encoding='utf8')

