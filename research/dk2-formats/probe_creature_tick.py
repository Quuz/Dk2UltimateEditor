"""Diagnostic whole original488930 with prepared source; no replacements.

This is discovery evidence, not a verified tick until its context is understood.
Uses the frozen phase103 CPU backend and saves complete actor plus memory diffs.
"""
from pathlib import Path
import json
import hashlib
import struct
import sys
import emulate_possession_speed as sp
import emulate_creature_factory as cf
import emulate_object_factory as f
from build_effect_semantics import EXE, pe_read
import native_unicorn_tick as cpu
from emulate_element_colors import region, pack32

ROOT = Path(__file__).resolve().parent


def build(tables=False, navigation=False, graphics=False):
    args, _ = sp.build(dict(sp.cases()[0], possessed=False, base_speed=1000))
    memory = {int(r['address'], 16): bytearray.fromhex(r['hex']) for r in json.loads(args['memory'])['regions']
              if int(r['address'], 16) < 0x11000000 and int(r['address'], 16) not in [f.OBS, sp.DRIVER, 0x7FFEE000]}
    f.put(memory[sp.VARS], 0x40, 20)
    if graphics:
        # Exact CPCEngineInterface slot64 is original RET10, not a replacement.
        assert pe_read(EXE.read_bytes(), 0x670574+0x64, 4) == pack32(0x5671D0)
        f.put(memory[f.GV], 0x64, 0x5671D0)
        assert pe_read(EXE.read_bytes(), 0x670574+0x80, 4) == pack32(0x59C140)
        f.put(memory[f.GV], 0x80, 0x59C140)
    if tables:
        from spell_state_tables import BASE as SB, initialized_states
        from creature_category_tables import BASE as CB, initialized_categories
        from portal_animation_tables import BASE as AB, initialized_animations
        for address, function, expected in [(SB, 0x47BBE0, initialized_states()[0]),
                                             (CB, 0x4798F0, initialized_categories()[0]),
                                             (AB, 0x4950C0, initialized_animations())]:
            initial = bytearray(pe_read(EXE.read_bytes(), address, len(expected))) if address == CB else bytearray([0xA5])*len(expected)
            stack = bytearray([0xA5])*16384
            struct.pack_into('<I', stack, 8192, f.OBS)
            call_args = dict(args, address=hex(function), registers=json.dumps(dict(ECX='0')),
                             memory=json.dumps(dict(regions=[region(address, initial), region(0x7FFEE000, stack)])),
                             max_steps=200000)
            path = ROOT/'ghidra'/f'phase107-initialize-{function:x}{"-pe-seed" if address == CB else ""}.json'
            if path.exists():
                value = json.loads(path.read_text(encoding='utf8'))
                assert value['input'] == call_args
                result = value['result']
            else:
                result = cpu.run(EXE, call_args, [], f.OBS, address, len(expected))
                path.write_text(json.dumps(dict(input=call_args, result=result), indent=2), encoding='utf8')
            assert result['success'] and result['hit_return'], result
            actual = bytes.fromhex(result['captured_hex'])
            assert actual == expected
            memory[address] = bytearray(actual)
    if navigation:
        address = 0x6ED408
        expected = bytearray([0xA5])*20
        f.put(expected, 0, 0x4D16B0)
        f.put(expected, 16, f.WORLD)
        stack = bytearray([0xA5])*16384
        struct.pack_into('<3I', stack, 8192, f.OBS, 0x4D16B0, f.WORLD)
        call_args = dict(args, address='0x4d27d0', registers=json.dumps(dict(ECX='0')),
                         memory=json.dumps(dict(regions=[region(address, bytes([0xA5])*20), region(0x7FFEE000, stack)])))
        path = ROOT/'ghidra/phase107-initialize-navigation.json'
        if path.exists():
            value = json.loads(path.read_text(encoding='utf8'))
            assert value['input'] == call_args
            result = value['result']
        else:
            result = cpu.run(EXE, call_args, [], f.OBS, address, 20)
            path.write_text(json.dumps(dict(input=call_args, result=result), indent=2), encoding='utf8')
        assert result['success'] and result['hit_return'] and bytes.fromhex(result['captured_hex']) == expected, result
        memory[address] = expected
        memory[0x6ECAE0] = bytearray(8)
    stack = bytearray([0xA5])*16384
    struct.pack_into('<I', stack, 8192, f.OBS)
    checks = [(a, bytes(b)) for a, b in memory.items() if a not in [f.GFX_CB, f.SOUND_CB, 0x28CFA4]]
    memory[0x7FFEE000] = stack
    args.update(address='0x488930', registers=json.dumps(dict(ECX=hex(cf.ACTOR))),
                memory=json.dumps(dict(regions=[region(a, b) for a, b in memory.items()])), max_steps=200000)
    return args, checks


def main():
    args, checks = build(tables='tables' in sys.argv, navigation='navigation' in sys.argv, graphics='graphics' in sys.argv)
    result = cpu.run(EXE, args, checks, f.OBS, cf.ACTOR, 1343+256)
    tag = hashlib.sha256((json.dumps(args, sort_keys=True)+(ROOT/'native_unicorn_tick.py').read_text(encoding='utf8')).encode()).hexdigest()[:10]
    path = ROOT/'ghidra'/f'phase107-whole-tick-diagnostic-{tag}.json'
    path.write_text(json.dumps(dict(scope=__doc__, input=args, result=result), indent=2), encoding='utf8')
    print(json.dumps({k: v for k, v in result.items() if k not in ['visited_instructions', 'captured_hex', 'context_mismatches']}))
    print('Differences', json.dumps(result['context_mismatches'])[:6000])


if __name__ == '__main__':
    main()
