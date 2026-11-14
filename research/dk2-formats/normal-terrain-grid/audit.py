"""Verify saved normal-terrain fixtures; optional original-PE byte comparison."""
from pathlib import Path
import argparse,hashlib,json,struct,sys,importlib.util
ap=argparse.ArgumentParser()
ap.add_argument('--research-root',type=Path,required=True)
ap.add_argument('--exe',type=Path)
cli=ap.parse_args()
root=Path(__file__).resolve().parent
digest=lambda b:hashlib.sha256(b).hexdigest()
manifest=json.loads((root/'manifest.json').read_text(encoding='utf8'))
for member in manifest['files']:
 data=(root/member['file']).read_bytes()
 assert digest(data)==member['sha256'],member['file']
 assert len(data)==member['bytes']
sys.path.insert(0,str(cli.research_root))
from model import grid
report=json.loads((root/'cases.json').read_text(encoding='utf8'))
assert len(report['cases'])==30
names=set()
for row in report['cases']:
 name=row['name'];assert name not in names;names.add(name)
 mode=row['mode'];topology=row['topology']
 occupied=lambda x,y:dict(free=False,east=x>=3,north=y>=4,isolated=x==2 and y==3,
     corner=x>=3 and y>=4,checker=bool((x+y)&1))[topology]
 occupancy=[int(occupied(x,y)) for y in range(2,6) for x in range(1,5)]
 actual=bytes.fromhex(row['grid_hex'])
 expected=grid(2,3,occupancy,[128 if mode in ['H','HV'] else 0]*3,
     [128 if mode in ['V','HV'] else 0]*3,128 if mode=='W' else 0,128 if mode=='W' else 0)
 assert actual==expected and len(actual)==240,name
 assert actual==b''.join(struct.pack('<3f',*p) for p in row['points']),name
 assert row['raw_map_nav_unchanged'] and not row['protected_failures'] and row['substitutions']==0
 assert row['source_exe_sha256']==manifest['source_exe_sha256']
 visits=row['visited_instructions']
 for address in ['0x4f6570','0x56eb60','0x56ed20','0x4f66e0','0x5a3590','0x5a3220','0x5a37f0','0x59e750']:
  assert visits.get(address)==1,(name,address,visits.get(address))
 assert visits.get('0x575fa0',0)==0,name
if cli.exe:
 data=cli.exe.read_bytes();assert digest(data)==manifest['source_exe_sha256']
 pe=struct.unpack_from('<I',data,60)[0];optional=pe+24
 sections=struct.unpack_from('<H',data,pe+6)[0]
 base=struct.unpack_from('<I',data,optional+28)[0]
 table=optional+struct.unpack_from('<H',data,pe+20)[0]
 for fn in json.loads((root/'primary-bytes.json').read_text())['functions']:
  address=int(fn['address'],16);n=fn['length'];rva=address-base
  for i in range(sections):
   virtual,virtual_address,size,raw=struct.unpack_from('<4I',data,table+40*i+8)
   if virtual_address<=rva and rva+n<=virtual_address+size:
    offset=raw+rva-virtual_address;break
  else:raise AssertionError('Function outside file sections')
  assert data[offset:offset+n]==bytes.fromhex(fn['hex']),fn['address']
print('PASS: package hashes; 30 native grids / 600 points / 7200 exact bytes; preserved inputs; original call path'+('; two complete Ghidra function ranges match original PE' if cli.exe else ''))

