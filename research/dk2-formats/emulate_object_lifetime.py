"""Complete originalObject tick(s), HP decay andunknownManaGen read audit.

Nativephase89 Object snapshot,originalvtables andphysics/renderinfo methods.
Noactivegraphics/audiohandles,ordinary state0,noDeathEffect/particleattachment.
RoomId prepared;nofullroomconstructionorWorld scheduler/removal claim.
"""
from pathlib import Path
import hashlib
import json
import sys
import probe_object_tick as probe
import native_unicorn_fieldtrace as cpu
from native_object_lifetime import after_ticks
from build_effect_semantics import EXE
from emulate_element_colors import region
f, cf = probe.f, probe.cf
ROOT = Path(__file__).resolve().parent


def cases():
    base = dict(flags=0, mana=0, hp=1000, room_id=0, in_hand=False, ticks=1)
    variants = [('normal', {}), ('always', dict(flags=1)), ('outside', dict(flags=2)),
                ('inside-room', dict(flags=2, room_id=200)), ('always-inside', dict(flags=1, room_id=200)),
                ('both-inside', dict(flags=3, room_id=200)), ('always-in-hand', dict(flags=1, in_hand=True)),
                ('outside-in-hand', dict(flags=2, in_hand=True)), ('hp1', dict(flags=1, hp=1)),
                ('hp0', dict(flags=1, hp=0)), ('hp-negative', dict(flags=1, hp=-1)),
                ('five-ticks', dict(flags=1, hp=3, ticks=5)),
                ('five-ticks-in-room', dict(flags=2, hp=3, ticks=5, room_id=200)),
                ('opaque-flag', dict(flags=0x80000001)), ('mana100', dict(flags=1, mana=100)),
                ('manaffff', dict(flags=1, mana=65535)),
                ('damage-and-decay', dict(flags=0x200001, hp=10, damage=5)),
                ('not-damageable-and-decay', dict(flags=1, hp=10, damage=5)),
                ('damage-only', dict(flags=0x200000, hp=10, damage=5))]
    return [dict(base, **v, name=n) for n, v in variants]


def build(c):
    args, _ = probe.build(c['flags'], c['mana'], c['hp'], c['room_id'] != 0, c['in_hand'])
    memory = {int(r['address'], 16): bytearray.fromhex(r['hex']) for r in json.loads(args['memory'])['regions']}
    f.put(memory[cf.ACTOR], 0x138, c['room_id'], 2)
    if c['ticks'] > 1:
        args['before_calls'] = [dict(address='0x49bd20', registers=dict(ECX=hex(cf.ACTOR)), stack_args=[]) for _ in range(c['ticks']-1)]
    if 'damage' in c:
        args['before_calls'] = [dict(address='0x4b2ce0', registers=dict(ECX=hex(cf.ACTOR)), stack_args=[(-c['damage'])&0xFFFFFFFF, 7, 0])]+args.get('before_calls', [])
    args['memory'] = json.dumps(dict(regions=[region(a, b) for a, b in memory.items()]))
    checks = {a: bytes(b) for a, b in memory.items() if a not in [0x7FFEE000, f.GFX_CB, f.SOUND_CB, 0x28CFA4]}
    initial_hp = c['hp']-(c.get('damage', 0) if c['flags'] & 0x200000 else 0)
    expected = dict(hp=after_ticks(initial_hp, c['flags'], c['room_id'], c['in_hand'], c['ticks']),
                    render_age=c['ticks'], unknown_mana_field_reads=0)
    wanted = {a: bytearray(b) for a, b in checks.items()}
    f.put(wanted[cf.ACTOR], 0x40, expected['hp'])
    f.put(wanted[cf.ACTOR], 0x6A, expected['render_age'], 2)
    if 'damage' in c:
        f.put(wanted[cf.ACTOR], 0x36, 7, 2)
    return args, expected, checks, wanted


def verify(c, args, expected, checks, wanted, result):
    assert result['success'] and result['hit_return'] and int(result['registers']['EAX'], 16) == 1
    assert not result['protected_write_or_execution_failures']
    assert len(result['before_calls']) == c['ticks']-1+int('damage' in c) and all(r['returned'] for r in result['before_calls'])
    post = {a: bytearray(b) for a, b in checks.items()}
    for row in result['context_mismatches']:
        address = int(row['address'], 16)
        assert len(post[address]) == row['length']
        for d in row['differences']:
            assert post[address][d['offset']] == d['expected']
            post[address][d['offset']] = d['actual']
    assert post == wanted, (c['name'], result['context_mismatches'])
    assert post[cf.ACTOR].hex() == result['captured_hex']
    assert post[cf.ACTOR][393:] == bytes([0xA5])*256
    unknown_reads = [r for r in result['observed_reads'] if int(r['address'], 16) < f.DEFINITION+0x108 and int(r['address'], 16)+r['size'] > f.DEFINITION+0x106]
    assert not unknown_reads
    assert hex(0x49BD20) in result['visited_instructions'] and hex(0x49BEB2) in result['visited_instructions']
    # Existence/pointer tables are among complete checked regions:HP0 stays
    # registered after this method;World cleanup remains a separate consumer.
    assert post[0x6ED540][100] == 1


def main():
    selected = cases()
    if len(sys.argv) > 1:
        selected = [c for c in selected if c['name'] == sys.argv[1]]
        assert selected
    rows = []
    backend = hashlib.sha256((ROOT/'native_unicorn_fieldtrace.py').read_bytes()).hexdigest()
    for c in selected:
        args, expected, checks, wanted = build(c)
        tag = hashlib.sha256((json.dumps(args, sort_keys=True)+backend).encode()).hexdigest()[:10]
        path = ROOT/'ghidra'/f'phase108-lifetime-{c["name"]}-{tag}.json'
        if path.exists():
            value = json.loads(path.read_text(encoding='utf8'))
            assert value['input'] == args and value['expected'] == expected
        else:
            result = cpu.run(EXE, args, list(checks.items()), f.OBS, cf.ACTOR, 393+256)
            value = dict(case=c, input=args, expected=expected, result=result)
            path.write_text(json.dumps(value, indent=2), encoding='utf8')
        verify(c, args, expected, checks, wanted, value['result'])
        rows.append(dict(case=c['name'], file=path.name, sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
        print(c['name'], expected, 'verified', flush=True)
    if len(selected) == len(cases()):
        (ROOT/'phase108-object-lifetime-emulations.json').write_text(json.dumps(dict(scope=__doc__, replays=rows), indent=2), encoding='utf8')


if __name__ == '__main__':
    main()
