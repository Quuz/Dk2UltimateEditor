"""Audit139 components, native Door file/World semantics and unchanged source bytes."""
import json
from build_native_door_reader import ROOT,load,sha,link,build,effective_layout

def main():
    s=load('native-door-reader-semantics.json');assert s==build()
    p=load('editor-format-package.json');old=load('phase160-prior-components.json')
    assert len(p['components'])==139 and len(old)==138 and set(p['components'])-set(old)=={'native_door_reader_semantics'}
    for key,row in p['components'].items():
        b=(ROOT/row['file']).read_bytes();assert sha(b)==row['sha256'] and json.loads(b)==p[key]
        if key in old:assert row==old[key]
    assert p['effective_catalogue_layouts']['Doors']==s['effective_door_layout']==effective_layout()
    stripped=json.loads(json.dumps(s['effective_door_layout']))
    for f in stripped['fields']:f.pop('verified_native_catalogue',None)
    assert stripped==load('phase160-prior-effective-doors.json')
    for family,key,subkey in [('Rooms','room_writer_semantics','effective_room_layout'),('Traps','trap_model_word_semantics','effective_trap_layout'),
        ('Effects','effect_adjacent_semantics','effective_effect_layout'),('Terrain','terrain_light_submission_semantics','effective_terrain_layout')]:
        assert p['effective_catalogue_layouts'][family]==p[key][subkey]
    for a,b in [('effective_shot_parameters',p['tremor_damage_semantics']['effective_shot_parameters']),
        ('effective_room_resource_consumption',p['room_resource_loader_semantics']['resource_consumption']),
        ('effective_creature_flag_enum',p['melee_animation_flag_semantics']['effective_creature_flag_enum'])]:assert p[a]==b
    expected=dict(native_scenarios=21,successful_scenarios=20,native_reader_entries=22,native_reader_normal_returns=21,expected_divide_exceptions=1,
        native_header_callbacks=18,native_World_callbacks=91,native_definition_loads=91,native_RAW_converters=93,native_MID_initializers=93,
        native_prepared_array_slot_constructors=252,Windows_read_requests=134,independent_full_Ghidra_controls=11,Ghidra_compared_regions=257,
        Ghidra_compared_bytes=569972,whole_memory_pairs=1,pair_compared_regions=22,pair_compared_bytes=53348,
        original_code_extents=24,static_only_function_extents=2,unique_executed_original_PCs=911,primary_data_blocks=5,
        exact_changed_container_rebuilds=164,source_Door_records=656,source_Door_files=82,newly_named_opaque_fields=0,
        preserved_source_files=420,preserved_executable_paths=7,source_writes=0)
    assert s['counts']==expected
    rows={r['case']['name']:r['outcome'] for r in s['native_executions']['replays']}
    assert rows['source']['sorted_runtime_IDs']==rows['source-two-pass']['sorted_runtime_IDs']==rows['reverse-source']['sorted_runtime_IDs']==list(range(1,9))
    assert rows['source-appended']['sorted_runtime_IDs']==list(range(1,10)) and rows['source-appended']['runtime_unknown_WORDs'][-1]==0xBEEF
    assert rows['duplicate-low']['lookup_slot']==0 and rows['duplicate-high']['lookup_slot']==3
    assert rows['zero-id']['lookup_pointer']==rows['missing-id']['lookup_pointer']==0
    assert rows['capacity-two']['processed_records']==4 and rows['capacity-two']['catalogue_records']==2 and rows['capacity-two']['reader_return']==1
    assert rows['update-existing']['World_checksum']==0x55555555
    assert rows['truncated-tail']['reader_return']==1 and rows['truncated-tail']['sorted_runtime_IDs']==[1,1] and rows['truncated-tail']['short_reads']==1
    assert rows['zero-count']['expected_divide_error'] and rows['zero-count']['reader_return'] is None and rows['zero-count-skip']['reader_return']==1
    assert rows['wrong-header']['reader_return']==rows['wrong-data']['reader_return']==0
    assert not s['opaque_review']['global_nonuse_proved'] and s['opaque_review']['status']=='unresolved'
    assert (ROOT/'INTEGRATION_RU.md').read_text(encoding='utf8').startswith('Актуальный этап 160:')
    assert 'пакет содержит 139 компонентов' in (ROOT/'READINESS_RU.md').read_text(encoding='utf8')
    assert 'Unknown236' in (ROOT/'NATIVE_DOOR_READER_RU.md').read_text(encoding='utf8')
    out=dict(phase=160,date='2026-11-01',**expected,package_components=139,previous138_components_preserved_byte_exact=True,
        physical_widths_and_codec_unchanged=True,previous_Doors_annotations_preserved=True,goal_achieved=False,significant_unknowns_remain=True,limitations=s['limitations'])
    (ROOT/'phase160-validation.json').write_text(json.dumps(out,ensure_ascii=False,indent=2),encoding='utf8')
    status=dict(phase=160,published_phase=160,date='2026-11-01',package_components=139,readiness_estimate_percent=75,
        previous_goal_turn_classification='Progress:phase159published15Terrain/raw/cache/Lightpaths,8wholeGhidra/328exactstoragecontrols;138components. Unk198/224 unresolved.',
        current_goal_turn_classification='Progress:whole56AC80/MyFile/Disc->nativeDoorWorldcallbacks->lookup,21scenarios/22readerentries/11wholeGhidra/164append-reordercontrols. IDsort/duplicates/capacity/update/shortreadacceptance/DIV0limits confirmed. Unknown236preserved/unresolved.139components.',
        pending_native_jobs=[],pending_controls=[],goal_achieved=False,significant_unknowns_remain=True,
        evidence=[link(n) for n in ['native-door-reader-semantics.json','NATIVE_DOOR_READER_RU.md','phase160-native-door-reader-emulations.json',
            'phase160-native-door-reader-crosschecks.json','phase160-native-door-reader-primary.json','phase160-door-catalogue-storage-validation.json',
            'phase160-door-triage-review.json','phase160-validation.json','editor-format-package.json','INTEGRATION_RU.md','READINESS_RU.md']],
        next_action='Do not repeat Door236loader/WORDsplit ornewnativeDoorreader/callbacks/lookup cases. Unknown236 remainsunknown; whole181-bytecopy/checksum isstorage, notgameplaymeaning. Fresh typedconsumer for Objects34C/Traps204/CreatureJump-Priority/Terrain198-224 orremainingglobal/Crypt fields. Outerdispatcher/open/Worldallocationbootstrap/GPU notexecuted; DIV0andtruncatedsuccessare scopedreaderfindings. Strictsourceframing andallunknownbytes preserved; full15familygoalactive.',limitations=s['limitations'])
    (ROOT/'phase160-search-status.json').write_text(json.dumps(status,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:v for k,v in out.items() if k!='limitations'}))
if __name__=='__main__':main()
