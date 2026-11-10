"""Audit cursor/Terrain consumer evidence without hiding CPU diagnostics."""
import json
from build_cursor_procedure import ROOT,load,sha,link,build,effective_terrain_layout
from build_area_damage import effective_shot_parameters
from validate_cursor_deformation import validate


def main():
    value=load('procedural-cursor-semantics.json');assert value==build()
    assert load('phase132-deformation-api-validation.json')==validate()
    package=load('editor-format-package.json');prior=load('phase132-prior-components.json')
    assert len(package['components'])==111 and len(prior)==110
    assert set(package['components'])-set(prior)=={'procedural_cursor_semantics'}
    for key,row in package['components'].items():
        data=(ROOT/row['file']).read_bytes();assert sha(data)==row['sha256'] and json.loads(data)==package[key]
        if key in prior:assert row==prior[key]
    assert package['procedural_cursor_semantics']==value
    assert package['effective_shot_parameters']==effective_shot_parameters()
    assert package['effective_catalogue_layouts']['Terrain']==effective_terrain_layout()
    old=package['catalogue_layouts']['Terrain'];current=package['effective_catalogue_layouts']['Terrain']
    assert len(old['fields'])==len(current['fields'])
    changed=0
    for before,after in zip(old['fields'],current['fields']):
        assert all(before[k]==after[k] for k in ['offset','size','kind','name'])
        if before!=after:assert 0x1CE<=before['offset']<=0x1D5;changed+=1
    assert changed==8
    assert package['effective_catalogue_layouts']['Rooms']==package['room_writer_semantics']['effective_room_layout']
    assert package['effective_enums']['Creature.ResistanceCategory']==package['combat_category_semantics']['native_resistance_categories']
    assert package['outstanding_controls_from_phases114_115']==0
    c=value['counts'];assert c['native_cursor_compositions']==28 and c['native_Terrain_sampler_compositions']==2
    assert c['selected_line_segments']==536 and c['native_meshes']==1072
    assert c['independent_Ghidra_CPU_controls']==4 and c['Ghidra_compared_regions']==238 and c['Ghidra_compared_bytes']==1587972
    assert c['retained_second_CPU_limit_cases']==2 and c['full_function_spans']==56 and c['data_blocks']==5 and c['unique_executed_original_PCs']==4361
    assert value['primary']['all_executed_original_PCs_covered']
    assert 'Актуальный этап 132:' in (ROOT/'INTEGRATION_RU.md').read_text(encoding='utf8')
    assert 'пакет содержит 111 компонентов' in (ROOT/'READINESS_RU.md').read_text(encoding='utf8')
    output=dict(phase=132,date='2026-10-30',**c,previous110_components_preserved_byte_exact=True,package_components=111,
        effective_Terrain_fields_annotated=8,historical_schemas_unchanged=True,
        native_selector3_executed=True,full_native_generic_wrapper_executed=True,
        Ghidra_fractional_floor_matches_native=False,Ghidra_full_deformed_frame_supported=False,
        full_game_engine_or_GPU_executed=False,unknown_Terrain198_224_1AE_meanings_resolved=False,
        goal_achieved=False,significant_unknowns_remain=True,limitations=value['limitations'])
    (ROOT/'phase132-validation.json').write_text(json.dumps(output,ensure_ascii=False,indent=2),encoding='utf8')
    status=dict(phase=132,published_phase=132,date='2026-10-30',package_components=111,readiness_estimate_percent=75,
        status='Published native procedural selector3/generic wrapper and rawTerrain Wibble/Lean consumers; CPU limitations explicit.',
        previous_goal_turn_classification='Progress:phase131 published actual multi-target falloff defect,27native/4Ghidra controls and246exact edits; previous continuation inspected remaining leads without claiming new semantics.',
        current_goal_turn_classification='Progress:28full native cursor paths plus2rawTerrain samplers,4matched Ghidra controls/2retained limitations,56whole functions and656exact edits;111component publication.',
        pending_native_jobs=[],pending_controls=[],goal_achieved=False,significant_unknowns_remain=True,
        evidence=[link(n) for n in ['procedural-cursor-semantics.json','PROCEDURAL_CURSOR_RU.md','phase132-cursor-primary.json',
            'phase132-cursor-emulations.json','phase132-Terrain-samples-emulations.json','phase132-cursor-crosschecks.json',
            'phase132-deformation-api-validation.json','phase132-validation.json','INTEGRATION_RU.md','READINESS_RU.md','editor-format-package.json']],
        next_action='Do not repeat28cursor/2sampler runs or656edits. No camera dependency or integer-Lean truncation defect: both were rejected by original instructions. To finish Lean behavior, prepare valid mixed occupancy/actual World220 path and trace59F4B0 rather than claiming uniform-context nonuse. Alternative priority is a fresh typed consumer for remaining Terrain198/224/1AE, Traps204, Doors236, CreatureJump/Priority, KeeperC8, spell/Element tails. Current Ghidra FRNDINT/FSIN limits are terminal diagnostics, not live jobs. Preserve all unknowns and full15family objective; no source/parallel-editor patch.',
        limitations=value['limitations'])
    (ROOT/'phase132-search-status.json').write_text(json.dumps(status,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:v for k,v in output.items() if k!='limitations'}))


if __name__=='__main__':main()
