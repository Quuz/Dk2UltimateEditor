"""Audit original type-change defect evidence and preserved109component package."""
import json
from build_art_type_editor import ROOT,load,sha,link,build
from build_poison import effective_shot_parameters


def main():
    value=load('art-resource-editor-semantics.json');assert value==build()
    package=load('editor-format-package.json');prior=load('phase130-prior-components.json')
    assert len(package['components'])==109 and len(prior)==108
    assert set(package['components'])-set(prior)=={'art_resource_editor_semantics'}
    for key,row in package['components'].items():
        data=(ROOT/row['file']).read_bytes();assert sha(data)==row['sha256'] and json.loads(data)==package[key]
        if key in prior:assert row==prior[key]
    assert package['art_resource_editor_semantics']==value
    assert package['effective_shot_parameters']==effective_shot_parameters()
    assert package['effective_catalogue_layouts']['Rooms']==package['room_writer_semantics']['effective_room_layout']
    assert package['effective_object_resource_bindings']['slots'][2]['display_name']=='EditorMapIconResource'
    assert package['effective_enums']['Creature.ResistanceCategory']==package['combat_category_semantics']['native_resistance_categories']
    for family,layout in package['combat_category_semantics']['effective_creature_layouts'].items():
        assert package['effective_catalogue_layouts'][family]==layout
    assert package['outstanding_controls_from_phases114_115']==0
    counts=value['counts'];assert counts['native_compositions']==22 and counts['type_helper_calls']==20
    assert counts['stack_copy_transitions']==12 and counts['known_same_type_no_copy']==8
    assert counts['original_writer_compositions']==4 and counts['exact_record_bytes_verified']==4220
    assert counts['entire_Room_models_preserved_except_resource_bytes']==5408
    assert counts['independent_Ghidra_CPU_controls']==3 and counts['Ghidra_compared_regions']==164 and counts['Ghidra_compared_bytes']==871149
    assert value['type_change']['unassigned_source_bytes']==83
    assert value['UI_binding']['type7_numeric_label']=='ID' and value['UI_binding']['procedure_field_control']==1660
    assert len(value['primary']['fresh_functions'])==11 and len(value['primary']['blocks'])==7
    assert all(not r['unlisted_intervals'] for r in value['primary']['fresh_functions'])
    assert value['other_editor_static_projection']['stock']['identical_to_Pro']
    assert value['other_editor_static_projection']['RC3']['instructions']==46
    assert len(value['other_editor_static_projection']['RC3']['changed_instruction_immediates'])==3
    assert 'Актуальный этап 130:' in (ROOT/'INTEGRATION_RU.md').read_text(encoding='utf8')
    assert 'пакет содержит 109 компонентов' in (ROOT/'READINESS_RU.md').read_text(encoding='utf8')
    output=dict(phase=130,date='2026-10-29',native_compositions=22,type_helper_calls=20,
        stack_copy_transitions=12,known_same_type_no_copy=8,unassigned_local_source_bytes=83,
        original_writer_compositions=4,complete_record_bytes_verified=4220,complete_model_bytes_verified=5408,
        output_containers_reparsed_and_repacked=4,independent_Ghidra_CPU_controls=3,Ghidra_compared_regions=164,
        Ghidra_compared_bytes=871149,new_whole_function_spans=11,inherited_key_spans=31,fresh_data_blocks=7,
        new_corpus_API_rebuilds=0,original_Room_source_files_preserved=82,original_executable_paths_preserved=7,
        source_writes=0,previous108_components_preserved_byte_exact=True,package_components=109,
        original_GUI_or_whole_OK_DDX_executed=False,new_stock_or_RC3_CPU_controls=0,
        opaque_procedural_DWORD_meanings_resolved=False,goal_achieved=False,significant_unknowns_remain=True,
        limitations=value['limitations'])
    (ROOT/'phase130-validation.json').write_text(json.dumps(output,ensure_ascii=False,indent=2),encoding='utf8')
    status=dict(phase=130,published_phase=130,date='2026-10-29',package_components=109,readiness_estimate_percent=75,
        status='Published original Pro ArtResource type-change stack residue defect and stable-type preservation; opaque union meanings remain unresolved.',
        previous_goal_turn_classification='Progress:phase129published raw procedural selector consumer,20native/3Ghidra controls and2320exact rebuilds.',
        current_goal_turn_classification='Progress:22native helpers/model/save paths and3Ghidra controls prove unassigned stack-to-file propagation and known same-type preservation;109component publication.',
        pending_native_jobs=[],pending_controls=[],goal_achieved=False,significant_unknowns_remain=True,
        evidence=[link(n) for n in ['art-resource-editor-semantics.json','ART_RESOURCE_EDITOR_RU.md','phase130-art-type-primary.json',
            'phase130-art-type-emulations.json','phase130-art-type-crosschecks.json','phase130-validation.json',
            'INTEGRATION_RU.md','READINESS_RU.md','editor-format-package.json']],
        next_action='Do not repeat the completed SetType stack-pattern cases or4Room writer variants. This phase doesnot assign opaqueDWORD semantics or show every historical value is residue. Pursue procedural selector3 typed runtime context or fresh remaining field consumers; alternatively follow the exact native name setter41BC50/GUI constraint boundary if a new lossless name-length issue needs resolving. Preserve prior ElementB1B2/Traps204/Doors236/Terrain/Creature unknown status. No original source or parallel editor changes.',
        limitations=value['limitations'])
    (ROOT/'phase130-search-status.json').write_text(json.dumps(status,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:v for k,v in output.items() if k!='limitations'}))


if __name__=='__main__':main()
