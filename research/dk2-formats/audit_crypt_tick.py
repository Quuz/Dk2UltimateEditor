"""Audit scoped full-tick evidence, CPU agreement and complete format package."""
import json
from pathlib import Path
from build_crypt_tick import ROOT, build, load, digest, link


def main():
    schema = load('crypt-tick-semantics.json')
    assert schema == build()
    package = load('editor-format-package.json')
    prior = load('phase107-prior-components.json')
    assert len(package['components']) == 86 and len(prior) == 85
    assert set(package['components'])-set(prior) == {'crypt_tick_semantics'}
    assert package['crypt_tick_semantics'] == schema
    for key, row in package['components'].items():
        data = (ROOT/row['file']).read_bytes()
        assert digest(data) == row['sha256'] and json.loads(data) == package[key]
        if key in prior:
            assert row == prior[key]
    for row in schema['helpers']+schema['dependencies']:
        assert digest((ROOT/row['file']).read_bytes()) == row['sha256']
    # Source data checks are read-only; previous328 rebuilds are not new edits.
    corpus = load('phase106-threat-api-validation.json')
    for row in corpus['files']:
        assert digest(Path(row['file']).read_bytes()) == row['sha256']
    assert corpus['source_files'] == 82
    outcomes = {r['case']: r['expected'] for r in schema['native_replays']}
    for name, peak, final, work in [('base', 1000, 1000, 0), ('fairy1', 1100, 1000, 0),
                                   ('fairy2', 1200, 1000, 0), ('fairy3', 1300, 1000, 0),
                                   ('fairy-cap', 4095, 4000, 0), ('wizard1', 1000, 1000, 205),
                                   ('wizard2', 1000, 1000, 410), ('wizard128', 1000, 1000, 0),
                                   ('both1', 1100, 1000, 205), ('crypt-fairy-insert', 1100, 1000, 0),
                                   ('crypt-fairy-clear', 1000, 1000, 0), ('crypt-fairy-other-owner', 1000, 1000, 0),
                                   ('crypt-wizard-insert', 1000, 1000, 205), ('crypt-wizard-clear', 1000, 1000, 0),
                                   ('crypt-wizard-other-owner', 1000, 1000, 0)]:
        e = outcomes[name]
        assert [e['transient_speed'], e['speed'], e['work_enhancement']] == [peak, final, work]
    assert schema['crypt']['groups_assigned_total'] == 8 and schema['crypt']['groups_remaining'] == 10
    assert 'Актуальный этап 107:' in (ROOT/'INTEGRATION_RU.md').read_text(encoding='utf8')
    assert 'пакет содержит 86 компонентов' in (ROOT/'READINESS_RU.md').read_text(encoding='utf8')
    assert (ROOT/'CRYPT_TICK_RU.md').exists()
    validation = dict(phase=107, date='2026-10-29', **schema['counts'],
        previous_goal_turn_classification='progress:phase106 publishedThreat/threeCrypt groups; '
                                          'phase107 executeswholeCreaturetick andtwoadditionalgroups',
        accepted_native_requests=28, accepted_full_tick_runs=24,
        accepted_native_Crypt_setter_calls_within_tick_requests=6,
        rejected_final_Fairy_expectation=1,
        exploratory_tick_diagnostics=len(schema['exploratory_diagnostics']),
        package_components=86, prior85_components_preserved_byte_exact=True,
        unchanged_source_executables=4, unchanged_Creature_source_files=82,
        source_game_data_writes=0, original_game_function_or_vtable_patches=0,
        executed_synthetic_IO_providers=0, frozen_phase103_backend_unchanged=True,
        full_live_game_runs=0, new_raw_loader_or_factory_runs=0,
        goal_achieved=False, significant_unknowns_remain=True, limitations=schema['limitations'])
    (ROOT/'phase107-validation.json').write_text(json.dumps(validation, ensure_ascii=False, indent=2), encoding='utf8')
    names = ['crypt-tick-semantics.json', 'CRYPT_TICK_RU.md', 'phase107-validation.json',
             'phase107-primary-manifest.json', 'phase107-crypt-tick-emulations.json',
             'phase107-tick-crosschecks.json', 'INTEGRATION_RU.md', 'READINESS_RU.md', 'editor-format-package.json']
    status = dict(phase=107, date='2026-10-29', published_phase=107,
        status='Published/audited wholeCreaturetick:temporaryFairy speed,Wizard WorkEnhancement andCPUcrosschecks',
        previous_goal_turn_classification=validation['previous_goal_turn_classification'],
        native_tick_cases=22, Ghidra_crosschecks=2, package_components=86, pending_native_jobs=[],
        readiness_estimate_percent=75, evidence=[link(n) for n in names],
        findings=['Original488930 completes inpreparedworld;actualAI/nav/graphicsmath paths,not truncatedcallback.',
                  'Fairy20/108C temporarilyraisescachedspeed1000->1100 thenrestores1000;cap4095 appliesinside tick.',
                  'Wizard16/1094 adds205/count tonativeWorkEnhancement;concretejoboutput remainsstaticonly.',
                  '6nativeCryptsetter/tick compositions shareCPU;otherroomowner8doesnot affectactor7.',
                  'Ghidra/Unicorn agreeall42contexts/95542bytes intwocontrols;prior85schemas/4EXEs/82sources unchanged.'],
        next_action='Do not repeat22tick cases. Restorepriority tounresolvedrawKWD semantics ratherthanfinishallCrypt groups first. '
                    'Review concrete newconsumerleads forJumpEE0/PriorityF55,Objects34C,Traps204,Doors236,Terrain198224 '
                    'or113114;do notreopenclosedfalse-boundaryscans. WorkEnhancement downstream49xxxx/48C800 '
                    'hasnewpreciselead ifneeded,butdo not claimactualresearch/manufactureoutputfromstaticQ12alone. '
                    'Keep PEinitialunconfiguredcamera andstationaryemptyforces scopeexplicit;fullWorld/livegame remainsopen.',
        goal_achieved=False, significant_unknowns_remain=True, limitations=schema['limitations'])
    (ROOT/'phase107-search-status.json').write_text(json.dumps(status, ensure_ascii=False, indent=2), encoding='utf8')
    print(json.dumps({k: v for k, v in validation.items() if k != 'limitations'}))


if __name__ == '__main__':
    main()
