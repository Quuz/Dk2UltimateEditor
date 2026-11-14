"""Integrity/evidence inventory for the saved projection supplement."""
from pathlib import Path
import argparse,ast,hashlib,json,struct
ap=argparse.ArgumentParser()
ap.add_argument('--exe',type=Path)
cli=ap.parse_args()
root=Path(__file__).resolve().parent
sha=lambda b:hashlib.sha256(b).hexdigest()
manifest=json.loads((root/'manifest.json').read_text(encoding='utf8'))
for member in manifest['files']:
 data=(root/member['file']).read_bytes()
 assert sha(data)==member['sha256'] and len(data)==member['bytes'],member['file']
 if member['file'].endswith('.py'):ast.parse(data.decode('utf8'))
for file,count in [('full-height-cases.json',30),('mixed-height-cases.json',4)]:
 report=json.loads((root/file).read_text(encoding='utf8'))
 assert report['full_height'] and len(report['cases'])==count
 for row in report['cases']:
  data=bytes.fromhex(row['grid_hex'])
  assert len(data)==240 and len(row['points'])==20
  assert data==b''.join(struct.pack('<3f',*v) for v in row['points'])
  assert row['raw_map_nav_unchanged'] and row['substitutions']==0 and not row['protected_failures']
  assert row['source_exe_sha256']==manifest['source_exe_sha256']
  assert row['visited_instructions']['0x5a3590']==1
  assert row['visited_instructions']['0x575fa0']==0
interpolation=json.loads((root/'vertex-interpolation.json').read_text(encoding='utf8'))
assert len(interpolation['cases'])==6 and sum(len(c['points']) for c in interpolation['cases'])==972
for case in interpolation['cases']:
 assert case['before_calls_returned'] and case['code_substitutions']==0
 assert case['source_sha256']==manifest['source_exe_sha256']
 assert {p['turn'] for p in case['points']}=={0,1,2,3}
if cli.exe:
 data=cli.exe.read_bytes();assert sha(data)==manifest['source_exe_sha256']
 pe=struct.unpack_from('<I',data,60)[0];optional=pe+24
 sections=struct.unpack_from('<H',data,pe+6)[0];base=struct.unpack_from('<I',data,optional+28)[0]
 table=optional+struct.unpack_from('<H',data,pe+20)[0]
 for fn in json.loads((root/'primary-bytes.json').read_text())['functions']:
  address=int(fn['address'],16);n=fn['length'];rva=address-base
  for i in range(sections):
   virtual,va,size,raw=struct.unpack_from('<4I',data,table+40*i+8)
   if va<=rva and rva+n<=va+size:offset=raw+rva-va;break
  else:raise AssertionError('Unmapped function')
  assert data[offset:offset+n]==bytes.fromhex(fn['hex'])
print('PASS: package hashes/syntax; 34 grid fixtures / 680 points; 972 interpolation results; invariants'+('; two complete primary ranges match PE' if cli.exe else '')+'; no CPU rerun claimed')

