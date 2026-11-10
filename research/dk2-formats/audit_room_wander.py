"""Audit124-component package and native nonempty-room branch evidence."""
import json
from build_room_wander import ROOT,load,sha,link,build
def main():
    v=load('room-wander-semantics.json');assert v==build()
    package=load('editor-format-package.json');prior=load('phase145-prior-components.json')
    assert len(package['components'])==124 and len(prior)==123
    assert set(package['components'])-set(prior)=={'room_wander_semantics'}
    for key,row in package['components'].items():
        data=(ROOT/row['file']).read_bytes();assert sha(data)==row['sha256'] and json.loads(data)==package[key]
        if key in prior:assert row==prior[key]
    assert package['room_wander_semantics']==v
    assert package['effective_thing_layouts']==package['wander_target_path_semantics']['effective_thing_layouts']
    assert package['effective_catalogue_layouts']['Doors']==package['door_manufacture_split_semantics']['effective_door_layout']
    assert package['effective_catalogue_layouts']['Effects']==package['effect_mode18_semantics']['effective_effect_layout']
    c=v['counts'];assert c['native_scenarios']==38 and c['complete_helper_cases']==35 and c['complete_state87_cases']==3
    assert c['source_room_types_tested']==26 and c['initially_eligible_source_types']==14
    assert c['independent_Ghidra_CPU_controls']==6 and c['Ghidra_compared_regions']==355 and c['Ghidra_compared_bytes']==843938
    assert c['paired_compared_bytes']==189856 and c['paired_complete_memory_controls']==1
    assert c['additional_whole_function_spans']==8 and c['inherited_whole_function_spans']==60 and c['unique_executed_original_PCs']==9759
    assert c['new_exact_container_edits']==26 and c['preserved_source_files']==309 and c['preserved_executable_paths']==7
    assert c['newly_named_opaque_fields']==c['source_writes']==0
    rows={r['case']['name']:r['outcome'] for r in v['native_executions']['replays']}
    assert rows['two-eligible-last']['room_trials']==[1] and rows['two-eligible-last']['target']==[83968,83968,0]
    assert rows['two-eligible-reversed']['target']==[14336,14336,0]
    assert rows['last-empty-skips-earlier']['selected_room'] is None
    assert rows['trailing-excluded-selected']['selected_room']==2 and rows['trailing-excluded-selected']['room_trials']==[1,2]
    assert rows['trailing-empty-then-excluded']['selected_room']==3
    assert rows['callback-room']==rows['callback-room-wander255']
    assert rows['callback-room']['state_after']==6 and rows['callback-room-blocked']['state_after']==87
    assert v['raw_WanderRadius']['no_global_unused_claim']
    assert 'Актуальный этап 145:' in (ROOT/'INTEGRATION_RU.md').read_text(encoding='utf8')
    assert 'пакет содержит 124 компонента' in (ROOT/'READINESS_RU.md').read_text(encoding='utf8')
    out=dict(phase=145,date='2026-10-31',**c,package_components=124,previous123_components_preserved_byte_exact=True,
        nonempty_room_branch_confirmed=True,normal_World_scheduler_executed=False,actual_actor_movement_executed=False,
        raw_WanderRadius_meaning='Unresolved',goal_achieved=False,significant_unknowns_remain=True,limitations=v['limitations'])
    (ROOT/'phase145-validation.json').write_text(json.dumps(out,ensure_ascii=False,indent=2),encoding='utf8')
    status=dict(phase=145,published_phase=145,date='2026-10-31',package_components=124,readiness_estimate_percent=75,
        status='Published native nonempty-room selection, last-eligible/following-excluded behavior, all26source types, CPUcontrols and exactRoomID copy edits; rawWanderRadius still unresolved.',
        previous_goal_turn_classification='No progress: preceding goal turn answered the user percentage/status question using published evidence; no new authoritative analysis then.',
        current_goal_turn_classification='Progress:38acceptednewnative scenarios,6Ghidra/8newspans/2tables/26exactedits,124componentpublication. Re-read Creature indexed leads were alreadyphase86 and are not new evidence.',
        pending_native_jobs=[],pending_controls=[],goal_achieved=False,significant_unknowns_remain=True,
        evidence=[link(n) for n in ['room-wander-semantics.json','ROOM_WANDER_RU.md','phase145-room-wander-emulations.json','phase145-room-wander-crosschecks.json',
          'phase145-room-wander-primary.json','phase145-room-ID-api-validation.json','phase145-validation.json','editor-format-package.json','INTEGRATION_RU.md','READINESS_RU.md']],
        next_action='Do not repeat38roomcases/6Ghidra/26IDedits orphase86indexedCreatureclassification. Prioritize genuinelynew typed consumer of remainingJump/Priority/Doors236/Traps204/Terrain198224/AP04/08/KeeperC8. New roombranch cellCFF/blockingobject/specialcoords andnormalAIphysicalmovement remain open, but do not mistake these for rawWanderRadius meaning. No source/parallel editor writes.',
        limitations=v['limitations'])
    (ROOT/'phase145-search-status.json').write_text(json.dumps(status,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:v for k,v in out.items() if k!='limitations'}))
if __name__=='__main__':main()
