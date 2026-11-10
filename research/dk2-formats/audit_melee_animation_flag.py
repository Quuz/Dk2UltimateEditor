"""Audit126 schemas and bounded native semantics of previously unknown bit29."""
import json
from build_melee_animation_flag import ROOT,load,sha,link,build
import native_melee_animation_flag as api
def main():
    v=load('melee-animation-flag-semantics.json');assert v==build()
    p=load('editor-format-package.json');old=load('phase147-prior-components.json')
    assert len(p['components'])==126 and len(old)==125
    assert set(p['components'])-set(old)=={'melee_animation_flag_semantics'}
    for k,row in p['components'].items():
        data=(ROOT/row['file']).read_bytes();assert sha(data)==row['sha256'] and json.loads(data)==p[k]
        if k in old:assert row==old[k]
    assert p['melee_animation_flag_semantics']==v
    assert p['effective_creature_flag_enum']==api.effective_enums()['Creature.CreatureFlag']
    assert p['enums']['Creature.CreatureFlag'][str(api.MASK)]=='UNKNOWN_5'
    assert p['effective_creature_flag_enum'][str(api.MASK)]==api.NAME
    changes=[k for k,value in p['enums']['Creature.CreatureFlag'].items() if value!=p['effective_creature_flag_enum'][k]];assert changes==[str(api.MASK)]
    assert p['effective_catalogue_layouts']['Traps']==p['trap_model_word_semantics']['effective_trap_layout']
    assert p['effective_catalogue_layouts']['Doors']==p['door_manufacture_split_semantics']['effective_door_layout']
    c=v['counts'];assert c['native_scenarios']==14 and c['complete_raw_to_melee_cases']==8 and c['complete_separate_animation_cases']==6
    assert c['independent_Ghidra_CPU_controls']==4 and c['Ghidra_compared_regions']==180 and c['Ghidra_compared_bytes']==449944
    assert c['original_function_extents']==95 and c['executed_function_extents']==94 and c['static_formatter_extents']==1
    assert c['unique_executed_original_PCs']==4727 and c['primary_data_blocks']==4
    assert c['whole_raw_melee_pair_regions']==44 and c['whole_raw_melee_pair_bytes']==112482 and c['pair_changed_bytes']==4
    assert c['new_exact_container_edits']==c['public_decode_rebuild_controls']==82 and c['source_Creature_records']==3734
    assert c['preserved_source_files']==310 and c['preserved_executable_paths']==7 and c['newly_named_unknown_gameplay_bits']==1 and c['source_writes']==0
    rows={r['case']['name']:r['outcome'] for r in v['native_executions']['replays']}
    assert rows['animation-0-moving0']['chosen_resource_slot']==39 and rows['animation-1-moving0']['chosen_resource_slot']==5
    assert rows['animation-0-moving1']['chosen_resource_slot']==rows['animation-1-moving1']['chosen_resource_slot']==5
    assert rows['melee-0-initial1']['target_HP']==rows['melee-1-initial0']['target_HP']==334
    assert rows['melee-0-cooldown']['root_return']==rows['melee-1-cooldown']['root_return']==1
    assert 'Актуальный этап 147:' in (ROOT/'INTEGRATION_RU.md').read_text(encoding='utf8')
    assert 'пакет содержит 126 компонентов' in (ROOT/'READINESS_RU.md').read_text(encoding='utf8')
    out=dict(phase=147,date='2026-10-31',**c,package_components=126,previous125_components_preserved_byte_exact=True,
        bit29_interpretation='PLAY_MELEE_INSTANCE_ANIMATION in selected standing active-instance branch',normal_AI_or_GPU_executed=False,
        goal_achieved=False,significant_unknowns_remain=True,limitations=v['limitations'])
    (ROOT/'phase147-validation.json').write_text(json.dumps(out,ensure_ascii=False,indent=2),encoding='utf8')
    status=dict(phase=147,published_phase=147,date='2026-10-31',package_components=126,readiness_estimate_percent=75,
      status='Published formerly unknown CreatureFlags bit29 raw/melee/runtime/animation semantics andnativePlayInstanceAnimation name; oneunknownbitidentified.',
      previous_goal_turn_classification='Progress:phase146publishedfulloriginalTrapmodel/saveandnativeWORD204 API,125componentpackage.',
      current_goal_turn_classification='Progress:14newnativecases/4Ghidra/95wholeextents/82one-bitedits; rawUNKNOWN_5bit29 identifiedwithnativeformatter andrealconsumer;126componentspublished.',
      pending_native_jobs=[],pending_controls=[],goal_achieved=False,significant_unknowns_remain=True,
      evidence=[link(n) for n in ['melee-animation-flag-semantics.json','MELEE_ANIMATION_FLAG_RU.md','phase147-melee-animation-emulations.json','phase147-melee-animation-crosschecks.json',
        'phase147-melee-animation-primary.json','phase147-animation-flag-api-validation.json','phase147-validation.json','editor-format-package.json','INTEGRATION_RU.md','READINESS_RU.md']],
      next_action='Do not repeat14animationcases/4Ghidra/82flag-edits. Nativeinstanceflagdebug formatter490240 has more specificstate names useful for fresh typed field tracing. UnknownCreatureFlags800/1000,extraFlagsbit2,Jump/Priority,Traps204/Doors236/Terrain198224/AP0408/KeeperC8 remainopen. Preserve separate raw/melee andpreparedrenderer coverage; no GPU/all-animation-disable claim. No source/parallel editor writes.',
      limitations=v['limitations'])
    (ROOT/'phase147-search-status.json').write_text(json.dumps(status,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:v for k,v in out.items() if k!='limitations'}))
if __name__=='__main__':main()
