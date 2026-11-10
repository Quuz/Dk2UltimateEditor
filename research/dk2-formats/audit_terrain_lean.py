"""Audit mixed-occupancy Lean and all prior112component package provenance."""
import json
from build_terrain_lean import ROOT,load,sha,link,build,effective_terrain_layout
from build_area_damage import effective_shot_parameters
from validate_terrain_lean_edit import validate


def main():
    value=load('terrain-lean-semantics.json');assert value==build()
    assert load('phase133-Lean-api-validation.json')==validate()
    package=load('editor-format-package.json');prior=load('phase133-prior-components.json')
    assert len(package['components'])==112 and len(prior)==111
    assert set(package['components'])-set(prior)=={'terrain_lean_semantics'}
    for key,row in package['components'].items():
        data=(ROOT/row['file']).read_bytes();assert sha(data)==row['sha256'] and json.loads(data)==package[key]
        if key in prior:assert row==prior[key]
    assert package['terrain_lean_semantics']==value
    assert package['effective_shot_parameters']==effective_shot_parameters()
    assert package['effective_catalogue_layouts']['Terrain']==effective_terrain_layout()
    before=package['procedural_cursor_semantics']['effective_terrain_layout'];after=package['effective_catalogue_layouts']['Terrain']
    changed=0
    for a,b in zip(before['fields'],after['fields']):
        assert all(a[k]==b[k] for k in ['offset','size','name','kind'])
        if a!=b:assert a['offset'] in [0x1CF,0x1D0,0x1D1,0x1D3,0x1D4,0x1D5];changed+=1
    assert changed==6
    assert package['effective_catalogue_layouts']['Rooms']==package['room_writer_semantics']['effective_room_layout']
    assert package['effective_enums']['Creature.ResistanceCategory']==package['combat_category_semantics']['native_resistance_categories']
    assert package['outstanding_controls_from_phases114_115']==0
    c=value['counts'];assert c['full_raw_to_cursor_compositions']==22 and c['isolated_Lean_native_controls']==4 and c['raw_to_World_occupancy_native_controls']==2
    assert c['full_path_World220_calls']==249 and c['independently_predicted_complete_output_bytes']==33264
    assert c['derived_input_bytes_preserved_from_measured_prestate']==1584
    assert c['independent_Ghidra_CPU_controls']==6 and c['Ghidra_compared_regions']==150 and c['Ghidra_compared_bytes']==859842
    assert c['new_function_spans']==5 and c['inherited_function_spans']==51 and c['unique_executed_original_PCs']==4100
    assert value['primary']['all_executed_original_PCs_covered']
    assert 'Актуальный этап 133:' in (ROOT/'INTEGRATION_RU.md').read_text(encoding='utf8')
    assert 'пакет содержит 112 компонентов' in (ROOT/'READINESS_RU.md').read_text(encoding='utf8')
    output=dict(phase=133,date='2026-10-30',**c,previous111_components_preserved_byte_exact=True,package_components=112,
        effective_Lean_fields_annotated=6,historical_schemas_unchanged=True,actual_mixed_World_occupancy_executed=True,
        entire_selected_final_grids_predicted=True,complete_Ghidra_FSIN_frame_supported=False,heterogeneous_Terrain_native_execution=False,
        unknown_Terrain198_224_1AE_meanings_resolved=False,goal_achieved=False,significant_unknowns_remain=True,
        limitations=value['limitations'])
    (ROOT/'phase133-validation.json').write_text(json.dumps(output,ensure_ascii=False,indent=2),encoding='utf8')
    status=dict(phase=133,published_phase=133,date='2026-10-30',package_components=112,readiness_estimate_percent=75,
        status='Published actual mixed World/CMap occupancy,3orderedLean layers, boundary interpolation and complete selected final grid model.',
        previous_goal_turn_classification='Progress:phase132 published procedural cursor3 and rawTerrain sample consumers;111components with2explicitCPU limitations.',
        current_goal_turn_classification='Progress:22full raw→cursor paths,6native control compositions/6Ghidra controls,complete27vertex predictions,fivefresh functions and82exact container edits;112component publication.',
        pending_native_jobs=[],pending_controls=[],goal_achieved=False,significant_unknowns_remain=True,
        evidence=[link(n) for n in ['terrain-lean-semantics.json','TERRAIN_LEAN_RU.md','phase133-Lean-primary.json',
            'phase133-Lean-emulations.json','phase133-Lean-control-emulations.json','phase133-Lean-output-validation.json',
            'phase133-Lean-crosschecks.json','phase133-Lean-api-validation.json','phase133-validation.json',
            'INTEGRATION_RU.md','READINESS_RU.md','editor-format-package.json']],
        next_action='Do not repeat22mixednav/6control cases or82combinedLean edits. SixLean byte consumers/layer mapping are established for selected uniformTerrain scenes. Prioritize fresh typed consumers of genuinely unresolvedTerrain198/224/16WORD1AE,Traps204,Doors236,CreatureJump/Priority,KeeperC8,spell/Elementtails. If following59E3A0/59E400 min4 withheterogeneousTerrain, distinguish new blend semantics from already completed uniform cases. FullGhidraFSIN/GPU and normal cursorcaller remain outside scope. No original/parallel editor writes.',
        limitations=value['limitations'])
    (ROOT/'phase133-search-status.json').write_text(json.dumps(status,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:v for k,v in output.items() if k!='limitations'}))


if __name__=='__main__':main()
