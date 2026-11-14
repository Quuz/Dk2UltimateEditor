"""Original normal-terrain 5A3590 to its 240-byte deformation grid.
Prepared raw Terrain -> original loader, World/CMap and native Lean/Wibble.
Stops before 575FA0 allocation/upload; does not render pixels or modify inputs.
"""
from pathlib import Path
import sys,json,struct,hashlib,argparse
ap=argparse.ArgumentParser()
ap.add_argument('--research-root',type=Path,required=True)
ap.add_argument('--output',type=Path,required=True)
cli=ap.parse_args()
ROOT=cli.research_root.resolve()
sys.path.insert(1,str(Path(__file__).resolve().parents[1]/'normal-terrain-grid'))
sys.path.insert(0,str(ROOT))
import emulate_terrain_lean as setup
import emulate_cursor_procedure as cursor
from terrain_native_trace import run
OUT=cli.output.resolve()
OUT.mkdir(parents=True,exist_ok=True)
STOP=0x5a37d1
GRID=0x7ffefaf0
fingerprint=hashlib.sha256(cursor.EXE.read_bytes()).hexdigest()
assert fingerprint=='d7a8b854e4e4f02709eba271bead6fe2307dd6e332b5a96e70857e5b0e19fb8a'
rows=[]
for topology in ['isolated','checker']:
 for mode in ['HV','W']:
  case=dict(name=topology+'-'+mode,topology=topology,nav_block=8,
      lean_h=[128]*3 if mode in ['H','HV'] else [0]*3,
      lean_v=[128]*3 if mode in ['V','HV'] else [0]*3)
  args,checks,source,parent=setup.build(case)
  memory=json.loads(args['memory'])['regions']
  raw=next(r for r in memory if int(r['address'],16)==cursor.TERRAIN_RAW)
  data=bytearray.fromhex(raw['hex'])
  if mode=='W':data[0x1ce]=data[0x1d2]=128
  raw['hex']=data.hex()
  next(r for r in memory if int(r['address'],16)==0x759ae8)['hex']='01000000'
  checks[cursor.TERRAIN_RAW]=bytes(data)
  RAW_B,MID_B,DEF_B=0x18400000,0x18401000,0x18402000
  second=bytearray(data);second[0x1d6]=9
  second[0x1ce:0x1d6]=bytes(v//2 for v in data[0x1ce:0x1d6])
  memory += [dict(address=hex(a),hex=b.hex()) for a,b in [(RAW_B,second),(MID_B,bytes(736)),(DEF_B,bytes(460))]]
  definitions=next(r for r in memory if int(r['address'],16)==0x6da4a8)
  pointers=bytearray.fromhex(definitions['hex']);struct.pack_into('<I',pointers,9*4,DEF_B);definitions['hex']=pointers.hex()
  maprow=next(r for r in memory if int(r['address'],16)==cursor.MAP)
  tiles=bytearray.fromhex(maprow['hex'])
  for yy in range(32):
   for xx in range(32):tiles[(yy*32+xx)*16+10]=8 if (xx+yy)%2==0 else 9
  maprow['hex']=tiles.hex();checks[cursor.MAP]=bytes(tiles);checks[RAW_B]=bytes(second)

  # Run the existing prepared loader calls; omit the cursor render.
  args['before_calls']=[c for c in args['before_calls'] if c['address'] in ['0x4f6570','0x56eb60','0x56ed20','0x4f66e0']]
  args['before_calls'] += [dict(address='0x4f6570',registers={'ECX':hex(DEF_B)},stack_args=[]),
    dict(address='0x56eb60',registers={'ECX':hex(MID_B)},stack_args=[]),
    dict(address='0x56ed20',registers={'ECX':hex(MID_B)},stack_args=[RAW_B]),
    dict(address='0x4f66e0',registers={'ECX':hex(DEF_B)},stack_args=[MID_B,0])]
  args['before_calls'].append(dict(address='0x5a3590',registers={'ECX':hex(cursor.base.GFX)},stack_args=[2,3]))
  args['memory']=json.dumps(dict(regions=memory))
  args['address']=hex(STOP)
  args['registers']='{}'
  saved=OUT/(case['name']+'.json')
  signature=hashlib.sha256((json.dumps(args,sort_keys=True)+'raw-map-nav-check-v2').encode()).hexdigest()
  prior=json.loads(saved.read_text(encoding='utf8')) if saved.exists() else {}
  if prior.get('input_signature')==signature:result=prior
  else:
   result=run(cursor.EXE,args,[(a,checks[a]) for a in [cursor.TERRAIN_RAW,cursor.MAP,cursor.NAV,RAW_B]],STOP,GRID,240)
   result['input_signature']=signature
   if result['success']:saved.write_text(json.dumps(result,indent=2),encoding='utf8')
  assert result['success'] and all(c['returned'] for c in result['before_calls']), (case['name'],result.get('emulation_error'),result['trace_tail'])
  assert not result['context_mismatches'] and not result['protected_write_or_execution_failures']
  points=list(struct.iter_unpack('<3f',bytes.fromhex(result['captured_hex'])))
  if mode in ['zero','HV']:baseline=points
  changed=[i for i,(p,q) in enumerate(zip(points,baseline)) if p!=q]
  rows.append(dict(name=case['name'],mode=mode,topology=topology,points=points,grid_hex=result['captured_hex'],changed_indices=changed,
      visited_instructions=result['visited_instructions'],source_exe_sha256=result['source_exe_sha256'],
      instruction_count=result['instruction_count'],protected_failures=result['protected_write_or_execution_failures'],
      substitutions=result['function_or_vtable_substitutions'],input_signature=signature,raw_map_nav_unchanged=True))
  if mode in ['H','V','HV']:
   assert result['visited_instructions'].get('0x5a37f0')==1
   assert result['visited_instructions'].get('0x59e750')==1
  print(case['name'],len(changed),'changed',points[:3],flush=True)
assert hashlib.sha256(cursor.EXE.read_bytes()).hexdigest()==fingerprint
report=dict(mixed_shapes=True,full_height=True,scope=__doc__,stop_before_allocation=hex(STOP),grid_address=hex(GRID),cases=rows,
    script_sha256=hashlib.sha256(Path(__file__).read_bytes()).hexdigest())
(OUT/'normal-terrain-cases.json').write_text(json.dumps(report,indent=2),encoding='utf8')
print('Saved',len(rows),'normal-terrain fixtures')

