"""Audit133 components, actual primary model callbacks and source preservation."""
import json
from build_primary_pick import ROOT,load,sha,link,build
def main():
    s=load('primary-pick-semantics.json');assert s==build()
    p=load('editor-format-package.json');old=load('phase154-prior-components.json')
    assert len(p['components'])==133 and len(old)==132 and set(p['components'])-set(old)=={'primary_pick_semantics'}
    for key,row in p['components'].items():
        b=(ROOT/row['file']).read_bytes();assert sha(b)==row['sha256'] and json.loads(b)==p[key]
        if key in old:assert row==old[key]
    assert p['primary_pick_semantics']==s
    assert p['effective_shot_parameters']==p['tremor_damage_semantics']['effective_shot_parameters']
    assert p['effective_room_resource_consumption']==p['room_resource_loader_semantics']['resource_consumption']
    assert p['effective_creature_flag_enum']==p['melee_animation_flag_semantics']['effective_creature_flag_enum']
    assert p['effective_catalogue_layouts']['Rooms']==p['room_writer_semantics']['effective_room_layout']
    assert p['effective_catalogue_layouts']['Doors']==p['door_manufacture_split_semantics']['effective_door_layout']
    assert p['effective_catalogue_layouts']['Traps']==p['trap_model_word_semantics']['effective_trap_layout']
    expected=dict(native_scenarios=27,complete_primary_pick_calls=27,actual_Creature_definition_loads=54,actual_Object_definition_loads=27,
        native_model_callbacks=60,dynamic_callbacks=48,animated_callbacks=12,native_animated_vertex_calls=30,
        independent_Ghidra_CPU_controls=8,Ghidra_compared_regions=457,Ghidra_compared_bytes=702282,original_code_extents=76,static_only_extents=3,
        unique_executed_original_PCs=4433,primary_data_blocks=3,new_exact_container_edits=0,inherited_exact_priority_edits=328,
        preserved_source_files=310,preserved_executable_paths=7,newly_named_opaque_fields=0,source_writes=0)
    assert s['counts']==expected
    rows={r['case']['name']:r['outcome'] for r in s['native_executions']['replays']}
    for n in ['dynamic-nearer','dynamic-reverse','animated-nearer','mixed-nearer','mean-depth','animated-mean-depth','priority-255-0','object-priority-255']:
        assert rows[n]['selected_tag']==10 and rows[n]['exact_triangle_flag']==1,n
    assert rows['far-exact-beats-near-coarse']['selected_tag']==9 and rows['far-exact-beats-near-coarse']['model_callback_order']==[0,1]
    assert rows['triangle-miss-coarse-fallback']['model_callback_order']==[0,1,0,1]
    assert rows['triangle-miss-coarse-fallback']['selected_tag']==10 and rows['triangle-miss-coarse-fallback']['exact_triangle_flag']==0
    for n in ['dynamic-tie','animated-tie','near-depth-clamp']:assert rows[n]['selected_tag']==9,n
    for n in ['registry-outside','registry-tangent','empty-registry']:assert rows[n]['selected_tag'] is None and rows[n]['native_model_callback_count']==0,n
    assert all(r['priority_or_Jump_reads']==0 and r['low_reads_only_FS_SEH'] for r in rows.values())
    assert not s['remaining_priority']['global_nonuse_proved'] and s['remaining_priority']['gameplay_purpose']=='unresolved'
    oldapi=s['serialization']['inherited_priority_API_validation'];assert sha((ROOT/oldapi['file']).read_bytes())==oldapi['sha256']
    assert (ROOT/'INTEGRATION_RU.md').read_text(encoding='utf8').startswith('Актуальный этап 154:')
    assert 'пакет содержит 133 компонента' in (ROOT/'READINESS_RU.md').read_text(encoding='utf8')
    out=dict(phase=154,date='2026-10-31',**expected,package_components=133,previous132_components_preserved_byte_exact=True,
        physical_and_effective_views_unchanged=True,goal_achieved=False,significant_unknowns_remain=True,limitations=s['limitations'])
    (ROOT/'phase154-validation.json').write_text(json.dumps(out,ensure_ascii=False,indent=2),encoding='utf8')
    status=dict(phase=154,published_phase=154,date='2026-10-31',package_components=133,readiness_estimate_percent=75,
        previous_goal_turn_classification='Progress:phase153publishedactual44DEC0fallbackafterrawloaders,34native/8Ghidra/328edits,132components.',
        current_goal_turn_classification='Progress:completeprimary572820registry/actualdynamic+animatedcallbacks,27native/60callbacks/8Ghidra/76byteextents. Actor-to-primitiveproducer remainsunexecuted; no inventedPriority/Jumpmeaning.',
        pending_native_jobs=[],pending_controls=[],goal_achieved=False,significant_unknowns_remain=True,
        evidence=[link(n) for n in ['primary-pick-semantics.json','PRIMARY_PICK_RU.md','phase154-primary-pick-emulations.json','phase154-primary-pick-crosschecks.json',
            'phase154-primary-pick-primary.json','phase154-validation.json','editor-format-package.json','INTEGRATION_RU.md','READINESS_RU.md']],
        next_action='Do not repeat27primarypickcases/8Ghidra orphase153fallback/328edits. FreshremainingbindingisActor/RenderInfo->actualprimitivecreation andregistryproducer:59D010/59CCF0/59C880 leadsrequireprimarytypeproof; selectedready-madeprimitivesdonotproveglobalPriorityunused. AlternativelyseekfreshunknownTerrain198224,Objects34C,Traps204,Doors236,AP0408,Crypt10groups orCreatureJump consumer. Ghidra8089recoveredonlyafteractualnoJava/no-listenercheck; currentprojectDX. Checkliveprocessifneeded,do not restartbasedonlyontimeouts. No originalsource/parallel editor edits.',
        limitations=s['limitations'])
    (ROOT/'phase154-search-status.json').write_text(json.dumps(status,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps({k:v for k,v in out.items() if k!='limitations'}))
if __name__=='__main__':main()
