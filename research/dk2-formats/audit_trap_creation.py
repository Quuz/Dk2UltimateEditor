"""Audit140 components and bounded rawTrap factory/activation semantics, all prior sources preserved."""
import json
from build_trap_creation import ROOT,load,sha,link,build,effective_layout

def main():
    s=load('trap-creation-activation-semantics.json');assert s==build()
    p=load('editor-format-package.json');old=load('phase161-prior-components.json')
    assert len(p['components'])==140 and len(old)==139 and set(p['components'])-set(old)=={'trap_creation_activation_semantics'}
    for key,row in p['components'].items():
        b=(ROOT/row['file']).read_bytes();assert sha(b)==row['sha256'] and json.loads(b)==p[key]
        if key in old:assert row==old[key]
    assert p['effective_catalogue_layouts']['Traps']==s['effective_trap_layout']==effective_layout()
    stripped=json.loads(json.dumps(s['effective_trap_layout']))
    for f in stripped['fields']:f.pop('verified_native_creation_activation',None)
    assert stripped==load('phase161-prior-effective-traps.json')
    for family,key,subkey in [('Rooms','room_writer_semantics','effective_room_layout'),('Doors','native_door_reader_semantics','effective_door_layout'),
        ('Effects','effect_adjacent_semantics','effective_effect_layout'),('Terrain','terrain_light_submission_semantics','effective_terrain_layout')]:
        assert p['effective_catalogue_layouts'][family]==p[key][subkey]
    for a,b in [('effective_shot_parameters',p['tremor_damage_semantics']['effective_shot_parameters']),
        ('effective_room_resource_consumption',p['room_resource_loader_semantics']['resource_consumption']),
        ('effective_creature_flag_enum',p['melee_animation_flag_semantics']['effective_creature_flag_enum'])]:assert p[a]==b
    expected=dict(native_scenarios=14,Trap_raw_loads=14,actual_Trap_factory_calls=14,actual_Trap_constructors=14,registered_Traps=14,
        complete_activation_calls=12,actual_Player_upkeep_add_calls=12,independent_full_Ghidra_controls=8,Ghidra_compared_regions=328,Ghidra_compared_bytes=631128,
        whole_memory_pairs=3,pair_compared_bytes=236661,original_code_extents=46,static_only_function_extents=2,unique_executed_original_PCs=1797,primary_data_blocks=5,
        exact_changed_container_rebuilds=246,source_Trap_records=1293,source_Trap_files=82,search_roots=438,search_states=8641,search_callee_contexts=29,
        search_limited_roots=1,search_limited_callees=3,byte_attested_direct_ABI_records=548,newly_named_opaque_fields=0,
        preserved_source_files=420,preserved_executable_paths=7,source_writes=0)
    assert s['counts']==expected
    rows={r['case']['name']:r['outcome'] for r in s['native_executions']['replays']}
    for name in ['gas-factory','trigger-factory','sentry-factory']:assert (rows[name]['Player_upkeep'],rows[name]['Player_income'])==(100,1000)
    for name in ['gas-active','gas-unknown0','gas-unknownFFFF','trigger-active','trigger-unknown','sentry-active']:assert (rows[name]['Player_upkeep'],rows[name]['Player_income'])==(105,995)
    assert (rows['gas-usage-minus1']['Player_upkeep'],rows['gas-usage-minus1']['Player_income'])==(99,1001)
    assert (rows['gas-usage-min']['Player_upkeep'],rows['gas-usage-min']['Player_income'])==(4294934628,33768)
    assert (rows['gas-twice']['Player_upkeep'],rows['gas-twice']['Player_income'])==(110,990)
    assert all(r['unknown_definition_reads']==0 and r['registered_tag']==100 for r in rows.values())
    assert not s['opaque_review']['global_nonuse_proved'] and s['opaque_review']['status']=='unresolved'
    assert not s['pointer_search']['global_nonuse_proved']
    assert (ROOT/'INTEGRATION_RU.md').read_text(encoding='utf8').startswith('Актуальный этап 161:')
    assert 'пакет содержит 140 компонентов' in (ROOT/'READINESS_RU.md').read_text(encoding='utf8')
    assert 'Unknown204' in (ROOT/'TRAP_CREATION_ACTIVATION_RU.md').read_text(encoding='utf8')
    out=dict(phase=161,date='2026-11-01',**expected,package_components=140,previous139_components_preserved_byte_exact=True,
        physical_widths_and_codec_unchanged=True,previous_Traps_annotations_preserved=True,goal_achieved=False,significant_unknowns_remain=True,limitations=s['limitations'])
    (ROOT/'phase161-validation.json').write_text(json.dumps(out,ensure_ascii=False,indent=2),encoding='utf8')
    status=dict(phase=161,published_phase=161,date='2026-11-01',package_components=140,readiness_estimate_percent=75,
        previous_goal_turn_classification='Progress:phase160publishedwholeDoorreader/Worldcatalogue/lookup,21scenarios/11Ghidra/164append-reordercontrols;139components.',
        current_goal_turn_classification='Progress:14completeRawTrap/factoryregistrationpaths/12activationcalls,8wholeGhidra/246exactedits;ManaUsageconsumercomposedwithactualcreation,3opaquecontrolpairs. New438root/548RETcleanupsearch,1root/3calleelimits,nonewopaquegameplaymeaning.140components.',
        pending_native_jobs=[],pending_controls=[],goal_achieved=False,significant_unknowns_remain=True,
        evidence=[link(n) for n in ['trap-creation-activation-semantics.json','TRAP_CREATION_ACTIVATION_RU.md','phase161-trap-creation-emulations.json',
            'phase161-trap-creation-crosschecks.json','phase161-trap-creation-primary.json','phase161-trap-activation-storage-validation.json',
            'phase161-catalogue-bounded-argument-leads.json','phase161-catalogue-call-ABI.json','phase161-validation.json','editor-format-package.json','INTEGRATION_RU.md','READINESS_RU.md']],
        next_action='Do notrepeat14RawTrap/factory/activationpaths oroldManaUsagefunctiontests. Unknown204stillunknown; originalGastypesFFF6isnotmeaning. Newconservativepointersearch548RETcleanupsavailable,4C5FD0falsearg32removed;oneObject4F4CFBroot/threecalleelimitsremain. Prioritizefreshconsumer forCreatureJump/Priority/Terrain198-224/remainingCrypt/global fields,or actual ownership/removal path ifitreadsunknown. No newwholeWorldscheduler/Triggernetwork/shot/removalproof. Preserve140schemas andsourcefiles; full15familygoalactive.',limitations=s['limitations'])
    (ROOT/'phase161-search-status.json').write_text(json.dumps(status,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:v for k,v in out.items() if k!='limitations'}))
if __name__=='__main__':main()
