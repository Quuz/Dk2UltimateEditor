"""Actual old-PE converter/getters and whole definition copy; no damage claim.

Converter baseline opaque bytes are measured once, then changes are predicted
only at the exact raw/intermediate resistance pairs. Copy expects every byte of
the2199-byte definition and guards. Original instructions are never replaced.
"""
from pathlib import Path
import hashlib, json, struct
import dk2_formats as fmt
import native_unicorn_factorytrace as cpu
from emulate_resistance import post_context
from emulate_element_colors import region

ROOT = Path(__file__).resolve().parent
EXE = ROOT / 'source-binaries/DKII-local13.exe'
SOURCE = ROOT.parent.parent / 'Dungeon Keeper 2/Data/editor/Creatures.kwd'
RAW, MID, SRC, DST, STOP = [0x18000000 + i * 0x10000 for i in range(5)]
SETS = {'zero': [(0, 0)] * 4, 'known': [(4, 0), (6, 100), (8, 50), (15, 255)],
    'unknown': [(16, 50), (30, 75), (32, 75), (255, 255)]}


def sha(blob):
    return hashlib.sha256(blob).hexdigest()


def cases():
    rows = [dict(name='converter-zero', kind='converter', values='zero', slot=0)]
    for kind, slot in [('type', 0), ('type', 1), ('type', 2), ('value', 0), ('value', 1), ('value', 2), ('type', 4), ('value', 4)]:
        rows.append(dict(name=f'{kind}-unknown-{slot}', kind=kind, values='unknown', slot=slot))
    rows += [dict(name='type-known-1', kind='type', values='known', slot=1),
        dict(name='value-known-1', kind='value', values='known', slot=1)]
    rows += [dict(name='copy-' + name, kind='copy', values=name, slot=0) for name in SETS]
    return rows


def build(case):
    if case['kind'] == 'copy':
        definition = bytearray((index * 37 + 11) & 255 for index in range(2199))
        definition[0x61C:0x624] = bytes(v for pair in SETS[case['values']] for v in pair)
        definition[0x78A] = 4
        memory = {SRC: bytes(definition) + bytes([165]) * 64, DST: bytes([90]) * 2199 + bytes([165]) * 64}
        calls = [dict(address='0x512fa0', registers=dict(ECX=hex(DST)), stack_args=[SRC])]
        target, ecx, capture, size = 0x59DEB0, DST, DST, 2199 + 64
        expected_eax = 0  # Exact canonical no-argument old helper: XOR EAX,EAX; RET.
    else:
        raw = bytearray(next(raw for section in fmt.parse(SOURCE.read_bytes()) if section.signature == 0xAA
            for raw in section.records if raw[0xF4B] == 8))
        raw[0xE0C:0xE14] = bytes(v for pair in SETS[case['values']] for v in pair)
        memory = {RAW: bytes(raw) + bytes([165]) * 64, MID: bytes([165]) * (6457 + 64)}
        calls = [dict(address='0x55f170', registers=dict(ECX=hex(MID)), stack_args=[RAW])]
        ecx, capture, size = MID, MID, 6457 + 64
        if case['kind'] == 'converter':
            target, expected_eax = 0x5600B0, 0  # ArgRAW>=4, no read.
        else:
            calls.append(dict(address='0x5600b0', registers=dict(ECX=hex(MID)), stack_args=[case['slot']]))
            target = 0x5600D0 if case['kind'] == 'type' else 0x5600F0
            expected_eax = SETS[case['values']][case['slot']][0 if case['kind'] == 'type' else 1] if case['slot'] < 4 else 0
    # Getter cases retain the last explicit presence-getter index for the root
    # type/value getter. Copy mutates its stack argument, so its root is a proved
    # no-argument helper instead of assuming that the source pointer survives.
    stack = bytes([165]) * 16384
    memory[0x7FFEE000] = stack
    args = dict(program='DKII-local13.exe', address=hex(target), registers=json.dumps(dict(ECX=hex(ecx))),
        memory=json.dumps(dict(regions=[region(address, blob) for address, blob in memory.items()])),
        before_calls=calls, max_steps=200000, observe_reads=[[0, 4096], [MID, 6521], [SRC, 2263]],
        observe_writes=[[MID, 6521], [DST, 2263]])
    checks = {address: blob for address, blob in memory.items() if address != 0x7FFEE000}
    return args, checks, capture, size, expected_eax


def path_for(case):
    args, *_ = build(case)
    for prior in sorted((ROOT / 'ghidra').glob(f'phase126-old-resistance-{case["name"]}-*.json')):
        saved = json.loads(prior.read_text(encoding='utf8'))
        if saved['input'] == args and saved['case'] == case:
            return prior
    tag = sha((json.dumps(args, sort_keys=True) + sha((ROOT / 'native_unicorn_factorytrace.py').read_bytes())).encode())[:12]
    return ROOT / 'ghidra' / f'phase126-old-resistance-{case["name"]}-{tag}.json'


def expected(case, checks):
    if case['kind'] == 'copy':
        return {**checks, DST: checks[SRC]}
    base = cases()[0]
    args, base_checks, *_ = build(base)
    saved = json.loads(path_for(base).read_text(encoding='utf8'))
    assert saved['input'] == args
    wanted = post_context(base_checks, saved['result'])
    wanted[RAW] = checks[RAW]
    mid = bytearray(wanted[MID])
    mid[0x11FC:0x1204] = bytes(v for pair in SETS[case['values']] for v in pair)
    wanted[MID] = bytes(mid)
    return wanted


def verify(case, args, checks, expected_eax, result):
    assert result['source_exe_sha256'] == '93e826a142f677ef5120699feddb0572799d93399167ef1d05ec4b48a8b2a342'
    assert result['success'] and result['hit_return'] and not result['protected_write_or_execution_failures']
    assert all(row['returned'] for row in result['before_calls'])
    assert int(result['registers']['EAX'], 16) == expected_eax
    assert result['function_or_vtable_substitutions'] == 0
    assert not any(int(row['address'], 16) < 4096 for row in result['observed_reads'])
    post = post_context(checks, result)
    assert post == expected(case, checks), case['name']
    if case['kind'] != 'copy':
        assert post[MID][0x11FC:0x1204] == bytes(v for pair in SETS[case['values']] for v in pair)
        assert post[MID][0x284:0x294] == struct.pack('<4I', 1, 1, 1, 1)
        assert post[MID][6457:] == bytes([165]) * 64
        assert result['visited_instructions'].get('0x55f170') == 1
    else:
        assert result['visited_instructions'].get('0x512fa0') == 1
    return dict(kind=case['kind'], pairs=[list(pair) for pair in SETS[case['values']]], slot=case['slot'], EAX=expected_eax,
        full_context_regions=len(post), full_context_bytes=sum(map(len, post.values())),
        instructions=result['instruction_count'], game_damage_path_executed=False)


def main():
    rows = []
    for case in cases():
        args, checks, capture, size, expected_eax = build(case)
        path = path_for(case)
        if path.exists():
            saved = json.loads(path.read_text(encoding='utf8'))
            assert saved['input'] == args and saved['case'] == case
        else:
            result = cpu.run(EXE, args, list(checks.items()), STOP, capture, size)
            saved = dict(case=case, input=args, result=result)
            path.write_text(json.dumps(saved, indent=2), encoding='utf8')
        outcome = verify(case, args, checks, expected_eax, saved['result'])
        rows.append(dict(case=case['name'], file=str(path.relative_to(ROOT)), sha256=sha(path.read_bytes()), outcome=outcome))
        print(case['name'], 'passed', outcome['instructions'])
    (ROOT / 'phase126-old-resistance-emulations.json').write_text(json.dumps(dict(scope=__doc__, replays=rows), indent=2), encoding='utf8')


if __name__ == '__main__':
    main()
