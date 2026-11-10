"""Audit phase104 cached native results, targeted edits and current full package."""
import contextlib
import io
import json
from build_possession_damage import ROOT, build, digest, load, link
import validate_possession_damage


def main():
    with contextlib.redirect_stdout(io.StringIO()):
        validate_possession_damage.main()
    schema = load('possession-damage-semantics.json')
    assert build() == schema
    package = load('editor-format-package.json')
    prior = load('phase104-prior-components.json')
    assert len(package['components']) == 83 and len(prior) == 82
    assert package['possession_damage_semantics'] == schema
    assert set(package['components']) - set(prior) == {'possession_damage_semantics'}
    for key, row in package['components'].items():
        blob = (ROOT/row['file']).read_bytes()
        assert digest(blob) == row['sha256'] and json.loads(blob) == package[key]
        if key in prior:
            assert row == prior[key]
    results = {r['case']: r['expected']['outcome'] for r in schema['native_replays']}
    for name, value in [('possessed60', 31), ('normal60', 20), ('secondary60', 15),
                        ('possessed100', 40), ('level2-125', 39), ('level3-150', 47),
                        ('minimum16', 203181), ('pit150', 50), ('pit256', 20),
                        ('possessed-pit', 31), ('secondary-pit', 15),
                        ('current-records-level2', 39), ('current-records-pit-level2', 50)]:
        assert results[name]['result_bits'] == value
    assert results['possessed60']['factor_Q12'] == 6553
    assert results['possessed60'] == results['high-bits60'] == results['scalar-zero'] == results['scalar-high']
    assert results['pit150'] == results['pit-high-bits150']
    assert not schema['jump_review']['meaning_resolved']
    for row in schema['helpers'] + schema['dependencies']:
        assert digest((ROOT/row['file']).read_bytes()) == row['sha256']
    row = schema['pit_flag_name']['diagnostic_evidence']
    assert digest((ROOT/row['file']).read_bytes()) == row['sha256']
    assert 'Актуальный этап 104:' in (ROOT/'INTEGRATION_RU.md').read_text(encoding='utf8')
    assert 'пакет содержит 83 компонента' in (ROOT/'READINESS_RU.md').read_text(encoding='utf8')
    assert (ROOT/'POSSESSION_DAMAGE_RU.md').exists()
    validation = dict(phase=104, date='2026-10-29', **schema['counts'],
                      previous_goal_turn_classification='progress:phase103 published model/native CPU evidence;phase104 verifies new possessionDamage consumer',
                      package_components=83, previous82_components_preserved_byte_exact=True,
                      rejected_initial_projection_attempts=1, total_native_attempts=28,
                      source_executables_unchanged=4, original_game_data_written=0,
                      live_game_runs=0, new_actor_factory_runs=0, raw_Creature_loader_runs=0,
                      actual_attack_or_target_HP_runs=0, original_game_code_or_vtable_patches=0,
                      Jump_semantics_resolved=False, goal_achieved=False,
                      significant_unknowns_remain=True, limitations=schema['limitations'])
    (ROOT/'phase104-validation.json').write_text(json.dumps(validation, ensure_ascii=False, indent=2), encoding='utf8')
    names = ['possession-damage-semantics.json', 'POSSESSION_DAMAGE_RU.md', 'phase104-validation.json',
             'phase104-primary-manifest.json', 'phase104-possession-damage-emulations.json',
             'phase104-damage-api-validation.json', 'INTEGRATION_RU.md', 'READINESS_RU.md',
             'editor-format-package.json']
    status = dict(phase=104, date='2026-10-29', status='Published/audited exact rawVariables-to-possession melee getter',
                  published_phase=104, previous_goal_turn_classification=validation['previous_goal_turn_classification'],
                  package_components=83, accepted_native_cases=27, raw_callbacks=60,
                  exact_container_rebuilds=1452, changed_containers=1447, identity_containers=5,
                  evidence=[link(n) for n in names], pending_native_jobs=[],
                  findings=['Type224/stat4 modifies native494D00 melee damage; currentP60/base20 produces31,not32 becauseQ12.',
                            'Type65/stat4 level coefficient precedes last modifier; current125/level2 withbase20 gives39.',
                            '225 adds100 inCombatPitFighter branch;possession andsecondary08 branches takepriority.',
                            'Wholecontexts andraw callbacks checked;actual498E30attack/targetHP staticonly.',
                            'Jump75C lead51F3B0 isdefinitioncopy;494D00/494EF0 consume5C4/5C6,not newJump meaning.',
                            '1452 edits across557 Variables preserveheaders/order/opaque bits;prior82schemas/4EXEs preserved.'],
                  next_action='Do not repeat accepted27 getter permutations or wideJump searches. Inspect complete498E30 attack-to-targetHP path '
                              'using real target class/vtable,valid lookup andoriginal modifiers. Keep targetfacing,ownerbonuses, '
                              'targetprotection,impact/sound boundaries explicit;do not replace gameplay functions just to pass. '
                              'Use Ghidra forprimary bytes andappropriate CPU backend forunsupported floatingops;keep phase103 helper hashes intact. '
                              'Jump/113114 andother15-family unknowns remain priorities when newconsumerleads emerge.',
                  readiness_estimate_percent=75, goal_achieved=False,
                  significant_unknowns_remain=True, limitations=schema['limitations'])
    (ROOT/'phase104-search-status.json').write_text(json.dumps(status, ensure_ascii=False, indent=2), encoding='utf8')
    print(json.dumps({k: v for k, v in validation.items() if k != 'limitations'}))


if __name__ == '__main__':
    main()
