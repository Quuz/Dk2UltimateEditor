"""Audit full-file normal/DX identity, actual CPU evidence and package preservation."""
import json
from build_game_executable_compatibility import ROOT, load, link, build, variants, api
from build_poison import effective_shot_parameters


def main():
    value = load('game-executable-compatibility.json')
    assert value == build()
    package = load('editor-format-package.json')
    prior = load('phase124-prior-components.json')
    assert len(package['components']) == 103 and len(prior) == 102
    assert set(package['components']) - set(prior) == {'game_executable_compatibility'}
    for key, row in package['components'].items():
        blob = (ROOT / row['file']).read_bytes()
        assert variants.sha(blob) == row['sha256'] and json.loads(blob) == package[key]
        if key in prior:
            assert row == prior[key]
    assert package['game_executable_compatibility'] == value
    assert package['effective_shot_parameters'] == effective_shot_parameters()
    assert package['effective_catalogue_layouts']['Rooms'] == package['room_writer_semantics']['effective_room_layout']
    assert package['effective_object_resource_bindings']['slots'][2]['display_name'] == 'EditorMapIconResource'
    assert package['outstanding_controls_from_phases114_115'] == 0
    assert 'Актуальный этап 124:' in (ROOT / 'INTEGRATION_RU.md').read_text(encoding='utf8')
    assert 'пакет содержит 103 компонента' in (ROOT / 'READINESS_RU.md').read_text(encoding='utf8')
    comparison = value['exhaustive_byte_comparison']
    assert comparison['exact_file_differences']['count'] == 5
    assert comparison['all_other_file_bytes_identical']
    assert all(row['identical'] for row in comparison['sections'] if row['executable'])
    assert [row['instruction_count'] for row in value['native_normal_EXE_controls']] == [8884, 268, 274]
    assert all(row['tracked_memory_and_instruction_trace_equal'] for row in value['native_normal_EXE_controls'])
    assert value['new_Ghidra_CPU_controls'] == 0
    assert len(value['original_executable_paths_preserved']) == 7
    patch = comparison['patch_DLL']
    from pathlib import Path
    assert variants.sha(Path(patch['file']).read_bytes()) == patch['sha256']
    assert patch['sha256'] == 'cb74d7f78f831b6aff0aa37271c9525ac9e97c4e1522a71cc79a3654a9cc26be'
    refused = 0
    for invalid in [None, 0, '', '0' * 64, '93e826a142f677ef5120699feddb0572799d93399167ef1d05ec4b48a8b2a342']:
        try:
            api.static_definition_profile(invalid)
        except ValueError:
            refused += 1
        else:
            raise AssertionError('Unknown executable profile accepted')
    output = dict(phase=124, date='2026-10-29', exact_whole_file_different_bytes=5,
        executable_sections_byte_identical=True, new_actual_normal_EXE_CPU_controls=3,
        new_normal_Ghidra_CPU_controls=0, original_executable_paths_preserved=7,
        patch_DLL_preserved=True, patch_DLL_runtime_audited=False,
        unknown_executable_profile_refusals=refused, editor_primary_function_ranges=8,
        DX_import_primary_blocks=2, Keeper_C8_meaning='unresolved',
        new_container_rebuilds=0, source_writes=0,
        previous102_components_preserved_byte_exact=True, package_components=103,
        goal_achieved=False, significant_unknowns_remain=True, limitations=value['limitations'])
    (ROOT / 'phase124-validation.json').write_text(json.dumps(output, ensure_ascii=False, indent=2), encoding='utf8')
    status = dict(phase=124, published_phase=124, date='2026-10-29',
        status='Published exact normal/DX static code profile identity; KeeperC8 remains unresolved.',
        previous_goal_turn_classification='no progress toward format specification: answered the damage-category question using existing evidence; no new controls or authoritative artifacts.',
        current_goal_turn_classification='progress: completed normal/DX whole-file evidence, 3 actual normal-PE controls, scoped KeeperC8 review and 103-component publication.',
        pending_native_jobs=[], pending_controls=[], package_components=103, readiness_estimate_percent=75,
        evidence=[link(n) for n in ['game-executable-compatibility.json', 'GAME_EXECUTABLES_RU.md',
            'phase124-game-executable-variants.json', 'phase124-normal-executable-controls.json',
            'phase124-validation.json', 'INTEGRATION_RU.md', 'READINESS_RU.md', 'editor-format-package.json']],
        next_action='Seek a new typed consumer or write/read path for remaining unknown fields. Do not repeat completed normal-PE controls, the 43 Keeper model178 literals or closed Room280 literals without a new lead. Remaining Terrain198/224, Room1C4, Traps204, Doors236, CreatureJumpEE0/PriorityF55, KeeperC8, AP flags and ElementB1/B2 are unresolved. Analyze actual combat-category-to-resistance type identity if new primary evidence is available; do not build an unauthorized game patch.',
        goal_achieved=False, significant_unknowns_remain=True, limitations=value['limitations'])
    (ROOT / 'phase124-search-status.json').write_text(json.dumps(status, ensure_ascii=False, indent=2), encoding='utf8')
    print(json.dumps({k: v for k, v in output.items() if k != 'limitations'}))


if __name__ == '__main__':
    main()
