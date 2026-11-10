"""Publish original Room construction7 producer and its composed Temple entry."""
from pathlib import Path
import json
import emulate_room_adjacency as native
import verify_room_adjacency as oracle
import crosscheck_room_adjacency as controls
import prepare_room_adjacency_primary as primary
from validate_room_adjacency_edits import validate as validate_edits

ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def sha(b):return native.sha(b)
def link(n):return dict(file=n,sha256=sha((ROOT/n).read_bytes()))

def build():
    attested=primary.build();assert attested==load('phase137-room-adjacency-primary.json')
    runs=load('phase137-room-adjacency-emulations.json');assert len(runs['replays'])==26 and len(runs['baselines'])==5
    for name,row in zip(oracle.BASE_NAMES,runs['baselines']):assert oracle.baseline(name)[1]==row
    for c,row in zip(native.cases(),runs['replays']):
        args,checks,saved,path=native.execute(c);_,out=oracle.verify(c,args,checks,saved['result'])
        assert out==row['outcome'] and link(str(path.relative_to(ROOT)))=={k:row[k] for k in ['file','sha256']}
        assert saved['result']['source_exe_sha256']==attested['profile']['sha256']
    cross=load('phase137-room-adjacency-crosschecks.json')['replays'];assert len(cross)==len(controls.CONTROLS)==5
    for row in cross:
        args,count,size=controls.build(row['case']);saved=load(row['file']);r=saved['result']
        assert args==saved['input'] and sha((ROOT/row['file']).read_bytes())==row['sha256']
        assert not saved['tool_is_error'] and r['success'] and r['hit_return']
        assert all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
        assert row['checked_regions']==count and row['checked_bytes']==size
    edits=validate_edits();assert edits==load('phase137-room-adjacency-api-validation.json')
    preservation=load('temple-creature-entry-semantics.json')['preservation']
    sources={r['file'].lower():r for r in preservation['source_files']}
    for row in edits['files']:sources[row['file'].lower()]=row
    for name in ['Rooms.kwd','Terrain.kwd']:
        path=native.prior.SOURCE.parent/name;sources[str(path).lower()]=dict(file=str(path),sha256=sha(path.read_bytes()))
    preservation=dict(source_files=list(sources.values()),executable_paths=preservation['executable_paths'])
    for row in preservation['source_files']+preservation['executable_paths']:assert sha(Path(row['file']).read_bytes())==row['sha256']
    prior=ROOT/'phase137-prior-components.json'
    if not prior.exists():
        parts=load('editor-format-package.json')['components'];assert len(parts)==115
        prior.write_text(json.dumps(parts,ensure_ascii=False,indent=2),encoding='utf8')
    assert len(load(prior.name))==115
    room_table=[dict(id=r[0x392],name=r[:32].split(b'\0')[0].decode('cp1252'),terrain_id=r[0x394],construction=r[0x395])
        for s in native.fmt.parse((native.prior.SOURCE.parent/'Rooms.kwd').read_bytes()) if native.fmt.FAMILIES.get(s.signature)=='Rooms' for r in s.records]
    diagnostics=[]
    for path in sorted((ROOT/'ghidra').glob('phase137-room-adjacency-native-*.json')):
        saved=load(str(path.relative_to(ROOT)))
        if saved['result']['success']:continue
        diagnostics.append(dict(**link(str(path.relative_to(ROOT))),passing_coverage=False,error=saved['result']['emulation_error'],
            reason='Exploration fixture missing provider90 or using wrong Session-provider54 stack cleanup. Corrected declared external interface; no game defect inferred.'))
    assert len(diagnostics)==2
    return dict(phase=137,date='2026-10-30',families=['Rooms','Terrain','Map runtime cells','Creatures','GlobalVariables type43'],profile=attested['profile'],primary=attested,
        coverage_extension=dict(new_scope='Whole raw Room/Terrain/Creature loaders, original CMap450470->RoomMgr4EDC10->4EFDE0, UI region request and selected complete Creature472930 entry.',
            supersedes='Prepared cellC constant in phase136 Temple fixture is replaced by actual native producer for the selected construction7 paths.',
            remaining='Runtime World/cell/room instance setup remains prepared. Native rawKLD World construction, full remesh, normal AI caller and final entity deletion are not newly executed.'),
        raw_chain=dict(Rooms=dict(raw_bytes=1055,raw_construction_offset=0x395,intermediate_bytes=1267,intermediate_offset=0x469,definition_bytes=1158,runtime_offset=0x452,
            constructor=0x4EAB70,initializer=0x56DE40,converter=0x56DFD0,loader=0x4EACC0),Terrain=dict(raw_bytes=552,intermediate_bytes=736,definition_bytes=460,initializer=0x56EB60,converter=0x56ED20,loader=0x4F66E0),
            opaque_scope='RoomMID/definition baselines measure complete outputs; opacity and unknown field names are preserved. Creature/Terrain reference scopes are explicit in baseline manifest.'),
        map_projection=dict(runtime_cell_bytes=16,mask_offset=12,original_update=0x450470,room_dispatcher=0x4EDC10,construction7_helper=0x4EFDE0,
            terrain_byte_offset=10,owner_word_offset=8,owner_mask=0xFFF,directions=[dict(bit=i,dx=dx,dy=dy) for i,(dx,dy) in enumerate(native.DIRS)],
            rule='Set bit if neighbour is in bounds, its TerrainId equals the supplied definition TerrainId and its owner low12 equals center owner low12.',
            neighbour_room_tag_compared=False,neighbour_room_instance_compared=False,owner_high_bits_compared=False,
            native_direction_table=0x6BC370,mode1='Write zero to runtime cell+C.',mode6='No switch case; retain prior runtime cell+C.',
            not_raw_KLD_byte3=True,raw_KLD_fourth_byte_still_opaque=True),
        navigation=dict(predicate=0x454110,setter_wrapper=0x454FD0,bitmap_setter=0x4D1930,requested_code=4,
            tested_room_categories=[11,13,16],all_ff_masks_request_setter=True,initial_bitmap_byte=4,bitmap_bytes_preserved=True,
            note='Setter sees old4==new4, so returns without4CE770. This proves request and no-change branch, not a changed navigation mesh or a semantic name for code4.'),
        UI_region_request=dict(original_wrapper=0x517480,forwarder=0x527150,payload_bytes=16,operation_code=6,
            x_start='x-2; if signed16<=1 then1 and width3, else width5; if start>=mapwidth subtract3.',
            y_start='y-2; if signed16<1 then1 and height3, else height5; if start>=mapheight subtract3.',
            provider_scope='Original Session+10 provider vslot28 accepts and captures request; Session26C increments. No UI rendered; operation6 broader purpose is not newly decoded.'),
        composed_temple_examples={r['case']:r['outcome'] for r in runs['replays'] if r['case'].startswith('entry-')},
        current_room_construction=room_table,native_executions=runs,independent_Ghidra_controls=cross,API_validation=edits,
        diagnostic_exclusions=diagnostics,preservation=preservation,
        counts=dict(full_native_producer_paths=26,rawload_baselines=5,raw_Room_loads=31,raw_Terrain_loads=31,raw_Creature_loads=31,raw_recipe_callbacks=31,
            composed_Creature_entries=3,independent_Ghidra_CPU_controls=5,Ghidra_compared_regions=sum(r['checked_regions'] for r in cross),Ghidra_compared_bytes=sum(r['checked_bytes'] for r in cross),
            checked_scene_bytes=sum(r['outcome']['complete_bytes'] for r in runs['replays']),executed_function_spans=len(attested['functions']),unique_executed_original_PCs=attested['unique_executed_original_PCs'],
            original_vtable_blocks=len(attested['blocks']),new_exact_container_rebuilds=82,preserved_source_files=len(sources),preserved_executable_paths=len(preservation['executable_paths']),newly_named_opaque_fields=0,source_writes=0),
        helpers=[link(n) for n in ['native_room_adjacency.py','emulate_room_adjacency.py','verify_room_adjacency.py','crosscheck_room_adjacency.py','prepare_room_adjacency_primary.py','validate_room_adjacency_edits.py']],
        limitations=['Exact supplied DX profile only; no new oldEXE/RC3/Flame or Windows livegame controls.',
            'World, grid, tags, room instances and postfactory Creature snapshot are prepared. No full original KLD parser/world-placement bootstrap or normal AI caller.',
            'NAV4 is explicit prestate. Actual mesh rebuild4CE770 is not executed by accepted cases; changed-nav behavior remains open.',
            'Graphics argument is NULL; declared Session-provider lookup returns failure and initializes a zero handle, UI requests are captured, VFX allocation returns failure. Actual rendering/audio/UI is not tested.',
            'Room1267MID and1158definition are whole measured baselines; this is not complete semantic decoding of their unknown fields.',
            'RecipeCreature5->mana5 is diagnostic; source recipes/EXE/data are unchanged. Final entity unlink/free and entry->nonmanafactory reward remain open.',
            'Runtime16byte cell+C is derived forconstruction7; rawKLD fourth byte remains opaque. Other construction modes except1/6 controls and nonroom cell+C producer are not newly modeled.',
            'Substantial unresolved questions remain across all15 requested file families.'])

if __name__=='__main__':
    value=build();(ROOT/'room-adjacency-semantics.json').write_text(json.dumps(value,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps(value['counts']))
