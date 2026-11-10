"""Audit phase165, original data/code preservation, and bounded Stat19 claims."""
import json
from pathlib import Path
from build_terrain_text_stat import ROOT,load,sha,link,build,effective_layout

def main():
    s=load('terrain-text-stat-semantics.json');assert s==build()
    p=load('editor-format-package.json');old=load('phase165-prior-components.json')
    assert len(old)==143 and len(p['components'])==144 and set(p['components'])-set(old)=={'terrain_text_stat_semantics'}
    for key,row in p['components'].items():
        b=(ROOT/row['file']).read_bytes();assert sha(b)==row['sha256'] and json.loads(b)==p[key]
        if key in old:assert row==old[key]
    assert p['effective_catalogue_layouts']['Terrain']==s['effective_terrain_layout']==effective_layout()
    stripped=json.loads(json.dumps(s['effective_terrain_layout']))
    for f in stripped['fields']:f.pop('verified_editor_text_stat',None)
    assert stripped==load('phase165-prior-effective-terrain.json')
    for family,key,subkey in [('Creatures','creature_work_initial_semantics','effective_creature_layout'),
        ('Rooms','room_writer_semantics','effective_room_layout'),('Doors','native_door_reader_semantics','effective_door_layout'),
        ('Traps','trap_creation_activation_semantics','effective_trap_layout'),('Effects','effect_adjacent_semantics','effective_effect_layout')]:
        assert p['effective_catalogue_layouts'][family]==p[key][subkey]
    for a,b in [('effective_shot_parameters',p['tremor_damage_semantics']['effective_shot_parameters']),
        ('effective_room_resource_consumption',p['room_resource_loader_semantics']['resource_consumption']),
        ('effective_creature_flag_enum',p['melee_animation_flag_semantics']['effective_creature_flag_enum'])]:assert p[a]==b
    expected=dict(native_scenarios=13,original_raw_conversions=15,allocated_model_callbacks=15,
        whole_Terrain_model_constructors=15,whole_text_callbacks=13,selected_Stat19_assignments=10,complete_writer_runs=13,
        actual_saved_output_reload_cases=2,independent_full_Ghidra_controls=5,Ghidra_compared_regions=306,Ghidra_compared_bytes=1468523,
        new_original_function_extents=94,reused_original_function_extents=34,static_only_function_extents=1,
        unique_executed_original_PCs=4552,constant_or_vtable_blocks=3,exact_changed_container_rebuilds=246,
        source_Terrain_files=82,source_Terrain_records=3306,new_editor_text_stat_bindings=1,newly_named_opaque_gameplay_fields=0,
        preserved_source_files=420,preserved_executable_paths=7,source_writes=0)
    assert s['counts']==expected
    byname={r['case']['name']:r for r in s['runtime_outcomes']}
    assert byname['minus-one-reload']['saved_raw224']==0xFFFFF000 and byname['minus-one-reload']['reloaded_model_double']==1048575.0
    assert byname['three-half-reload']['saved_raw224']==0x3800 and byname['three-half-reload']['reloaded_model_double']==3.5
    assert all(byname[k]['saved_raw224']==0x1234 and not byname[k]['selected_assignment_executed'] for k in ['dash-skip','minus-zero-skip','code20-ignore'])
    assert byname['zero']['saved_raw224']==0 and byname['zero']['selected_assignment_executed']
    assert all(r['low_page_preserved'] and r['only_attested_FS0_low_accesses'] for r in s['runtime_outcomes'])
    assert s['field']['gameplay_meaning']=='unresolved' and s['field']['native_editor_label'] is None
    current=s['preservation'];historical=load('creature-work-initial-semantics.json')['preservation']
    before={r['file']:r['sha256'] for r in historical['source_files']}
    drift=[r for r in current['source_files'] if r['sha256']!=before[r['file']]]
    assert len(drift)==len(current['historical_phase164_drift'])==4
    assert {r['file'] for r in drift}=={r['file'] for r in current['historical_phase164_drift']}
    assert current['executable_paths']==historical['executable_paths']
    assert 'after native/Ghidra checks, before publication' in current['baseline_scope']
    assert (ROOT/'INTEGRATION_RU.md').read_text(encoding='utf8').startswith('Актуальный этап 165:')
    assert 'пакет содержит 144 компонента' in (ROOT/'READINESS_RU.md').read_text(encoding='utf8')
    assert '1048575' in (ROOT/'TERRAIN_TEXT_STAT_RU.md').read_text(encoding='utf8')
    out=dict(phase=165,date='2026-11-02',**expected,package_components=144,previous143_components_preserved_byte_exact=True,
        physical_widths_and_codec_unchanged=True,previous_Terrain_annotations_preserved=True,explicit_historical_source_differences=4,
        goal_achieved=False,significant_unknowns_remain=True,limitations=s['limitations'])
    (ROOT/'phase165-validation.json').write_text(json.dumps(out,ensure_ascii=False,indent=2),encoding='utf8')
    status=dict(phase=165,published_phase=165,date='2026-11-02',package_components=144,readiness_estimate_percent=75,
        previous_goal_turn_classification='Progress: pinnedDk2maptoolsource review excludedstubcataloguehandlersasnewsemantic evidence; nextactionremainedoriginalEXEpendingTerrainStat19.',
        current_goal_turn_classification='Progress:13native/5wholeGhidra/246losslessedits; genuineoriginalTerrainStat19->model320->raw224save/reloadbindingpublished. Fourpreexistingmapchangesrecordedwithoutrollingbackhistoricalevidence.',
        pending_native_jobs=[],pending_controls=[],goal_achieved=False,significant_unknowns_remain=True,
        evidence=[link(n) for n in ['terrain-text-stat-semantics.json','TERRAIN_TEXT_STAT_RU.md','phase165-terrain-text-emulations.json',
            'phase165-Terrain-text-crosschecks.json','phase165-terrain-text-primary.json','phase165-source-baseline.json','phase165-validation.json',
            'editor-format-package.json','INTEGRATION_RU.md','READINESS_RU.md']],
        next_action='Do notrepeat13acceptedTerrainStat19cases. Gameplaypurpose224stillunknown; pursue typedpointerlead48131F/6A0onlyafterfamilyproof or remaining game consumers/opaque fields. Full4A39B0headergrammarnotexecuted. Crypt8/18, CEE/EF0/Jump/Priority, other15familyunknownsremainopen. Preserve144components andrecordedcurrent420sourcebaseline/7EXE.',
        limitations=s['limitations'])
    (ROOT/'phase165-search-status.json').write_text(json.dumps(status,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:v for k,v in out.items() if k!='limitations'}))

if __name__=='__main__':main()
