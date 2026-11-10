"""Audit corrected resistance taxonomy, actual CPU evidence and lossless publication."""
import json
from build_combat_categories import ROOT, load, link, build, sha
from build_poison import effective_shot_parameters


def main():
    value = load('combat-category-semantics.json')
    assert value == build()
    package = load('editor-format-package.json')
    prior = load('phase125-prior-components.json')
    assert len(package['components']) == 104 and len(prior) == 103
    assert set(package['components']) - set(prior) == {'combat_category_semantics'}
    for key, row in package['components'].items():
        blob = (ROOT / row['file']).read_bytes()
        assert sha(blob) == row['sha256'] and json.loads(blob) == package[key]
        if key in prior:
            assert row == prior[key]
    assert package['combat_category_semantics'] == value
    assert package['effective_shot_parameters'] == effective_shot_parameters()
    assert package['effective_object_resource_bindings']['slots'][2]['display_name'] == 'EditorMapIconResource'
    assert package['effective_catalogue_layouts']['Rooms'] == package['room_writer_semantics']['effective_room_layout']
    assert package['outstanding_controls_from_phases114_115'] == 0
    assert package['effective_enums']['Creature.ResistanceCategory'] == value['native_resistance_categories']
    assert package['effective_enums']['Shot.AttackCategory'] == value['native_shot_categories']
    assert package['enums']['Creature.AttackType']['6'] == 'GAS_MISSILE'
    assert package['effective_enums']['Creature.ResistanceCategory']['6'] == 'LIGHTNING'
    for family, layout in value['effective_creature_layouts'].items():
        assert package['effective_catalogue_layouts'][family] == layout
        old = package['catalogue_layouts'][family]
        changed = [index for index, (a, b) in enumerate(zip(layout['fields'], old['fields'])) if a != b]
        assert len(changed) == 8
        for index in changed:
            a, b = layout['fields'][index], old['fields'][index]
            assert a['name'] == b['name'] and a['offset'] == b['offset'] and a['size'] == b['size']
            assert a['name'].startswith('resistances[')
    assert len(value['selected_native_paths']) == 9 and len(value['editor_native_cases']) == 23
    assert all(row['outcome']['HP'] == 980 and row['outcome']['category'] == row['outcome']['resistance_type'] for row in value['selected_native_paths'])
    controls = value['independent_Ghidra_controls']
    assert len(controls) == 6 and sum(row['checked_regions'] for row in controls) == 164
    assert sum(row['checked_bytes'] for row in controls) == 368367
    primary = load('phase125-primary-manifest.json')
    assert len(primary['functions']) == 16 and len(primary['blocks']) == 5
    assert 'Актуальный этап 125:' in (ROOT / 'INTEGRATION_RU.md').read_text(encoding='utf8')
    assert 'пакет содержит 104 компонента' in (ROOT / 'READINESS_RU.md').read_text(encoding='utf8')
    corpus = load('phase125-category-api-validation.json')
    assert sorted(int(k) for k in corpus['type_counts'] if int(k) > 15) == [16, 30, 32]
    assert corpus['exact_container_rebuilds'] == 410 and corpus['changed_containers'] == 328
    output = dict(phase=125, date='2026-10-29', native_resistance_UI_categories=list(range(16)),
        shared_Shot_resistance_categories=list(range(4, 16)), same_IDs_labels_and_string_pointers=True,
        corrected_Creature_fields=8, corrected_Creature_layout_variants=2,
        native_editor_cases=23, matched_raw_to_damage_cases=9,
        independent_Ghidra_CPU_controls=6, Ghidra_compared_regions=164, Ghidra_compared_bytes=368367,
        key_editor_function_spans=16, original_editor_data_blocks=5,
        resistance_0_100_native_UI_validation=True, runtime_percentage_formula_proved=False,
        native_UI_OK_none_clears_value_static=True, whole_MFC_UI_commit_executed=False,
        unknown_source_categories=[16, 30, 32], original_Creature_files_preserved=82,
        Creature_records_inspected=3734, resistance_slots_inspected=14936,
        exact_container_rebuilds=410, changed_containers=328, identity_containers=82,
        original_executable_paths_preserved=7, source_writes=0, game_patch_implemented=False,
        previous103_components_preserved_byte_exact=True, package_components=104,
        goal_achieved=False, significant_unknowns_remain=True, limitations=value['limitations'])
    (ROOT / 'phase125-validation.json').write_text(json.dumps(output, ensure_ascii=False, indent=2), encoding='utf8')
    status = dict(phase=125, published_phase=125, date='2026-10-29',
        status='Corrected resistance spell-list interpretation using native Editor tables; published matched-category damage evidence.',
        previous_goal_turn_classification='no progress: user question answered from existing primary files; no new experiment or authoritative format artifact.',
        current_goal_turn_classification='progress: completed phase124 publication and discovered primary native category correspondence;32new native executions/6Ghidra controls/410rebuilds and104component package.',
        pending_native_jobs=[], pending_controls=[], readiness_estimate_percent=75, package_components=104,
        evidence=[link(name) for name in ['combat-category-semantics.json', 'COMBAT_CATEGORIES_RU.md',
            'phase125-primary-manifest.json', 'phase125-matched-category-emulations.json',
            'phase125-editor-combat-emulations.json', 'phase125-combat-type-crosschecks.json',
            'phase125-category-api-validation.json', 'phase125-validation.json',
            'INTEGRATION_RU.md', 'READINESS_RU.md', 'editor-format-package.json']],
        next_action='Do not repeat the completed category/Shot experiments. Investigate unknown source types16/30/32 using fresh primary version/editor or runtime consumers before naming them; native UI mapping only0..15. Remaining physical-field priorities still include Terrain198/224, Room1C4, Traps204, Doors236, CreatureJumpEE0/PriorityF55, KeeperC8 and ElementB1/B2. Prefer new typed consumers rather than growing generic negative scans. No game patch or original-source writes authorized.',
        goal_achieved=False, significant_unknowns_remain=True, limitations=value['limitations'])
    (ROOT / 'phase125-search-status.json').write_text(json.dumps(status, ensure_ascii=False, indent=2), encoding='utf8')
    print(json.dumps({key: val for key, val in output.items() if key != 'limitations'}))


if __name__ == '__main__':
    main()
