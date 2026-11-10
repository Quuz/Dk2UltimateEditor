"""Verify whole native buffers, source hashes, new raw patches and package."""
from pathlib import Path
from collections import Counter
import json
import struct
import dk2_formats as fmt
from build_door_loading_semantics import ROOT, load, digest, primary
from native_door_loading import *
from emulate_door_loading import cases, build_case, TOKEN


def main():
    s = load('door-loading-semantics.json')
    functions, blocks = primary()
    assert functions == s['primary_program_bytes'] and blocks == s['primary_data_ranges']
    assert len(cases()) == len(s['native_emulations']) == 58
    counts, buffers, sound_calls = Counter(), {}, 0
    for c, row in zip(cases(), s['native_emulations']):
        saved = load('ghidra/' + row['file'])
        args, want = build_case(c)
        r = saved['result']
        assert row['sha256'] == digest((ROOT / 'ghidra' / row['file']).read_bytes())
        assert saved['case'] == c and saved['input'] == args and saved['expected_registers'] == want
        assert r['success'] and r['hit_return'] and all(int(r['registers'][k], 16) == v for k, v in want.items())
        m = {int(x['address'], 16): bytes.fromhex(x['hex']) for x in json.loads(args['memory'])['regions']}
        counts[c['kind']] += 1
        buffers[c['name']] = m
        sound_calls += int.from_bytes(m[0x10006400][:4], 'little')
        for fn in functions:
            if fn['program'] == args['program']:
                assert all(a + len(b) <= fn['address'] or a >= fn['address'] + fn['length'] for a, b in m.items())
        mid, prev, out = m[0x10003000], m[0x10004000], m[0x10005000]
        if c['kind'] == 'init':
            assert mid == bytes(732) and prev == out
        elif c['kind'] == 'converter':
            assert prev == out and all(struct.unpack_from('<I', mid, i)[0] == 1 for i in range(0, 0x74, 4))
            assert mid[0x292:0x298] == bytes([c['seed']])*6 and mid[0x2A4:0x2A6] == bytes([c['seed']])*2
            assert struct.unpack_from('<I', mid, 0x29C)[0] == (c['seed']*0x01010101 | 0x11223344)
        else:
            p = profile(c['sha'])
            gate = c.get('gate', 0)
            applies = struct.unpack_from('<I', mid, 0x38)[0]
            assert out[0x99:0x9B] == (mid[0x2AA:0x2AC] if applies else prev[0x99:0x9B])
            expected_sum = sum(mid) if p['gate'] is None or not gate else int.from_bytes(prev[0x87:0x8B], 'little')
            assert int.from_bytes(out[0x87:0x8B], 'little') == expected_sum
            requested = sound_requested(mid, c['sha'], gate)
            assert bool(int.from_bytes(m[0x10006400][:4], 'little')) == requested
            assert out[0xA9:0xAD] == (TOKEN.to_bytes(4, 'little') if requested else prev[0xA9:0xAD])
            if c['kind'] == 'chain':
                assert mid[0x292:0x298] == bytes(6) and mid[0x2A4:0x2A6] == bytes(2)
                assert struct.unpack_from('<I', mid, 0x29C)[0] == 0x11223344
    assert counts == dict(init=6, converter=12, loader=16, chain=24)
    for sha in PROFILES:
        for gate in [0, 1]:
            base = buffers[f'chain-{sha[:3]}-gap-0-gate-{gate}']
            for gap in [255, 'pattern']:
                other = buffers[f'chain-{sha[:3]}-gap-{gap}-gate-{gate}']
                assert base[0x10003000] == other[0x10003000] and base[0x10005000] == other[0x10005000]
            a = buffers[f'chain-{sha[:3]}-research-0-gate-{gate}'][0x10005000]
            b = buffers[f'chain-{sha[:3]}-research-65535-gate-{gate}'][0x10005000]
            assert a[:0x87] == b[:0x87] and a[0x8B:] == b[0x8B:]
            assert (a == b) == (sha == DX and gate == 1)
        a = buffers[f'chain-{sha[:3]}-sum-collision-255'][0x10005000]
        b = buffers[f'chain-{sha[:3]}-sum-collision-65280'][0x10005000]
        assert a != b and a[0x87:0x8B] == b[0x87:0x8B]
        assert a[0x99:0x9B] == b'\xff\0' and b[0x99:0x9B] == b'\0\xff'
    coverage = Counter()
    for _, _, dst, n in CONVERT + RESOURCES:
        coverage.update(range(dst, dst+n))
    for off, n in [(0, 0x74), (0x74, 32), (0x2BC, 32), (0x29C, 4)]:
        coverage.update(range(off, off+n))
    assert set(range(732)) - set(coverage) == set(range(0x292, 0x298)) | set(range(0x2A4, 0x2A6))
    assert set(coverage.values()) == {1}
    files = records = patches = 0
    for row in s['corpus']['files']:
        source = Path(row['file']).read_bytes()
        assert digest(source) == row['sha256']
        parts = fmt.parse(source)
        assert b''.join(q.pack() for q in parts) == source
        files += 1
        for si, sec in enumerate(parts):
            if fmt.FAMILIES.get(sec.signature) != 'Doors':
                continue
            relative = len(sec.header)
            for ri, raw in enumerate(sec.records):
                value = fmt.u16(raw, 0x224) ^ 0xFFFF
                changed = patch_research_time(raw, value)
                rr = list(sec.records)
                rr[ri] = changed
                new = fmt.Section(sec.offset, sec.signature, sec.header, list(sec.records), sec.suffix, sec.stride)
                new.replace_records(rr)
                packed = b''.join(new.pack() if j == si else q.pack() for j, q in enumerate(parts))
                manual = bytearray(source)
                at = sec.offset + relative + 0x224
                manual[at:at+2] = value.to_bytes(2, 'little')
                assert packed == manual and b''.join(q.pack() for q in fmt.parse(packed)) == packed
                records += 1
                patches += 1
                relative += len(raw)
    assert (files, records, patches) == (82, 656, 656)
    checks = 0
    for n in [0, 1, 32767, 32768, 65535]:
        assert fmt.u16(patch_research_time(bytes(616), n), 0x224) == n
        checks += 1
    bad = [lambda n=n: patch_research_time(bytes(616), n) for n in [-1, 65536, True, 1.0, '1']]
    bad += [lambda n=n: intermediate(bytes(n), bytes(732)) for n in [615, 617]]
    bad += [lambda n=n: definition(bytes(732), bytes(n), DX) for n in [180, 182]]
    bad += [lambda n=n: definition(bytes(732), bytes(181), n) for n in ['', None, '1.3']]
    for fn in bad:
        try:
            fn()
        except ValueError:
            checks += 1
        else:
            raise AssertionError('Invalid API input accepted')
    rc3 = load('camera-catalogue-semantics.json')['rc3']
    assert digest(Path(rc3['original_file']).read_bytes()) == digest(Path(rc3['research_copy']).read_bytes()) == rc3['sha256']
    old = load('creature-version-layout-semantics.json')['old_source']
    assert digest(Path(old['original_path']).read_bytes()) == digest(Path(old['path']).read_bytes()) == old['sha256']
    for file, sha in load('executable-hash-verification.json').items():
        assert digest((ROOT.parent.parent / file).read_bytes()) == sha
    pack = load('editor-format-package.json')
    assert len(pack['components']) == 67 and pack['door_semantics']['phase67_loading'] == s
    for key, row in pack['components'].items():
        raw = (ROOT / row['file']).read_bytes()
        assert digest(raw) == row['sha256'] and json.loads(raw) == pack[key]
    v = dict(phase=67, date='2026-10-27', native_emulations=58, native_cases=dict(counts),
             captured_sound_lookups=sound_calls, complete_executed_native_functions=8,
             complete_primary_pe_functions=8, primary_data_ranges=2,
             source_files_roundtripped=files, door_records=records, new_exact_in_memory_patches=patches,
             api_boundary_checks=checks, source_executables_unchanged=4, package_components=67,
             research_writes_to_game_data=0, modified_gameplay_runs=0,
             word236_gameplay_still_open=True, limitations=s['limitations'])
    (ROOT / 'phase67-validation.json').write_text(json.dumps(v, ensure_ascii=False, indent=2), encoding='utf8')
    print(json.dumps({k: val for k, val in v.items() if k != 'limitations'}))


if __name__ == '__main__':
    main()
