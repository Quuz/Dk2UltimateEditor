"""Publish real Crypt visitors/waiting/DANCE, distinct from active registration."""
from copy import deepcopy
import json
import emulate_crypt_waiting as native
import crosscheck_crypt_waiting as cross
import prepare_crypt_waiting_primary as primary
import build_recovery_step as prior_storage
from portal_animation_tables import initialized_animations,BASE as ANIMATION_BASE
ROOT=native.ROOT
KEY='crypt_waiting_semantics'
SCHEMA='crypt-waiting-semantics.json'
sha=native.sha
load=lambda n:json.loads((ROOT/n).read_text(encoding='utf-8'))
dump=lambda n,d:(ROOT/n).write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
link=lambda n:dict(file=n,sha256=sha((ROOT/n).read_bytes()))
canonical=lambda d:sha(json.dumps(d,sort_keys=True,ensure_ascii=False).encode())

def build():
    manifest=load('phase187-crypt-waiting-emulations.json');assert len(manifest['replays'])==len(native.cases())==6
    traces=[];post_regular=None;raw_regular=None;post_crowd=None
    for c,row in zip(native.cases(),manifest['replays']):
        a,ch,s,f=native.execute(c);post,out=native.verify(c,a,ch,s['result'])
        assert row==dict(case=c,file=str(f.relative_to(ROOT)),sha256=sha(f.read_bytes()),outcome=out)
        traces.append(s['result'])
        if c['name']=='occupied-dancer':post_regular=post;raw_regular=ch[native.p.target.RAW]
        if c['name']=='King-crowd':post_crowd=post
    controls=load('phase187-crypt-waiting-crosschecks.json')['replays'];assert [r['case'] for r in controls]==cross.CONTROLS
    for row in controls:
        args,n,size=cross.build(row['case']);saved=load(row['file']);r=saved['result']
        assert saved['input']==args and (n,size)==(row['checked_regions'],row['checked_bytes']) and link(row['file'])['sha256']==row['sha256']
        assert not saved['tool_is_error'] and r['success'] and r['hit_return']
        assert all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
    proof=primary.build();assert proof==load('phase187-crypt-waiting-primary.json')
    def at(post,a,n):
        base=next(b for b,v in post.items() if b<=a and a+n<=b+len(v));return post[base][a-base:a-base+n]
    table=initialized_animations();actual=at(post_regular,ANIMATION_BASE,len(table));assert actual==table
    row76=table[76*29:77*29];assert int.from_bytes(row76[:4],'little')==0 and row76[20]==28
    assert raw_regular[0x950:0x990].split(b'\0')[0]==b'War-Dance'
    category=at(post_crowd,0x699948,64)
    assert int.from_bytes(category[4:8],'little')==0x47AFB0 and category[20:24]==bytes(4)
    assert int.from_bytes(category[36:40],'little')==0x47B920
    baseline=load('phase165-source-baseline.json')
    for ref in baseline['source_files']+baseline['executable_paths']:assert sha(native.Path(ref['file']).read_bytes())==ref['sha256'],ref['file']
    previous=load('phase187-prior-package-state.json')
    for ref in previous['components'].values():assert link(ref['file'])==ref
    visits=lambda a:sum(r['visited_instructions'].get(hex(a),0) for r in traces)
    counts=dict(native_scenes=6,independent_Ghidra_controls=5,checkpoint_snapshots=36,
        original_AI_dispatches=visits(0x476FC0),initial_Crypt_callbacks=visits(0x483790),active_position_callbacks=visits(0x483870),
        waiting_Crypt_callbacks=visits(0x4839E0),action76_starts=4,action80_starts=1,active80_effect_callbacks=visits(0x499CA0),
        native_Dancer_creator_calls=visits(0x4D9410),cleanup_calls=visits(0x4D9570),native_Dancer_deletions=visits(0x4E62D0),
        King_Dancers_created_by_AI=40,HP_requests=sum(len(r['hp_requests']) for r in traces),
        Ghidra_compared_regions=sum(r['checked_regions'] for r in controls),Ghidra_compared_bytes=sum(r['checked_bytes'] for r in controls),
        additional_primary_extents=0,inherited_primary_extents=len(proof['inherited']),unique_executed_original_PCs=proof['unique_executed_original_PCs'],
        prior_components_preserved=164,source_files_preserved=420,EXE_paths_preserved=7,new_raw_fields=0,new_raw_edits=0,source_writes=0,
        Crypt_passive_counter_meanings_assigned=8,Crypt_active_groups_assigned=10,Crypt_counter_meanings_remaining=10)
    assert counts['original_AI_dispatches']==19 and counts['waiting_Crypt_callbacks']==11 and counts['native_Dancer_deletions']==41
    return dict(phase=187,status='verified_bounded_living_Crypt_waiting_and_AI_created_King_crowd',counts=counts,
        profile=dict(file=str(native.EXE),sha256=sha(native.EXE.read_bytes())),
        roles=dict(active='RoomWORD53storesoneactualregisteredCreaturetag; ownercounterusesitsgroup. Arrivalstate275startsaction80.',
            visitor='OccupiedroomandRoomDWORD55nonzero:274returnsaccepted276; nextoriginalAI4839E0starts76ifnotbusy. Waiterkeepsroommembershipbutdoesnotregister/addspeciescounter.',
            occupied_without_count='WithRoom53=100and55=0, original274callbackreturns0; realAIselects1andunlinksvisitor101, preservinglivingregistered100.',
            free_wait_state='Explicitprepared276withfreeslotacceptsmovement6andregisters101; notproofthatordinaryoccupantexitautomaticallyhandsoverslot.',
            exit='Originalexitofregisteredparticipantunlinksitthen4D8E60(0)/4D9570resetswaitingCreatures1andunlinksallroommembers, deletesDancer143,clearscounter/tag/count. BothCreatureHP900/registryentriespreserved.',
            residual_action='Exitstate/categorycleanupdoesnotitselfclearwaitingaction76. Selectedsnapshotsafterexitretain76; Kingaction80alsoremains. Noindefinitelivebehaviorclaimed.'),
        gate=dict(owner_room_type=26,registered_offset=0x53,count_offset=0x55,
            caveat='GateisnumericRoom55,notaliveObjectcount. Phase179provescreatoraddsrequestedcountevenonallocationfailure. NewwaitingpositivefixtureshaveactualDancers; nodependencyonrenderedSkeletoninteractionproved.'),
        dance=dict(action=76,initializer=0x4950C0,state276_callback=0x4839E0,activator=0x496700,table_row_hex=row76.hex(),
            callback=0,resource_selector=28,raw_ArtResource_offset=0x950,source_Warlock_resource_name='War-Dance',
            field_status='ExistingAnimation.DANCEstorage; newconfirmedconsumer,nofanewfield. Metadata/actionselected;modelsandpixelsnotrendered.'),
        King_crowd=dict(participant_species=21,visitor_species=8,registered_tag=101,visitor_tag=100,
            chain='ExplicitKing274->originalregistration6->AI275/80->original4D9410(40), then generic4E8DA0(visitor,0)->274->276->76; originalKingexitclearsbothmembersand40actual143Objects.',
            constraints='InitialKingentry/placement/sourceWorld/NAV/nullrendererandcallbackscheduleprepared. Hostdoesnotcall4D9410orpass40inthesesceneinputs. Visitorentryusesactualgenericroomtable; nospontaneousjobchoiceproved.',
            counters='Onlygroup09/King1093=1; Vampirevisitor108Eremains0. Active80effectcallbackisnotexecutedintheKingcrowdscene.'),
        category11=dict(runtime_base=0x699948,row_hex=category.hex(),native_initializer=0x4798F0,entry=0x47AFB0,exit=0x47B920,per_AI_category_predicate=0,
            scope='Nativeinitializedtableandoriginal476FC0reader. Nullcategorypredicatealoneisnotproofthatneeds/otherAIcannotevertaketheCreatureout.'),
        interpretation=dict(established='CryptdistinguishesregisteredbonusparticipantandunregisteredDANCEvisitors. Selectedoriginalentry/wait/exitpathskeepbothalive; nothingisconsumedinthesescenes.',
            unknown='Notordinarysalary/experienceworkproved; notallpossiblefuturedeath/sacrificeorWorldschedulerstatesexcluded. Tenownercounterreadersremainunknown.'),
        native_executions=manifest,independent_Ghidra_controls=controls,primary=proof,
        storage=dict(new_raw_fields=0,new_raw_edits=0,rechecked=prior_storage.storage(),reference=link('phase184-validation.json')),
        preservation=link('phase165-source-baseline.json'),helpers=[link(n) for n in ['probe_crypt_waiting.py','probe_crypt_crowd.py','emulate_crypt_waiting.py','crosscheck_crypt_waiting.py','prepare_crypt_waiting_primary.py']],
        priority=dict(current='Crypt groups',block_complete=False,next=['Realreadersof10unassignedownercounters.',
            'Naturaljobselectionandtermination; ordinaryWorldscheduler/longerinterruptions.', 'Pretimeout255visualrequestcontextremainsopen.']),
        limitations=['6selectedscenes,19AIdispatches; notallCreatures/allrooms/allversions.',
            'Firstfiveusepreparedregistration100andoptionalnativeDancer1condition. SixthKingcaseletstheoriginalAIcreate40itself.',
            'NoGPUmodels/pixelsorskeletonpairingobserved; actionmetadataandobjectlistsareproved.',
            '8passivecountermeanings/10activeeffectgroupsremainseparate; noadditionalcounternamed;full15familygoalactive.'])

NOTE='crypt_waiting_semantics phase187:6native/5Ghidra/36snapshots. Actual274occupied->276->action76 selectsDANCE/War-Dance,waiteraddsnospeciescounter. AIactivatedKingcreates40Dancers;genericvisitorentrywaits;KingexitunlinksbothlivingCreaturesanddeletes40Objects.19AIdispatches,11waitingcallbacks;8passive/10active roles and10unknowncountersunchanged.164priorcomponents/source/codec preserved;goalactive.'

def main():
    p=load('editor-format-package.json');prior=ROOT/'phase187-prior-package-state.json'
    if not prior.exists():
        assert len(p['components'])==164
        dump(prior.name,dict(components=deepcopy(p['components']),codec=link('dk2_formats.py'),package_sha256=sha((ROOT/'editor-format-package.json').read_bytes()),
            preserved_top_level={k:canonical(v) for k,v in p.items() if k not in ['components','precedence',KEY]}))
    old=load(prior.name);s=build();dump(SCHEMA,s)
    assert set(p['components']) in [set(old['components']),set(old['components'])|{KEY}]
    for k,v in old['components'].items():assert p['components'][k]==v
    for k,h in old['preserved_top_level'].items():assert canonical(p[k])==h
    p[KEY]=s;p['components'][KEY]=link(SCHEMA)
    if NOTE not in p['precedence']:p['precedence'].append(NOTE)
    dump('editor-format-package.json',p)
    dump('phase187-search-status.json',dict(phase=187,goal_status='active',current_block='Crypt groups',block_complete=False,
        previous_goal_turn=dict(classification='progress',reason='Phase186published/audited;5waitingnative/4GhidracontrolsandKingcrowdnativeevidencechangednextaction.'),
        current_turn=dict(classification='progress',reason='KingcrowdincludedinstrictchecksandindependentGhidra;newlivingvisitor/action76modelpublishedwithoutassigningunknowncounters.'),
        counts=s['counts'],remaining=s['priority']['next'],evidence=[link(SCHEMA)]))
    print(json.dumps(s['counts']))

if __name__=='__main__':main()
