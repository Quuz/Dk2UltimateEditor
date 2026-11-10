"""Attest original-editor behavior without promoting it to game authority."""
from pathlib import Path
from collections import Counter
import json,struct
import dk2_formats as fmt
from build_trap_writer import ROOT,load,digest,primary,comparisons,dialogs
from emulate_trap_writer import cases,build_case,FLAGS
from native_trap_writer import PROFILES,record,patch_unknown

def main():
 s=load('trap-writer-semantics.json');p=primary();assert s['primary_program_bytes']==p['functions'] and s['primary_data_ranges']==p['blocks'] and s['full_body_comparisons']==comparisons() and s['dialog_resources']==dialogs();assert len(cases())==len(s['native_emulations'])==38
 counts=Counter();buffers={};written=0;malformed=0
 for c,row in zip(cases(),s['native_emulations']):
  path=ROOT/'ghidra'/row['file'];v=load('ghidra/'+row['file']);args,want=build_case(c);r=v['result'];assert 'writer-v4-' in row['file'] and digest(path.read_bytes())==row['sha256'] and c==v['case'] and args==v['input'] and want==v['expected_registers'] and r['success'] and r['hit_return'];assert all(int(r['registers'][k],16)==n for k,n in want.items());counts['chain' if c.get('commit') else 'writer']+=1
  m={int(q['address'],16):bytes.fromhex(q['hex']) for q in json.loads(args['memory'])['regions']};buffers[c['name']]=m;f=PROFILES[c['profile']];out=m[0x10014000];rr=[out[i:i+579] for i in range(56,len(out),579)];written+=len(rr)
  assert fmt.u32(out,20)==len(c['slots']) and fmt.u32(out,52)==579*len(c['slots']) and fmt.u32(out,8)==56+579*len(c['slots'])
  if 0 in c['slots']:
   try:fmt.parse(out)
   except ValueError:malformed+=1
   else:raise AssertionError('Null-slot header mismatch must reject')
  else:
   parsed=fmt.parse(out);assert b''.join(sec.pack() for sec in parsed)==out and len(parsed[0].records)==len(rr)
  for raw,n in zip(rr,[n for n in c['slots'] if n]):
   model=m[0x10016000 if n==1 else 0x10017000];assert raw==record(model,bytes([64+n])*5,bytes([96+n])*7,flag_fields=FLAGS);assert fmt.u16(raw,0x204)==(c['value']^(0xFFFF if n==2 else 0));assert not(fmt.u32(raw,0x1EC)&~0x3FFBF)
   if c.get('commit'):assert fmt.u16(raw,0x1FA)==12345 and fmt.u32(raw,0x1D4)==(0x12345678 if c['require'] else 0)
  assert f['writer'] not in m
  if c.get('commit'):
   for a in [f['ddx'],f['commit'],0x425EB0 if c['profile']=='Pro' else 0x430320]:assert a not in m
   assert fmt.u32(m[0x1001C000],0x78)==12345 and m[0x10002000][0x238:0x23A]==m[0x10016000][0x238:0x23A]
 for c in cases():
  if c['profile']!='Pro':continue
  a=buffers[c['name']][0x10014000];b=buffers[c['name'].replace('Pro-','RC3-',1)][0x10014000]
  assert a[:46]==b[:46] and a[48:]==b[48:]
 assert counts==dict(writer=26,chain=12) and malformed==4 and written==44
 # Conservatively patch original records rather than native model projections.
 patches=roundtrips=records=0
 for row in s['corpus']['files']:
  b=Path(row['file']).read_bytes();assert digest(b)==row['sha256'];parts=fmt.parse(b);assert b''.join(q.pack() for q in parts)==b;roundtrips+=1
  for si,sec in enumerate(parts):
   if sec.signature!=130:continue
   rel=len(sec.header)
   for ri,raw in enumerate(sec.records):
    records+=1
    for off,n,mask in [(0x204,2,0xFFFF),(0x1FA,2,0xFFFF),(0x1EC,4,0x80000000)]:
     value=int.from_bytes(raw[off:off+n],'little')^mask;changed=patch_unknown(raw,value) if off==0x204 else raw[:off]+value.to_bytes(n,'little')+raw[off+n:]
     rr=list(sec.records);rr[ri]=changed;new=fmt.Section(sec.offset,sec.signature,sec.header,list(sec.records),sec.suffix,sec.stride);new.replace_records(rr);packed=b''.join(new.pack() if j==si else q.pack() for j,q in enumerate(parts));manual=bytearray(b);at=sec.offset+rel+off;manual[at:at+n]=changed[off:off+n];assert packed==manual and b''.join(q.pack() for q in fmt.parse(packed))==packed;patches+=1
    rel+=len(raw)
 api=0
 for n in [0,1,32767,32768,65526,65535]:
  raw=bytes([165])*579;out=patch_unknown(raw,n);assert out[:0x204]==raw[:0x204] and out[0x206:]==raw[0x206:] and fmt.u16(out,0x204)==n;api+=1
 for n in [-1,65536,True,1.0,'1']:
  try:patch_unknown(bytes(579),n)
  except ValueError:api+=1
  else:raise AssertionError(n)
 for n in [578,580]:
  try:patch_unknown(bytes(n),0)
  except ValueError:api+=1
  else:raise AssertionError(n)
 for n in [31,32]:
  from native_trap_writer import padded_string
  assert len(padded_string(b'A'*n))==32;api+=1
 for v in [b'A'*33,'A']:
  try:padded_string(v)
  except ValueError:api+=1
  else:raise AssertionError(v)
 inherited=s['manufacture_game_evidence'];assert digest((ROOT/inherited['file']).read_bytes())==inherited['sha256']
 rc3=load('camera-catalogue-semantics.json')['rc3'];assert digest(Path(rc3['original_file']).read_bytes())==digest(Path(rc3['research_copy']).read_bytes())==rc3['sha256']
 old=load('creature-version-layout-semantics.json')['old_source'];assert digest(Path(old['original_path']).read_bytes())==old['sha256']
 for file,sha in load('executable-hash-verification.json').items():assert digest((ROOT.parent.parent/file).read_bytes())==sha
 pack=load('editor-format-package.json');assert len(pack['components'])==67 and pack['trap_semantics']['phase60_editor_writer']==s
 for key,row in pack['components'].items():
  b=(ROOT/row['file']).read_bytes();assert digest(b)==row['sha256'] and json.loads(b)==pack[key]
 assert roundtrips==82 and records==1293 and patches==3879
 v=dict(phase=60,date='2026-10-27',native_emulations=38,native_cases=dict(counts),complete_native_output_records=written,null_slot_malformed_output_cases=malformed,complete_primary_pe_functions=22,primary_data_ranges=2,source_files_roundtripped=roundtrips,trap_records=records,exact_in_memory_patches=patches,api_boundary_checks=api,source_executables_unchanged=4,package_components=67,research_writes_to_game_data=0,modified_gameplay_runs=0,editor_behavior_is_not_gameplay_authority=True,unknown204_gameplay_still_open=True,limitations=s['limitations'])
 (ROOT/'phase60-validation.json').write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps({k:n for k,n in v.items() if k!='limitations'}))
if __name__=='__main__':main()
