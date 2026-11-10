"""Publish full native Map Room placement, decoration cleanup and changed NAV."""
from pathlib import Path
import json
import emulate_map_room_chain as native
import crosscheck_map_room_chain as controls
import prepare_map_room_primary as primary
from validate_map_room_edits import validate as validate_edits

ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def sha(b):return native.sha(b)
def link(n):return dict(file=n,sha256=sha((ROOT/n).read_bytes()))

def build():
    attested=primary.build();assert attested==load('phase138-map-room-primary.json')
    manifest=load('phase138-map-room-emulations.json');runs=manifest['replays'];assert len(runs)==11
    posts={};pairs=[]
    for c,row in zip(native.cases(),runs):
        args,checks,saved,path=native.execute(c);post,out=native.verify(c,args,checks,saved['result']);posts[c['name']]=post
        assert out==row['outcome'] and sha(path.read_bytes())==row['sha256'] and str(path.relative_to(ROOT))==row['file']
        assert saved['result']['source_exe_sha256']==attested['profile']['sha256']
        if c['name'].startswith('square-'):pairs.append(native.paired(posts['square'],post,c))
    assert pairs==manifest['paired_controls']
    cross=load('phase138-map-room-crosschecks.json')['replays'];assert len(cross)==len(controls.CONTROLS)==4
    for row in cross:
        args,count,size=controls.build(row['case']);saved=load(row['file']);r=saved['result']
        assert saved['input']==args and sha((ROOT/row['file']).read_bytes())==row['sha256']
        assert not saved['tool_is_error'] and r['success'] and r['hit_return']
        assert all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
        assert row['checked_regions']==count and row['checked_bytes']==size
    edits=validate_edits();assert edits==load('phase138-map-room-api-validation.json')
    old=load('room-adjacency-semantics.json')['preservation'];sources={r['file'].lower():r for r in old['source_files']}
    for r in edits['files']:sources[r['file'].lower()]={k:r[k] for k in ['file','sha256']}
    for n in ['Objects.kwd','Rooms.kwd','Terrain.kwd']:
        path=native.p.SOURCE.parent/n;sources[str(path).lower()]=dict(file=str(path),sha256=sha(path.read_bytes()))
    preservation=dict(source_files=list(sources.values()),executable_paths=old['executable_paths'])
    for row in preservation['source_files']+preservation['executable_paths']:assert sha(Path(row['file']).read_bytes())==row['sha256']
    prior=ROOT/'phase138-prior-components.json'
    if not prior.exists():
        parts=load('editor-format-package.json')['components'];assert len(parts)==116
        prior.write_text(json.dumps(parts,ensure_ascii=False,indent=2),encoding='utf8')
    assert len(load(prior.name))==116
    diagnostic=[]
    for path in sorted((ROOT/'ghidra').glob('phase138-map-room-discovery-*.json')):
        saved=load(str(path.relative_to(ROOT)));r=saved['result']
        if r['success']:
            # Old snapshots may have creation-only allocator sentinels. They
            # never enter accepted11case evidence, even if callback returned.
            reason='Exploration snapshot, not accepted scene; may lack original Object allocator initializer/complete free list.'
        else:reason='Incomplete exploration fixture: absent provider D4/38, missing Object manager backpointer or zero ticks-per-second. No game crash conclusion.'
        diagnostic.append(dict(**link(str(path.relative_to(ROOT))),accepted_coverage=False,reason=reason,cpu_success=r['success'],error=r['emulation_error']))
    return dict(phase=138,date='2026-10-30',families=['Map','Rooms','Terrain','Objects'],profile=attested['profile'],primary=attested,
        coverage_extension=dict(new_scope='Complete51EC60 Map callback, native Room construction/activation/attach/join, original decoration factories/cleanup, full changed-navigation remesh with nonzero5x5 dimensions.',
            supersedes='Prior Map flag fragment patched out placement; phase137 used prepared active room and map cells and unchangedNAV4. This phase preserves original game instructions and builds selected room cells.',
            remaining='Host projects raw4byte cells to established16byte MapMID; complete native file parser/World bootstrap, general pool creation and normal AI caller remain open.'),
        MapMID=dict(bytes=16,source_projection='native_map_cells.intermediate_cell; historical original parser proof retained separately.',callback=0x51EC60,ABI='cdecl(x,y,MapMID*,World*)',
            native_sum=dict(function=0x5108C0,world_offset=0x72E7,rule='Add signed8 values of all16 MapMID bytes modulo2^32, including presence flags and zero padding.',
                not_a_raw_KLD_header_checksum=True),
            native_PlayerId='MapMID+D chooses World+84+(PlayerId-1)*0x1125; selected input2 gives PlayerWORDtag2. No broad invalid-ID bounds guarantee.',
            Terrain_room_flag=0x10,room_lookup_by_TerrainId=0x50B550,room_builder=0x4C5C30,
            final_flag='MapMID+E bit02 updates runtime cell WORD8 bit1000 after terrain/room placement; other tested bits affect sum only in these scenes.',
            fourth_raw_byte_still_opaque=True),
        room_creation=dict(manager=0x4ED1A0,branch=0x4ED320,create=0x4EC490,create_attach=0x4EC5A0,join=0x4EC650,
            native_slot_constructor=0x4E3790,native_activation=0x4E3800,cell_attach=0x450770,
            prepared_free_list='One free room tag1000, two native-constructed/register-tagged169byte slots1000/1001; only connected placements tested. Full672slot malloc initializer4EC000 not executed.',
            definition_scope='Original Temple13/CombatPit16 Room records loaded in sorted catalogue; TerrainTemple22/Ground3 loaded; selected callbacks build only Temple13.',
            verified_invariants=['one active room for all selected connected points','room tag1000, owner2','tile linked list and tile count equal placed set','room health sum10000*tilecount','rawRoom/Terrain-derived cellC masks']),
        decorations=dict(ObjectIds_loaded=[72,73,97,98,111,132],native_factory=0x4B7A90,native_state_initializer=0x49E600,
            actual_live_ObjectIds=[111],actual_object_type=0x66D654,
            allocator_initializer=0x554AD0,allocator_allocate=0x554B30,allocator_free=0x554BD0,
            usable_arena_bytes=16384,arena_envelope_bytes=24,live_allocation_bytes=408,live_object_bytes=393,
            verified='Native rebuild removes/replaces earlier decorations. Used/free lists partition all16384usablebytes; usedbytes matches World76; every live Object has exact registered tag/owner pointer and native vtable.',
            no_final_sacrificed_Creature_removal_claim=True),
        navigation=dict(native_setup=[0x4CE690,0x4CE740,0x4C8F30,0x4CEF20],storage_bytes=1454,extra_guard_bytes=64,
            map_dimensions=[5,5],stride=5,initial_bitmap=0,
            original_rebuild=0x4CE770,initial_rebuilds_per_scene=1,additional_changed_rebuilds='One for3x3square including flag variants/reversed order; zero formissingdiagonal/rectangle/pair/single.',
            bitmap_rule='Only room cells with construction7 maskFF become4 in these scenes; all other25bitmapbytes remain0.',
            CPU_agreement_scope='Ghidra and Unicorn agree on all prepared output regions including mesh/work buffers. Internal mesh byte meanings/triangulation/optimality are not newly decoded.'),
        native_executions=manifest,independent_Ghidra_controls=cross,API_validation=edits,diagnostic_exclusions=diagnostic,preservation=preservation,
        counts=dict(native_scenes=11,baseline_scenes=1,full_Map_callbacks=sum(r['outcome']['room_tiles'] for r in runs),
            whole_native_Room_loads=22,whole_native_Terrain_loads=22,whole_native_Object_loads=66,native_Object_pool_initializers=11,
            composed_Creature_entry_executions=0,independent_Ghidra_CPU_controls=4,Ghidra_compared_regions=sum(r['checked_regions'] for r in cross),Ghidra_compared_bytes=sum(r['checked_bytes'] for r in cross),
            paired_complete_memory_controls=4,paired_compared_bytes=sum(r['whole_compared_bytes'] for r in pairs),
            executed_function_spans=len(attested['functions']),unique_executed_original_PCs=attested['unique_executed_original_PCs'],primary_blocks=len(attested['blocks']),
            new_exact_Map_container_patches=110,preserved_source_files=len(sources),preserved_executable_paths=len(preservation['executable_paths']),newly_named_opaque_fields=0,source_writes=0),
        helpers=[link(n) for n in ['probe_map_room_placement.py','emulate_map_room_chain.py','crosscheck_map_room_chain.py','prepare_map_room_primary.py','validate_map_room_edits.py']],
        limitations=['Exact supplied DX profile only; no new oldEXE/RC3/Flame/liveWindows coverage.',
            'Prepared5x5World, native-constructed free slots, explicit bounded free-list and arena backing memory. Full native4EC000/CRT allocation and rawKLD World bootstrap not executed.',
            'RawMap->MapMID is host projection backed by historical parser evidence. Current native path starts at complete51EC60 callback; does not claim full native KLD parser.',
            'Whole-scene Ghidra controls compare measured native outputs; independent semantic checks cover named projections/pool invariants and paired flags, not meaning of every opaque output byte.',
            'External providers explicitly return no graphics/overlay/VFX, capture UI requests and supply inherited synthetic sound tokens/release. No real pixels/audio/UI.',
            'Only connected Temple shapes and owner2 selected; native general room merging/splitting, invalid selectors and otherRoom types are not newly proved.',
            'Native callback71calls and real NAV rebuild do not execute normalAI state65 caller or physicalCreature drop/sacrifice/final deletion. Phase137 supplies separate entry linkage.',
            'RawKLD fourth byte and other15family unresolved fields remain opaque; no new unknown field names are claimed.'])

if __name__=='__main__':
    value=build();(ROOT/'map-room-chain-semantics.json').write_text(json.dumps(value,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps(value['counts']))
