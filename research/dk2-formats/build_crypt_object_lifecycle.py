"""Publish original Crypt Object lifecycle with explicit native/static scope."""
from pathlib import Path
from copy import deepcopy
import json
import check_crypt_object_scenes as native
import crosscheck_crypt_objects as cross
import prepare_crypt_object_primary as primary
import check_crypt_scene_static as static
ROOT=Path(__file__).resolve().parent
sha=native.sha
KEY='crypt_object_lifecycle_semantics'
SCHEMA='crypt-object-lifecycle-semantics.json'
load=lambda n:json.loads((ROOT/n).read_text(encoding='utf-8'))
dump=lambda n,d:(ROOT/n).write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
link=lambda n:dict(file=n,sha256=sha((ROOT/n).read_bytes()))
canonical=lambda d:sha(json.dumps(d,sort_keys=True,ensure_ascii=False).encode())

def build():
    manifest=load('phase179-crypt-object-scene-checks.json');assert len(manifest['scenes'])==19
    traces=[]
    for c,row in zip(native.cases(),manifest['scenes']):
        assert canonical(row)==canonical(native.run(c))
        traces.append(native.execute(c)[2]['result'])
    controls=load('phase179-crypt-objects-crosschecks.json')['replays'];assert [r['case'] for r in controls]==cross.CONTROLS
    for row in controls:
        args,n,size=cross.build(row['case']);saved=load(row['file']);r=saved['result']
        assert saved['input']==args and (n,size)==(row['checked_regions'],row['checked_bytes'])
        assert row['sha256']==sha((ROOT/row['file']).read_bytes()) and not saved['tool_is_error'] and r['success'] and r['hit_return']
        assert all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
    proof=primary.build();assert proof==load('phase179-crypt-objects-primary.json')
    action=static.check();assert action==load('phase179-crypt-action-static-checks.json')
    prior=load('phase179-prior-package-state.json')
    for ref in prior['components'].values():assert sha((ROOT/ref['file']).read_bytes())==ref['sha256']
    baseline=load('phase165-source-baseline.json')
    for ref in baseline['source_files']+baseline['executable_paths']:assert sha(Path(ref['file']).read_bytes())==ref['sha256']
    visits=lambda a:sum(r['visited_instructions'].get(hex(a),0) for r in traces)
    counts=dict(native_scenes=19,whole_dancer_creator_calls=visits(0x4D9410),Object_factory_attempts=visits(0x4B7A90),
        native_room_Object_attaches=visits(0x4E6220),whole_Crypt_cleanups=visits(0x4D9570),native_Object_unlinks=visits(0x4E62D0),
        World_Object_deletions=visits(0x4B7B10),Object_terminal_calls=visits(0x49B5E0),allocator_frees=visits(0x554BD0),Creature_resets=visits(0x48AD30),
        source_dancer_counts_tested=[0,1,10,20,40],independent_Ghidra_controls=6,
        Ghidra_compared_regions=sum(r['checked_regions'] for r in controls),Ghidra_compared_bytes=sum(r['checked_bytes'] for r in controls),
        fresh_primary_extents=len(proof['functions']),fresh_bounded_CRT_spans=sum('extent_kind' in r for r in proof['functions']),
        inherited_primary_extents=len(proof['inherited']),unique_executed_original_PCs=proof['unique_executed_original_PCs'],original_vtables=6,
        Crypt_groups_assigned_total=8,Crypt_groups_remaining=10,preserved_prior_components=156,preserved_source_files=420,preserved_EXE_paths=7,
        new_raw_fields=0,new_raw_edits=0,source_writes=0)
    return dict(phase=179,date='2026-11-03',families=['Objects','Rooms','Creatures'],profile=proof['profile'],counts=counts,
        priority=dict(current_block='Crypt groups',block_complete=False,next='Execute action80/499CA0 effects and automatic participant entry/exit;10 owner-counter meanings remain unknown.'),
        source_identity=dict(file=str(native.probe.SOURCE),raw_ID_offset=0x35A,raw_stride=894,definition_stride=285,
            names={'142':'Crypt Bell','143':'Crypt Dancer','144':'Crypt Special Dancer'},distinction='Object143 is not Creature Skeleton9. No new serialized field assigned.'),
        creator=dict(address=0x4D9410,signed_parameter='number of attempts',Object_factory=0x4B7A90,Object_ID=143,
            position='X=RoomWORD4B*4096-4096+RNG(3000h); Y=RoomWORD4D*4096-4096+RNG(3000h); Z=0. Repeat until x<1000h OR y<1000h OR y>2000h.',
            success='NativeRNG(7FFh)&7FFh direction;49EF60 enters state57;4E6220 attaches;DWORD133=1.',
            bookkeeping='RoomDWORD55 += full signed parameter modulo32 regardless allocation success; request2 with0/1 availableblocks gives55=2 and0/1 Objects.',
            zero_negative='Zero leaves previous55 unchanged; diagnostic -1 at55=3 gives2 without any factory calls.'),
        list_layout=dict(room_head_WORD=0xE,Object_previous_WORD=0x128,Object_next_WORD=0x12A,Object_room_tag_WORD=0x138,
            attach=0x4E6220,unlink=0x4E62D0,order='Head insertion; both directions checked.'),
        removal=dict(Crypt_cleanup=0x4D9570,registration_setter=0x4D8E60,
            trigger='Nonzero old registered RoomWORD53; with oldtag0, same Object list is preserved.',
            filter='Only Object143;142/144 survive. Factory-default143 state0/marker0 is also removed.',
            chain='4E62D0 unlink ->4B7B10 ->49B5E0 terminal/native virtual destructor ->507970 unregister/aliveclear ->554BD0 free/coalesce. WorldObjectcount decreases.',
            pool='Remove2/40 restores total free bytes; two-block create2/remove2/create1 reuses an actual releasedaddress. Used/free links and full byte balance checked.',
            occupied='One composed cleanup removes Object143 and two real Creature snapshots from room. Creature state1/alive/HPunchanged; registered minimalmember is a separate preparedrecord.',
            after='RoomDWORD55 clears; non143 Objects remain linked.'),
        static_AI_caller=dict(address=0x483870,source_of_counts={'1':[13,37],'10':[29],'20':[2,26,31],'40':[21],'0':'all others'},
            condition='At bell-side position/direction compare55 to selectedcount; if different call creator with fullcount, then activate action80.',
            scope='Static only; full automatic jobselection, actual arrival and accessibility not executed.'),
        static_ritual_action=action,
        interpretation=dict(job='Continuous ritual is supported statically; ordinary work/jobselection not established.',
            sacrifice='No mandatory participantconsumption established in read80/499CA0/496AA0chain; complete participant lifetime and alternative routes unknown.',
            unknown_groups='Periodic effects do not assign10unknown owner counters; no global nonuse claim.'),
        fixture=dict(Room='Prepared room/member and lists; actual raw Crypt/Terrain definitions reused from a separate complete original loader snapshot.',
            Objects='Verbatim raw142/143/144, original MID/definition loading, complete native factories/stateinitialization/attach/destruct/free.',
            allocator='Correct descending free list/sentinels; earlier null freetail, ascendinglist or null Roomdefinition exploratory results excluded.',
            IO='Declared external Sound38 notification(2args,unusedreturn) and imported IsBadReadPtr adapter; no nativegame/vtable replacement.',
            lowpage='All observed lower4096 accesses onlyDWORD FS:[0]; no lazy mapping.',no_live_game='Isolated CPU, not Windows game launch/fullWorldscheduler/GPU/audio.'),
        native_executions=manifest,independent_Ghidra_controls=controls,primary=proof,preservation=baseline,
        helpers=[link(n) for n in ['probe_crypt_objects.py','check_crypt_object_scenes.py','crosscheck_crypt_objects.py','prepare_crypt_object_primary.py','check_crypt_scene_static.py']],
        limitations=['Exact suppliedDX only; no automatic retail1.3/RC3 transfer.',
            'No new rawfield/edits; codec/effectiveparameters preserved. 8/18groups,10unknown.',
            'Action499CA0 and fullparticipant life/automaticentry/visiblebellhit remain unexecuted.',
            'Twelve fresh function extents plus explicit256-byte CRTspan; existingGhidra functionbody identifies onlySEHprologue, no invented fullboundary.',
            'Full15-family goal remains active.'])

def main():
    package=load('editor-format-package.json');file=ROOT/'phase179-prior-package-state.json'
    if not file.exists():
        assert len(package['components'])==156
        dump(file.name,dict(package_sha256=sha((ROOT/'editor-format-package.json').read_bytes()),components=deepcopy(package['components']),codec=link('dk2_formats.py'),
             preserved_top_level={k:canonical(v) for k,v in package.items() if k not in ['components','precedence',KEY]}))
    prior=load(file.name);s=build();dump(SCHEMA,s)
    assert len(prior['components'])==156 and set(package['components']) in [set(prior['components']),set(prior['components'])|{KEY}]
    for k,v in prior['components'].items():assert package['components'][k]==v
    for k,h in prior['preserved_top_level'].items():assert canonical(package[k])==h
    package[KEY]=s;package['components'][KEY]=link(SCHEMA)
    note='crypt_object_lifecycle_semantics phase179:19native/6Ghidra; Object143 create/attach/unlink/destruct/free/reuse, counts0/1/10/20/40 and allocationfailure. Room55 adds requestedcount regardlesssuccess; cleanup filters143, preserves142/144, oldtag0 skips. Fullpoolbalance/source/vtables/guards/FS0 verified. Action80/499CA0 isstaticlead. Prior156/codec/effective/baseline165 preserved;Crypt8/18,goalactive.'
    if note not in package['precedence']:package['precedence'].append(note)
    dump('editor-format-package.json',package)
    dump('phase179-search-status.json',dict(phase=179,goal_status='active',current_block='Crypt groups',block_complete=False,
        previous_goal_turn=dict(classification='progress',reason='19original Object lifecycle scenarios and6independent Ghidra fullmemory controls, sourcebyte coverage12606PC; discovered action80callback499CA0.'),
        counts=s['counts'],remaining=s['priority']['next'],evidence=[link(SCHEMA)]))
    print(json.dumps(s['counts']))

if __name__=='__main__':main()
