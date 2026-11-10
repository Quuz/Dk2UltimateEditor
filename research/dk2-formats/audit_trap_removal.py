"""Audit complete Trap remove/free, reject old bad pool, preserve package history."""
from pathlib import Path
import json
from build_trap_removal_semantics import ROOT,load,link,sha,build,native
from build_poison import effective_shot_parameters


def main():
    value=load('trap-removal-semantics.json');assert value==build()
    package=load('editor-format-package.json');prior=load('phase120-prior-components.json')
    assert len(package['components'])==99 and len(prior)==98
    assert set(package['components'])-set(prior)=={'trap_removal_semantics'}
    assert package['trap_removal_semantics']==value
    assert package['effective_shot_parameters']==effective_shot_parameters()
    assert package['outstanding_controls_from_phases114_115']==0
    for key,row in package['components'].items():
        raw=(ROOT/row['file']).read_bytes();assert sha(raw)==row['sha256'] and json.loads(raw)==package[key]
        if key in prior:assert row==prior[key]
    bad=load(value['corrected_fixture']['rejected_discovery']['file'])['result']
    try:native.verify_low_page_reads(bad)
    except AssertionError:pass
    else:raise AssertionError('OldNULL+4poolfixture accepted')
    assert all(row['outcome']['registered'] is False and row['outcome']['pool_free_bytes']==512
        and row['outcome']['pool_used_bytes']==0 and row['outcome']['unknown_reads']==0 for row in value['native_replays'])
    assert sum(c.get('active',True) for c in native.cases())==11
    assert sum(c.get('bad_cell',False) for c in native.cases())==1
    sources={row['file']:row['sha256'] for row in value['source_executables_unchanged']+package['editor_variant_semantics']['profiles']}
    for path,digest in sources.items():assert sha(Path(path).read_bytes())==digest
    for row in value['source_corpus']['files']:assert sha(Path(row['file']).read_bytes())==row['sha256']
    assert 'Актуальный этап 120:' in (ROOT/'INTEGRATION_RU.md').read_text(encoding='utf8')
    assert 'пакет содержит 99 компонентов' in (ROOT/'READINESS_RU.md').read_text(encoding='utf8')
    output=dict(phase=120,date='2026-10-29',native_full_cycles=17,original_pool_initializers=17,
        creations=17,removals=17,activations=11,full_ticks=1,paired_full_context_cases=8,
        independent_Ghidra_CPU_controls=3,Ghidra_compared_regions=87,Ghidra_compared_bytes=232461,
        key_function_ranges=27,newly_queried_function_ranges=14,data_tables=4,
        original_pool_coalesces_back_to512=True,entity_tags_owner_cell_and_kind_lists_cleared=True,
        rejected_bad_NULLplus4_fixture=True,ordinary_upkeep_refund_confirmed=True,
        World723B_numeric_gate_global_meaning_open=True,unknown204_gameplay_still_open=True,
        source_Trap_files_preserved=82,original_executable_paths_preserved=len(sources),
        new_container_rebuilds=0,previous98_components_preserved_byte_exact=True,package_components=99,
        earlier8_pending_Ghidra_controls_remain_resolved=True,new_unknown_gameplay_names=0,
        function_or_vtable_gameplay_substitutions=0,goal_achieved=False,significant_unknowns_remain=True,
        limitations=value['limitations'])
    (ROOT/'phase120-validation.json').write_text(json.dumps(output,ensure_ascii=False,indent=2),encoding='utf8')
    status=dict(phase=120,published_phase=120,date='2026-10-29',
        status='Published nativeTrap remove/unregister/free withrealpoolinit andscopedownerrefund;unknown204stillopen.',
        previous_goal_turn_classification='progress:23rawTrapcreation/activation/tick chains and98componentpackage.',
        current_goal_turn_classification='progress:17wholeoriginalremovalcycles,realpoolcoalescing,oldNULL+4fixtureexcluded,3Ghidrachecks and99componentpackage.',
        pending_native_jobs=[],pending_controls=[],package_components=99,readiness_estimate_percent=75,
        evidence=[link(n) for n in ['trap-removal-semantics.json','TRAP_REMOVAL_RU.md','phase120-validation.json',
            'phase120-primary-manifest.json','phase120-trap-removal-emulations.json','phase120-trap-removal-crosschecks.json',
            'INTEGRATION_RU.md','READINESS_RU.md','editor-format-package.json']],
        next_action='Do notrepeat17acceptedremovalcycles/8pairs/3Ghidrachecks oroldcreation/scans. Unknown204unusedinselectedlifecycle,notglobalunused. Newconcreteroute:typedCWorld723Bwriters andtheircaller contexts maylocateaggregate-rebuild/load-registrycode forunknownstructurefields;do notassignloadingmeaningwithoutactualwriterproof. Seekfreshunresolved15familyconsumer ratherthanmoreTrapparameterpermutations. FullWorldscheduler/naturaltargetfire andotherversions remainoutside. GhidraPID19308wasliveatdispatch;nooutstandingreviewblocker.',
        goal_achieved=False,significant_unknowns_remain=True,limitations=value['limitations'])
    (ROOT/'phase120-search-status.json').write_text(json.dumps(status,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:v for k,v in output.items() if k!='limitations'}))


if __name__=='__main__':main()
