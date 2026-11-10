"""Audit phase106 native results, exact Threat patches and all package entries."""
import contextlib
import io
import json
from pathlib import Path
from build_crypt_threat import ROOT, build, load, digest, link
import validate_threat_api


def main():
    with contextlib.redirect_stdout(io.StringIO()):
        validate_threat_api.main()
    schema = load('crypt-threat-semantics.json')
    assert schema == build()
    package = load('editor-format-package.json')
    prior = load('phase106-prior-components.json')
    assert len(package['components']) == 85 and len(prior) == 84
    assert set(package['components'])-set(prior) == {'crypt_threat_semantics'}
    assert package['crypt_threat_semantics'] == schema
    for key, row in package['components'].items():
        data = (ROOT/row['file']).read_bytes()
        assert digest(data) == row['sha256'] and json.loads(data) == package[key]
        if key in prior:
            assert row == prior[key]
    for row in schema['helpers']+schema['dependencies']:
        assert digest((ROOT/row['file']).read_bytes()) == row['sha256']
    out = schema['outcomes']
    for name, value in [('base', 100), ('vampire1', 130), ('vampire2', 160),
                        ('unique1-alone', 100), ('unique3-alone', 100), ('both1', 182),
                        ('vampire1-unique3', 182), ('vampire2-unique1', 288),
                        ('maiden1', 90), ('both1-maiden1', 163), ('maiden10', 0),
                        ('maiden11-wrap', 65526), ('level2', 125), ('level2-both', 226),
                        ('target-sum-wrap', 64), ('duplicate-slots', 99),
                        ('inactive-counts-in-denominator', 50), ('target-inactive', 0),
                        ('crypt-vampire-insert', 130), ('crypt-unique-alone', 100),
                        ('crypt-unique-with-vampire', 182), ('crypt-unique-clear', 130),
                        ('crypt-maiden-target', 90), ('crypt-maiden-source-owner', 100),
                        ('threat-zero', 0), ('threat200', 200), ('current-level-record', 125)]:
        assert out[name]['targets']['101']['value'] == value, (name, out[name])
    assert out['two-slots']['targets']['101']['value'] == out['two-slots']['targets']['102']['value'] == 50
    assert out['base']['source526'] == 60 and out['heart-near']['source526'] == 0
    assert out['heart-distance-clamp']['source526'] == 100
    assert out['threshold-equal']['targets']['101']['source'] == 999
    assert out['source-state-gate']['source524'] == out['source-word44b-gate']['source524'] == 100
    assert schema['crypt_effects'][1]['coefficient_source'] == 0x108E
    assert schema['counts']['crypt_groups_remaining'] == 12
    assert 'Актуальный этап 106:' in (ROOT/'INTEGRATION_RU.md').read_text(encoding='utf8')
    assert 'пакет содержит 85 компонентов' in (ROOT/'READINESS_RU.md').read_text(encoding='utf8')
    assert (ROOT/'CRYPT_THREAT_RU.md').exists()
    validation = dict(phase=106, date='2026-10-29', **schema['counts'],
        previous_goal_turn_classification='progress:phase105 published fullattack/HP andtwoCrypt effects; '
                                          'phase106 confirmsThreat consumer andthreemorecountergroups',
        accepted_native_attempts=39, rejected_native_attempts=0,
        non_native_cache_key_normalization_fix='Pythoninteger targetkeys normalizedtostrings beforecachedcomparison;no nativefailure.',
        package_components=85, prior84_components_preserved_byte_exact=True,
        unchanged_source_executables=4, original_game_data_writes=0,
        original_game_code_or_vtable_patches=0, external_IO_provider_calls=0,
        new_full_rawCreature_loader_runs=0, live_game_runs=0,
        goal_achieved=False, significant_unknowns_remain=True, limitations=schema['limitations'])
    (ROOT/'phase106-validation.json').write_text(json.dumps(validation, ensure_ascii=False, indent=2), encoding='utf8')
    names = ['crypt-threat-semantics.json', 'CRYPT_THREAT_RU.md', 'phase106-validation.json',
             'phase106-primary-manifest.json', 'phase106-threat-emulations.json', 'phase106-threat-api-validation.json',
             'phase106-crypt-consumer-leads.json', 'INTEGRATION_RU.md', 'READINESS_RU.md', 'editor-format-package.json']
    status = dict(phase=106, date='2026-10-29', published_phase=106,
        status='Published/audited nativeThreat distribution andVampire/UniqueVampire/Maiden Crypt modifiers',
        previous_goal_turn_classification=validation['previous_goal_turn_classification'],
        accepted_native_cases=39, package_components=85, pending_native_jobs=[],
        evidence=[link(n) for n in names], readiness_estimate_percent=75,
        findings=['CreatureF12 unsignedThreat→definition698 actual45E4F0 distribution;65/stat3 level coefficient executed.',
                  'Vampire108E +30percent/count;Unique108F gatesadditional multiplierusing108E,not itsowncount.',
                  'Maiden108D recipientmodifier (10-count)*10percent hasno localclamp;diagnostic11 givesnegativeWORDwrap.',
                  'Nonzeroslots countedbefore validity/visibility;duplicates accumulate;receivedthreshold isstrict andperaddition.',
                  '39nativecases/6Cryptcompositions,328rebuilds/82sources,prior84schemas/4EXEs preserved.',
                  '4BCB60/4BCF10 indexed108Areads arechecksum/diagnostic,not unresolvedgameplaybonusconsumers.'],
        next_action='Do not repeat39Threat cases orlinearCrypt scan. Inspect savedwhole488930 tick andactualvslot90/8C '
                    'sourcebinding forowner108C,plusowner1094→actor3B3,withoutclaiming fulltick fromsmallhelpers. '
                    'Use newconsumers toclose meaningfulremainingfields;prioritize unresolvedrawKWD consumers '
                    'Jump/Priority/Objects34C/Traps204/Doors236/Terrain198224/113114 whenconcreteleads exist. '
                    'Do not reinterpretchecksum loopsasgameplay orcall12remaininggroupsunused.',
        goal_achieved=False, significant_unknowns_remain=True, limitations=schema['limitations'])
    (ROOT/'phase106-search-status.json').write_text(json.dumps(status, ensure_ascii=False, indent=2), encoding='utf8')
    print(json.dumps({k: v for k, v in validation.items() if k != 'limitations'}))


if __name__ == '__main__':
    main()
