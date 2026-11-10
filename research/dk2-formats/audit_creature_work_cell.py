"""Audit142 components, rawCF2/Variables -> nativework -> actualTerrainHP, retainingold141schemas."""
import json
from build_creature_work_cell import ROOT,load,sha,link,build,effective_layout

def main():
    s=load('creature-work-cell-semantics.json');assert s==build()
    p=load('editor-format-package.json');old=load('phase163-prior-components.json')
    assert len(old)==141 and len(p['components'])==142 and set(p['components'])-set(old)=={'creature_work_cell_semantics'}
    for key,row in p['components'].items():
        b=(ROOT/row['file']).read_bytes();assert sha(b)==row['sha256'] and json.loads(b)==p[key]
        if key in old:assert row==old[key]
    assert p['effective_catalogue_layouts']['Creatures']==s['effective_creature_layout']==effective_layout()
    stripped=json.loads(json.dumps(s['effective_creature_layout']))
    for field in stripped['fields']:field.pop('verified_cell_work',None)
    assert stripped==load('phase163-prior-effective-creatures.json')
    for family,key,subkey in [('Rooms','room_writer_semantics','effective_room_layout'),('Doors','native_door_reader_semantics','effective_door_layout'),
        ('Traps','trap_creation_activation_semantics','effective_trap_layout'),('Effects','effect_adjacent_semantics','effective_effect_layout'),
        ('Terrain','terrain_light_submission_semantics','effective_terrain_layout')]:assert p['effective_catalogue_layouts'][family]==p[key][subkey]
    for a,b in [('effective_shot_parameters',p['tremor_damage_semantics']['effective_shot_parameters']),
        ('effective_room_resource_consumption',p['room_resource_loader_semantics']['resource_consumption']),
        ('effective_creature_flag_enum',p['melee_animation_flag_semantics']['effective_creature_flag_enum'])]:assert p[a]==b
    expected=dict(native_scenarios=13,Creature_raw_loads=13,Terrain_raw_conversions=13,Terrain_definition_loads=13,raw_variable_callbacks=39,
        complete_levelup_calls=12,eligible_levelups=11,whole_work_callbacks=13,CMap_work_calls=13,whole_Terrain_HP_calls=13,Terrain_change_calls=0,
        independent_full_Ghidra_controls=8,Ghidra_compared_regions=504,Ghidra_compared_bytes=1750840,original_code_extents=87,static_only_function_extents=0,
        unique_executed_original_PCs=4650,primary_data_blocks=3,exact_changed_container_rebuilds=3,action_storage_source_files=1,source_action_records=3,
        numeric_projection_controls=13,newly_named_opaque_fields=0,preserved_source_files=420,preserved_executable_paths=7,source_writes=0)
    assert s['counts']==expected
    rows={r['case']['name']:r for r in s['runtime_outcomes']}
    outcomes=dict(base=594,quarter=592,double=588,**{'no-level-up':594,'enhancement205':593,'both':592,'zero-modifier':600,
        'negative-modifier':606,'neutral':287,'enemy':600,'non-Imp':555,'level10':594,'action-minus100':593})
    assert {k:v['HP_after'] for k,v in rows.items()}==outcomes
    assert [r['action_per_tick'] for r in s['variables']]==[-250,-6,0]
    assert (ROOT/'INTEGRATION_RU.md').read_text(encoding='utf8').startswith('Актуальный этап 163:')
    assert 'пакет содержит 142 компонента' in (ROOT/'READINESS_RU.md').read_text(encoding='utf8')
    assert 'CF2' in (ROOT/'CREATURE_WORK_CELL_RU.md').read_text(encoding='utf8')
    out=dict(phase=163,date='2026-11-01',**expected,package_components=142,previous141_components_preserved_byte_exact=True,
        physical_widths_and_codec_unchanged=True,previous_Creatures_annotations_preserved=True,goal_achieved=False,significant_unknowns_remain=True,limitations=s['limitations'])
    (ROOT/'phase163-validation.json').write_text(json.dumps(out,ensure_ascii=False,indent=2),encoding='utf8')
    status=dict(phase=163,published_phase=163,date='2026-11-01',package_components=142,readiness_estimate_percent=75,
        previous_goal_turn_classification='No progress: userrequestedpercentassessment; currentartifactsinspectedand75percentreported; noschemaschanged. Revalidated141componentstateandcontinuedunfinishedphase163.',
        current_goal_turn_classification='Progress: publishedselectedactualCF2/levelup/workcallback/cellHPchain,13native/8wholeGhidra/3exactVariablesedits;142components. ExtendedCF2annotationwithoutmutatingprior141schemas.',
        pending_native_jobs=[],pending_controls=[],goal_achieved=False,significant_unknowns_remain=True,
        evidence=[link(n) for n in ['creature-work-cell-semantics.json','CREATURE_WORK_CELL_RU.md','phase163-creature-work-cell-emulations.json',
            'phase163-creature-work-cell-crosschecks.json','phase163-creature-work-cell-primary.json','phase163-creature-work-action-storage.json',
            'phase163-validation.json','editor-format-package.json','INTEGRATION_RU.md','READINESS_RU.md']],
        next_action='Do notrepeatCF2leveluporselected496E00workconsumer,nowactualRockHPconfirmed. CEEinitialActor3AFconsumerstillunknown;51F3B0isdefinitioncopy. SearchtrueActorinitialization orremainingEF0/Jump/Priority/opaqueTerrain/Traps/Doors/Objects/CryptfieldsusingtypedprovenpointeroriginsandboundedreturnABI. Preserve142schemas/420sources/7EXE; full15familygoalactive.',limitations=s['limitations'])
    (ROOT/'phase163-search-status.json').write_text(json.dumps(status,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:v for k,v in out.items() if k!='limitations'}))

if __name__=='__main__':main()
