"""Original PE model paths in Unicorn; retained Ghidra fcos failure is diagnostic.

Whole256-byte snapshots compare byte-exactly to an independent sin/cos/scale
projection for the explicitly prepared angles. Palette comparisons are exact
across the entire256-byte snapshots. No all-angle numerical equivalence claim.
"""
from pathlib import Path
import hashlib
import json
import math
import struct
import sys
from build_effect_semantics import EXE, pe_read
import emulate_element_models as model
from native_unicorn import run

ROOT = Path(__file__).resolve().parent


def f32(value):
    return struct.unpack('<f', struct.pack('<f', value))[0]


def expected(case):
    image = EXE.read_bytes()
    constant = lambda a: struct.unpack('<f', pe_read(image, a, 4))[0]
    blob = bytearray([0xA5]) * 256
    blob[:20] = bytes(20)
    struct.pack_into('<IHB', blob, 12, 192, 30, 1)
    blob[19] = 1
    obj = 20
    kind = case['kind']
    scalar = [x/4096 for x in case.get('scale', [4096, 4096])]
    scale = [scalar[0], scalar[0], scalar[1]] if kind == 6 else [scalar[0], scalar[1], scalar[0]]
    theta = f32((123+1024) * constant(0x6704B8) * constant(0x670548))
    c, s = f32(math.cos(theta)), f32(math.sin(theta))
    # 5801E0 passes the fifth stack argument to axis0;59D010 supplies yaw there.
    # With prepared render20/24=0 this is X rotation, unlike model5/6 Z rotation.
    rotation = [1, 0, 0, 0, c, -s, 0, s, c] if kind == 4 else [c, -s, 0, s, c, 0, 0, 0, 1]
    matrix = [f32(v * scale[i//3]) for i, v in enumerate(rotation)]
    u32 = {0: 0x66FD64 if kind == 6 else 0x66FD1C, 4: 0}
    floats = {8: 10.5, 12: 10.5, 16: -2}
    floats.update(zip(range(0x14, 0x38, 4), matrix))
    flags = 0x10203040 | (0x4000 if case.get('resource_flags', 1) & 0x200 else 0)
    rgb = [f32(f32(x * constant(0x670520)) * constant(0x66FC18))
           for x in case.get('rgb', [10, 20, 30])]
    alpha = f32(case.get('alpha', 255) * constant(0x670520))
    if kind == 6:
        previous, current = case.get('frame_q12', [0, 0])
        phase = f32((previous+current)/8192)
        frame = f32(phase * (case.get('frames', 3)-1))
        u32.update({0x4C: flags, 0x50: 0, 0x54: model.CACHE,
                    0x68: min(max(0, int(frame)), case.get('frames', 3)-2)})
        floats.update({0x38: phase, 0x3C: alpha, 0x58: 0,
                       0x60: max(scale), 0x64: frame})
        floats.update(zip([0x40, 0x44, 0x48], rgb))
        byte_fields = {0x88: 0, 0x89: 0, 0x98: 0}
    else:
        u32.update({0x38: model.CACHE, 0x4C: 0, 0x50: flags})
        floats.update({0x48: alpha, 0x54: 0, 0x58: max(scale)})
        floats.update(zip([0x3C, 0x40, 0x44], rgb))
        byte_fields = {0x7C: 0, 0x7D: 0}
        if kind == 5:
            byte_fields[0x8C] = 0
    for offset, value in u32.items():
        struct.pack_into('<I', blob, obj+offset, value)
    for offset, value in floats.items():
        struct.pack_into('<f', blob, obj+offset, value)
    for offset, value in byte_fields.items():
        blob[obj+offset] = value
    return bytes(blob), matrix


def verify(case, result):
    assert result['success'] and result['hit_return'], (case['name'], result.get('emulation_error'))
    assert not result['context_mismatches'], (case['name'], result['context_mismatches'])
    assert not result['protected_write_or_execution_failures']
    assert int(result['registers']['EAX'], 16) == 1
    actual = bytes.fromhex(result['captured_hex'])
    reference, matrix = expected(case)
    matrix_begin, matrix_end = 20+0x14, 20+0x38
    assert actual[:matrix_begin] == reference[:matrix_begin]
    assert actual[matrix_end:] == reference[matrix_end:], (case['name'], [
        (i, a, b) for i, (a, b) in enumerate(zip(actual, reference))
        if a != b and not matrix_begin <= i < matrix_end])
    values = struct.unpack_from('<9f', actual, matrix_begin)
    assert actual == reference, (case['name'], values, matrix)
    return actual


def main():
    selected = model.cases()
    if len(sys.argv) > 1:
        selected = [c for c in selected if c['name'] == sys.argv[1]]
        assert selected
    rows = []
    for case in selected:
        args, checks = model.build(case, True)
        tag = hashlib.sha256(json.dumps(args, sort_keys=True).encode()).hexdigest()[:10]
        path = ROOT / 'ghidra' / f'phase103-unicorn-{case["name"]}-{tag}.json'
        if case['name'] == 'model5-palette-0-0':
            initial = ROOT / 'ghidra/phase103-unicorn-model5-initial.json'
            if initial.exists():
                path = initial
        if path.exists():
            saved = json.loads(path.read_text(encoding='utf8'))
            assert saved['case'] == case and saved['input'] == args
        else:
            result = run(EXE, args, checks, model.OBSERVER, model.HEAP, 256)
            saved = dict(case=case, input=args, result=result)
            path.write_text(json.dumps(saved, indent=2), encoding='utf8')
        blob = verify(case, saved['result'])
        rows.append(dict(case=case['name'], file=path.name,
                         sha256=hashlib.sha256(path.read_bytes()).hexdigest(),
                         snapshot_sha256=hashlib.sha256(blob).hexdigest()))
        print(case['name'], 'verified', saved['result']['instruction_count'], flush=True)
    if len(selected) == len(model.cases()):
        (ROOT / 'phase103-model-emulations.json').write_text(
            json.dumps(dict(scope=__doc__, replays=rows), indent=2), encoding='utf8')


if __name__ == '__main__':
    main()
