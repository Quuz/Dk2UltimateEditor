"""Bounded native CPU audit of the FIRST player-scoped Variables union branch.

Target
------
Shipped ``DK2 Editor pro.exe`` writer ``0x4937C0``, first player branch
``FUN_4943C0`` (entry ``0x4943C0``, called from ``0x49386D``) and the matching
count helper ``FUN_48CF50`` called from ``FUN_48CA00`` at ``0x48CA36``.

The writer stores, for every selected player (player ``+0x1C`` byte >= 3), one
16-byte record for each entry of the player ``+0x4C`` container whose value
DWORD is non-zero:

    +0  DWORD 1                    (constant emitted by 0x4943CD)
    +4  DWORD zero-extended key    (low byte of player[+0x50][i*8+0])
    +8  DWORD value                (player[+0x50][i*8+4])
    +12 DWORD zero-extended type   (player[+0x1C] byte)

The count helper walks the same ``+0x4C`` container (data pointer ``+0x50``,
count ``+0x54``) and adds 1 for each non-zero value DWORD.  The player array is
``Map +0xA64`` (pointer to an array of player pointers) and ``Map +0xA68``
(dword count); both are confirmed by the original accessor ``0x488830``.

Scope / limits
--------------
Only the branch above is newly exercised.  The player-scoped pool/availability
groups (types ``1 + (player[+0x1C] << 16)`` .. ``5 + ...``) are left with zero
local and global lists and are NOT interpreted here.  No GUI reachability,
gameplay meaning, Type77/A80 content or Type 29/186/113/114 semantics are
claimed.  Type77 count at Map ``+0x110`` stays 0.  SDT/Crypt are untouched.

Fixtures
--------
The fixture builder ``emulate_editor_variables_writer.build()`` and runner
``native_editor_variables_writer_cpu.run()`` are reused unchanged.  They
declare the Map model at ``0x17700000``, 248 doubles at ``0x17710000``,
Terrain #6 raw bytes, 0xA5-filled stack, the CFile sink, HeapAlloc/HeapFree,
lstrlen, interlocked, critical-section, clock/TLS/error IAT adapters and the
original Terrain/default-initializer calls.  This audit adds only data:

    Map +0xA64 / +0xA68          explicit player pointer array / count
    player object                zero-filled except +0x1C and +0x4C (+0x50/+0x54)
    entry array                  8-byte {key, value} records
    pool/availability locals     explicitly zero (branch not in scope)

No instruction is patched, no register/vtable/getter is replaced and no code
or read-only section is written.  All 16 bytes of every new player record are
assigned by the original instructions and checked against an expectation built
only from the declared inputs; scalar record tails are the inherited
writer-entry stack scratch and are reported as reproduction, not as a new
semantic claim.
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

import emulate_editor_variables_writer as ev          # fixture builder, reused
import native_editor_variables_writer_cpu as cpu      # original CPU runner, reused
from build_effect_semantics import pe_read

ENGINE = ev.defaults.engine
W = ENGINE.writer
MAP = ev.MAP
VALUES = ev.VALUES
EXE = ENGINE.EXE
STACK = W.STACK
EXE_SHA256 = '989c45df779bccfc9a9e509b2ad0224a9bd269629f74792adc0320fa4b99fb09'

# New data-only fixture addresses (do not overlap the inherited fixture).
PLAYER_ARRAY = 0x17790000
PLAYER = 0x177A0000
ENTRIES = 0x177B0000

HELPER_FILES = [
    'emulate_editor_variables_writer.py',
    'native_editor_variables_writer_cpu.py',
    'emulate_editor_global_variables.py',
    'emulate_room_model_chain.py',
    'probe_editor_global_defaults.py',
    'emulate_terrain_text_stat.py',
    'emulate_room_writer.py',
    'build_effect_semantics.py',
]

BRANCH = (0x4943C0, 0x494464)      # FUN_4943C0 whole function bytes
COUNT = (0x48CF50, 0x48CF7F)       # FUN_48CF50 whole function bytes
SCALED = {67, 155, 158, 185, 220, 221}


def sha(blob):
    return hashlib.sha256(bytes(blob)).hexdigest()


def hx(address):
    return f'0x{address:X}'


SCENES = [
    dict(
        name='player-no-players-baseline',
        description='Map +0xA68 = 0: FUN_4943C0 returns 1 before the loop; '
                    'no player record. Expected inheritance of the published 196-record scene.',
        players=[],
    ),
    dict(
        name='player-one-accepted',
        description='One player, byte +0x1C = 3, one +0x4C entry {0x94, 0x11223344}: '
                    'one independently derived 16-byte record.',
        players=[dict(kind=3, entries=[(0x94, 0x11223344)])],
    ),
    dict(
        name='player-two-order',
        description='One player, two +0x4C entries {0x11, 0xAABBCCDD} then '
                    '{0x22, 0x01020304}: two records in entry-array order.',
        players=[dict(kind=3, entries=[(0x11, 0xAABBCCDD), (0x22, 0x01020304)])],
    ),
    dict(
        name='player-kind2-boundary',
        description='One player, byte +0x1C = 2 (gate is strict 2 < byte): count '
                    'and writer both skip; no record, declared count stays 196.',
        players=[dict(kind=2, entries=[(0x94, 0x11223344)])],
    ),
]


def scene_fixture(scene):
    """Reuse the published builder; add only explicit player/entry memory."""
    c = dict(name=scene['name'], TPS=4, seed=165, overrides={}, terrain={},
             tail=None, extra=False, lists=[], error=None)
    f = ev.build(c)
    mem = {int(row['address'], 16): bytearray.fromhex(row['hex'])
           for row in f['memory']}
    players = scene['players']
    assert len(players) <= 1, 'one player is enough for this bounded branch'
    struct.pack_into('<2I', mem[MAP], 0xA64, PLAYER_ARRAY if players else 0,
                     len(players))
    if players:
        mem[PLAYER_ARRAY] = bytearray(b''.join(ev.p32(PLAYER) for _ in players))
        for index, player in enumerate(players):
            address = PLAYER + index * 0x1000
            obj = bytearray(0x200)                     # all other fields zero
            obj[0x1C] = player['kind'] & 0xFF
            entries = player['entries']
            if entries:
                struct.pack_into('<2I', obj, 0x50, ENTRIES, len(entries))
                mem[ENTRIES] = bytearray(b''.join(
                    struct.pack('<2I', key, value) for key, value in entries))
            mem[address] = obj
    f['memory'] = [ev.region(a, bytes(b)) for a, b in sorted(mem.items())]
    spans = sorted((a, len(b)) for a, b in mem.items())
    assert all(a + n <= b for (a, n), (b, _) in zip(spans, spans[1:])), spans
    return f


def independent_player_records(scene):
    """Derived only from declared inputs, not from the emulator output."""
    rows = []
    for player in scene['players']:
        if (player['kind'] & 0xFF) < 3:
            continue
        for key, value in player['entries']:
            if value & 0xFFFFFFFF:
                rows.append((1, key & 0xFF, value & 0xFFFFFFFF,
                             player['kind'] & 0xFF))
    return rows


def inherited_fixed_records(fixture, result):
    """Scalar/fixed records: inherited reproduction, not a new branch claim.

    Scales/IDS are the published 193-type list and input doubles.  The two tail
    DWORDs are the writer-entry stack scratch observed before 0x4937C0.
    """
    inputs = {int(row['address'], 16): bytes.fromhex(row['hex'])
              for row in fixture['memory']}
    ids, _, _ = ev.defaults.static_defaults()
    assert len(ids) == 193, len(ids)
    seed = bytes.fromhex(result['writer_entry_record_seed_hex'])
    tail0, tail1 = struct.unpack_from('<2I', seed, 8)
    scalar = []
    for typ in ids:
        value = struct.unpack_from('<d', inputs[VALUES], typ * 8)[0]
        scalar.append((typ, int(value * (4096 if typ in SCALED else 1)) & 0xFFFFFFFF,
                       tail0, tail1))
    model = inputs[MAP]
    gold = struct.unpack_from('<H', inputs[ENGINE.RAW], 0x19A)[0]
    max_health = struct.unpack_from('<H', inputs[ENGINE.RAW], 0x1FF)[0]
    tps = struct.unpack_from('<H', model, 0x946)[0]
    type7 = int(struct.unpack_from('<d', inputs[VALUES], 7 * 8)[0])
    product = ev.signed((abs(type7) & 0xFFFFFFFF) * gold)
    margin = ev.idiv(ev.idiv(product, max_health), tps) & 0xFFFFFFFF
    return scalar + [(74, margin, 0, 0), (241, 5120, 0, 0),
                     (242, 0x11223344, 0, 0)], tail0, tail1


def verify(scene, fixture, result):
    assert result['source_exe_sha256'] == EXE_SHA256, result['source_exe_sha256']
    assert result['code_substitutions'] == 0
    assert not result['failures'], result['failures']
    assert result['success'] and result['error'] is None, (result['error'], result['calls'])
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

    expected_players = independent_player_records(scene)
    expected_inherited, tail0, tail1 = inherited_fixed_records(fixture, result)
    expected = expected_players + expected_inherited

    out = post[W.OUTPUT]
    signature, version, size = struct.unpack_from('<3I', out, 0)
    assert (signature, version) == (0xE6, 4)
    declared = struct.unpack_from('<I', out, 20)[0]
    assert struct.unpack_from('<I', out, 52)[0] == 16 * declared
    # 193 scalar + Type74 + Type241 + Type242 inherited, plus new player rows.
    assert declared == len(expected_inherited) + len(expected_players), declared
    assert size == 56 + 16 * declared, (size, declared)
    records = [struct.unpack_from('<4I', out, 56 + 16 * i)
               for i in range(declared)]
    assert records[:len(expected_players)] == expected_players, \
        (records[:len(expected_players)], expected_players)
    assert records[len(expected_players):] == expected_inherited, \
        [(a, b) for a, b in zip(records[len(expected_players):], expected_inherited)
         if a != b][:4]
    assert out[size:] == bytes([0xA5]) * (len(out) - size)
    assert struct.unpack('<3I', post[W.META]) == (3 + declared, W.OUTPUT + size, 0)

    if scene['players']:
        assert post[PLAYER] == inputs[PLAYER]
        if scene['players'][0]['entries']:
            assert post[ENTRIES] == inputs[ENTRIES]

    kind = scene['players'][0]['kind'] if scene['players'] else None
    count_entries = (sum(1 for _, value in scene['players'][0]['entries']
                         if value & 0xFFFFFFFF)
                     if scene['players'] and kind >= 3 else 0)
    required = {
        '0x4937D0': 1,                    # CALL 0x48CA00
        '0x49386D': 1,                    # CALL 0x4943C0
        '0x4943C0': 1,                    # branch function entered
        '0x4943E8': len(scene['players']),  # CALL 0x488830 per player
        '0x494434': len(expected_players),  # CFile sink per emitted player row
        '0x48CA36': 1 if kind is not None and kind >= 3 else 0,
        '0x48CF50': 1 if kind is not None and kind >= 3 else 0,
        '0x48CF71': count_entries,          # non-zero value increments
    }
    visited = result['visited_instructions']
    observed = {pc: visited.get(pc.lower(), 0) for pc in required}
    assert observed == required, (observed, required)
    assert result['visited_instructions'].get('0x4943cc', 0) == 0  # no header path confusion

    return dict(
        outcome='PASS',
        declared_records=declared,
        emitted_records=declared,
        independent_player_records=[list(row) for row in expected_players],
        observed_player_records=[list(row) for row in records[:len(expected_players)]],
        independent_player_records_hex=[struct.pack('<4I', *row).hex()
                                        for row in expected_players],
        observed_player_records_hex=[struct.pack('<4I', *row).hex()
                                     for row in records[:len(expected_players)]],
        inherited_records=len(expected_inherited),
        inherited_tail_reproduction=[tail0, tail1],
        branch_pc_visits=observed,
        output_bytes=size,
        type77_count=struct.unpack_from('<I', inputs[MAP], 0x110)[0],
        native_steps=result['instruction_count'],
    )


def audit(write=False):
    assert sha(EXE.read_bytes()) == EXE_SHA256, 'pinned Pro EXE hash mismatch'
    blob = EXE.read_bytes()
    branch_bytes = pe_read(blob, BRANCH[0], BRANCH[1] - BRANCH[0])
    count_bytes = pe_read(blob, COUNT[0], COUNT[1] - COUNT[0])
    source = {
        'exe': str(EXE),
        'exe_sha256': EXE_SHA256,
        'branch': dict(address=hx(BRANCH[0]), end=hx(BRANCH[1]),
                       bytes_hex=branch_bytes.hex(),
                       bytes_sha256=sha(branch_bytes)),
        'count_helper': dict(address=hx(COUNT[0]), end=hx(COUNT[1]),
                             bytes_hex=count_bytes.hex(),
                             bytes_sha256=sha(count_bytes)),
        'player_accessor': dict(address='0x488830',
                                bytes_hex=pe_read(blob, 0x488830, 0x20).hex(),
                                bytes_sha256=sha(pe_read(blob, 0x488830, 0x20))),
    }
    branch_semantics = dict(
        player_array=dict(pointer_offset='Map +0xA64', count_offset='Map +0xA68',
                          accessor='0x488830: index<count ? *(Map[+0xA64]+index*4) : 0'),
        gate='selected player: unsigned byte player[+0x1C] >= 3 (strict 2 < byte)',
        count_helper='0x48CF50(player+0x4C): count entries i where DWORD '
                     'player[+0x50][i*8+4] != 0',
        first_emitted_type=1,
        record=dict(size=16, fields=[
            dict(offset=0, value='DWORD constant 1', pc='0x4943CD'),
            dict(offset=4, value='DWORD zero-extended key byte player[+0x50][i*8+0]',
                 pc='0x49441C'),
            dict(offset=8, value='DWORD player[+0x50][i*8+4]', pc='0x49442B'),
            dict(offset=12, value='DWORD zero-extended player[+0x1C]', pc='0x4943FD'),
        ], sink_call_pc='0x494434', unasserted_bytes=0),
        call_sites=dict(total_count='0x4937D0 -> 0x48CA00',
                        player_records='0x49386D -> 0x4943C0',
                        player_lookup='0x4943E8 -> 0x488830',
                        count_helper='0x48CA36 -> 0x48CF50'),
    )
    helpers = {name: sha((RES / name).read_bytes()) for name in HELPER_FILES}
    rows = []
    ok = True
    for scene in SCENES:
        try:
            fixture = scene_fixture(scene)
            result = cpu.run(fixture)
            outcome = verify(scene, fixture, result)
            rows.append(dict(name=scene['name'], description=scene['description'],
                             fixture_sha256=sha(json.dumps(fixture, sort_keys=True).encode()),
                             **outcome))
        except Exception as exc:  # keep a reproducible failing checkpoint
            ok = False
            rows.append(dict(name=scene['name'], description=scene['description'],
                             outcome='FAIL', error=f'{type(exc).__name__}: {exc}',
                             traceback=traceback.format_exc()))
    report = dict(
        scope=__doc__,
        overall='PASS' if ok else 'FAIL',
        source=source,
        branch_semantics=branch_semantics,
        helper_sha256=helpers,
        fixture_contract=dict(
            player_array=dict(address=hx(PLAYER_ARRAY), layout='DWORD player pointers'),
            player_object=dict(address=hx(PLAYER), size=0x200,
                               fields='+0x1C kind byte; +0x4C container {+0x50 data, +0x54 count}; '
                                      'other pool/availability containers explicitly zero'),
            entries=dict(address=hx(ENTRIES), layout='8-byte {DWORD key, DWORD value}'),
            map_fields='Map +0xA64 pointer array, +0xA68 count; +0x110 Type77 count remains 0',
            registers='every call: EAX/EBX/ECX/EDX/ESI/EDI zeroed except declared ECX; '
                      'ESP=EBP=0x7FFF0000; EFLAGS=2; FPCW=0x37F; return stub at 0x16000000',
            inherited='CFile sink, HeapAlloc/HeapFree, lstrlen, interlocked, critical-section, '
                      'clock/TLS/error IAT adapters and Terrain #6/default-initializer calls '
                      'belong to the reused published fixture, not to this audit',
        ),
        limitations=[
            'Only FUN_4943C0 / FUN_48CF50 with one selected player is exercised.',
            'Pool/availability groups 1..5 (types 1+(kind<<16)..) are NOT executed or interpreted.',
            'No GUI reachability, full save, gameplay consumer or real-file write is claimed.',
            'Type77 count 110 is zero; Type 29, 113, 114 and 186 keep prior treatment.',
            'Scalar record tails +8..+15 are inherited stack-scratch reproduction, not a new meaning.',
            'Zero-value +0x4C entries (skip branch of writer and count helper) are not exercised.',
            'Every scene has at most one player, so accessor index > 0 and cross-player order are untested.',
            'Header timestamp/date bytes and other inherited header fields are not re-derived here.',
        ],
        scenes=rows,
    )
    if write:
        (ROOT / 'player-writer-results.json').write_text(
            json.dumps(report, ensure_ascii=False, indent=2) + '\n', encoding='utf8')
    return report


def main(argv):
    write = '--no-write' not in argv
    report = audit(write=write)
    compact = dict(overall=report['overall'],
                   scenes=[{k: v for k, v in row.items()
                            if k in ('name', 'outcome', 'declared_records',
                                     'emitted_records', 'independent_player_records',
                                     'observed_player_records', 'independent_player_records_hex',
                                     'observed_player_records_hex', 'branch_pc_visits',
                                     'error')}
                           for row in report['scenes']])
    print(json.dumps(compact, ensure_ascii=False, indent=2))
    return 0 if report['overall'] == 'PASS' else 1


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
