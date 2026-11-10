"""Audit native loading inputs/results, exact raw patches and package evidence."""
from pathlib import Path
from collections import Counter
import json,struct
import dk2_formats as fmt
from build_trap_loading import ROOT,load,digest,primary,equivalence,rejected_identity
from native_trap_loading import *
from emulate_trap_loading import cases,build_case,TOKEN

def main():
 s=load('trap-loading-semantics.json');functions,blocks=primary();assert s['primary_program_bytes']==functions and s['primary_data_ranges']==blocks and s['whole_function_equivalence']==equivalence() and s['rejected_candidate']==rejected_identity()
 counts=Counter();buffers={};sound_calls=0
 for c,row in zip(cases(),s['native_emulations']):
  path=ROOT/'ghidra'/row['file'];saved=load('ghidra/'+row['file']);args,want=build_case(c);r=saved['result'];assert row['sha256']==digest(path.read_bytes()) and saved['case']==c and saved['input']==args and saved['expected_registers']==want and r['success'] and r['hit_return'];assert all(int(r['registers'][k],16)==v for k,v in want.items());counts[c['kind']]+=1
  regions=json.loads(args['memory'])['regions'];m={int(x['address'],16):bytes.fromhex(x['hex']) for x in regions};buffers[c['name']]=m;sound_calls+=int.from_bytes(m[0x10006400][:4],'little')
  # No original code, including partial function spans, may be patched.
  for fn in functions:
   if fn['program']!=args['program']:continue
   lo=fn['address'];hi=lo+fn['length'];assert all(a+len(b)<=lo or a>=hi for a,b in m.items())
  mid=m[0x10003000];previous=m[0x10004000];expected=m[0x10005000]
  if c['kind']=='init':assert mid==bytes(747) and previous==expected
  elif c['kind']=='converter':assert previous==expected and all(struct.unpack_from('<I',mid,i)[0]==1 for i in range(0,0xA8,4))
  else:
   p=profile(c['sha']);value=struct.unpack_from('<H',mid,0x2AC)[0];pres=struct.unpack_from('<I',mid,0x5C)[0];assert expected[0x98:0x9A]==(value.to_bytes(2,'little') if pres else previous[0x98:0x9A]);assert expected[0xC1:0xCD]==mid[0x2D9:0x2E5]
   checksum=sum(mid) if p['checksum_gate'] is None or not c['gate'] else struct.unpack_from('<I',previous,0x6C)[0];assert struct.unpack_from('<I',expected,0x6C)[0]==checksum
   requested=sound_requested(mid,c['sha'],c['gate']);assert bool(int.from_bytes(m[0x10006400][:4],'little'))==requested and struct.unpack_from('<I',expected,0xB0)[0]==(TOKEN if requested else struct.unpack_from('<I',previous,0xB0)[0])
   if c['kind']=='initialized_chain':assert struct.unpack_from('<I',mid,0x294)[0]==0x11223344
 for c in cases():
  if c['sha']!=DX:continue
  key=c['name'].replace('d7a','93e');a=buffers[c['name']];b=buffers[key];assert a[0x10003000]==b[0x10003000]
  # These are the only observed profile differences in the null-provider cases.
  x=a[0x10005000];y=b[0x10005000];assert x[:0x6C]==y[:0x6C] and x[0x70:0xB0]==y[0x70:0xB0] and x[0xB4:]==y[0xB4:]
 for sha in PROFILES:
  a=buffers[f'chain-{sha[:3]}-255'][0x10005000];b=buffers[f'chain-{sha[:3]}-65280'][0x10005000];assert a!=b and a[0x6C:0x70]==b[0x6C:0x70] and a[0x98:0x9A]==b'\xFF\0' and b[0x98:0x9A]==b'\0\xFF'
 assert len(s['native_emulations'])==len(cases())==60 and counts==dict(init=6,converter=12,loader=24,chain=16,initialized_chain=2) and sound_calls==30
 # The independent converter table covers every converted byte exactly once,
 # except flags294 explicitly OR into the previous value.
 coverage=Counter()
 for _,_,dst,n in CONVERT+RESOURCES:
  coverage.update(range(dst,dst+n))
 coverage.update(range(0,0xA8));coverage.update(range(0xA8,0xC8));coverage.update(range(0x2B7,0x2D7));coverage.update(range(0x294,0x298));assert set(coverage)==set(range(747)) and set(coverage.values())=={1}
 patches=records=files=0
 for row in s['corpus']['files']:
  source=Path(row['file']).read_bytes();assert digest(source)==row['sha256'];parts=fmt.parse(source);assert b''.join(q.pack() for q in parts)==source;files+=1
  for si,sec in enumerate(parts):
   if sec.signature!=130:continue
   relative=len(sec.header)
   for ri,raw in enumerate(sec.records):
    value=fmt.u16(raw,0x204)^0xFFFF;changed=patch_unknown(raw,value);assert changed[:0x204]==raw[:0x204] and changed[0x206:]==raw[0x206:];records+=1
    rr=list(sec.records);rr[ri]=changed;new=fmt.Section(sec.offset,sec.signature,sec.header,list(sec.records),sec.suffix,sec.stride);new.replace_records(rr);packed=b''.join(new.pack() if j==si else q.pack() for j,q in enumerate(parts));manual=bytearray(source);at=sec.offset+relative+0x204;manual[at:at+2]=value.to_bytes(2,'little');assert packed==manual and b''.join(q.pack() for q in fmt.parse(packed))==packed;patches+=1;relative+=len(raw)
 api=0
 for v in [0,1,32767,32768,65526,65535]:assert fmt.u16(patch_unknown(bytes(579),v),0x204)==v;api+=1
 bad=[lambda n=n:patch_unknown(bytes(579),n) for n in [-1,65536,True,1.0,'1']]
 bad += [lambda n=n:intermediate(bytes(n),bytes(747)) for n in [578,580]]
 bad += [lambda n=n:intermediate(bytes(579),bytes(n)) for n in [746,748]]
 bad += [lambda n=n:definition(bytes(747),bytes(n),20,DX) for n in [208,210]]
 bad += [lambda n=n:definition(bytes(747),bytes(209),n,DX) for n in [-1,0x100000000,True,20.0]]
 bad += [lambda n=n:definition(bytes(747),bytes(209),20,n) for n in ['',None,'1.3']]
 bad += [lambda n=n:sound_requested(bytes(747),DX,n) for n in [-1,0x100000000,False]]
 for fn in bad:
  try:fn()
  except ValueError:api+=1
  else:raise AssertionError('Invalid API input accepted')
 rc3=load('camera-catalogue-semantics.json')['rc3'];assert digest(Path(rc3['original_file']).read_bytes())==digest(Path(rc3['research_copy']).read_bytes())==rc3['sha256']
 old=load('creature-version-layout-semantics.json')['old_source'];assert digest(Path(old['original_path']).read_bytes())==digest(Path(old['path']).read_bytes())==old['sha256']
 for file,sha in load('executable-hash-verification.json').items():assert digest((ROOT.parent.parent/file).read_bytes())==sha
 pack=load('editor-format-package.json');assert len(pack['components'])==67 and pack['trap_semantics']['phase61_full_loading']==s
 for key,row in pack['components'].items():
  raw=(ROOT/row['file']).read_bytes();assert digest(raw)==row['sha256'] and json.loads(raw)==pack[key]
 assert files==82 and records==patches==1293
 v=dict(phase=61,date='2026-10-27',native_emulations=60,native_cases=dict(counts),captured_sound_lookups=sound_calls,complete_primary_pe_functions=11,primary_data_ranges=5,complete_executed_native_functions=8,source_files_roundtripped=files,trap_records=records,exact_in_memory_patches=patches,api_boundary_checks=api,source_executables_unchanged=4,package_components=67,research_writes_to_game_data=0,modified_gameplay_runs=0,unknown204_gameplay_still_open=True,limitations=s['limitations'])
 (ROOT/'phase61-validation.json').write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps({k:n for k,n in v.items() if k!='limitations'}))
if __name__=='__main__':main()
