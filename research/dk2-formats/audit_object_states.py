"""Audit phase110 native states, byte preservation and complete package."""
import contextlib
import io
import json
from build_object_states import ROOT, load, build, digest, link
import validate_object_states_api


def main():
    with contextlib.redirect_stdout(io.StringIO()):
        validate_object_states_api.main()
    schema = load('object-state-semantics.json')
    assert schema == build()
    package, prior = load('editor-format-package.json'), load('phase110-prior-components.json')
    assert len(package['components']) == 89 and len(prior) == 88
    assert set(package['components']) - set(prior) == {'object_state_semantics'}
    assert package['object_state_semantics'] == schema
    for key, row in package['components'].items():
        data = (ROOT/row['file']).read_bytes()
        assert digest(data) == row['sha256'] and json.loads(data) == package[key]
        if key in prior:
            assert row == prior[key]
    for row in schema['helpers'] + schema['dependencies']:
        assert digest((ROOT/row['file']).read_bytes()) == row['sha256']
    results = {r['case']: r['expected'] for r in schema['native_replays']}
    assert (results['zero-stub2-two']['active'], results['zero-stub2-two']['age']) == (0, 1)
    assert (results['zero-stub2-three']['active'], results['zero-stub2-three']['age']) == (2, 0)
    assert (results['copy59-only']['active'], results['copy59-only']['age']) == (59, 0)
    assert results['age-wrap']['age'] == 0
    assert sum(r['expected_null_target_rejection'] for r in schema['native_replays']) == 3
    assert schema['registry']['record_count'] == 59 and len(schema['registry']['entries']) == 59
    assert not schema['unknown_mana_gen']['meaning_resolved']
    assert schema['counts']['newly_named_unknown_raw_fields'] == 0
    assert schema['corpus']['exact_container_rebuilds'] == 738
    assert 'Актуальный этап 110:' in (ROOT/'INTEGRATION_RU.md').read_text(encoding='utf8')
    assert 'пакет содержит 89 компонентов' in (ROOT/'READINESS_RU.md').read_text(encoding='utf8')
    assert (ROOT/'OBJECT_STATES_RU.md').exists()
    validation = dict(
        phase=110, date='2026-10-29', **schema['counts'],
        package_components=89, prior88_components_preserved_byte_exact=True,
        unchanged_source_executables=4, original_source_data_writes=0,
        original_game_code_or_vtable_patches=0, executed_synthetic_IO_providers=0,
        frozen_phase108_backend_unchanged=True, live_game_runs=0,
        expected_rejections='Exactly three original null callback attempts; backend prevents execution at0; no Windows crash observed.',
        goal_achieved=False, significant_unknowns_remain=True, limitations=schema['limitations'])
    (ROOT/'phase110-validation.json').write_text(json.dumps(validation, ensure_ascii=False, indent=2), encoding='utf8')
    names = ['object-state-semantics.json', 'OBJECT_STATES_RU.md', 'phase110-validation.json',
             'phase110-primary-manifest.json', 'phase110-object-state-emulations.json',
             'phase110-object-state-crosschecks.json', 'phase110-object-state-api-validation.json',
             'INTEGRATION_RU.md', 'READINESS_RU.md', 'editor-format-package.json']
    status = dict(
        phase=110, date='2026-10-29', published_phase=110,
        status='Published/audited Object StartState registry and whole raw-loader/tick compositions; unknown ManaGen remains open',
        previous_goal_turn_classification='progress: phase109 published and audited88-component package; phase110 establishes native state constraints and periodic MiscEffect consumer',
        package_components=89, pending_native_jobs=[], evidence=[link(n) for n in names],
        readiness_estimate_percent=75,
        findings=[
            'RawStartState35B->definition11B->copy/update/uncheckeddispatch confirmed in original code.',
            'Initializer binds0..58; selected59/63/255 reach null target on second update in prepared original PE/BSS.',
            'State39 requests effect288 at3*TPS; state40 uses raw358 MiscEffectId; original null graphics path returns normally.',
            '26native compositions:23normal/3expected rejections;3Ghidra controls;738exact rebuilds/82sources;11756identity views.',
            'All game code/vtables and prior88 schemas preserved; significant raw-field meanings remain unresolved.',
        ],
        next_action=(
            'Do not repeat26state cases or the broad displacement scans. Trace actual natural activation/claim event of ManaVault101 '
            'and ClaimedManaVault108, including union state and credited player fields, if following Object34C. '
            'Neither start-state names nor the effect288 request prove mana generation. '
            'Alternatively pursue a concrete unresolved field consumer in another family; Terrain198/224 searches already have extensive bounded negative results. '
            'Jump/Priority/113114/Trap204/Door236 and other15-family unknowns remain open.'),
        goal_achieved=False, significant_unknowns_remain=True, limitations=schema['limitations'])
    (ROOT/'phase110-search-status.json').write_text(json.dumps(status, ensure_ascii=False, indent=2), encoding='utf8')
    print(json.dumps({k: v for k, v in validation.items() if k != 'limitations'}))


if __name__ == '__main__':
    main()
