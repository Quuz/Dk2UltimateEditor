"""Publish bounded phase 102 graphics evidence without rerunning native cases."""
from pathlib import Path
import hashlib
import json
from build_effect_semantics import pe_read, EXE
import emulate_element_graphics as native
from audit_targeted_spell import executable_ranges

ROOT = Path(__file__).resolve().parent


def load(name):
    return json.loads((ROOT / name).read_text(encoding='utf8'))


def digest(data):
    return hashlib.sha256(data).hexdigest()


def link(name):
    return dict(file=name, sha256=digest((ROOT / name).read_bytes()))


def primary():
    data = EXE.read_bytes()
    assert digest(data) == 'd7a8b854e4e4f02709eba271bead6fe2307dd6e332b5a96e70857e5b0e19fb8a'
    out = {}
    for key, rows in load('phase102-primary-manifest.json').items():
        out[key] = []
        for row in rows:
            path = ROOT / 'ghidra' / row['memory']
            value = json.loads(path.read_text(encoding='utf8'))
            blob = bytes(value['data'])
            assert int(value['address'], 16) == row['address']
            assert len(blob) == value['length'] == row['length']
            assert blob == pe_read(data, row['address'], row['length'])
            out[key].append(dict(row, memory_sha256=digest(path.read_bytes()),
                                 program_bytes_sha256=digest(blob)))
    assert len(out['functions']) == 39 and sum(r['executed'] for r in out['functions']) == 14
    assert len(out['blocks']) == 8
    return out


def results():
    manifest = load('phase102-element-graphics-emulations.json')
    assert manifest['scope'] == native.__doc__
    cases = {c['name']: c for c in native.cases()}
    assert len(manifest['replays']) == len(cases) == 30
    protected = executable_ranges(EXE.read_bytes())
    protected += [(r['address'], r['length']) for r in load('phase102-primary-manifest.json')['blocks']]
    heaps, rows = {}, []
    for row in manifest['replays']:
        path = ROOT / 'ghidra' / row['file']
        saved = json.loads(path.read_text(encoding='utf8'))
        case = cases[row['case']]
        args = native.build(case)
        assert saved['input'] == args and saved['case'] == case
        assert not saved['response_is_error'] and digest(path.read_bytes()) == row['sha256']
        blob = native.verify(case, saved['result'])
        assert digest(blob) == row['heap_sha256']
        regions = [(int(r['address'], 16), bytes.fromhex(r['hex']))
                   for r in json.loads(args['memory'])['regions']]
        assert all(a + len(b) <= p or a >= p + n
                   for a, b in regions for p, n in protected)
        heaps[case['name']] = blob
        rows.append(dict(row, entry=args['address'], source_wrapper=bool(case.get('source_entry')),
                         complete128byte_heap_hex=blob.hex()))
    pairs = []
    for label, base in [('sprite1', 'sprite1-palette-0-0'), ('sprite2', 'sprite-base'),
                        ('sprite3', 'sprite3-palette-0-0'), ('quad', 'quad-palette-0-0')]:
        for palette in ([255, 0], [0, 255], [255, 255]):
            changed = f'{label}-palette-{palette[0]}-{palette[1]}'
            assert heaps[changed] == heaps[base]
            pairs.append(dict(base=base, changed=changed, complete_heap_equal=True))
    assert heaps['source-sprite-base'] == heaps['source-sprite-palette-both']
    pairs.append(dict(base='source-sprite-base', changed='source-sprite-palette-both',
                      complete_heap_equal=True))
    for name in ['sprite-rgb-control', 'sprite-alpha0', 'sprite-alpha128',
                 'sprite-scale-control', 'sprite-offset-control']:
        assert heaps[name] != heaps['sprite-base']
    assert heaps['quad-rgb-alpha-control'] != heaps['quad-palette-0-0']
    return rows, pairs


def sources():
    corpus = load('element-initialization-semantics.json')['corpus']
    for row in corpus['files']:
        assert digest(Path(row['file']).read_bytes()) == row['sha256']
    profiles = dict(load('executable-hash-verification.json'))
    rc3 = load('rc3-job-overlap-semantics.json')['profile']
    old = load('creature-version-layout-semantics.json')['old_source']
    checks = [(str(ROOT.parent.parent / path), sha) for path, sha in profiles.items()]
    checks += [(rc3['original_file'], rc3['sha256']), (old['original_path'], old['sha256'])]
    for path, sha in checks:
        assert digest(Path(path).read_bytes()) == sha
    return dict(effect_source_files=len(corpus['files']),
                physical_records=corpus['records'], new_raw_API_patches=0,
                prior_exact_patch_evidence=link('element-initialization-semantics.json'),
                source_executables=[dict(file=p, sha256=s) for p, s in checks])


def build():
    p = primary()
    rows, pairs = results()
    raw = sources()
    prior = ROOT / 'phase102-prior-components.json'
    if not prior.exists():
        components = load('editor-format-package.json')['components']
        assert len(components) == 80
        prior.write_text(json.dumps(components, ensure_ascii=False, indent=2), encoding='utf8')
    assert len(load(prior.name)) == 80
    return dict(
        phase=102, date='2026-10-29', family='EffectElements',
        profile=dict(program='DKII-DX.exe', exe=str(EXE), sha256=digest(EXE.read_bytes())),
        primary=p, native_replays=rows, palette_equalities=pairs, source_preservation=raw,
        dependencies=[link(n) for n in ['element-color-semantics.json',
                     'element-initialization-semantics.json', 'ghidra/phase81-element-base.bin']],
        helpers=[link(n) for n in ['emulate_element_graphics.py', 'prepare_element_graphics_primary.py']],
        structure=dict(raw_record_bytes=182, definition_bytes=79, instance_bytes=287,
                       render_info_instance_offset=0x26, resource_pointer_render_offset=0x28,
                       resource_pointer_instance_offset=0x4E, definition_pointer_instance_offset=0x103),
        color_channels=[dict(raw=0xAE+i, definition=0x4A+i, instance=0x6E+i,
                             render=0x48+i, sprite_float=0x34+4*i, quad_float=0x1C+4*i)
                        for i in range(3)],
        graphics_mapping=dict(
            sprite_resource_types=[1, 2, 3], quad_selecting_resource_mask=0x100,
            sprite_constructor=0x57F1E0, quad_constructor=0x57FAF0,
            rgb_formula='f32(f32(byte * f32[670520]) * f32[66FC18]); latter constant=255.',
            alpha_formula='f32(alphaByte * f32[670520]); no final multiplication by255.',
            alpha_offsets=dict(instance=0x71, render=0x4B, sprite_float=0x40, quad_float=0x30),
            example_blue30_float32_hex='0100f041',
            position='Render signedWORD offsets51/53/55 add to actorXYZ; XY /4096, Z /-4096.',
            branch_limits='Prepared current/previous positions and angles equal; frame94=0. Not general interpolation proof.'),
        entry_path=dict(source_wrapper=0x599C60, dispatcher=0x59B880,
                        sprite_builder=0x59C360, quad_builder=0x59C720,
                        allocator_wrapper=0x57CEB0, allocator=0x57CD30, queue_append=0x571AE0,
                        exact_native_cases=30, source_wrapper_cases=4,
                        description='Source wrapper passes actor26/D6/E2; native kind1 skips separate kind2 light branch. '
                                    'Prepared exact128byte free heap block; original allocator executes non-growing branch. '
                                    'Queue context30 stores produced object, original queue append executes.'),
        gates=[dict(where='resource', offset=0xB, mask=1, requirement='set'),
               dict(where='render_info', offset=0xAF, mask=1, requirement='set'),
               dict(where='resource', offset=0xB, mask=0x40000,
                    requirement='when set, globalDWORD759ADC must be nonzero; gameplay name unresolved'),
               dict(where='source_wrapper', argument=4, requirement='zero to submit graphics')],
        palette=dict(raw_offsets=[0xB1, 0xB2], definition_offsets=[0x4D, 0x4E],
                     known_storage_from_prior=True, meaning_resolved=False,
                     global_nonuse_proved=False,
                     finding='Four groups of00/FF00/00FF/FFFF and one source-wrapper pair give identical128byte heap. '
                             'Definition retains varied bytes. Positive RGB/alpha/scale/offset controls change heap. '
                             'Only these prepared sprite/quad paths are covered.'),
        static_review=dict(
            render_info_vtable_methods={hex(0x4B4710): 'field/bit sum', hex(0x4B47C0): 'diagnostic print'},
            element_slot_C=dict(address=0x435EE0, instruction='MOV AX,WORD[ECX+107h];RET'),
            wrapper59D420='Builds mesh through59D260, queries subobject metadata/position, then destroys it. '
                         'This is not the world render submission entry.',
            models='59CCF0/59D010/59C880 and constructors retained as static leads; not executed by new cases.'),
        counts=dict(native_cases=30, source_wrapper_cases=4, allocated_objects=26,
                    sprite_objects=20, quad_objects=6, inactive_cases=4,
                    whole_code_extents=39, executed_path_code_extents=14,
                    file_backed_blocks=8, palette_equal_pairs=13,
                    effect_source_files=raw['effect_source_files'], new_raw_API_patches=0),
        limitations=[
            'Exact supplied DX hash only. No automatic transfer to old/RC3/Pro.',
            'Prepared post-creation phase81 element; no raw loader, new creation, full tick, live game or GPU.',
            'Type1/3 cases preserve prepared RGB; actual initialization has type1 white/player-color priorities. '
            'These prepared post-update colors are not claimed as their native creation colors.',
            'Models4/5/6, procedural7, full interpolation, shaders/framebuffer and other palette consumers remain open.',
            'No game function, executable code or vtable replacement; no external callback provider.',
            'Heap growth/reclamation not executed; exact fitting free block avoids both.',
            '39 code extents attested,14 contain executed fixture paths; not coverage of all their branches.',
            'Case names/fixture key gloom refer only to global759ADC test input, not an assigned gameplay meaning.',
            'No new raw editing API; prior exact patch evidence retained;82 original sources hash-checked.',
            'All significant unknowns in15-family objective remain tracked; goal not achieved.'])


def main():
    schema = build()
    (ROOT / 'element-graphics-semantics.json').write_text(
        json.dumps(schema, ensure_ascii=False, indent=2), encoding='utf8')
    print(json.dumps(schema['counts']))


if __name__ == '__main__':
    main()
