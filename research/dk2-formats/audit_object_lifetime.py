"""Audit phase108 Object lifetime,losslesspatches and complete format package."""
import contextlib
import io
import json
from build_object_lifetime import ROOT, build, load, digest, link
import validate_object_lifetime_api


def main():
    with contextlib.redirect_stdout(io.StringIO()):
        validate_object_lifetime_api.main()
    schema = load('object-lifetime-semantics.json')
    assert schema == build()
    package = load('editor-format-package.json')
    prior = load('phase108-prior-components.json')
    assert len(package['components']) == 87 and len(prior) == 86
    assert set(package['components'])-set(prior) == {'object_lifetime_semantics'}
    assert package['object_lifetime_semantics'] == schema
    for key, row in package['components'].items():
        data = (ROOT/row['file']).read_bytes()
        assert digest(data) == row['sha256'] and json.loads(data) == package[key]
        if key in prior:
            assert row == prior[key]
    for row in schema['helpers']+schema['dependencies']:
        assert digest((ROOT/row['file']).read_bytes()) == row['sha256']
    results = {r['case']: r['expected'] for r in schema['native_replays']}
    for name, hp in [('normal', 1000), ('always', 999), ('outside', 999), ('inside-room', 1000),
                     ('always-inside', 999), ('both-inside', 999), ('always-in-hand', 1000),
                     ('hp1', 0), ('hp0', 0), ('hp-negative', -1), ('five-ticks', 0),
                     ('five-ticks-in-room', 3), ('mana100', 999), ('manaffff', 999),
                     ('damage-and-decay', 4), ('not-damageable-and-decay', 9), ('damage-only', 5)]:
        assert results[name]['hp'] == hp
    assert schema['counts']['unknown_semantic_fields_closed'] == 0
    assert not schema['unknown_mana_gen']['meaning_resolved']
    assert 'Актуальный этап 108:' in (ROOT/'INTEGRATION_RU.md').read_text(encoding='utf8')
    assert 'пакет содержит 87 компонентов' in (ROOT/'READINESS_RU.md').read_text(encoding='utf8')
    assert (ROOT/'OBJECT_LIFETIME_RU.md').exists()
    validation = dict(phase=108, date='2026-10-29', **schema['counts'],
        previous_goal_turn_classification='progress:phase107 completedCreaturetick andtwoCryptmodifiers; '
                                          'phase108 confirmsObjectFlags/Hpconsumer andtargetedlosslessAPI',
        accepted_Object_native_requests=21, additional_Creature_readtrace_diagnostic=1,
        package_components=87, prior86_components_preserved_byte_exact=True,
        unchanged_source_executables=4, original_source_data_writes=0,
        original_game_code_or_vtable_patches=0, executed_synthetic_IO_providers=0,
        frozen_phase107_backend_unchanged=True, newraw_loader_or_factory_runs=0,
        live_game_runs=0, goal_achieved=False, significant_unknowns_remain=True,
        limitations=schema['limitations'])
    (ROOT/'phase108-validation.json').write_text(json.dumps(validation, ensure_ascii=False, indent=2), encoding='utf8')
    names = ['object-lifetime-semantics.json', 'OBJECT_LIFETIME_RU.md', 'phase108-validation.json',
             'phase108-primary-manifest.json', 'phase108-object-lifetime-emulations.json',
             'phase108-object-crosschecks.json', 'phase108-object-lifetime-api-validation.json',
             'INTEGRATION_RU.md', 'READINESS_RU.md', 'editor-format-package.json']
    status = dict(phase=108, date='2026-10-29', published_phase=108,
        status='Published/audited completeObjecttick HPdecayconsumer andexactFlags/HpAPI;unknownManaGen remainsopen',
        previous_goal_turn_classification=validation['previous_goal_turn_classification'],
        package_components=87, native_object_cases=19, Ghidra_crosschecks=2, pending_native_jobs=[],
        evidence=[link(n) for n in names], readiness_estimate_percent=75,
        findings=['Flags344 masks1/2 actuallydecrementsharedHP348 by1perObjecttick underRoomId/InHand gates.',
                  'Damageanddecay shareinstanceDWORD40;HP0 doesnotunregisterinside49BD20;Worldremoval separate.',
                  '574patches/82sources preserveallotherFlags/unknownManaGen/opaque bytes;11756identityviews.',
                  'ManaGen106 notreadinselectedState0ticks;Creature unknownfields notreadinonescopedtick;noglobalnonuseclaim.',
                  '7wholePE/3blocks/4EXEs/prior86schemas/frozenbackend107 preserved.'],
        next_action='Do not repeat19Objecttick cases orwidefalse-boundaryscans. '
                    'Concrete next route forObjects34C/ManaGen106:trace completeoriginalObjectfactory andownerregistration '
                    'withdefinitionreadhooks,usingexistingphase89 factory fixture andexplicitexternalIO boundaries. '
                    'Consumption mayoccuratregistration/owneraggregate ratherthanperObjecttick;verify actualreceivers. '
                    'Consider analogousTrap98/Door99 creation paths onlyafterrealtypebinding. '
                    'OtherObjectstatecallbacks andWorldmanaaggregation remainpossible;no unusedclaim. '
                    'Jump/Priority/113114/Terrain198224 andall15-family scope remainopen.',
        goal_achieved=False, significant_unknowns_remain=True, limitations=schema['limitations'])
    (ROOT/'phase108-search-status.json').write_text(json.dumps(status, ensure_ascii=False, indent=2), encoding='utf8')
    print(json.dumps({k: v for k, v in validation.items() if k != 'limitations'}))


if __name__ == '__main__':
    main()
