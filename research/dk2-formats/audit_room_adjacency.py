"""Audit116component publication and bounded native Room adjacency evidence."""
import json
from build_room_adjacency import ROOT,load,sha,link,build

def main():
    value=load('room-adjacency-semantics.json');assert value==build()
    package=load('editor-format-package.json');prior=load('phase137-prior-components.json')
    assert len(package['components'])==116 and len(prior)==115
    assert set(package['components'])-set(prior)=={'room_adjacency_semantics'}
    for key,row in package['components'].items():
        data=(ROOT/row['file']).read_bytes();assert sha(data)==row['sha256'] and json.loads(data)==package[key]
        if key in prior:assert row==prior[key]
    assert package['room_adjacency_semantics']==value
    for key in ['effective_catalogue_layouts','effective_enums','effective_shot_parameters','effective_object_resource_bindings']:
        assert key in package
    assert package['effective_catalogue_layouts']['Rooms']==package['room_writer_semantics']['effective_room_layout']
    assert package['effective_catalogue_layouts']['Terrain']==package['terrain_opaque_table_semantics']['effective_terrain_layout']
    assert package['effective_shot_parameters']==package['area_damage_semantics']['effective_shot_parameters']
    assert package['effective_enums']['Creature.ResistanceCategory']==package['combat_category_semantics']['native_resistance_categories']
    assert package['outstanding_controls_from_phases114_115']==0
    c=value['counts'];assert c['full_native_producer_paths']==26 and c['rawload_baselines']==5
    assert c['raw_Room_loads']==c['raw_Terrain_loads']==c['raw_Creature_loads']==c['raw_recipe_callbacks']==31
    assert c['Ghidra_compared_regions']==240 and c['Ghidra_compared_bytes']==1147255
    assert c['checked_scene_bytes']==5965726 and c['executed_function_spans']==104 and c['unique_executed_original_PCs']==9049
    assert c['new_exact_container_rebuilds']==82 and c['preserved_source_files']==89 and c['preserved_executable_paths']==7
    assert c['newly_named_opaque_fields']==c['source_writes']==0
    assert value['primary']['all_executed_original_PCs_covered']
    assert value['map_projection']['raw_KLD_fourth_byte_still_opaque']
    assert not value['map_projection']['neighbour_room_tag_compared']
    ex=value['composed_temple_examples'];assert ex['entry-all']['accepted_sacrifice'] and ex['entry-all']['HP']==0 and ex['entry-all']['mana']==55
    assert ex['entry-missing-diagonal']['cellC']==253 and ex['entry-missing-diagonal']['transition_to67'] and ex['entry-missing-diagonal']['HP']==1000
    assert ex['entry-other-owner']['cellC']==0 and ex['entry-other-owner']['transition_to67']
    assert all(not r['outcome']['full_remesh_executed'] for r in value['native_executions']['replays'])
    assert len(value['diagnostic_exclusions'])==2 and all(not r['passing_coverage'] for r in value['diagnostic_exclusions'])
    assert 'Актуальный этап 137:' in (ROOT/'INTEGRATION_RU.md').read_text(encoding='utf8')
    assert 'пакет содержит 116 компонентов' in (ROOT/'READINESS_RU.md').read_text(encoding='utf8')
    output=dict(phase=137,date='2026-10-30',**c,previous115_components_preserved_byte_exact=True,package_components=116,
        native_construction7_producer_executed=True,full_native_KLD_World_bootstrap_executed=False,changed_navmesh_rebuild_executed=False,
        normal_AI_caller_executed=False,final_entity_unlink_executed=False,raw_KLD_fourth_byte_still_opaque=True,
        goal_achieved=False,significant_unknowns_remain=True,limitations=value['limitations'])
    (ROOT/'phase137-validation.json').write_text(json.dumps(output,ensure_ascii=False,indent=2),encoding='utf8')
    status=dict(phase=137,published_phase=137,date='2026-10-30',package_components=116,readiness_estimate_percent=75,
        status='Published rawRoom/Terrain -> actual construction7 adjacency -> selected Temple Creature entry, with bounded UI/NAV providers and exact source preservation.',
        previous_goal_turn_classification='No progress: previous percentage answer inspected current reports and restated75%; it added no reverse-engineering evidence. Resumed unfinished phase137producer lead safely.',
        current_goal_turn_classification='Progress:26native producer paths,5rawload baselines,5Ghidra controls,104function spans,82exact container edits and116component publication; replaces preparedTemple cellC constant by original producer.',
        pending_native_jobs=[],pending_controls=[],goal_achieved=False,significant_unknowns_remain=True,
        evidence=[link(n) for n in ['room-adjacency-semantics.json','ROOM_ADJACENCY_RU.md','phase137-room-adjacency-primary.json',
            'phase137-room-adjacency-emulations.json','phase137-room-adjacency-crosschecks.json','phase137-room-adjacency-api-validation.json',
            'phase137-validation.json','INTEGRATION_RU.md','READINESS_RU.md','editor-format-package.json']],
        next_action='Do not repeat26producer/5Ghidra controls or82construction edits. Connect original51EC60 KLD callback/44FC40/4C5C30 room placement to this producer, or trace normalAI state65activation; fullchanged-nav4CE770 and finalWorldunlink remain open. Keep rawKLD fourthbyte opaque. Prioritize fresh typed consumers of remaining15family unknowns; no original/parallel editor writes.',
        limitations=value['limitations'])
    (ROOT/'phase137-search-status.json').write_text(json.dumps(status,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:v for k,v in output.items() if k!='limitations'}))

if __name__=='__main__':main()
