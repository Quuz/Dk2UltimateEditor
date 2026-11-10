"""Original raw Object conversion/loading, state initialization and whole dispatch/tick.

Prepared existing Object instance, null graphics, no audio handles; not World startup.
Out-of-table cases attempt a null callback, rejected before execution by the CPU backend.
No live game is launched; a Windows crash is not observed by these probes.
"""
import hashlib
import json
import struct
import sys
from pathlib import Path
import dk2_formats as fmt
import probe_object_tick as probe
import native_unicorn_fieldtrace as cpu
import native_object_loading as loading
from native_object_states import patch_start_state
from object_state_tables import BASE, SIZE, initialized_table
from build_effect_semantics import EXE, pe_read
from emulate_element_colors import region

ROOT = Path(__file__).resolve().parent
f, cf = probe.f, probe.cf
RAW, MID = 0x10090000, 0x10091000
SOURCE = ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/Objects.kwd'


def cases():
    base = dict(start=0, active=0, age=7, ticks=1, whole_tick=True, fault=False, mana=0)
    variants = [
        ('none', {}), ('copy-persistent16', dict(start=16)),
        ('persistent16-two', dict(start=16, ticks=2)),
        ('zero-stub2-two', dict(start=2, ticks=2)),
        ('zero-stub2-three', dict(start=2, ticks=3)),
        ('zero-stub57-two', dict(start=57, ticks=2)),
        ('zero-stub58-two', dict(start=58, ticks=2)),
        ('persistent22-two', dict(start=22, ticks=2)),
        ('persistent51-two', dict(start=51, ticks=2)),
        ('persistent56-two', dict(start=56, ticks=2)),
        ('active-ignores-start', dict(start=2, active=16, age=5)),
        ('age-wrap', dict(start=0, active=16, age=0xFFFFFFFF)),
        ('copy59-only', dict(start=59)),
        ('outside59-two', dict(start=59, ticks=2, fault=True)),
        ('outside63-two', dict(start=63, ticks=2, fault=True)),
        ('outside255-two', dict(start=255, ticks=2, fault=True)),
        ('mana-vault-off-period', dict(active=39, age=58)),
        ('mana-vault-mana100', dict(active=39, age=58, mana=100)),
        ('mana-vault-manaffff', dict(active=39, age=58, mana=65535)),
        ('misc-effect-zero-period', dict(active=40, age=59)),
        ('mana-vault-period', dict(active=39, age=59, emit=True)),
        ('mana-vault-period-mana100', dict(active=39, age=59, emit=True, mana=100)),
        ('mana-vault-period-manaffff', dict(active=39, age=59, emit=True, mana=65535)),
        ('misc-effect-period', dict(active=40, age=59, emit=True, misc_effect=123)),
        ('mana-vault-definition-period', dict(active=39, age=59, emit=True, source_id=101)),
        ('claimed-vault-definition-period', dict(active=39, age=59, emit=True, source_id=108)),
    ]
    return [dict(base, **v, name=n) for n, v in variants]


def source_record(source_id=17):
    rows = [b for s in fmt.parse(SOURCE.read_bytes()) if s.signature == 0xF0
            for b in s.records if b[0x35A] == source_id]
    assert len(rows) == 1 and len(rows[0]) == 894
    return rows[0]


def build(c):
    args, _ = probe.build(mana=c['mana'])
    memory = {int(r['address'], 16): bytearray.fromhex(r['hex'])
              for r in json.loads(args['memory'])['regions']}
    raw = bytearray(patch_start_state(source_record(c.get('source_id', 17)), c['start']))
    struct.pack_into('<H', raw, 0x34C, c['mana'])
    # Preserve the source MiscEffectId unless this case explicitly edits it.
    if c.get('source_id', 17) == 17:
        assert raw[0x358:0x35A] == bytes(2)
    if 'misc_effect' in c:
        struct.pack_into('<H', raw, 0x358, c['misc_effect'])
    if c.get('emit'):
        # Exact original CPCEngineInterface slot90 returns null. No injected code.
        assert pe_read(EXE.read_bytes(), 0x670574 + 0x90, 4) == struct.pack('<I', 0x4430D0)
        f.put(memory[f.GV], 0x90, 0x4430D0)
    memory[RAW] = raw
    memory[MID] = bytearray([0xA5]) * 1022
    memory[BASE] = bytearray(SIZE)
    memory[0x6FCDCF] = bytearray(struct.pack('<I', 1))
    f.put(memory[cf.ACTOR], 0x13A, c['active'])
    f.put(memory[cf.ACTOR], 0x13E, c['age'])
    f.put(memory[cf.ACTOR], 0x12C, c.get('source_id', 17), 1)
    before = [dict(address='0x49e600', registers=dict(ECX='0'), stack_args=[]),
              dict(address='0x56d4f0', registers=dict(ECX=hex(MID)), stack_args=[]),
              dict(address='0x56d620', registers=dict(ECX=hex(MID)), stack_args=[RAW]),
              dict(address='0x49dd60', registers=dict(ECX=hex(f.DEFINITION)), stack_args=[MID, 0])]
    entry = 0x49BD20 if c['whole_tick'] else 0x49EED0
    before += [dict(address=hex(entry), registers=dict(ECX=hex(cf.ACTOR)), stack_args=[])
               for _ in range(c['ticks'] - 1)]
    args.update(address=hex(entry), before_calls=before,
                memory=json.dumps(dict(regions=[region(a, b) for a, b in memory.items()])),
                observe_reads=[[f.DEFINITION, 285]], max_steps=200000)
    if c.get('emit'):
        args['observe_reads'].append([0x7FFEE000, 16384])
    checks = {a: bytes(b) for a, b in memory.items()
              if a not in [0x7FFEE000, f.GFX_CB, f.SOUND_CB, 0x28CFA4]}
    wanted = {a: bytearray(b) for a, b in checks.items()}
    wanted[BASE] = bytearray(initialized_table())
    mid = loading.intermediate(bytes(raw), bytes(1022))
    wanted[MID] = bytearray(mid)
    wanted[f.DEFINITION] = bytearray(loading.definition(mid, checks[f.DEFINITION], loading.DX, 20, 1))
    state, age = c['active'], c['age']
    for _ in range(c['ticks']):
        if state == 0:
            if c['start']:
                state, age = c['start'], 0
        else:
            age = (age + 1) & 0xFFFFFFFF
            if state in [2, 7, 57, 58]:
                state = 0
            else:
                assert state in [16, 22, 51, 56, 39, 40] or c['fault']
    f.put(wanted[cf.ACTOR], 0x13A, state)
    f.put(wanted[cf.ACTOR], 0x13E, age)
    render_ticks = c['ticks'] - int(c['fault']) if c['whole_tick'] else 0
    f.put(wanted[cf.ACTOR], 0x6A, render_ticks, 2)
    expected = dict(active=state, age=age, render_ticks=render_ticks,
                    unknown_mana_reads=0, null_dispatch_fault=c['fault'])
    if c.get('emit'):
        expected['effect_id'] = 288 if state == 39 else c['misc_effect']
    return args, expected, checks, wanted


def verify(c, args, expected, checks, wanted, result):
    assert len(result['before_calls']) == 4 + c['ticks'] - 1
    assert all(r['returned'] for r in result['before_calls'])
    if c['fault']:
        assert not result['success'] and not result['hit_return']
        assert int(result['registers']['EIP'], 16) == 0 and result['emulation_error'] is None
        assert result['protected_write_or_execution_failures'] == [
            dict(reason='execution outside original PE code', address=0)]
    else:
        assert result['success'] and result['hit_return'] and int(result['registers']['EAX'], 16) == 1
        assert not result['protected_write_or_execution_failures']
    post = {a: bytearray(b) for a, b in checks.items()}
    for row in result['context_mismatches']:
        address = int(row['address'], 16)
        assert len(post[address]) == row['length']
        for d in row['differences']:
            assert post[address][d['offset']] == d['expected']
            post[address][d['offset']] = d['actual']
    assert post == wanted, (c['name'], [(hex(a), [(i, x, y) for i, (x, y) in enumerate(zip(post[a], wanted[a])) if x != y][:20])
                                       for a in post if post[a] != wanted[a]])
    assert post[cf.ACTOR].hex() == result['captured_hex']
    unknown = [r for r in result['observed_reads'] if int(r['address'], 16) < f.DEFINITION + 0x108
               and int(r['address'], 16) + r['size'] > f.DEFINITION + 0x106]
    assert not unknown
    assert '0x49eed0' in result['visited_instructions']
    if c.get('emit'):
        for entry in ['0x509580', '0x50e490', '0x4430d0']:
            assert result['visited_instructions'][entry] == 1
        # Six arguments are read in reverse order by the original World bridge.
        pcs = [0x50E49A, 0x50E4A1, 0x50E4A6, 0x50E4AB, 0x50E4B0, 0x50E4B5]
        words = []
        for pc in pcs:
            reads = [r for r in result['observed_reads'] if int(r['pc'], 16) == pc]
            assert len(reads) == 1 and reads[0]['size'] == 4
            words.append(int.from_bytes(bytes.fromhex(reads[0]['value_hex']), 'little'))
        assert words[-1] & 0xFFFF == expected['effect_id']
        assert words[-2] & 0xFFFF == 7
        assert words[2] == words[3] == cf.ACTOR + 0x1A
        assert all(0x7FFEE000 <= p < 0x7FFF0000 for p in words[:2])
    else:
        assert '0x509580' not in result['visited_instructions']
    for entry in ['0x49e600', '0x56d4f0', '0x56d620', '0x49dd60']:
        assert entry in result['visited_instructions']


def main():
    selected = cases()
    if len(sys.argv) > 1:
        selected = [c for c in selected if c['name'] == sys.argv[1]]
        assert selected
    rows = []
    backend = hashlib.sha256((ROOT/'native_unicorn_fieldtrace.py').read_bytes()).hexdigest()
    for c in selected:
        args, expected, checks, wanted = build(c)
        tag = hashlib.sha256((json.dumps(args, sort_keys=True) + backend).encode()).hexdigest()[:10]
        path = ROOT/'ghidra'/f'phase110-state-{c["name"]}-{tag}.json'
        if path.exists():
            saved = json.loads(path.read_text(encoding='utf8'))
            assert saved['input'] == args and saved['expected'] == expected
        else:
            result = cpu.run(EXE, args, list(checks.items()), f.OBS, cf.ACTOR, 649)
            saved = dict(case=c, input=args, expected=expected, result=result)
            path.write_text(json.dumps(saved, indent=2), encoding='utf8')
        verify(c, args, expected, checks, wanted, saved['result'])
        rows.append(dict(case=c['name'], file=path.name, sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
        print(c['name'], expected, 'verified', flush=True)
    if len(selected) == len(cases()):
        (ROOT/'phase110-object-state-emulations.json').write_text(
            json.dumps(dict(scope=__doc__, replays=rows), indent=2), encoding='utf8')


if __name__ == '__main__':
    main()
