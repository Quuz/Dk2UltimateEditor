"""Audit exact Terrain model roundtrips, boundary fields and preserved package."""
import json
from build_terrain_model_chain import ROOT, load, link, sha, build
from build_poison import effective_shot_parameters


def main():
    value = load('terrain-model-chain-semantics.json')
    assert value == build()
    package = load('editor-format-package.json')
    previous = load('phase128-prior-components.json')
    assert len(package['components']) == 107 and len(previous) == 106
    assert set(package['components']) - set(previous) == {'terrain_model_chain_semantics'}
    for key, row in package['components'].items():
        blob = (ROOT / row['file']).read_bytes()
        assert sha(blob) == row['sha256'] and json.loads(blob) == package[key]
        if key in previous:
            assert row == previous[key]
    assert package['terrain_model_chain_semantics'] == value
    assert package['effective_shot_parameters'] == effective_shot_parameters()
    assert package['effective_catalogue_layouts']['Rooms'] == package['room_writer_semantics']['effective_room_layout']
    assert package['effective_object_resource_bindings']['slots'][2]['display_name'] == 'EditorMapIconResource'
    assert package['effective_enums']['Creature.ResistanceCategory'] == package['combat_category_semantics']['native_resistance_categories']
    for family, layout in package['combat_category_semantics']['effective_creature_layouts'].items():
        assert package['effective_catalogue_layouts'][family] == layout
    assert package['outstanding_controls_from_phases114_115'] == 0
    runs = value['native_executions']
    assert len(runs) == 6 and sum(row['outcome']['records'] for row in runs) == 46
    assert all(row['outcome']['all_record_bytes_preserved'] for row in runs)
    assert sum(row['outcome']['selected_model_field_bytes_verified'] for row in runs) == 1932
    assert sum(row['outcome']['model_bytes_fingerprinted'] for row in runs) == 45264
    controls = value['independent_Ghidra_controls']
    assert len(controls) == 2 and sum(row['checked_regions'] for row in controls) == 192
    assert sum(row['checked_bytes'] for row in controls) == 643316
    assert len(value['source_preservation']['Terrain_sources']) == 82
    assert len(value['source_preservation']['executable_paths']) == 7
    assert all(row['meaning'] == 'unresolved' for row in value['unknown_fields'])
    assert 'Актуальный этап 128:' in (ROOT / 'INTEGRATION_RU.md').read_text(encoding='utf8')
    assert 'пакет содержит 107 компонентов' in (ROOT / 'READINESS_RU.md').read_text(encoding='utf8')
    output = dict(phase=128, date='2026-10-29', whole_native_compositions=6,
        original_source_records=41, diagnostic_records=5, exact_output_records=46,
        complete_record_bytes_verified=25392, selected_model_field_bytes_verified=1932,
        whole_model_bytes_fingerprinted_only=45264, independent_Ghidra_CPU_controls=2,
        Ghidra_compared_regions=192, Ghidra_compared_bytes=643316,
        new_primary_Terrain_function_spans=7, inherited_converter_span=1,
        native_unknown224_high_bit_roundtrip_verified=True,
        unknown_gameplay_fields=['Terrain198', 'Terrain224', 'Terrain1AE'],
        unknown_fields_new_semantic_names=0, original_parser_initializer_executed=False,
        live_GUI_commit_executed=False, original_executable_paths_preserved=7,
        original_Terrain_files_hash_preserved=82, source_files_executed_through_new_chain=1,
        new_native_output_containers_reparsed_repacked=6, new_corpus_API_rebuilds=0,
        source_writes=0, previous106_components_preserved_byte_exact=True, package_components=107,
        goal_achieved=False, significant_unknowns_remain=True, limitations=value['limitations'])
    (ROOT / 'phase128-validation.json').write_text(json.dumps(output, ensure_ascii=False, indent=2), encoding='utf8')
    status = dict(phase=128, published_phase=128, date='2026-10-29',
        status='Published full native Terrain raw/model/save preservation; unknown198/224/16WORD1AE remain unnamed.',
        previous_goal_turn_classification='progress:phase127closed real Room construction/registration gap with26source/4diagnostic records and2Ghidra controls.',
        current_goal_turn_classification='progress:41Terrain source records and5high-bit/array diagnostics through full original model/save,2Ghidra controls and107component publication.',
        pending_native_jobs=[], pending_controls=[], package_components=107, readiness_estimate_percent=75,
        evidence=[link(name) for name in ['terrain-model-chain-semantics.json', 'TERRAIN_MODEL_CHAIN_RU.md',
            'phase128-terrain-model-primary.json', 'phase128-terrain-model-emulations.json',
            'phase128-terrain-model-crosschecks.json', 'phase128-validation.json',
            'INTEGRATION_RU.md', 'READINESS_RU.md', 'editor-format-package.json']],
        next_action='Do not repeat completed41Terrain records/fiveboundarychains/2Ghidra controls or old32model320/3D0literal leads. 198/224/1AE remain unknown; primary goal priority is new semantic consumers, not more preservation counts. Pursue fresh typed clues in Traps204/Doors236/CreatureJumpEE0/PriorityF55/KeeperC8/ElementB1B2 or other unverified fields; new full model infrastructure is available when it can answer a different normalization issue. No original source writes or game patch authorized.',
        goal_achieved=False, significant_unknowns_remain=True, limitations=value['limitations'])
    (ROOT / 'phase128-search-status.json').write_text(json.dumps(status, ensure_ascii=False, indent=2), encoding='utf8')
    print(json.dumps({key: val for key, val in output.items() if key != 'limitations'}))


if __name__ == '__main__':
    main()
