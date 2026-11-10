"""Audit icon meaning, actual CPU evidence, exact rebuilds and package history."""
from pathlib import Path
import json
from build_editor_object_icons import ROOT,load,build,link,native
from build_poison import effective_shot_parameters

def main():
    value=load('editor-object-icon-semantics.json');assert value==build()
    package=load('editor-format-package.json');prior=load('phase121-prior-components.json')
    assert len(package['components'])==100 and len(prior)==99
    assert set(package['components'])-set(prior)=={'editor_object_icon_semantics'}
    assert package['editor_object_icon_semantics']==value
    for k,row in package['components'].items():
        b=(ROOT/row['file']).read_bytes();assert native.sha(b)==row['sha256'] and json.loads(b)==package[k]
        if k in prior:assert row==prior[k]
    effective=package['effective_object_resource_bindings']['slots'][2]
    assert effective==value['effective_resource_slot'] and effective['raw_offset']==0xC8
    assert package['object_resource_bindings']['slots'][2]['meaning_status']=='unresolved'
    assert package['effective_shot_parameters']==effective_shot_parameters()
    assert package['outstanding_controls_from_phases114_115']==0
    assert 'Актуальный этап 121:' in (ROOT/'INTEGRATION_RU.md').read_text(encoding='utf8')
    assert 'пакет содержит 100 компонентов' in (ROOT/'READINESS_RU.md').read_text(encoding='utf8')
    assert len(value['source_executables_unchanged'])==6
    bitmap=value['source_bitmap'];assert native.sha(Path(bitmap['file']).read_bytes())==bitmap['sha256']
    result=dict(phase=121,date='2026-10-29',native_separate_bounded_executions=18,
        full_raw_converters=5,original_atoi_executions=6,original_atlas_helper_executions=7,
        independent_Ghidra_CPU_controls=2,Ghidra_compared_regions=6,Ghidra_compared_bytes=4872,
        whole_function_ranges=14,original_vtable_blocks=2,stock_identical_code_ranges=14,
        object_sources_preserved=82,records_inspected=11756,exact_container_rebuilds=328,
        changed_container_rebuilds=246,identity_container_rebuilds=82,invalid_API_inputs_rejected=10,
        original_executable_paths_preserved=6,bitmap_preserved=True,
        previous99_components_preserved_byte_exact=True,package_components=100,
        new_resolved_resource_slots=1,raw_C8_is_editor_map_icon=True,
        full_raw_to_MFC_model_CPU_chain_executed=False,GUI_and_full_original_save_executed=False,
        original_instructions_imports_or_vtables_substituted=0,earlier8_Ghidra_controls_still_resolved=True,
        goal_achieved=False,significant_unknowns_remain=True,limitations=value['limitations'])
    (ROOT/'phase121-validation.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8')
    status=dict(phase=121,published_phase=121,date='2026-10-29',
        status='Published Objects+C8 editor-map icon original consumer,GUI binding and raw-save projection;fullMFC/GUI execution explicitly outside scope.',
        previous_goal_turn_classification='progress:phase120Trap removal published;new723Bwriter/session primary files saved as bounded leads.',
        current_goal_turn_classification='progress:previouslyunresolvedC8resource nowtyped throughnative map rendering;18boundedoriginalCPU runs,2Ghidra controls,328exactrebuilds and100componentpackage.',
        pending_native_jobs=[],pending_controls=[],package_components=100,readiness_estimate_percent=75,
        editor_Ghidra=dict(processId=3188,port=8090,project='editor-headless-project/DriverResearch.gpr',
            independent_copy=True,DX_server_retained=True),
        evidence=[link(n) for n in ['editor-object-icon-semantics.json','EDITOR_OBJECT_ICONS_RU.md',
            'phase121-primary-manifest.json','phase121-editor-icon-emulations.json',
            'phase121-editor-icon-crosschecks.json','phase121-icon-api-validation.json','phase121-validation.json',
            'INTEGRATION_RU.md','READINESS_RU.md','editor-format-package.json']],
        next_action='Do notrepeat18iconcases/328rebuilds orC8UIbinding. SeparateProserver now available for remainingunknowns:Room1C4resource nativeeditor consumer,CreatureJumpEE0/PriorityF55,Doors236/Traps204. Traceactualgetter/GUIwriter ormapconsumer;do notnamebyadjacency. ExistingWorld723Bfreshstaticqueries are auxiliary;Sessiontick abort condition is notmanaaggregate evidence. If used,attestfreshmemorytoPE andpublishscopedfacts,notloadinghypothesis. Full15familygoal andother gameplayroutes remainopen.',
        goal_achieved=False,significant_unknowns_remain=True,limitations=value['limitations'])
    (ROOT/'phase121-search-status.json').write_text(json.dumps(status,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:v for k,v in result.items() if k!='limitations'}))

if __name__=='__main__':main()
