"""Audit119component publication and scoped unresolved Element bytes."""
import json
from build_element_palette_chain import ROOT,load,sha,link,build,effective_layout

def main():
    value=load('element-palette-chain-semantics.json');assert value==build()
    package=load('editor-format-package.json');prior=load('phase140-prior-components.json')
    assert len(package['components'])==119 and len(prior)==118
    assert set(package['components'])-set(prior)=={'element_palette_chain_semantics'}
    for key,row in package['components'].items():
        data=(ROOT/row['file']).read_bytes();assert sha(data)==row['sha256'] and json.loads(data)==package[key]
        if key in prior:assert row==prior[key]
    assert package['element_palette_chain_semantics']==value
    assert package['effective_catalogue_layouts']['EffectElements']==effective_layout()
    old=package['catalogue_layouts']['EffectElements'];new=package['effective_catalogue_layouts']['EffectElements']
    assert old['record_size']==new['record_size']==182 and len(old['fields'])==len(new['fields'])
    for before,after in zip(old['fields'],new['fields']):
        for k,v in before.items():assert after[k]==v
        assert (before==after)==(before['offset'] not in [0xB1,0xB2])
    assert package['effective_catalogue_layouts']['Rooms']==package['room_writer_semantics']['effective_room_layout']
    assert package['effective_catalogue_layouts']['Terrain']==package['terrain_opaque_table_semantics']['effective_terrain_layout']
    assert package['effective_shot_parameters']==package['area_damage_semantics']['effective_shot_parameters']
    assert package['effective_enums']['Creature.ResistanceCategory']==package['combat_category_semantics']['native_resistance_categories']
    assert package['outstanding_controls_from_phases114_115']==0
    c=value['counts'];assert c['native_chains']==c['raw_definition_loads']==23
    assert c['full_spawn_paths']==c['full_render_paths']==21 and c['full_update_paths']==17 and c['individual_native_physics_calls']==762
    assert c['paired_complete_memory_controls']==15 and c['paired_compared_bytes']==589380
    assert c['independent_Ghidra_CPU_controls']==6 and c['Ghidra_compared_regions']==252 and c['Ghidra_compared_bytes']==235752
    assert c['executed_function_spans']==53 and c['unique_executed_original_PCs']==2956 and c['original_vtable_blocks']==5
    assert c['new_exact_container_edits']==82 and c['preserved_source_files']==199 and c['preserved_executable_paths']==7
    assert c['source_writes']==c['newly_named_opaque_fields']==0
    assert value['unknown_result']['status']=='Unresolved' and value['unknown_result']['not_a_global_unused_claim']
    assert all(not r['outcome']['unknown_definition_reads'] for r in value['native_executions']['replays'])
    ex=value['positive_controls'];assert ex['late_original']['RGBA']==[5,5,5,13] and ex['late_RGB_edit']['RGBA']==[20,40,60,13]
    assert ex['cloud_late']['RGBA']==[0,0,0,255] and ex['zero_life']['lifetime_after']==0
    assert value['API_validation']['source_records']==48416 and value['API_validation']['source_unknown_pair_histogram']==[dict(values=[0,0],records=48416)]
    assert 'Актуальный этап 140:' in (ROOT/'INTEGRATION_RU.md').read_text(encoding='utf8')
    assert 'пакет содержит 119 компонентов' in (ROOT/'READINESS_RU.md').read_text(encoding='utf8')
    out=dict(phase=140,date='2026-10-30',**c,previous118_components_preserved_byte_exact=True,package_components=119,
        whole_raw_to_spawn_tick_render_chain_executed=True,actual_GPU_or_asset_decode=False,World_scheduler_or_final_deletion_executed=False,
        B1_B2_meaning='Unresolved',global_unused_claim=False,legacy_palette_names_native_confirmed=False,
        goal_achieved=False,significant_unknowns_remain=True,limitations=value['limitations'])
    (ROOT/'phase140-validation.json').write_text(json.dumps(out,ensure_ascii=False,indent=2),encoding='utf8')
    status=dict(phase=140,published_phase=140,date='2026-10-30',package_components=119,readiness_estimate_percent=75,
        status='Published fullrawElement/spawn/tick/render palette-boundary chain; B1/B2 remainunresolved andunreadinselectedpaths; legacy names explicitlyannotated in effectiveview.',
        previous_goal_turn_classification='Progress: phase139published17nativeMapreaderpaths/6Ghidra/5pairs/110opaqueedits and118components.',
        current_goal_turn_classification='Progress:23fullrawElementchains/15completepairs/6Ghidra/53functionspans/82combinedunknownedits and119components. PositiveRGB/fade controls distinguish realrender path from inactivegraphics. Unknownpalette names remainunconfirmed; no semanticclosure inflation.',
        pending_native_jobs=[],pending_controls=[],goal_achieved=False,significant_unknowns_remain=True,
        evidence=[link(n) for n in ['element-palette-chain-semantics.json','ELEMENT_PALETTE_CHAIN_RU.md','phase140-element-palette-chain-primary.json',
            'phase140-element-palette-chain-emulations.json','phase140-element-palette-chain-crosschecks.json','phase140-element-palette-api-validation.json',
            'phase140-validation.json','INTEGRATION_RU.md','READINESS_RU.md','editor-format-package.json']],
        next_action='Do not repeat23Elementchains/15palettepairs/6Ghidra/82edits or previous sprite/model permutations. B1/B2nativepalette meaning remainsopen; only pursue withfresh typedconsumer. Seek genuinelynewconsumers inDoors236/Traps204/Terrain198224/CreatureJumpEE0/PriorityF55/KeeperC8 or unclosedmode-specificdata/flags. WanderRadiusunits mustnotbe borrowed fromunrelatedcontextAF. No source/parallel editor writes.',
        limitations=value['limitations'])
    (ROOT/'phase140-search-status.json').write_text(json.dumps(status,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:v for k,v in out.items() if k!='limitations'}))

if __name__=='__main__':main()
