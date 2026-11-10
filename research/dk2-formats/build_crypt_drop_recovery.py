"""Publish uninterrupted native release/recovery/Crypt with bounded scheduling."""
from pathlib import Path
from copy import deepcopy
import json
import emulate_crypt_drop_recovery as native
import crosscheck_crypt_drop_recovery as cross
import prepare_crypt_drop_recovery_primary as primary
import build_recovery_step as prior_storage
ROOT=Path(__file__).resolve().parent
sha=native.sha
KEY='crypt_drop_recovery_semantics'
SCHEMA='crypt-drop-recovery-semantics.json'
load=lambda n:json.loads((ROOT/n).read_text(encoding='utf-8'))
dump=lambda n,d:(ROOT/n).write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
link=lambda n:dict(file=n,sha256=sha((ROOT/n).read_bytes()))
canonical=lambda d:sha(json.dumps(d,sort_keys=True,ensure_ascii=False).encode())

def build():
    manifest=load('phase185-crypt-drop-recovery-emulations.json');assert len(manifest['replays'])==len(native.cases())==9
    traces=[]
    for c,row in zip(native.cases(),manifest['replays']):
        a,ch,s,file=native.execute(c);_,out=native.verify(c,a,ch,s['result'])
        assert row['case']==c and row['outcome']==out and row['file']==str(file.relative_to(ROOT)) and row['sha256']==sha(file.read_bytes())
        traces.append(s['result'])
    controls=load('phase185-crypt-drop-recovery-crosschecks.json')['replays'];assert [r['case'] for r in controls]==cross.CONTROLS
    for row in controls:
        a,n,size=cross.build(row['case']);saved=load(row['file']);r=saved['result']
        assert saved['input']==a and (n,size)==(row['checked_regions'],row['checked_bytes']) and row['sha256']==sha((ROOT/row['file']).read_bytes())
        assert not saved['tool_is_error'] and r['success'] and r['hit_return']
        assert all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
    proof=primary.build();assert proof==load('phase185-crypt-drop-recovery-primary.json')
    baseline=load('phase165-source-baseline.json')
    for ref in baseline['source_files']+baseline['executable_paths']:assert sha(Path(ref['file']).read_bytes())==ref['sha256'],ref['file']
    old=load('phase185-prior-package-state.json')
    for ref in old['components'].values():assert sha((ROOT/ref['file']).read_bytes())==ref['sha256']
    visit=lambda a:sum(r['visited_instructions'].get(hex(a),0) for r in traces)
    counts=dict(native_scenes=9,independent_Ghidra_controls=6,checkpoint_snapshots=sum(len(r['outcome']['checkpoints']) for r in manifest['replays']),
        native_hand_drops=visit(0x4BC710),drop_callbacks=visit(0x46EAB0),stun_callbacks=visit(0x4810E0),recovery_callbacks=visit(0x481200),
        original_RenderInfo_updates=visit(0x4B3C10),original_action_ticks=visit(0x496AA0),whole_Creature_ticks=visit(0x488930),
        Crypt_registration_callbacks=visit(0x483790),Crypt_action_callbacks=visit(0x499CA0),HP_requests=sum(len(r['hp_requests']) for r in traces),
        Ghidra_compared_regions=sum(r['checked_regions'] for r in controls),Ghidra_compared_bytes=sum(r['checked_bytes'] for r in controls),
        additional_primary_extents=len(proof['functions']),inherited_primary_extents=len(proof['inherited']),unique_executed_original_PCs=proof['unique_executed_original_PCs'],
        prior_components_preserved=162,source_files_preserved=420,EXE_paths_preserved=7,new_raw_fields=0,new_raw_edits=0,source_writes=0,
        Crypt_passive_counter_meanings_assigned=8,Crypt_active_groups_assigned=10,Crypt_counter_meanings_remaining=10)
    return dict(phase=185,status='verified_uninterrupted_bounded_release_to_Crypt_chain',counts=counts,
        profile=dict(file=str(native.EXE),sha256=sha(native.EXE.read_bytes())),
        chain=dict(start='PreparedalreadyheldCreature101, actualsourceWarlock5,owner7,HP900. Native4BC710 releaseshand, re-addsactualmapmembership andrequests76. Pickupacquisitionitselfnotexecuted.',
            stun='Original476FC0/46EAB0 enters255/action52. SourceafterloaderdefinitionDWORD74C=40. Declaredclockadvances0->40;4810E0 starts53 and257. Neitherstate257nor274ishostrequested.',
            animation='Original4B3C10 updates actualActor+44 RenderInfo using sourceArtResource metadata: incrementDWORDDC120, progressDWORDD8.34updates=4080;35=4096/endflag2,stillincrement120;36processesendandsetsincrement0.',
            action='Original496AA0after36RenderInfoupdates clears53. Withoutit,257/action53remains despiteanimationincrement0. At35updates actioncountfalls2->1but53remains.',
            recovery='NextoriginalAI481200 applies raw128B/source1490 facing-relativeXYZslot2Y; position22528/26624/3584->24018/26624/4096. Generic4E8DA0 chooses274fromactualCrypttable; nextAIregisters101andstate6.',
            full='Threecompositionscontinuethesamememorywith72wholeCreatureticks,nativeforces/speedsetter256/clock41..112. Arrive30644/26617/4096,state275/action80,HP910. Originalcategoryexitclearscounter/member; onefulltickat212enters89andheals915.',
            clear='Matchedoriginal4966A0afterexit: lastfulltickkeepsaction0/HP910. Participantneverconsumed;Bell142survives.',
            residual='Startingfullupdatesafter35RenderInfoupdateswhile53stillactivealsorecoversandcompletesritual. Thus thefullCreaturetickreallyprocessesresidualanimation/action; hostdoesnotsettheresult.'),
        interpretation=dict(solved='Previouslyseparateheld/drop andrecovery257fixturesare nowconnectedbyoriginaltimedstun/animation/actionfunctions inonecontrolledscene,then actualwalk/ritual/exit.',
            not_solved='Notliverenderedframes ornormalWorldscheduler; neednotinterpret36callsas36videoframes or1.8seconds. NoautomaticchoiceofCryptfromneeds/jobrankingproved.',
            field_correction='ActorDWORDDC is a RenderInfo animation increment in the tested path, not a graphicsinstancepointer. Earlier null-descriptor scenarios yieldingDC0 are insufficient to explain sourceanimationcompletion.',
            consequence='Livingparticipation/ritual continuesafterordinaryrelease/recoverypath underpreparedconditions; not evidence ofmandatorysacrifice orallalternativeoutcomes.'),
        controls=dict(no_animation='Clock40switches257/53; withoutRenderInfoupdatesaction53doesnotcomplete.',short34='Progress4080,increment120,action53; busyrecoveryAIwaits.',
            endpoint35='Progress4096/endflag2aloneisnotcompletion; action53stillactive,count1afteractiontick.',
            complete36='FollowingRenderInfoupdatezeroesincrement; actiontickthenallows274->6.',extra40='Extraendupdatespreservecompletedresult.',
            no_action='36RenderInfoupdateswithout496AA0stillleave53; state/actioncompletion areseparatestages.'),
        fixture=dict(schedule='Explicitclock0->40andexplicitRenderInfo/action/AIcallorder. Shortscenesclockremains40duringallRenderInfocalls; thisisalogiccomposition,nottheactualWorldschedule.',
            graphics='NativefreshCreature/Objectsloaderswithdeclaredexternalresourcereplies(success,handle0). Originalmetadata/RenderInfo/AI/actioncodeexecutes. NoGPU/pixels.',
            source='RealWarlockspells1/2/14/41,actualBellfactory142,native32x32NAVandforces/source10240Btrig retained. Singletoninitializedasbefore;CRTstartup/shutdownexcluded.',
            strict='OnlyDWORD FS:[0]below4096; raws/vtables/otherowner/thirdCreature/guards,mapprevnext,alivebits,poolbalance checked. No gamecode/vtable or state/actioncallbackreplacement.',
            excluded='Exploratoryclock39 path enters pretimeoutvisual-star graphicsquery and reachesunpreparedexternalcontext/invalidfetch; excluded, not a gamecrash. Thatfirst-pretimeoutvisualbranchremainsopen.'),
        storage=dict(new_raw_edits=0,reference=link('phase184-validation.json'),rechecked=prior_storage.storage(),
            note='Noadditionalserializedfieldorcodecchange. Existing7sourceexactroundtrips and5raw128Binverseeditsrechecked; extremaarestorageonly.'),
        native_executions=manifest,independent_Ghidra_controls=controls,primary=proof,preservation=baseline,
        helpers=[link(n) for n in ['probe_crypt_drop_recovery.py','emulate_crypt_drop_recovery.py','crosscheck_crypt_drop_recovery.py','prepare_crypt_drop_recovery_primary.py']],
        priority=dict(current='Crypt groups',block_complete=False,next=['Pretimeout255visual-stars request context, ordinaryWorldschedule and longerinterruptions.',
            'OrdinaryjobselectionleadingtoCrypt, stilldistinctfromrelease-roomassignment.', 'Realreadersof10unassignedownercounters;directactiveeffectsarenotcountersemantics.']),
        limitations=['ExactprovidedDX/Warlock/preparedheldmap/clock/renderer; otherCreaturetypes/versions/notallanimations.',
            'Controlledlogicchain,notallnormalstartup/pickup/input/rendering/Worldservice.',
            'No newrawfield oradditionalCryptcountermeaning;8passive/10active roles distinct;full15familygoalactive.'])

def main():
    package=load('editor-format-package.json');file=ROOT/'phase185-prior-package-state.json'
    if not file.exists():
        assert len(package['components'])==162
        dump(file.name,dict(components=deepcopy(package['components']),codec=link('dk2_formats.py'),package_sha256=sha((ROOT/'editor-format-package.json').read_bytes()),
            preserved_top_level={k:canonical(v) for k,v in package.items() if k not in ['components','precedence',KEY]}))
    old=load(file.name);s=build();dump(SCHEMA,s)
    assert set(package['components']) in [set(old['components']),set(old['components'])|{KEY}]
    for k,v in old['components'].items():assert package['components'][k]==v
    for k,h in old['preserved_top_level'].items():assert canonical(package[k])==h
    package[KEY]=s;package['components'][KEY]=link(SCHEMA)
    note='crypt_drop_recovery_semantics phase185:9native/6Ghidra. Actualhanddrop76->255/52->source40stun->257/53->originalRenderInfo/actioncompletion->raw128Bshift->generic274->walk/ritual/exit.34/35/36andnoactioncontrols;explicitcallschedule/nullrenderer,pretimeoutvisualbranchopen.162priorcomponents/codec/source preserved;10counterreadersunknown;goalactive.'
    if note not in package['precedence']:package['precedence'].append(note)
    dump('editor-format-package.json',package)
    dump('phase185-search-status.json',dict(phase=185,goal_status='active',current_block='Crypt groups',block_complete=False,
        previous_goal_turn=dict(classification='progress',reason='Published184/API/Ghidraproof;auditwasconfirmedliveandlatersuccessful. Nextprobewasnotexecutedbecauseapprovalservicefailure.'),
        current_turn=dict(classification='progress',reason='Continuousoriginalheld/drop/stun/RenderInfo/Cryptchainverified;strictindependentcontrolsandPEbytespublished.'),
        counts=s['counts'],remaining=s['priority']['next'],evidence=[link(SCHEMA)]))
    print(json.dumps(s['counts']))

if __name__=='__main__':main()
