"""Audit114component package and bounded complete object-sacrifice entry."""
import json
from build_temple_object_entry import ROOT,load,sha,link,build
from build_terrain_table import effective_terrain_layout
from build_area_damage import effective_shot_parameters

def main():
    value=load('temple-object-entry-semantics.json');assert value==build()
    package=load('editor-format-package.json');prior=load('phase135-prior-components.json')
    assert len(package['components'])==114 and len(prior)==113
    assert set(package['components'])-set(prior)=={'temple_object_entry_semantics'}
    for key,row in package['components'].items():
        data=(ROOT/row['file']).read_bytes();assert sha(data)==row['sha256'] and json.loads(data)==package[key]
        if key in prior:assert row==prior[key]
    assert package['temple_object_entry_semantics']==value
    assert package['effective_shot_parameters']==effective_shot_parameters()
    assert package['effective_catalogue_layouts']['Terrain']==effective_terrain_layout()
    assert package['effective_catalogue_layouts']['Rooms']==package['room_writer_semantics']['effective_room_layout']
    assert package['effective_enums']['Creature.ResistanceCategory']==package['combat_category_semantics']['native_resistance_categories']
    assert package['outstanding_controls_from_phases114_115']==0
    c=value['counts'];assert c['full_native_object_entry_paths']==19 and c['independent_Ghidra_CPU_controls']==5
    assert c['Ghidra_compared_regions']==130 and c['Ghidra_compared_bytes']==380970
    assert c['full_prepared_bytes_independently_predicted']==1447590 and c['unique_executed_original_PCs']==1105
    assert c['preserved_source_files']==87 and c['executed_function_spans']==22
    assert value['primary']['all_executed_original_PCs_covered'] and value['fields']['tested_mask']==64
    assert not value['examples']['source_Gold']['accepted'] and value['examples']['source_DoorKey']['accepted']
    assert value['corpus']['current_ingredient_type_counts']=={'1':92,'3':9}
    assert 'Актуальный этап 135:' in (ROOT/'INTEGRATION_RU.md').read_text(encoding='utf8')
    assert 'пакет содержит 114 компонентов' in (ROOT/'READINESS_RU.md').read_text(encoding='utf8')
    output=dict(phase=135,date='2026-10-30',**c,previous113_components_preserved_byte_exact=True,package_components=114,
        gold_recipe_bypass_and_HP_zeroing_verified=True,normal_callback_dispatch_executed=False,final_World_removal_executed=False,
        full_Creature_sacrifice_entry_executed=False,live_visual_effect_executed=False,
        goal_achieved=False,significant_unknowns_remain=True,limitations=value['limitations'])
    (ROOT/'phase135-validation.json').write_text(json.dumps(output,ensure_ascii=False,indent=2),encoding='utf8')
    status=dict(phase=135,published_phase=135,date='2026-10-30',package_components=114,readiness_estimate_percent=75,
        status='Published full object-sacrifice entry from rawObject/rawrecipe: Gold exclusion, HP0 before matching, owner/history/height rules with independent CPU controls.',
        previous_goal_turn_classification='Progress: phase134 completed two Terrain loader profiles, primary spans, opaque32byte edit tests and113component publication; array purpose retainedunknown.',
        current_goal_turn_classification='Progress:19new fullObjectTemple paths,5Ghidra controls,raw flags/game predicate semantics,82exactGold-bit edits and114component publication. Initial Terrain198/224/Depth triage reused prior search data and didnot establish a new consumer; not counted as newsemantic coverage.',
        pending_native_jobs=[],pending_controls=[],goal_achieved=False,significant_unknowns_remain=True,
        evidence=[link(n) for n in ['temple-object-entry-semantics.json','TEMPLE_OBJECT_ENTRY_RU.md',
            'phase135-temple-object-primary.json','phase135-temple-object-emulations.json','phase135-temple-object-crosschecks.json',
            'phase135-temple-object-api-validation.json','phase135-validation.json','INTEGRATION_RU.md','READINESS_RU.md','editor-format-package.json']],
        next_action='Do notrepeat19Object callback cases or82Gold-bit edits. Follow actual normalObject state dispatch/physicalentry into49F600 or complete Creature472930Temple entry with original lifecycle; distinguish HP0 from finalWorlddelete and staticregistration from executedactivation. Original44recipes contain noObjectingredients, diagnosticObject recipes muststayexplicit. Terrain198/224/16WORD purpose and other15family unknowns remain; seek fresh typedleads ratherthan repeating exhausted direct/adjusted scans. No original/parallel editor writes.',
        limitations=value['limitations'])
    (ROOT/'phase135-search-status.json').write_text(json.dumps(status,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:v for k,v in output.items() if k!='limitations'}))

if __name__=='__main__':main()
