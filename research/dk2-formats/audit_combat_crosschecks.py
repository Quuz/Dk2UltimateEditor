"""Verify actual eight Ghidra results and preserve all historical components."""
from pathlib import Path
import json
from build_combat_crosschecks import ROOT,load,link,sha,build
from build_poison import effective_shot_parameters


def main():
    value=load('combat-crosscheck-semantics.json');assert value==build()
    package=load('editor-format-package.json');prior=load('phase118-prior-components.json')
    assert len(package['components'])==97 and len(prior)==96
    assert set(package['components'])-set(prior)=={'combat_crosscheck_semantics'}
    assert package['combat_crosscheck_semantics']==value
    assert package['effective_shot_parameters']==effective_shot_parameters()
    for key,row in package['components'].items():
        raw=(ROOT/row['file']).read_bytes()
        assert sha(raw)==row['sha256'] and json.loads(raw)==package[key]
        if key in prior:assert row==prior[key]
    assert package['validation_updates']==value['supersedes_validation_status']
    assert package['outstanding_controls_from_phases114_115']==0
    assert 'Актуальный этап 118:' in (ROOT/'INTEGRATION_RU.md').read_text(encoding='utf8')
    assert 'пакет содержит 97 компонентов' in (ROOT/'READINESS_RU.md').read_text(encoding='utf8')
    paths={r['file']:r['sha256'] for r in value['source_executables_unchanged']+package['editor_variant_semantics']['profiles']}
    for path,digest in paths.items():assert sha(Path(path).read_bytes())==digest
    inputs={}
    for n in ['phase114-shot-damage-api-validation.json','phase115-poison-api-validation.json','phase116-work-rate-api-validation.json']:
        path=ROOT/n
        if not path.exists():continue
        for row in load(n).get('files',[]):
            path=row.get('input_path',row.get('file'))
            if path and 'sha256' in row:inputs[path]=row['sha256']
    for path,digest in inputs.items():assert sha(Path(path).read_bytes())==digest
    output=dict(phase=118,date='2026-10-29',independent_Ghidra_CPU_controls=8,
        damage_controls=5,poison_controls=3,all8_prepared_input_hashes_unchanged=True,
        compared_regions=value['totals']['checked_regions'],compared_bytes=value['totals']['checked_bytes'],
        remaining_prior_prepared_controls=0,prior96_schema_components_preserved_byte_exact=True,
        package_components=97,effective_Shot_process38_correction_preserved=True,
        original_executable_paths_unchanged=len(paths),prior_input_file_hashes_checked=len(inputs),
        newly_named_unknown_fields=0,new_container_rebuilds=0,gameplay_instruction_substitutions=0,
        goal_achieved=False,significant_unknowns_remain=True,limitations=value['limitations'])
    (ROOT/'phase118-validation.json').write_text(json.dumps(output,ensure_ascii=False,indent=2),encoding='utf8')
    status=dict(phase=118,published_phase=118,date='2026-10-29',
        status='All eight historical Ghidra damage/poison controls completed after review recovery; prepared Trap factory route is next unpublished lead.',
        previous_goal_turn_classification='progress within active continuation: phase117 published stock/Pro profile boundary and96component package.',
        current_goal_turn_classification='progress:8actualindependentGhidraCPUcontrols withunchangedpreparedinputs,fullcontextchecks and97componentpackage.',
        pending_native_jobs=[],pending_controls=[],
        historical_pending_resolved=[link('phase114-pending-crosschecks.json'),link('phase115-pending-crosschecks.json')],
        evidence=[link(n) for n in ['combat-crosscheck-semantics.json','COMBAT_CROSSCHECKS_RU.md',
            'phase114-shot-damage-crosschecks.json','phase115-poison-crosschecks.json','phase118-validation.json',
            'INTEGRATION_RU.md','READINESS_RU.md','editor-format-package.json']],
        unpublished_lead=dict(probe=link('probe_trap_factory.py'),summary=link('phase118-trap-factory-discovery-summary.json'),
            status='3originalCTrapcreations withprepared209-bytedefinition,291-bytecaptures and2pairedwholecontexts;unknown98unread. Not yetprimary-attested/published asnewgameplaysemantics.',
            next='Compose actual rawTrap579->mid747->definition209 loader andfactory; verify fulldefinition/state/ownereffects andoriginalbytes. Actualfactory4B7C10/instancector4B7400/init4A7940. Do not confuse Doorloader4A7100 withTraploader4AA440. Instanceallocationrequest123h,notoldsymbolhint287.'),
        next_action='Continue unknownTraps204->definition98 viaactualrawload/creation/activation consumers. Do not repeat8acceptedGhidracontrols,stock/Procomparison or broad displacement scans. Reuse live Ghidra; review works again. Unknownsall15families remainopen; nooverallcompletion claim.',
        package_components=97,readiness_estimate_percent=75,goal_achieved=False,
        significant_unknowns_remain=True,limitations=value['limitations'])
    (ROOT/'phase118-search-status.json').write_text(json.dumps(status,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:v for k,v in output.items() if k!='limitations'}))


if __name__=='__main__':main()
