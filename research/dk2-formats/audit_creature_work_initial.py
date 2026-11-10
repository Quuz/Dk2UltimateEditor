"""Audit143components andactualinitialWorkModifier3276, preservingCEE/globalunknownboundaries."""
import json
from build_creature_work_initial import ROOT,load,sha,link,build,effective_layout

def main():
    s=load('creature-work-initial-semantics.json');assert s==build()
    p=load('editor-format-package.json');old=load('phase164-prior-components.json')
    assert len(old)==142 and len(p['components'])==143 and set(p['components'])-set(old)=={'creature_work_initial_semantics'}
    for key,row in p['components'].items():
        b=(ROOT/row['file']).read_bytes();assert sha(b)==row['sha256'] and json.loads(b)==p[key]
        if key in old:assert row==old[key]
    assert p['effective_catalogue_layouts']['Creatures']==s['effective_creature_layout']==effective_layout()
    stripped=json.loads(json.dumps(s['effective_creature_layout']))
    for field in stripped['fields']:field.pop('verified_work_initial',None)
    assert stripped==load('phase164-prior-effective-creatures.json')
    for family,key,subkey in [('Rooms','room_writer_semantics','effective_room_layout'),('Doors','native_door_reader_semantics','effective_door_layout'),
        ('Traps','trap_creation_activation_semantics','effective_trap_layout'),('Effects','effect_adjacent_semantics','effective_effect_layout'),
        ('Terrain','terrain_light_submission_semantics','effective_terrain_layout')]:assert p['effective_catalogue_layouts'][family]==p[key][subkey]
    for a,b in [('effective_shot_parameters',p['tremor_damage_semantics']['effective_shot_parameters']),
        ('effective_room_resource_consumption',p['room_resource_loader_semantics']['resource_consumption']),
        ('effective_creature_flag_enum',p['melee_animation_flag_semantics']['effective_creature_flag_enum'])]:assert p[a]==b
    expected=dict(native_scenarios=8,Creature_raw_loads=8,Keeper4_raw_loads=8,whole_Creature_constructors=8,whole_Creature_factories=8,
        whole_Creature_initializers=8,native_WorkModifier_literal_assignments=8,whole_memory_pairs=6,pair_compared_regions=288,pair_compared_bytes=592200,
        independent_full_Ghidra_controls=4,Ghidra_compared_regions=196,Ghidra_compared_bytes=394816,original_code_extents=115,unique_executed_original_PCs=5042,
        primary_data_blocks=2,new_storage_edits=0,inherited_CF2_CEE_exact_container_edits=246,newly_named_opaque_fields=0,excluded_incomplete_fixture_files=2,
        preserved_source_files=420,preserved_executable_paths=7,source_writes=0)
    assert s['counts']==expected and len(s['paired_controls'])==6
    assert all(r['Actor_WorkModifier']==3276 and r['Actor_WorkEnhancement']==0 for r in s['runtime_outcomes'])
    assert not s['CEE_binding']['global_nonuse_proved'] and all(not r['global_nonuse_proved'] for r in s['paired_controls'])
    assert s['primary']['additional_callback_entry_proofs']==[dict(entry=0x4D2A30,original_PUSH=0x4D55CF,PUSH_executed=True,bytes_hex='68302a4d00')]
    assert (ROOT/'INTEGRATION_RU.md').read_text(encoding='utf8').startswith('Актуальный этап 164:')
    assert 'пакет содержит 143 компонента' in (ROOT/'READINESS_RU.md').read_text(encoding='utf8')
    assert '3276' in (ROOT/'CREATURE_WORK_INITIAL_RU.md').read_text(encoding='utf8')
    out=dict(phase=164,date='2026-11-01',**expected,package_components=143,previous142_components_preserved_byte_exact=True,
        physical_widths_and_codec_unchanged=True,previous_Creatures_annotations_preserved=True,goal_achieved=False,significant_unknowns_remain=True,limitations=s['limitations'])
    (ROOT/'phase164-validation.json').write_text(json.dumps(out,ensure_ascii=False,indent=2),encoding='utf8')
    status=dict(phase=164,published_phase=164,date='2026-11-01',package_components=143,readiness_estimate_percent=75,
        previous_goal_turn_classification='PreviouscompletedturnwasNoProgress(percentassessmentonly); revalidatedcurrentstateandpublishedphase163work/cellchaininthiscontinuationbeforephase164.',
        current_goal_turn_classification='Progress:actualrawCreature/Keeper4->nativeconstructor/free-slotfactory/fullinitializerprovesliteral3276/0Actor3AF/3B3,8native/6wholememorypairs/4wholeGhidra/115extents;143components. CEEdoesnotdetermineselectedinitialvalue,globalconsumerstillunknown.',
        pending_native_jobs=[],pending_controls=[],goal_achieved=False,significant_unknowns_remain=True,
        evidence=[link(n) for n in ['creature-work-initial-semantics.json','CREATURE_WORK_INITIAL_RU.md','phase164-creature-work-initial-emulations.json',
            'phase164-creature-work-initial-crosschecks.json','phase164-creature-work-initial-primary.json','phase164-validation.json',
            'editor-format-package.json','INTEGRATION_RU.md','READINESS_RU.md']],
        next_action='Do notrepeatCF2levelup/selected496E00workconsumerorinitialActor3AFcreation:3276literalat48746Cconfirmedwithrealrawdefinitions/factory. CEEdoesnotdetermineselectedinitialvalue; otherCEEconsumersunresolved. PreferremainingEF0/Jump/Priority/opaqueTerrain198/224/Doors236/Traps204/Objects34C/unknownCryptgroupsusingtypedpointerproofsandboundedABI; fullfactory->levelup->workcompositionnotyetexecuted. Preserve143schemas/420sources/7EXE; full15familygoalactive.',limitations=s['limitations'])
    (ROOT/'phase164-search-status.json').write_text(json.dumps(status,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:v for k,v in out.items() if k!='limitations'}))

if __name__=='__main__':main()
