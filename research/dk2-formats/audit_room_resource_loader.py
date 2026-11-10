"""Audit129 components and bounded activeRoom resource/checksum semantics."""
import json
from build_room_resource_loader import ROOT,load,sha,link,build

def main():
    value=load('room-resource-loader-semantics.json');assert value==build()
    p=load('editor-format-package.json');old=load('phase150-prior-components.json')
    assert len(p['components'])==129 and len(old)==128
    assert set(p['components'])-set(old)=={'room_resource_loader_semantics'}
    for key,row in p['components'].items():
        data=(ROOT/row['file']).read_bytes();assert sha(data)==row['sha256'] and json.loads(data)==p[key]
        if key in old:assert row==old[key]
    assert p['room_resource_loader_semantics']==value and p['effective_room_resource_consumption']==value['resource_consumption']
    assert p['effective_shot_parameters']==p['shot_lifecycle_semantics']['effective_shot_parameters']
    assert p['effective_creature_flag_enum']==p['melee_animation_flag_semantics']['effective_creature_flag_enum']
    assert p['effective_catalogue_layouts']['Rooms']==p['room_writer_semantics']['effective_room_layout']
    assert p['effective_catalogue_layouts']['Doors']==p['door_manufacture_split_semantics']['effective_door_layout']
    assert p['effective_catalogue_layouts']['Traps']==p['trap_model_word_semantics']['effective_trap_layout']
    c=value['counts']
    for key,wanted in dict(native_scenarios=62,source_Room_records_executed=26,diagnostic_construction_modes=13,whole_memory_pairs=19,
        independent_Ghidra_CPU_controls=6,Ghidra_compared_regions=312,Ghidra_compared_bytes=1440258,original_function_extents=21,
        unique_executed_original_PCs=2616,primary_data_blocks=5,new_exact_container_edits=328,public_decode_rebuild_controls=328,
        source_Room_records=2075,clarified_resource_loader_spans=4,newly_named_opaque_fields=0,preserved_source_files=310,preserved_executable_paths=7,source_writes=0).items():assert c[key]==wanted,(key,c[key])
    assert value['API_validation']['nonzero_resource_records']==value['API_validation']['nonempty_name_records']=={}
    assert value['checksum']['source_Temple_checksum']==6520 and value['checksum']['four_filled_checksum']==20980
    assert value['pairs']['compared_regions_per_pair']==51 and value['pairs']['compared_bytes_per_pair']==240039
    rows={r['case']['name']:r['outcome'] for r in value['native_executions']['replays']}
    assert rows['update-source']['checksum']==rows['update-filled']['checksum']==0x12345678
    assert not rows['no-service-source']['requests'] and not rows['no-service-filled']['requests']
    assert not rows['source-room19']['requests'] and rows['mode7-source']['requests']
    assert rows['mode7-source']['requests']!=rows['complete-control']['requests']
    assert 'Актуальный этап 150:' in (ROOT/'INTEGRATION_RU.md').read_text(encoding='utf8')
    assert 'пакет содержит 129 компонентов' in (ROOT/'READINESS_RU.md').read_text(encoding='utf8')
    out=dict(phase=150,date='2026-10-31',**c,package_components=129,previous128_components_preserved_byte_exact=True,
        real_assets_GPU_or_Worldtick_executed=False,global_nonuse_proven=False,goal_achieved=False,significant_unknowns_remain=True,limitations=value['limitations'])
    (ROOT/'phase150-validation.json').write_text(json.dumps(out,ensure_ascii=False,indent=2),encoding='utf8')
    status=dict(phase=150,published_phase=150,date='2026-10-31',package_components=129,readiness_estimate_percent=75,
        status='Published completeactiveRoom resource-request loader; fourraw84-byteblocks readonlybyMIDchecksum in13selectedmodes, positiveCompleteMeshcontrol works. Unknown1C4unnamed.',
        previous_goal_turn_classification='Progress:phase149boundedtwofreshunknownsearches andquarantined12falseCryptcounterchains,128components.',
        current_goal_turn_classification='Progress:62newnativepaths/19wholepairs/6Ghidra/328edits; actualactiveassetIO andnativeRoommetadata/checksum behavior confirmed,129components.',
        pending_native_jobs=[],pending_controls=[],goal_achieved=False,significant_unknowns_remain=True,
        evidence=[link(n) for n in ['room-resource-loader-semantics.json','ROOM_RESOURCE_LOADER_RU.md','phase150-room-resource-loader-emulations.json',
            'phase150-room-resource-loader-crosschecks.json','phase150-room-resource-loader-primary.json','phase150-room-resource-api-validation.json','phase150-validation.json','editor-format-package.json','INTEGRATION_RU.md','READINESS_RU.md']],
        next_action='Do not repeat62Roomcases/19pairs/6Ghidra/328edits. FourRoomresource spans11C/170/1C4/218 confirmedchecksum-onlyinselectedactiveLoader; allzero in2075sources, notglobaldeadfieldproof. Shift tootherunresolvedfamilies orfreshconsumerpaths: CreatureJump/Priority,Objects34C,Traps204,Doors236,Terrain198224,AP0408,KeeperC8. NativeRoomassetrequests nowconcrete; actualresource files/GPU/geometry notexecuted. Preserveunknown1C4/byte36F andallpriorphysical/APIviews. No originalfile/parallel editor edits.',
        limitations=value['limitations'])
    (ROOT/'phase150-search-status.json').write_text(json.dumps(status,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:v for k,v in out.items() if k!='limitations'}))

if __name__=='__main__':main()
