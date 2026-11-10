"""Audit scoped RC3/old resistance evidence, current views and preserved sources."""
import json
from build_resistance_versions import ROOT, load, link, build, sha
from build_poison import effective_shot_parameters


def main():
    value = load('resistance-version-semantics.json')
    assert value == build()
    package = load('editor-format-package.json')
    prior = load('phase126-prior-components.json')
    assert len(package['components']) == 105 and len(prior) == 104
    assert set(package['components']) - set(prior) == {'resistance_version_semantics'}
    for key, row in package['components'].items():
        blob = (ROOT / row['file']).read_bytes()
        assert sha(blob) == row['sha256'] and json.loads(blob) == package[key]
        if key in prior:
            assert row == prior[key]
    assert package['resistance_version_semantics'] == value
    assert package['effective_shot_parameters'] == effective_shot_parameters()
    assert package['effective_object_resource_bindings']['slots'][2]['display_name'] == 'EditorMapIconResource'
    assert package['effective_catalogue_layouts']['Rooms'] == package['room_writer_semantics']['effective_room_layout']
    assert package['effective_enums']['Creature.ResistanceCategory'] == package['combat_category_semantics']['native_resistance_categories']
    for family, layout in package['combat_category_semantics']['effective_creature_layouts'].items():
        assert package['effective_catalogue_layouts'][family] == layout
    assert package['outstanding_controls_from_phases114_115'] == 0
    assert len(value['native_old_executions']) == 14
    assert sum(row['outcome']['kind'] == 'copy' for row in value['native_old_executions']) == 3
    controls = value['independent_Ghidra_controls']
    assert len(controls) == 2 and sum(row['checked_regions'] for row in controls) == 4
    assert sum(row['checked_bytes'] for row in controls) == 16560
    primary = load('phase126-old-primary-manifest.json')
    assert len(primary['functions']) == 16 and len(primary['direct_leads']) == 20
    assert len(value['source_preservation']['executable_paths']) == 7
    assert len(value['source_preservation']['Creature_files']) == 82
    assert value['unknown_types']['meaning'] == 'unresolved'
    assert not value['old_profile']['certified_retail_version']
    assert 'Актуальный этап 126:' in (ROOT / 'INTEGRATION_RU.md').read_text(encoding='utf8')
    assert 'пакет содержит 105 компонентов' in (ROOT / 'READINESS_RU.md').read_text(encoding='utf8')
    output = dict(phase=126, date='2026-10-29', RC3_native_tables_compared=3,
        RC3_resistance_dialog_byte_identical_to_Pro=True, unknown_source_IDs=[16, 30, 32],
        unknown_ID_meanings='unresolved', exact_old_profile_authenticated_as_retail13=False,
        original_old_converter_getter_cases=11, whole_old2199byte_copy_cases=3,
        independent_Ghidra_CPU_controls=2, Ghidra_compared_regions=4, Ghidra_compared_bytes=16560,
        old_primary_function_spans=16, old_direct_operand_leads=20,
        old_direct_lead_contexts=11, full_old_damage_path_executed=False,
        full_old_raw_to_World_chain_executed=False, new_RC3_GUI_or_CPU_controls=0,
        original_executable_paths_preserved=7, original_Creature_files_preserved=82,
        new_container_rebuilds=0, source_writes=0, previous104_components_preserved_byte_exact=True,
        package_components=105, goal_achieved=False, significant_unknowns_remain=True,
        limitations=value['limitations'])
    (ROOT / 'phase126-validation.json').write_text(json.dumps(output, ensure_ascii=False, indent=2), encoding='utf8')
    status = dict(phase=126, published_phase=126, date='2026-10-29',
        status='Published old-PE forward preservation of unknown resistance IDs and bounded RC3/table/direct-operand compatibility.',
        previous_goal_turn_classification='progress:phase125corrected primary resistance category taxonomy,9matched damage chains/23editor executions/6Ghidra controls and410exact rebuilds.',
        current_goal_turn_classification='progress:3RC3table/resource comparisons,16oldPEspans/20canonical leads,14old-native cases/2Ghidra controls and105component publication;old analysis server ready.',
        pending_native_jobs=[], pending_controls=[], package_components=105, readiness_estimate_percent=75,
        evidence=[link(name) for name in ['resistance-version-semantics.json', 'RESISTANCE_VERSIONS_RU.md',
            'phase126-resistance-editor-version-tables.json', 'phase126-old-primary-manifest.json',
            'phase126-old-resistance-emulations.json', 'phase126-old-resistance-crosschecks.json',
            'phase126-validation.json', 'INTEGRATION_RU.md', 'READINESS_RU.md', 'editor-format-package.json']],
        next_action='Do not repeat phase125matched damage cases,14old storage cases/2Ghidra controls or20old direct resistance literals. Unknown16/30/32stayopaque. The separately imported/analyzed oldPEserver8091can be reused for genuinely new typed consumers of remaining Terrain198/224, Room1C4, Traps204, Doors236, CreatureJumpEE0/PriorityF55, KeeperC8 and ElementB1/B2; do not repeat old phase47Jump/Priority literal scans. A full old combat chain remains unexecuted, but prioritize fresh field meaning rather than compatibility-count growth. No source writes or game patch authorized.',
        goal_achieved=False, significant_unknowns_remain=True, limitations=value['limitations'])
    (ROOT / 'phase126-search-status.json').write_text(json.dumps(status, ensure_ascii=False, indent=2), encoding='utf8')
    print(json.dumps({key: val for key, val in output.items() if key != 'limitations'}))


if __name__ == '__main__':
    main()
