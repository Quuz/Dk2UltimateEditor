"""Audit130 components, mode8 wordarithmetic and complete selected Terrain transition."""
import json
from build_tremor_damage import ROOT,load,sha,link,build
import native_tremor as api

def main():
    value=load('tremor-damage-semantics.json');assert value==build()
    p=load('editor-format-package.json');old=load('phase151-prior-components.json')
    assert len(p['components'])==130 and len(old)==129
    assert set(p['components'])-set(old)=={'tremor_damage_semantics'}
    for key,row in p['components'].items():
        data=(ROOT/row['file']).read_bytes();assert sha(data)==row['sha256'] and json.loads(data)==p[key]
        if key in old:assert row==old[key]
    assert p['tremor_damage_semantics']==value and p['effective_shot_parameters']==api.effective_parameters()
    before=p['shot_lifecycle_semantics']['effective_shot_parameters'];after=p['effective_shot_parameters']
    assert after['processes']==before['processes']
    assert [k for k in after['damage_types'] if after['damage_types'][k]!=before['damage_types'][k]]==['8']
    assert p['effective_room_resource_consumption']==p['room_resource_loader_semantics']['resource_consumption']
    assert p['effective_creature_flag_enum']==p['melee_animation_flag_semantics']['effective_creature_flag_enum']
    assert p['effective_catalogue_layouts']['Rooms']==p['room_writer_semantics']['effective_room_layout']
    assert p['effective_catalogue_layouts']['Doors']==p['door_manufacture_split_semantics']['effective_door_layout']
    assert p['effective_catalogue_layouts']['Traps']==p['trap_model_word_semantics']['effective_trap_layout']
    c=value['counts']
    for key,wanted in dict(native_scenarios=24,complete_Shot_tick_scenarios=3,complete_terrain_transition_scenarios=3,native_Terrain_HP_calls=74,
        native_cells_destroyed=7,independent_Ghidra_CPU_controls=6,Ghidra_compared_regions=450,Ghidra_compared_bytes=2093064,
        original_function_extents=177,unique_executed_original_PCs=11453,primary_data_blocks=5,new_exact_container_edits=246,
        public_decode_rebuild_controls=246,source_Shot_records=5551,source_Terrain_records=3306,observed_source_mode8_records=0,
        newly_named_opaque_fields=0,preserved_source_files=310,preserved_executable_paths=7,source_writes=0).items():assert c[key]==wanted,(key,c[key])
    rows={r['case']['name']:r['outcome'] for r in value['native_executions']['replays']}
    assert rows['ordinary']['final_affected_HP']==[85]*3
    assert rows['second-full-tick']['final_affected_HP']==[5]*6
    assert rows['data1-negative80']['final_affected_HP']==[245]*3
    assert rows['data1-65536']['final_affected_HP']==[165]*3 and rows['data1-65537']['final_affected_HP']==[164]*3
    assert rows['data1-INTMIN']['final_affected_HP']==[165]*3
    assert rows['destroy-three']['final_affected_HP']==[1]*3 and rows['zeroHP-same-type']['final_affected_HP']==[0]*3
    assert len(rows['edge-zero']['requests'])==3 and len(rows['edge-last']['requests'])==8
    assert all(not rows[n]['requests'] for n in ['extent-zero','extent-subcell','extent-raw1','extent-negative'])
    assert all(r['low_reads_only_FS_SEH'] for r in rows.values())
    assert 'Актуальный этап 151:' in (ROOT/'INTEGRATION_RU.md').read_text(encoding='utf8')
    assert 'пакет содержит 130 компонентов' in (ROOT/'READINESS_RU.md').read_text(encoding='utf8')
    out=dict(phase=151,date='2026-10-31',**c,package_components=130,previous129_components_preserved_byte_exact=True,
        raw_Data1_Data2_storage_unchanged=True,actual_CMap_NAV_transition_executed=True,live_World_scheduler_or_GPU_executed=False,
        goal_achieved=False,significant_unknowns_remain=True,limitations=value['limitations'])
    (ROOT/'phase151-validation.json').write_text(json.dumps(out,ensure_ascii=False,indent=2),encoding='utf8')
    status=dict(phase=151,published_phase=151,date='2026-10-31',package_components=130,readiness_estimate_percent=75,
        status='Published complete selectedTremor cellHP/CMap/NAVdestruction andlow16arithmetic; originalrawloaders/factory andwholeShot ticks. No newopaquefieldname.',
        previous_goal_turn_classification='Progress:phase150published62activeRoomresourcepaths/19pairs/6Ghidra/328edits,129components.',
        current_goal_turn_classification='Progress:24nativepaths/74cellHPcalls/7actualchanges/6Ghidra/177ranges/246edits; validnativeNAVinitialization andstrictNULL-read rejection,130components.',
        pending_native_jobs=[],pending_controls=[],goal_achieved=False,significant_unknowns_remain=True,
        evidence=[link(n) for n in ['tremor-damage-semantics.json','TREMOR_DAMAGE_RU.md','phase151-tremor-emulations.json','phase151-tremor-crosschecks.json',
            'phase151-tremor-primary.json','phase151-tremor-api-validation.json','phase151-validation.json','editor-format-package.json','INTEGRATION_RU.md','READINESS_RU.md']],
        next_action='Do not repeat24Tremorcases/6Ghidra/246edits. Mode8terrainHPword/Q12/ownerLow12 andsourceRock->Dirt pathsconfirmed; newtraces do notreadunknownTerrain183/1B5. RemainingotherTerrain/max-healthtypechanges/gold/rooms/drop/effectsuccess/liveWorldscheduler remainopen. Prioritizefreshunknownfields/callers: CreatureJump/Priority,Objects34C,Traps204,Doors236,Terrain198224,AP0408,KeeperC8; preserveallcurrentphysicalmappings. NativeCMap/NAVvalidfixture nowavailablefornewterrainconsumers. Originaldata/EXE/parallel editor untouched.',
        limitations=value['limitations'])
    (ROOT/'phase151-search-status.json').write_text(json.dumps(status,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:v for k,v in out.items() if k!='limitations'}))

if __name__=='__main__':main()
