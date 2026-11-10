"""Publish original raw preference/idle AI -> Job28 -> living Crypt ritual."""
from copy import deepcopy
import json
import emulate_crypt_job_selection as native
import crosscheck_crypt_job_selection as cross
import prepare_crypt_job_primary as primary
import crypt_job_preference_evidence as storage
import native_crypt_job_preference as api
ROOT=native.ROOT
KEY='crypt_job_selection_semantics'
SCHEMA='crypt-job-selection-semantics.json'
sha=native.sha
load=lambda n:json.loads((ROOT/n).read_text(encoding='utf-8'))
dump=lambda n,d:(ROOT/n).write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
link=lambda n:dict(file=n,sha256=sha((ROOT/n).read_bytes()))
canonical=lambda d:sha(json.dumps(d,sort_keys=True,ensure_ascii=False).encode())

def build():
    m=load('phase188-crypt-job-emulations.json');assert len(m['replays'])==len(native.cases())==12
    traces=[];by={}
    for c,row in zip(native.cases(),m['replays']):
        a,ch,s,f=native.execute(c);post,out=native.verify(c,a,ch,s['result'])
        assert row==dict(case=c,file=str(f.relative_to(ROOT)),sha256=sha(f.read_bytes()),outcome=out)
        traces.append(s['result']);by[c['name']]=(a,ch,s['result'],post)
    controls=load('phase188-crypt-job-crosschecks.json')['replays'];assert [r['case'] for r in controls]==cross.CONTROLS
    for row in controls:
        a,n,size=cross.build(row['case']);saved=load(row['file']);r=saved['result']
        assert saved['input']==a and (n,size)==(row['checked_regions'],row['checked_bytes']) and row['sha256']==link(row['file'])['sha256']
        assert not saved['tool_is_error'] and r['success'] and r['hit_return']
        assert all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
    proof=primary.build();assert proof==load('phase188-crypt-job-primary.json')
    physical=dict(corpus=storage.corpus(),storage=storage.storage());assert physical==load('phase188-preference-storage.json')
    # Distinguish fallback from the possibly-zero random weighted choice.
    zero=by['preference-zero-weight'][2]['visited_instructions']
    assert zero.get('0x478476',0)>0 and zero.get('0x47844e',0)==0
    normal=by['preference-job28'][2]['visited_instructions'];assert normal.get('0x47844e',0)>0
    assert native.p.pe_read(native.EXE.read_bytes(),0x483AC0,12)==bytes.fromhex('6a006812010000e86432ffff')
    baseline=load('phase165-source-baseline.json')
    for ref in baseline['source_files']+baseline['executable_paths']:assert sha(native.Path(ref['file']).read_bytes())==ref['sha256'],ref['file']
    old=load('phase188-prior-package-state.json')
    for ref in old['components'].values():assert link(ref['file'])==ref
    visits=lambda a:sum(r['visited_instructions'].get(hex(a),0) for r in traces)
    counts=dict(native_scenes=12,independent_Ghidra_controls=9,checkpoint_snapshots=72,
        unchanged_source_scenes=4,unchanged_source_Crypt_scenes=2,source_Creature_definition_loaders=visits(0x492320),
        original_AI_dispatches=visits(0x476FC0),happy_selectors=visits(0x4780E0),unhappy_selectors=visits(0x4782C0),
        job_dispatches=visits(0x478800),Job28_handlers=visits(0x483AC0),Crypt_activation_callbacks=visits(0x483870),Crypt_effect_callbacks=visits(0x499CA0),
        Ghidra_compared_regions=sum(r['checked_regions'] for r in controls),Ghidra_compared_bytes=sum(r['checked_bytes'] for r in controls),
        additional_primary_extents=len(proof['functions']),inherited_primary_extents=len(proof['inherited']),unique_executed_original_PCs=proof['unique_executed_original_PCs'],
        current_corpus_files=82,current_corpus_record_copies=3734,source_Job28_occurrences=3,inverse_preference_container_edits=30,
        prior_components_preserved=165,source_files_preserved=420,EXE_paths_preserved=7,new_raw_fields=0,source_writes=0,
        Crypt_passive_counter_meanings_assigned=8,Crypt_active_groups_assigned=10,Crypt_counter_meanings_remaining=10)
    return dict(phase=188,status='verified_selected_raw_preferences_and_idle_AI_choose_Job28_Crypt',counts=counts,
        profile=dict(file=str(native.EXE),sha256=sha(native.EXE.read_bytes())),
        JobType28=dict(name='Crypt',dispatcher=0x478800,handler=0x483AC0,requested_state=274,category=11,
            proof='Original478800case28calls483AC0;originalhandlerrequests274via476D30. Twelvecheckedscenesneverhostrequest274/register101/activate80.'),
        physical_preferences=dict(happy=dict(raw_base=0xE14,count=3,stride=12,runtime_base=0x628,runtime_count_offset=0x78B,selector=0x4780E0),
            unhappy=dict(raw_base=0xE38,count=2,stride=12,runtime_base=0x63C,runtime_count_offset=0x78C,selector=0x4782C0),
            fields=dict(job=dict(offset=0,raw_bytes=4,loaded_bytes=1,transform='low8bits; raw11CselectsJob28withoutnormalizingrawDWORD'),
                weight=dict(offset=8,raw_bytes=1,loaded_bytes=1),mood=dict(offset=4,raw_bytes=2,interpretation='Preservedbits; nonzerogameplayeffectnotassignedhere'),
                mana=dict(offset=6,raw_bytes=2,interpretation='Preservedbits; nonzerogameplayeffectnotassignedhere'),reserved=dict(offset=9,raw_bytes=3,preserve=True)),
            raw_field_status='ExistingphysicalJobPreferenceentries; newvalidatedJob28consumerandboundedselectionsemantics,noadditionalrawfield.'),
        weighted_selection=dict(range_limit=100,weighted_endpoints='Originalcomparisonsareinclusive; storedweightsarenotautomaticallyexactindependentpercentages.',
            fallback='Afterweightedattempts,tryeachnonzerojobwithoutrequiringpositiveweight. Inweight0sceneactual478476fallbackCALLexecutesand47844eweightedCALLdoesnot.',
            cooldown='HappycontextFDandUnhappyF9last-choiceclocks; selectorscompareelapsedwith5*simulationTPS. Explicitinitialclock10avoidszero/never-chosenalias; naturalclock/schedulerfrequencyunproved.'),
        controls=dict(job0='BothUnhappyjobs0->noselectedCrypt; ownercounter0/action0.',owner_bit4='OwnerBYTE76bit04blocksdirectJob28in478800; only10/16exceptionbranchesvisibleinoriginaldispatcher, notexecutedhere.',
            hate28='SourceHate1rawE74=28loadsbutselectedpreferenceJob28stillruns;494480isnevercalledinthesescenes. NotaglobalclaimaboutHateJobs.',
            highbits='RawfirstUnhappyjob11C->loadedBYTE1C->Job28; highrawbitsremainavailable/preserved.',
            zero_return='Actual476FC0returns0evenwhenmemoryshowsvalid274andlater80;returnalonenotafailureindicator.'),
        selected_chains=dict(Warlock='RawUnhappyjob28/weight100->native568E30/492320->4782C0->478800->483AC0->274->AI6->275/80->twoHPrequests900->905->explicitexit.',
            full_AI_Warlock='Explicitcontextmode1andordinary476FC0choose28fromrawpreferencewithoutdirectselector/state274call. Moodmodechoiceitselfisapreparedcondition.',
            unchanged_Firefly='ExactFrontEnd3DLevelGlobalsrecord32 [Unused]Firefly; Happyjobs28(weight55)/24(weight45). Seeds0/2select24. Seed17selects28via4780E0; full476FC0fromstate1/defaultmode0reaches89thenHappyselection28andritual80.',
            source_effect='UnchangedFirefly32common-group80createsactualRatObject11; noHPchange. ExplicitexitkeepsCreaturealive; naturaltermination/pay/experienceunproved.'),
        room_job_distinction=dict(generic_room_table_JobID=0,preference_dispatch_JobID=28,
            scope='Differentroutes. Phase183fixedRoom26tableJob0governsgeneric4E8DA0/HateJobs; itdoesnotmeanCryptlacksajob. Preferencecase28directlyrequeststheCryptstate.'),
        fixture=dict(source='ActualsourceCreature/Objects/cataloguesandoriginalloaders; declaredmap/currentposition/NAV/nullrenderer/RNG/clockandAIcallorder.',
            lairs='SourceObjects21/27loadedbyoriginalconstructors/converterswiththesorted9/11/21/27/142/143/144catalogue. Original46F390looksattheactualLairdefinitionbeforeUnhappyselection.',
            singletons='Explicitalready-initializedzero-jobfallbacksingletons6E4164/6E4170and6E415C/6E4134; CRTstartup/shutdownnotreproduced.',
            excluded='OldexploratoryCRTregistrationpathandnullLairdefinitionreadsat114areretainedbutexcluded. Accepted12allowonlyactualDWORD FS:[0]under4096;nogame/vtablesubstitution.'),
        corpus=physical['corpus'],storage=physical['storage'],physical_API=link('native_crypt_job_preference.py'),
        native_executions=m,independent_Ghidra_controls=controls,primary=proof,preservation=link('phase165-source-baseline.json'),
        helpers=[link(n) for n in ['probe_crypt_job_selection.py','probe_crypt_source_job.py','emulate_crypt_job_selection.py','crosscheck_crypt_job_selection.py',
            'prepare_crypt_job_primary.py','crypt_job_preference_evidence.py','native_crypt_job_preference.py','crosscheck_crypt_waiting.py']],
        priority=dict(current='Crypt groups',block_complete=False,next=['Realconsumersof10unassignedCryptownercounters.',
            'Naturalend/interruption/pay/experienceandnormalWorldscheduler; selectedjobentrynowproved.', 'Othermood/needcontextsandothergameversions.']),
        limitations=['OrdinaryAIpathprovedonlyinpreparedmap/catalogue/clock/RNGscenes; notclaimallcreaturesautonomouslychooseCrypt.',
            'Corpusdefinitionsarenotproofsofactualmapplacement/liveexecution; FrontEndsourceisunmodifiedbutsceneisprepared.',
            'Onefallbackcontrolandselectedweights; exactdistributionallseeds/invalidpreferencesnotproved.',
            'Tenunknowncounterreadersandbroader15familygoalremainopen; thisdoesnotestablishsalary/experienceormandatorysacrifice.'])

NOTE='crypt_job_selection_semantics phase188:12native/9Ghidra/72snapshots. Realjobdispatcher28->483AC0->Crypt274;rawHappy/Unhappypreferencesselectit,includingunchangedFirefly32fromFrontEndviaordinaryidleAI/RNG17. Weight0fallbackverified;Hate28notconsultedonselectedroute;owner76bit04blocks. RoomtableJob0andpreferenceJob28aredifferent.30inversecontaineredits/API;165priorcomponents/source/codec preserved;10counterreadersopen,goalactive.'

def main():
    p=load('editor-format-package.json');f=ROOT/'phase188-prior-package-state.json'
    if not f.exists():
        assert len(p['components'])==165
        dump(f.name,dict(components=deepcopy(p['components']),codec=link('dk2_formats.py'),package_sha256=sha((ROOT/'editor-format-package.json').read_bytes()),
            preserved_top_level={k:canonical(v) for k,v in p.items() if k not in ['components','precedence',KEY]}))
    old=load(f.name);s=build();dump(SCHEMA,s)
    assert set(p['components']) in [set(old['components']),set(old['components'])|{KEY}]
    for k,v in old['components'].items():assert p['components'][k]==v
    for k,h in old['preserved_top_level'].items():assert canonical(p[k])==h
    p[KEY]=s;p['components'][KEY]=link(SCHEMA)
    if NOTE not in p['precedence']:p['precedence'].append(NOTE)
    dump('editor-format-package.json',p)
    dump('phase188-search-status.json',dict(phase=188,goal_status='active',current_block='Crypt groups',block_complete=False,
        previous_goal_turn=dict(classification='progress',reason='Phase187published/auditedlivingwaitingandKingcrowd; nextactionwasordinaryjobselection.'),
        current_turn=dict(classification='progress',reason='OriginalJob28dispatchandraw/sourceidleAIentryverified;30pointAPIeditsand12native/9Ghidrapublished.'),
        counts=s['counts'],remaining=s['priority']['next'],evidence=[link(SCHEMA)]))
    print(json.dumps(s['counts']))

if __name__=='__main__':main()
