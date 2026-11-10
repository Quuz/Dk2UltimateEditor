"""Native editor findings with explicit separation from game semantics."""
from pathlib import Path
import json,struct
from collections import Counter
from build_keeper_cast import ROOT,load,digest
from build_effect_semantics import pe_read
from native_trap_writer import PROFILES,DIRECT,SCALED,RESOURCES,COMMIT_COPY
from extract_editor_dialogs import extract

def primary():
 spec=load('phase60-primary-manifest.json');functions=[];blocks=[]
 for r in spec['functions']:
  before=ROOT/'ghidra'/r['before'];after=ROOT/'ghidra'/r['after'];mem=ROOT/'ghidra'/r['memory'];v=json.loads(mem.read_text());b=bytes(v['data']);assert before.read_text().rstrip()==after.read_text().rstrip() and int(v['address'],16)==r['address'] and len(b)==v['length']==r['length'] and b==pe_read(Path(r['exe']).read_bytes(),r['address'],r['length'])
  functions.append({**r,'before_sha256':digest(before.read_bytes()),'memory_sha256':digest(mem.read_bytes()),'program_bytes_sha256':digest(b)})
 for r in spec['blocks']:
  mem=ROOT/'ghidra'/r['memory'];v=json.loads(mem.read_text());b=bytes(v['data']);assert int(v['address'],16)==r['address'] and len(b)==v['length']==r['length'] and b==pe_read(Path(r['exe']).read_bytes(),r['address'],r['length']);assert struct.unpack('<d',b)[0]==4096;blocks.append({**r,'memory_sha256':digest(mem.read_bytes()),'program_bytes_sha256':digest(b)})
 return dict(functions=functions,blocks=blocks)

def comparisons():
 from compare_rc3_converters import PRO,RC3
 a=PRO.read_bytes();b=RC3.read_bytes();out=[]
 for row in load('phase60-trap-matches.json'):
  p=pe_read(a,row['pro'],row['bytes']);address=row['rc3_candidates'][0] if row['rc3_candidates'] else 0x4AE750;q=pe_read(b,address,row['bytes']);mask=bytes.fromhex(row['mask_hex']);d=[i for i,(x,y,m) in enumerate(zip(p,q,mask)) if m and x!=y]
  if row['name']=='writer':assert d==[0x18,0x4E,0xA9,0xD4,0xF0,0x42C]
  else:assert not d
  out.append(dict(name=row['name'],pro=row['pro'],rc3=address,bytes=row['bytes'],nonrelocation_differences=[dict(offset=i,pro=p[i],rc3=q[i]) for i in d],scope='Masked relocation targets are not asserted behaviorally equivalent;MFC calls are fixtures in native chains. Writer six byte differences correspond to five count offsets4F4→4F0 and one array4F0→4EC.'))
 return out

def dialogs():
 out=[]
 for name,exe in [('Pro',ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/DK2 Editor pro.exe'),('RC3',ROOT/'source-binaries/DK2-Editor-rc3.exe')]:
  b=exe.read_bytes();ds=extract(b)
  for ident in [255,326]:
   d=next(x for x in ds if x['resource_path'][1]==ident);controls=d['controls'];ix=next(i for i,c in enumerate(controls) if 'Mana Gen.' in str(c['title']));caption=controls[ix];edit=next(c for c in controls if c['id']==12861);assert not caption['template_visible'] and edit['template_visible'];out.append(dict(profile=name,exe_sha256=digest(b),dialog=ident,caption=caption,adjacent_edit=edit,meaning='Caption alone is not property identity. Code and nativeDDX→commit→writer bindedit12861 toManufToBuildraw1FA,notunknown204.'))
 return out

def main():
 p=primary();s=dict(phase=60,scope='Exact editor behavior;never automatically promoted to game rules. User reminder that original editors can contain bugs explicitly retained.',profiles=PROFILES,primary_program_bytes=p['functions'],primary_data_ranges=p['blocks'],native_emulations=load('phase60-trap-writer-emulations.json')['cases'],full_body_comparisons=comparisons(),dialog_resources=dialogs(),
 serialization=dict(raw_record_bytes=579,header_bytes=56,direct=[dict(raw=a,model=b,bytes=n) for a,b,n in DIRECT],scaled=[dict(raw=a,model=b,multiplier=4096) for a,b in SCALED],resources=[dict(raw=a,model=b,bytes=84) for a,b in RESOURCES],flag_mask=0x3FFBF,other_getters='Native2E4DWORD→raw1DC;explicitvirtualgetters2C→raw20DBYTE,38→raw1E0DWORD,40→raw1E4DWORD. Resourcegetterandboundedfixturestringsrunoriginalnativehelpers. Labels/soundfixturelengths5/7;nooverlongstringtests. FTOLusesexplicitexactquarterincrements;noextraarbitraryroundingclaims.'),
 unknown204='RawWORD204→mid2AC presence5C→modelWORD238 isprimary-staticpathinbothprofiles;all26fullwriterand12DDXcommitwriterchainscopyentiremodelWORD238backtoraw204. DDX/commit changecontrols/flagsbutleaveunknownwordunchangedforcheckedinputs. NoManaGennameorgameplayconsumerinferred.',
 manufacture_control='FullDDX470740/475900 requestsedit12861withpanelDWORD78. Inputfixturewrites12345there. Fullcommit471E00/476FC0copieslow16intomodelWORD240;fullwriter4A8F40/4AE750writesrawWORD1FA=12345. Everybyteofmodel/rawchecked. ExistingindependentDXmanufacturingproofinmanufacture-semantics.jsontiesraw1FAtomid2A2→runtime94→queuerequiredpoints. ThisisnotManaGen204.',
 manufacture_game_evidence=dict(file='manufacture-semantics.json',sha256=digest((ROOT/'manufacture-semantics.json').read_bytes()),scope='Inherited independently verified gameplay evidence;not rerun as a new nativegamecase in phase60.'),
 commit=dict(copy=[dict(model=a,panel=b,bytes=n) for a,b,n in COMMIT_COPY],require_mana='panelDWORD120==0→model23CDWORD0→raw1D4DWORD0;nonzero1/2retainsfixturecost12345678. ThisisGUIcommitbehavioronly;notproofgameengineignorescostwhenflagclear.',fixtures='MFC DDXhelperssupplyonecontrolandotherwiseleavefields;rangehelpersno-op;UpdateDatareturns1;CStringcopyfixtureleavesstringhandleunchanged;virtualmodelsettersno-op. DirectTriggerDatasetter425EB0/430320executesrealnativecode. Selectedtablesexplicitboundedpreallocated. NoWindowsGUI,rangerejection,dialogconstructionorcompletefileload.'),
 original_writer_quirks=['HeaderNusescontextcountincludingnullslots. Recordloopskipsnullpointers,butN,S,Dstilldeclare579×allslots. Sixsyntheticedgecasesacrossbothprofiles:twoemptyarraysyieldvalidemptyfiles;fournullslot/mixedarraysyieldphysicallyshorteroutputthanheader. DoesnotprovethatnormalUIactuallycreatesnullslots.',
 'Flagsencoderreconstructsonlymask0003FFBFfrommodelBOOLs;unsupportedrawbitsnotrepresentedinmodelarelost. SupportedbitBOOLscanbenoncanonicalnonzero;writerturnsintobitflags. Notalosslesssavepolicy.',
 'Headerbytes46/47arenotassignedbywriter/datefixtureandreflectpreviousstack. 26directwritercasescompareallheaderbytesincludingseededstackorigin;12DDXcommitwriterchainscompareheaderbytes0..45/48..55butexplicitlyexclude46/47becauseprecedingcallsreusebuffer. Notsemanticdatesandnottoemulateinnewcodec.'],
 native_execution='38cases:26fullwriters(13eachprofile),12fullDDX→fullcommit→fullwriter(6each). Full579-byteoutputrecordscomparedindependently,everymodel300hex andcontext600hex bytechecked. 12chainsalsofull512-bytepanelandcontrolhitcountchecked. Date/fileoutput/virtualgettersfixtures;originalflags/resource/CStringwrite/ftol/getter/setterhelpersrun. FinalEAX/EBPareobserverchecks,notwriterreturnalone.',
 corpus=dict(files=[{**r,'records':r['records']['Traps']} for r in load('structure-overlay-semantics.json')['corpus']['files'] if 'Traps' in r['records']],file_count=82,records=1293),
 api=dict(file='native_trap_writer.py',operations=['record','patch_unknown','commit_projection'],policy='recordisresearchnativewriterprojection;patch_unknownonlychangeschosenWORD,preservesheader/rawunknownflags/allotherbytes. Do notserializebystoringanativeGUIprojectionunlessexplicitcompatibilityexportisrequested.'),
 limitations=['Noeditorisperfectspecification. Labels,DDVconstraints,GUIcommits,serializationandgameconsumersaredifferentclaims. SamecodeinRC3andProcancontainthesamebug.','38nativeemulationsuseexplicitproviders;MFCUIvalidationsnotrun. Whole579recordcomparisondoesnotmeanallfieldsnowhavegameplaynames.','Partialunknownmid→modelpresencepathstaticallyattestedfromcompletecallbackbodies;fullmodelcreation/fileloadnotexecuted.','Nativewriterstringhelpermayoverrun32forlonglabelsasestablishedinotherfamilies;notretestedhereandnotgeneralizedwithoutnewtest.','Finalv4emulationsonlyareauthoritative. Earlierfailedpatternrunsrevealedincorrectexpectedpaddingindex;correctedfromactualstackinstructionsandpatternbyteprobe.','UnknownTraps204,Objects34C,Terrain198/224gameplaypurposesremainopen. Nooriginalgamefile/EXE/parallel-editorwrites.'])
 assert len(s['native_emulations'])==38 and len(s['primary_program_bytes'])==22 and len(s['corpus']['files'])==82
 (ROOT/'trap-writer-semantics.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8');t=load('trap-semantics.json');t['phase60_editor_writer']=s;(ROOT/'trap-semantics.json').write_text(json.dumps(t,ensure_ascii=False,indent=2),encoding='utf8');print('Trapwriter:38native/22PE/2data;82sources1293records;gameunknown204stillopen')
if __name__=='__main__':main()
