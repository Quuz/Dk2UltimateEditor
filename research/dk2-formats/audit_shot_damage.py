"""Audit phase114 evidence without treating pending Ghidra replays as passed."""
import copy
import json
from build_shot_damage import ROOT,build,load,digest,link
import emulate_shot_damage as native
import crosscheck_shot_damage as cross


def main():
    s=load('shot-damage-semantics.json');assert s==build()
    package,prior=load('editor-format-package.json'),load('phase114-prior-components.json')
    assert len(package['components'])==93 and len(prior)==92
    assert set(package['components'])-set(prior)=={'shot_damage_semantics'}
    assert package['shot_damage_semantics']==s
    for k,row in package['components'].items():
        blob=(ROOT/row['file']).read_bytes();assert digest(blob)==row['sha256'] and json.loads(blob)==package[k]
        if k in prior:assert row==prior[k]
    for row in s['helpers']+s['dependencies']:assert digest((ROOT/row['file']).read_bytes())==row['sha256']
    c=next(c for c in native.probe.cases() if c['name']=='hit-res100');args,checks=native.probe.build(c)
    r=load('ghidra/'+native.path_for(args).name)['result']
    wrong=copy.deepcopy(r);wrong['captured_hex']=wrong['captured_hex'][:128]+'e8030000'+wrong['captured_hex'][136:]
    try:native.verify(c,args,checks,wrong)
    except AssertionError:pass
    else:raise AssertionError('False resistance immunity accepted')
    wrong=copy.deepcopy(r);wrong['observed_reads'].append(dict(pc='0x4ac480',address=hex(native.probe.target.attack.TARGET_DEF+0x61C),size=1,value_hex='12'))
    try:native.verify(c,args,checks,wrong)
    except AssertionError:pass
    else:raise AssertionError('Damage-path table read accepted as non-reading')
    pending=load('phase114-pending-crosschecks.json')
    assert [x['case'] for x in pending['controls']]==cross.CONTROLS
    for row in pending['controls']:
        assert row['prepared'] and not row['executed']
        assert row['input_sha256']==digest(json.dumps(cross.build(row['case']),sort_keys=True).encode())
    hp=sum(r['outcome']['HP_calls'] for r in s['native_replays']);assert hp==13
    assert s['verification']['independent_Ghidra_CPU_controls']==0
    assert 'Актуальный этап 114:' in (ROOT/'INTEGRATION_RU.md').read_text(encoding='utf8')
    assert 'пакет содержит 93 компонента' in (ROOT/'READINESS_RU.md').read_text(encoding='utf8')
    out=dict(phase=114,date='2026-10-29',native_raw_definition_factory_damage_chains=18,
             native_HP_calls=hp,full_context_comparisons=18,measured_baselines=3,
             Ghidra_instruction_range_comparisons=6,inherited_Ghidra_byte_ranges=10,additional_PE_ranges=7,
             independent_Ghidra_CPU_controls=0,pending_Ghidra_CPU_controls=5,
             exact_container_rebuilds=410,identity_records=5551,source_Shot_containers_unchanged=82,
             source_executables_unchanged=4,package_components=93,prior92_components_preserved_byte_exact=True,
             false_immunity_rejected=True,false_table_nonreading_rejected=True,
             gameplay_function_replacements=0,live_game_runs=0,all_damage_paths_verified=False,
             goal_achieved=False,significant_unknowns_remain=True,limitations=s['limitations'])
    (ROOT/'phase114-validation.json').write_text(json.dumps(out,ensure_ascii=False,indent=2),encoding='utf8')
    status=dict(phase=114,published_phase=114,date='2026-10-29',
        status='Published original rawShot category consumer and selected direct/twoAoE non-reading of Creature resistances;18 local CPU chains,0 independent Ghidra CPU controls.',
        previous_goal_turn_classification='progress: new Shot discovery plus original-code queries established raw loader/factory path; no game patch authorized or made.',
        current_goal_turn_classification='progress:18 complete original-code compositions,category Boulder consumer,375byte intermediate proof,410 exact rebuilds and93component package.',
        pending_native_jobs=[],pending_controls=link('phase114-pending-crosschecks.json'),
        approval_review_failure='Independent Ghidra replay command not executed because automatic approval review could not complete due usage limit. No bypass attempted. Other local evidence work completed.',
        package_components=93,readiness_estimate_percent=75,
        evidence=[link(n) for n in ['shot-damage-semantics.json','SHOT_DAMAGE_RU.md','phase114-validation.json',
            'phase114-primary-manifest.json','phase114-shot-damage-emulations.json','phase114-shot-damage-api-validation.json',
            'INTEGRATION_RU.md','READINESS_RU.md','editor-format-package.json']],
        next_action='When review availability is restored,run prepared crosscheck_shot_damage.py before asserting independent CPU controls. '
                    'Avoid repeating18 acceptedcases without new evidence. Follow remaining status/source-context retention or a new unresolved field consumer; '
                    'otherDamageTypes/processes are not certified. Work113Research/Manufacture queries remain WIP. Preserve all15families scope.',
        goal_achieved=False,significant_unknowns_remain=True,limitations=s['limitations'])
    (ROOT/'phase114-search-status.json').write_text(json.dumps(status,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:v for k,v in out.items() if k!='limitations'}))


if __name__=='__main__':main()
