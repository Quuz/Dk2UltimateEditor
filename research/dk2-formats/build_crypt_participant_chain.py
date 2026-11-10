"""Publish bounded original Crypt participant entry/activation/exit chains."""
from pathlib import Path
from copy import deepcopy
import json
import emulate_crypt_participant_chain as native
import crosscheck_crypt_participant_chain as cross
import prepare_crypt_participant_primary as primary
ROOT=Path(__file__).resolve().parent
sha=native.sha
KEY='crypt_participant_chain_semantics'
SCHEMA='crypt-participant-chain-semantics.json'
load=lambda n:json.loads((ROOT/n).read_text(encoding='utf-8'))
dump=lambda n,d:(ROOT/n).write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
link=lambda n:dict(file=n,sha256=sha((ROOT/n).read_bytes()))
canonical=lambda d:sha(json.dumps(d,sort_keys=True,ensure_ascii=False).encode())

def build():
    manifest=load('phase181-crypt-chain-emulations.json');assert len(manifest['replays'])==len(native.cases())==19
    traces=[]
    for c,row in zip(native.cases(),manifest['replays']):
        args,ch,s,file=native.execute(c);_,out=native.verify(c,args,ch,s['result'])
        assert row['case']==c and row['outcome']==out and row['file']==str(file.relative_to(ROOT)) and row['sha256']==sha(file.read_bytes())
        traces.append(s['result'])
    controls=load('phase181-crypt-chain-crosschecks.json')['replays'];assert [r['case'] for r in controls]==cross.CONTROLS
    for row in controls:
        args,n,size=cross.build(row['case']);saved=load(row['file']);r=saved['result']
        assert saved['input']==args and (n,size)==(row['checked_regions'],row['checked_bytes']) and row['sha256']==sha((ROOT/row['file']).read_bytes())
        assert not saved['tool_is_error'] and r['success'] and r['hit_return']
        assert all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
    proof=primary.build();assert proof==load('phase181-crypt-chain-primary.json')
    baseline=load('phase165-source-baseline.json')
    for ref in baseline['source_files']+baseline['executable_paths']:assert sha(Path(ref['file']).read_bytes())==ref['sha256']
    prior=load('phase181-prior-package-state.json')
    for ref in prior['components'].values():assert sha((ROOT/ref['file']).read_bytes())==ref['sha256']
    visit=lambda a:sum(r['visited_instructions'].get(hex(a),0) for r in traces)
    counts=dict(native_scenes=19,independent_Ghidra_controls=6,checkpoint_snapshots=19*6,
        original_room_category_entries=visit(0x47AFB0),original_Creature_room_attaches=visit(0x4E5A70),
        Crypt_registration_setter_calls=visit(0x4D8E60),Crypt_entry_callback_calls=visit(0x483790),Crypt_bellside_callback_calls=visit(0x483870),
        original_action_activations=visit(0x496700),original_action_dispatchers=visit(0x496AA0),original_action80_callbacks=visit(0x499CA0),
        original_AI_dispatchers=visit(0x476FC0),whole_Creature_ticks=visit(0x488930),original_category_exits=visit(0x47B920),
        original_Crypt_cleanups=visit(0x4D9570),Object_deletions=visit(0x4B7B10),HP_requests=sum(len(r['hp_requests']) for r in traces),
        Ghidra_compared_regions=sum(r['checked_regions'] for r in controls),Ghidra_compared_bytes=sum(r['checked_bytes'] for r in controls),
        additional_primary_extents=len(proof['functions']),inherited_primary_extents=len(proof['inherited']),unique_executed_original_PCs=proof['unique_executed_original_PCs'],
        prior_components_preserved=158,source_files_preserved=420,EXE_paths_preserved=7,new_raw_fields=0,new_raw_edits=0,source_writes=0,
        Crypt_passive_counter_meanings_assigned=8,Crypt_active_groups_assigned=10,Crypt_counter_meanings_remaining=10)
    return dict(phase=181,date='2026-11-03',families=['Creatures','Rooms','Objects','CreatureSpells'],profile=proof['profile'],counts=counts,
        priority=dict(current_block='Crypt groups',block_complete=False,next='Normal job selection and movement completion tostate275, later cancellation/lifetime of action80, unresolved10owner-counter readers.'),
        source_chain=dict(category_entry='Directoriginal476D30 request1->275 (or274). Category11 entry47AFB0 uses actualWorldtile and room300;4E7F80/4E5A70 attach fullCreature101 natively. No prelinkedroomlist in entryscenes.',
            bell='Actual sourceObject142 rawloading/factory. Adeclared binding fixture reads its actualoutput tag and calls original4E6220, then stores centralBell reference; no predictedallocationaddress or fabricatedObject.',
            registration='Mostscenes call original4D8E60(tag101) ontheactualCreature. Additional274/483790case itself calls movementrequest471890 and setter, records101 and counter+1; reachedstate6 means requestaccepted, not arrived.',
            activation='Actual483870 checks busyaction, memberroomtag, roomtype26, nonzeroBellreference, Xtile=centerX+1,Ytile=centerY,direction600h. Whenready calls4D9410selecteddancercount then original496700(80,0,0,0). Action80 is not hostwritten.',
            tick='Explicitfixture clock0->100, original496AA0 calls499CA0. HP100->105/108, knight/king angerdelta anddancers1/40 confirmed with counterregistered in same memory.',
            exit='Original476D30(1,0) invokes category11 exit47B920, which unlinks actualCreature then calls4D8E60(0) ifparticipant matches. Counterreturns0;143dancersdestroyed;142Bell survives; participantalive.'),
        checkpoints=dict(steps=['after native roomentry and Bellbinding','after registration or itsAIrequest','after bellside AIactivation','after actiontick','after categoryexit','after optionalfollowing tick/control'],
            implementation='Read-only observation helpers copy actual actor, room, owner and recipient memory; checkpointindex is metadata. Clockhelpers explicitlywrite onlysessionclock. Helpers do not supply action/counter/HPresults.'),
        causal_controls=dict(no_bell='ActualBell remains attached, but room49=0 rejectsactivation/AIregistration. Tests referencegate rather than visualabsence.',
            facing='Wrongdirection calls original4B2C10; thisinvocationdoesnotactivate80.',
            busy='Existingaction17 returns beforeactivating80; no fakebusyreturn.',
            membership='NoCreature roomtag rejectsactivation; otherroomtype14rejectsactivation.',
            no_registration='Explicitcategory275 request, roommembership/Bellready, registerskipped:483870stillactivates80 andWarlockheals with all18owner counters0. Normal274registrationpath is a different gate.'),
        after_exit=dict(observation='Standalonecategorytransition clears registration/list/counter, but leavesaction80. Later496AA0 atclock200heals105->110, roomtag0/counters0.',
            AI_control='Oneoriginal476FC0afterexit entersstate89; action80persists andlater496AA0healsagain.',
            full_tick='With real32x32NAVbootstrap, original4D27D0/4D16B0callback and sourceCreatureSpell1/2/14/41catalogue, onecomplete488930afterexit alsoentersstate89andheals105->110. No low-pagefallback or nativefunctionsubstitution.',
            stop_control='Callingoriginal4966A0 onActor+38D afterexit clearsaction; otherwiseidenticalcomplete488930keepsHP105 andaction0.',
            classification='Boundedbehavior/potentialinterruptiongap, not a provenpersistentlivegameexploit. Normal Worldschedule/nextinterruption/longlifetime not tested; no EXE patch made.'),
        fixture=dict(initial='Creature starts explicitly atbellside7.5/6.5,direction600h onpreparedroom300tile. Roomregistry/owner8/sourceCryptdefinition/ownerlists explicit; no fullmapbootstrap.',
            raw='OriginalfullCreature/rawObjects andKeeper loaders retained. UnrelatedShotfire/collision omitted. FiveObjectdefinitions sorted byID[9,11,142,143,144] for realgetter lookup.',
            observation='Declaredmemory-copy checkpoints andclockcontrol code plus explicitexternalresource/sound/notificationIO. Allgamecode/vtablesunchanged.',
            strict='Alllower4096accesses onlyDWORD FS:[0]. Actor/source/vtables/thirdCreature/otherownercounters/guards checked; Bellactualregistry and allocatorusedbytes verified.',
            excluded='ExploratorywrongpredictedBelladdress, missingNAVfunctionpointer, missingSpelllookup27 andlongersecondtickfailedcontexts excluded. Failedtraces retained, not promoted.',
            lifetime='Only onefullpostexitCreaturetick peraccepted fulltickcase. Four-updateexplorationneedsadditionalgraphicscontext andisnotaccepted.'),
        native_executions=manifest,independent_Ghidra_controls=controls,primary=proof,preservation=baseline,
        helpers=[link(n) for n in ['probe_crypt_participant_chain.py','emulate_crypt_participant_chain.py','crosscheck_crypt_participant_chain.py','prepare_crypt_participant_primary.py']],
        limitations=['Exact providedDX, not allversions or ordinarylivegame.',
            'Category/request/readyposition explicitlyprepared; no normaljobselection or movementcompletion fromregisteredstate6to275 composed.',
            'RuntimeAI/action differences are not newKWDfields orproof ofcountermeanings. 8passive/10activegroups;10counterreadersremainunknown.',
            'No mandatoryparticipantsacrifice inacceptedchains, but fullalternativepaths/lifetime not proven.',
            'Prior158components/codec/effectiveparameters/baseline165 preserved; full15familygoalactive.'])

def main():
    package=load('editor-format-package.json');file=ROOT/'phase181-prior-package-state.json'
    if not file.exists():
        assert len(package['components'])==158
        dump(file.name,dict(components=deepcopy(package['components']),codec=link('dk2_formats.py'),package_sha256=sha((ROOT/'editor-format-package.json').read_bytes()),
             preserved_top_level={k:canonical(v) for k,v in package.items() if k not in ['components','precedence',KEY]}))
    prior=load(file.name);s=build();dump(SCHEMA,s)
    assert set(package['components']) in [set(prior['components']),set(prior['components'])|{KEY}]
    for k,v in prior['components'].items():assert package['components'][k]==v
    for k,h in prior['preserved_top_level'].items():assert canonical(package[k])==h
    package[KEY]=s;package['components'][KEY]=link(SCHEMA)
    note='crypt_participant_chain_semantics phase181:19native/6Ghidra/114checkpoints; originalcategoryentry adds actualCreature, Bellfactory->registration->483870->496700action80->effects->categoryexit/counterclear/dancerdelete. AI274acceptsmovement/registerswhileinstat6,arrivalnotproved. Exitdoesnotclearaction80; lateronefull488930healsagain,counter0; original4966A0causallystops. StrictFS0/source/guards/vtables andprior158preserved; notlivebugorfullWorldclaim; goalactive.'
    if note not in package['precedence']:package['precedence'].append(note)
    dump('editor-format-package.json',package)
    dump('phase181-search-status.json',dict(phase=181,goal_status='active',current_block='Crypt groups',block_complete=False,
        previous_goal_turn=dict(classification='progress',reason='Phase180published/audited; composed actualentry/activation/exit andnativefullpostexitCreaturetickwithcausalactionclearcontrol.'),
        counts=s['counts'],remaining=s['priority']['next'],evidence=[link(SCHEMA)]))
    print(json.dumps(s['counts']))

if __name__=='__main__':main()
