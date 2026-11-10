"""Audit the published phase 102 package and retained native results only."""
from pathlib import Path
import json
import struct
from build_element_graphics import ROOT, build, digest, load, link


def main():
    schema = load('element-graphics-semantics.json')
    assert build() == schema
    package = load('editor-format-package.json')
    prior = load('phase102-prior-components.json')
    assert len(package['components']) == 81 and len(prior) == 80
    assert package['element_graphics_semantics'] == schema
    assert set(package['components']) - set(prior) == {'element_graphics_semantics'}
    for key, row in package['components'].items():
        blob = (ROOT / row['file']).read_bytes()
        assert digest(blob) == row['sha256'] and json.loads(blob) == package[key]
        if key in prior:
            assert row == prior[key]
    heaps = {row['case']: bytes.fromhex(row['complete128byte_heap_hex'])
             for row in schema['native_replays']}
    allocated = [b for b in heaps.values() if b[18] == 1]
    assert len(allocated) == 26
    vtables = [struct.unpack_from('<I', b, 20)[0] for b in allocated]
    assert vtables.count(0x66FCCC) == 20 and vtables.count(0x66FCF4) == 6
    assert len(heaps) - len(allocated) == 4
    assert heaps['sprite-base'][20+0x3C:20+0x40].hex() == '0100f041'
    assert struct.unpack_from('<f', heaps['sprite-alpha0'], 20+0x40)[0] == 0
    assert struct.unpack_from('<f', heaps['sprite-alpha128'], 20+0x40)[0] > 0.5
    assert struct.unpack_from('<3f', heaps['sprite-offset-control'], 20+0x1C) == (11.5, 10.0, -2.25)
    assert sum(r['source_wrapper'] for r in schema['native_replays']) == 4
    assert len(schema['palette_equalities']) == 13
    for row in schema['helpers'] + schema['dependencies']:
        assert digest((ROOT / row['file']).read_bytes()) == row['sha256']
    assert 'Актуальный этап 102:' in (ROOT / 'INTEGRATION_RU.md').read_text(encoding='utf8')
    assert 'пакет содержит 81 компонент' in (ROOT / 'READINESS_RU.md').read_text(encoding='utf8')
    assert (ROOT / 'ELEMENT_GRAPHICS_RU.md').exists()
    validation = dict(phase=102, date='2026-10-29', **schema['counts'],
                      previous_goal_turn_classification='progress: new static graphics path, saved primary evidence and next action changed',
                      package_components=81, previous80_components_preserved_byte_exact=True,
                      source_executables_unchanged=4, original_game_data_written=0,
                      external_callback_providers=0, game_code_or_vtable_replacements=0,
                      new_raw_loaders_or_creations=0, live_game_or_GPU_runs=0,
                      palette_global_nonuse_proved=False, goal_achieved=False,
                      significant_unknowns_remain=True, limitations=schema['limitations'])
    (ROOT / 'phase102-validation.json').write_text(
        json.dumps(validation, ensure_ascii=False, indent=2), encoding='utf8')
    names = ['element-graphics-semantics.json', 'ELEMENT_GRAPHICS_RU.md',
             'phase102-validation.json', 'phase102-primary-manifest.json',
             'phase102-element-graphics-emulations.json', 'INTEGRATION_RU.md',
             'READINESS_RU.md', 'editor-format-package.json']
    status = dict(phase=102, date='2026-10-29', status='Published and audited bounded sprite/quad graphics submission',
                  published_phase=102, previous_goal_turn_classification=validation['previous_goal_turn_classification'],
                  package_components=81, native_cases=30, new_raw_API_patches=0,
                  evidence=[link(n) for n in names], pending_native_jobs=[],
                  findings=['30 native cases,4 through whole actor wrapper599C60; original allocator, constructors and queue append.',
                            'Full128byte heaps plus guards and source contexts match independent expected bytes.',
                            '13 palette pairs unchanged; RGB/alpha/scale/signed offsets give positive differences.',
                            '39 whole code extents/8 PE blocks,14 functions have executed fixture paths. No GPU/live frame claim.',
                            '82 source hashes and4 original EXEs unchanged; prior80 schemas exact; B1/B2 semantics unresolved.'],
                  next_action='Do not repeat accepted30 sprite/quad cases. Inspect actual resource4/5/6 and procedural7 consumers, '
                              'using static primary functions retained in phase102 and preserving definition/resource pointer provenance. '
                              'Native model constructors need a valid original mesh resource; do not use unmapped/null mesh memory '
                              'or replace game functions merely to obtain a passing run. Other15-family unknowns remain open.',
                  readiness_estimate_percent=75, goal_achieved=False,
                  significant_unknowns_remain=True, limitations=schema['limitations'])
    (ROOT / 'phase102-search-status.json').write_text(
        json.dumps(status, ensure_ascii=False, indent=2), encoding='utf8')
    print(json.dumps({k: v for k, v in validation.items() if k != 'limitations'}))


if __name__ == '__main__':
    main()
