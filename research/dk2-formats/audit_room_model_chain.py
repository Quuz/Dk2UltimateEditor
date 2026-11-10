"""Audit real Room model composition, exact records, scoped checks and package."""
import json
from build_room_model_chain import ROOT, load, link, build, sha
from build_poison import effective_shot_parameters


def main():
    value = load('room-model-chain-semantics.json')
    assert value == build()
    package = load('editor-format-package.json')
    previous = load('phase127-prior-components.json')
    assert len(package['components']) == 106 and len(previous) == 105
    assert set(package['components']) - set(previous) == {'room_model_chain_semantics'}
    for key, row in package['components'].items():
        blob = (ROOT / row['file']).read_bytes()
        assert sha(blob) == row['sha256'] and json.loads(blob) == package[key]
        if key in previous:
            assert row == previous[key]
    assert package['room_model_chain_semantics'] == value
    assert package['effective_shot_parameters'] == effective_shot_parameters()
    assert package['effective_object_resource_bindings']['slots'][2]['display_name'] == 'EditorMapIconResource'
    assert package['effective_catalogue_layouts']['Rooms'] == package['room_writer_semantics']['effective_room_layout']
    assert package['effective_enums']['Creature.ResistanceCategory'] == package['combat_category_semantics']['native_resistance_categories']
    for family, layout in package['combat_category_semantics']['effective_creature_layouts'].items():
        assert package['effective_catalogue_layouts'][family] == layout
    assert package['outstanding_controls_from_phases114_115'] == 0
    cases = value['native_executions']
    assert len(cases) == 5 and sum(row['outcome']['records'] for row in cases) == 30
    assert sum(row['outcome']['resource_blocks_checked'] for row in cases) == 330
    assert sum(row['outcome']['model_bytes_checked'] for row in cases) == 40560
    assert all(row['outcome']['output_records_byte_identical'] for row in cases)
    assert [row['outcome']['unknown_resource_index_cache'] for row in cases[1:]] == [9, 1, 65535, 12]
    primary = load('phase127-Room-model-primary-manifest.json')
    assert len(primary['fresh_functions']) == 20 and len(primary['inherited_functions']) == 11
    controls = value['independent_Ghidra_controls']
    assert len(controls) == 2 and sum(row['checked_regions'] for row in controls) == 162
    assert sum(row['checked_bytes'] for row in controls) == 654090
    assert value['preserved_unknown_resource']['meaning'].startswith('unresolved')
    assert value['coverage']['whole_output_files_identical_to_source'] is False
    assert 'Актуальный этап 127:' in (ROOT / 'INTEGRATION_RU.md').read_text(encoding='utf8')
    assert 'пакет содержит 106 компонентов' in (ROOT / 'READINESS_RU.md').read_text(encoding='utf8')
    output = dict(phase=127, date='2026-10-29', whole_native_compositions=5,
        original_catalogue_records=26, diagnostic_unknown_resource_records=4,
        all30_output_records_byte_identical_to_their_raw_inputs=True,
        complete_native_output_containers_reparsed_and_repacked=5,
        source_record_bytes_verified=31650, resource_blocks_verified=330,
        resource_bytes_verified=27720, complete_model_bytes_verified=40560,
        independent_Ghidra_CPU_controls=2, Ghidra_compared_regions=162, Ghidra_compared_bytes=654090,
        Ghidra_unassigned_header_bytes_excluded=[42, 43, 47], all_record_bytes_compared=True,
        key_primary_function_spans=31, fresh_function_spans=20, inherited_function_spans=11,
        original_code_or_vtable_substitutions=0, Win32_IAT_data_bindings=12,
        whole_native_model_construction_registration_and_save_executed=True,
        whole_original_parser_initializer_executed=False, live_GUI_commit_executed=False,
        unknown_Room1C4_meaning='unresolved', original_executable_paths_preserved=7,
        original_Room_files_hashes_preserved=82, Room_files_executed_through_new_chain=1,
        new_corpus_API_rebuilds=0, source_writes=0,
        previous105_components_preserved_byte_exact=True, package_components=106,
        goal_achieved=False, significant_unknowns_remain=True, limitations=value['limitations'])
    (ROOT / 'phase127-validation.json').write_text(json.dumps(output, ensure_ascii=False, indent=2), encoding='utf8')
    status = dict(phase=127, published_phase=127, date='2026-10-29',
        status='Published full original Room raw-converter/allocated-model/CString-registration/writer chains; unknown1C4 preserved, gameplay meaning remains unresolved.',
        previous_goal_turn_classification='progress:phase126RC3tables,old resistance converter/getters/copy14native/2Ghidra cases and105component package.',
        current_goal_turn_classification='progress:closed actual Room model creation/registration gap with26source records/4diagnostic resources,5wholechains/2Ghidra controls and106component publication.',
        pending_native_jobs=[], pending_controls=[], package_components=106, readiness_estimate_percent=75,
        evidence=[link(name) for name in ['room-model-chain-semantics.json', 'ROOM_MODEL_CHAIN_RU.md',
            'phase127-Room-model-primary-manifest.json', 'phase127-Room-model-chain-emulations.json',
            'phase127-Room-model-chain-crosschecks.json', 'phase127-validation.json',
            'INTEGRATION_RU.md', 'READINESS_RU.md', 'editor-format-package.json']],
        next_action='Do not repeat five whole Room chains,26source records or twoGhidra comparisons. Room1C4 has no new gameplay label; direct model280 scan already closed. Prioritize a fresh typed consumer of remaining Terrain198/224, Traps204, Doors236, CreatureJumpEE0/PriorityF55, KeeperC8, ElementB1/B2. New native MFC/CRT allocation setup can support an actually new raw-to-model property normalization question, but avoid duplicating unchanged resource fixtures. Full parser/GUI commit remains separate coverage. No source writes or game patch authorized.',
        goal_achieved=False, significant_unknowns_remain=True, limitations=value['limitations'])
    (ROOT / 'phase127-search-status.json').write_text(json.dumps(status, ensure_ascii=False, indent=2), encoding='utf8')
    print(json.dumps({key: val for key, val in output.items() if key != 'limitations'}))


if __name__ == '__main__':
    main()
