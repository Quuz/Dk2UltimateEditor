"""Publish AP HeroLair consumer plus scoped search-policy review links."""
from pathlib import Path
import json,hashlib
import dk2_formats as fmt
from native_action_points import record_view
from build_effect_semantics import pe_read
from emulate_ap_retreat import cases
ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def digest(b):return hashlib.sha256(b).hexdigest()
def link(n):return dict(file=n,sha256=digest((ROOT/n).read_bytes()))

def primary():
    spec=load('phase64-primary-manifest.json');functions=[];blocks=[]
    for r in spec['functions']:
        before=ROOT/'ghidra'/r['before'];after=ROOT/'ghidra'/r['after'];mem=ROOT/'ghidra'/r['memory'];v=json.loads(mem.read_text());b=bytes(v['data']);assert before.read_text().rstrip()==after.read_text().rstrip() and int(v['address'],16)==r['address'] and len(b)==v['length']==r['length'] and b==pe_read(Path(r['exe']).read_bytes(),r['address'],r['length']);functions.append({**r,'before_sha256':digest(before.read_bytes()),'memory_sha256':digest(mem.read_bytes()),'program_bytes_sha256':digest(b)})
    for r in spec['blocks']:
        mem=ROOT/'ghidra'/r['memory'];v=json.loads(mem.read_text());b=bytes(v['data']);assert int(v['address'],16)==r['address'] and len(b)==v['length']==r['length'] and b==pe_read(Path(r['exe']).read_bytes(),r['address'],r['length']);blocks.append({**r,'memory_sha256':digest(mem.read_bytes()),'program_bytes_sha256':digest(b),'hex':b.hex()})
    return functions,blocks

def corpus():
    rows=[];captured={r['file'].lower():r for r in load('phase47-input-manifest.json')['main_files']}
    for old in load('action-point-visibility-semantics.json')['corpus']['files']:
        cap=captured.get(old['file'].lower());source=Path(cap.get('snapshot',old['file']) if cap else old['file']);b=source.read_bytes();sha=digest(b);assert sha==(cap['sha256'] if cap else old['sha256'])
        records=[r for sec in fmt.parse(b) if sec.signature==190 for r in sec.records if fmt.u32(r)==197];flags=[record_view(r)['flags'] for r in records]
        rows.append(dict(file=old['file'],input_path=str(source),sha256=sha,historical_sha256=old['sha256'],records=len(records),hero_lair_bit_records=sum(bool(f&1) for f in flags),raw04_records=sum(bool(f&4) for f in flags),raw08_records=sum(bool(f&8) for f in flags)))
    return dict(files=rows,file_count=len(rows),records=sum(r['records'] for r in rows),hero_lair_records=sum(r['hero_lair_bit_records'] for r in rows),scope='106 inputs inherited from AP corpus;AutoSaveThings uses retainedphase47snapshot. Physicalorder is not proven runtime listorder.')

def main():
    funcs,blocks=primary();native=load('phase64-ap-retreat-emulations.json')['cases'];assert len(native)==len(cases())==23
    source=bytes.fromhex(blocks[-1]['hex']).split(b'\0',1)[0].decode('cp1252');assert source.endswith('FightWithDrawState.cpp')
    s=dict(phase=64,family='ActionPoint.HeroLair retreat consumer',program='DKII-DX.exe',executable_sha256='d7a8b854e4e4f02709eba271bead6fe2307dd6e332b5a96e70857e5b0e19fb8a',raw_flag_mask=1,raw_flags_payload_offset=18,runtime_flag_offset=50,primary_program_bytes=funcs,primary_data_and_fragments=blocks,native_emulations=native,corpus=corpus(),
        identity=dict(function='0x4611D0',native_source_path=source,branch='entityWORD28==globalWORD6F254C andentityBYTE46F&10;otherowner branch is not executed.',native_type_links='CWorldheadWORD12→tagtable6EE540→APnextWORD08,matching inherited AP nativecreator/visibility/toolboxpaths;APinitializer4B0420 invokesbasekind5. NativeGUI HeroLair label/mask1 binding is independently documented inaction-point-editor-semantics.json.',debug_map_attribution='Suppliedsources/DKII_EXE_v170.sgmap names6F254C g_goodPlayerId. Branch semantics here are stated using exact native comparison,not name alone.'),
        selection=dict(rule='Traverse runtime AP linked-list order;onlyBYTE32&1 candidates get navigationcheck. First candidate with nonzero4D0E60providerresult wins;not nearest orlastmatch.',point='X=(unsignedWORDstartX38<<12)+2048,Y=(unsignedWORDstartY3A<<12)+2048,Z=0;endX3C/endY3E,wait30,next33,trigger34,id37 do not choose point inthisbranch.',navigation='Call4D0E60 withECX=creature+124,args(creature,&point,0). It is an explicit reachabilityfixture,not a fullnavigator;thepoint argument is captured.',other_flags='Runtime4/8/0C alone do not selectpoint;1/5/9/0D/3D produce identical selection/calls with matchinggeometry. This only excludes effect inthisbranch;globalAP04/08gameplaypurpose remains unknown.'),
        continuation=dict(state='Call478050 oncreature+24E. Ifresult!=16hex,request476D30(controller,C7,0);zero rejectscontinuation.',movement='Try471890(controller,&selectedpoint,10A,E8);on0try10A,68. Both0return0;firstsuccess preventssecondcall. Calls/providerscaptured,actual movement notsimulated.',success='Call4887D0(creature,1,0),then audio75B468 virtual20(oldentityDWORD2E,definitionDWORD6E3,DC,&entityposition1A);native stores providerreturn inentity2E.',tracking='ForcreatureBYTE36F!=1,storelow16(signedentityX1A>>12) intoBOTH52Aand52C,thenactual517490 reads world4→clock4 intoentity52E. TwoXloads at4617FB/46180B are nativefacts;intent andgameplayimpactunresolved. CreatureID1skipstrackingwrites.',result='Native0/1 saved separately inobservationbuffer beforeobserverEAXoverwrites.'),
        fixtures=dict(native_functions=['4611D0 fullbody1654bytes','517490 fullgetter7bytes'],providers=['4D0E60 reachability','478050 state query','476D30 state request','471890 movement request','4887D0 post-success update','audio vslot20'],comparison='Complete16-bytecontroller,1536-bytecreature,32-byteworld,16-byteclock,2304-bytedefinition,all64-byteAPnodesand512observations compared. Nativefunctions executeunchanged;providers localEmulatorHelperonly.'),
        read_search_review=link('memory-read-policy.json'),inherited_gui_evidence=link('action-point-editor-semantics.json'),
        api=dict(file='native_ap_retreat.py',operation='project',scope='Explicitprovider projection ofonly owner-match/APbranch;notlosslessserializer. Rawpatchesuse native_action_points.patch_raw.'),
        limitations=['Exact supplied DX only;no oldgame/RC3 runtimeequivalenceclaim.','23 full4611D0 invocations coverowner-match/AP branch andsuccess/failurecontinuation;otherownerroom/randomretreatbranchesnotexecuted.','Navigation,state,movement,post-success andsoundeffects suppliedbyfixtures. Fullgame,actualretreatbehavior,collisionsandrenderingnotexecuted.','No assumption thatphysicalfileorder equalsruntimeAP linked-listorder. No listcycle execution;projectionrejectscycles ratherthan claimingnativecyclehandling.','HeroLair bit1 now has thisverifiedgameconsumer;AP04/08 stillnotfullydecoded.','Readpolicycorrection exposes historical staticsearchblindspot,not wrongnativeexecution. Newnegativepasses remainbounded;8AProots/28callees hitlimits.','RepeatedXtrackingwriteis verifiedinstruction/statefact,not demonstratedvisiblebug or editordefect.'])
    (ROOT/'ap-retreat-semantics.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8')
    p=ROOT/'action-point-semantics.json';base=load(p.name);base['phase64_retreat']=s;p.write_text(json.dumps(base,ensure_ascii=False,indent=2),encoding='utf8')
    for filename in ['unresolved-field-traces.json','creature-version-layout-semantics.json']:
        p=ROOT/filename;t=load(filename);t['phase64_search_review']=link('memory-read-policy.json');p.write_text(json.dumps(t,ensure_ascii=False,indent=2),encoding='utf8')
    print('APretreat23native;corpus',s['corpus']['file_count'],s['corpus']['records'],'HeroLair',s['corpus']['hero_lair_records'])
if __name__=='__main__':main()
