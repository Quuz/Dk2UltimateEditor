"""Publish bounded mode2 generation, raw edits and scoped Effects pointer search."""
from pathlib import Path
from copy import deepcopy
import json
import emulate_effect_adjacent as native
import crosscheck_effect_adjacent as controls
import prepare_effect_adjacent_primary as primary
import validate_effect_adjacent_storage as storage
import effect_call_abi as abi
ROOT=Path(__file__).resolve().parent
sha=native.sha
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def link(n):return dict(file=n,sha256=sha((ROOT/n).read_bytes()))

def effective_layout():
    out=deepcopy(load('phase158-prior-effective-effects.json'))
    for f in out['fields']:
        o=f['offset']
        if o==0xB4:f['verified_mode2_flags']=dict(target_mask=0x40,element_mask_0x80_not_required=True,both_masks_choose='Effects',quality_mask_0x4000='No generation scale read/effect in tested mode2 paths',random_mask_0x400='Calls native435E30; testedall-eight-equalID1, no new distribution probabilities.')
        elif 0xCA<=o<=0xD8 and (o-0xCA)%2==0:f['verified_mode2_slot']=dict(index=(o-0xCA)//2,used=True,normal_order='7,6,5,4,3,2,1,0 perbatch pereligibleadjacentcell',zero='skip',duplicates='preserved',target='Effects iff currentinstance definition flags40, otherwiseEffectElements')
        elif o==0xF2:f['verified_mode2']=dict(callback=0x4376E0,legacy_name='ADJACENT_EXPLOSION',native_name_confirmed=False,rule='Four Q12 cardinal-neighbor centers, checked by447A70; normal selection traverses all8slots perbatch. Preparednativefixtures, not originalmap execution.')
        elif o==0xF3:f['verified_mode2_count']=dict(formula='unsignedBYTE batches pereligibleadjacentcell',normal_requests='eligibleCellCount*N*nonzeroSlotCount',unit='Requests before CBridge rejection/NextEffectId/allocation; notsuccessfulchildcount or persecond',quality='flags4000/SCALE69752C do not scale tested mode2 branch')
        elif o in [0xEE,0xF0]:f['verified_mode2_opaque']=dict(status='unresolved',runtime_offset=0x4B if o==0xEE else 0x4D,observation='Loaded unchanged; no definition reads in16 testedcompletepaths. Notglobalnonuse.')
    return out

def build():
    attest=primary.build();assert attest==load('phase158-adjacent-primary.json')
    manifest=load('phase158-adjacent-emulations.json');assert len(manifest['replays'])==16
    posts={};pairs=[]
    for c,row in zip(native.cases(),manifest['replays']):
        args,ch,d,file=native.execute(c);post,out=native.verify(c,args,ch,d['result']);posts[c['name']]=post
        assert row['case']==c and row['outcome']==out and row['sha256']==sha(file.read_bytes())
        if c['name'] in ['parent-misc','child-misc']:pairs.append(native.compare_pair(posts['one'],post,c))
        if c['name']=='scale255':pairs.append(native.compare_pair(posts['scale50'],post,c))
    assert pairs==manifest['paired_controls']
    cross=load('phase158-adjacent-crosschecks.json')['replays'];assert len(cross)==7
    for row in cross:
        args,n,size=controls.build(row['case']);d=load(row['file']);r=d['result']
        assert row['sha256']==sha((ROOT/row['file']).read_bytes()) and d['input']==args and (n,size)==(row['checked_regions'],row['checked_bytes'])
        assert not d['tool_is_error'] and r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
    serial=storage.validate();assert serial==load('phase158-adjacent-storage-validation.json') and serial['mode2_record_count']==0
    search=load('phase158-effect-bounded-argument-leads.json');oldsearch=load('phase143-effect-misc-argument-leads.json')
    assert search['constant_domain_sha256']==sha((ROOT/'constant_branch_domain.py').read_bytes()) and search['ABI_evidence_sha256']==sha((ROOT/'phase158-effect-call-ABI.json').read_bytes())
    assert len(abi.evidence())==52
    key=lambda h:(h['root']['address'],h['field'],h['read']['address'],h['read']['offset'],h['read']['callee'],h['read']['call'])
    old={key(h) for h in oldsearch['candidates']};new={key(h) for h in search['candidates']}
    assert len(old-new)==9 and new-old=={('0x4376e0','controlFlags','0x4378c2',95,None,None)}
    assert not any(h['field'] in ['Misc2','Misc3'] for h in search['candidates'])
    prior=load('phase158-prior-components.json');assert len(prior)==136
    for row in prior.values():assert sha((ROOT/row['file']).read_bytes())==row['sha256']
    preservation=load('bounded-crypt-search-semantics.json')['preservation']
    for row in preservation['source_files']+preservation['executable_paths']:assert sha(Path(row['file']).read_bytes())==row['sha256']
    replays=manifest['replays']
    return dict(phase=158,date='2026-10-31',families=['Effects','EffectElements'],profile=attest['profile'],primary=attest,
        native_mode2=dict(callback=0x4376E0,legacy_name='ADJACENT_EXPLOSION',native_name_confirmed=False,
            parent='PreparedCEffect; currentdefinition pointerandargumentdefinition explicitlysame. Realparentrawdefinitionloadrun; parentconstructor/factory notrun.',
            eligible_positions='Forpositiveparentlifetime, evaluate4 centers aroundinputXYZ: [X,Y+4096,Z],[X,Y-4096,Z],[X+4096,Y,Z],[X-4096,Y,Z]. Eachpassesnative447A70beforegeneration.',
            coordinates='Q12; Z unchanged forzeroorigin/height ranges ofdiagnosticparent. Arithmeticextremes/nonzerojitterranges notexecuted.',
            count='rawF3unsignedBYTE -> definition8A. N batches per eligiblecell; all4approvedinpositive-lifefixtures. Count0stillchecksneighborsbutcreatesnothing.',
            slots='Normal selection reads eight physical WORDs GenerateIds[7..0], skipping0, preservingduplicates. No F3/4 batchdivision here. Last4slotsareused.',
            target='Mask40 is readfromcurrentinstance definition5F andchoosesEffects. Withitclear, EffectElements arecreated evenif80clear. BothC0chooseEffects.',
            random='Argumentdefinitionflags400 routesselectionthroughnative435E30. Tested all8slotsID1 withN2:oneID1requestperbatch/cell. No broaderprobability/zerogaprule assigned.',
            quality='Nativecodeandtwofullmemorypairedscenes: scale50versus255withflags4040 leaveoutputsidenticalexceptinputglobal69752C. No overlappingglobalreadin16paths.',
            static_return='Original437FAF returns1 onpositivelifebranch, includingcount0;437FCF returns0 onnonpositive-lifebranch. Nativebeforecallresultsrecordnormalreturn, notthisEAXvalue.'),
        creation=dict(bridge_chain='Effects:43A8E0->440420->440350,originalCBridgevtable66CC94. Elements:4352E0->native435E70/435F20.',
            real_factories='123Effects and8EffectElements registered across16independentfixtures; all nativepools/tags/definitions/positions/listlinks validated.',
            next_chain='OriginalchildID1Next143 andID2Next221 loaded. BasicfourID1wrapperrequestsproduce8Effects (1,143 percell). Sparse7..0slots[4,3,1]produce4,3,1,143 percell.',
            rejection='tileflags0doesNOTstop447A70intheseproviders. Four43A8E0/440420wrapperrequestsoccur;440420rejectsbefore440350. Thus0actualfactorycalls/0children. Wrapperrequestandfactoryentryareseparatecounts.',
            partial_pool='1080-bytearena:4ID1wrapper/factoryrequests,3registeredEffects[1,143,1],free0;remainingcreationrefused. No rollbackofexistinginstances. Used/freepartitionandguardschecked.',
            runtime_Element_fields=dict(id_WORD=0x101,definition_DWORD=0x103,ownerTag_WORD=0x107,parentTag_WORD=0x111,position_s32x3=0xD6,tag_WORD=0xF2),
            field_binding='4352E0passespreparedparentownerWORD12D=7 andparenttagWORDF2=2;435F20storesowner7atElement107andparenttag2at111. Thoseareruntimenodeoffsets,notKWDoffsets.'),
        opaque_review=dict(raw_WORDs=[0xEE,0xF0],intermediate_WORDs=[0x1A2,0x1A4],definition_WORDs=[0x4B,0x4D],
            conclusion='Gameplaymeaningsremainunresolved.16fullmode2pathsdonotreaddefinitionwords. Parent/childopaquechangesaffectonlyraw/MID/definitioncopiesin2wholememorypairs.',
            global_nonuse_proved=False),
        search_review=dict(evidence=link('phase158-effect-bounded-argument-leads.json'),ABI_evidence=link('phase158-effect-call-ABI.json'),
            changes='Separate integerstacklocals,provenCMP/DECbranches,andretainstack/frameanchorsatbyte-attestedcalleeRETcleanups. Unknownargcountinvalidatesstackvalues;unknowncallee/indirectcallresetsknowledge.',
            assumptions='OrdinarynativeABI;6indirectsitesboundtooriginalEffect/CBridgevtablesasinspecificacceptedfixtures. Notarbitrarydispatchproof. Otherpointeroriginsremainhints.',
            producer_roots=len(search['producer_roots']),root_states=search['root_states'],limited_roots=search['limited_roots'],callee_contexts=search['callee_summaries'],limited_callee_contexts=search['limited_callees'],
            removed_opaque_false_chains=6,removed_control_false_chains=3,new_control_flag_read=0x4378C2,
            native_confirmation='4378C2readsexactParentDef+5FDWORD60timesacross16cases;4perpositive-lifesceneincludingcount0andtileflags0.',
            global_nonuse_proved=False),
        native_executions=manifest,independent_Ghidra_controls=cross,storage_validation=serial,preservation=preservation,effective_effect_layout=effective_layout(),
        counts=dict(native_scenarios=16,parent_and_child_Effect_raw_loads=112,Element_raw_loads=2,complete_mode2_calls=16,
            bridge_Effect_wrapper_requests=sum(r['outcome']['bridge_wrapper_requests'] for r in replays),actual_Effect_factory_calls=sum(r['outcome']['factory_calls'] for r in replays),
            actual_Element_factory_calls=8,registered_Effects=sum(r['outcome']['registered_children'] for r in replays if r['outcome']['child_kind']=='Effects'),
            registered_Elements=sum(r['outcome']['registered_children'] for r in replays if r['outcome']['child_kind']=='EffectElements'),
            independent_full_Ghidra_controls=7,Ghidra_compared_regions=sum(r['checked_regions'] for r in cross),Ghidra_compared_bytes=sum(r['checked_bytes'] for r in cross),
            whole_memory_pairs=len(pairs),pair_compared_bytes=sum(r['compared_bytes'] for r in pairs),original_code_extents=len(attest['functions']),unique_executed_original_PCs=attest['unique_executed_original_PCs'],primary_data_blocks=len(attest['blocks']),
            exact_container_edits=serial['exact_container_edits'],source_Effect_records=serial['records'],source_mode2_records=serial['mode2_record_count'],newly_named_opaque_fields=0,
            preserved_source_files=len(preservation['source_files']),preserved_executable_paths=len(preservation['executable_paths']),source_writes=0),
        helpers=[link(n) for n in ['probe_effect_adjacent.py','emulate_effect_adjacent.py','crosscheck_effect_adjacent.py','prepare_effect_adjacent_primary.py','validate_effect_adjacent_storage.py','effect_call_abi.py','scan_effect_misc_bounded.py']],
        limitations=['30207Effectsrecords/82sourcescontain0mode2records. NativeparentsarediagnosticcopiesoforiginalID1withspecifiedfieldsmodifiedinmemory,notoriginalmode2mapexecution.',
            'Preparedparent/Worldbridge/connectedengineproviders; realchilddefinitions/factories. No parentfactory,fullWorldscheduler,realassets/GPU,subsequentchildticks/deletion.',
            'Visibility1/obstruction0/floor0/tileflags20or0/lightdeclinedproviders. UniformQ16table6C1488inherited;notactualnativeanglecoefficients. Zeroparentorigin/height/jitterrangesonly.',
            'Nonrandomslottestsplusall-equalrandomcontrol;generalrandomdistribution,nonzerojitter,velocityinheritance/rotationflagsnotnewlyvalidated.',
            'Search7root/1calleecontextlimitsremain. SixindirectsiteABIbindingsscopedtovalidatedfixturevtables;unsupported/indirect/otherprofilepathsnotexhausted.',
            'Miscee/f0meaningsremainopen. Physicalwidths/codec/old136componentspreserved;onlyeffectiveEffectsannotationsextended. Full15familygoalunfinished.'])

def main():
    s=build();(ROOT/'effect-adjacent-semantics.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8')
    p=load('editor-format-package.json');old=load('phase158-prior-components.json');assert set(p['components']) in [set(old),set(old)|{'effect_adjacent_semantics'}]
    for k,row in old.items():assert p['components'][k]==row
    p['effect_adjacent_semantics']=s;p['components']['effect_adjacent_semantics']=link('effect-adjacent-semantics.json');p['effective_catalogue_layouts']['Effects']=s['effective_effect_layout']
    note='effect_adjacent_semantics phase158 confirmsmode2fourcardinalQ12neighbors;NrawF3batchespereligiblecell,8slots7..0normally,40selectsEffectselseElementswithout80.4000qualityscalenotreadselectedpaths.16native/112Effect+2Elementloads,72Effectwrapperrequests/68actualfactoryentries,123Effects+8Elementscreated;tileflags0rejectsafterwrapperbeforenativefactory.7wholeGhidra/301regions/1235346bytes;51extents/2807PC/4blocks.328exactstorageedits;30207corpusEffectsinclude0mode2records:preparedparentdiagnostics. EE/F0stillunresolved. EffectiveEffectsannotationsonlyextended;prior136schemas/420sources/7EXEpreserved.'
    if note not in p['precedence']:p['precedence'].append(note)
    (ROOT/'editor-format-package.json').write_text(json.dumps(p,ensure_ascii=False,indent=2),encoding='utf8');print(s['counts'])
if __name__=='__main__':main()
