"""Audit phase105 primary/native evidence and the complete editor package."""
import json
from pathlib import Path
from build_melee_attack import ROOT, build, load, digest, link


def main():
    schema = load('melee-attack-semantics.json')
    assert schema == build()
    package = load('editor-format-package.json')
    prior = load('phase105-prior-components.json')
    assert len(package['components']) == 84 and len(prior) == 83
    assert package['melee_attack_semantics'] == schema
    assert set(package['components'])-set(prior) == {'melee_attack_semantics'}
    for key, row in package['components'].items():
        data = (ROOT/row['file']).read_bytes()
        assert digest(data) == row['sha256'] and json.loads(data) == package[key]
        if key in prior:
            assert row == prior[key]
    for row in schema['helpers']+schema['dependencies']:
        assert digest((ROOT/row['file']).read_bytes()) == row['sha256']
    # Recheck all source hashes without claiming new1452patch/native executions.
    corpus = load('phase104-damage-api-validation.json')
    for row in corpus['files']:
        assert digest(Path(row['file']).read_bytes()) == row['sha256']
    assert corpus['file_count'] == 557 and corpus['exact_container_rebuilds'] == 1452
    outcomes = schema['outcomes']
    for name, hp in [('object-possessed60', 969), ('object-normal60', 980),
                     ('creature-possessed60', 969), ('creature-both-possessed', 985),
                     ('object-owner-both2', 952), ('current-variable-records', 961),
                     ('crypt-darkangel-insert', 966), ('crypt-darkangel-two', 963),
                     ('crypt-unique-darkangel-insert', 965), ('crypt-darkangel-normal', 978),
                     ('crypt-darkangel-clear', 969), ('crypt-unique-darkangel-clear', 969),
                     ('crypt-darkangel-other-owner', 969), ('crypt-unique-darkangel-other-owner', 969),
                     ('creature-north', 954), ('creature-east', 962), ('creature-west', 962)]:
        assert outcomes[name]['hp'] == hp
    base = outcomes['creature-possessed60']
    assert all(base == outcomes[n] for n in ['creature-heading-changed', 'creature-only-target-heading',
                                            'creature-only-attacker-heading'])
    assert outcomes['normal-cooldown-gate']['attack_return'] == 1
    assert outcomes['possessed-skips-cooldown']['hp'] == 969
    assert outcomes['object-not-damageable']['hp'] == 1000
    assert outcomes['creature-target445']['sound'] == 0
    assert outcomes['creature-both-possessed']['event'] == 1
    assert schema['crypt']['other_unassigned_groups'] == 15
    assert not schema['crypt']['raw_percentage_field_found']
    assert 'Актуальный этап 105:' in (ROOT/'INTEGRATION_RU.md').read_text(encoding='utf8')
    assert 'пакет содержит 84 компонента' in (ROOT/'READINESS_RU.md').read_text(encoding='utf8')
    assert (ROOT/'MELEE_ATTACK_RU.md').exists()
    validation = dict(phase=105, date='2026-10-29', **schema['counts'],
        previous_goal_turn_classification='progress:original fullattack executed and Object timestamps distinguished; '
                                          'continued to40accepted cases andtwoCrypt effect assignments',
        accepted_native_attempts=40, rejected_expectation_diagnostics=1, total_native_attempts=41,
        prior83_components_preserved_byte_exact=True, package_components=84,
        unchanged_source_executables=4, unchanged_variable_sources=557,
        original_game_data_writes=0, original_game_code_or_vtable_patches=0,
        new_rawCreature_Object_loader_or_factory_runs=0, live_game_runs=0,
        goal_achieved=False, significant_unknowns_remain=True, limitations=schema['limitations'])
    (ROOT/'phase105-validation.json').write_text(json.dumps(validation, ensure_ascii=False, indent=2), encoding='utf8')
    names = ['melee-attack-semantics.json', 'MELEE_ATTACK_RU.md', 'phase105-validation.json',
             'phase105-primary-manifest.json', 'phase105-melee-attack-emulations.json',
             'INTEGRATION_RU.md', 'READINESS_RU.md', 'editor-format-package.json']
    status = dict(phase=105, date='2026-10-29', published_phase=105,
        status='Published/audited rawVariables-to-complete melee-to-targetHP andtwoCrypt effects',
        previous_goal_turn_classification=validation['previous_goal_turn_classification'],
        package_components=84, accepted_native_cases=40, pending_native_jobs=[],
        evidence=[link(n) for n in names],
        findings=['Complete498E30 reaches targetHP withoriginalclasses/vtables;P60/base20->31,possessedvictim->15.',
                  'Crypt23->1090->10percent/count;44->1091->15percent/count;insert/clear/otherowner tested.',
                  'WorldXY angle changes requestedCreature damage;preparedF4 orientation doesnot,not provenbackstab.',
                  'NativeNULL-model query/fallback executed;actualmodel/effects/audio/livecombat remainopen.',
                  '40accepted wholecontexts,26functions/8blocks,557sources/4EXEs/prior83schemas preserved.'],
        readiness_estimate_percent=75, goal_achieved=False, significant_unknowns_remain=True,
        next_action='Do not repeat40accepted melee cases orclaim allcombat solved. '
                    'Use existingphase94 counter-consumer evidence toseek remaining15Crypt group meanings; '
                    'prioritize newactualconsumers ofunresolved KWDfields whenalead exists. '
                    'Jump/113114/Priority/Objects34C/Traps204/Doors236/Terrain198224 andother15family questions remainopen. '
                    'Inspect actual code/address bases,not GUIlabels alone;do not renew closed false-boundary scans.',
        limitations=schema['limitations'])
    (ROOT/'phase105-search-status.json').write_text(json.dumps(status, ensure_ascii=False, indent=2), encoding='utf8')
    print(json.dumps({k: v for k, v in validation.items() if k != 'limitations'}))


if __name__ == '__main__':
    main()
