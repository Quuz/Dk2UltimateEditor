"""Audit131 components, native pre-cast semantics and exact source preservation."""
import json
from build_keeper_validation import ROOT,load,sha,link,build
import native_keeper_validation as api
def main():
    value=load('keeper-validation-semantics.json');assert value==build()
    p=load('editor-format-package.json');old=load('phase152-prior-components.json')
    assert len(p['components'])==131 and len(old)==130 and set(p['components'])-set(old)=={'keeper_validation_semantics'}
    for key,row in p['components'].items():
        b=(ROOT/row['file']).read_bytes();assert sha(b)==row['sha256'] and json.loads(b)==p[key]
        if key in old:assert row==old[key]
    assert p['effective_shot_parameters']==p['tremor_damage_semantics']['effective_shot_parameters']
    assert p['effective_room_resource_consumption']==p['room_resource_loader_semantics']['resource_consumption']
    assert p['effective_creature_flag_enum']==p['melee_animation_flag_semantics']['effective_creature_flag_enum']
    assert p['effective_catalogue_layouts']['Rooms']==p['room_writer_semantics']['effective_room_layout']
    assert p['effective_catalogue_layouts']['Doors']==p['door_manufacture_split_semantics']['effective_door_layout']
    assert p['effective_catalogue_layouts']['Traps']==p['trap_model_word_semantics']['effective_trap_layout']
    c=value['counts']
    expected=dict(native_scenarios=82,full_coordinate_validator_scenarios=42,full_creature_validator_scenarios=40,C8_boundary_scenarios=10,
        independent_Ghidra_CPU_controls=10,Ghidra_compared_regions=260,Ghidra_compared_bytes=599450,original_code_extents=32,
        unique_executed_original_PCs=1407,primary_data_blocks=2,source_files=82,source_Keeper_records=1066,new_exact_container_edits=246,
        public_decode_rebuild_controls=246,preserved_source_files=310,preserved_executable_paths=7,newly_named_opaque_fields=0,source_writes=0)
    assert c==expected
    rows={r['case']['name']:r['outcome'] for r in value['native_executions']['replays']}
    for name in ['sight-point','possession-creature','call-to-arms-point','secret1-Heal-own-creature','mana-equal','cooldown-boundary','cast1-land8-allied1','target1-inverted-owner8']:
        assert rows[name]['return_value']==1,name
    for name in ['mana-below','cooldown-before','possession-Horny','possession-actor-flag8','possession-actor-flag8000','cast1-land8-allied0','target3-owner0','cast2-flag2-marked0']:
        assert rows[name]['return_value']==0,name
    assert all(r['C8_definition_reads']==0 for r in rows.values()) and not value['C8']['global_nonuse_proved']
    for kind,limit in [('cast',6),('target',7)]:
        for i in range(limit):assert api.describe_rule(kind,i)['selector']==i
        assert api.describe_rule(kind,255)['native_dispatch_return']==0
    for kind,value0 in [('bad',1),('cast',-1),('target',256),('cast',True)]:
        try:api.describe_rule(kind,value0)
        except ValueError:pass
        else:raise AssertionError((kind,value0))
    assert (ROOT/'INTEGRATION_RU.md').read_text(encoding='utf8').startswith('Актуальный этап 152:')
    assert 'пакет содержит 131 компонент' in (ROOT/'READINESS_RU.md').read_text(encoding='utf8')
    out=dict(phase=152,date='2026-10-31',**c,package_components=131,previous130_components_preserved_byte_exact=True,
        physical_and_effective_views_unchanged=True,goal_achieved=False,significant_unknowns_remain=True,limitations=value['limitations'])
    (ROOT/'phase152-validation.json').write_text(json.dumps(out,ensure_ascii=False,indent=2),encoding='utf8')
    status=dict(phase=152,published_phase=152,date='2026-10-31',package_components=131,readiness_estimate_percent=75,
        previous_goal_turn_classification='No progress toward reverse engineering: user requested only readiness assessment; revalidated active goal and resumed concrete native work.',
        current_goal_turn_classification='Progress: published actualKeeper pre-cast validation afterrawloaders,82native/10Ghidra/32byteextents/246exactedits. C8 scope narrowed without inventedmeaning.',
        pending_native_jobs=[],pending_controls=[],goal_achieved=False,significant_unknowns_remain=True,
        evidence=[link(n) for n in ['keeper-validation-semantics.json','KEEPER_VALIDATION_RU.md','phase152-keeper-validation-emulations.json',
            'phase152-keeper-validation-crosschecks.json','phase152-keeper-validation-primary.json','phase152-keeper-validation-api-validation.json',
            'phase152-validation.json','editor-format-package.json','INTEGRATION_RU.md','READINESS_RU.md']],
        next_action='Do not repeat82Keeper cases/10Ghidra/246edits. Fullvalidators nowexecuted; C8 stillunread. Prioritizefreshunknownfields (CreatureJump/Priority,Objects34C,Traps204,Doors236,Terrain198224,AP0408,remainingCryptgroups). SpecialKeeperroom/carried-owner branches remainopen, butdo not replaceunknownfield priority withmore tests ofalreadyknowncases. 517800 listingstaleonebyte; exact12Ccallbackbytes/PCcoverage andtwoGhidraCPUcontrols valid; no server/config restartrequired.',
        limitations=value['limitations'])
    (ROOT/'phase152-search-status.json').write_text(json.dumps(status,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:v for k,v in out.items() if k!='limitations'}))
if __name__=='__main__':main()
