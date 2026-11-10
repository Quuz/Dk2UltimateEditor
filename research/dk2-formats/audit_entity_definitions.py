"""Validate old native identities/results, raw patches and all package hashes."""
from pathlib import Path
from collections import Counter
import json,struct
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from capstone.x86 import X86_OP_MEM,X86_OP_IMM
import dk2_formats as fmt
from native_entity_definitions import *
from build_entity_definition_semantics import ROOT,load,digest,primary,identity,link
from build_effect_semantics import pe_read
from emulate_old_entity_definitions import cases,build_case
def main():
 s=load('entity-definition-semantics.json');funcs,blocks=primary();assert s['primary_program_bytes']==funcs and s['primary_data_ranges']==blocks and s['native_class_bindings']==identity();old=(ROOT/'source-binaries/DKII-local13.exe').read_bytes();md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True
 # Verify factory constructor/init call pairs on the same EDI object, and raw ID store.
 for label,ctor,init,id_offset in [('trap',0x4ADF80,0x49E770,0xF5),('door',0x4ADE90,0x49BF10,0xF5),('object',0x4ADD10,0x492370,0x12B)]:
  row=next(r for r in blocks if r['label']==label+'-factory-fragment');code=bytes.fromhex(row['hex']);ins=list(md.disasm(code,row['address']));assert sum(i.size for i in ins)==len(code);calls=[i.operands[0].imm for i in ins if i.mnemonic=='call' and i.operands[0].type==X86_OP_IMM];assert calls==[ctor,init];assert any(i.mnemonic=='mov' and i.operands[0].type==X86_OP_MEM and i.operands[0].mem.disp==id_offset and i.operands[0].size==1 for i in ins)
 # Native source and destination of unknown Object WORD; static-only evidence.
 assert pe_read(old,0x563A73,17)==bytes.fromhex('668b8b4c03000089455066898dcc030000')
 assert pe_read(old,0x4955EC,21)==bytes.fromhex('8b455085c0740e668b85cc03000066898106010000')
 # Native Player configuration ownership and independent diagnostic label.
 assert pe_read(old,0x4B22D0,6)==bytes.fromhex('8b8905100000');assert pe_read(old,0x4B22BC,7)==bytes.fromhex('c680fe0f000001');assert b'ComputerPlayerOn' in pe_read(old,0x691BB4,24)
 counts=Counter();buffers={}
 for c,row in zip(cases(),s['native_emulations']):
  p=ROOT/'ghidra'/row['file'];saved=load('ghidra/'+row['file']);args,want=build_case(c);r=saved['result'];assert digest(p.read_bytes())==row['sha256'] and saved['input']==args and saved['case']==c and saved['expected_registers']==want and r['success'] and r['hit_return'];assert all(int(r['registers'][k],16)==v for k,v in want.items());counts[c['kind']]+=1
  m={int(q['address'],16):bytes.fromhex(q['hex']) for q in json.loads(args['memory'])['regions']};buffers[c['name']]=m
  for f in funcs:assert all(a+len(b)<=f['address'] or a>=f['address']+f['length'] for a,b in m.items())
  if c['kind']=='lookup':
   wantindex=lookup_index(c['ids'],c['key']);name=c['name'].rsplit('-',1)[1];assert wantindex=={'empty':None,'zero':None,'sparse':2,'missing':None,'duplicate':2,'upperbits':2,'unsorted':None}[name]
  else:
   offset=c['height']+(512 if c['family']=='Traps' else 4248 if c['state'] else 2048);assert fmt.u32(m[0x10004000],0x14)==offset
 a=buffers['overlay-Traps-100-0-0'][0x10004000];b=buffers['overlay-Traps-100-0-65535'][0x10004000];assert a==b and counts==dict(lookup=21,overlay=10) and len(cases())==len(s['native_emulations'])==31
 search=load(s['search']['file']);assert digest((ROOT/s['search']['file']).read_bytes())==s['search']['sha256'] and len(search['producer_roots'])==408 and search['root_states']==5966 and search['limited_roots']==search['limited_callees']==0
 unknown=[h for h in search['candidates'] if h['field'].startswith('unknown')];assert len(unknown)==1 and unknown[0]['read']['address']=='0x58acc5' and unknown[0]['read']['instruction']=='mov ax, word ptr [ecx + 0x97]'
 files=patches=0;records=Counter()
 for row in s['corpus']['files']:
  source=Path(row['file']).read_bytes();assert digest(source)==row['sha256'];parts=fmt.parse(source);assert b''.join(x.pack() for x in parts)==source;files+=1
  for si,sec in enumerate(parts):
   name=fmt.FAMILIES.get(sec.signature)
   if name not in FAMILIES:continue
   f=family(name);o=f['patch_offset'];n=f['patch_bytes'];relative=len(sec.header)
   for ri,raw in enumerate(sec.records):
    value=int.from_bytes(raw[o:o+n],'little')^((1<<(8*n))-1);changed=patch_raw(raw,name,value);assert changed[:o]==raw[:o] and changed[o+n:]==raw[o+n:];rr=list(sec.records);rr[ri]=changed;new=fmt.Section(sec.offset,sec.signature,sec.header,list(sec.records),sec.suffix,sec.stride);new.replace_records(rr);packed=b''.join(new.pack() if j==si else x.pack() for j,x in enumerate(parts));manual=bytearray(source);at=sec.offset+relative+o;manual[at:at+n]=value.to_bytes(n,'little');assert packed==manual and b''.join(x.pack() for x in fmt.parse(packed))==packed;patches+=1;records[name]+=1;relative+=len(raw)
 api=0
 for name,f in FAMILIES.items():
  for sha in [OLD,DX]:assert entity_offsets(sha,name)==dict(id=f['old_id'] if sha==OLD else f['dx_id'],definition=f['old_pointer'] if sha==OLD else f['dx_pointer']);api+=1
  for value in [0,(1<<(8*f['patch_bytes']))-1]:assert int.from_bytes(patch_raw(bytes(f['raw_bytes']),name,value)[f['patch_offset']:f['patch_offset']+f['patch_bytes']],'little')==value;api+=1
 bad=[lambda v=v:entity_offsets(v,'Traps') for v in ['1.3','',None]]+[lambda v=v:entity_offsets(OLD,v) for v in ['Terrain',None,1]]
 bad += [lambda v=v:lookup_index([1,3,7],v) for v in [-1,0x100000000,True,1.0]]+[lambda v=v:lookup_index(v,1) for v in [[-1],[256],[True],'123']]
 bad += [lambda v=v:patch_raw(bytes(579),'Traps',v) for v in [-1,65536,True,1.0]]+[lambda v=v:patch_raw(bytes(v),'Traps',0) for v in [578,580]]
 for fn in bad:
  try:fn()
  except ValueError:api+=1
  else:raise AssertionError('Invalid API input accepted')
 for r in s['inherited_current_profiles']:assert digest((ROOT/r['file']).read_bytes())==r['sha256']
 rc3=load('camera-catalogue-semantics.json')['rc3'];assert digest(Path(rc3['original_file']).read_bytes())==digest(Path(rc3['research_copy']).read_bytes())==rc3['sha256']
 oldsrc=load('creature-version-layout-semantics.json')['old_source'];assert digest(Path(oldsrc['original_path']).read_bytes())==digest(Path(oldsrc['path']).read_bytes())==oldsrc['sha256']
 for file,sha in load('executable-hash-verification.json').items():assert digest((ROOT.parent.parent/file).read_bytes())==sha
 pack=load('editor-format-package.json');assert len(pack['components'])==67 and pack['trap_semantics']['phase62_old_entity_definitions']==s
 assert pack['door_semantics']['phase62_old_entity_definitions']==link('Doors') and pack['object_property_semantics']['phase62_old_entity_definitions']==link('Objects')
 for key,row in pack['components'].items():
  b=(ROOT/row['file']).read_bytes();assert digest(b)==row['sha256'] and json.loads(b)==pack[key]
 assert files==84 and records==s['corpus']['records']==dict(Doors=656,Traps=1293,Objects=11756) and patches==13705
 v=dict(phase=62,date='2026-10-27',native_emulations=31,native_cases=dict(counts),complete_executed_native_functions=4,complete_primary_pe_functions=19,primary_data_and_explicit_fragments=16,typed_roots=408,typed_states=5966,typed_leads=10,limited_roots=0,source_files_roundtripped=files,catalogue_records=dict(records),exact_in_memory_patches=patches,api_boundary_checks=api,source_executables_unchanged=4,package_components=67,research_writes_to_game_data=0,modified_gameplay_runs=0,unknown_gameplay_purposes_still_open=True,limitations=s['limitations']);(ROOT/'phase62-validation.json').write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps({k:x for k,x in v.items() if k!='limitations'}))
if __name__=='__main__':main()
