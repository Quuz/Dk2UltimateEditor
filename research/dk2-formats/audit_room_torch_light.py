"""Audit actual light CPU outputs, scoped semantics, preservation and package."""
from pathlib import Path
import json
from build_room_torch_light import ROOT,load,build,link,native
from build_poison import effective_shot_parameters

def main():
    value=load('room-torch-light-semantics.json');assert value==build()
    package=load('editor-format-package.json');prior=load('phase123-prior-components.json')
    assert len(package['components'])==102 and len(prior)==101
    assert set(package['components'])-set(prior)=={'room_torch_light_semantics'}
    for key,row in package['components'].items():
        b=(ROOT/row['file']).read_bytes();assert native.sha(b)==row['sha256'] and json.loads(b)==package[key]
        if key in prior:assert row==prior[key]
    assert package['room_torch_light_semantics']==value
    assert package['effective_catalogue_layouts']['Rooms']==package['room_writer_semantics']['effective_room_layout']
    assert package['effective_shot_parameters']==effective_shot_parameters()
    assert package['effective_object_resource_bindings']['slots'][2]['display_name']=='EditorMapIconResource'
    assert package['outstanding_controls_from_phases114_115']==0
    assert 'Актуальный этап 123:' in (ROOT/'INTEGRATION_RU.md').read_text(encoding='utf8')
    assert 'пакет содержит 102 компонента' in (ROOT/'READINESS_RU.md').read_text(encoding='utf8')
    assert sum(r['outcome']['created_render_record'] for r in value['native_executions'])==13
    primary=load('phase123-primary-manifest.json')
    assert len(primary['fresh_function_ranges'])==11 and len(primary['inherited_allocator_ranges'])==2 and len(primary['blocks'])==3
    controls=value['independent_Ghidra_controls'];assert sum(r['checked_regions'] for r in controls)==18 and sum(r['checked_bytes'] for r in controls)==4556
    output=dict(phase=123,date='2026-10-29',whole_original_light_render_cases=16,
        actual_light_record_creations=13,independent_Ghidra_CPU_controls=2,
        Ghidra_compared_regions=18,Ghidra_compared_bytes=4556,
        original_native_allocator_and_queue_executed=True,game_function_or_vtable_substitutions=0,external_API_adapters=0,
        key_primary_function_spans=13,original_primary_data_blocks=3,
        typed_Room_to_Light_route_static=True,whole_RawRoom_to_wall_light_CPU_chain_executed=False,
        native_normalization_divisor=128,intensity0_still_queues_when_radius_positive=True,
        final_pixel_brightness_or_full_GPU_frame_verified=False,
        original_Room_files_preserved=82,Room_records_inspected=2075,original_executable_paths_preserved=6,
        new_container_rebuilds=0,previous101_components_preserved_byte_exact=True,package_components=102,
        goal_achieved=False,significant_unknowns_remain=True,limitations=value['limitations'])
    (ROOT/'phase123-validation.json').write_text(json.dumps(output,ensure_ascii=False,indent=2),encoding='utf8')
    status=dict(phase=123,published_phase=123,date='2026-10-29',
        status='Published nativeCLight intensity normalization andtwo actual fullrender-record allocator/queue paths;scoped preparedscene,notfullpixels.',
        previous_goal_turn_classification='progress:phase122101componentpublication correctedTorchIntensityu8,wholeRoomwriter/calendar and246rebuilds;newtypedlightstaticrouteattested.',
        current_goal_turn_classification='progress:16wholeLight render functionswithoriginalallocator,13records,2Ghidra controls;N/128unitsconfirmed and102componentpackage.',
        pending_native_jobs=[],pending_controls=[],package_components=102,readiness_estimate_percent=75,
        evidence=[link(n) for n in ['room-torch-light-semantics.json','ROOM_TORCH_LIGHT_RU.md',
            'phase123-primary-manifest.json','phase123-light-render-emulations.json','phase123-light-render-crosschecks.json',
            'phase123-validation.json','INTEGRATION_RU.md','READINESS_RU.md','editor-format-package.json']],
        next_action='Do notrepeat16Lightcases/2Ghidra controls orphase122writer rebuilds. Remainingunknowns stillpriority:Room1C4,Traps204,Doors236,CreatureJumpEE0/PriorityF55,Terrain198/224,KeeperC8 andElementB1/B2. UseavailableoriginalProserver8090 fornewtypedGUI/read/save consumer ratherthanliteralrescan ofclosed24Room280leads. Renderer N/128recordconsumer canextendto finalattenuationonly if newfieldmeaning,notmoreunchangedfixtures. Full15familyobjective remainsactive;no modpatch ororiginalsourcewrites authorized.',
        goal_achieved=False,significant_unknowns_remain=True,limitations=value['limitations'])
    (ROOT/'phase123-search-status.json').write_text(json.dumps(status,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:v for k,v in output.items() if k!='limitations'}))

if __name__=='__main__':main()
