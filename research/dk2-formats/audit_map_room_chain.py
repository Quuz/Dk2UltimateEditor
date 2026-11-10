"""Audit117component publication and bounded complete native Map room chain."""
import json
from build_map_room_chain import ROOT,load,sha,link,build

def main():
    value=load('map-room-chain-semantics.json');assert value==build()
    package=load('editor-format-package.json');prior=load('phase138-prior-components.json')
    assert len(package['components'])==117 and len(prior)==116
    assert set(package['components'])-set(prior)=={'map_room_chain_semantics'}
    for key,row in package['components'].items():
        data=(ROOT/row['file']).read_bytes();assert sha(data)==row['sha256'] and json.loads(data)==package[key]
        if key in prior:assert row==prior[key]
    assert package['map_room_chain_semantics']==value
    assert package['effective_catalogue_layouts']['Rooms']==package['room_writer_semantics']['effective_room_layout']
    assert package['effective_catalogue_layouts']['Terrain']==package['terrain_opaque_table_semantics']['effective_terrain_layout']
    assert package['effective_shot_parameters']==package['area_damage_semantics']['effective_shot_parameters']
    assert package['effective_enums']['Creature.ResistanceCategory']==package['combat_category_semantics']['native_resistance_categories']
    assert package['outstanding_controls_from_phases114_115']==0
    c=value['counts'];assert c['native_scenes']==11 and c['full_Map_callbacks']==71
    assert c['whole_native_Room_loads']==c['whole_native_Terrain_loads']==22 and c['whole_native_Object_loads']==66
    assert c['native_Object_pool_initializers']==11 and c['composed_Creature_entry_executions']==0
    assert c['Ghidra_compared_regions']==264 and c['Ghidra_compared_bytes']==1321324
    assert c['paired_complete_memory_controls']==4 and c['paired_compared_bytes']==1321452
    assert c['executed_function_spans']==207 and c['unique_executed_original_PCs']==16514 and c['primary_blocks']==9
    assert c['new_exact_Map_container_patches']==110 and c['preserved_source_files']==199 and c['preserved_executable_paths']==7
    assert c['newly_named_opaque_fields']==c['source_writes']==0 and value['primary']['all_executed_original_PCs_covered']
    by_name={r['case']:r['outcome'] for r in value['native_executions']['replays']}
    assert by_name['square']['masks']['2,2']==255 and by_name['square']['full_native_remesh_calls']==2
    assert by_name['missing-diagonal']['masks']['2,2']==253 and by_name['missing-diagonal']['full_native_remesh_calls']==1
    assert all(p['all_other_prepared_bytes_equal'] for p in value['native_executions']['paired_controls'])
    assert value['MapMID']['fourth_raw_byte_still_opaque']
    assert 'Актуальный этап 138:' in (ROOT/'INTEGRATION_RU.md').read_text(encoding='utf8')
    assert 'пакет содержит 117 компонентов' in (ROOT/'READINESS_RU.md').read_text(encoding='utf8')
    output=dict(phase=138,date='2026-10-30',**c,previous116_components_preserved_byte_exact=True,package_components=117,
        complete_Map_callback_executed=True,complete_native_KLD_reader_executed=False,complete_World_bootstrap_executed=False,
        real_nonzero_dimension_NAV_remesh_executed=True,all_arena_blocks_accounted_for=True,FS_only_low_page_accesses=True,
        normal_AI_caller_executed=False,final_sacrificed_Creature_removal_executed=False,raw_KLD_fourth_byte_still_opaque=True,
        goal_achieved=False,significant_unknowns_remain=True,limitations=value['limitations'])
    (ROOT/'phase138-validation.json').write_text(json.dumps(output,ensure_ascii=False,indent=2),encoding='utf8')
    status=dict(phase=138,published_phase=138,date='2026-10-30',package_components=117,readiness_estimate_percent=75,
        status='Published complete native71Map Room callbacks, real Room activation/join, decoration lifecycle and changedNAVremesh; rawMapMID remains host projection.',
        previous_goal_turn_classification='Progress: phase137 published26construction7producer cases,5Ghidra controls,82exact Room edits and116components.',
        current_goal_turn_classification='Progress:11scenes/71fullMapcallbacks,4Ghidra controls/4wholememoryflagpairs,207functionspans,110exactMapedits and117components. Corrected incomplete exploratory allocator before accepted cases; real554AD0 gives fullarena accounting and FS-onlylowreads.',
        pending_native_jobs=[],pending_controls=[],goal_achieved=False,significant_unknowns_remain=True,
        evidence=[link(n) for n in ['map-room-chain-semantics.json','MAP_ROOM_CHAIN_RU.md','phase138-map-room-primary.json',
            'phase138-map-room-emulations.json','phase138-map-room-crosschecks.json','phase138-map-room-api-validation.json',
            'phase138-validation.json','INTEGRATION_RU.md','READINESS_RU.md','editor-format-package.json']],
        next_action='Do not repeat71callbacks/4Ghidra/4flagpairs/110Mapedits. Next connect original native56D2D0file reader to51EC60 with genuine external file-I/O boundary (historicalphase28reader patched game function and captured callbacks, not accepted fullchain), or pursue originalnormalAIstate65caller/fresh typed remaining15family unknowns. Full672Roompoolallocation and generalroommerge/split remainopen. Keep rawKLDfourthbyte opaque; no source/parallel editor writes.',
        limitations=value['limitations'])
    (ROOT/'phase138-search-status.json').write_text(json.dumps(status,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:v for k,v in output.items() if k!='limitations'}))

if __name__=='__main__':main()
