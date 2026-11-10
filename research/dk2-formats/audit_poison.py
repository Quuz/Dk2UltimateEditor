"""Audit correction, native state and current effective package semantics."""
import json
from build_poison import ROOT,load,build,digest,link,effective_shot_parameters
import emulate_poison as native
import crosscheck_poison as cross


def main():
    s=load('poison-process-semantics.json');assert s==build()
    package,prior=load('editor-format-package.json'),load('phase115-prior-components.json')
    assert len(package['components'])==94 and len(prior)==93
    assert set(package['components'])-set(prior)=={'poison_process_semantics'}
    for key,row in package['components'].items():
        b=(ROOT/row['file']).read_bytes();assert digest(b)==row['sha256'] and json.loads(b)==package[key]
        if key in prior:assert row==prior[key]
    assert package['poison_process_semantics']==s
    assert package['effective_shot_parameters']==effective_shot_parameters()
    old,new=package['shot_parameters'],package['effective_shot_parameters']
    assert old['processes'][38]['data1']=='Poison severity (low u16)'
    assert new['processes'][38]==s['corrected_process38']
    for i,row in enumerate(old['processes']):
        if i!=38:assert new['processes'][i]==row
    assert package['semantic_corrections']==[dict(component='shot_parameters',json_pointer='/processes/38',
        replacement_component='poison_process_semantics',replacement_key='corrected_process38',
        effective_view='effective_shot_parameters',historical_value_retained_for_audit=True)]
    c=native.cases()[0];a,checks=native.probe.build(**native.inputs(c));r=load('ghidra/'+native.path_for(a).name)['result']
    inverted=dict(c,duration=5,severity=50)
    try:native.verify(inverted,a,checks,r)
    except AssertionError:pass
    else:raise AssertionError('Inverted historical timer/severity expectation accepted')
    pending=load('phase115-pending-crosschecks.json');assert [x['case'] for x in pending['controls']]==cross.CONTROLS
    for row in pending['controls']:
        assert row['prepared'] and not row['executed']
        assert row['input_sha256']==digest(json.dumps(cross.build(row['case']),sort_keys=True).encode())
    assert 'Актуальный этап 115:' in (ROOT/'INTEGRATION_RU.md').read_text(encoding='utf8')
    assert 'пакет содержит 94 компонента' in (ROOT/'READINESS_RU.md').read_text(encoding='utf8')
    assert '| 38 | POISON_SPIT | Poison duration,' in (ROOT/'SHOT_PARAMETERS.md').read_text(encoding='utf8')
    out=dict(phase=115,date='2026-10-29',native_scenarios=16,full_Creature_updates=20,poison_applications=17,
        HP_handler_calls=16,HP_stores=15,complete_context_pairs=4,
        corrected_historical_parameter_order=True,rejected_inverted_expectation=True,
        effective_process38_verified=True,unchanged_other_effective_process_rows=len(old['processes'])-1,
        original_PE_ranges=11,inherited_Ghidra_byte_ranges=12,independent_Ghidra_CPU_controls=0,
        pending_phase115_Ghidra_controls=3,pending_phase114_Ghidra_controls=5,
        exact_container_rebuilds=820,unique_source_files_unchanged=83,identity_records=9086,
        package_components=94,prior93_components_preserved_byte_exact=True,source_executables_unchanged=4,
        gameplay_function_replacements=0,live_game_runs=0,all_damage_paths_verified=False,
        goal_achieved=False,significant_unknowns_remain=True,limitations=s['limitations'])
    (ROOT/'phase115-validation.json').write_text(json.dumps(out,ensure_ascii=False,indent=2),encoding='utf8')
    status=dict(phase=115,published_phase=115,date='2026-10-29',
        status='Published PoisonSpit parameter-order correction and full original rawCreature/rawShot/rawCreatureSpell->caster->collision->poison->tick evidence.',
        previous_goal_turn_classification='progress: phase114 published18native direct/twoAoE chains,categoryconsumer and93component package; Ghidra controls pending review.',
        current_goal_turn_classification='progress: corrected an actual earlier semantic error,executed16 chains/20fullticks,820rebuilds and94component package with effective corrected view.',
        pending_native_jobs=[],pending_controls=[link('phase114-pending-crosschecks.json'),link('phase115-pending-crosschecks.json')],
        package_components=94,readiness_estimate_percent=75,
        evidence=[link(n) for n in ['poison-process-semantics.json','POISON_RU.md','SHOT_PARAMETERS.md',
            'phase115-primary-manifest.json','phase115-poison-emulations.json','phase115-poison-api-validation.json',
            'phase115-validation.json','INTEGRATION_RU.md','READINESS_RU.md','editor-format-package.json']],
        next_action='Once automatic approval review availability is restored,run prepared crosscheck_shot_damage.py andcrosscheck_poison.py without restarting live Ghidra. '
                    'Do not repeat accepted16 poison scenarios without new evidence. New unknown-field consumers,remaining Cryptwork outputs orspecial damage paths remain worthwhile; '
                    'do not generalize noresistance/Invulnerable445 behavior to everyspell orversion. HistoricalProcess38row iswrong; editorconsumers shoulduse effective_shot_parameters.',
        review_scope='No new Ghidra requests thisphase afterprior approvalreview usage-limit failure; originalPE/cachedGhidra evidence used; no bypass.',
        goal_achieved=False,significant_unknowns_remain=True,limitations=s['limitations'])
    (ROOT/'phase115-search-status.json').write_text(json.dumps(status,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:v for k,v in out.items() if k!='limitations'}))


if __name__=='__main__':main()
