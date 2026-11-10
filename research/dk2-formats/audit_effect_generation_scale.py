"""Audit120-component publication, exact source preservation and native scope."""
import json
from build_effect_generation_scale import ROOT,load,sha,link,build,effective_layout

def main():
    value=load('effect-generation-scale-semantics.json');assert value==build()
    package=load('editor-format-package.json');prior=load('phase141-prior-components.json')
    assert len(package['components'])==120 and len(prior)==119
    assert set(package['components'])-set(prior)=={'effect_generation_scale_semantics'}
    for key,row in package['components'].items():
        data=(ROOT/row['file']).read_bytes();assert sha(data)==row['sha256'] and json.loads(data)==package[key]
        if key in prior:assert row==prior[key]
    assert package['effect_generation_scale_semantics']==value
    assert package['effective_catalogue_layouts']['Effects']==effective_layout()
    for before,after in zip(package['catalogue_layouts']['Effects']['fields'],effective_layout()['fields']):
        for k,v in before.items():assert after[k]==v
        assert (before==after)==(before['offset'] not in [0xB4,0xF3])
    assert package['effective_catalogue_layouts']['EffectElements']==package['element_palette_chain_semantics']['effective_element_layout']
    assert package['effective_catalogue_layouts']['Terrain']==package['terrain_opaque_table_semantics']['effective_terrain_layout']
    assert package['effective_catalogue_layouts']['Rooms']==package['room_writer_semantics']['effective_room_layout']
    assert package['effective_shot_parameters']==package['area_damage_semantics']['effective_shot_parameters']
    c=value['counts'];assert c['native_raw_chains']==18 and c['native_rate_fragments']==9 and c['successful_children']==44
    assert c['independent_Ghidra_CPU_controls']==5 and c['Ghidra_compared_regions']==170 and c['Ghidra_compared_bytes']==840620
    assert c['executed_function_spans']==43 and c['unique_executed_original_PCs']==2273
    assert c['new_exact_container_edits']==82 and c['preserved_source_files']==199 and c['preserved_executable_paths']==7
    assert c['newly_named_opaque_fields']==c['source_writes']==0 and c['newly_verified_mode_specific_flag_rules']==1
    assert value['API_validation']['source_records']==30207
    cases={r['case']['name']:r['summary'] for r in value['native_executions']['replays']}
    assert [cases[f'scaled6-{s}']['successful_child_creations'] for s in [0,50,127,128,254,255]]==[1,1,2,3,5,6]
    assert cases['unscaled6-zero']['successful_child_creations']==6 and cases['three-physical-slots']['successful_child_creations']==6
    assert cases['random-first-zero']['successful_child_creations']==cases['zero-count']['successful_child_creations']==0
    assert value['raw_chain']['intermediate_effect_bytes']==426
    assert 'Актуальный этап 141:' in (ROOT/'INTEGRATION_RU.md').read_text(encoding='utf8')
    assert 'пакет содержит 120 компонентов' in (ROOT/'READINESS_RU.md').read_text(encoding='utf8')
    out=dict(phase=141,date='2026-10-30',**c,previous119_components_preserved_byte_exact=True,package_components=120,
        scoped_generation_scale_rule_confirmed=True,complete_World_tick_or_frame_timing_executed=False,
        parent_factory_executed=False,actual_GPU_or_asset_decode=False,
        Misc2_Misc3_meaning='Unresolved',global_unused_claim=False,goal_achieved=False,significant_unknowns_remain=True,
        limitations=value['limitations'])
    (ROOT/'phase141-validation.json').write_text(json.dumps(out,ensure_ascii=False,indent=2),encoding='utf8')
    status=dict(phase=141,published_phase=141,date='2026-10-30',package_components=120,readiness_estimate_percent=75,
        status='Published native mode4 adaptive generation-scale consumer, timer lineage, precise batch formula and82 exact container edits.',
        previous_goal_turn_classification='No progress: preceding response assessed readiness only; phase140 was already published. Current authoritative files and Ghidra instance were revalidated before new work.',
        current_goal_turn_classification='Progress: new actual flags4000 consumer plus18 full raw/gen paths,9 separate arithmetic fragments,5 independent Ghidra controls and120-component integration. Misc words remainunknown.',
        pending_native_jobs=[],pending_controls=[],goal_achieved=False,significant_unknowns_remain=True,
        evidence=[link(n) for n in ['effect-generation-scale-semantics.json','EFFECT_GENERATION_SCALE_RU.md','phase141-effect-scale-primary.json',
            'phase141-effect-scale-emulations.json','phase141-effect-scale-crosschecks.json','phase141-effect-scale-api-validation.json',
            'phase141-validation.json','INTEGRATION_RU.md','READINESS_RU.md','editor-format-package.json']],
        next_action='Do not repeat18 scale chains/9 arithmetic fragments/5 Ghidra/82 edits. Seek actual remaining mode-specific consumers or new typed paths to Doors236,Traps204,Terrain198224,CreatureJump/Priority,KeeperC8. Misc2/Misc3 andElementB1/B2 remainunknown; global nonuse unproved. Nonzero Effects-target generation,parentfactory andfullscheduler remainopen.',
        limitations=value['limitations'])
    (ROOT/'phase141-search-status.json').write_text(json.dumps(status,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:v for k,v in out.items() if k!='limitations'}))

if __name__=='__main__':main()
