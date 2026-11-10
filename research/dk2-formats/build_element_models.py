"""Publish original-PE model execution, typed cache boundary and raw bit edits."""
from pathlib import Path
import base64
import csv
import hashlib
import json
from build_effect_semantics import EXE, pe_read
from build_element_graphics import sources as unchanged_sources
import emulate_element_models as fixture
import run_element_models_unicorn as native
import memory_readout

ROOT = Path(__file__).resolve().parent


def load(name):
    return json.loads((ROOT/name).read_text(encoding='utf8'))


def digest(data):
    return hashlib.sha256(data).hexdigest()


def link(name):
    return dict(file=name, sha256=digest((ROOT/name).read_bytes()))


def primary():
    data = EXE.read_bytes()
    out = {}
    for kind, rows in load('phase103-primary-manifest.json').items():
        out[kind] = []
        for row in rows:
            path = ROOT/'ghidra'/row['memory']
            value = json.loads(path.read_text(encoding='utf8'))
            blob = bytes(value['data'])
            assert int(value['address'], 16) == row['address']
            assert len(blob) == row['length'] == value['length']
            assert blob == pe_read(data, row['address'], row['length'])
            out[kind].append(dict(row, memory_sha256=digest(path.read_bytes()),
                                  program_bytes_sha256=digest(blob)))
    assert len(out['functions']) == 35 and sum(r['executed'] for r in out['functions']) == 33
    assert len(out['blocks']) == 14
    return out


def replays():
    manifest = load('phase103-model-emulations.json')
    assert manifest['scope'] == native.__doc__
    cases = {c['name']: c for c in fixture.cases()}
    rows, snapshots, visited = [], {}, set()
    for row in manifest['replays']:
        path = ROOT/'ghidra'/row['file']
        value = json.loads(path.read_text(encoding='utf8'))
        case = cases[row['case']]
        args, checks = fixture.build(case, True)
        assert value['input'] == args and value['case'] == case
        assert digest(path.read_bytes()) == row['sha256']
        result = value['result']
        assert result['backend'] == 'Unicorn' and result['backend_version'] == '2.1.4'
        assert result['source_exe_sha256'] == digest(EXE.read_bytes())
        blob = native.verify(case, result)
        assert digest(blob) == row['snapshot_sha256']
        visited.update(int(a, 16) for a in result['visited_instructions'])
        snapshots[case['name']] = blob
        rows.append(dict(row, instruction_count=result['instruction_count'],
                         complete256bytes_hex=blob.hex(), context_check_regions=len(checks)))
    assert len(rows) == len(cases) == 22
    pairs = []
    for kind in (4, 5, 6):
        base = f'model{kind}-palette-0-0'
        for a, b in ((255, 0), (0, 255), (255, 255)):
            changed = f'model{kind}-palette-{a}-{b}'
            assert snapshots[base] == snapshots[changed]
            pairs.append(dict(base=base, changed=changed, complete256bytes_equal=True))
        assert snapshots[base] != snapshots[f'model{kind}-scale']
        assert snapshots[base] != snapshots[f'model{kind}-resource-bit200']
    assert snapshots['model6-frame'] != snapshots['model6-frame-count5']
    cross = load('phase103-cross-engine-validation.json')
    assert len(cross['replays']) == 2
    for row in cross['replays']:
        for key in ['Ghidra', 'Unicorn']:
            assert digest((ROOT/row[key]['file']).read_bytes()) == row[key]['sha256']
        ghidra = load(row['Ghidra']['file'])
        cpu = load(row['Unicorn']['file'])
        assert ghidra['input'] == cpu['input']
        assert bytes.fromhex(cpu['result']['captured_hex']) == memory_readout.decode(ghidra['result']['registers'])
        assert cpu['result']['success'] and not cpu['result']['protected_write_or_execution_failures']
        visited.update(int(a, 16) for a in cpu['result']['visited_instructions'])
    assert len(visited) == 2325
    spans = load('phase103-primary-manifest.json')['functions']
    assert all(any(r['address'] <= a < r['address']+r['length'] for r in spans) for a in visited)
    diagnostic = list((ROOT/'ghidra').glob('phase103-model-model5-palette-0-0-*.json'))
    assert len(diagnostic) == 1
    failed = json.loads(diagnostic[0].read_text(encoding='utf8'))['result']
    assert not failed['success'] and not failed['hit_return'] and 'fcos' in failed['emulation_error']
    return rows, pairs, cross, link(str(diagnostic[0].relative_to(ROOT)))


def dependency():
    value = load('phase103-cpu-dependency.json')
    for row in value['files']:
        assert digest((ROOT/row['file']).read_bytes()) == row['sha256']
    target = ROOT/'dependencies/unicorn-2.1.4'
    record = target/'unicorn-2.1.4.dist-info/RECORD'
    checked = 0
    for path, encoded, size in csv.reader(record.read_text(encoding='utf8').splitlines()):
        if not encoded:
            continue
        algorithm, encoded = encoded.split('=', 1)
        assert algorithm == 'sha256'
        blob = (target/path).read_bytes()
        actual = base64.urlsafe_b64encode(hashlib.sha256(blob).digest()).decode().rstrip('=')
        assert actual == encoded and len(blob) == int(size)
        checked += 1
    return dict(value, installed_RECORD_hashes_verified=checked,
                documentation_sources=['https://www.unicorn-engine.org/docs/',
                                       'https://pypi.org/project/unicorn/2.1.4/'])


def build():
    p = primary()
    rows, pairs, cross, diagnostic = replays()
    preservation = unchanged_sources()
    corpus = load('phase103-resource-corpus.json')
    assert corpus['records'] == 48416 and corpus['nonzero_palette_records'] == 0
    assert corpus['exact_container_rebuild_cases'] == 164 and corpus['changed_containers'] == 82
    prior = ROOT/'phase103-prior-components.json'
    if not prior.exists():
        before = load('editor-format-package.json')['components']
        assert len(before) == 81
        prior.write_text(json.dumps(before, ensure_ascii=False, indent=2), encoding='utf8')
    assert len(load(prior.name)) == 81
    return dict(phase=103, date='2026-10-29', family='EffectElements',
        profile=dict(program='DKII-DX.exe', exe=str(EXE), sha256=digest(EXE.read_bytes())),
        primary=p, model_replays=rows, palette_equalities=pairs, cross_engine=cross,
        Ghidra_failure_diagnostic=diagnostic, cpu_dependency=dependency(),
        helpers=[link(n) for n in ['emulate_element_models.py', 'run_element_models_unicorn.py',
                 'native_unicorn.py', 'native_element_model_flags.py',
                 'crosscheck_unicorn_graphics.py', 'validate_element_model_flags.py',
                 'prepare_element_models_primary.py']],
        dependencies=[link(n) for n in ['element-color-semantics.json', 'element-graphics-semantics.json',
                      'element-initialization-semantics.json', 'ghidra/phase81-element-base.bin']],
        source_preservation=preservation, corpus=corpus,
        entry_path=dict(entry=0x599C60, dispatcher=0x59B880,
                        builders={'4': 0x59D010, '5': 0x59CCF0, '6': 0x59C880},
                        constructors={'4': 0x5801E0, '5': 0x580480, '6': 0x583120},
                        queue_append=0x571AE0),
        cache_contract=dict(resource_handle_offset=0xF, table_global=0x766A6C,
                            table_entry_bytes=12, cache_pointer_entry_offset=4,
                            cache_loaded_metadata_pointer_offset=0,
                            frame_count_metadata_offset=8, looping_flag_metadata_offset=0x1C,
                            frame_setup=0x585ED0, cache_acquire=0x57ED30, cache_release=0x57ECE0,
                            fixture='Valid index1, loaded singleton cache node, explicit3/5-frame metadata. '
                                    'No lazy loading, actual geometry, asset parser or GPU draw. '
                                    'All metadata/cache buffers and guards compared;expected LRU global update checked.'),
        color_mapping=dict(instance_rgb_bytes=[0x6E, 0x6F, 0x70],
                           static_model_rgb_floats=[0x3C, 0x40, 0x44],
                           animated_model_rgb_floats=[0x40, 0x44, 0x48],
                           instance_alpha_byte=0x71, static_alpha_float=0x48, animated_alpha_float=0x3C,
                           numeric_contract='Same byte->f32 normalized->f32 RGB*255 as phase102;alpha normalized only.'),
        transforms=dict(prepared_angle=123, render20=0, render24=0,
                        angle_formula='f32((123+1024)*float32[6704B8]*float32[670548])',
                        type4='Under these prepared angles, axis0 rotation;5801E0 passes fifth stackarg to41C580 axis0.',
                        type5_and6='Under these prepared angles, axis2 rotation.',
                        scale_diagonal_static=['X', 'Y', 'X'], scale_diagonal_animated=['X', 'X', 'Y'],
                        exact_comparison='All256bytes including matrix floats match independent projection for selected inputs. '
                                         'Not a general all-angle/interpolation claim.'),
        resource_bit200=dict(raw_flags_DWORD_offset=0x60, raw_type_BYTE_offset=0x70,
                             loaded_resource_flags_offset=0xB,
                             baseline_name='DOESNT_USE_PROGRESSIVE_MESH',
                             confirmed='When resource mask200h is set,OR4000h into model render flags.',
                             static_object_flags_offset=0x50, animated_object_flags_offset=0x4C,
                             raw_API='native_element_model_flags.patch_resource_bit200(raw182,boolean)',
                             limitation='Underlying LOD/render implementation not executed. Raw edit and native runtime flag cases separate; '
                                        'not a new raw-loader-to-GPU composition.'),
        palette=dict(raw_offsets=[0xB1, 0xB2], definition_offsets=[0x4D, 0x4E],
                     meaning_resolved=False, global_nonuse_proved=False,
                     finding='Nine pairs across model types4/5/6 produce identical complete256byte outcomes.'),
        procedural_review=dict(dispatcher=0x5A01A0, mesh_constructor=0x588190,
                               evidence='Static whole function extents only;resourceDWORD0 selects procedural branch.',
                               original_corpus_type7_records=0, executed_cases=0,
                               limitation='Not all procedural helper consumers traced;not a palette nonuse proof.'),
        counts=dict(new_model_cases=22, cross_engine_cases=2, successful_original_code_runs=24,
                    rejected_Ghidra_fcos_probe=1, palette_equal_pairs=9, whole_code_extents=35,
                    executed_path_code_extents=33, file_backed_blocks=14,
                    unique_visited_instructions=2325, exact_container_rebuild_cases=164,
                    changed_containers=82, identity_containers=82, effect_source_files=82),
        limitations=['Exact supplied DX hash only; no automatic old/RC3/Pro transfer.',
                     'Prepared post-creation element,cache node,metadata. No new raw loader/factory/fulltick/asset decoding.',
                     'No model geometry read by GPU;frames header contract is not a complete model file specification.',
                     'Type4 is an EXE-supported control;corpus has4346 type5 and820 type6,zero4/7.',
                     'Original functions/vtables never replaced. Unicorn code/write hooks observe or reject only;unmapped access fatal.',
                     'Two same-explicit-input cross-engine heaps do not prove global emulator equivalence;Ghidra model probe failed atfcos.',
                     'All-angle transforms,complete interpolation/procedural effects/framebuffer and B1/B2 meaning remain open.',
                     'Whole code extents and visited-PC coverage do not prove execution of all branches.',
                     '22 new model runs plus2 cross-engine control runs are counted separately;failedGhidra probe is not accepted.',
                     '164 physical container checks include82 identity outcomes. Original sources unmodified.',
                     'All15-family significant unknowns persist;readiness remains about75%,goal not achieved.'])


def main():
    schema = build()
    (ROOT/'element-model-semantics.json').write_text(json.dumps(schema, ensure_ascii=False, indent=2), encoding='utf8')
    print(json.dumps(schema['counts']))


if __name__ == '__main__':
    main()
