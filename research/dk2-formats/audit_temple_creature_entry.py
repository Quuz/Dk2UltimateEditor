"""Audit115component package, registered Creature Temple context and recipe append."""
import json
from build_temple_creature_entry import ROOT,load,sha,link,build
from build_terrain_table import effective_terrain_layout
from build_area_damage import effective_shot_parameters

def main():
    value=load('temple-creature-entry-semantics.json');assert value==build()
    package=load('editor-format-package.json');prior=load('phase136-prior-components.json')
    assert len(package['components'])==115 and len(prior)==114
    assert set(package['components'])-set(prior)=={'temple_creature_entry_semantics'}
    for key,row in package['components'].items():
        data=(ROOT/row['file']).read_bytes();assert sha(data)==row['sha256'] and json.loads(data)==package[key]
        if key in prior:assert row==prior[key]
    assert package['temple_creature_entry_semantics']==value
    assert package['effective_shot_parameters']==effective_shot_parameters()
    assert package['effective_catalogue_layouts']['Terrain']==effective_terrain_layout()
    assert package['effective_catalogue_layouts']['Rooms']==package['room_writer_semantics']['effective_room_layout']
    assert package['effective_enums']['Creature.ResistanceCategory']==package['combat_category_semantics']['native_resistance_categories']
    assert package['outstanding_controls_from_phases114_115']==0
    c=value['counts'];assert c['full_Creature_entry_paths']==14 and c['registered_state65_paths']==12 and c['direct_state1_diagnostic_paths']==2
    assert c['independent_Ghidra_CPU_controls']==7 and c['Ghidra_compared_regions']==238 and c['Ghidra_compared_bytes']==738578
    assert c['checked_scene_bytes_including_declared_measured_raw_baseline']==1475652 and c['unique_executed_original_PCs']==9190
    assert c['preserved_source_files']==88 and c['executed_function_spans']==113 and c['raw_type43_callback_invocations']==57
    assert value['primary']['all_executed_original_PCs_covered']
    assert value['decision']['registered_state']==dict(state=101,category=12,callback=4663600,proof=value['decision']['registered_state']['proof'])
    ex=value['state_request']['examples'];assert ex['registered_capacity0']['final_AI_state']==103 and not ex['registered_capacity0']['room_capacity_checked']
    assert ex['diagnostic_refused']['initial_AI_state']==1 and ex['diagnostic_refused']['final_AI_state']==1 and ex['diagnostic_refused']['room_capacity_checked']
    app=value['recipe_append']['validation'];assert app['rebuilt_variable_records']==622 and app['rebuilt_recipes']==45 and app['entire_other_section_byte_exact']
    assert 'Актуальный этап 136:' in (ROOT/'INTEGRATION_RU.md').read_text(encoding='utf8')
    assert 'пакет содержит 115 компонентов' in (ROOT/'READINESS_RU.md').read_text(encoding='utf8')
    output=dict(phase=136,date='2026-10-30',**c,previous114_components_preserved_byte_exact=True,package_components=115,
        registered_state65_context_verified=True,state1_diagnostics_distinguished=True,
        whole_native_KWD_parser_executed=False,normal_AI_caller_executed=False,physical_drop_executed=False,final_World_removal_executed=False,
        no_global_fullTemple_refusal_rule_inferred=True,goal_achieved=False,significant_unknowns_remain=True,limitations=value['limitations'])
    (ROOT/'phase136-validation.json').write_text(json.dumps(output,ensure_ascii=False,indent=2),encoding='utf8')
    status=dict(phase=136,published_phase=136,date='2026-10-30',package_components=115,readiness_estimate_percent=75,
        status='Published full Creature Temple entry with correct native state65 registration, explicitstate1diagnostics, original navsetup and canonicalVariables recipeappend.',
        previous_goal_turn_classification='Progress: phase135 published19ObjectTemple entries,5Ghidra controls,Gold exclusion/HP0 semantics,82losslessGoldbit edits and114components.',
        current_goal_turn_classification='Progress:14Creatureentry paths,one rawloadbaseline,7Ghidra controls,57rawrecipe callbacks,113functionspans,one completeVariablesappend and115component publication. Corrected earlyState1context generalization before publication; registeredState65doesnotrecheck capacity for67transition.',
        pending_native_jobs=[],pending_controls=[],goal_achieved=False,significant_unknowns_remain=True,
        evidence=[link(n) for n in ['temple-creature-entry-semantics.json','TEMPLE_CREATURE_ENTRY_RU.md',
            'phase136-temple-creature-primary.json','phase136-temple-creature-emulations.json','phase136-temple-creature-crosschecks.json',
            'phase136-temple-recipe-append-validation.json','phase136-validation.json','INTEGRATION_RU.md','READINESS_RU.md','editor-format-package.json']],
        next_action='Do notrepeat14entry/7control cases orcanonical621→622append. Follow actual producer ofruntimeMapcellBYTEC and originalAIcaller/state65activation toconnect rawKLD/Rooms toTemple geometry/entry. Do not infer runtimecellC fromrawMapfourthbyte. FinalWorldunlink and entry→nonmanafactory reward remain open. Prioritize fresh typedleads for remaining15family unknowns; no original/parallel editor writes.',
        limitations=value['limitations'])
    (ROOT/'phase136-search-status.json').write_text(json.dumps(status,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:v for k,v in output.items() if k!='limitations'}))

if __name__=='__main__':main()
