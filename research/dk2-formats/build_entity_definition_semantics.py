"""Old entity identity, typed-search limits and exact version-specific pointers."""
from pathlib import Path
import json,hashlib,struct
from collections import Counter
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import pe_read
import dk2_formats as fmt
from native_entity_definitions import FAMILIES,DX,OLD
ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def digest(b):return hashlib.sha256(b).hexdigest()
def primary():
 spec=load('phase62-primary-manifest.json');out=[];blocks=[]
 for r in spec['functions']:
  before=ROOT/'ghidra'/r['before'];after=ROOT/'ghidra'/r['after'];mem=ROOT/'ghidra'/r['memory'];v=json.loads(mem.read_text());b=bytes(v['data']);assert before.read_text().rstrip()==after.read_text().rstrip() and int(v['address'],16)==r['address'] and len(b)==v['length']==r['length'] and b==pe_read(Path(r['exe']).read_bytes(),r['address'],r['length']);out.append({**r,'before_sha256':digest(before.read_bytes()),'memory_sha256':digest(mem.read_bytes()),'program_bytes_sha256':digest(b)})
 for r in spec['blocks']:
  mem=ROOT/'ghidra'/r['memory'];v=json.loads(mem.read_text());b=bytes(v['data']);assert int(v['address'],16)==r['address'] and len(b)==v['length']==r['length'] and b==pe_read(Path(r['exe']).read_bytes(),r['address'],r['length']);blocks.append({**r,'memory_sha256':digest(mem.read_bytes()),'program_bytes_sha256':digest(b),'hex':b.hex()})
 return out,blocks
def identity():
 data=(ROOT/'source-binaries/DKII-local13.exe').read_bytes();md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;out=[];vt=struct.unpack('<230I',pe_read(data,0x65F31C,920))
 for name,cl,init,ctor,slot,store in [('Traps','CTrap',0x49E770,0x4ADF80,0x160,0x49E7A6),('Doors','CDoor',0x49BF10,0x4ADE90,0x138,0x49BF49),('Objects','CObject',0x492370,0x4ADD10,0x188,0x4923AC)]:
  f=FAMILIES[name];r=next(r for r in load('phase62-old-structure-rtti-leads.json') if r['name']==cl);locator=struct.unpack('<5I',pe_read(data,r['locator'],20));native_name=('.?AV'+cl+'@@\0').encode();assert locator[3]==r['type_descriptor'] and pe_read(data,r['type_descriptor']+8,len(native_name))==native_name and struct.unpack('<I',pe_read(data,r['vtable']-4,4))[0]==r['locator'];assert vt[slot//4]==f['lookup_old']
  text=(ROOT/'ghidra'/f'phase62-old-{init:x}-disassemble_function.txt').read_text();assert f'PUSH 0x{f["kind"]:x}' in text and f'0x{f["old_pointer"]:x}],EAX' in text;ins=next(md.disasm(pe_read(data,store,15),store));assert ins.mnemonic=='mov' and ins.operands[0].mem.disp==f['old_pointer']
  ctor_text=(ROOT/'ghidra'/f'phase62-old-{ctor:x}-disassemble_function.txt').read_text();assert f'0x{r["vtable"]:x}' in ctor_text
  out.append(dict(family=name,native_class=cl,initializer=init,constructor=ctor,vtable=r['vtable'],locator=r['locator'],type_descriptor=r['type_descriptor'],lookup_vslot=slot,lookup=f['lookup_old'],pointer_store=store,entity_definition_offset=f['old_pointer'],entity_id_offset=f['old_id'],kind=f['kind']))
 assert pe_read(data,0x4AB5B2,3)==bytes.fromhex('894112')
 return out
def corpus():
 names={r['file'] for r in load('structure-overlay-semantics.json')['corpus']['files']};names.add(str(ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/Objects.kwd'));rows=[];total=Counter()
 for n in sorted(names):
  raw=Path(n).read_bytes();counts=Counter()
  for sec in fmt.parse(raw):
   f=fmt.FAMILIES.get(sec.signature)
   if f in FAMILIES:counts[f]+=len(sec.records)
  total.update(counts);rows.append(dict(file=n,sha256=digest(raw),records=dict(counts)))
 return dict(files=rows,file_count=len(rows),records=dict(total))
def link(name):
 p=ROOT/'entity-definition-semantics.json';s=load(p.name);return dict(file=p.name,sha256=digest(p.read_bytes()),family=name,profile=s['families'][name],scope='Exactoldentityoffsets and lookup/overlay consumers;nativeclass/initializationbinding static;unknown gameplay purposes remain open.')
def main():
 functions,blocks=primary();search=load('phase62-old-typed-consumer-leads-v3.json');assert len(search['producer_roots'])==408 and search['root_states']==5966 and search['limited_roots']==0 and search['limited_callees']==0 and len(search['candidates'])==10
 s=dict(phase=62,scope='Exact supplied old executable entity/class/definition identity, bounded typed search and native lookup/overlay tests. Does not resolve unknown gameplay fields.',old_sha256=OLD,dx_sha256=DX,version_scope='Folderlabel13 not independent retailversionproof;exactSHA required.',families=FAMILIES,primary_program_bytes=functions,primary_data_ranges=blocks,native_class_bindings=identity(),native_emulations=load('phase62-entity-emulations.json')['cases'],
 lookup=dict(rule='key=requestedDWORD&255;i=min(key-1,count-1);walkbackwards,equalityreturnsindex,smallerkeyorexhaustionreturnsnull.',native_old_functions=[0x502300,0x5020A0,0x502560],tests='21 complete native calls:three families × empty,physicalID0,sparseIDs,missingID,duplicateID,upperDWORDbits,unsortedinput. All table/worldbytes unchanged. PhysicalID0 notfound becauseinitialindex -1. UnsortedfixturecanmisspresentID;not proof of unsortednormalcatalogueinsertion.',normal_world_order='Array order is runtime state;never silently reorder physical KWD records. Existing catalogue callback behavior remains separately specified.'),
 overlay=dict(function=0x58ACA0,tests='10 complete native calls;all300 context bytes projected,512 entity bytes andcomplete181/209 definition bytes unchanged. Only kinds3/4 branchesrun;no rendererorresource providers.',old_pointer=0xF6,old_door_state=0x10C,trap='unsignedWORDdefinitionA0+512',door='unsignedWORDdefinition97+2048,plus2200 when entityDWORD10C!=0',writes='context14=offset;addoffsetmodulo2**32toZ2Dand39.',rejected_hit='Read58ACC5WORD97 belongskind4 DoorHeight. It overlaps numericTrap98, butnotTrapdefinitiontype. Trapbranch58ACF4readsWORD A0. Native pairedTrapheight100 cases varydefinitionWORD98=0000/FFFF withsamecompletecontextoutput.'),
 object_unknown_storage=dict(raw=0x34C,raw_bytes=2,converter=0x563920,raw_read=0x563A73,intermediate=0x3CC,presence=0x50,loader=0x494E80,loader_gate=0x4955EC,loader_read=0x4955F3,loader_store=0x4955FA,definition=0x106,scope='Static instructions in completeoldconverter/loaderbodies. Not an emulatedfullObjectsloader or gameplay consumer. Native editor ManaGen label remains only UI evidence.'),
 rejected_ai_candidate=dict(read=0x4ED876,function=0x4ED3B0,owner_bridge=0x4B22D0,owner_pointer=0x1005,enable_fragment=0x4B22B0,enable_byte=0xFFE,native_label='ComputerPlayerOn',label_address=0x691BB4,diagnostic_fragment=0x4B4341,caller=0x512D00,source_copy=dict(address=0x512D6B,source_offset=0x28,bytes=158),scope='Owner recordcallbackcopies158-byteconfigurationintoastacktemporary;bridge forwards it toownedpointer1005. Enableuses1005andsetsFFE;native diagnosticlabelsFFE ComputerPlayerOn. TheWORD98readisonthatconfigurationbuffer,not averifiedTrapsdefinition. No gameplaypurposeassignedtothisAIconfigWORD.'),
 search=dict(file='phase62-old-typed-consumer-leads-v3.json',sha256=digest((ROOT/'phase62-old-typed-consumer-leads-v3.json').read_bytes()),roots=len(search['producer_roots']),states=search['root_states'],limited_roots=0,callee_summaries=search['callee_summaries'],limited_callees=0,leads=10,scope=search['scope'],rejected_unknown_lead='Onlyunknown204lead58ACC5isDoorHeight;zeroObjects34Cleadsinthisboundedpass. Thisdoesnotestablishglobalnon-use.',diagnostics='v1usedDXF7/12Dincorrectlyforoldentityhints;v2usedcorrectF6/12CbutincludedESPstackloads,oneofwhichhitlimit. v3excludesESPentityseeds;onlyv3authoritative. Previousoutputsretainedasdiagnostics.'),
 inherited_current_profiles=[dict(file=n,sha256=digest((ROOT/n).read_bytes())) for n in ['structure-overlay-semantics.json','placed-object-semantics.json']],corpus=corpus(),api=dict(file='native_entity_definitions.py',operations=['entity_offsets','lookup_index','patch_raw'],policy='Runtime pointeroffsets never usedasrawfileoffsets;patch_raw preservesallbytesoutsideoneexplicitrawspan.'),limitations=['UnknownTraps204,Objects34C,Terrain198/224 meanings remain open. Same numericdisplacement is not objectidentity.','Only four distinct native functions executed:newoldthreegettersandfull58ACA0overlay. Constructors,initializers,basekind,AIconfiguration,Objectsstorageandfactoryfragmentsstaticonly.','Code fragments explicitlylisted among16ranges;19fullbodiesattestedseparately. Ghidradisjointenablebody is not declaredcontiguouswholefunction.','No completeallocator/worldinsertion/AI/gameplay/rendererexecution;no modifiedoriginalfiles.','Bounded search excludesdynamicindices,numericloopbounds,indirectcallreturnsandstack-pointee inference. No global non-use proof.'])
 assert len(functions)==19 and len(blocks)==16 and len(s['native_emulations'])==31
 p=ROOT/'entity-definition-semantics.json';p.write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8');t=load('trap-semantics.json');t['phase62_old_entity_definitions']=s;(ROOT/'trap-semantics.json').write_text(json.dumps(t,ensure_ascii=False,indent=2),encoding='utf8')
 for name,file in [('Doors','door-semantics.json'),('Objects','object-property-semantics.json')]:
  t=load(file);t['phase62_old_entity_definitions']=link(name);(ROOT/file).write_text(json.dumps(t,ensure_ascii=False,indent=2),encoding='utf8')
 print('Entitydefinitions:31native/19PE/16ranges;408roots5966states10leads;corpus',s['corpus']['file_count'],s['corpus']['records'])
if __name__=='__main__':main()
