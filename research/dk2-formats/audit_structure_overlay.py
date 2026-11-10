"""Verify native overlay evidence, typed rejection and exact catalogue patches."""
from pathlib import Path
from collections import Counter
import json,struct
import dk2_formats as fmt
from build_structure_overlay import ROOT,load,digest,primary,corpus
from emulate_structure_overlay import cases,build_case
from native_structure_overlay import FAMILIES,height_offset,project,patch_height

def main():
 s=load('structure-overlay-semantics.json');ps=primary();assert s['primary_program_bytes']==ps['functions'] and s['primary_data_ranges']==ps['blocks'] and s['corpus']==corpus();assert len(cases())==len(s['native_emulations'])==49
 counts=Counter();buffers={};submissions=0
 for c,row in zip(cases(),s['native_emulations']):
  p=ROOT/'ghidra'/row['file'];v=load('ghidra/'+row['file']);args,want=build_case(c);r=v['result'];assert 'overlay-v2-' in row['file'] and digest(p.read_bytes())==row['sha256'] and c==v['case'] and args==v['input'] and want==v['expected_registers'] and r['success'] and r['hit_return'];assert all(int(r['registers'][k],16)==n for k,n in want.items());counts[c['mode']]+=1
  m={int(q['address'],16):bytes.fromhex(q['hex']) for q in json.loads(args['memory'])['regions']};buffers[c['name']]=m;f=FAMILIES[c['family']]
  for a in [f['converter'],0x6361A0,0x5953D0,0x5955C0,0x5956E0,0x5958F0,0x4B5C10,0x40CFD0]:assert a not in m
  assert m[f['stop']]==b'\xC3' and len(m[f['loader']])==5
  got=struct.unpack_from('<H',m[0x10023000],f['definition_height'])[0];assert got==((c['height']&65535) if c['presence'] else 0xA5A5)
  cap=m[0x10025000];n=fmt.u32(cap,0);submissions+=n
  if c['mode']=='caller' and c['ready'] and c['trapid']!=16:
   assert n==fmt.u32(cap,4)==2
   for i in range(2):
    p=8+64*i;assert cap[p+8:p+20]==m[0x10022000][0x31:0x3D] and cap[p+20:p+32]==m[0x10022000][0x25:0x31]
    assert cap[p+32:p+59]==bytes([180 if i==0 else 199])*27
  else:assert n==fmt.u32(cap,4)==0
  if not c['ready']:assert m[0x10001000]==m[0x10022000]
 # These distinct raw DWORDs must yield identical native consumer outcomes.
 for family in FAMILIES:
  for state in ([0,1] if family=='Doors' else [0]):
   for values in [[0,65536],[65535,0x1234FFFF,0xFFFFFFFF]]:
    mm=[buffers[f'caller-{family}-{state}-{v}'] for v in values]
    for a in [0x10022000,0x10023000,0x10025000]:assert all(m[a]==mm[0][a] for m in mm)
 assert counts==dict(helper=22,caller=27) and submissions==48
 # Native labels and exact type bindings are independently read from PE data.
 data={r['label']:bytes(load('ghidra/'+r['memory'])['data']) for r in s['primary_data_ranges']}
 assert b'.?AVCTrap@@\0' in data['trap-type'] and b'.?AVCDoor@@\0' in data['door-type'] and data['DoorOpen-label'].split(b'\0')[0].strip()==b'DoorOpen %d\n'.strip()
 assert struct.unpack_from('<I',data['world-vtable'],0x138)[0]==0x50D650 and struct.unpack_from('<I',data['world-vtable'],0x160)[0]==0x50D8C0
 search=load(s['search']['file']);assert digest((ROOT/s['search']['file']).read_bytes())==s['search']['sha256'];unknown=[r for r in search['candidates'] if r['field'].startswith('unknown')];assert len(unknown)==1 and unknown[0]['address']=='0x5955e5'
 patches=roundtrips=0;records=Counter()
 for row in s['corpus']['files']:
  b=Path(row['file']).read_bytes();assert digest(b)==row['sha256'];parts=fmt.parse(b);assert b''.join(q.pack() for q in parts)==b;roundtrips+=1
  for si,sec in enumerate(parts):
   if sec.signature not in [130,140]:continue
   family={130:'Traps',140:'Doors'}[sec.signature];f=FAMILIES[family];off=f['raw_height'];rel=len(sec.header)
   for ri,raw in enumerate(sec.records):
    records[family]+=1
    for mask in [0x80000000,0x8000]:
     value=fmt.u32(raw,off)^mask;out=patch_height(raw,family,value);assert (value&65535)==(fmt.u32(raw,off)&65535) if mask==0x80000000 else (value&65535)!=(fmt.u32(raw,off)&65535)
     rs=list(sec.records);rs[ri]=out;new=fmt.Section(sec.offset,sec.signature,sec.header,list(sec.records),sec.suffix,sec.stride);new.replace_records(rs);packed=b''.join(new.pack() if j==si else q.pack() for j,q in enumerate(parts));manual=bytearray(b);at=sec.offset+rel+off;manual[at:at+4]=out[off:off+4];assert packed==manual and b''.join(q.pack() for q in fmt.parse(packed))==packed;patches+=1
    rel+=len(raw)
 api=0
 for family,f in FAMILIES.items():
  for value in [0,32767,32768,65535,65536,0x80000000,0xFFFFFFFF]:
   raw=bytes([165])*f['raw_bytes'];out=patch_height(raw,family,value);off=f['raw_height'];assert out[:off]==raw[:off] and out[off+4:]==raw[off+4:] and fmt.u32(out,off)==value;api+=1
  for bad in [-1,0x100000000,True,1.0,'1']:
   try:patch_height(bytes(f['raw_bytes']),family,bad)
   except ValueError:api+=1
   else:raise AssertionError(bad)
 for kind in [3,4]:
  for h in [0,32768,65535]:
   for state in [0,1,2,0xFFFFFFFF]:assert height_offset(kind,h,state)==h+(512 if kind==3 else 4248 if state else 2048);api+=1
 for kind,h,state in [(2,0,0),(3,-1,0),(3,65536,0),(4,0,-1),(True,0,0),(4,0,True)]:
  try:height_offset(kind,h,state)
  except ValueError:api+=1
  else:raise AssertionError((kind,h,state))
 for n in [299,301]:
  try:project(bytes(n),3,0)
  except ValueError:api+=1
  else:raise AssertionError(n)
 for file,sha in load('executable-hash-verification.json').items():assert digest((ROOT.parent.parent/file).read_bytes())==sha
 old=load('creature-version-layout-semantics.json')['old_source'];assert digest(Path(old['original_path']).read_bytes())==old['sha256'];rc3=load('camera-catalogue-semantics.json')['rc3'];assert digest(Path(rc3['original_file']).read_bytes())==digest(Path(rc3['research_copy']).read_bytes())==rc3['sha256']
 pack=load('editor-format-package.json');assert len(pack['components'])==67 and pack['trap_semantics']['phase59_structure_overlay']==s and pack['door_semantics']['phase59_height_consumer']['sha256']==digest((ROOT/'structure-overlay-semantics.json').read_bytes())
 for key,row in pack['components'].items():
  b=(ROOT/row['file']).read_bytes();assert digest(b)==row['sha256'] and json.loads(b)==pack[key]
 assert roundtrips==83 and records==dict(Traps=1293,Doors=656) and patches==3898
 v=dict(phase=59,date='2026-10-27',native_emulations=49,native_case_kinds=dict(counts),captured_native_submissions=submissions,complete_primary_pe_functions=18,primary_data_ranges=8,source_files_roundtripped=roundtrips,catalogue_records=dict(records),exact_in_memory_patches=patches,api_boundary_checks=api,source_executables_unchanged=4,package_components=67,research_writes_to_game_data=0,modified_gameplay_runs=0,unknown_gameplay_fields_remain_open=['Objects34C','Traps204','Terrain198/224'],limitations=s['limitations'])
 (ROOT/'phase59-validation.json').write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps({k:n for k,n in v.items() if k!='limitations'}))
if __name__=='__main__':main()
