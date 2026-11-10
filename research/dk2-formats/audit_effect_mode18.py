"""Audit122-component publication and selected native mode18 factory coverage."""
import json
from build_effect_mode18 import ROOT,load,sha,link,build,effective_layout

def main():
    value=load('effect-mode18-semantics.json');assert value==build()
    package=load('editor-format-package.json');prior=load('phase143-prior-components.json')
    assert len(package['components'])==122 and len(prior)==121
    assert set(package['components'])-set(prior)=={'effect_mode18_semantics'}
    for key,row in package['components'].items():
        data=(ROOT/row['file']).read_bytes();assert sha(data)==row['sha256'] and json.loads(data)==package[key]
        if key in prior:assert row==prior[key]
    assert package['effect_mode18_semantics']==value
    current=package['effective_catalogue_layouts']['Effects'];old=package['effect_generation_scale_semantics']['effective_effect_layout']
    assert current==effective_layout() and current['record_size']==old['record_size']==246
    assert len(current['fields'])==len(old['fields'])
    for before,after in zip(old['fields'],current['fields']):
        for k,v in before.items():assert after[k]==v
        assert (before==after)==(before['offset'] not in [0xF2,0xF3]+list(range(0xCA,0xDA,2)))
    assert package['effective_catalogue_layouts']['Doors']==package['door_manufacture_split_semantics']['effective_door_layout']
    assert package['effective_catalogue_layouts']['EffectElements']==package['element_palette_chain_semantics']['effective_element_layout']
    c=value['counts'];assert c['native_raw_chains']==12 and c['raw_effect_definition_loads']==84 and c['successful_child_Effects']==52 and c['factory_requests']==38
    assert c['paired_complete_memory_controls']==2 and c['paired_compared_bytes']==352956
    assert c['independent_Ghidra_CPU_controls']==5 and c['Ghidra_compared_regions']==215 and c['Ghidra_compared_bytes']==882390
    assert c['executed_function_spans']==26 and c['unique_executed_original_PCs']==1344 and c['original_vtable_blocks']==3
    assert c['rejected_static_Misc_leads']==6 and c['new_exact_container_edits']==82 and c['preserved_source_files']==200 and c['preserved_executable_paths']==7
    assert c['newly_named_opaque_fields']==c['source_writes']==0
    cases={r['case']['name']:r['outcome'] for r in value['native_executions']['replays']}
    assert cases['source-chains']['creation_order']==[4,3,2,221,1,143]
    assert cases['count8']['creation_order']==[4,3,2,221,1,143]*2 and cases['count3']['creation_order']==[]
    assert cases['partial-chain']['creation_order']==[4,3,2]
    assert cases['partial-chain']['first_output_per_request'][2]==dict(effect_id=2,returned=1,first=2,chain=[2])
    assert all(not r['outcome']['unknown_definition_reads'] for r in value['native_executions']['replays'])
    assert value['API_validation']['source_mode18_records']==492 and value['unknown_result']['meaning']=='Unresolved'
    assert 'Актуальный этап 143:' in (ROOT/'INTEGRATION_RU.md').read_text(encoding='utf8')
    assert 'пакет содержит 122 компонента' in (ROOT/'READINESS_RU.md').read_text(encoding='utf8')
    out=dict(phase=143,date='2026-10-31',**c,previous121_components_preserved_byte_exact=True,package_components=122,
        full_selected_Effect_child_factories_executed=True,parent_factory_executed=False,actual_assets_GPU_or_World_scheduler=False,
        Misc2_Misc3_meaning='Unresolved',global_unused_claim=False,initial_incomplete_closure_probe_excluded=True,
        goal_achieved=False,significant_unknowns_remain=True,limitations=value['limitations'])
    (ROOT/'phase143-validation.json').write_text(json.dumps(out,ensure_ascii=False,indent=2),encoding='utf8')
    status=dict(phase=143,published_phase=143,date='2026-10-31',package_components=122,readiness_estimate_percent=75,
        status='Published fullselectedmode18/Effectfactory/NextEffectIdchain andpartialallocation proof; sixinfeasibleMisc aliasleads rejected.',
        previous_goal_turn_classification='Progress: phase142 fixed researchDoorWORD API/layout,5 nativechains/3 Ghidra/82 container/decode checks and121components.',
        current_goal_turn_classification='Progress: new producer-rootedMisc scan/review,12 selectednativepaths/84rawloads/52childEffects/38factoryrequests,5 Ghidra/2wholepairs/82edits,122components. No newopaque meaningassigned.',
        pending_native_jobs=[],pending_controls=[],goal_achieved=False,significant_unknowns_remain=True,
        evidence=[link(n) for n in ['effect-mode18-semantics.json','EFFECT_MODE18_RU.md','phase143-effect-mode18-primary.json','phase143-effect-mode18-emulations.json',
            'phase143-effect-mode18-crosschecks.json','phase143-effect-mode18-api-validation.json','phase143-effect-misc-argument-review.json',
            'phase143-validation.json','INTEGRATION_RU.md','READINESS_RU.md','editor-format-package.json']],
        next_action='Do not repeat12mode18paths/5Ghidra/82edits orsixfalseMisc loopiterations. Misc2/Misc3 remainunknown; seek freshconsumer outsideboundedloops or anotherremainingfamily. Fullparentfactory/laterchildtick/destruction/normalWorldscheduler remainopen. OtherunknownDoors236,Traps204,Terrain198224,CreatureJump/Priority,KeeperC8 stillopen. No source/parallel editor writes.',
        limitations=value['limitations'])
    (ROOT/'phase143-search-status.json').write_text(json.dumps(status,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:v for k,v in out.items() if k!='limitations'}))

if __name__=='__main__':main()
