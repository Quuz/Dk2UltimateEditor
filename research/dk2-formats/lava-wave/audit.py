from pathlib import Path
import argparse,ast,hashlib,json,math,struct
ap=argparse.ArgumentParser();ap.add_argument('--exe',type=Path);cli=ap.parse_args()
root=Path(__file__).resolve().parent
sha=lambda b:hashlib.sha256(b).hexdigest()
f32=lambda v:struct.unpack('<f',struct.pack('<f',v))[0]
manifest=json.loads((root/'manifest.json').read_text(encoding='utf8'))
for item in manifest['files']:
 data=(root/item['file']).read_bytes()
 assert sha(data)==item['sha256'] and len(data)==item['bytes']
 if item['file'].endswith('.py'):ast.parse(data.decode('utf8'))
report=json.loads((root/'cases.json').read_text(encoding='utf8'));assert len(report['cases'])==16
count=0
for row in report['cases']:
 assert row['grid_preserved'] and row['gfx_preserved'] and row['substitutions']==0
 for iy in range(3):
  sy=f32(math.sin(((iy*32-row['y']*64+row['phase'])&127)*f32(math.pi)/64)*.04)
  for ix in range(3):
   expected=f32(math.sin(((ix*32-row['x']*64+row['phase'])&127)*f32(math.pi)/64)*.04+sy-f32(.85))
   assert expected==row['heights'][iy*3+ix];count+=1
assert count==144
if cli.exe:
 data=cli.exe.read_bytes();assert sha(data)==report['source_exe_sha256']
 pe=struct.unpack_from('<I',data,60)[0];optional=pe+24;base=struct.unpack_from('<I',data,optional+28)[0]
 table=optional+struct.unpack_from('<H',data,pe+20)[0]
 fn=json.loads((root/'primary-bytes.json').read_text());rva=int(fn['address'],16)-base;n=fn['length']
 for i in range(struct.unpack_from('<H',data,pe+6)[0]):
  virtual,va,size,raw=struct.unpack_from('<4I',data,table+40*i+8)
  if va<=rva and rva+n<=va+size:offset=raw+rva-va;break
 else:raise AssertionError('Unmapped native function')
 assert data[offset:offset+n]==bytes.fromhex(fn['hex'])
print('PASS: package hashes/syntax; 16 native grids / 144 independently matched heights; input invariants'+('; complete primary range matched PE' if cli.exe else '')+'; no CPU rerun claimed')
