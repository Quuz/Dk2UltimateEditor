"""Audit original procedural field evidence, corpus preservation and108part package."""
import json
from build_procedural_resources import ROOT,load,sha,link,build
from build_poison import effective_shot_parameters


def main():
    value=load('procedural-resource-semantics.json');assert value==build()
    package=load('editor-format-package.json');prior=load('phase129-prior-components.json')
    assert len(package['components'])==108 and len(prior)==107
    assert set(package['components'])-set(prior)=={'procedural_resource_semantics'}
    for key,row in package['components'].items():
        data=(ROOT/row['file']).read_bytes();assert sha(data)==row['sha256'] and json.loads(data)==package[key]
        if key in prior:assert row==prior[key]
    assert package['procedural_resource_semantics']==value
    assert package['effective_shot_parameters']==effective_shot_parameters()
    assert package['effective_catalogue_layouts']['Rooms']==package['room_writer_semantics']['effective_room_layout']
    assert package['effective_object_resource_bindings']['slots'][2]['display_name']=='EditorMapIconResource'
    assert package['effective_enums']['Creature.ResistanceCategory']==package['combat_category_semantics']['native_resistance_categories']
    for family,layout in package['combat_category_semantics']['effective_creature_layouts'].items():
        assert package['effective_catalogue_layouts'][family]==layout
    assert package['outstanding_controls_from_phases114_115']==0
    counts=value['counts'];corpus=value['corpus']
    assert counts['native_compositions']==20 and len(value['opaque_DWORD_pairs'])==4
    assert counts['independent_Ghidra_CPU_controls']==3 and counts['Ghidra_compared_regions']==126
    assert counts['Ghidra_compared_bytes']==1093597 and len(value['primary']['functions'])==34 and len(value['primary']['blocks'])==4
    assert len(corpus['files'])==84 and corpus['records_seen']==84174 and corpus['type7_records']==580
    assert corpus['exact_container_rebuilds']==2320 and corpus['changed_containers']==1740 and corpus['identity_containers']==580
    assert corpus['API_refusals']==8 and len(value['source_executables'])==7
    assert corpus['counts']['EffectElements_records']==48416 and corpus['counts'].get('EffectElements_type7_records',0)==0
    variant=value['ArtResource_variant'];assert variant['fields'][0]['size']==4
    assert all(f['meaning_resolved'] is False for f in variant['fields'][1:])
    assert variant['handle']['successful'] and variant['handle']['value_in_selected_native_request']==0
    assert 'Актуальный этап 129:' in (ROOT/'INTEGRATION_RU.md').read_text(encoding='utf8')
    assert 'пакет содержит 108 компонентов' in (ROOT/'READINESS_RU.md').read_text(encoding='utf8')
    output=dict(phase=129,date='2026-10-29',newly_confirmed_field='ArtResource type7 union+44h:u32 procedure selector',
        unknown_union_fields_preserved=['opaque_1:u32@48h','opaque_2:u32@4Ch'],native_compositions=20,
        original_standalone_type7_records=7,original_procedural_selector_values_executed=[2,4,7,8],
        diagnostic_selector_values_executed=[0,1,5,6,0xFFFFFFFF],paired_complete_context_predictions=4,
        independent_Ghidra_CPU_controls=3,Ghidra_compared_regions=126,Ghidra_compared_bytes=1093597,
        whole_canonical_function_spans=34,primary_data_blocks=4,covered_executed_original_PCs=2971,
        source_files=84,source_records=84174,type7_records=580,exact_container_rebuilds=2320,
        changed_containers=1740,identity_containers=580,API_refusals=8,original_executable_paths_preserved=7,
        source_writes=0,previous107_components_preserved_byte_exact=True,package_components=108,
        live_scene_or_GPU=False,generic59B880_wrapper_executed=False,full_Effect_constructor_executed=False,
        selector3_full_consumer_executed=False,ElementB1B2_resolved=False,goal_achieved=False,
        significant_unknowns_remain=True,limitations=value['limitations'])
    (ROOT/'phase129-validation.json').write_text(json.dumps(output,ensure_ascii=False,indent=2),encoding='utf8')
    status=dict(phase=129,published_phase=129,date='2026-10-29',package_components=108,readiness_estimate_percent=75,
        status='Published original procedural resource selector mapping and real geometry consumers; two union DWORDs remain opaque.',
        previous_goal_turn_classification='No new format evidence: answered the damage category/AoE question using already published native paths. Revalidated existing phase128state before new work.',
        current_goal_turn_classification='Progress: new raw type7 DWORD consumer,20native paths/3Ghidra controls,2320exact rebuilds and108part publication.',
        pending_native_jobs=[],pending_controls=[],goal_achieved=False,significant_unknowns_remain=True,
        evidence=[link(n) for n in ['procedural-resource-semantics.json','PROCEDURAL_RESOURCES_RU.md','phase129-primary-manifest.json',
            'phase129-procedural-native-emulations.json','phase129-procedural-crosschecks.json','phase129-procedural-api-validation.json',
            'phase129-validation.json','INTEGRATION_RU.md','READINESS_RU.md','editor-format-package.json']],
        next_action='Do not repeat the completed raw2/4/7/8 geometry runs, four opaque permutations or three controls. Pursue original editor procedural parameter/type bindings and selector3 typed context, or fresh gameplay consumers among remaining Traps204/Doors236/CreatureJumpEE0/PriorityF55/KeeperC8/ElementB1B2/Terrain unknowns. The two procedural opaque DWORDs retain unknown meaning; no pointer or global-nonuse label. Use fresh primary clues, not more palette permutations without a consumer.',
        limitations=value['limitations'])
    (ROOT/'phase129-search-status.json').write_text(json.dumps(status,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:v for k,v in output.items() if k!='limitations'}))


if __name__=='__main__':main()
