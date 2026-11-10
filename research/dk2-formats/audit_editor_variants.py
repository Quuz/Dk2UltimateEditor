"""Audit the exact editor profile boundary and preserve earlier package schemas."""
from pathlib import Path
import json
from build_editor_variant_semantics import ROOT, schema, load, link
from scan_editor_variants import sha, read_profiles
from survey_local_modules import build as modules
from build_poison import effective_shot_parameters


def main():
    value=load('editor-variant-semantics.json')
    assert value==schema()
    assert load('phase117-local-module-survey.json')==modules()
    package=load('editor-format-package.json')
    prior=load('phase117-prior-components.json')
    assert len(prior)==95 and len(package['components'])==96
    assert set(package['components'])-set(prior)=={'editor_variant_semantics'}
    assert package['editor_variant_semantics']==value
    assert package['effective_shot_parameters']==effective_shot_parameters()
    for key,row in package['components'].items():
        raw=(ROOT/row['file']).read_bytes()
        assert sha(raw)==row['sha256'] and json.loads(raw)==package[key]
        if key in prior:
            assert row==prior[key]
    read_profiles()
    sources={r['file']:r['sha256'] for r in value['source_executables_unchanged']+value['profiles']}
    assert len(sources)==6
    for path,digest in sources.items():
        assert sha(Path(path).read_bytes())==digest
    files=load('phase116-work-rate-api-validation.json')['files']
    assert len(files)==82
    for row in files:
        assert sha(Path(row['input_path']).read_bytes())==row['sha256']
    assert 'Актуальный этап 117:' in (ROOT/'INTEGRATION_RU.md').read_text(encoding='utf8')
    assert 'пакет содержит 96 компонентов' in (ROOT/'READINESS_RU.md').read_text(encoding='utf8')
    output=dict(phase=117,date='2026-10-29',complete_editor_PE_comparisons=2,
        changed_code_bytes=28,changed_code_regions=14,dialog_resource_switches=13,
        resource_leaf_pairs=281,identical_resource_leaf_pairs=280,
        parsed_dialog_pairs=115,identical_dialog_pairs=114,
        cached_Ghidra_range_pairs=232,identical_Ghidra_range_pairs=231,
        known_dialog_constructor_different_pairs=1,new_Ghidra_CPU_controls=0,
        new_container_rebuilds=0,new_GUI_saves=0,newly_named_unknown_raw_fields=0,
        unchanged_source_executable_paths=6,unchanged_local_DLL_paths=5,
        unchanged_prior_Creature_source_files=82,package_components=96,
        prior95_components_preserved_byte_exact=True,
        effective_Shot_process38_correction_preserved=True,
        goal_achieved=False,significant_unknowns_remain=True,limitations=value['limitations'])
    (ROOT/'phase117-validation.json').write_text(json.dumps(output,ensure_ascii=False,indent=2),encoding='utf8')
    status=dict(phase=117,published_phase=117,date='2026-10-29',
        status='Published exact stock/Pro editor boundary and shared serialization-code evidence; no new unknown gameplay labels.',
        previous_goal_turn_classification='no progress on artifacts: preceding reply answered the theoretical damage-context question using published evidence. Current source state revalidated before new work.',
        current_goal_turn_classification='progress: exhaustive original stock/Pro comparison,281 resource pairs,232 cached Ghidra range reattestations,protected-name control-flow evidence and96component package.',
        pending_native_jobs=[],pending_controls=[link('phase114-pending-crosschecks.json'),link('phase115-pending-crosschecks.json')],
        findings=['Provided stock aliases arebyteidentical; fanPro changes13resourceID pushes andonepostwarningabort branch.',
                  'Expanded templates preexist in stock;280/281resourceleaves unchanged,onlyAbout differs.',
                  '231/232priorProbyte ranges also match stock;lastconstructor differsonlyinknownresourceID immediate.',
                  'DKII.dll exports shareonezero-return stub;not additional originalconsumer code.',
                  'APpassage-write candidates areuntyped;two4773D0writes addressCreature52C,notAP2E. No04/08meaning assigned.'],
        evidence=[link(n) for n in ['editor-variant-semantics.json','EDITOR_VARIANTS_RU.md',
            'phase117-editor-variant-evidence.json','phase117-primary-manifest.json','phase117-local-module-survey.json',
            'phase117-ap-passage-write-leads.json','phase117-rejected-ap-leads.json',
            'phase117-validation.json','INTEGRATION_RU.md','READINESS_RU.md','editor-format-package.json']],
        package_components=96,readiness_estimate_percent=75,
        next_action='Do not repeatstock/Probinarycomparison or broad AP/Terrain scans. Unknownrawfield gameplay still priority: seek a concrete typed indirectconsumer. Procedural7staticcode alone didnot establishB1/B2reads; do not run mere render permutationswithout anewlead. Earlierphase114/115eightGhidraCPUcontrols remainprepared; retry through the same reviewed path afterstatedreviewreset,withoutrestartinglivePID19308solelybecauseoftimeout. Flameimplementationnotanalysed;stubexportnamesnotprimarysemantics.',
        goal_achieved=False,significant_unknowns_remain=True,limitations=value['limitations'])
    (ROOT/'phase117-search-status.json').write_text(json.dumps(status,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:v for k,v in output.items() if k!='limitations'}))


if __name__=='__main__':
    main()
