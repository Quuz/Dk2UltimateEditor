"""Verify136 components and retain unresolved Crypt groups without globalabsenceclaims."""
import json
from build_bounded_crypt_search import ROOT,load,sha,link,build

def main():
    s=load('bounded-crypt-search-semantics.json');assert s==build()
    p=load('editor-format-package.json');old=load('phase157-prior-components.json')
    assert len(p['components'])==136 and len(old)==135 and set(p['components'])-set(old)=={'bounded_crypt_search_semantics'}
    for key,row in p['components'].items():
        b=(ROOT/row['file']).read_bytes();assert sha(b)==row['sha256'] and json.loads(b)==p[key]
        if key in old:assert row==old[key]
    for a,b in [('effective_shot_parameters',p['tremor_damage_semantics']['effective_shot_parameters']),
        ('effective_room_resource_consumption',p['room_resource_loader_semantics']['resource_consumption']),
        ('effective_creature_flag_enum',p['melee_animation_flag_semantics']['effective_creature_flag_enum'])]:assert p[a]==b
    for family,key,subkey in [('Rooms','room_writer_semantics','effective_room_layout'),('Doors','door_manufacture_split_semantics','effective_door_layout'),('Traps','trap_model_word_semantics','effective_trap_layout')]:assert p['effective_catalogue_layouts'][family]==p[key][subkey]
    expected=dict(native_scenarios=11,independent_full_Ghidra_controls=5,Ghidra_compared_regions=15,Ghidra_compared_bytes=22130,
        actual_record_comparisons=506,original_code_extents=3,static_only_extents=2,unique_executed_original_PCs=11,
        producer_roots=3171,root_states=195046,callee_contexts=690,limited_roots=43,limited_callee_contexts=49,
        removed_false_candidate_chains=12,retained_candidate_chains=33,newly_named_opaque_fields=0,crypt_groups_assigned_total=8,
        crypt_groups_remaining=10,new_raw_edits=0,preserved_source_files=420,preserved_executable_paths=7,source_writes=0)
    assert s['counts']==expected
    assert s['search']['root_types']=={'entity-field-hint':421,'exact-lookup-return':118,'untyped-global-tag-table':2612,'untyped-player-stride-hint':20}
    assert s['search']['old_root_hints_retained']==539 and not s['search']['global_nonuse_proved'] and s['search']['new_candidate_chains']==0
    assert s['bounded_table']['entry_count']==46 and s['bounded_table']['last_comparison_byte_offset']==0xE81
    assert len(s['search']['remaining_unknown_offsets'])==10
    assert (ROOT/'INTEGRATION_RU.md').read_text(encoding='utf8').startswith('Актуальный этап 157:')
    assert 'пакет содержит 136 компонентов' in (ROOT/'READINESS_RU.md').read_text(encoding='utf8')
    out=dict(phase=157,date='2026-10-31',**expected,package_components=136,previous135_components_preserved_byte_exact=True,
        physical_and_effective_views_unchanged=True,goal_achieved=False,significant_unknowns_remain=True,limitations=s['limitations'])
    (ROOT/'phase157-validation.json').write_text(json.dumps(out,ensure_ascii=False,indent=2),encoding='utf8')
    status=dict(phase=157,published_phase=157,date='2026-10-31',package_components=136,readiness_estimate_percent=75,
        previous_goal_turn_classification='Progress:phase156published99completeAPticks/12Ghidracontrols/852exactstorageedits,newAPlink/repeatrules,135components.',
        current_goal_turn_classification='Progress:loop-awaresearch3171roots/690calleecontexts,12falsechainsautomaticallyremoved;11complete46-entrynativehelpers/5wholeGhidracontrols,136components. Zero newopaquepropertyassignments,readinessunchanged.',
        pending_native_jobs=[],pending_controls=[],goal_achieved=False,significant_unknowns_remain=True,
        evidence=[link(n) for n in ['bounded-crypt-search-semantics.json','BOUNDED_CRYPT_SEARCH_RU.md','phase157-bounded-owner-crypt-leads.json',
            'phase157-owner-table-emulations.json','phase157-owner-table-crosschecks.json','phase157-bounded-crypt-primary.json',
            'phase157-validation.json','editor-format-package.json','INTEGRATION_RU.md','READINESS_RU.md']],
        next_action='Do notrerun3171rootsearch/11ownerhelpercases/5GhidraorhistoricalE8E9shots. Twelvefalse4C7368chainsnowpruned;new4DC7E0tabledefinitivelyendsE81. Cryptunknownreadsremainin4D8E60setteronly; avoidclaimninthbonus/globalnonuse. Numericdomainavailablebutremaining43root/49calleelimitsandoldstack-CALLmodelrestrictionsmustremainexplicit. Prioritizepositiveprimaryconsumer evidenceforfreshrawunknowns Terrain198/224,EffectsEE/F0,ElementB1/B2,Objects34C,Traps204,Doors236,CreatureJump/Priority. Existingfullroutinecomparisonsdonotjustifyanotherlargecaseexpansionunlessnewtypedbindingorbranch. Sourcesandparalleleditorunchanged,full15familygoalactive.',
        limitations=s['limitations'])
    (ROOT/'phase157-search-status.json').write_text(json.dumps(status,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps({k:v for k,v in out.items() if k!='limitations'}))
if __name__=='__main__':main()
