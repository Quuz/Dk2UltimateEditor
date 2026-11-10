"""Publish complete Creature sacrifice entry, native preparation and canonical recipe append."""
from pathlib import Path
import json
import emulate_temple_creature_entry as native
import crosscheck_temple_creature_entry as controls
import prepare_temple_creature_primary as primary
import native_temple_creature_entry as rules
from validate_temple_recipe_append import validate as validate_append
from memory_readout_ymm import decode

ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def sha(b):return native.sha(b)
def link(n):return dict(file=n,sha256=sha((ROOT/n).read_bytes()))

def inherited_definition():
    rows=[r for r in load('phase85-raw-creature-definition-emulations.json')['replays'] if r['case']=='raw-warlock']
    assert len(rows)==9;images=[]
    for row in rows:
        path=ROOT/'ghidra'/row['file'];assert sha(path.read_bytes())==row['sha256'];saved=load(str(path.relative_to(ROOT)));r=saved['result']
        assert saved['expected']['source']['sha256']==sha(native.SOURCE.read_bytes()) and saved['expected']['raw_id']==5
        assert r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
        images.append(decode(r['registers']))
    image=b''.join(images)[:2199];assert image==(ROOT/'ghidra/phase85-raw-creature-definition-raw-warlock.bin').read_bytes()
    return dict(inherited_not_new_Ghidra_replays=9,rows=rows,complete_definition_bytes=2199,definition_sha256=sha(image),
        scope='Nine historical deterministic YMM pages reconstruct priorGhidra2199byte definition; not an atomic live process image or nine new controls.')

def diagnostics():
    out=[]
    for path in sorted((ROOT/'ghidra').glob('phase136-temple-creature-native-*.json')):
        saved=load(str(path.relative_to(ROOT)));r=saved['result']
        if r['success']:continue
        assert not saved['baseline']
        timeout=path.name.endswith('-walltimeout-diagnostic.json')
        if timeout:
            assert r['emulation_error'] is None and not r['protected_write_or_execution_failures'] and r['instruction_count']<saved['input']['max_steps']
            reason='Backend wall-time budget ended before rawloader returned;31970steps below600000per-call limit,noCPUerror. Same inputs successfully replayed; archivedtimeout is not passing coverage.'
        else:
            assert saved['case']['entry']
            reason='Incomplete exploration fixture: missing native navigation arrays/work heap. Not a game crash or passing coverage.'
        out.append(dict(**link(str(path.relative_to(ROOT))),error=r['emulation_error'],final_PC=r['registers']['EIP'],
            accepted_case=False,reason=reason))
    assert len(out)==3
    return out

def build():
    attested=primary.build();assert attested==load('phase136-temple-creature-primary.json')
    base,baseline=native.reference();manifest=load('phase136-temple-creature-emulations.json');assert baseline==manifest['baseline']
    runs=manifest['replays'];assert len(runs)==len(native.cases())==14
    for c,row in zip(native.cases(),runs):
        args,checks=native.build(c);wanted,accepted=native.expected(c,checks,base);path=native.path_for(args);saved=load(str(path.relative_to(ROOT)))
        assert saved['input']==args and saved['case']==json.loads(json.dumps(c)) and sha(path.read_bytes())==row['sha256']
        assert saved['result']['source_exe_sha256']==attested['profile']['sha256']
        assert native.verify(c,args,checks,wanted,accepted,saved['result'])==row['outcome']
        decision=rules.decision(room_present=c['room'],room_category=c['room_kind'],surface_height=0 if c['cell_kind']==255 else 4096,
            cell_owner=2,creature_owner=c['owner'])
        assert (decision=='sacrifice')==accepted and (decision=='request_state67')==row['outcome']['transition_requested']
    cross=load('phase136-temple-creature-crosschecks.json')['replays'];assert len(cross)==len(controls.CONTROLS)==7
    for row in cross:
        args,count,size=controls.build(row['case']);saved=load(row['file']);r=saved['result']
        assert saved['input']==args and sha((ROOT/row['file']).read_bytes())==row['sha256']
        assert not saved['tool_is_error'] and r['success'] and r['hit_return']
        assert all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
        assert row['checked_regions']==count and row['checked_bytes']==size
    append=validate_append();assert append==load('phase136-temple-recipe-append-validation.json')
    inherited=load('temple-object-entry-semantics.json')['preservation'];sources={r['file'].lower():r for r in inherited['source_files']}
    sources[str(native.SOURCE).lower()]=dict(file=str(native.SOURCE),sha256=sha(native.SOURCE.read_bytes()))
    sources[append['source']['file'].lower()]=append['source']
    preservation=dict(source_files=list(sources.values()),executable_paths=inherited['executable_paths'])
    for row in preservation['source_files']+preservation['executable_paths']:assert sha(Path(row['file']).read_bytes())==row['sha256']
    prior=ROOT/'phase136-prior-components.json'
    if not prior.exists():
        parts=load('editor-format-package.json')['components'];assert len(parts)==114
        prior.write_text(json.dumps(parts,ensure_ascii=False,indent=2),encoding='utf8')
    assert len(load(prior.name))==114
    by_name={r['case']:r['outcome'] for r in runs}
    return dict(phase=136,date='2026-10-30',families=['Creatures','GlobalVariables type43','Temple room/runtime map cells'],
        profile=attested['profile'],primary=attested,
        coverage_extension=dict(supersedes='Historical static-only Creature472930entry limitation in sacrifice_semantics and phase135temple_object_entry_semantics.',
            new_scope='Complete original472930from entry toreturn, real embeddedcontext Actor24E, rawCreature constructor/init/converter/loader, rawrecipe callback(s), native navinit and actual AIstate transition.',
            remaining='Prepared actor and runtimeMap/room/triangle topology; no newCreaturefactory, wholeWorld/fileparser bootstrap, physical drop, finalentity deletion or realaudio/visual output.'),
        raw_chain=dict(creature_raw_bytes=5449,intermediate_bytes=6457,definition_bytes=2199,source_CreatureId=5,
            ctor=0x491DC0,init=0x568820,convert=0x568E30,load=0x492320,
            measured_baseline=baseline,inherited_definition=inherited_definition(),
            evidence_scope='Entire6457MID measured from bounded rawload baseline and reused unchanged across scenes, not independently named/decoded. Entire2199definition also matches historical Ghidra9page image. Actor/history/reward deltas and nativeinitializer tables/headers checked separately.'),
        decision=dict(callback=0x472930,context_offset_in_actor=0x24E,context_actor_pointer=8,context_world_pointer=12,context_preparation_flag=28,
            room='Worldvslot1D4→508FE0 resolves runtimecell;453A20checks roomcategory BYTE18==0D.',
            surface='4545C0 returns surface height. Zero enters sacrifice; nonzero can request state67 if cellowner low12 matches creatureowner WORD via4539B0.',
            registered_state=dict(state=0x65,category=12,callback=0x472930,
                proof='Executed original47BBE0initializer and independent table projection agree: state101 callback472930; native state table bytes preserved/verified.'),
            actorZ='Not compared against surface by thiscallback. SuppliedZ1000h and8000h onzero-surface cell both sacrifice.',
            owner='Zero-surface sacrifice has noownercomparison in thiscallback; foreignowner3 accepted. Nonzero-surface owner3 doesnot request transition inowner2cell.',
            tested_surface_cells='RuntimecellBYTE0C=FF→zero for selectedTemplecentre;BYTE0C=0→1000h. This is runtime16byte cell data, not rawKLD offsetC or a newly decoded rawmap fourthbyte.'),
        sacrificed=dict(history='Append(type1,actorCreatureIdBYTE36F,actorlevelBYTE403) beforeHP0. Levels3/10observed in history; matcher level averaging remains phase88rule.',
            audio_request='Original sound vslot20 requested with previoushandle0, loadeddefinition6E3=11223344h,eventE2,actorXYZpointer. Declaredexternalprovider returns76543210h intoactor2E. No actualsound played.',
            flags='ActorDWORD3E6 clears20h andsets40h. These runtime flags are not new rawCreatureflag assignments.',
            HP='OriginalCreaturevslot20→4B2AD0sets HP40=0. Tag100, alive/cell registries and preparedTemplemembership remain unchanged; finalunlink/free outside scope.',
            no_recipe='Still records creature andzerosHP when no recipe exists.',
            reward='First matching4F1660recipe invokes4F1780. Selectedmana5goes to roomowner2 andclears history indices; otherrewardbranches not newlycomposed withentry.',
            return_value=0),
        state_request=dict(when='Nonzero surface andmatchingcell/creatureowner; call476D30(context,67h,0), thenreturn1 regardlesstransition acceptance.',
            real_initializers=[0x4798F0,0x47BBE0],category=12,entry_handler=0x47B8C0,
            registered_state65='State65and67bothcategory12: transition doesnot re-enter category or recheck4E53B0capacity. Preparedalready-admittedmember remainsroomtag1; capacity100and0bothmove65→67withHP1000,nohistory.',
            direct_state1_diagnostic='Directly invoking callback from state1(category1) is a separate diagnostic: transition re-enters category12 andchecks native4E53B0 used+definitionBYTE672<=capacity. It is not normal state65callback context.',
            direct_state1_success='Capacity100:actualAIstate67,roomtag1,headtag100,used6B/6D=1; HP1000,nohistory.',
            direct_state1_refused='Capacity0:callbackstillreturns1,state remains1,roomtag0,HP1000; contextclock+18updates100→0 despite refusal.',
            exploration_correction='Early state1 fixtures were valid direct calls but couldnot establish normalstate65capacity semantics. Effective cases distinguish registeredstate65 and explicitdirect-state1diagnostics; no blanket fullTemple refusal rule.',
            not_success_bool=True,examples=dict(registered=by_name['nonfloor-own'],registered_capacity0=by_name['nonfloor-room-full'],
                diagnostic_success=by_name['direct-from-state1-room-available'],diagnostic_refused=by_name['direct-from-state1-room-full'])),
        preparation=dict(condition='ContextDWORD1C==1 invokes48ECE0 before roomtest.',
            chain=[0x48ECE0,0x4D1070,0x4D0F60,0x4D59E0,0x4D5100],
            native_nav_setup=dict(function=0x4CE690,buffer_bytes=236,capacity=8,
                layout='n=(size-4)/29;triangles=n*16;vertices=(n/2)*4;work arrays follow. Nativeinitializer writes pointers/capacities; the supplied CWtriangle covers fixedcentreXY(800h,800h).',
                scope='One prepared triangle with same-position search. No rawMap→navmesh builder or generalnavigation spec. Othertriangle/work bytes kept opaque.'),
            result='Full original path completes and actorproceeds tosacrifice. Controller fields,target/onepoint waypoint and complete navwork outputs checked. No movementframe orlivepath simulation.',
            rejected_fixture_diagnostics=diagnostics()),
        recipe_append=dict(validation=append,applied_original_file_writes=0,
            native_callbacks='Extract45type43records from rebuiltVariables bytes, load all45through51DB30→50F7E0 in order, thenrun fullCreatureentry. Original44donotmatch freshCreature5; appendedindex44matches andawardsmana5.',
            full_native_KWD_parser_executed=False,other_Variable_types_loaded=False,
            important_count_distinction='HeaderN622counts allVariables records,not45recipes. HeaderS/D updates are section sizes, not crypto or CRC checksums.'),
        native_executions=runs,independent_Ghidra_controls=cross,preservation=preservation,
        counts=dict(full_Creature_entry_paths=14,registered_state65_paths=12,direct_state1_diagnostic_paths=2,raw_Creature_loader_baselines=1,raw_Creature_loads_including_baseline=15,
            raw_type43_callback_invocations=sum(len(native.recipes_for_case(c)) for c in native.cases()),
            independent_Ghidra_CPU_controls=7,Ghidra_compared_regions=sum(r['checked_regions'] for r in cross),
            Ghidra_compared_bytes=sum(r['checked_bytes'] for r in cross),
            checked_scene_bytes_including_declared_measured_raw_baseline=sum(r['outcome']['complete_prepared_bytes'] for r in runs),
            entire_raw_definition_bytes_matching_prior_Ghidra=2199,opaque_intermediate_baseline_bytes=6457,
            executed_function_spans=attested['executed_function_spans'],unique_executed_original_PCs=attested['unique_executed_original_PCs'],
            original_vtable_blocks=3,new_complete_container_append_checks=1,new_raw_field_patch_sweeps=0,
            preserved_source_files=len(preservation['source_files']),preserved_executable_paths=7,newly_named_opaque_fields=0,source_writes=0),
        helpers=[link(n) for n in ['emulate_temple_creature_entry.py','crosscheck_temple_creature_entry.py',
            'prepare_temple_creature_primary.py','native_temple_creature_entry.py','append_temple_recipe.py','validate_temple_recipe_append.py',
            'native_unicorn_factorytrace.py','native_sacrifice.py','spell_state_tables.py','creature_category_tables.py',
            'ghidra/phase90-creature-factory-base.bin']],
        limitations=['ExactDXprofile only, no oldEXE/RC3/Flame or Windows livegame behavior.',
            'RawCreature5definition loads natively, but actor is a prepared post-factory snapshot with explicit identity/context/position/level. No newfactory, player drop, complete normalAI scheduler or physicalfall.',
            'Entire6457MID is a measured rawloadbaseline, not independently decoded fields.2199definition matches reconstructed historicalGhidraimage. Opaque outputs remain opaque.',
            'RuntimeMap/room/owners/tags and smallCWnav topology prepared. Native4CE690initializes work-array pointers; no realKLD world/navmesh generation. Same-position path only.',
            'State67request accepted/refused paths executed; return1is not sacrifice or successfultransition. Full normalcaller activation and laterstate67tick not proved.',
            'HP0 leaves entity registered; finaldelete/free not executed. Actual audio/UI/room effect suppressed or captured by declaredexternalproviders; no sound or pixels.',
            'Appendedfile tested by lossless codec/manualbyteoracle and45original recipe callbacks. FullnativeKWDparser and nonrecipeVariable callbacks not executed; sourcefile not changed.',
            'Templeentry→mana composed. Creature/Object/gold reward factories are prior separate evidence, not newly composed with472930. Other15family unknowns remain.'])

if __name__=='__main__':
    v=build();(ROOT/'temple-creature-entry-semantics.json').write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps(v['counts']))
