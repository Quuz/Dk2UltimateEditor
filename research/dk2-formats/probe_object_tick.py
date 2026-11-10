"""Original Object tick discovery: valid class snapshot, unknown-definition reads."""
from pathlib import Path
import hashlib
import json
import struct
import sys
import probe_creature_tick as parent
import emulate_creature_factory as cf
import emulate_object_factory as f
import native_unicorn_fieldtrace as cpu
from emulate_element_colors import region
from build_effect_semantics import EXE

ROOT = Path(__file__).resolve().parent


def build(flags=0, mana=0, hp=1000, in_room=False, pause=False):
    args, _ = parent.build(True, True, True)
    memory = {int(r['address'], 16): bytearray.fromhex(r['hex']) for r in json.loads(args['memory'])['regions']}
    memory[cf.ACTOR] = bytearray((ROOT/'ghidra/phase89-object-factory-base.bin').read_bytes())+bytearray([0xA5])*256
    for off, value in [(0x2E, 0), (0x32, 0), (0x40, hp)]:
        f.put(memory[cf.ACTOR], off, value)
    f.put(memory[cf.ACTOR], 0x138, int(in_room), 2)
    if pause:
        memory[cf.ACTOR][0x146] |= 0x10
    f.put(memory[f.DEFINITION], 0xF4, flags)
    f.put(memory[f.DEFINITION], 0x106, mana, 2)
    f.put(memory[f.DEFINITION], 0x119, 0, 2)
    args.update(address='0x49bd20', registers=json.dumps(dict(ECX=hex(cf.ACTOR))),
                observe_reads=[[f.DEFINITION, 285]], observe_writes=[[cf.ACTOR, 393]],
                memory=json.dumps(dict(regions=[region(a, b) for a, b in memory.items()])))
    checks = [(a, bytes(b)) for a, b in memory.items() if a not in [0x7FFEE000, f.GFX_CB, f.SOUND_CB, 0x28CFA4]]
    return args, checks


def main():
    flags = int(sys.argv[1], 0) if len(sys.argv) > 1 else 0
    args, checks = build(flags=flags)
    tag = hashlib.sha256(json.dumps(args, sort_keys=True).encode()).hexdigest()[:10]
    path = ROOT/'ghidra'/f'phase108-object-tick-diagnostic-{tag}.json'
    result = cpu.run(EXE, args, checks, f.OBS, cf.ACTOR, 393+256)
    path.write_text(json.dumps(dict(scope=__doc__, input=args, result=result), indent=2), encoding='utf8')
    print(json.dumps({k: v for k, v in result.items() if k not in ['captured_hex', 'visited_instructions', 'context_mismatches', 'observed_writes', 'observed_reads', 'stack_at_stop_hex']}))
    print('DefinitionReads', result['observed_reads'])
    print('Changes', result['context_mismatches'])


if __name__ == '__main__':
    main()
