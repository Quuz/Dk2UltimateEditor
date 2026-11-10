"""Audit137 components and scoped mode2 semantics, sources and effective annotations."""
import json
from build_effect_adjacent import ROOT,load,sha,link,build,effective_layout

def main():
    s=load('effect-adjacent-semantics.json');assert s==build()
    p=load('editor-format-package.json');old=load('phase158-prior-components.json')
    assert len(p['components'])==137 and len(old)==136 and set(p['components'])-set(old)=={'effect_adjacent_semantics'}
    for key,row in p['components'].items():
        b=(ROOT/row['file']).read_bytes();assert sha(b)==row['sha256'] and json.loads(b)==p[key]
        if key in old:assert row==old[key]
    for a,b in [('effective_shot_parameters',p['tremor_damage_semantics']['effective_shot_parameters']),
        ('effective_room_resource_consumption',p['room_resource_loader_semantics']['resource_consumption']),
        ('effective_creature_flag_enum',p['melee_animation_flag_semantics']['effective_creature_flag_enum'])]:assert p[a]==b
    for family,key,subkey in [('Rooms','room_writer_semantics','effective_room_layout'),('Doors','door_manufacture_split_semantics','effective_door_layout'),('Traps','trap_model_word_semantics','effective_trap_layout')]:assert p['effective_catalogue_layouts'][family]==p[key][subkey]
    assert p['effective_catalogue_layouts']['Effects']==s['effective_effect_layout']==effective_layout()
    stripped=json.loads(json.dumps(s['effective_effect_layout']))
    for f in stripped['fields']:
        for key in list(f):
            if key.startswith('verified_mode2'):f.pop(key)
    assert stripped==load('phase158-prior-effective-effects.json')
    expected=dict(native_scenarios=16,parent_and_child_Effect_raw_loads=112,Element_raw_loads=2,complete_mode2_calls=16,
        bridge_Effect_wrapper_requests=72,actual_Effect_factory_calls=68,actual_Element_factory_calls=8,registered_Effects=123,registered_Elements=8,
        independent_full_Ghidra_controls=7,Ghidra_compared_regions=301,Ghidra_compared_bytes=1235346,whole_memory_pairs=3,pair_compared_bytes=529434,
        original_code_extents=51,unique_executed_original_PCs=2807,primary_data_blocks=4,exact_container_edits=328,source_Effect_records=30207,
        source_mode2_records=0,newly_named_opaque_fields=0,preserved_source_files=420,preserved_executable_paths=7,source_writes=0)
    assert s['counts']==expected
    rows={r['case']['name']:r['outcome'] for r in s['native_executions']['replays']}
    assert rows['one']['registered_children']==rows['eighth-slot']['registered_children']==8
    assert rows['sparse-slots']['registered_children']==16
    assert rows['tile-flags0']['bridge_wrapper_requests']==4 and rows['tile-flags0']['factory_calls']==0 and rows['tile-flags0']['registered_children']==0
    assert rows['partial-pool']['registered_children']==3 and rows['partial-pool']['free_bytes']==0
    assert rows['element-flag0']['registered_children']==rows['element-flag80']['registered_children']==4
    assert rows['both-flags']['child_kind']=='Effects'
    assert sum(r['recovered_argument_flag_reads'] for r in rows.values())==60
    assert all(r['quality_scale_reads']==r['unknown_definition_reads']==0 for r in rows.values())
    assert not s['opaque_review']['global_nonuse_proved'] and not s['search_review']['global_nonuse_proved']
    assert s['search_review']['limited_roots']==7 and s['search_review']['limited_callee_contexts']==1
    assert (ROOT/'INTEGRATION_RU.md').read_text(encoding='utf8').startswith('Актуальный этап 158:')
    assert 'пакет содержит 137 компонентов' in (ROOT/'READINESS_RU.md').read_text(encoding='utf8')
    out=dict(phase=158,date='2026-10-31',**expected,package_components=137,previous136_components_preserved_byte_exact=True,
        physical_widths_and_codec_unchanged=True,previous_Effects_annotations_preserved=True,goal_achieved=False,significant_unknowns_remain=True,limitations=s['limitations'])
    (ROOT/'phase158-validation.json').write_text(json.dumps(out,ensure_ascii=False,indent=2),encoding='utf8')
    status=dict(phase=158,published_phase=158,date='2026-10-31',package_components=137,readiness_estimate_percent=75,
        previous_goal_turn_classification='Progress:phase157publishedloop-awareCryptsearch3171roots/12falsechainsremoved,11native46-entryhelpers/5Ghidra,136components.',
        current_goal_turn_classification='Progress:16completemode2nativegenerationpaths/112Effect+2Elementloads,123Effects+8Elementsregistered,7fullGhidracontrols/328exactedits. Newscopedcount/slots/targets/qualityrules,EE/F0stillunresolved,137components.',
        pending_native_jobs=[],pending_controls=[],goal_achieved=False,significant_unknowns_remain=True,
        evidence=[link(n) for n in ['effect-adjacent-semantics.json','EFFECT_ADJACENT_RU.md','phase158-adjacent-emulations.json','phase158-adjacent-crosschecks.json',
            'phase158-adjacent-primary.json','phase158-adjacent-storage-validation.json','phase158-effect-bounded-argument-leads.json','phase158-effect-call-ABI.json',
            'phase158-validation.json','editor-format-package.json','INTEGRATION_RU.md','READINESS_RU.md']],
        next_action='Do notrepeat16mode2paths/7Ghidra/328edits orold143/141scenes. Mode2normal8slot/cardinal/count/40target/no4000scalerulesestablishedwithpreparedparent;0mode2incorpus. EE/F0notdecoded. Keepdistinctionwrapperrequests72vsactualEffectfactory68,especiallytileflags0. Element107ownerTagand111parentTagseparate. Numericstack/callcleanupsearchavailablewith6scopedindirectABIsites,7root/1calleelimits. PrioritizefreshprimaryconsumerforTerrain198/224 orremainingCreatureJump/Priority/Doors236/Traps204/Objects34C;do notturnnegativeboundedreadsintoglobalnonuse. 15families/fullgoalactive,sources/paralleleditorunchanged.',
        limitations=s['limitations'])
    (ROOT/'phase158-search-status.json').write_text(json.dumps(status,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps({k:v for k,v in out.items() if k!='limitations'}))
if __name__=='__main__':main()
