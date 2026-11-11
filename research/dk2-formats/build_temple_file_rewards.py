"""Build bounded Variables-file/temple reward composition and preservation evidence."""
from pathlib import Path
import json,struct
import emulate_temple_file_rewards as n
import crosscheck_temple_file_rewards as cross
import append_temple_recipe as app
from copy import deepcopy
ROOT,load,dump,sha=n.ROOT,n.load,n.dump,n.sha
KEY,SCHEMA='temple_file_reward_semantics','temple-file-reward-semantics.json'
canonical=lambda x:sha(json.dumps(x,sort_keys=True,ensure_ascii=False).encode())
link=lambda f:dict(file=f,sha256=sha((ROOT/f).read_bytes()))

def physical():
 raw=n.files.GLOBAL.read_bytes();parts=n.files.fmt.parse(raw);assert b''.join(p.pack() for p in parts)==raw
 oldsize,oldcount,olddata=[int.from_bytes(raw[o:o+4],'little') for o in [8,20,52]];rows=[]
 for kind,value in [(3,5000),(2,17)]:
  recipe=n.raw_recipe([(3,9)],kind,value);out=app.append(raw,recipe)
  expected=bytearray(raw[:oldsize]+recipe+raw[oldsize:])
  for off,v in [(8,oldsize+16),(20,oldcount+1),(52,olddata+16)]:struct.pack_into('<I',expected,off,v)
  assert out==expected and out[oldsize+16:]==raw[oldsize:]
  restored=bytearray(out[:oldsize]+out[oldsize+16:])
  for off in [8,20,52]:restored[off:off+4]=raw[off:off+4]
  assert restored==raw and b''.join(s.pack() for s in n.files.fmt.parse(out))==out
  rows.append(dict(reward_kind=kind,value=value,raw_recipe=recipe.hex(),source_bytes=len(raw),result_bytes=len(out),old_S_N_D=[oldsize,oldcount,olddata],new_S_N_D=[oldsize+16,oldcount+1,olddata+16],all_other_bytes_and_trailing_section_preserved=True,exact_removal_inverse=True))
 return dict(appended_recipe_controls=rows,source_sha256=sha(raw),new_physical_fields=0,source_writes=0)

def build():
 old=load('phase202-prior-package-state.json');assert len(old['components'])==179
 for ref in old['components'].values():assert ref==link(ref['file'])
 assert old['codec']['sha256']==link('dk2_formats.py')['sha256']
 for ref in load('phase194-source-baseline.json')['source_files']+load('phase194-source-baseline.json')['executable_paths']:assert sha(Path(ref['file']).read_bytes())==ref['sha256'],ref['file']
 accepted=load('phase202-temple-file-emulations.json');assert len(accepted['replays'])==16
 for c,row in zip(n.cases(),accepted['replays']):
  args,ch,meta,saved,file=n.execute(c);_,out=n.verify(c,args,ch,meta,saved['result'])
  assert row==dict(case=c,file=str(file.relative_to(ROOT)),sha256=sha(file.read_bytes()),outcome=out)
 controls=load('phase202-temple-file-crosschecks.json')['replays'];assert [r['case'] for r in controls]==cross.CONTROLS
 for row in controls:
  args,count,size=cross.build(row['case']);saved=load(row['file']);r=saved['result']
  assert saved['input']==args and sha((ROOT/row['file']).read_bytes())==row['sha256']
  assert (count,size)==(row['checked_regions'],row['checked_bytes']) and not saved['tool_is_error'] and r['success'] and r['hit_return']
  assert all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
 primary=load('phase202-temple-file-primary.json');assert primary['all_executed_original_PCs_covered']
 for row in primary['functions']:
  for key in ['memory','assembly']:assert sha((ROOT/row[key]).read_bytes())==row[key+'_sha256']
 counts=dict(native_compositions=16,file_records=sum(r['outcome']['file_records'] for r in accepted['replays']),native_instructions=sum(r['outcome']['native_steps'] for r in accepted['replays']),
  Ghidra_full_context_controls=5,Ghidra_compared_regions=sum(r['checked_regions'] for r in controls),Ghidra_compared_bytes=sum(r['checked_bytes'] for r in controls),
  original_PCs=primary['unique_executed_original_PCs'],function_byte_extents=len(primary['functions']),prior_components_preserved=179,source_files_preserved=420,EXE_paths_preserved=7,new_physical_fields=0,source_writes=0)
 return dict(phase=202,date='2026-11-11',profile=dict(file=str(n.EXE),sha256=sha(n.EXE.read_bytes())),counts=counts,primary=primary,native=accepted,Ghidra_controls=controls,
  scope='Compose previously separate original Variables reader, spell sacrifice entry, ordered matcher and gold/Object factory paths. No new physical field or new enum name. Earlier phase88/89 mechanisms remain earlier results, not newly decoded fields.',
  chain=[dict(address=hex(a),role=role) for a,role in [(0x56CBD0,'original outer file dispatch'),(0x5716E0,'Variables record reader'),(0x521790,'World dispatch and checksum'),(0x51DB30,'type43 converter'),(0x50F7E0,'store34-byte recipe'),(0x5133A0,'complete spell sacrifice entry'),(0x4F1600,'append history'),(0x4F1660,'ordered suffix match'),(0x4F1780,'reward dispatcher'),(0x4BA420,'gold placement/credit helper'),(0x4B7A90,'Object factory')]],
  files=dict(source=link('temple-recipes-current.json'),source_Variables_sha256=physical()['source_sha256'],original_total_records=621,original_recipe_records=44,
   source_recipe_inputs='All44 source recipes use Creature ingredients; no original spell/gold reward recipe is claimed.',
   selected_recipes_only='14 scenes read a prepared real E6 section containing44 unchanged source recipes plus optional controlled Type43 records.',
   complete_source_section='Two scenes read the exact first9992-byte E6 section of sourceGlobalVariables (621 records), or the exact10008-byte first section from losslessly appended copy(622 records). All callbacks execute, not only43. TrailingBE section and whole Level/World construction are not included in the runtime replay.',
   original_file_layers='Actual MyFile/Disc/stream and real56CBD0/5716E0/521790. Header callback and bounded in-memory Win32 provider declared. No physical disk output.'),
  composition=dict(selected_recipe_before_failure='Recipe selected and history cleared even if later allocation gate/pool refuses Object creation. No rollback or search for a later recipe was seen in these cases.',
   source_plus_gold='621 unchanged source records + one spell9->gold5000 recipe reaches full5133A0 and creates registered Object1 with native amount5000.',
   ownership='Roomowner8 differs from global Objectowner7; physical Object reward belongs7, direct positive gold credit at x0/y0 changes roomowner8+7E. This is scoped native behavior, not a recommendation for neutral/edge cells.',
   negative_gold='Negative request with empty treasury list leaves balance and object pool unchanged. Does not generalize to a player with treasury rooms.',
   position='Original4E4D90 selects eligible dry cell from prepared two-cell finite room list. Action at neighbouring pool cell, factory object at selected cell center and z1000; no natural room construction or physical drop.'),
  outcomes={r['case']['name']:r['outcome'] for r in accepted['replays']},physical_preservation=physical(),
  fixture_limits=dict(World='PreparedWorld,32x32 cells, owner lists and object heap. Relocated declared Session/GFX/Sound provider buffers permit fullA3D0World; original game code/vtables remain unchanged.',
   definitions='Prepared285-byte Object runtime definition from phase89, not newly loaded from Objects.kwd. Actual factory/constructor/initializer/tag/map/owner registration execute; graphics null and sound providers are explicit.',
   entry='Complete spell sacrifice entry5133A0 only. Creature472930 and Object49F600 entry-to-factory composition is still open. No whole AI/drop/movement/deletion or live render/audio simulation.',
   services='Original PE/IAT targets are retained. Existing explicit external IsBadReadPtr, GFX, sound and file provider code executes outside game text. No game-function/vtable substitution.'),
  limitations=['No decoded meaning added for Globals29/186 or scalar113/114. Global block remains unfinished; goal active.','No successful treasury-room allocation/debit, Creature reward with this file reader, or fresh Object raw loader in this composition.','Custom spell/gold recipes are diagnostics beyond original44; observed support does not prove original GUI exposes those options.','Runtime recipes at most46 in these scenes; no array-capacity or overflow proof.','Crypt remains deferred; ten meanings unchanged. Resistances/creature flags follow global work.'],
  helpers=[link(x) for x in ['emulate_temple_file_rewards.py','crosscheck_temple_file_rewards.py','prepare_temple_file_rewards_primary.py','build_temple_file_rewards.py','audit_temple_file_rewards.py']])
NOTE='temple_file_reward_semantics phase202 composes previous88/89 mechanisms withactualVariablesfile andwhole5133A0:16native/5fullcontextGhidra. Exact621recordsourceE6/622appendedcopyexecuteallcallbacks,thennewspell9->gold5000createsregisteredObject1. Source44unchanged; firstmatchedrecipeconsumedbeforefactoryfailure;globalObjectowner7vsRoomowner8andx0/y0creditconfirmedwithinpreparedfixtures. No freshObjectrawload,Creatureentry-tofactory,treasuryornewfieldmeaning;179priorcomponents/codec/420sources/7EXEspreserved,package180,Globalsactive,Cryptdeferred.'
def main():
 package=load('editor-format-package.json');old=load('phase202-prior-package-state.json')
 for k,h in old['preserved_top_level'].items():
  if k!='precedence':assert canonical(package[k])==h
 assert canonical(package['precedence'][:len(old['precedence'])])==old['preserved_top_level']['precedence']
 assert set(package['components']) in [set(old['components']),set(old['components'])|{KEY}]
 s=build();dump(SCHEMA,s);package[KEY]=s;package['components'][KEY]=link(SCHEMA)
 if NOTE not in package['precedence']:package['precedence'].append(NOTE)
 dump('editor-format-package.json',package)
 dump('phase202-search-status.json',dict(phase=202,current_block='Global parameters',block_complete=False,goal_status='active',Crypt_deferred=True,counts=s['counts'],schema=link(SCHEMA)))
 print(s['counts'])
if __name__=='__main__':main()
