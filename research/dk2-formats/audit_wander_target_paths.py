"""Audit123 schemas, selected native state87 paths and raw WanderRadius bounds."""
import json
from build_wander_target_paths import ROOT,load,sha,link,build,effective_things

def main():
    value=load('wander-target-path-semantics.json');assert value==build()
    package=load('editor-format-package.json');prior=load('phase144-prior-components.json')
    assert len(package['components'])==123 and len(prior)==122
    assert set(package['components'])-set(prior)=={'wander_target_path_semantics'}
    for key,row in package['components'].items():
        data=(ROOT/row['file']).read_bytes();assert sha(data)==row['sha256'] and json.loads(data)==package[key]
        if key in prior:assert row==prior[key]
    assert package['wander_target_path_semantics']==value
    current=package['effective_thing_layouts'];assert current==effective_things()
    annotated=0
    for tag,old in package['thing_layouts'].items():
        new=current[tag];assert len(old['fields'])==len(new['fields'])
        for k,v in old.items():
            if k!='fields':assert new[k]==v
        for before,after in zip(old['fields'],new['fields']):
            for k,v in before.items():assert after[k]==v
            if before!=after:
                annotated+=1;assert tag in ['0xC6','0xC7','0xC9'] and after['prior_native_label']=='WanderRadius'
                assert before['size']==1 and before['kind']=='u8'
    assert annotated==18
    assert package['effective_catalogue_layouts']['Effects']==package['effect_mode18_semantics']['effective_effect_layout']
    assert package['effective_catalogue_layouts']['Doors']==package['door_manufacture_split_semantics']['effective_door_layout']
    assert package['effective_catalogue_layouts']['Terrain']==package['terrain_opaque_table_semantics']['effective_terrain_layout']
    assert package['effective_shot_parameters']==package['area_damage_semantics']['effective_shot_parameters']
    c=value['counts'];assert c['native_scenarios']==c['distinct_native_runs']==17
    assert c['complete_target_helper_cases']==8 and c['complete_state87_cases']==9 and c['accepted_native_path_requests']==7
    assert c['paired_complete_memory_controls']==1 and c['paired_compared_bytes']==187838
    assert c['independent_Ghidra_CPU_controls']==6 and c['Ghidra_compared_regions']==326 and c['Ghidra_compared_bytes']==930082
    assert c['executed_function_spans']==60 and c['unique_executed_original_PCs']==8535
    assert c['original_vtable_blocks']==c['original_ring_switch_blocks']==2
    assert c['new_exact_container_edits']==102 and c['carrier_files_without_supported_records']==7
    assert c['preserved_source_files']==309 and c['preserved_executable_paths']==7
    assert c['newly_named_opaque_fields']==c['source_writes']==0
    assert value['preservation']['previous_source_roster_count']==200 and value['preservation']['added_historical_carrier_files']==109
    cases={r['case']['name']:r['outcome'] for r in value['native_executions']['replays']}
    assert cases['callback-open']['state_after']==6 and cases['callback-open']['target']==[68948,33450,0]
    assert cases['callback-open']==cases['callback-wander255']
    assert cases['helper-only-before-start']['target'] is None and cases['helper-unvisited-corner']['target'] is None
    assert cases['helper-duplicated-corner']['target']==[26624,26624,0]
    assert not cases['callback-blocked']['path_accepted'] and cases['callback-blocked']['state_after']==87
    assert cases['callback-foreign-fallback']['helper_runs']==6 and cases['callback-foreign-fallback']['path_accepted']
    assert value['raw_byte_boundary']['not_a_global_unused_claim'] and value['raw_byte_boundary']['no_mapping_to_hardcoded_radius']
    assert 'Актуальный этап 144:' in (ROOT/'INTEGRATION_RU.md').read_text(encoding='utf8')
    assert 'пакет содержит 123 компонента' in (ROOT/'READINESS_RU.md').read_text(encoding='utf8')
    out=dict(phase=144,date='2026-10-31',**c,previous122_components_preserved_byte_exact=True,package_components=123,
        selected_native_path_acceptance_confirmed=True,actual_actor_movement_executed=False,normal_World_scheduler_executed=False,
        raw_WanderRadius_meaning='Unresolved',global_unused_claim=False,goal_achieved=False,significant_unknowns_remain=True,
        limitations=value['limitations'])
    (ROOT/'phase144-validation.json').write_text(json.dumps(out,ensure_ascii=False,indent=2),encoding='utf8')
    status=dict(phase=144,published_phase=144,date='2026-10-31',package_components=123,readiness_estimate_percent=75,
        status='Published selected state87 target helper/native NAV acceptance; ring asymmetry verified, rawWanderRadius units/effect remain unresolved.',
        previous_goal_turn_classification='Progress: saved17acceptednativecases/6Ghidra/60functionattestation/102rawedits. Publication interrupted by automaticapprovalreview usage failure before builder was written.',
        current_goal_turn_classification='Progress: revalidated savedcases/AI tables/fullmemory controls, completed schema/docs/package123/audit; preservation roster extends200->309 with109 historicallyhashedThings files. No extraCPU executions claimed.',
        pending_native_jobs=[],pending_controls=[],goal_achieved=False,significant_unknowns_remain=True,
        evidence=[link(n) for n in ['wander-target-path-semantics.json','WANDER_TARGET_PATH_RU.md','phase144-wander-target-path-primary.json',
            'phase144-wander-target-path-emulations.json','phase144-wander-target-path-crosschecks.json','phase144-wander-placement-api-validation.json',
            'phase144-validation.json','INTEGRATION_RU.md','READINESS_RU.md','editor-format-package.json']],
        next_action='Do not repeat17wander cases/6Ghidra/102edits orphase75literal scan. RawWanderRadius Actor418 remainsunknown; seek genuinelynew consumer or remainingCreatureJump/Priority/Doors236/Traps204/Terrain198224/KeeperC8 path. Optionalnonemptyroom/helperbranch andnormalAI/physicalmovement remainopen. No source/parallel editor writes.',
        limitations=value['limitations'])
    (ROOT/'phase144-search-status.json').write_text(json.dumps(status,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:v for k,v in out.items() if k!='limitations'}))

if __name__=='__main__':main()
