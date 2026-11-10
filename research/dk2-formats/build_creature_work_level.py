"""Publish confirmed CF2 level-up binding, keeping CEE initialization/job output unresolved."""
from pathlib import Path
from copy import deepcopy
import json
import emulate_creature_work_level as native
import crosscheck_creature_work_level as controls
import prepare_creature_work_level_primary as primary
import validate_creature_work_storage as storage
ROOT=Path(__file__).resolve().parent
sha=native.sha
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def link(n):return dict(file=n,sha256=sha((ROOT/n).read_bytes()))

def effective_layout():
    out=deepcopy(load('phase162-prior-effective-creatures.json'))
    for f in out['fields']:
        if f['offset']==0xCF2:
            f['legacy_semantics_before_phase162']=f['semantics'];f['semantics']='Native game48B120 adds all32rawCF2bits toActorWorkModifier3AF on eligiblelevel-up; modulo32. NativeGUIlabel Work Modifier Level Increase.'
            f['verified_work_level']=dict(status='confirmed_scoped_consumer',MID_offset=0x10E2,definition_offset=0x797,Actor_offset=0x3AF,
                callback=0x48B120,getter=0x48B330,formula='ActorWorkModifier = low32(previous + loadedCF2)',
                gates='Exactlevel10 skips; originalActorvslot28 skipswhen signedHP<=0. Testedlevels1/9/10; no arbitrarylevel-rangeclaim.',
                representation='Rawu32bits; nativeeditor displays unsignedraw/4096. ExistingjobconsumerusesQ12, but noactualjoboutput newlyexecuted.',
                recommended_UI_label='Work Modifier Level Increase',initial_default_CEE_bound=False,full_world_or_all_jobs_proved=False)
        elif f['offset']==0xCEE:f['verified_work_level']=dict(status='initial_gameplay_consumer_unresolved',MID_offset=0x10DE,definition_offset=0x793,
            native_UI_label='Work Modifier Default',observation='Rawloadedandpreserved; CEE-onlyfullmemorypair hasidenticallevel-upoutput. Static51F3B0read is definition-to-definitioncopy, notActorinitialization.',global_nonuse_proved=False)
    return out

def build():
    attest=primary.build();assert attest==load('phase162-creature-work-level-primary.json')
    manifest=load('phase162-creature-work-level-emulations.json');assert len(manifest['replays'])==11
    posts={};trace=[]
    for c,row in zip(native.cases(),manifest['replays']):
        args,ch,d,path=native.execute(c);post,out=native.verify(c,args,ch,d['result']);posts[c['name']]=post;trace.append(d['result'])
        assert row['case']==c and row['outcome']==out and row['sha256']==sha(path.read_bytes())
    pair=native.compare_default_pair(posts['quarter'],posts['default-only']);assert manifest['paired_controls']==[pair]
    cross=load('phase162-creature-work-level-crosschecks.json')['replays'];assert len(cross)==8
    for row in cross:
        args,n,size=controls.build(row['case']);d=load(row['file']);r=d['result']
        assert row['sha256']==sha((ROOT/row['file']).read_bytes()) and d['input']==args and (n,size)==(row['checked_regions'],row['checked_bytes'])
        assert not d['tool_is_error'] and r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
    serial=storage.validate();assert serial==load('phase162-creature-work-storage-validation.json')
    assert serial['source_pair_histogram']==[dict(rawCEE=0,rawCF2=0,records=3734)]
    assert sha((ROOT/'dk2_formats.py').read_bytes())==load('phase159-terrain-light-storage-validation.json')['codec']['sha256']
    search=load('phase162-creature-work-bounded-leads.json');unknown=[h for h in search['candidates'] if not h['field'].startswith('control')]
    assert len(unknown)==1 and unknown[0]['field']=='WorkModifierLevelIncrease' and unknown[0]['read']['address']=='0x48b335'
    assert search['ABI_evidence_sha256']==sha((ROOT/'phase161-catalogue-call-ABI.json').read_bytes()) and search['constant_domain_sha256']==sha((ROOT/'constant_branch_domain.py').read_bytes())
    assert len(search['producer_roots'])==739 and search['root_states']==18661 and search['limited_roots']==4 and search['limited_callees']==2
    prior=load('phase162-prior-components.json');assert len(prior)==140
    for row in prior.values():assert sha((ROOT/row['file']).read_bytes())==row['sha256']
    preservation=load('trap-creation-activation-semantics.json')['preservation']
    for row in preservation['source_files']+preservation['executable_paths']:assert sha(Path(row['file']).read_bytes())==row['sha256']
    # EarlierGUIlabel/scaling remain frozenprimaryevidence; no neweditor replay.
    ui=next(x for x in load('creature-editor-scaled.json')['fields'] if x['raw_offset']==0xCF2)
    assert ui['native_label']=='Work Modifier Level Increase' and ui['raw_to_display']['denominator']==4096
    blob=native.p.EXE.read_bytes()
    assert native.p.pe_read(blob,0x49034F,6)==bytes.fromhex('8b96af030000')
    assert native.p.pe_read(blob,0x69D74C,24).split(b'\0')[0]==b'    WorkModifier %d\n'
    assert native.p.pe_read(blob,0x69D734,24).split(b'\0')[0]==b'    WorkEnhancement %d\n'
    failed='ghidra/phase162-creature-work-level-probe-ffcbc2f36f50.json';bad=load(failed)
    assert not bad['result']['success'] and int(bad['result']['registers']['EIP'],16)==0
    visits=lambda a:sum(r['visited_instructions'].get(hex(a),0) for r in trace)
    counts=dict(native_scenarios=11,Creature_raw_loads=visits(0x492320),complete_levelup_calls=visits(0x48B120),eligible_levelups=visits(0x48B330),
        independent_full_Ghidra_controls=8,Ghidra_compared_regions=sum(r['checked_regions'] for r in cross),Ghidra_compared_bytes=sum(r['checked_bytes'] for r in cross),
        whole_memory_pairs=1,pair_compared_regions=pair['compared_regions'],pair_compared_bytes=pair['compared_bytes'],original_code_extents=len(attest['functions']),
        static_only_function_extents=2,unique_executed_original_PCs=attest['unique_executed_original_PCs'],primary_data_blocks=len(attest['blocks']),
        exact_changed_container_rebuilds=serial['exact_changed_container_rebuilds'],source_Creature_records=serial['records'],source_Creature_files=len(serial['files']),
        generic_scalar_controls=serial['generic_scalar_controls'],extended_record_helper_controls=serial['extended_record_helper_controls'],
        search_roots=len(search['producer_roots']),search_states=search['root_states'],search_callee_contexts=search['callee_summaries'],search_limited_roots=search['limited_roots'],search_limited_callees=search['limited_callees'],
        newly_confirmed_gameplay_field_bindings=1,preserved_source_files=len(preservation['source_files']),preserved_executable_paths=len(preservation['executable_paths']),source_writes=0)
    return dict(phase=162,date='2026-11-01',family='Creatures',profile=attest['profile'],primary=attest,
        field=dict(raw_offset=0xCF2,raw_bytes=4,MID_offset=0x10E2,definition_offset=0x797,Actor_offset=0x3AF,
            native_UI_label='Work Modifier Level Increase',legacy_name='Unkcf2',storage='u32bits, preserveall32',
            UI_units='Unsignedraw/4096 fromfrozenphase18nativeeditor; not a newGUIexecution.',
            consumer='48B120 readsactualloadeddefinition via48B330 andaddsDWORD toActor3AF. Existingnativeformatter490240 labels3AFWorkModifier,3B3WorkEnhancement; keepseparate.'),
        level_up=dict(callback=0x48B120,getter=0x48B330,
            rule='IfBYTElevel403==10 ororiginalActorvslot28(4B5C30) reports signedHP40<=0, returncurrentlevelwithnoincrement. Otherwiselevel++ andActor3AF=low32(old3AF+loadeddefinition797).',
            HP='OriginalQ12ratio/helpers and4B2CE0 execute. FixturebaseHP1000; growth100/150/.../550%, actorfullHPbefore.1->2 gives1000->1500,9->10 gives5000->5500.',
            examples=[dict(before=4096,increment=1024,after=5120),dict(before=4096,increment=4096,after=8192),
                dict(before=4096,increment=0xFFFFFFFF,after=4095),dict(before=0xFFFFFFFF,increment=1,after=0)],
            no_saturation='NativeADD wrapsmodulo32; no clamp ofmodifier inthiscallback. UIunsigneddisplaydoesnotprove signedrangeofalllaterconsumers.',
            effects='FullWorld38C/50E490 forwardslevelupvisualrequest153hextoexternalengine90,explicitlydeclined. Native sound20providerreturns13572468; actualActor2E andtimestamps38/3C updated.',
            scheduler='Directfullcallbackafterrealrawdefinitionload; no ordinaryXPthreshold/training tick, actualCreaturefactory or fullAI scheduler newlyexecuted.'),
        source_and_fixture=dict(source='OriginalstandaloneVampireID8 5449bytereCord; onlyCF2/CEE/HPpatchedinmemory. Allsourcefilesunchanged.',
            actor='Prepared1343-byte Actor fromverifiedphase90nativefactorysnapshot; original66D524vtable,HP1000/originalfullHPwithselectedlevel,WorkModifier4096explicitinput. Actorfactorynotrerun.',
            definition='Native491DC0/568820/568E30/492320;6457byteMID/2199bytedefinition,checksum=sumMID andCF2/CEEcopiedexactly.',
            raw_corpus='3734records/82containers:CEE0andCF20throughout. Newmechanicaleffectsarediagnosticcopies,notobservedvanillaconfiggrowth.'),
        default_CEE_review=dict(raw=0xCEE,MID=0x10DE,definition=0x793,status='initial_gameplay_consumer_unresolved',
            static_read='51F3B0 copiesdefinition793/797toanotherdefinition. It doesnotinitializeActorWorkModifier; earlierliteralreadtriagewasnotActorassignmentproof.',
            pair=pair,global_nonuse_proved=False),
        job_consumer_boundary=dict(static_function=0x496E00,
            observed='Originalcodeuses43D750 toaddActor3AF+3B3, then4363F0Q12math beforetileoperation4508D0. Fullfunctionbytesattestedbutnotexecutedinthisphase.',
            actual_job_output_newly_executed=False,claim='No claimof+25percenttoeveryjob,researchexperience/manufacture/etc fromlevel-upadditiononly.'),
        discovery_review=dict(search=link('phase162-creature-work-bounded-leads.json'),helper=link('scan_creature_work_bounded.py'),
            result='One newunknown-shapedhint,48B335,confirmedthroughentirelevelup. Fourroot/twocalleelimitsremain;rawEF0,Jump,Prioritynotresolved.',
            failed_initial_fixture=dict(**link(failed),excluded=True,reason='Connectedenginevslot90notprovided; originalWorldforwarder calledNULL. Currentproviderexplicitdecline; no game/formatbugproof.')),
        native_executions=manifest,independent_Ghidra_controls=cross,storage_validation=serial,preservation=preservation,effective_creature_layout=effective_layout(),counts=counts,
        helpers=[link(n) for n in ['probe_creature_work_level.py','emulate_creature_work_level.py','crosscheck_creature_work_level.py','prepare_creature_work_level_primary.py',
            'native_creature_work_level.py','validate_creature_work_storage.py']],
        limitations=['PreparedActor/World/player/map andgrowthtable; realrawdefinitionloadandwholelevel-up. No newActorfactory/Worldbootstrap/normalXPthresholdcaller.',
            'Levelupvisualsdeclinedthroughdeclaredexternalengine90; soundlookup0/playtokenproviders. Noassets/GPU/pixels/audio.',
            'RawCF2completeincrementconsumerconfirmed; actualjoboutput/tick andallmodifierconsumersnotexecuted. CEEinitialconsumerunresolved;3B3WorkEnhancementseparate.',
            'Onlylevels1/9/10andHP0gate executed; exactlevel10checkcannotbeexpandedtoalloutofrangevalues. Additionwrapis32bit; noalljobsafetyguarantee.',
            'All3734sourceCEE/CF2pairs0; diagnosticnonzeroeditsinmemoryonly. Genericcodecstillrejects5537scalarwrites; narrowhelperpreservesknownextendedrecords.',
            'Searchfourroot/twocalleelimits,untypedentity/vslothintsandunsupportedreturns/indirectsremain. EF0/Jump/Priorityandother15familyunknownsopen.',
            'Prior140components/physicalcodec/sources preserved; effectiveCreaturesannotationonlyCF2scopeandCEEboundary updated. Fullgoalunfinished.'])

def main():
    s=build();(ROOT/'creature-work-level-semantics.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8')
    p=load('editor-format-package.json');old=load('phase162-prior-components.json');key='creature_work_level_semantics'
    assert set(p['components']) in [set(old),set(old)|{key}]
    for k,row in old.items():assert p['components'][k]==row
    p[key]=s;p['components'][key]=link('creature-work-level-semantics.json');p['effective_catalogue_layouts']['Creatures']=s['effective_creature_layout']
    note='creature_work_level_semantics phase162 newlyconfirms rawCF2WorkModifierLevelIncrease->MID10E2->definition797->native48B120/48B330->Actor3AFADDmod32oneligiblelevelup.11native/11rawloads/9eligiblelevelups;8wholeGhidra/376regions/787416bytes;72extents/3296PC/2blocks. Level10orHP<=0gates;1->2and9->10/overflowverified. CEEonlypairdiffstorage/checksum,initialdefaultconsumerstillunknown;51F3B0readiscopydefinition.496E00joboutputstaticonly.246exactcontaineredits82files/3734records(allCEE/CF20),extended5537narrowhelperpreserves tails/genericcodecunchanged.1newconfirmedgameplayfieldbinding;prior140schemas/420sources/7EXEpreserved;effectiveCreaturesannotationsupdated.'
    if note not in p['precedence']:p['precedence'].append(note)
    (ROOT/'editor-format-package.json').write_text(json.dumps(p,ensure_ascii=False,indent=2),encoding='utf8');print(s['counts'])
if __name__=='__main__':main()
