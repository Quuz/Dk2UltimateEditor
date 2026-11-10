"""Discovery of original raw Object loader -> complete factory -> idle-effect tick.

This probe is not accepted until all observed contexts have independent expectations.
External providers: inherited null graphics query and explicit null sound registration.
"""
import hashlib
import json
import struct
import sys
from pathlib import Path
import emulate_object_factory as f
import native_object_loading as loading
import emulate_object_states as states
import native_unicorn_factorytrace as cpu
from object_state_tables import BASE, SIZE
from emulate_element_colors import region
from build_effect_semantics import EXE, pe_read

ROOT = Path(__file__).resolve().parent
RAW, MID, VARS = 0x10090000, 0x10091000, 0x6F2550


def build(ident=101, ticks=0, start=None, mana=0, misc=None, tps=20):
    case = dict(f.cases()[0], ident=ident)
    args, expected = f.build(case, 0)
    allmem = {int(r['address'], 16): bytearray.fromhex(r['hex']) for r in json.loads(args['memory'])['regions']}
    memory = {a: b for a, b in allmem.items() if (a < 0x11000000 or a == 0x7FFEF000) and a != f.OBS}
    raw = bytearray(states.source_record(ident))
    if start is not None:
        raw[0x35B] = start
    struct.pack_into('<H', raw, 0x34C, mana)
    if misc is not None:
        struct.pack_into('<H', raw, 0x358, misc)
    memory[RAW], memory[MID], memory[BASE] = raw, bytearray([0xA5])*1022, bytearray(SIZE)
    memory[VARS] = bytearray(4096)
    f.put(memory[VARS], 0x40, tps)
    memory[0x6E4154] = bytearray(struct.pack('<I', VARS))
    memory[0x6FCDCF] = bytearray(struct.pack('<I', 1))
    # Sound remains an explicit external provider; a null return avoids fake handles.
    assert memory[f.SOUND_CB][6] == 0xB8
    memory[f.SOUND_CB][7:11] = bytes(4)
    for slot in [0x64, 0x80, 0x90]:
        f.put(memory[f.GV], slot, int.from_bytes(pe_read(EXE.read_bytes(), 0x670574+slot, 4), 'little'))
    calls = [dict(address='0x49e600', registers=dict(ECX='0'), stack_args=[]),
             dict(address='0x56d4f0', registers=dict(ECX=hex(MID)), stack_args=[]),
             dict(address='0x56d620', registers=dict(ECX=hex(MID)), stack_args=[RAW]),
             dict(address='0x49dd60', registers=dict(ECX=hex(f.DEFINITION)), stack_args=[MID, 0]),
             dict(address='0x4b7a90', registers=dict(ECX=hex(f.WORLD+8)), stack_args=[ident, 7, f.ARGS, f.OUT+4])]
    calls += [dict(address='0x49bd20', registers=dict(ECX=hex(expected['node'])), stack_args=[])
              for _ in range(max(0, ticks-1))]
    args.update(address=hex(0x49BD20 if ticks else 0x49EF80), registers=json.dumps(dict(ECX=hex(expected['node']))),
                before_calls=calls, max_steps=300000,
                memory=json.dumps(dict(regions=[region(a, b) for a, b in memory.items()])),
                allowed_external_IO_code=[[a, len(memory[a])] for a in [f.GFX_CB, f.SOUND_CB]],
                observe_reads=[[f.DEFINITION, 285]], observe_writes=[[expected['node'], 393], [f.PLAYER, 0x1200]])
    checks = {a: bytes(b) for a, b in memory.items() if a not in [0x7FFEF000, f.GFX_CB, f.SOUND_CB]}
    return args, expected, checks, case


def main():
    ident = int(sys.argv[1]) if len(sys.argv)>1 else 101
    ticks = int(sys.argv[2]) if len(sys.argv)>2 else 0
    args, expected, checks, case = build(ident, ticks)
    result = cpu.run(EXE, args, list(checks.items()), f.OBS, expected['node'], 393)
    tag = hashlib.sha256(json.dumps(args, sort_keys=True).encode()).hexdigest()[:10]
    path = ROOT/'ghidra'/f'phase111-vault-factory-diagnostic-{tag}.json'
    path.write_text(json.dumps(dict(input=args, expected=expected, result=result), indent=2), encoding='utf8')
    print(json.dumps({k:v for k,v in result.items() if k not in ['visited_instructions','captured_hex','context_mismatches','observed_reads','observed_writes','stack_at_stop_hex']}))
    if result['success']:
        actual = bytes.fromhex(result['captured_hex'])
        baseline = (ROOT/'ghidra/phase89-object-factory-base.bin').read_bytes()
        print('BASELINE DIFFS', [(hex(i), b, a) for i,(a,b) in enumerate(zip(actual,baseline)) if a != b])
    print('Changed contexts',[(r['address'],len(r['differences'])) for r in result['context_mismatches']])


if __name__ == '__main__':
    main()
