"""Bounded native CPU audit of the Pro Variables writer Type77 (0x4D) branch.

Scope
-----
Only the positive branch of the shipped `DK2 Editor pro.exe` writer
`0x4937C0` (Type77 block 0x4941FD..0x494299) and the count helper `0x48CA00`
(Type77 block 0x48CF00..0x48CF35) is exercised.  The original fixture builder
`emulate_editor_variables_writer.build()` and the original runner
`native_editor_variables_writer_cpu.run()` are reused in memory.  No original
instruction is modified and no per-instruction replacement is introduced.
The inherited fixture already declares its own external adapters (CFile sink,
heap alloc/free, lstrlen, interlocked, critical-section and clock/TLS/error
IAT stubs); this audit adds only one declared caller that writes two globals
and both count-side and writer-side stack slots (the writer-side initialization
is overwritten before 0x49421D). Function-entry register initialisation and the
explicit CFile/vtable sink fixture belong to the baseline runner, not to a new
substitution.

No claim is made about the meaning of Type77 in a live editor, about the full
game/GUI, or about any global outside 0x54F9D0/0x54F9D4.  Globals 29/186 keep
their previous treatment (not assigned new semantics).
"""
import hashlib
import json
import struct
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent
RES = ROOT.parent
sys.path.insert(0, str(RES))
sys.path.insert(0, str(RES / 'dependencies/unicorn-2.1.4'))

import emulate_editor_variables_writer as ev          # builder (reused as-is)
import native_editor_variables_writer_cpu as cpu      # runner (reused as-is)

ENGINE = ev.defaults.engine
W = ENGINE.writer
MAP = ev.MAP
STACK = W.STACK
EXE = ENGINE.EXE
RAW = ENGINE.RAW
TAIL = (0x11223344, 0x55667788)

# Pinned expected source; the audit fails if the on-disk EXE differs.
EXE_SHA256 = '989c45df779bccfc9a9e509b2ad0224a9bd269629f74792adc0320fa4b99fb09'
HELPER_FILES = [
    'emulate_editor_variables_writer.py',
    'native_editor_variables_writer_cpu.py',
    'emulate_editor_global_variables.py',
    'emulate_room_model_chain.py',
    'probe_editor_global_defaults.py',
    'emulate_terrain_text_stat.py',
]

# Type77 fixture globals / blocks, all declared before the writer call.
CALLER = 0x17740000          # provider that initializes globals + both scratch slots
PTRS = 0x17781000            # pointer array DAT_0054F9D0 points at
ENTITY_BASE = 0x17782000     # entities carrying the +0x168 filter byte
ENTITY_STRIDE = 0x1000

# Exact released 8-byte local stack slots the accessor 0x48D720 returns.
COUNT_SCRATCH = STACK - 0x24C
WRITER_SCRATCH = STACK - 0x17C

# Observed-fixture golden values (NOT an independent oracle): the shipped
# writer reads these leftovers on iteration 0 for this fixture.  The value
# 0x00494193 is the return address pushed by CALL [EDX+0x38] at 0x494190; the
# key 0x00000294 is written by the sink stub before the Type77 loop.
WRITER_KEY = 0x00000294
WRITER_VALUE = 0x00494193

VISIT_PCS = (0x48CF07, 0x48CF24, 0x48CF29,
             0x494218, 0x494247, 0x494256, 0x49425A, 0x494289)

SCENES = [
    dict(
        name='zero-declared',
        description='Map +0x110 = 0: both Type77 loops are skipped entirely.',
        map_count=0,
        entities=[],
        count_key=WRITER_KEY,
        count_value=0xCAFEBABE,
        writer_records=[],
        expected_declared=196,
        expected_emitted=196,
        expected_divergence=False,
        expected_visits={0x48CF07: 0, 0x48CF24: 0, 0x48CF29: 0,
                         0x494218: 0, 0x494247: 0, 0x494256: 0, 0x49425A: 0, 0x494289: 0},
    ),
    dict(
        name='zero-vector',
        description='Map +0x110 = 1 but DAT_0054F9D4 = 0: loops run, filter has no entity.',
        map_count=1,
        entities=[],
        count_key=WRITER_KEY,
        count_value=0xCAFEBABE,
        writer_records=[],
        expected_declared=196,
        expected_emitted=196,
        expected_divergence=False,
        expected_visits={0x48CF07: 1, 0x48CF24: 0, 0x48CF29: 0,
                         0x494218: 1, 0x494247: 0, 0x494256: 0, 0x49425A: 0, 0x494289: 0},
    ),
    dict(
        name='one-accepted',
        description='Map +0x110 = 1, vector[0].+0x168 = 0x94, count scratch key 0x294.',
        map_count=1,
        entities=[0x94],
        count_key=WRITER_KEY,
        count_value=0xCAFEBABE,
        writer_records=[(WRITER_KEY, WRITER_VALUE)],
        expected_declared=197,
        expected_emitted=197,
        expected_divergence=False,
        expected_visits={0x48CF07: 1, 0x48CF24: 1, 0x48CF29: 1,
                         0x494218: 1, 0x494247: 1, 0x494256: 1, 0x49425A: 1, 0x494289: 1},
    ),
    dict(
        name='two-declared',
        description='Map +0x110 = 2: one Type77 record is observed; exact second-iteration '
                    'accessor reads are not captured in the published evidence.',
        map_count=2,
        entities=[0x94],
        count_key=WRITER_KEY,
        count_value=0xCAFEBABE,
        writer_records=[(WRITER_KEY, WRITER_VALUE)],
        expected_declared=197,
        expected_emitted=197,
        expected_divergence=False,
        expected_visits={0x48CF07: 2, 0x48CF24: 1, 0x48CF29: 1,
                         0x494218: 2, 0x494247: 2, 0x494256: 1, 0x49425A: 1, 0x494289: 1},
    ),
    dict(
        name='entity-mismatch',
        description='Vector[0].+0x168 = 0x00 != key low byte 0x94: both filters reject.',
        map_count=1,
        entities=[0x00],
        count_key=WRITER_KEY,
        count_value=0xCAFEBABE,
        writer_records=[],
        expected_declared=196,
        expected_emitted=196,
        expected_divergence=False,
        expected_visits={0x48CF07: 1, 0x48CF24: 0, 0x48CF29: 0,
                         0x494218: 1, 0x494247: 1, 0x494256: 0, 0x49425A: 0, 0x494289: 0},
    ),
    dict(
        name='count-bh20-boundary',
        description='Count scratch key 0x2094 (BH = 0x20) rejects at 0x48CF24/0x48CF27, '
                    'while the writer stack-slot key 0x294 (BH = 0x02) still emits: '
                    'declared count and emitted records diverge.',
        map_count=1,
        entities=[0x94],
        count_key=0x2094,
        count_value=0xCAFEBABE,
        writer_records=[(WRITER_KEY, WRITER_VALUE)],
        expected_declared=196,
        expected_emitted=197,
        expected_divergence=True,
        expected_visits={0x48CF07: 1, 0x48CF24: 1, 0x48CF29: 0,
                         0x494218: 1, 0x494247: 1, 0x494256: 1, 0x49425A: 1, 0x494289: 1},
    ),
    dict(
        name='count-bh1f-control',
        description='Count scratch key 0x1F94 (BH = 0x1F) passes the 0x20 boundary and '
                    'accepts, giving declared count 197.',
        map_count=1,
        entities=[0x94],
        count_key=0x1F94,
        count_value=0xCAFEBABE,
        writer_records=[(WRITER_KEY, WRITER_VALUE)],
        expected_declared=197,
        expected_emitted=197,
        expected_divergence=False,
        expected_visits={0x48CF07: 1, 0x48CF24: 1, 0x48CF29: 1,
                         0x494218: 1, 0x494247: 1, 0x494256: 1, 0x49425A: 1, 0x494289: 1},
    ),
]


def mov32(address, value):
    return bytes.fromhex('c705') + struct.pack('<I', address) + struct.pack('<I', value & 0xffffffff)


def build(scene):
    """Load the unmodified phase199 fixture, then attach explicit Type77 inputs."""
    case = dict(name='type77-' + scene['name'], TPS=4, seed=165, overrides={}, terrain={},
                tail=list(TAIL), extra=False, lists=[], error=None)
    fixture = ev.build(case)
    mem = {int(row['address'], 16): bytearray.fromhex(row['hex']) for row in fixture['memory']}

    # Declared Type77 vector length (the only Map field the selected Type77
    # paths read; not a statement about the whole build).
    struct.pack_into('<I', mem[MAP], 0x110, scene['map_count'])

    entities = scene['entities']
    ptr_words = [ENTITY_BASE + index * ENTITY_STRIDE for index in range(len(entities))]
    mem[PTRS] = bytearray(struct.pack('<%dI' % max(1, len(ptr_words)),
                                      *(ptr_words or [0])))
    for index, byte in enumerate(entities):
        entity = bytearray(0x200)
        entity[0x168] = byte & 0xff
        mem[ENTITY_BASE + index * ENTITY_STRIDE] = entity

    code = b''.join([
        mov32(0x54F9D0, PTRS),
        mov32(0x54F9D4, len(entities)),
        mov32(COUNT_SCRATCH, scene['count_key']),
        mov32(COUNT_SCRATCH + 4, scene['count_value']),
        # Writer slot is written too, but the writer's own earlier sink/RNG calls
        # overwrite it before 0x49421D; the audit records that observed residue.
        mov32(WRITER_SCRATCH, scene['count_key']),
        mov32(WRITER_SCRATCH + 4, scene['count_value']),
    ]) + b'\xc3'
    mem[CALLER] = bytearray(code)
    fixture['providers'] = fixture['providers'] + [[CALLER, len(code)]]

    calls = list(fixture['calls'])
    calls.insert(len(calls) - 1, dict(address=CALLER, ecx=0, stack=[]))
    fixture['calls'] = calls

    fixture['memory'] = [ENGINE.region(a, b) for a, b in sorted(mem.items())]
    spans = sorted((a, len(b)) for a, b in mem.items())
    assert all(a + n <= b for (a, n), (b, _) in zip(spans, spans[1:])), 'region overlap'
    return fixture


def signed(value):
    return (value & 0x7fffffff) - (value & 0x80000000)


def idiv(a, b):
    return (abs(a) // abs(b)) * (-1 if (a < 0) != (b < 0) else 1)


def expected_records(scene, raw_values, raw_terrain):
    ids, _, _ = ev.defaults.static_defaults()
    scaled_types = {67, 155, 158, 185, 220, 221}
    tails = TAIL
    scalars = []
    for typ in ids:
        value = struct.unpack_from('<d', raw_values, typ * 8)[0]
        scaled = int(value * (4096 if typ in scaled_types else 1)) & 0xffffffff
        scalars.append((typ, scaled, tails[0], tails[1]))

    gold = struct.unpack_from('<H', raw_terrain, 0x19A)[0]
    max_health = struct.unpack_from('<H', raw_terrain, 0x1FF)[0]
    type7 = int(struct.unpack_from('<d', raw_values, 7 * 8)[0])
    product = signed((abs(type7) & 0xffffffff) * gold)
    quotient = idiv(idiv(product, max_health), 4) & 0xffffffff
    derived74 = (74, quotient, 0, 0)

    type77 = [(77, key & 0xff, (key >> 8) & 0xff, value & 0xffffffff)
              for key, value in scene['writer_records']]
    return scalars + [derived74] + type77 + [(241, 5120, 0, 0), (242, 0x11223344, 0, 0)]


def parse_records(out):
    records = []
    for index in range(4096):
        record = struct.unpack_from('<4I', out, 56 + 16 * index)
        records.append(record)
        if record[0] == 242:
            return records
    raise AssertionError('Type242 terminator not found')


def run_scene(scene):
    fixture = build(scene)
    fixture_digest = hashlib.sha256(json.dumps(fixture, sort_keys=True).encode()).hexdigest()
    result = cpu.run(fixture)

    post = {int(row['address'], 16): bytes.fromhex(row['hex']) for row in result['output']}
    pre = {int(row['address'], 16): bytes.fromhex(row['hex']) for row in fixture['memory']}
    out = post[W.OUTPUT]

    raw_values = pre[ev.VALUES]
    raw_terrain = pre[RAW]
    expected = expected_records(scene, raw_values, raw_terrain)
    expected_bytes = b''.join(struct.pack('<4I', *record) for record in expected)
    actual = parse_records(out)
    actual_bytes = b''.join(struct.pack('<4I', *record) for record in actual)

    declared = struct.unpack_from('<I', out, 20)[0]
    size = struct.unpack_from('<I', out, 8)[0]
    record_bytes = struct.unpack_from('<I', out, 52)[0]
    header_words = struct.unpack('<14I', out[:56])
    expected_header = {0: 0xE6, 1: 0x4, 2: 56 + 16 * scene['expected_declared'],
                       3: 0xE7, 4: 0x1C, 5: scene['expected_declared'], 6: 0x1,
                       12: 0xE8, 13: 16 * scene['expected_declared']}
    header_indices = (0, 1, 2, 3, 4, 5, 6, 12, 13)

    visits = result['visited_instructions']
    visits = {pc: visits.get(hex(pc), 0) for pc in VISIT_PCS}
    meta_words = struct.unpack('<3I', post[W.META])

    checks = {
        'success': bool(result['success']),
        'no_error': result['error'] is None,
        'no_failures': not result['failures'],
        'no_code_substitutions': result['code_substitutions'] == 0,
        'exe_sha256_pinned': (result['source_exe_sha256'] == EXE_SHA256
                              and hashlib.sha256(EXE.read_bytes()).hexdigest() == EXE_SHA256),
        'all_calls_returned': (len(result['calls']) == len(fixture['calls'])
                               and all(c['returned'] for c in result['calls'])),
        'writer_return_low_byte_1': (int(result['registers']['EAX'], 16) & 0xff) == 1,
        'header_declared_count': declared == scene['expected_declared'],
        'header_size': size == 56 + 16 * scene['expected_declared'],
        'header_record_bytes_field': record_bytes == 16 * scene['expected_declared'],
        'header_defined_fields': all(header_words[i] == expected_header[i] for i in header_indices),
        'emitted_record_count': len(actual) == scene['expected_emitted'],
        'records_match_explicit': actual == expected,
        'records_sha256_match': (hashlib.sha256(actual_bytes).hexdigest()
                                 == hashlib.sha256(expected_bytes).hexdigest()),
        'bytes_match_explicit': out[56:56 + len(expected_bytes)] == expected_bytes,
        'unwritten_bytes_are_A5': (out[56 + len(expected_bytes):]
                                   == bytes([0xA5]) * (len(out) - 56 - len(expected_bytes))),
        'visited_pcs': visits == scene['expected_visits'],
        'meta_emitted_count': meta_words[0] == 3 + scene['expected_emitted'],
        'meta_end_offset': meta_words[1] == W.OUTPUT + 56 + 16 * scene['expected_emitted'],
        'meta_terminator': meta_words[2] == 0,
        'map_values_preserved': post[MAP] == pre[MAP] and post[ev.VALUES] == pre[ev.VALUES],
    }
    passed = all(value for key, value in checks.items() if key != 'visited_pcs') and checks['visited_pcs']

    return dict(
        name=scene['name'],
        description=scene['description'],
        input=dict(
            fixture_digest=fixture_digest,
            map_count=scene['map_count'],
            vector_entities=[hex(byte) for byte in scene['entities']],
            count_scratch_address=hex(COUNT_SCRATCH),
            count_scratch_key=hex(scene['count_key']),
            count_scratch_value=hex(scene['count_value']),
            writer_scratch_address=hex(WRITER_SCRATCH),
            writer_observed_key=hex(WRITER_KEY),
            writer_observed_value=hex(WRITER_VALUE),
            globals=dict(DAT_0054F9D0=hex(PTRS), DAT_0054F9D4=len(scene['entities'])),
        ),
        observed=dict(
            header=dict(declared_count=declared, size=size, record_bytes_field=record_bytes,
                        defined_words={str(i): hex(header_words[i]) for i in header_indices},
                        unasserted_stack_residue_28_47=out[28:48].hex()),
            emitted_count=len(actual),
            type77_records=[list(record) for record in actual if record[0] == 77],
            records=[list(record) for record in actual],
            records_sha256=hashlib.sha256(actual_bytes).hexdigest(),
            visited_pcs={hex(pc): count for pc, count in visits.items()},
            meta=post[W.META].hex(),
            meta_defined=dict(count=meta_words[0], end_offset=hex(meta_words[1]),
                              terminator=meta_words[2]),
            instruction_count=result['instruction_count'],
        ),
        expected=dict(
            header=dict(declared_count=scene['expected_declared'],
                        size=56 + 16 * scene['expected_declared'],
                        record_bytes_field=16 * scene['expected_declared'],
                        defined_words={str(i): hex(expected_header[i]) for i in header_indices}),
            emitted_count=scene['expected_emitted'],
            type77_records=[[77, key & 0xff, (key >> 8) & 0xff, value & 0xffffffff]
                            for key, value in scene['writer_records']],
            type77_source='observed-fixture golden values (not an independent oracle); '
                          '0x00494193 is the return address after CALL [EDX+0x38] at '
                          '0x494190 and 0x00000294 is written by the sink stub',
            visited_pcs={hex(pc): count for pc, count in scene['expected_visits'].items()},
            meta_defined=dict(count=3 + scene['expected_emitted'],
                              end_offset=hex(W.OUTPUT + 56 + 16 * scene['expected_emitted']),
                              terminator=0),
            bytes_sha256=hashlib.sha256(expected_bytes).hexdigest(),
            records_sha256=hashlib.sha256(expected_bytes).hexdigest(),
            divergent_header=scene['expected_divergence'],
        ),
        checks=checks,
        verdict='PASS' if passed else 'FAIL',
    )


def helper_hashes():
    out = {}
    for name in HELPER_FILES:
        path = RES / name
        out[name] = hashlib.sha256(path.read_bytes()).hexdigest() if path.exists() else None
    return out


def audit(write=True, report_dir=None):
    """Run all scenes and return the results document (also optionally writes it)."""
    source_sha = hashlib.sha256(EXE.read_bytes()).hexdigest()
    rows = []
    for scene in SCENES:
        row = run_scene(scene)
        rows.append(row)
        print('%-22s %-4s declared=%s emitted=%s type77=%s' % (
            row['name'], row['verdict'],
            row['observed']['header']['declared_count'],
            row['observed']['emitted_count'],
            row['observed']['type77_records']), flush=True)

    passed = sum(1 for row in rows if row['verdict'] == 'PASS')
    helpers = helper_hashes()
    scenes_doc = dict(
        scope=__doc__.strip().splitlines()[0],
        source=dict(path=str(EXE), pinned_sha256=EXE_SHA256,
                    observed_sha256=source_sha, sha256_matches=source_sha == EXE_SHA256),
        helper_sha256=helpers,
        builder='emulate_editor_variables_writer.build',
        runner='native_editor_variables_writer_cpu.run',
        memory_anchors=dict(MAP=hex(MAP), VALUES=hex(ev.VALUES), STACK=hex(STACK),
                            COUNT_SCRATCH=hex(COUNT_SCRATCH), WRITER_SCRATCH=hex(WRITER_SCRATCH),
                            DAT_0054F9D0=hex(PTRS), entity_base=hex(ENTITY_BASE)),
        scenes=[dict(name=s['name'], description=s['description'], map_count=s['map_count'],
                     vector_entities=[hex(b) for b in s['entities']],
                     count_scratch_key=hex(s['count_key']),
                     expected_declared=s['expected_declared'],
                     expected_emitted=s['expected_emitted'],
                     expected_divergence=s['expected_divergence'],
                     expected_type77=[[77, key & 0xff, (key >> 8) & 0xff, value & 0xffffffff]
                                      for key, value in s['writer_records']],
                     expected_type77_source='observed-fixture golden values')
                for s in SCENES],
    )
    results_doc = dict(
        scope=__doc__.strip().splitlines()[0],
        source=dict(path=str(EXE), pinned_sha256=EXE_SHA256,
                    observed_sha256=source_sha, sha256_matches=source_sha == EXE_SHA256),
        helper_sha256=helpers,
        runner_constraints=dict(
            code_substitutions=0,
            original_instructions_unmodified=True,
            per_instruction_replacement=False,
            added_provider='one declared external caller: globals 0x54F9D0/0x54F9D4 '
                           'and both stack slots (count and writer); the writer-side '
                           'write is overwritten before 0x49421D',
            inherited_fixture_adapters='baseline runner already declares CFile sink, '
                                       'heap alloc/free, lstrlen, interlocked, '
                                       'critical-section and clock/TLS/error IAT stubs',
            register_initialisation='function-entry register setup only (baseline runner)',
            writes='own folder only; EXE/KWD read-only',
        ),
        summary=dict(scenes=len(rows), passed=passed, failed=len(rows) - passed,
                     blocked=0 if passed == len(rows) else len(rows) - passed),
        ok=passed == len(rows),
        scenes=rows,
    )
    if write:
        target = Path(report_dir) if report_dir else ROOT
        target.mkdir(parents=True, exist_ok=True)
        (target / 'type77-scenes.json').write_text(
            json.dumps(scenes_doc, ensure_ascii=False, indent=2) + '\n', encoding='utf8')
        (target / 'type77-results.json').write_text(
            json.dumps(results_doc, ensure_ascii=False, indent=2) + '\n', encoding='utf8')
    print('summary', results_doc['summary'])
    print('source_sha256', source_sha, 'pinned', EXE_SHA256,
          'match', source_sha == EXE_SHA256)
    return results_doc


def main(argv=None):
    import argparse
    parser = argparse.ArgumentParser(description='Audit Type77 writer/count branches.')
    parser.add_argument('--no-write', action='store_true',
                        help='run and print results without overwriting reports')
    parser.add_argument('--report-dir', default=None,
                        help='write type77-scenes.json/type77-results.json here')
    args = parser.parse_args(argv)
    results = audit(write=not args.no_write, report_dir=args.report_dir)
    return 0 if results['ok'] else 1


if __name__ == '__main__':
    sys.exit(main())
