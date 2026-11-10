"""Original488930 complete tick: Fairy speed andWizard WorkEnhancement.

Original state/category/action/nav initializers are already executed and checked
by probe_creature_tick. Prepared tables/empty forces/null actor graphics are
explicit. Optional native Crypt setter andtick share one CPU/memory instance.
All game code/vtables retained; original graphics64/80 providers execute.
"""
from pathlib import Path
import hashlib
import json
import struct
import sys
import probe_creature_tick as probe
import native_unicorn_tick as cpu
import emulate_creature_factory as cf
import emulate_object_factory as f
from emulate_element_colors import region
from build_effect_semantics import EXE
from native_room_counters import counter_slot, change_counter

ROOT = Path(__file__).resolve().parent
ROOM = 0x10600000
MEMBER = 0x10610000
OTHEROWNER = 0x10620000


def cases():
    base = dict(cache=1000, fairy=0, wizard=0)
    changes = [('base', {}), ('fairy1', dict(fairy=1)), ('fairy2', dict(fairy=2)),
               ('fairy3', dict(fairy=3)), ('fairy127', dict(fairy=127)), ('fairy128', dict(fairy=128)),
               ('fairy255', dict(fairy=255)), ('fairy-cap', dict(fairy=1, cache=4000)),
               ('fairy-zero-speed', dict(fairy=1, cache=0)), ('base4000', dict(cache=4000)),
               ('base-zero-speed', dict(cache=0)), ('wizard1', dict(wizard=1)),
               ('wizard2', dict(wizard=2)), ('wizard127', dict(wizard=127)),
               ('wizard128', dict(wizard=128)), ('both1', dict(fairy=1, wizard=1)),
               ('crypt-fairy-insert', dict(crypt_species=20, crypt_operation='insert', crypt_initial=0, crypt_owner=7)),
               ('crypt-fairy-clear', dict(fairy=1, crypt_species=20, crypt_operation='clear', crypt_initial=1, crypt_owner=7)),
               ('crypt-fairy-other-owner', dict(crypt_species=20, crypt_operation='insert', crypt_initial=0, crypt_owner=8)),
               ('crypt-wizard-insert', dict(crypt_species=16, crypt_operation='insert', crypt_initial=0, crypt_owner=7)),
               ('crypt-wizard-clear', dict(wizard=1, crypt_species=16, crypt_operation='clear', crypt_initial=1, crypt_owner=7)),
               ('crypt-wizard-other-owner', dict(crypt_species=16, crypt_operation='insert', crypt_initial=0, crypt_owner=8))]
    return [dict(base, **v, name=n) for n, v in changes]


def outcome(c):
    fairy, wizard = c['fairy'], c['wizard']
    result = {}
    if 'crypt_species' in c:
        slot = counter_slot(c['crypt_species'])
        count = change_counter(c['crypt_initial'], 1 if c['crypt_operation'] == 'insert' else -1)
        if c['crypt_owner'] == 7:
            if slot == 0x108C:
                fairy = count
            else:
                assert slot == 0x1094
                wizard = count
        result['crypt'] = dict(slot=slot, count=count, tag=110 if c['crypt_operation'] == 'insert' else 0,
                               room55=1234 if c['crypt_operation'] == 'insert' else 0)
    speed = c['cache']
    if 0 < fairy < 128:
        speed = min(4095, ((speed*(fairy+10)*10)&0xFFFFFFFF)//100)
    result.update(speed=c['cache'], controller_max=c['cache']*16, transient_speed=speed,
                  transient_controller_max=speed*16, work_enhancement=205*wizard if 0 < wizard < 128 else 0,
                  fairy_effective=fairy, wizard_effective=wizard)
    return result


def build(c):
    args, _ = probe.build(True, True, True)
    memory = {int(r['address'], 16): bytearray.fromhex(r['hex']) for r in json.loads(args['memory'])['regions']}
    e = outcome(c)
    f.put(memory[cf.ACTOR], 0x11C, c['cache'], 2)
    f.put(memory[cf.ACTOR], 0x206, c['cache']*16)
    f.put(memory[f.PLAYER], 0x108C, c['fairy'], 1)
    f.put(memory[f.PLAYER], 0x1094, c['wizard'], 1)
    if 'crypt' in e:
        room = bytearray(0xC0)+bytearray([0xA5])*64
        for off, value, n in [(4, 200, 2), (0x18, 26, 1), (0x1D, c['crypt_owner'], 2),
                               (0x53, 110 if c['crypt_operation'] == 'clear' else 0, 2), (0x55, 1234, 4)]:
            f.put(room, off, value, n)
        memory[ROOM] = room
        member = bytearray(0x380)+bytearray([0xA5])*64
        f.put(member, 4, 110, 2)
        f.put(member, 0x36F, c['crypt_species'], 1)
        memory[MEMBER] = member
        owner = f.PLAYER
        if c['crypt_owner'] == 8:
            owner = OTHEROWNER
            memory[owner] = bytearray(0x1200)
            f.put(memory[0x6EE540], 8*4, owner)
            f.put(memory[0x6ED540], 8, 1, 1)
        f.put(memory[owner], e['crypt']['slot'], c['crypt_initial'], 1)
        for tag, address in [(200, ROOM), (110, MEMBER)]:
            f.put(memory[0x6EE540], tag*4, address)
            f.put(memory[0x6ED540], tag, 1, 1)
        args['before_calls'] = [dict(address='0x4d8e60', registers=dict(ECX=hex(ROOM)), stack_args=[e['crypt']['tag']])]
    checks = {a: bytes(b) for a, b in memory.items() if a not in [0x7FFEE000, f.GFX_CB, f.SOUND_CB, 0x28CFA4]}
    args['observe_writes'] = [[cf.ACTOR+0x11C, 2], [cf.ACTOR+0x206, 4], [cf.ACTOR+0x3B3, 4], [cf.ACTOR+0x1A, 12]]
    args['memory'] = json.dumps(dict(regions=[region(a, b) for a, b in memory.items()]))
    return args, e, checks


def verify(c, result, expected):
    assert result['success'] and result['hit_return']
    assert int(result['registers']['EAX'], 16) == 1
    assert not result['protected_write_or_execution_failures']
    assert all(r['returned'] for r in result['before_calls'])
    actor = bytes.fromhex(result['captured_hex'])
    assert len(actor) == 1343+256 and actor[1343:] == bytes([0xA5])*256
    for offset, size, value in [(0x11C, 2, expected['speed']), (0x206, 4, expected['controller_max']),
                                 (0x3B3, 4, expected['work_enhancement'])]:
        assert int.from_bytes(actor[offset:offset+size], 'little') == value, (c['name'], hex(offset), actor[offset:offset+size].hex(), value)
    assert int.from_bytes(actor[0x40:0x44], 'little') == 1000
    assert list(struct.unpack_from('<3I', actor, 0x1A)) == [0x5800, 0x6800, 0x2000]
    assert int.from_bytes(actor[0x25E:0x262], 'little') == 89
    visits = result['visited_instructions']
    assert all(hex(a) in visits for a in [0x488930, 0x48AB1F, 0x59C140, 0x594E10, 0x575D70])
    if 0 < expected['fairy_effective'] < 128:
        assert all(hex(a) in visits for a in [0x4898B3, 0x48D010])
        writes = result['observed_writes']
        speeds = [r['value'] for r in writes if int(r['address'], 16) == cf.ACTOR+0x11C]
        maxima = [r['value'] for r in writes if int(r['address'], 16) == cf.ACTOR+0x206]
        assert speeds == [expected['transient_speed'], expected['speed']], (c['name'], speeds)
        assert maxima == [expected['transient_controller_max'], expected['controller_max']], (c['name'], maxima)
    if expected['wizard_effective'] > 0 and expected['wizard_effective'] < 128:
        assert hex(0x4895FE) in visits or hex(0x4895CD) in visits


def main():
    selected = cases()
    if len(sys.argv) > 1:
        selected = [c for c in selected if c['name'] == sys.argv[1]]
        assert selected
    rows = []
    backend_sha = hashlib.sha256((ROOT/'native_unicorn_tick.py').read_bytes()).hexdigest()
    for c in selected:
        args, expected, checks = build(c)
        tag = hashlib.sha256((json.dumps(args, sort_keys=True)+backend_sha).encode()).hexdigest()[:10]
        path = ROOT/'ghidra'/f'phase107-tick-{c["name"]}-{tag}.json'
        if path.exists():
            value = json.loads(path.read_text(encoding='utf8'))
            assert value['input'] == args and value['expected'] == expected
        else:
            result = cpu.run(EXE, args, list(checks.items()), f.OBS, cf.ACTOR, 1343+256)
            value = dict(case=c, input=args, expected=expected, result=result)
            path.write_text(json.dumps(value, indent=2), encoding='utf8')
        verify(c, value['result'], expected)
        rows.append(dict(case=c['name'], file=path.name, sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
        print(c['name'], expected, 'verified', flush=True)
    if len(selected) == len(cases()):
        (ROOT/'phase107-crypt-tick-emulations.json').write_text(json.dumps(dict(scope=__doc__, replays=rows), indent=2), encoding='utf8')


if __name__ == '__main__':
    main()
