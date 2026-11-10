"""Audit phase109 factory/mana evidence and lossless package publication."""
import json
from build_factory_mana import ROOT, build, load, digest, link


def main():
    schema = load('factory-mana-semantics.json')
    assert schema == build()
    package = load('editor-format-package.json')
    prior = load('phase109-prior-components.json')
    assert len(package['components']) == 88 and len(prior) == 87
    assert set(package['components']) - set(prior) == {'factory_mana_semantics'}
    assert package['factory_mana_semantics'] == schema
    for key, row in package['components'].items():
        data = (ROOT / row['file']).read_bytes()
        assert digest(data) == row['sha256'] and json.loads(data) == package[key]
        if key in prior:
            assert row == prior[key]
    for row in schema['helpers'] + schema['dependencies']:
        assert digest((ROOT / row['file']).read_bytes()) == row['sha256']
    expected_mana = {
        'base': 1120, 'object-mana100': 1120, 'object-manaffff': 1120,
        'positive-rate2': 1240, 'zero-rate': 1000, 'income-ceiling': 1080,
        'stored-cap': 10000, 'negative-income': 950, 'upkeep-and-ceiling': 1000,
        'deadline-wait': 1000, 'deadline-exact': 1120,
        'one-shot-counters-reset': 1120, 'mode3-setting2': 1240,
        'mode3-setting0': 1060,
    }
    results = {r['case']: r['expected'] for r in schema['native_replays']}
    assert set(results) == set(expected_mana)
    assert all(results[name]['current'] == value for name, value in expected_mana.items())
    assert not results['deadline-wait']['due'] and results['deadline-wait']['last'] == 81
    assert results['deadline-exact']['due'] and results['deadline-exact']['last'] == 100
    assert schema['counts']['actual_cost_helper_calls_in_Unicorn'] == 26
    assert schema['counts']['unknown_semantic_fields_closed'] == 0
    assert not schema['object_unknown']['meaning_resolved']
    assert schema['object_unknown']['observed_reads'] == 0
    assert schema['external_IO']['gameplay_function_replacements'] == 0
    assert 'Актуальный этап 109:' in (ROOT/'INTEGRATION_RU.md').read_text(encoding='utf8')
    assert 'пакет содержит 88 компонентов' in (ROOT/'READINESS_RU.md').read_text(encoding='utf8')
    assert (ROOT/'FACTORY_MANA_RU.md').exists()
    validation = dict(
        phase=109, date='2026-10-29', **schema['counts'],
        package_components=88, prior87_components_preserved_byte_exact=True,
        unchanged_source_executables=4, original_source_data_writes=0,
        original_game_code_or_vtable_patches=0,
        allowed_external_IO_providers_per_factory=2,
        actual_external_IO_calls_per_factory=dict(null_graphics=1, fixed_sound_registration=2),
        frozen_phase108_fieldtrace_backend_unchanged=True,
        exploratory_stack_copy_failure=dict(accepted=False, original_instructions_executed=0,
                                            classification='fixture-copy error, not game crash'),
        new_raw_loader_runs=0, live_game_runs=0, goal_achieved=False,
        significant_unknowns_remain=True, limitations=schema['limitations'])
    (ROOT/'phase109-validation.json').write_text(
        json.dumps(validation, ensure_ascii=False, indent=2), encoding='utf8')
    names = ['factory-mana-semantics.json', 'FACTORY_MANA_RU.md', 'phase109-validation.json',
             'phase109-primary-manifest.json', 'phase109-factory-mana-emulations.json',
             'phase109-factory-mana-crosschecks.json', 'INTEGRATION_RU.md',
             'READINESS_RU.md', 'editor-format-package.json']
    status = dict(
        phase=109, date='2026-10-29', published_phase=109,
        status='Published/audited original Object factory plus complete Player mana handler; ManaGen remains unresolved',
        previous_goal_turn_classification='progress: complete original factory/registration/income composition, bounded negative ManaGen evidence',
        package_components=88, native_compositions=14, Ghidra_crosschecks=2,
        pending_native_jobs=[], evidence=[link(n) for n in names],
        readiness_estimate_percent=75,
        findings=[
            '14 same-CPU factory-to-income compositions and two Ghidra controls pass.',
            'Deadline, income ceiling, stored capacity, rate and numeric mode branches verified in prepared states.',
            'ManaGen definition106 values 0/100/65535 are unread and do not affect selected income; no global nonuse conclusion.',
            'All game instructions and vtables preserved; exactly two inherited external graphics/sound providers allowed.',
            'Four EXEs, 82 Object source files, prior87 schemas and frozen phase108 backend preserved.',
        ],
        next_action=(
            'Trace actual writers of Player10E2 with receiver identity, including World aggregate rebuild and ownership transitions. '
            'Do not repeat the14 arithmetic cases or closed false-boundary scans. '
            'Other Object state callbacks or older-version consumers remain possible. '
            'Continue concrete consumers for unknown raw fields across all15 families; '
            'Trap204/Door236 require actual type binding, and Jump/Priority/113114/Terrain198224 remain open.'),
        goal_achieved=False, significant_unknowns_remain=True, limitations=schema['limitations'])
    (ROOT/'phase109-search-status.json').write_text(
        json.dumps(status, ensure_ascii=False, indent=2), encoding='utf8')
    print(json.dumps({k: v for k, v in validation.items() if k != 'limitations'}))


if __name__ == '__main__':
    main()
