"""Publish selected state87/native NAV acceptance without naming raw-byte units."""
from pathlib import Path
import json
import emulate_wander_target_paths as native
import crosscheck_wander_target_paths as controls
import prepare_wander_target_primary as primary
from validate_wander_placement_bytes import validate

ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def sha(b):return native.sha(b)
def link(n):return dict(file=n,sha256=sha((ROOT/n).read_bytes()))

def effective_things():
    layouts=json.loads(json.dumps(load('thing-layouts.json')))
    for tag in ['0xC6','0xC7','0xC9']:
        targets=[23] if tag=='0xC6' else [29] if tag=='0xC7' else [43+32*i+29 for i in range(16)]
        for f in layouts[tag]['fields']:
            if f['offset'] not in targets:continue
            assert f['size']==1 and f['kind']=='u8'
            f['prior_native_label']='WanderRadius'
            f['meaning_status']='Gameplay effect and units unresolved; native label/storage confirmed earlier. Not the state87 hardcoded4..7 ring radius.'
            f['verified_runtime_storage']=dict(DX_byte_offset=0x418,older_profile_byte_offset=0x417)
            f['selected_native_boundary']='Phase144 actual World queries/NAV path/state87->6 unaffected by instanceBYTE418 0/255; all other prepared memory identical.'
            f['recommended_UI_label']='WanderRadius (units/effect unresolved)'
            f['serialization']='Preserve fullu8 and other bytes; do not normalize0 to off or substitute the hardcoded state87 radius.'
    return layouts

def build():
    attested=primary.build();assert attested==load('phase144-wander-target-path-primary.json')
    manifest=load('phase144-wander-target-path-emulations.json');rows=manifest['replays']
    assert len(rows)==17 and len({r['file'] for r in rows})==17
    states={}
    for c,row in zip(native.cases(),rows):
        args,checks,saved,p=native.execute(c);post,out=native.verify(c,args,checks,saved['result']);states[c['name']]=post
        assert row['case']==c and row['outcome']==out and row['file']==str(p.relative_to(ROOT)) and row['sha256']==sha(p.read_bytes())
        assert saved['result']['source_exe_sha256']==attested['profile']['sha256']
    expected={a:bytearray(b) for a,b in states['callback-open'].items()};expected[native.direct.cf.ACTOR][0x418]=255
    assert {a:bytes(b) for a,b in expected.items()}==states['callback-wander255']
    cross=load('phase144-wander-target-path-crosschecks.json')['replays'];assert len(cross)==6
    for row in cross:
        args,n,size=controls.build(row['case']);saved=load(row['file']);r=saved['result']
        assert saved['input']==args and sha((ROOT/row['file']).read_bytes())==row['sha256'] and row['checked_regions']==n and row['checked_bytes']==size
        assert not saved['tool_is_error'] and r['success'] and r['hit_return']
        assert all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
    edits=validate();assert edits==load('phase144-wander-placement-api-validation.json')
    previous=load('effect-mode18-semantics.json')['preservation']
    sources={str(Path(r['file'])).lower():r for r in previous['source_files']}
    # Add all109 historical source-hashed carrier files, not just102 edits.
    # Earlier common preservation roster covered map/globals, not everyThings.
    for row in edits['files']+edits['skipped']:
        key=str(Path(row['file'])).lower()
        if key in sources:assert sources[key]['sha256']==row['sha256']
        sources[key]={k:row[k] for k in ['file','sha256']}
    preservation=dict(source_files=list(sources.values()),executable_paths=previous['executable_paths'],
        previous_source_roster_count=len(previous['source_files']),added_historical_carrier_files=len(sources)-len(previous['source_files']))
    for row in preservation['source_files']+preservation['executable_paths']:assert sha(Path(row['file']).read_bytes())==row['sha256']
    prior=ROOT/'phase144-prior-components.json'
    if not prior.exists():
        components=load('editor-format-package.json')['components'];assert len(components)==122
        prior.write_text(json.dumps(components,ensure_ascii=False,indent=2),encoding='utf8')
    assert len(load(prior.name))==122
    cbs=[r for r in rows if r['case']['kind']=='callback'];helpers=[r for r in rows if r['case']['kind']=='helper']
    return dict(phase=144,date='2026-10-31',families=['Things C6/C7/C9','Creature runtime AI'],profile=attested['profile'],primary=attested,
        registration=dict(native_initializer=0x47BBE0,state=87,category=0,callback=0x4808E0,
            execution='Callback invoked directly with prepared embedded Actor+24E AIcontext in state87; normal World AI scheduler not executed.'),
        state87=dict(predicate=0x496D00,predicate_rule='ActorDWORD3A9 nonzero and not15h returns immediatelytrue before RNG/target search.',
            seed_storage='CWorld+15A0: one shared LCG stream for radius, helper and jitter.',
            radius='RNG(4)+4, hardcoded4..7; after helper failure retry decremented radius down to1.',helper=0x480A00,
            jitter='One RNG(9), original682/2047/3412 per-axis offsets, retain chosen cell XY upperbits; fallback starts at current ActorXYZ.',
            fallback='All ring helpers fail -> jitter currentcell and still request movement; foreign-owner fixture accepts this fallback despite ring ownership rejection.',
            request=0x471890,path_controller=0x4D0F60,accepted_state=6,
            acceptance_scope='Native path request accepted and AIstate becomes6; actorXYZ unchanged. Movementstep, animation/endofroute not executed.'),
        ring_helper=dict(radius_storage='Low8 of helper argument; ordinary tested radii1..7 in cells.',
            selected_scope='Prepared32x32 CWorld/CMap/NAV, empty ownerroomlist, no room tags/blocking objects.',
            optional_room_branch='Native RNG(8) also draws; owner room list empty here, nonempty room selection not covered.',
            scan='RNG(8*r) starting index, then increasing through8*r-1; selected normal-coordinate cases do not wrap.',
            sides=['(cx+step-r,cy-r)','(cx+r,cy+step-r)','(cx+step-r,cy+r)','(cx-r,cy+step-r)'],
            gates='Original World1FC bounds, World208 NAVbit8, World224/1D8 room/owner lookup, cellWORD8 low12 owner or Actorflags3E6 mask4000, original453600 blocker search.',
            asymmetry='Ordinary forward scan from0 visits northwest corner twice, southeast never. Fromstart4 atorigin10,10/r4, earlier-onlypoint7,6 not revisited; openSEcorner14,14 also remains unselected.',
            diagnostic='Exact selectedsearchbehavior; not a claim allwander AI or every coordinate branch follows this oracle.'),
        raw_byte_boundary=dict(native_label='WanderRadius',neutral_payload_offset=0x17,good_payload_offset=0x1D,party_member_payload_offset=0x1D,
            runtime_DX_offset=0x418,runtime_older_offset=0x417,meaning='Units and gameplay consumer unresolved.',selected_pair=[0,255],
            observed='No instance418 read in17accepted paths; successful target/RNG/NAV/controller/state equal, fullmemory pair differs onlyActor418.',
            not_a_global_unused_claim=True,no_mapping_to_hardcoded_radius=True,
            lineage='Raw->Actor storage is earlier31/47/75 evidence. New execution begins with prepared postfactory Actor, not fullThingsloader/newCreaturefactory.'),
        native_setup=dict(world_dimensions=[32,32],AI_initializers=[0x4798F0,0x47BBE0],NAV_initializers=[0x4CE690,0x4CE740,0x4C8F30,0x4CEF20],
            original_World_vtable=0x66E3EC,original_Creature_vtable=0x66D524,external_code_executed=False,
            triangle_storage_bytes=native.path.TRIANGLE_BYTES,triangle_capacity=native.path.NTRIANGLES,
            initial_failed_probe='UninitializedNAV path probe failed at4C9934 before nativeNAVsetup; excluded from17accepted cases. Fixture omission, not evidence of game crash.',
            decompiler_pointer_correction='Typeddecompiler World+568 is DWORDpointer arithmetic; actual RNGargument World+15A0 verified by instructions and entry observation.'),
        native_executions=manifest,independent_Ghidra_controls=cross,API_validation=edits,preservation=preservation,effective_thing_layouts=effective_things(),
        counts=dict(native_scenarios=17,distinct_native_runs=17,complete_target_helper_cases=len(helpers),complete_state87_cases=len(cbs),accepted_native_path_requests=sum(r['outcome']['path_accepted'] for r in cbs),
            paired_complete_memory_controls=1,paired_compared_bytes=manifest['paired_placement_control']['whole_bytes'],
            independent_Ghidra_CPU_controls=6,Ghidra_compared_regions=sum(r['checked_regions'] for r in cross),Ghidra_compared_bytes=sum(r['checked_bytes'] for r in cross),
            executed_function_spans=len(attested['functions']),unique_executed_original_PCs=attested['unique_executed_original_PCs'],original_vtable_blocks=2,original_ring_switch_blocks=2,
            new_exact_container_edits=edits['new_exact_container_edits'],carrier_files_without_supported_records=len(edits['skipped']),
            preserved_source_files=len(preservation['source_files']),preserved_executable_paths=len(preservation['executable_paths']),newly_named_opaque_fields=0,source_writes=0),
        helpers=[link(n) for n in ['native_wander_target.py','probe_wander_target.py','probe_wander_path.py','emulate_wander_target_paths.py','native_unicorn_wandertrace.py','crosscheck_wander_target_paths.py','prepare_wander_target_primary.py','validate_wander_placement_bytes.py']],
        limitations=['Exact suppliedDX only for new execution; olderActor417 lineage remains historical, not fresh olderprofile wandering.',
            'Prepared postfactoryActor/runtime definition/world/ownergrid/emptyroomlist; no new rawCreatures/Things->factory or completeKLDworldbootstrap.',
            'Original AI/NAV initializers, CWorld queries and pathrequest execute without externalcode. Directcallback, not normalWorldscheduler, actualmovement, animation/endofroute or lifecycle.',
            'Nonempty room branch, roomtag/type5 rejection, blocking objects and special-coordinate fallback branch not executed. Oracle bounded to selectedemptyroomlist/normal ringinputs.',
            'InstanceWanderRadius byte effect/units and substantialunknowns in15families remain open. No globalnonuse/zero-disables claim; readiness remains about75%.'])

if __name__=='__main__':
    v=build();(ROOT/'wander-target-path-semantics.json').write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps(v['counts']))
