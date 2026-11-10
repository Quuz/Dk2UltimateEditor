"""Publish Crypt action80 effects while retaining unknown counter consumers."""
from pathlib import Path
from copy import deepcopy
import json
import emulate_crypt_active_action as native
import crosscheck_crypt_active_action as cross
import prepare_crypt_active_primary as primary
from validate_crypt_action_storage import validate
from native_room_counters import GROUPS
ROOT=Path(__file__).resolve().parent
sha=native.sha
KEY='crypt_active_action_semantics'
SCHEMA='crypt-active-action-semantics.json'
load=lambda n:json.loads((ROOT/n).read_text(encoding='utf-8'))
dump=lambda n,d:(ROOT/n).write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
link=lambda n:dict(file=n,sha256=sha((ROOT/n).read_bytes()))
canonical=lambda d:sha(json.dumps(d,sort_keys=True,ensure_ascii=False).encode())

def build():
    manifest=load('phase180-crypt-action-emulations.json');assert len(manifest['replays'])==len(native.cases())==59
    traces=[]
    for c,row in zip(native.cases(),manifest['replays']):
        args,ch,s,file=native.execute(c);_,out=native.verify(c,args,ch,s['result'])
        assert row['case']==c and row['outcome']==out and row['file']==str(file.relative_to(ROOT)) and row['sha256']==sha(file.read_bytes())
        traces.append(s['result'])
    controls=load('phase180-crypt-action-crosschecks.json')['replays'];assert [r['case'] for r in controls]==cross.CONTROLS
    for row in controls:
        args,n,size=cross.build(row['case']);saved=load(row['file']);r=saved['result']
        assert saved['input']==args and (n,size)==(row['checked_regions'],row['checked_bytes']) and row['sha256']==sha((ROOT/row['file']).read_bytes())
        assert not saved['tool_is_error'] and r['success'] and r['hit_return']
        assert all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
    proof=primary.build();assert proof==load('phase180-crypt-action-primary.json')
    storage=validate();assert storage==load('phase180-crypt-action-storage.json')
    baseline=load('phase165-source-baseline.json')
    for ref in baseline['source_files']+baseline['executable_paths']:assert sha(Path(ref['file']).read_bytes())==ref['sha256']
    prior=load('phase180-prior-package-state.json')
    for ref in prior['components'].values():assert sha((ROOT/ref['file']).read_bytes())==ref['sha256']
    visits=lambda a:sum(r['visited_instructions'].get(hex(a),0) for r in traces)
    groups=[]
    for slot,ids in sorted(GROUPS.items()):
        rules=[native.rule(i) for i in ids]
        assert len({r[0] for r in rules})==1
        groups.append(dict(registration_counter=slot,Creature_IDs=ids,active_action_kind=rules[0][0],
            per_species_rules=[dict(Creature_ID=i,kind=k,clock_threshold=t,parameter=v) for i,(k,t,v) in zip(ids,rules)],
            counter_read_required_for_this_action=False))
    active=[r for r in groups if r['active_action_kind']!='none'];assert len(active)==10
    counts=dict(native_scenes=59,source_Creature_species=46,independent_Ghidra_controls=6,
        whole_crypt_action_callbacks=visits(0x499CA0),whole_action_dispatchers=visits(0x496AA0),
        HP_requests=sum(len(r['hp_requests']) for r in traces),anger_calls=visits(0x478B80),money_calls=visits(0x4BA420),
        quake_queue_calls=visits(0x510290),Object_factory_calls=visits(0x4B7A90),source_variable_callbacks=visits(0x51CD90),
        Ghidra_compared_regions=sum(r['checked_regions'] for r in controls),Ghidra_compared_bytes=sum(r['checked_bytes'] for r in controls),
        fresh_primary_extents=len(proof['functions']),inherited_primary_extents=len(proof['inherited']),unique_executed_original_PCs=proof['unique_executed_original_PCs'],
        Crypt_groups_with_active_action=10,Crypt_groups_with_previously_assigned_passive_effect=8,
        Crypt_owner_counter_meanings_still_unassigned=10,new_raw_fields=0,exact_in_memory_Variables_edits=1,
        preserved_prior_components=157,preserved_source_files=420,preserved_EXE_paths=7,source_writes=0)
    return dict(phase=180,date='2026-11-03',families=['Creatures','Objects','GlobalVariables','KeeperSpells'],profile=proof['profile'],counts=counts,
        priority=dict(current_block='Crypt groups',block_complete=False,next='Compose actual participant bell-side AI/action activation and exit; distinguish10 activegroup roles from10 stillunassigned counter consumers.'),
        action=dict(ID=80,table_initializer=0x4950C0,callback=0x499CA0,dispatcher=0x496AA0,animation_selector=5,
            preparation='Source species BYTE36F and whole raw loaded Creaturedefinition; currentaction80 explicitly prepared. No normal jobselection/arrival activation claimed.',
            continuation='All59scenes return1, keepaction80, participant alive/HPnondecreased. Dispatcher controls call actual499CA0 indirectly through initializedtable.',
            timing='Active branch runs iff unsignedclock > (last+threshold)mod32; updateslast=clock before sideeffect. Sameclock repeats do not repeat reward; even Objectfactoryfailure consumes period.',
            lifetime='This proves nonconsuming selectedactionupdates, not complete participant life/exit/alternative routes.'),
        rules=dict(heal=dict(Creature_IDs=[5,34],HP_requests=[5,8],threshold=4,
                recipients='OwnerWORD6 Creaturehead then CreatureWORD10 chain; sourceparticipant and anotherlistedcreature heal, owner7Creature unchanged. Emptyhead gives noHPrequests.'),
            anger=dict(Creature_IDs=[13,37,2,21,26,29,31],parameter_type=102,runtime_address=0x6F28E8,source_signed32=-275,
                loader=0x51CD90,raw_storage='16byte Variablesrecord:DWORDtype0, DWORDvalue4;explicit16byte nativeprefix supplied to commoncallback.',
                narrowing='copy low16, read as signed16; per-channel delta truncates signed division toward0. Knight/UniqueSkeleton /5=-55, princes/lord /4=-68, king /2=-137.',
                recipients='Allsix angerchannels at Creature26E, sameownerlinkedlist. Needs world72A1bit1 enabled and recipientdefinition68Abit4 clear. Clamp0..65533, originalhelpers and classification executed.',
                controls='Sourcecallbackcausality, disabledmoods, lowvalueclamp, positive/zero diagnosticrawvalues; sourcefilesunchanged.'),
            gold=dict(Creature_IDs=[18,24,40],threshold=8,ordinary='nativeRNG(10):0..9',unique='nativeRNG(10)+10:10..19',
                result='With worldseed1, actualRNG4 gives ownergold4/14 and externalfloatingnotification. OwnerB43location explicitlyzero leads original4BA420 to directledger7E; treasuryroomrouting not covered.',
                RNG='Original5558D0; independent model: seed=low32(1664525*seed+1013904223);ROR32(seed,13)%10.'),
            object=dict(Chicken9=dict(Creature_IDs=[17,25,30],threshold=12,state=10),
                Rat11=dict(Creature_IDs=[4,6,7,9,10,11,12,22,32,33,35,36,38,39,43],threshold=2),
                source='ActualObjects.kwd9Chicken/11Rat and originalMID/definition/factory/state methods; no fabricatedObjectbody.',
                position='X=(participantX>>12)*4096-2048;Y=(participantY>>12)*4096+2048;Z=0.',
                failure='No spareblock produces noObject butlaststillupdates; sameclock cannotretry.'),
            quake=dict(Creature_IDs=[3,15,41],threshold=12,function=0x510290,
                ordinary='QueueXYZ, clock, duration3*TPS andstrength1000h.',unique='Duration5*TPS,strength3000h.',
                observed='PreparedTPS20 yields60/100 duration and1000h/3000h inWorld9BB0firstqueueentry. OriginalKeeper8lookup/soundnotification executes with source-loaded122bytedefinition.',
                limits='No visualscreen shake or fullqueueexpiry/rendering. NoHPrequests in selectedbranches; no AOE damage assigned.')),
        registration_groups=groups,
        distinction=dict(known_passive='8countergroups from earlierstages; unchanged evidence.',
            active='Theother10registrationgroups have directspecies-dependent activeactioneffects. All18counterbytes werezero and stayzero in these59scenes.',
            unknown='These effects do not prove a gameplayreader of theother10owner counters. Do not label them unused or increase assignedcountercount to18.'),
        fixture=dict(instances='Three full initialized Creature snapshots; one original loadedsourceparticipantdefinition plus runtime species binding; explicitowners/lists/HP/anger/clock; not fullfactoryforeverycreature.',
            dependencies='OriginalObjects9/11/142/143/144andKeeper8loaders. Explicitnullassetprovider, soundIOandfourargfloatingnotificationadapter preserveoriginalgameplaymethods/vtables.',
            failures='Earlier nullKeeper lookup6Dread and droppedresourceprovider exploratorytraces excluded. FrozenCPU5s cutoff mayretryonlyterminalincomplete unchangedinput, preservingfailedfiles; onlycompletesuccess accepted.',
            invariants='Lowerpage onlyDWORD FS:[0], zero badreadabilityprobes, source/vtable/guards/thirdCreature andcounterbytes checked.'),
        native_executions=manifest,independent_Ghidra_controls=controls,primary=proof,storage_validation=storage,preservation=baseline,
        helpers=[link(n) for n in ['probe_crypt_active_action.py','emulate_crypt_active_action.py','crosscheck_crypt_active_action.py','prepare_crypt_active_primary.py','validate_crypt_action_storage.py']],
        limitations=['Exact providedDX/current46sourceIDs; no crossversionequivalence.',
            'Action80 explicitlyprepared; no continuousautomaticentry/activation/exit,visiblebellstrike orcompleteparticipantscheduling.',
            'Readinessabout75%,10owner-countermeaningsstillunassigned, fullgoalactive. Allpriorcomponents/codec/effectiveparameters preserved.'])

def main():
    package=load('editor-format-package.json');file=ROOT/'phase180-prior-package-state.json'
    if not file.exists():
        assert len(package['components'])==157
        dump(file.name,dict(components=deepcopy(package['components']),codec=link('dk2_formats.py'),package_sha256=sha((ROOT/'editor-format-package.json').read_bytes()),
             preserved_top_level={k:canonical(v) for k,v in package.items() if k not in ['components','precedence',KEY]}))
    prior=load(file.name);s=build();dump(SCHEMA,s)
    assert set(package['components']) in [set(prior['components']),set(prior['components'])|{KEY}]
    for k,v in prior['components'].items():assert package['components'][k]==v
    for k,h in prior['preserved_top_level'].items():assert canonical(package[k])==h
    package[KEY]=s;package['components'][KEY]=link(SCHEMA)
    note='crypt_active_action_semantics phase180:59native/46sourceIDs/6Ghidra; realaction80callback499CA0heals5/8, sixangerchannels use sourceVariables102=-275 via51CD90, gold4/14atseed1, actualChicken9/Rat11factory, quakequeue60/100ticksTPS20. Sameclockgates, aliveparticipants, allocationfailure, zeroownercryptcounters verified.10activegroup roles do not assign10owner-counterreaders. Prior157/codec/effective/baseline165preserved, goalactive.'
    if note not in package['precedence']:package['precedence'].append(note)
    dump('editor-format-package.json',package)
    dump('phase180-search-status.json',dict(phase=180,goal_status='active',current_block='Crypt groups',block_complete=False,
        previous_goal_turn=dict(classification='progress',reason='Published/auditedphase179 and executed actualaction80 effects plus source102link; staticlead replaced by native/Ghidraevidence.'),
        counts=s['counts'],remaining=s['priority']['next'],evidence=[link(SCHEMA)]))
    print(json.dumps(s['counts']))

if __name__=='__main__':main()
