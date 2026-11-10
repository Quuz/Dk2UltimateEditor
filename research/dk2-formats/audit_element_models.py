"""Current phase103 audit: cached CPU results, source bytes and full package."""
from collections import Counter
import contextlib
import io
import json
import struct
from capstone import Cs, CS_ARCH_X86, CS_MODE_32
from build_effect_semantics import EXE, pe_read
from build_element_models import ROOT, build, load, digest, link
import validate_element_model_flags


def main():
    with contextlib.redirect_stdout(io.StringIO()):
        validate_element_model_flags.main()
    schema = load('element-model-semantics.json')
    assert build() == schema
    package = load('editor-format-package.json')
    prior = load('phase103-prior-components.json')
    assert len(package['components']) == 82 and len(prior) == 81
    assert package['element_model_semantics'] == schema
    assert set(package['components']) - set(prior) == {'element_model_semantics'}
    for key, row in package['components'].items():
        raw = (ROOT/row['file']).read_bytes()
        assert digest(raw) == row['sha256'] and json.loads(raw) == package[key]
        if key in prior:
            assert row == prior[key]
    states = {r['case']: bytes.fromhex(r['complete256bytes_hex']) for r in schema['model_replays']}
    assert struct.unpack_from('<f', states['model6-frame'], 20+0x64)[0] == 1
    assert struct.unpack_from('<f', states['model6-frame-count5'], 20+0x64)[0] == 2
    assert struct.unpack_from('<I', states['model6-frame'], 20+0x68)[0] == 1
    assert struct.unpack_from('<I', states['model6-frame-count5'], 20+0x68)[0] == 2
    for kind in (4, 5, 6):
        base, changed = states[f'model{kind}-palette-0-0'], states[f'model{kind}-resource-bit200']
        offset = 20+(0x4C if kind == 6 else 0x50)
        assert struct.unpack_from('<I', changed, offset)[0] == struct.unpack_from('<I', base, offset)[0] | 0x4000
        assert base[:offset] == changed[:offset] and base[offset+4:] == changed[offset+4:]
    assert states['model4-palette-0-0'][40:76] != states['model5-palette-0-0'][40:76]
    for row in schema['helpers'] + schema['dependencies']:
        assert digest((ROOT/row['file']).read_bytes()) == row['sha256']
    md = Cs(CS_ARCH_X86, CS_MODE_32)
    image = EXE.read_bytes()
    opcodes = Counter()
    for row in schema['model_replays']:
        result = load('ghidra/'+row['file'])['result']
        for address, count in result['visited_instructions'].items():
            address = int(address, 16)
            instruction = next(md.disasm(pe_read(image, address, 15), address))
            opcodes[instruction.mnemonic] += count
    assert opcodes['fcos'] > 0 and opcodes['fsin'] > 0
    assert 'Актуальный этап 103:' in (ROOT/'INTEGRATION_RU.md').read_text(encoding='utf8')
    assert 'пакет содержит 82 компонента' in (ROOT/'READINESS_RU.md').read_text(encoding='utf8')
    assert (ROOT/'ELEMENT_MODELS_RU.md').exists()
    validation = dict(phase=103, date='2026-10-29', **schema['counts'],
                      previous_goal_turn_classification='progress:phase102 published actual graphics path;phase103 adds model paths and CPU backend',
                      package_components=82, previous81_components_preserved_byte_exact=True,
                      executed_fcos=opcodes['fcos'], executed_fsin=opcodes['fsin'],
                      dependency_RECORD_files_verified=schema['cpu_dependency']['installed_RECORD_hashes_verified'],
                      source_executables_unchanged=4, original_game_data_written=0,
                      game_code_or_vtable_substitutions=0, OS_API_stubs=0,
                      live_game_or_GPU_runs=0, actual_asset_loader_runs=0,
                      procedural_runs=0, palette_global_nonuse_proved=False,
                      goal_achieved=False, significant_unknowns_remain=True,
                      limitations=schema['limitations'])
    (ROOT/'phase103-validation.json').write_text(json.dumps(validation, ensure_ascii=False, indent=2), encoding='utf8')
    names = ['element-model-semantics.json', 'ELEMENT_MODELS_RU.md', 'phase103-validation.json',
             'phase103-primary-manifest.json', 'phase103-model-emulations.json',
             'phase103-cross-engine-validation.json', 'phase103-resource-corpus.json',
             'phase103-cpu-dependency.json', 'INTEGRATION_RU.md', 'READINESS_RU.md',
             'editor-format-package.json']
    status = dict(phase=103, date='2026-10-29', status='Published/audited bounded model/cache/queue original-PE execution',
                  published_phase=103, previous_goal_turn_classification=validation['previous_goal_turn_classification'],
                  package_components=82, new_model_cases=22, cross_engine_cases=2,
                  exact_container_rebuild_cases=164, changed_containers=82, identity_containers=82,
                  evidence=[link(n) for n in names], pending_native_jobs=[],
                  findings=['All256bytes of22 model outcomes match independent projection;9 palette pairs unchanged.',
                            'Actual metadata/cache/frame and raw200h->render4000h consumers verified for selected paths.',
                            'Type4 prepared yaw rotatesaxis0;5/6 axis2. Nonuniformscale tested all3;not generalall-angle rule.',
                            'Ghidra fcos unsupported;one diagnostic retained. Isolated Unicorn executes unchangedPE with fatal unmapped access.',
                            'Two explicit-input cross-engine128byte controls match;not global CPU equivalence.',
                            'Corpus48416has4346 type5/820 type6,zero4/7;allpalettebyteszero.',
                            '35 code extents/14 blocks/2325visited PCs;82sourcehashes/4EXEs/prior81schemas preserved.'],
                  next_action='Do not rerun accepted sprite/quad/model permutations. B1/B2 semantics still open;procedural7 absent from this corpus, '
                              'only pursue with a new concrete consumer lead. New Unicorn backend can execute previously unavailable original '
                              'FCOS/FSIN paths without substitutions. Prioritize remaining CreatureJump rawEE0->definition75C or other '
                              'unassigned15-family field consumers, reviewing previous candidate/status files before queries. '
                              'Carry explicit prepared-cache/no-geometry scope forward;do not claim GPU or global palette nonuse.',
                  readiness_estimate_percent=75, goal_achieved=False,
                  significant_unknowns_remain=True, limitations=schema['limitations'])
    (ROOT/'phase103-search-status.json').write_text(json.dumps(status, ensure_ascii=False, indent=2), encoding='utf8')
    print(json.dumps({k: v for k, v in validation.items() if k != 'limitations'}))


if __name__ == '__main__':
    main()
