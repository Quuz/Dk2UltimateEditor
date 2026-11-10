"""Publish native nonempty-room-list selection; keep unknown placement byte unresolved."""
from pathlib import Path
import json
import emulate_room_wander as native
import crosscheck_room_wander as controls
import prepare_room_wander_primary as primary
from validate_room_wander_ids import validate
ROOT=Path(__file__).resolve().parent
sha=native.sha
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def link(n):return dict(file=n,sha256=sha((ROOT/n).read_bytes()))
def build():
    attested=primary.build();assert attested==load('phase145-room-wander-primary.json')
    manifest=load('phase145-room-wander-emulations.json');rows=manifest['replays'];assert len(rows)==38 and len({r['file'] for r in rows})==38
    states={}
    for c,row in zip(native.cases(),rows):
        args,checks,saved,p=native.execute(c);post,out=native.verify(c,args,checks,saved['result']);states[c['name']]=post
        assert row['case']==c and row['outcome']==out and row['file']==str(p.relative_to(ROOT)) and row['sha256']==sha(p.read_bytes())
        assert saved['result']['source_exe_sha256']==attested['profile']['sha256']
    wanted={a:bytearray(b) for a,b in states['callback-room'].items()};wanted[native.base.cf.ACTOR][0x418]=255
    assert {a:bytes(b) for a,b in wanted.items()}==states['callback-room-wander255']
    cross=load('phase145-room-wander-crosschecks.json')['replays'];assert len(cross)==6
    for row in cross:
        args,n,size=controls.build(row['case']);saved=load(row['file']);r=saved['result']
        assert saved['input']==args and sha((ROOT/row['file']).read_bytes())==row['sha256']
        assert (row['checked_regions'],row['checked_bytes'])==(n,size)
        assert not saved['tool_is_error'] and r['success'] and r['hit_return']
        assert all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
    edits=validate();assert edits==load('phase145-room-ID-api-validation.json')
    preservation=load('wander-target-path-semantics.json')['preservation']
    for row in preservation['source_files']+preservation['executable_paths']:assert sha(Path(row['file']).read_bytes())==row['sha256']
    before=ROOT/'phase145-prior-components.json'
    if not before.exists():
        parts=load('editor-format-package.json')['components'];assert len(parts)==123
        before.write_text(json.dumps(parts,ensure_ascii=False,indent=2),encoding='utf8')
    assert len(load(before.name))==123
    eligible=native.oracle.ELIGIBLE
    corpus=[dict(r,initial_room_filter=r['id'] in eligible) for r in edits['catalogue']]
    # Preserve all development artifacts, but exclude old/incomplete contexts
    # from the accepted scenario counts and published behavior evidence.
    accepted={r['file'] for r in rows};excluded=[]
    for p in sorted((ROOT/'ghidra').glob('phase145-room-wander-native-*.json')):
        if str(p.relative_to(ROOT)) in accepted:continue
        d=load(str(p.relative_to(ROOT)));low=[x for x in d['result']['observed_reads'] if 0<int(x['address'],16)<4096]
        excluded.append(dict(**link(str(p.relative_to(ROOT))),case=d['case']['name'],reason='Superseded initial fixture before explicit Terrain cache/definition.',invalid_lowpage_read_count=len(low)))
    return dict(phase=145,date='2026-10-31',families=['Rooms.kwd','Things C6/C7/C9','Creature runtime AI'],profile=attested['profile'],primary=attested,
      governing_ID=dict(raw_Rooms_byte_offset=0x392,native_room_type_byte_offset=0x18,
        lineage='RawRoomId catalogue source verified here; historical full rawRoom->definition->nativeRoom creation from phases138/139. Current cases begin with explicit runtime room nodes.',
        inherited_room_creation=link('map-room-chain-semantics.json'),source_catalogue=corpus),
      owner_room_list=dict(head='low16 at ownerPlayer+36',next='roomWORD+8 -> global6EE540[tag]',scope='Explicit finite valid linked lists; no normal scheduler/bootstrap or merge-order proof.'),
      room_gate=dict(helper=0x480A00,source='RNG(8) on World+15A0; enter room branch only on result0.',
        probability='One of8 RNG residues, not a claim of exactly12.5% observed gameplay visits.',radius='Helper radius does not limit this room branch; radius1 at cell10,10 selected roomcell20,20.'),
      initial_filter=dict(native_reads=[0x480A66,0x480AAC],eligible_room_ids=sorted(eligible),excluded_source_room_ids=sorted(set(range(1,27))-eligible),
        first_pass='Count all eligible room types on owner list; no fresh RNG(count) chooses a room.'),
      retry=dict(counter='Second pass starts counter=count. Each eligible node decrements it; before zero no room tile selection is attempted.',
        first_trial='Last eligible room in list order.',after_failure='When counter is zero, every later list node is tried, including an initially excluded type.',
        no_revisit='Earlier eligible rooms are not revisited; failure of final candidate with no later success falls through to ring search.',
        selected_examples=['Two Treasury/Lair nodes: only second node tried; reversed list selects other physical room.',
          'First valid Treasury + final empty Lair: earlier Treasury skipped, ring fallback.',
          'Same list + trailing Portal3 or WoodenBridge8: trailing excluded node can supply the target.'],
        boundary='Observed native algorithm; design intent and complete normal-game list lifecycle are not inferred.'),
      cell_selection=dict(function=0x4E8430,anchor='roomBYTE2D/2E',cell_room='cellWORD2 low12 -> global6EE540',cell_next='cellWORD4 packs next Xlow8,Yhigh8;0,0 terminates',
        algorithm='Count cells for which original454110 and454230 both return0; consume RNG(count), walk same chain again to selected valid cell.',
        output='Cell center Q12: x*4096+2048,y*4096+2048,Z0.',
        outer_blocker='After cell helper success, original453600 must find no blocking object; otherwise advance to next room, no repeated draw from this room.',
        scope='All accepted room cells use CellC0, no object chains. Both original predicates execute; all their other branches are not claimed.'),
      callback=dict(function=0x4808E0,state=87,category=0,room_path='Original helper -> jitter RNG9 ->471890 -> original NAV/4D0F60 -> state6, continuation87.',
        blocked_path='All NAV8 with explicit non-corner-blocking Terrain definition: room target still selected, later path request rejected, state87 remains.',
        physical_movement=False,normal_World_scheduler=False),
      raw_WanderRadius=dict(runtime_DX_byte_offset=0x418,meaning='Gameplay effect and units unresolved.',new_pair=[0,255],
        result='In room-success callback, all other compared memory and targets/RNG/controller state identical; no418 read across38accepted cases.',
        no_global_unused_claim=True,do_not_substitute_room_or_ring_radius=True),
      fixtures=dict(map_size=[32,32],room_object_bytes=169,room_guards=64,room_nodes='Prepared nodes and cell links, not freshly constructed from KLD.',
        Terrain='Explicit valid prepared ID1 definition460bytes, flags17B=0, cache6DA4A8[1] nonNULL; not a new original rawTerrain load.',
        original_AI_NAV_setup='Same six original initializers as phase144 for3callbacks; original World/Creature vtables unchanged.',external_code_executed=False),
      native_executions=manifest,independent_Ghidra_controls=cross,API_validation=edits,diagnostic_exclusions=excluded,preservation=preservation,
      counts=dict(native_scenarios=38,complete_helper_cases=35,complete_state87_cases=3,source_room_types_tested=26,initially_eligible_source_types=14,
        accepted_native_path_requests=2,rejected_native_path_requests=1,paired_complete_memory_controls=1,paired_compared_bytes=manifest['paired_control']['whole_bytes'],
        independent_Ghidra_CPU_controls=6,Ghidra_compared_regions=sum(x['checked_regions'] for x in cross),Ghidra_compared_bytes=sum(x['checked_bytes'] for x in cross),
        additional_whole_function_spans=len(attested['functions']),inherited_whole_function_spans=len(attested['inherited_attestation']['reused_function_addresses']),
        unique_executed_original_PCs=attested['unique_executed_original_PCs'],additional_primary_blocks=len(attested['blocks']),
        new_exact_container_edits=edits['new_exact_container_edits'],preserved_source_files=len(preservation['source_files']),preserved_executable_paths=len(preservation['executable_paths']),
        newly_named_opaque_fields=0,source_writes=0),
      helpers=[link(n) for n in ['probe_room_wander.py','native_room_wander.py','emulate_room_wander.py','native_unicorn_roomwandertrace.py',
        'prepare_room_wander_trace.py','crosscheck_room_wander.py','prepare_room_wander_controls.py','prepare_room_wander_primary.py','validate_room_wander_ids.py']],
      limitations=['Exact suppliedDX only; no certified retail1.3/RC3game behavior claim.',
        'Prepared Actor, runtime room/cell/owner lists and floor definition. No new rawThings/Roomfactory/KLD bootstrap or natural room-list order/merges.',
        'Room cells CellC0/no blocking objects; special cell predicates, blocked-object retry and later update/deletion lifecycle remain unexecuted.',
        'NAV acceptance and AIstate changes are proved, but physicalmovement/animation/endofroute/normalWorldscheduler are not executed.',
        'RoomID diagnostic markers test copy-only byte preservation. Changing IDs can invalidate references; source files unchanged.',
        'RawWanderRadius, AP04/08, Terrain198/224 and other significant unknowns remain. No new opaque field named; readiness remains about75%.'])
if __name__=='__main__':
    v=build();(ROOT/'room-wander-semantics.json').write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps(v['counts']))
