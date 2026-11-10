"""Audit phase156 evidence, lossless edits and preservation of134 prior components."""
import json
from build_ap_trigger_tree import ROOT,load,sha,link,build

def main():
    s=load('ap-trigger-tree-semantics.json');assert s==build()
    p=load('editor-format-package.json');old=load('phase156-prior-components.json')
    assert len(p['components'])==135 and len(old)==134 and set(p['components'])-set(old)=={'ap_trigger_tree_semantics'}
    for key,row in p['components'].items():
        b=(ROOT/row['file']).read_bytes();assert sha(b)==row['sha256'] and json.loads(b)==p[key]
        if key in old:assert row==old[key]
    assert p['ap_trigger_tree_semantics']==s
    for a,b in [('effective_shot_parameters',p['tremor_damage_semantics']['effective_shot_parameters']),
        ('effective_room_resource_consumption',p['room_resource_loader_semantics']['resource_consumption']),
        ('effective_creature_flag_enum',p['melee_animation_flag_semantics']['effective_creature_flag_enum'])]:assert p[a]==b
    for family,key,subkey in [('Rooms','room_writer_semantics','effective_room_layout'),('Doors','door_manufacture_split_semantics','effective_door_layout'),('Traps','trap_model_word_semantics','effective_trap_layout')]:assert p['effective_catalogue_layouts'][family]==p[key][subkey]
    expected=dict(native_scenarios=33,complete_native_AP_ticks=99,complete_native_bind_calls=33,native_condition_dispatch_calls=93,native_action_dispatch_calls=57,
        recursive_tree_calls=237,native_repeat_decrement_calls=0,exact_original_source_node_scenarios=3,independent_full_Ghidra_controls=12,
        Ghidra_compared_regions=98,Ghidra_compared_bytes=358952,original_code_extents=12,static_only_extents=1,unique_executed_original_PCs=367,
        primary_data_blocks=3,exact_container_edits=852,preserved_source_files=420,preserved_executable_paths=7,newly_named_opaque_fields=0,source_writes=0)
    assert s['counts']==expected
    rows={r['case']['name']:r['outcome'] for r in s['native_executions']['replays']}
    assert len(rows['flags00-repeat1']['action_events'])==1 and len(rows['flags00-repeat2']['action_events'])==3
    assert len(rows['action-repeat255-parent1']['action_events'])==1 and len(rows['action-repeat1-parent2']['action_events'])==3
    assert rows['nested-actions']['world_gate']==0xA7 and len(rows['nested-condition-repeat2']['action_events'])==3
    assert all(rows[k]['world_gate']==0xA1 for k in ['missing-root-falls-back','missing-action-falls-back','duplicate-condition-first','duplicate-action-first'])
    assert rows['original-3DLevel-clock1']['world_gate']==0xA5 and rows['original-3DLevel-clock2147483648']['world_gate']==0xA7
    assert all(r['flags_byte_reads']==0 and r['repeat_decrement_calls']==0 for r in rows.values())
    assert not s['AP_flags_review']['global_nonuse_proved']
    serial=s['storage_validation'];assert serial['counts']['Triggers']['files']==109 and serial['counts']['Triggers']['empty_files_identity_only']==2
    assert serial['counts']['Triggers']['records']==25588 and serial['counts']['ActionPoints']['records']==3501
    assert (ROOT/'INTEGRATION_RU.md').read_text(encoding='utf8').startswith('Актуальный этап 156:')
    assert 'пакет содержит 135 компонентов' in (ROOT/'READINESS_RU.md').read_text(encoding='utf8')
    out=dict(phase=156,date='2026-10-31',**expected,package_components=135,previous134_components_preserved_byte_exact=True,
        physical_and_effective_views_unchanged=True,goal_achieved=False,significant_unknowns_remain=True,limitations=s['limitations'])
    (ROOT/'phase156-validation.json').write_text(json.dumps(out,ensure_ascii=False,indent=2),encoding='utf8')
    status=dict(phase=156,published_phase=156,date='2026-10-31',package_components=135,readiness_estimate_percent=75,
        previous_goal_turn_classification='No progress toward specification: user requested percentage status, so phase155 artifacts/active goal were revalidated and status reported. Next safe analysis now executed.',
        current_goal_turn_classification='Progress:33nativeAPbind/tree scenarios/99completeAPticks/12fullGhidracontrols,852exactcontaineredits. Newscopedlink/repeat semantics andsignedLEVEL_TIMEconfirmed;AP04/08notgloballyresolved.135componentspublished.',
        pending_native_jobs=[],pending_controls=[],goal_achieved=False,significant_unknowns_remain=True,
        evidence=[link(n) for n in ['ap-trigger-tree-semantics.json','AP_TRIGGER_TREE_RU.md','phase156-AP-tree-emulations.json','phase156-AP-tree-crosschecks.json',
            'phase156-AP-tree-primary.json','phase156-AP-tree-storage-validation.json','phase156-validation.json','editor-format-package.json','INTEGRATION_RU.md','READINESS_RU.md']],
        next_action='Do not repeat33APtrees/12Ghidra/852storageedits. FullordinaryAPticknowexecuted,04/08stillunresolved. Prioritizefreshremainingrawfieldconsumers (CreatureSpellsE8/E9,ElementB1/B2,Terrain198/224,Crypt10groups,Objects34C,Traps204,Doors236,CreatureJump/Priority). ForAP04/08onlyfollownewtypedconsumer: previouslylimitedphase76contexts needboundedreview,notanothergeneralnegativeclaim. MissingIDfallback/RepeatTimesrulesareAPspecific,notwholegamebug. Alloriginalsourcesandparalleleditorunchanged;full15familygoalactive.',
        limitations=s['limitations'])
    (ROOT/'phase156-search-status.json').write_text(json.dumps(status,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps({k:v for k,v in out.items() if k!='limitations'}))
if __name__=='__main__':main()
