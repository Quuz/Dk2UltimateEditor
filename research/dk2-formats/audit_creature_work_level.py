"""Audit141 components, rawCF2 level-up binding and preserved current/historical layouts."""
import json
from build_creature_work_level import ROOT,load,sha,link,build,effective_layout

def main():
    s=load('creature-work-level-semantics.json');assert s==build()
    p=load('editor-format-package.json');old=load('phase162-prior-components.json')
    assert len(p['components'])==141 and len(old)==140 and set(p['components'])-set(old)=={'creature_work_level_semantics'}
    for key,row in p['components'].items():
        b=(ROOT/row['file']).read_bytes();assert sha(b)==row['sha256'] and json.loads(b)==p[key]
        if key in old:assert row==old[key]
    assert p['effective_catalogue_layouts']['Creatures']==s['effective_creature_layout']==effective_layout()
    stripped=json.loads(json.dumps(s['effective_creature_layout']))
    for f in stripped['fields']:
        f.pop('verified_work_level',None)
        if 'legacy_semantics_before_phase162' in f:f['semantics']=f.pop('legacy_semantics_before_phase162')
    assert stripped==load('phase162-prior-effective-creatures.json')
    for family,key,subkey in [('Rooms','room_writer_semantics','effective_room_layout'),('Doors','native_door_reader_semantics','effective_door_layout'),
        ('Traps','trap_creation_activation_semantics','effective_trap_layout'),('Effects','effect_adjacent_semantics','effective_effect_layout'),
        ('Terrain','terrain_light_submission_semantics','effective_terrain_layout')]:assert p['effective_catalogue_layouts'][family]==p[key][subkey]
    for a,b in [('effective_shot_parameters',p['tremor_damage_semantics']['effective_shot_parameters']),
        ('effective_room_resource_consumption',p['room_resource_loader_semantics']['resource_consumption']),
        ('effective_creature_flag_enum',p['melee_animation_flag_semantics']['effective_creature_flag_enum'])]:assert p[a]==b
    expected=dict(native_scenarios=11,Creature_raw_loads=11,complete_levelup_calls=11,eligible_levelups=9,
        independent_full_Ghidra_controls=8,Ghidra_compared_regions=376,Ghidra_compared_bytes=787416,whole_memory_pairs=1,
        pair_compared_regions=46,pair_compared_bytes=98423,original_code_extents=72,static_only_function_extents=2,
        unique_executed_original_PCs=3296,primary_data_blocks=2,exact_changed_container_rebuilds=246,source_Creature_records=3734,source_Creature_files=82,
        generic_scalar_controls=15,extended_record_helper_controls=231,search_roots=739,search_states=18661,search_callee_contexts=48,
        search_limited_roots=4,search_limited_callees=2,newly_confirmed_gameplay_field_bindings=1,preserved_source_files=420,preserved_executable_paths=7,source_writes=0)
    assert s['counts']==expected
    rows={r['case']['name']:r['outcome'] for r in s['native_executions']['replays']}
    assert rows['quarter']['WorkModifier_after']==rows['default-only']['WorkModifier_after']==5120
    assert rows['one']['WorkModifier_after']==8192 and rows['minus-one-bitpattern']['WorkModifier_after']==4095
    assert rows['wrap']['WorkModifier_after']==0 and rows['level9']['level_after']==10
    assert not rows['level10']['active'] and not rows['dead-gate']['active']
    assert s['default_CEE_review']['status']=='initial_gameplay_consumer_unresolved' and not s['default_CEE_review']['global_nonuse_proved']
    assert not s['job_consumer_boundary']['actual_job_output_newly_executed']
    assert s['discovery_review']['failed_initial_fixture']['excluded']
    assert (ROOT/'INTEGRATION_RU.md').read_text(encoding='utf8').startswith('Актуальный этап 162:')
    assert 'пакет содержит 141 компонент' in (ROOT/'READINESS_RU.md').read_text(encoding='utf8')
    assert 'CF2' in (ROOT/'CREATURE_WORK_LEVEL_RU.md').read_text(encoding='utf8')
    out=dict(phase=162,date='2026-11-01',**expected,package_components=141,previous140_components_preserved_byte_exact=True,
        physical_widths_and_codec_unchanged=True,previous_Creatures_annotations_preserved=True,goal_achieved=False,significant_unknowns_remain=True,limitations=s['limitations'])
    (ROOT/'phase162-validation.json').write_text(json.dumps(out,ensure_ascii=False,indent=2),encoding='utf8')
    status=dict(phase=162,published_phase=162,date='2026-11-01',package_components=141,readiness_estimate_percent=75,
        previous_goal_turn_classification='Progress:phase161publishedrawTrap/factory/activation14scenarios/8Ghidra/246edits,140components;Unknown204unresolved.',
        current_goal_turn_classification='Progress:confirmedactualrawCF2WorkModifierLevelIncreaseconsumerthrough11fullrawloader/levelupscenarios,8wholeGhidra/246exactstoragecontrolsincluding231extendedtailcases;141components. CEEinitialconsumer/joboutputnotproved.',
        pending_native_jobs=[],pending_controls=[],goal_achieved=False,significant_unknowns_remain=True,
        evidence=[link(n) for n in ['creature-work-level-semantics.json','CREATURE_WORK_LEVEL_RU.md','phase162-creature-work-level-emulations.json',
            'phase162-creature-work-level-crosschecks.json','phase162-creature-work-level-primary.json','phase162-creature-work-storage-validation.json',
            'phase162-creature-work-bounded-leads.json','phase162-validation.json','editor-format-package.json','INTEGRATION_RU.md','READINESS_RU.md']],
        next_action='Do notrepeatCF2fulllevelup48B120/48B330,nowconfirmedActor3AFADDmod32;level10/HP<=0gatesknown. CEE793read51F3B0iscopydefinition,notActorinitialization. Actualjobconsumer496E00andother3AF/3B3pathsarestaticallyknownbutnotnewlyexecuted;couldtracefinaltile/workoutputnext. DistinguishWorkModifier3AFfromCryptWizardWorkEnhancement3B3. All3734sourceCEE/CF2zero. EF0/Jump/Priority/Terrain198/224/Doors236/Traps204/Objects34C/remainingCryptunknownsremain. Preserve141schemas andsourcebytes; full15familygoalactive.',limitations=s['limitations'])
    (ROOT/'phase162-search-status.json').write_text(json.dumps(status,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:v for k,v in out.items() if k!='limitations'}))
if __name__=='__main__':main()
