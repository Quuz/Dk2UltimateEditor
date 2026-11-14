"""Bounded native CPU audit: zero-value skip and cross-player order in the
first player-scoped Variables union branch.

Target
------
Shipped ``DK2 Editor pro.exe`` (pinned SHA-256 below), original instructions
only: writer ``0x4937C0``, first player branch ``FUN_4943C0`` (called from
``0x49386D``), total-count pass ``FUN_48CA00`` (called from ``0x4937D0``), its
player count helper ``FUN_48CF50`` (called from ``0x48CA36``) and the player
accessor ``FUN_488830`` (called from ``0x4943E8``, ``0x48CA25``, ``0x48CA6F``).

The accepted parent audit (``../variables-player-writer/audit_player_writer.py``)
proved one accepted player, entry order inside one player and the strict
``2 < player[+0x1C]`` gate.  It explicitly left open:

* the writer/count-helper skip of a ``+0x4C`` entry whose value DWORD is zero,
* player-array index > 0, i.e. cross-player order and Map ``+0xA64``/``+0xA68``
  with more than one pointer.

This audit covers only those two gaps with four new data-only fixtures.  The
fixed/player pool groups (types ``1 + (kind << 16)`` .. ``5 + ...``) stay with
zero local and global lists and are NOT interpreted.  Type77 count at Map
``+0x110`` stays 0.  Types 29/113/114/186, SDT and Crypt are untouched.

Fixtures
--------
``emulate_editor_variables_writer.build()`` and
``native_editor_variables_writer_cpu.run()`` are reused unchanged (original
instructions, no patches, no register/vtable/getter replacement, no
new instruction hooks).  The published Map/Terrain/stack/CFile/heap scaffolding is
inherited; this audit adds only data:

    Map +0xA64 / +0xA68    explicit pointer array / player count
    0x177A0000 + i*0x1000  player object i (zeros except +0x1C and +0x4C)
    0x177B0000 + i*0x1000  player i's 8-byte {key, value} entry array

Every player and every entry array has its own memory, so two players cannot
alias.  Expected records are derived only from the declared inputs; the
inherited scalar record tails are the writer-entry stack scratch observed in
the reused runner and are reported as reproduction, not as a new semantic
claim.
"""
import hashlib
import json
import struct
import sys
import traceback
from pathlib import Path

ROOT = Path(__file__).resolve().parent
RES = ROOT.parent
sys.path.insert(0, str(RES))
sys.path.insert(0, str(RES / 'dependencies/unicorn-2.1.4'))
sys.path.insert(0, str(RES / 'variables-player-writer'))

import emulate_editor_variables_writer as ev          # fixture builder, unchanged
import native_editor_variables_writer_cpu as cpu      # original CPU runner, unchanged
import audit_player_writer as parent                  # accepted parent audit
from build_effect_semantics import pe_read

ENGINE = ev.defaults.engine
W = ENGINE.writer
MAP = ev.MAP
VALUES = ev.VALUES
EXE = ENGINE.EXE
EXE_SHA256 = parent.EXE_SHA256

PLAYER_ARRAY = 0x17790000
PLAYER_BASE = 0x177A0000
ENTRIES_BASE = 0x177B0000
PLAYER_STRIDE = 0x1000
PLAYER_SIZE = 0x200

HELPER_FILES = [
    'emulate_editor_variables_writer.py',
    'native_editor_variables_writer_cpu.py',
    'emulate_editor_global_variables.py',
    'emulate_room_model_chain.py',
    'probe_editor_global_defaults.py',
    'emulate_terrain_text_stat.py',
    'emulate_room_writer.py',
    'build_effect_semantics.py',
    'emulate_element_colors.py',
    'audit_targeted_spell.py',
    'variables-player-writer/audit_player_writer.py',
]

# Static PC roles.  Addresses only; the instruction text is disassembled from
# the pinned EXE in audit(), so a wrong PC would show up in the report.
PC_ROLES = [
    (0x4937D0, 'CALL 0x48CA00 total count pass'),
    (0x49386D, 'CALL 0x4943C0 first player branch'),
    (0x49389D, 'other player loop in 0x4937C0: CALL 0x488830 (not interpreted)'),
    (0x4943C0, 'FUN_4943C0 entry'),
    (0x4943E5, 'PUSH EBP: per-player loop head (index push)'),
    (0x4943E8, 'CALL 0x488830: player pointer lookup'),
    (0x4943F4, 'JB 0x494443: strict kind >= 3 gate'),
    (0x494414, 'JZ 0x494437: zero-value skip in writer'),
    (0x494434, 'CALL [EDX+0x38]: emit one 16-byte record'),
    (0x494437, 'entry loop tail (both zero and non-zero paths)'),
    (0x49443A, 'INC EDI: next entry index'),
    (0x49443D, 'JL 0x494408: entry loop'),
    (0x494449, 'INC EBP: next player index'),
    (0x49444C, 'JC 0x4943E5: player loop'),
    (0x488843, 'accessor valid-index path'),
    (0x488849, 'accessor: load pointer array element'),
    (0x48884C, 'accessor: return element'),
    (0x48883E, 'accessor out-of-range path (must stay 0 here)'),
    (0x48CA25, 'count pass: CALL 0x488830'),
    (0x48CA36, 'CALL 0x48CF50 player count helper'),
    (0x48CA43, 'count pass: INC ESI next player index'),
    (0x48CA6F, 'total-count function, second player loop: CALL 0x488830'),
    (0x48CA77, 'total-count function, second player loop: store player pointer'),
    (0x48CF50, 'FUN_48CF50 entry'),
    (0x48CF6F, 'JE 0x48CF72: zero-value skip in count helper'),
    (0x48CF71, 'INC EAX: count one non-zero value'),
]

SCENES = [
    dict(
        name='order-zero-value-skip',
        description='One accepted player (kind 3) with entries '
                    '{0x40, 0x0BADF00D}, {0x41, 0x00000000}, {0x42, 0xFEEDFACE}: '
                    'the zero-valued middle entry is skipped by both the writer '
                    'and the count helper, the two non-zero entries keep their '
                    'array order.',
        players=[dict(kind=3, entries=[(0x40, 0x0BADF00D),
                                       (0x41, 0x00000000),
                                       (0x42, 0xFEEDFACE)])],
        pointers=[0],
    ),
    dict(
        name='order-two-players-forward',
        description='Two accepted players, pointer array [P0(kind 3, 0x11/'
                    '0xAABBCCDD), P1(kind 4, 0x22/0x01020304)]: Map +0xA64/+0xA68 '
                    'index 1 is used and records follow the pointer array.',
        players=[dict(kind=3, entries=[(0x11, 0xAABBCCDD)]),
                 dict(kind=4, entries=[(0x22, 0x01020304)])],
        pointers=[0, 1],
    ),
    dict(
        name='order-two-players-reversed',
        description='Same two player objects, pointer array reversed [P1, P0]: '
                    'the emitted order follows the pointer array, not object '
                    'address order.',
        players=[dict(kind=3, entries=[(0x11, 0xAABBCCDD)]),
                 dict(kind=4, entries=[(0x22, 0x01020304)])],
        pointers=[1, 0],
    ),
    dict(
        name='order-kind2-then-kind3',
        description='Pointer array [P0(kind 2, 0x51/0x11111111), P1(kind 3, '
                    '0x52/0x22222222 and 0x53/0)]: the unselected player emits '
                    'and counts nothing and its entries are not visited, while '
                    'traversal continues to the next player.',
        players=[dict(kind=2, entries=[(0x51, 0x11111111)]),
                 dict(kind=3, entries=[(0x52, 0x22222222), (0x53, 0x00000000)])],
        pointers=[0, 1],
    ),
]


def sha(blob):
    return hashlib.sha256(bytes(blob)).hexdigest()


def hx(address):
    return f'0x{address:X}'


def scene_fixture(scene):
    """Reuse the published builder; add only explicit player/entry data."""
    c = dict(name=scene['name'], TPS=4, seed=165, overrides={}, terrain={},
             tail=None, extra=False, lists=[], error=None)
    f = ev.build(c)
    mem = {int(row['address'], 16): bytearray.fromhex(row['hex'])
           for row in f['memory']}
    players = scene['players']
    pointers = scene['pointers']
    assert sorted(pointers) == list(range(len(players))), pointers
    struct.pack_into('<2I', mem[MAP], 0xA64,
                     PLAYER_ARRAY if pointers else 0, len(pointers))
    mem[PLAYER_ARRAY] = bytearray(b''.join(
        ev.p32(PLAYER_BASE + index * PLAYER_STRIDE) for index in pointers))
    for index, player in enumerate(players):
        obj = bytearray(PLAYER_SIZE)                   # all other fields zero
        obj[0x1C] = player['kind'] & 0xFF
        entries = player['entries']
        if entries:
            struct.pack_into('<2I', obj, 0x50,
                             ENTRIES_BASE + index * PLAYER_STRIDE, len(entries))
            mem[ENTRIES_BASE + index * PLAYER_STRIDE] = bytearray(b''.join(
                struct.pack('<2I', key, value) for key, value in entries))
        mem[PLAYER_BASE + index * PLAYER_STRIDE] = obj
    f['memory'] = [ev.region(a, bytes(b)) for a, b in sorted(mem.items())]
    spans = sorted((a, len(b)) for a, b in mem.items())
    assert all(a + n <= b for (a, n), (b, _) in zip(spans, spans[1:])), spans
    return f


def selected_players(scene):
    """Players reached by the branch, in pointer-array order."""
    return [scene['players'][index] for index in scene['pointers']
            if scene['players'][index]['kind'] >= 3]


def independent_rows(scene):
    """Derived only from declared inputs and declared pointer order."""
    rows = []
    for player in selected_players(scene):
        for key, value in player['entries']:
            if value & 0xFFFFFFFF:
                rows.append((1, key & 0xFF, value & 0xFFFFFFFF,
                             player['kind'] & 0xFF))
    return rows


def pc_expectations(scene):
    players = scene['players']
    selected = selected_players(scene)
    n_players = len(scene['pointers'])
    total = sum(len(p['entries']) for p in selected)
    nonzero = sum(1 for p in selected for _, value in p['entries']
                  if value & 0xFFFFFFFF)
    required = {
        '0x4937d0': 1,
        '0x49386d': 1,
        '0x49389d': n_players,
        '0x4943c0': 1,
        '0x4943e5': n_players,
        '0x4943e8': n_players,
        '0x4943f4': n_players,
        '0x494414': total,
        '0x494434': nonzero,
        '0x494437': total,
        '0x49443a': total,
        '0x49443d': total,
        '0x494449': n_players,
        '0x49444a': n_players,
        '0x49444c': n_players,
        '0x48883e': 0,
        '0x48ca25': n_players,
        '0x48ca36': len(selected),
        '0x48ca43': n_players,
        '0x48ca6f': n_players,
        '0x48ca77': n_players,
        '0x48cf50': len(selected),
        '0x48cf6f': total,
        '0x48cf71': nonzero,
        '0x4943cc': 0,
    }
    return required, dict(players=n_players, selected=len(selected),
                          entries_visited=total, entries_nonzero=nonzero,
                          entries_zero_skipped=total - nonzero)


def verify(scene, fixture, result):
    assert result['source_exe_sha256'] == EXE_SHA256, result['source_exe_sha256']
    assert result['code_substitutions'] == 0
    assert not result['failures'], result['failures']
    assert result['success'] and result['error'] is None, result['error']
    assert len(result['calls']) == len(fixture['calls'])
    assert all(row['returned'] for row in result['calls'])

    inputs = {int(row['address'], 16): bytes.fromhex(row['hex'])
              for row in fixture['memory']}
    post = {int(row['address'], 16): bytes.fromhex(row['hex'])
            for row in result['output']}
    assert post[MAP] == inputs[MAP]
    assert post[VALUES] == inputs[VALUES]
    assert post[ENGINE.RAW] == inputs[ENGINE.RAW]
    assert post[ENGINE.MID][-64:] == bytes(64)
    assert post[0] == inputs[0] == bytes(4096)

    expected_players = independent_rows(scene)
    expected_inherited, tail0, tail1 = parent.inherited_fixed_records(
        fixture, result)
    expected = expected_players + expected_inherited

    out = post[W.OUTPUT]
    signature, version, size = struct.unpack_from('<3I', out, 0)
    assert (signature, version) == (0xE6, 4), (signature, version)
    assert struct.unpack_from('<I', out, 12)[0] == 0xE7
    assert struct.unpack_from('<I', out, 48)[0] == 0xE8
    declared = struct.unpack_from('<I', out, 20)[0]
    assert struct.unpack_from('<I', out, 52)[0] == 16 * declared
    assert declared == len(expected), (declared, len(expected))
    assert size == 56 + 16 * declared, (size, declared)
    records = [struct.unpack_from('<4I', out, 56 + 16 * i)
               for i in range(declared)]
    assert records[:len(expected_players)] == expected_players, \
        (records[:len(expected_players)], expected_players)
    assert records[len(expected_players):] == expected_inherited, \
        [(a, b) for a, b in zip(records[len(expected_players):],
                                expected_inherited) if a != b][:4]
    assert out[size:] == bytes([0xA5]) * (len(out) - size)
    assert struct.unpack('<3I', post[W.META]) == (3 + declared,
                                                  W.OUTPUT + size, 0)

    # Declared player data must stay untouched; only OUTPUT/META change.
    assert post[PLAYER_ARRAY] == inputs[PLAYER_ARRAY]
    for index, player in enumerate(scene['players']):
        assert post[PLAYER_BASE + index * PLAYER_STRIDE] == \
            inputs[PLAYER_BASE + index * PLAYER_STRIDE]
        if player['entries']:
            address = ENTRIES_BASE + index * PLAYER_STRIDE
            assert post[address] == inputs[address]

    required, counts = pc_expectations(scene)
    visited = result['visited_instructions']
    observed = {pc: visited.get(pc.lower(), 0) for pc in required}
    assert observed == required, (observed, required)
    # The accessor is entered from four distinct original call sites, each
    # once per player: 0x49389D (another, uninterpreted player loop of
    # 0x4937C0), 0x4943E8 (first player branch), 0x48CA25 (count pass) and
    # 0x48CA6F (write pass).  Its valid-index path must be reached every time
    # and its out-of-range path never.
    n_players = len(scene['pointers'])
    getter = {pc: visited.get(pc.lower(), 0)
              for pc in ('0x488843', '0x488849', '0x48884c', '0x48883e')}
    assert getter['0x488843'] == getter['0x488849'] == getter['0x48884c'] \
        == 4 * n_players, getter

    header = out[:56]
    return dict(
        outcome='PASS',
        declared_records=declared,
        emitted_records=declared,
        independent_player_records=[list(row) for row in expected_players],
        observed_player_records=[list(row) for row in
                                 records[:len(expected_players)]],
        independent_player_records_hex=[struct.pack('<4I', *row).hex()
                                        for row in expected_players],
        observed_player_records_hex=[struct.pack('<4I', *row).hex()
                                     for row in records[:len(expected_players)]],
        player_record_count=len(expected_players),
        inherited_records=len(expected_inherited),
        inherited_tail_reproduction=[tail0, tail1],
        output_bytes=size,
        output_tail_is_A5=(out[size:] == bytes([0xA5]) * (len(out) - size)),
        output_tail_bytes=len(out) - size,
        header_hex=header.hex(),
        header_sha256=sha(header),
        header_fields=dict(signature=hex(signature), version=version,
                           size_at_8=size, tag1_at_12=hex(
                               struct.unpack_from('<I', out, 12)[0]),
                           count_at_20=declared, tag2_at_48=hex(
                               struct.unpack_from('<I', out, 48)[0]),
                           record_bytes_at_52=16 * declared),
        meta=list(struct.unpack_from('<3I', post[W.META], 0)),
        pc_visits=observed,
        pc_counts=counts,
        accessor_path_visits=getter,
        accessor_evidence=dict(
            call_sites_per_player={pc: visited.get(pc.lower(), 0) for pc in
                                   ('0x49389d', '0x4943e8', '0x48ca25',
                                    '0x48ca6f')},
            valid_index_path_total=getter['0x488843'],
            out_of_range_path=getter['0x48883e'],
            pointer_array_order=scene['pointers'],
            note='The unchanged runner exposes PC visit counts, not the '
                 'accessor argument; index 1 is supported by four accessor '
                 'entries per player into the valid path, the player-loop '
                 'iteration count and the unique kind/key/value of the second '
                 'pointer, plus the reversed-pointer control scene.',
        ),
        type77_count=struct.unpack_from('<I', inputs[MAP], 0x110)[0],
        native_steps=result['instruction_count'],
    )


def audit(write=False):
    assert sha(EXE.read_bytes()) == EXE_SHA256, 'pinned Pro EXE hash mismatch'
    blob = EXE.read_bytes()
    from capstone import Cs, CS_ARCH_X86, CS_MODE_32
    md = Cs(CS_ARCH_X86, CS_MODE_32)
    pc_semantics = []
    for pc, role in PC_ROLES:
        ins = next(md.disasm(pe_read(blob, pc, 15), pc))
        pc_semantics.append(dict(pc=hx(pc), static=f'{ins.mnemonic} {ins.op_str}',
                                 role=role))
    source = dict(
        exe=str(EXE),
        exe_sha256=EXE_SHA256,
        branch=dict(address='0x4943C0', end='0x494464',
                    bytes_hex=pe_read(blob, 0x4943C0, 0xA4).hex(),
                    bytes_sha256=sha(pe_read(blob, 0x4943C0, 0xA4))),
        count_pass=dict(address='0x48CA00', end='0x48CEE6', scope='prefix only; full execution also reaches later count code',
                        bytes_sha256=sha(pe_read(blob, 0x48CA00, 0x4E6))),
        count_helper=dict(address='0x48CF50', end='0x48CF7F',
                          bytes_hex=pe_read(blob, 0x48CF50, 0x2F).hex(),
                          bytes_sha256=sha(pe_read(blob, 0x48CF50, 0x2F))),
        player_accessor=dict(address='0x488830', end='0x48884F',
                             bytes_hex=pe_read(blob, 0x488830, 0x1F).hex(),
                             bytes_sha256=sha(pe_read(blob, 0x488830, 0x1F))),
    )
    helpers = {name: sha((RES / name).read_bytes()) for name in HELPER_FILES}
    rows = []
    ok = True
    for scene in SCENES:
        result = None
        try:
            fixture = scene_fixture(scene)
            result = cpu.run(fixture)
            outcome = verify(scene, fixture, result)
            rows.append(dict(name=scene['name'], description=scene['description'],
                             pointers=scene['pointers'],
                             fixture_sha256=sha(json.dumps(fixture,
                                                          sort_keys=True).encode()),
                             **outcome))
        except Exception as exc:  # keep a reproducible failing checkpoint
            ok = False
            row = dict(name=scene['name'], description=scene['description'],
                       outcome='FAIL', error=f'{type(exc).__name__}: {exc}',
                       traceback=traceback.format_exc())
            if result is not None:
                post = {int(r['address'], 16): bytes.fromhex(r['hex'])
                        for r in result['output']}
                if W.OUTPUT in post:
                    out = post[W.OUTPUT]
                    row['observed_header_hex'] = out[:56].hex()
                    row['observed_output_prefix_hex'] = out[:256].hex()
                    row['observed_source_exe_sha256'] = \
                        result.get('source_exe_sha256')
            rows.append(row)
    report = dict(
        scope=__doc__,
        overall='PASS' if ok else 'FAIL',
        source=source,
        helper_sha256=helpers,
        pc_semantics=pc_semantics,
        fixture_contract=dict(
            inherited='build()/run() reused unchanged; Map 0x17700000, 248 '
                      'doubles 0x17710000, Terrain #6 raw, 0xA5 stack, CFile '
                      'sink, HeapAlloc/HeapFree, lstrlen, interlocked, '
                      'critical-section, clock/TLS/error IAT adapters and the '
                      'Terrain/default-initializer calls come from the '
                      'published fixture',
            player_array=dict(address=hx(PLAYER_ARRAY),
                              layout='DWORD player pointers, order declared '
                                     'per scene'),
            player_object=dict(address=f'{hx(PLAYER_BASE)} + i*0x1000',
                               size=PLAYER_SIZE,
                               fields='+0x1C kind byte; +0x4C container '
                                      '{+0x50 data, +0x54 count}; every other '
                                      'pool/availability field zero'),
            entries=dict(address=f'{hx(ENTRIES_BASE)} + i*0x1000',
                         layout='8-byte {DWORD key, DWORD value} per player'),
            map_fields='Map +0xA64 pointer array, +0xA68 count; +0x110 Type77 '
                       'count remains 0; no local/global pool lists',
            registers='harness default per call: EAX/EBX/ECX/EDX/ESI/EDI = 0 '
                      'except the declared ECX; ESP=EBP=0x7FFF0000; EFLAGS=2; '
                      'FPCW=0x37F; return stub at 0x16000000',
        ),
        limitations=[
            'Only FUN_4943C0 / FUN_48CF50 / FUN_488830 inside FUN_4937C0 and '
            'FUN_48CA00 are newly exercised; pool/availability groups '
            '1+(kind<<16)..5+... are NOT executed or interpreted.',
            'No GUI reachability, full save, gameplay consumer or real-file '
            'write is claimed; the CFile sink is the published adapter.',
            'Type77 count 110 is zero; Types 29/113/114/186 keep prior '
            'treatment; SDT/Crypt untouched.',
            'Player-array index 1 itself is not read from a register: the '
            'unchanged runner exposes PC visit counts and output memory only, '
            'so index 1 is evidenced by two player-loop iterations, the '
            'unique kind/key/value of the second player and the reversed-'
            'pointer control scene, not by an argument-value trace.',
            'Scalar record tails +8..+15 are inherited stack-scratch '
            'reproduction (observed writer-entry seed), not a new meaning.',
            'Header time/date/ID fields produced by the original clock/text '
            'calls are inherited, not re-derived; only signature/version/size/'
            'count/tag fields are checked.',
            'Kind 2 is exercised only as a skipped player; no other kind '
            'values or true pool-content paths are tested.',
        ],
        scenes=rows,
    )
    if write:
        (ROOT / 'player-order-results.json').write_text(
            json.dumps(report, ensure_ascii=False, indent=2) + '\n',
            encoding='utf8')
    return report


def main(argv):
    write = '--no-write' not in argv
    report = audit(write=write)
    keep = ('name', 'outcome', 'pointers', 'declared_records',
            'emitted_records', 'player_record_count',
            'independent_player_records', 'observed_player_records',
            'independent_player_records_hex', 'observed_player_records_hex',
            'pc_counts', 'pc_visits', 'accessor_path_visits', 'output_bytes',
            'header_hex', 'meta', 'error')
    compact = dict(overall=report['overall'],
                   zero_skip_pc_counts={
                       row['name']: {k: row['pc_counts'][k] for k in
                                     ('entries_visited', 'entries_zero_skipped',
                                      'entries_nonzero')}
                       for row in report['scenes'] if 'pc_counts' in row},
                   scenes=[{k: v for k, v in row.items() if k in keep}
                           for row in report['scenes']])
    print(json.dumps(compact, ensure_ascii=False, indent=2))
    return 0 if report['overall'] == 'PASS' else 1


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
