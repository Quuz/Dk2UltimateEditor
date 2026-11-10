"""Audit138 components and bounded Terrain->actualLight evidence, source preservation and effective annotations."""
import json
from build_terrain_light import ROOT,load,sha,link,build,effective_layout

def main():
    s=load('terrain-light-submission-semantics.json');assert s==build()
    p=load('editor-format-package.json');old=load('phase159-prior-components.json')
    assert len(p['components'])==138 and len(old)==137 and set(p['components'])-set(old)=={'terrain_light_submission_semantics'}
    for key,row in p['components'].items():
        b=(ROOT/row['file']).read_bytes();assert sha(b)==row['sha256'] and json.loads(b)==p[key]
        if key in old:assert row==old[key]
    assert p['effective_catalogue_layouts']['Terrain']==s['effective_terrain_layout']==effective_layout()
    stripped=json.loads(json.dumps(s['effective_terrain_layout']))
    for f in stripped['fields']:f.pop('verified_tile_light_submission',None)
    assert stripped==load('phase159-prior-effective-terrain.json')
    for family,key,subkey in [('Rooms','room_writer_semantics','effective_room_layout'),('Doors','door_manufacture_split_semantics','effective_door_layout'),
        ('Traps','trap_model_word_semantics','effective_trap_layout'),('Effects','effect_adjacent_semantics','effective_effect_layout')]:
        assert p['effective_catalogue_layouts'][family]==p[key][subkey]
    for a,b in [('effective_shot_parameters',p['tremor_damage_semantics']['effective_shot_parameters']),
        ('effective_room_resource_consumption',p['room_resource_loader_semantics']['resource_consumption']),
        ('effective_creature_flag_enum',p['melee_animation_flag_semantics']['effective_creature_flag_enum'])]:assert p[a]==b
    expected=dict(native_scenarios=15,raw_Terrain_loads=30,native_cache_fills=15,actual_Light_factory_calls=15,actual_Lights_created=15,
        full_tile_light_calls=15,submitted_lights=12,independent_full_Ghidra_controls=8,Ghidra_compared_regions=488,Ghidra_compared_bytes=1603200,
        whole_memory_pairs=1,pair_compared_bytes=200400,original_code_extents=26,static_only_function_extents=3,unique_executed_original_PCs=1586,
        primary_data_blocks=5,exact_changed_container_rebuilds=328,source_Terrain_records=3306,source_Terrain_files=82,newly_named_opaque_fields=0,
        excluded_invalid_discovery_fixtures=1,preserved_source_files=420,preserved_executable_paths=7,source_writes=0)
    assert s['counts']==expected
    rows={r['case']['name']:r['outcome'] for r in s['native_executions']['replays']}
    assert rows['encoded-high']['runtime_RGB_WORDs']==[510,0,256]
    assert rows['global-dirt']['global_colour_override'] and not rows['global-gold']['global_colour_override']
    assert rows['fallback-global']['global_colour_override'] and rows['fallback-global']['selected_terrain']==38
    assert not rows['fallback-colour']['global_colour_override'] and rows['fallback-colour']['selected_terrain']==38
    assert rows['negative-height']['position_Q12'][2]==0 and rows['raised-floor']['position_Q12'][2]==8192 and rows['negative-floor']['position_Q12'][2]==-2048
    for name in ['disabled','cellC-blocked','fallback-disabled']:assert not rows[name]['submitted']
    assert all(r['unknown_definition_reads']==0 and r['native_Light_tag']==1 for r in rows.values())
    assert not s['opaque_review']['global_nonuse_proved'] and not s['opaque_review']['radius_assignment_proved']
    assert s['discovery_review']['invalid_fixture']['excluded'] and not s['static_bootstrap']['executed']
    assert s['profile_comparison']['same_text'] and s['profile_comparison']['text_raw_bytes']==2432000
    assert (ROOT/'INTEGRATION_RU.md').read_text(encoding='utf8').startswith('Актуальный этап 159:')
    assert 'пакет содержит 138 компонентов' in (ROOT/'READINESS_RU.md').read_text(encoding='utf8')
    assert 'Unk198' in (ROOT/'TERRAIN_LIGHT_SUBMISSION_RU.md').read_text(encoding='utf8')
    out=dict(phase=159,date='2026-11-01',**expected,package_components=138,previous137_components_preserved_byte_exact=True,
        physical_widths_and_codec_unchanged=True,previous_Terrain_annotations_preserved=True,goal_achieved=False,significant_unknowns_remain=True,limitations=s['limitations'])
    (ROOT/'phase159-validation.json').write_text(json.dumps(out,ensure_ascii=False,indent=2),encoding='utf8')
    status=dict(phase=159,published_phase=159,date='2026-11-01',package_components=138,readiness_estimate_percent=75,
        previous_goal_turn_classification='No progress against researchgoal: status answer confirmed137components/75percent; no new formatproof. Revalidated unfinishedTerrainfixtures and liveGhidra before continuation.',
        current_goal_turn_classification='Progress:15raw/cache/actualLight/full441CD0nativepaths,8wholeGhidracontrols,328exactedits; RGB/GoldValueoverride/height/floor/selectiongatesestablished. Invaliddiscoveryfixtureexcluded; Unk198/224remainunresolved.138components.',
        pending_native_jobs=[],pending_controls=[],goal_achieved=False,significant_unknowns_remain=True,
        evidence=[link(n) for n in ['terrain-light-submission-semantics.json','TERRAIN_LIGHT_SUBMISSION_RU.md','phase159-terrain-light-emulations.json',
            'phase159-terrain-light-crosschecks.json','phase159-terrain-light-primary.json','phase159-terrain-light-storage-validation.json',
            'phase159-validation.json','editor-format-package.json','INTEGRATION_RU.md','READINESS_RU.md']],
        next_action='Do not repeat selected441CD0Lightpipeline; raw198/224notreadhere, Light117staysbootstrap1200. Prioritize a fresh typed consumer for Terrain198/224 or remaining CreatureJump/Priority/Doors236/Traps204/Objects34C. Existing pointersearchoutparamflow452DC0hasnowbeenfollowedtoactualLight. FullWorld/KLDloader/GPU/bootstrapnotproved. OrdinaryandDX.textidenticalonly;RC3captionsarenotbindings. Significant15familyunknownsremain; preserveold138schemas andunknownbytes.',limitations=s['limitations'])
    (ROOT/'phase159-search-status.json').write_text(json.dumps(status,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:v for k,v in out.items() if k!='limitations'}))
if __name__=='__main__':main()
