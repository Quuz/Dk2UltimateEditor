"""Audit128 components and exact bounds of the new unknown-field search review."""
import json
from build_unknown_search_review import ROOT,load,sha,link,build

def main():
    value=load('unknown-search-review.json');assert value==build()
    package=load('editor-format-package.json');old=load('phase149-prior-components.json')
    assert len(package['components'])==128 and len(old)==127
    assert set(package['components'])-set(old)=={'unknown_search_review'}
    for key,row in package['components'].items():
        data=(ROOT/row['file']).read_bytes();assert sha(data)==row['sha256'] and json.loads(data)==package[key]
        if key in old:assert row==old[key]
    assert package['unknown_search_review']==value
    assert package['effective_shot_parameters']==package['shot_lifecycle_semantics']['effective_shot_parameters']
    assert package['effective_creature_flag_enum']==package['melee_animation_flag_semantics']['effective_creature_flag_enum']
    assert package['effective_catalogue_layouts']['Doors']==package['door_manufacture_split_semantics']['effective_door_layout']
    assert package['effective_catalogue_layouts']['Traps']==package['trap_model_word_semantics']['effective_trap_layout']
    c=value['counts']
    for key,wanted in dict(native_scenarios=8,independent_Ghidra_CPU_controls=4,original_function_extents=7,executed_function_extents=1,
        static_function_extents=6,unique_executed_original_PCs=12,rejected_false_candidate_chains=12,newly_named_opaque_fields=0,
        crypt_groups_assigned_total=8,crypt_groups_remaining=10,new_raw_patches=0,preserved_source_files=310,preserved_executable_paths=7,source_writes=0).items():assert c[key]==wanted,(key,c[key])
    assert c['Ghidra_register_controls_only'] is True
    assert value['extra_flags']['corpus']['bit2_set_records']==3 and value['extra_flags']['corpus']['records']==3734
    assert {r['raw_id'] for r in value['extra_flags']['corpus']['examples']}=={28}
    assert value['rejected_lead']['actual_loop_bound']==3 and value['rejected_lead']['invented_loop_index']==20
    assert value['extra_flags']['search']['limited_callees']==1 and value['crypt_counter_search']['search']['limited_callees']==6
    rows={r['case']['name']:r['outcome'] for r in value['native_executions']['replays']}
    assert all(rows[f'crypt108b-{n}']['result']==0 for n in [0,1,127,255])
    assert all(rows[f'slot{i}-active']['result']==1 for i in range(3))
    assert all(0x108B not in row['read_offsets'] for row in rows.values())
    assert 'Актуальный этап 149:' in (ROOT/'INTEGRATION_RU.md').read_text(encoding='utf8')
    assert 'пакет содержит 128 компонентов' in (ROOT/'READINESS_RU.md').read_text(encoding='utf8')
    out=dict(phase=149,date='2026-10-31',**c,package_components=128,previous127_components_preserved_byte_exact=True,
        effective_views_and_storage_unchanged=True,global_nonuse_proven=False,goal_achieved=False,significant_unknowns_remain=True,limitations=value['limitations'])
    (ROOT/'phase149-validation.json').write_text(json.dumps(out,ensure_ascii=False,indent=2),encoding='utf8')
    status=dict(phase=149,published_phase=149,date='2026-10-31',package_components=128,readiness_estimate_percent=75,
        status='New extraFlag2/unknownCrypt pointer scans bounded;12falsecounter108Bchains quarantined byactual3-readloop andindependentexecution. No ninthCryptgroup ornewopaquefieldname.',
        previous_goal_turn_classification='Progress:phase148published22completeShotlifecyclecases/6Ghidra/164edits,127components.',
        current_goal_turn_classification='Progress:twofreshpointersearches,sourceflag2distribution andnativeprimaryrejection of12newfalseleads;128componentpackagepublished withunchangedsemanticviews.',
        pending_native_jobs=[],pending_controls=[],goal_achieved=False,significant_unknowns_remain=True,
        evidence=[link(n) for n in ['unknown-search-review.json','UNKNOWN_SEARCH_REVIEW_RU.md','phase149-extra-creature-flags-leads.json','phase149-owner-crypt-pointer-leads.json',
            'phase149-crypt-counter-boundary-emulations.json','phase149-crypt-boundary-crosschecks.json','phase149-unknown-search-primary.json','phase149-validation.json','editor-format-package.json','INTEGRATION_RU.md','READINESS_RU.md']],
        next_action='Do not repeatthetwo149scans/eighthelpercases/fourGhidra. CreatureextraFlag2correlateswithID28 onlyin3/3734sources,notconsumerproof. InvestigateactualID28 gamepathsifnewprimaryleadexists; do not inferflagmeaning fromID. Cryptremaining108A/B,1092/3,1096..9B stillopen; avoidfalse4C7368loopread. Remaininglimitedcalleecontexts andindexedargumentpaths neednumericbounds, not blindlytrustpointerwalker. OtheropaqueJump/Priority/Objects34C/Traps204/Doors236/Terrain198224/AP0408/KeeperC8 remainopen. Originaldata/EXE/parallel editor untouched.',
        limitations=value['limitations'])
    (ROOT/'phase149-search-status.json').write_text(json.dumps(status,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:v for k,v in out.items() if k!='limitations'}))

if __name__=='__main__':main()
