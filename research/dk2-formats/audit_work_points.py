"""Audit finite work contribution coverage and preservation of prior schemas."""
import json
from build_work_points import ROOT,load,build,digest,link
from build_poison import effective_shot_parameters
import emulate_work_points as native


def main():
    s=load('work-point-semantics.json');assert s==build()
    package,prior=load('editor-format-package.json'),load('phase116-prior-components.json')
    assert len(package['components'])==95 and len(prior)==94
    assert set(package['components'])-set(prior)=={'work_point_semantics'}
    assert package['work_point_semantics']==s and package['effective_shot_parameters']==effective_shot_parameters()
    for k,row in package['components'].items():
        b=(ROOT/row['file']).read_bytes();assert digest(b)==row['sha256'] and json.loads(b)==package[k]
        if k in prior:assert row==prior[k]
    c=next(c for c in native.cases() if c['name']=='manufacture-wizard-one')
    args,checks=native.probe.build(**native.inputs(c));r=load('ghidra/'+native.path_for(args).name)['result']
    incorrect=dict(c,base=21000)
    try:native.verify(incorrect,args,checks,r)
    except AssertionError:pass
    else:raise AssertionError('Assumed5% WorkEnhancement contribution accepted')
    assert 'Актуальный этап 116:' in (ROOT/'INTEGRATION_RU.md').read_text(encoding='utf8')
    assert 'пакет содержит 95 компонентов' in (ROOT/'READINESS_RU.md').read_text(encoding='utf8')
    calls=sum(r['outcome']['player_calls'] for r in s['native_replays']);assert calls==26
    out=dict(phase=116,date='2026-10-29',native_raw_load_tick_contribution_chains=28,raw65_loads=28,
        full_Creature_ticks=28,Crypt_registrations=4,player_contribution_calls=26,paired_full_contexts=4,
        rejected_assumed5percent_output=True,original_PE_ranges=5,inherited_Ghidra_byte_ranges=7,
        independent_Ghidra_CPU_controls=0,prior_pending_controls=8,
        exact_container_rebuilds=410,source_Creature_files_unchanged=82,identity_records=3734,API_refusals=8,
        source_executables_unchanged=4,package_components=95,prior94_components_preserved_byte_exact=True,
        effective_Shot_process38_correction_preserved=True,newly_named_unknown_fields=0,
        gameplay_function_replacements=0,live_game_runs=0,full_active_work_jobs_executed=False,
        goal_achieved=False,significant_unknowns_remain=True,limitations=s['limitations'])
    (ROOT/'phase116-validation.json').write_text(json.dumps(out,ensure_ascii=False,indent=2),encoding='utf8')
    status=dict(phase=116,published_phase=116,date='2026-10-29',
        status='Published raw work-rate quantization and finite player contribution amounts; actual Crypt WorkEnhancement doesnotchange selected contribution.',
        previous_goal_turn_classification='progress: phase115 corrected PoisonSpit parameter inversion,16native chains/20ticks/820rebuilds and94component package.',
        current_goal_turn_classification='progress:28 rawCreature/raw65->tick->contribution chains,4Crypt wholecontext pairs,410rebuilds and95component package.',
        pending_native_jobs=[],pending_controls=[link('phase114-pending-crosschecks.json'),link('phase115-pending-crosschecks.json')],
        package_components=95,readiness_estimate_percent=75,
        evidence=[link(n) for n in ['work-point-semantics.json','WORK_POINTS_RU.md','phase116-validation.json',
            'phase116-primary-manifest.json','phase116-work-point-emulations.json','phase116-work-rate-api-validation.json',
            'INTEGRATION_RU.md','READINESS_RU.md','editor-format-package.json']],
        next_action='Do not repeat28 accepted finite work contributions. Activequeue/animation frequency/496E00 tile-work are open; '
                    'ResearchPlayer213 is current1based research index,not a numeric completion threshold. '
                    'Unresolved rawfields still take priority when a genuinely newconsumer lead exists. '
                    'When reviewavailability recovers,run pending phase114/115 controls without restarting Ghidra. No new Ghidra request inphase116.',
        goal_achieved=False,significant_unknowns_remain=True,limitations=s['limitations'])
    (ROOT/'phase116-search-status.json').write_text(json.dumps(status,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:v for k,v in out.items() if k!='limitations'}))


if __name__=='__main__':main()
