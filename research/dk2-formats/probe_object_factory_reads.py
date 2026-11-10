"""Originalphase89 factory with realdefinitionread tracing;explicitexternalIO.

Only the existing nullGFX/fixedsound providers are allowed outside originalPE.
No gameplay function/vtable change. UnknownManaGen meaning not assumed.
"""
from pathlib import Path
import hashlib
import json
import sys
import emulate_object_factory as f
import native_unicorn_factorytrace as cpu
from emulate_element_colors import region
from build_effect_semantics import EXE

ROOT = Path(__file__).resolve().parent


def build(mana=0, flags=0):
    case = f.cases()[0]
    args, expected = f.build(case, 0)
    allmem = {int(r['address'], 16): bytearray.fromhex(r['hex']) for r in json.loads(args['memory'])['regions']}
    memory = {a: b for a, b in allmem.items() if (a < 0x11000000 or a == 0x7FFEF000) and a != f.OBS}
    f.put(memory[f.DEFINITION], 0x106, mana, 2)
    f.put(memory[f.DEFINITION], 0xF4, flags)
    checks = [(a, bytes(b)) for a, b in memory.items() if a not in [0x7FFEF000, f.GFX_CB, f.SOUND_CB]]
    args.update(memory=json.dumps(dict(regions=[region(a, b) for a, b in memory.items()])),
                observe_reads=[[f.DEFINITION, 285]],
                allowed_external_IO_code=[[a, len(memory[a])] for a in [f.GFX_CB, f.SOUND_CB]])
    return args, expected, checks


def main():
    mana = int(sys.argv[1], 0) if len(sys.argv) > 1 else 0
    flags = int(sys.argv[2], 0) if len(sys.argv) > 2 else 0
    args, expected, checks = build(mana, flags)
    tag = hashlib.sha256(json.dumps(args, sort_keys=True).encode()).hexdigest()[:10]
    result = cpu.run(EXE, args, checks, f.OBS, expected['node'], 393)
    path = ROOT/'ghidra'/f'phase109-factory-read-diagnostic-{tag}.json'
    path.write_text(json.dumps(dict(scope=__doc__, mana=mana, flags=flags, input=args, expected=expected, result=result), indent=2), encoding='utf8')
    print(json.dumps({k: v for k, v in result.items() if k not in ['captured_hex', 'visited_instructions', 'context_mismatches', 'observed_writes', 'observed_reads', 'stack_at_stop_hex']}))
    print('DefinitionReads', result['observed_reads'])
    if result['success']:
        f.verify_node(f.cases()[0], bytes.fromhex(result['captured_hex']), expected)
        print('CompleteObject snapshot basicfields verified')


if __name__ == '__main__':
    main()
