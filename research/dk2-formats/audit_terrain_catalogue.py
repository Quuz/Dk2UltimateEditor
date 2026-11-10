"""Attest native Terrain compositions, checksum collision and lossless patches."""
from pathlib import Path
from collections import Counter
import json,struct
import dk2_formats as fmt
from build_terrain_catalogue_validation import ROOT,load,digest,primary,corpus
from emulate_terrain_catalogue import cases,build_case,records,IDS
from native_terrain_catalogue import intermediate,patch_unknown,lookup_address,u32

def main():
 s=load('terrain-catalogue-validation.json');assert s['primary_program_bytes']==primary() and s['corpus']==corpus();assert len(cases())==len(s['native_emulations'])==32;counts=Counter();buffers={}
 for c,row in zip(cases(),s['native_emulations']):
  p=ROOT/'ghidra'/row['file'];v=load('ghidra/'+row['file']);args,want=build_case(c);r=v['result']
  assert 'terrain-v2-' in row['file'] and digest(p.read_bytes())==row['sha256'] and c==v['case'] and args==v['input'] and want==v['expected_registers'] and r['success'] and r['hit_return'];assert all(int(r['registers'][k],16)==n for k,n in want.items());counts[c['kind']]+=1
  m={int(q['address'],16):bytes.fromhex(q['hex']) for q in json.loads(args['memory'])['regions']};buffers[c['name']]=m
  # No patched instructions in any native function under investigation.
  for a in [0x56F090,0x56EB60,0x56ED20,0x51EB30,0x51EB50,0x4F66E0,0x4F6570,0x504D60,0x508DD0,0x6361A0]:assert a not in m
  assert len(m[0x10100000])==len(m[0x10200000])==256*460
  if c['kind']=='pipeline':
   rr=records(c);world=m[0x10020000];assert u32(world,0x15AC)==9+(0 if c['update'] else len(rr))
   if c['update']:assert u32(world,0x15B0)==0x1122 and world[0x9BAE:0x9BB0]==bytes([201,202])
   for ident in set(c['ids']):
    j=max(i for i,k in enumerate(c['ids']) if k==ident);record=rr[j];off=460*ident
    assert m[0x10200000][off+0x183:off+0x185]==record[0x198:0x19A]
    assert m[0x10200000][off+0x1B5:off+0x1B9]==(bytes(4) if c['stride']==548 else record[0x224:0x228])
   if c['name'].startswith('stale-water'):
    assert u32(m[0x10200000],460*7+0x17B)==0 and world[0x9BAE:0x9BB0]==bytes([7,3])
 a,b=(buffers['checksum-collision-'+k] for k in ['a','b']);assert a[0x10320000]!=b[0x10320000] and sum(a[0x10320000])==sum(b[0x10320000]);assert a[0x10200000]!=b[0x10200000] and a[0x10020000]==b[0x10020000]
 patches=roundtrips=records_count=0
 for row in s['corpus']['files']:
  original=Path(row['file']).read_bytes();assert digest(original)==row['sha256'];parts=fmt.parse(original);assert b''.join(q.pack() for q in parts)==original;roundtrips+=1
  for si,sec in enumerate(parts):
   if sec.signature!=110:continue
   rel=len(sec.header)
   assert fmt.u32(sec.header,20)==len(sec.records)
   for ri,raw in enumerate(sec.records):
    records_count+=1
    for off,n in [(0x198,2),(0x224,4)]:
     value=int.from_bytes(raw[off:off+n],'little')^((1<<(8*n))-1);changed=patch_unknown(raw,off,value)
     rs=list(sec.records);rs[ri]=changed;new=fmt.Section(sec.offset,sec.signature,sec.header,list(sec.records),sec.suffix,sec.stride);new.replace_records(rs)
     packed=b''.join(new.pack() if j==si else q.pack() for j,q in enumerate(parts));manual=bytearray(original);at=sec.offset+rel+off;manual[at:at+n]=changed[off:off+n]
     assert packed==manual and b''.join(q.pack() for q in fmt.parse(packed))==packed;patches+=1
    rel+=len(raw)
 api=0
 for off,n in [(0x198,2),(0x224,4)]:
  for value in [0,1,(1<<(8*n-1))-1,1<<(8*n-1),(1<<(8*n))-1]:
   raw=bytes([165])*552;out=patch_unknown(raw,off,value);assert out[:off]==raw[:off] and out[off+n:]==raw[off+n:] and int.from_bytes(out[off:off+n],'little')==value;api+=1
  for bad in [-1,1<<(8*n),True,1.0,'1']:
   try:patch_unknown(bytes(552),off,bad)
   except ValueError:api+=1
   else:raise AssertionError(bad)
 for ident in IDS:assert lookup_address(0x10100000,ident)==0x10100000+460*(ident&255);api+=1
 for bad in [-1,0x100000000,True,'1']:
  try:lookup_address(0x10100000,bad)
  except ValueError:api+=1
  else:raise AssertionError(bad)
 for n in [0,551,553]:
  for fn in [intermediate,lambda raw:patch_unknown(raw,0x198,0)]:
   try:fn(bytes(n))
   except ValueError:api+=1
   else:raise AssertionError((n,fn))
 for file,sha in load('executable-hash-verification.json').items():assert digest((ROOT.parent.parent/file).read_bytes())==sha
 old=load('creature-version-layout-semantics.json')['old_source'];assert digest(Path(old['original_path']).read_bytes())==old['sha256'];rc3=load('camera-catalogue-semantics.json')['rc3'];assert digest(Path(rc3['original_file']).read_bytes())==digest(Path(rc3['research_copy']).read_bytes())==rc3['sha256']
 assert load('terrain-lighting-semantics.json')['phase58_complete_loading']==s
 pack=load('editor-format-package.json');assert len(pack['components'])==67 and pack['terrain_lighting_semantics']['phase58_complete_loading']==s
 for key,row in pack['components'].items():
  raw=(ROOT/row['file']).read_bytes();assert digest(raw)==row['sha256'] and json.loads(raw)==pack[key]
 assert roundtrips==82 and records_count==3306 and patches==6612 and counts==dict(pipeline=24,partial=8)
 v=dict(phase=58,date='2026-10-27',native_emulations=32,native_case_kinds=dict(counts),native_getter_calls=32*len(IDS),complete_primary_pe_functions=11,whole_file_roundtrips=roundtrips,terrain_records=records_count,exact_in_memory_patches=patches,api_boundary_checks=api,checksum_collision_pair_verified=True,source_executables_unchanged=4,research_writes_to_game_data=0,modified_gameplay_runs=0,package_components=67,unknown_gameplay_purposes=['Terrain198','Terrain224','Traps204','Objects34C'],limitations=s['limitations'])
 (ROOT/'phase58-validation.json').write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps({k:n for k,n in v.items() if k!='limitations'}))
if __name__=='__main__':main()
