"""Attach shared Trap/Door height consumers to existing family components."""
from pathlib import Path
from collections import Counter
import json,struct
import dk2_formats as fmt
from build_keeper_cast import ROOT,load,digest
from build_effect_semantics import pe_read
from native_structure_overlay import FAMILIES

def primary():
 spec=load('phase59-primary-manifest.json');functions=[];blocks=[]
 for r in spec['functions']:
  before=ROOT/'ghidra'/r['before'];after=ROOT/'ghidra'/r['after'];mem=ROOT/'ghidra'/r['memory'];v=json.loads(mem.read_text());b=bytes(v['data'])
  assert before.read_text().rstrip()==after.read_text().rstrip() and int(v['address'],16)==r['address'] and len(b)==v['length']==r['length'] and b==pe_read(Path(r['exe']).read_bytes(),r['address'],r['length'])
  functions.append({**r,'before_sha256':digest(before.read_bytes()),'memory_sha256':digest(mem.read_bytes()),'program_bytes_sha256':digest(b)})
 for r in spec['blocks']:
  mem=ROOT/'ghidra'/r['memory'];v=json.loads(mem.read_text());b=bytes(v['data']);assert int(v['address'],16)==r['address'] and len(b)==v['length']==r['length'] and b==pe_read(Path(r['exe']).read_bytes(),r['address'],r['length']);blocks.append({**r,'memory_sha256':digest(mem.read_bytes()),'program_bytes_sha256':digest(b)})
 return dict(functions=functions,blocks=blocks)

def corpus():
 paths={}
 for row in load('terrain-lighting-semantics.json')['corpus']['files']:
  p=Path(row['file'])
  if p.name=='Terrain.kwd':
   for family in FAMILIES:
    q=p.with_name(family+'.kwd');paths[q]=digest(q.read_bytes())
  else:paths[p]=row['sha256']
 files=[];counts=Counter();height=Counter();profiles=[]
 for p,sha in sorted(paths.items()):
  b=p.read_bytes();assert digest(b)==sha;families={}
  for sec in fmt.parse(b):
   if sec.signature not in [130,140]:continue
   family={130:'Traps',140:'Doors'}[sec.signature];families[family]=len(sec.records);counts[family]+=len(sec.records)
   for r in sec.records:
    value=fmt.u32(r,FAMILIES[family]['raw_height']);height[(family,value)]+=1
  files.append(dict(file=str(p),sha256=sha,records=families))
 return dict(files=files,file_count=len(files),records=dict(counts),height_dword_counts={f'{f}:{v}':n for (f,v),n in sorted(height.items())})

def main():
 p=primary();search=load('phase59-typed-definition-leads.json')
 s=dict(phase=59,program='DKII-DX.exe',program_sha256=load('executable-hash-verification.json')['Dungeon Keeper 2/DKII-DX.exe'],primary_program_bytes=p['functions'],primary_data_ranges=p['blocks'],native_emulations=load('phase59-overlay-emulations.json')['cases'],corpus=corpus(),fields=FAMILIES,
 identity='Trap4A7940 pushes3,Door4A5070 pushes4 into4B28D0→4B4A10,which storesentityDWORD12. They obtain worlddefinition throughvslots160/138 and storeentityPTRF7. NativeRTTI.?AVCTrap@@/.?AVCDoor@@attested. Native4A6F10labelsDWORD10D asDoorOpen at69E514.',
 loader_scope='All49run complete family converter withzeroed intermediate. OnlyheightDWORD raw→mid independentlychecked;otherconverterfieldsnotassignednewmeanings. Loader limited to presence-gated4AA4D6..4AA4E9 or4A71A6..4A71B9;copieslow16DWORDintoheightWORD. EntryjumpoverotherloadercodeandRETstoponlyinEmulatorHelpermemory.',
 consumer='Full5955C0:kind3 readsunsigneddefinitionWORDA0 and adds200hex. Kind4readsunsignedWORD97 and adds800hex,plus898hexifinstanceDWORD10D!=0. WritesoffsetDWORD14 and addsitmodulo32toZDWORD2D/39 in300-byteoverlaycontext. No inferenceofallphysicalheight/collision/renderingrules.',
 examples=[dict(raw_height=32768,trap_offset=33280,door_closed_offset=34816,door_open_offset=37016),dict(raw_height=65535,trap_offset=66047,door_closed_offset=67583,door_open_offset=69783),dict(raw_height=65536,trap_offset=512,door_closed_offset=2048,door_open_offset=4248)],
 caller='27full5953D0runs:24activeordinarypaths,2falseeligibilityreturns,1TrapId16specialreturn. Activecallcopiesentityposition1A/1E/22intobothcontextvectors,executesreal5955C0,real5956E0colour/timingandreal5958F0resourcestaging. Native5958F0andoutercall eachsubmitto59B880fixture,48total. CaptureincludesbothXYZ,renderinfo/subjectpointersand27-byteresource;allcomparedexactly. TrapId16stillpositions/colours,butreturnsbeforebothsubmissions. Eligibilityfalseskipsallcontextwrites.',
 partial='Intermediatepresence0keepspreviousheightWORDA5A5;2andFFFFFFFFloadlow16justlike1. DoorOpen0/1/2/FFFFFFFFtested;allnonzerocheckedpatternsselectsameadditionaloffset.',
 fixtures=['Entityvcall5Celigibility;vcall2Cusesreal4B5C10positiongetter','Worldplayerlist/player/ownercolourproviders;syntheticnull134word andfuturetimers','5B5E90returns1000;596EB0returnsknown27-byteresource','4B37D0capturesresource-conversioncount;59B880capturesupto2submissions,boundedbeforeREP'],
 native_execution='49completeconverters+49explicitheightloaderfragments;22directfull5955C0checksand27full5953D0calls(25alsoexecute5955C0). All300context,181/209definition,1536entity,579/616raw,136capturedrawbytescompared;converterheightDWORDindependentlymatchesraw. Full732/747intermediatecomparedtoanativepostconversionsnapshotforlaterimmutabilityonly. Snapshotisnotindependentsemanticvalidationofallconverterfields.',
 search=dict(file='phase59-typed-definition-leads.json',sha256=digest((ROOT/'phase59-typed-definition-leads.json').read_bytes()),producer_roots=len(search['producer_roots']),states=search['states'],limited_roots=search['limited_roots'],mapped_spans=search['mapped_spans'],candidates=len(search['candidates']),rejection='Onlyunknown204overlaphitat5955E5belongsentitykind4DoorHeightWORD97,notTrap98. EntityF7isasharedoffsetofdistinctTrap/Doordefinitionpointers. Nativekinddispatchandconstructorbindingsrejectthisapparentunknownread.',result='Objects34C/Traps204gameplaypurposesremainunresolved. PreviouslyconfirmedObjectHealthandTrapManaUsagereadsarepositivecontrols;notallotherhintrootsareproventypes.',limits=search['scope']),
 api=dict(file='native_structure_overlay.py',operations=['height_offset','project','patch_height'],scope='Onlyentityoverlayplacement;rawheightpatchretainsfullDWORDandallotherrecordbytes,nevernormalizeshigh16bits'),
 limitations=['ExactDXonly;noRC3/local13gameplayequivalenceclaim.','Fullfamilyconvertersrun,butonlyheighttransferindependentlyvalidated. Completecatalogueparser/worldinitialization/fullfamilyloadernotexecuted.','5953D0/5955C0/5956E0/5958F0arecompletebodyexecutionswithspecifiedproviders;rendererandresourceconversionfunctionsarefixtures,noliveimageassertion.','49syntheticstatesincludeDWORDoverflowandnoncanonicalDoorOpenpatterns;notclaimtheseallarisefromnormalGUI. No automaticclamportruncateofrawHeightinserialization.','NativeDoorOpenlabelprovesfieldidentity,notdooranimation/state-machineemulation.','Searchisboundedanddoesnotestablishglobalnon-use. Objects34C/Traps204/Terrain198/224remainopen.','Sourcegamefiles/fourEXEs/parallel-editoruntouched. NativecodepatchesonlyinsideEmulatorHelpertemporarymemory.'])
 assert len(s['native_emulations'])==49 and len(s['primary_program_bytes'])==18 and len(s['primary_data_ranges'])==8
 (ROOT/'structure-overlay-semantics.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8')
 traps=load('trap-semantics.json');traps['phase59_structure_overlay']=s;(ROOT/'trap-semantics.json').write_text(json.dumps(traps,ensure_ascii=False,indent=2),encoding='utf8')
 doors=load('door-semantics.json');doors['phase59_height_consumer']=dict(file='structure-overlay-semantics.json',sha256=digest((ROOT/'structure-overlay-semantics.json').read_bytes()),field=FAMILIES['Doors'],scope=s['consumer']);(ROOT/'door-semantics.json').write_text(json.dumps(doors,ensure_ascii=False,indent=2),encoding='utf8')
 print('Structureoverlay:49native;18PE/8data;',s['corpus']['file_count'],'files;',s['corpus']['records'])
if __name__=='__main__':main()
