"""Complete Objectfactory -> original Player mana tick/cost helper,oneCPU.

UnknownObject106 traced. Existing phase89 pool/registration expectations and
full393byte snapshot are reused,not re-counted as newformatmapping. Explicit
nullGFX/fixedsound external providers only;gameplay functions stay original.
"""
from pathlib import Path
import hashlib
import json
import struct
import sys
import emulate_object_factory as f
import probe_object_factory_reads as source
import native_unicorn_factorytrace as cpu
from build_effect_semantics import EXE
from emulate_element_colors import region

ROOT = Path(__file__).resolve().parent
VARS = 0x6F2550


def cases():
    base = dict(mana_gen=0, current=1000, extra=20, basic=100, ceiling=500,
                capacity=10000, rate=1, last=0, now=100, tps=20,
                upkeep=0, one_gain=0, one_loss=0, mode=1, setting=1)
    changes = [('base', {}), ('object-mana100', dict(mana_gen=100)),
               ('object-manaffff', dict(mana_gen=65535)),
               ('positive-rate2', dict(rate=2)), ('zero-rate', dict(rate=0)),
               ('income-ceiling', dict(ceiling=80)), ('stored-cap', dict(current=9990)),
               ('negative-income', dict(extra=-150)), ('upkeep-and-ceiling', dict(extra=-80, upkeep=100, ceiling=100)),
               ('deadline-wait', dict(last=81)), ('deadline-exact', dict(last=80)),
               ('one-shot-counters-reset', dict(one_gain=25, one_loss=15)),
               ('mode3-setting2', dict(mode=3, setting=2)), ('mode3-setting0', dict(mode=3, setting=0))]
    return [dict(base, **v, name=n) for n, v in changes]


def s32(n):
    n &= 0xFFFFFFFF
    return n if n < 0x80000000 else n-0x100000000


def outcome(c):
    due = c['now'] >= ((c['last']+c['tps'])&0xFFFFFFFF)
    delta = 0
    if due:
        gross = min(s32(c['extra']+c['basic']+c['upkeep']), c['ceiling'])
        delta = s32(gross-c['upkeep'])
        if delta > 0:
            delta = s32(delta*c['rate'])
        if c['mode'] == 3:
            if c['setting'] == 2:
                delta = s32(delta*2)
            elif c['setting'] == 0:
                n = s32(delta*2)
                delta = (n+(3 if n < 0 else 0))>>2
    current = s32(c['current']+delta)
    if due:
        current = max(0, current) if delta < 0 else min(current, c['capacity'])
    return dict(due=due, delta=delta, current=current,
                last=c['now'] if due else c['last'], one_gain=0 if due else c['one_gain'],
                one_loss=0 if due else c['one_loss'], player_flags=0x100 if due else 0)


def build(c):
    args, factory, _ = source.build(c['mana_gen'])
    memory = {int(r['address'], 16): bytearray.fromhex(r['hex']) for r in json.loads(args['memory'])['regions']}
    variables = bytearray(4096)
    f.put(variables, 0x40, c['tps'])
    memory[VARS] = variables
    memory[0x6E4154] = bytearray(struct.pack('<I', VARS))
    memory[0x6E5054] = bytearray(struct.pack('<I', f.WORLD))
    memory[0x75AD4B] = bytearray(struct.pack('<I', c['mode']))
    memory[0x75AEA7] = bytearray(struct.pack('<H', c['setting']))
    f.put(memory[f.SESSION], 4, c['now'])
    for off, value in [(0x82, c['current']), (0x86, c['rate']), (0x9A, c['last']), (0xAE, 0xFFFFFFFF),
                        (0x19A, 1), (0x10E2, c['extra']), (0x10E6, c['basic']), (0x10DE, c['ceiling']),
                        (0x10EA, c['capacity']), (0x10D6, c['upkeep']), (0x10EE, c['one_gain']), (0x10F2, c['one_loss'])]:
        f.put(memory[f.PLAYER], off, value)
    wanted = {a: bytearray(b) for a, b in memory.items() if a not in [0x7FFEF000, f.GFX_CB, f.SOUND_CB]}
    # Full pool/owner/map registration from the previously verified factory;
    # merge changed check bytes, retaining our added mana-context input fields.
    old_args, old_e = f.build(f.cases()[0], 0)
    allmem = {int(r['address'], 16): bytes.fromhex(r['hex']) for r in json.loads(old_args['memory'])['regions']}
    shadow = 0x11000000
    for row in old_e['checks']:
        address, n = int(row['address'], 16), row['bytes']
        expected = allmem[shadow]
        shadow += ((n+255)//256)*256
        original = next((blob[address-a:address-a+n] for a, blob in allmem.items() if a < 0x11000000 and a <= address and address+n <= a+len(blob)), None)
        assert original is not None and len(expected) == n
        for a, blob in wanted.items():
            if a <= address and address+n <= a+len(blob):
                for i, (before, after) in enumerate(zip(original, expected)):
                    if before != after:
                        blob[address-a+i] = after
                break
        else:
            raise AssertionError(hex(address))
    node = factory['node']
    image = (ROOT/'ghidra/phase89-object-factory-base.bin').read_bytes()
    wanted[f.BLOCK][node-f.BLOCK:node-f.BLOCK+393] = image
    # Factory return value is not stored by a synthetic observer in thisCPU.
    f.put(wanted[f.OUT], 0, 0)
    e = outcome(c)
    for off, value in [(0x82, e['current']), (0x9A, e['last']), (0x10EE, e['one_gain']),
                        (0x10F2, e['one_loss']), (0x76, e['player_flags'])]:
        f.put(wanted[f.PLAYER], off, value)
    args.update(address='0x4c10e0', registers=json.dumps(dict(ECX=hex(f.PLAYER))),
                before_calls=[dict(address='0x4b7a90', registers=dict(ECX=hex(f.WORLD+8)), stack_args=[17, 7, f.ARGS, f.OUT+4])],
                memory=json.dumps(dict(regions=[region(a, b) for a, b in memory.items()])))
    return args, e, wanted, node


def verify(c, args, expected, wanted, node, result):
    assert result['success'] and result['hit_return'] and not result['protected_write_or_execution_failures']
    assert len(result['before_calls']) == 1 and result['before_calls'][0]['returned']
    assert not result['context_mismatches'], (c['name'], result['context_mismatches'])
    assert bytes.fromhex(result['captured_hex']) == bytes(wanted[f.PLAYER])
    assert not any(int(r['address'], 16) < f.DEFINITION+0x108 and int(r['address'], 16)+r['size'] > f.DEFINITION+0x106 for r in result['observed_reads'])
    visits = result['visited_instructions']
    assert all(hex(a) in visits for a in [0x4B7A90, 0x4C10E0])
    if expected['due']:
        assert hex(0x4C17A0) in visits


def main():
    selected = cases()
    if len(sys.argv) > 1:
        selected = [c for c in selected if c['name'] == sys.argv[1]]
        assert selected
    rows = []
    backend = hashlib.sha256((ROOT/'native_unicorn_factorytrace.py').read_bytes()).hexdigest()
    for c in selected:
        args, expected, wanted, node = build(c)
        tag = hashlib.sha256((json.dumps(args, sort_keys=True)+backend).encode()).hexdigest()[:10]
        path = ROOT/'ghidra'/f'phase109-factory-mana-{c["name"]}-{tag}.json'
        if path.exists():
            value = json.loads(path.read_text(encoding='utf8'))
            assert value['input'] == args and value['expected'] == expected
        else:
            result = cpu.run(EXE, args, list(wanted.items()), f.OBS, f.PLAYER, len(wanted[f.PLAYER]))
            value = dict(case=c, input=args, expected=expected, node=node, result=result)
            path.write_text(json.dumps(value, indent=2), encoding='utf8')
        verify(c, args, expected, wanted, node, value['result'])
        rows.append(dict(case=c['name'], file=path.name, sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
        print(c['name'], expected, 'verified', flush=True)
    if len(selected) == len(cases()):
        (ROOT/'phase109-factory-mana-emulations.json').write_text(json.dumps(dict(scope=__doc__, replays=rows), indent=2), encoding='utf8')


if __name__ == '__main__':
    main()
