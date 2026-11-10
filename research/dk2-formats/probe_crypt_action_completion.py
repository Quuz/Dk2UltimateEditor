"""Phase189: original Crypt action80 versus animation completion signals.

Reuse frozen phase188 full raw preference/AI/ritual scene. The new declared
control changes only clock and explicit runtime animation status conditions.
Original action76/stop APIs provide causal controls; no game code is replaced.
"""
from pathlib import Path
import json, struct
import probe_crypt_job_selection as old
from emulate_element_colors import region
from emulate_resistance import post_context

ROOT = Path(__file__).resolve().parent
p, EXE, sha, u = old.p, old.EXE, old.sha, old.u
ACTOR, ROOM, LOG, STRIDE = old.old.ACTOR, old.old.ROOM, old.old.LOG, old.old.STRIDE
CONTROL, RETURNS, TABLE = 0x29000000, 0x29010000, 0x29011000


def cases():
    return [
        dict(name='80-boundary', clocks=[104, 105], flags=0, status=0, count=2),
        dict(name='80-animation-end', clocks=[105, 110], flags=6, status=0, count=0),
        dict(name='80-all-completed-no-model', clocks=[105, 110], flags=6, status=3, count=0, no_model=True),
        dict(name='80-all-bits', clocks=[105, 110], flags=255, status=255, count=255, no_model=True),
        dict(name='80-clock-wrap', clocks=[0xFFFFFFFF, 0, 4], flags=6, status=3, count=0),
        dict(name='76-animation-in-progress', clocks=[105], flags=0, status=0, count=2, control_action=76, ai=False),
        dict(name='76-animation-end', clocks=[105], flags=6, status=0, count=0, control_action=76, ai=False),
        dict(name='76-completed-bits', clocks=[105], flags=0, status=3, count=2, control_action=76, ai=False),
        dict(name='80-explicit-stop', clocks=[105], flags=6, status=3, count=0, stop=True, ai=False),
    ]


def build(c):
    args, checks, offsets = old.build(dict(name='completion-base', method='AI', raw_job=28, mood_mode=1))
    mem = {int(r['address'], 16): bytearray.fromhex(r['hex']) for r in json.loads(args['memory'])['regions']}
    calls = args['before_calls']
    snap3 = next(i for i, call in enumerate(calls) if int(call['address'], 16) == old.old.CONTROL + 3 * 256)
    calls = calls[:snap3 + 1]
    # Keep the first four accepted phase188 checkpoints. There is no requested
    # category exit: the original participant remains registered in Crypt.
    assert not any(x['address'] == '0x476d30' and x['stack_args'] == [1, 0] for x in calls)
    mem[RETURNS] = bytearray([165]) * 32
    mem[TABLE] = bytearray([165]) * 58
    checks[RETURNS], checks[TABLE] = bytes(mem[RETURNS]), bytes(mem[TABLE])
    controls = []
    session = u(mem[p.f.WORLD], 4)

    def helper(code):
        address = CONTROL + len(controls) * 256
        mem[address] = bytearray(code + b'\xc3')
        controls.append([address, len(mem[address])])
        calls.append(dict(address=hex(address), registers=dict(ECX='0'), stack_args=[]))

    def native(address, ecx, stack=()):
        calls.append(dict(address=hex(address), registers=dict(ECX=hex(ecx)), stack_args=list(stack)))

    # Observe the actual initializer output; table bytes are never supplied.
    code = bytearray(bytes.fromhex('9c60fc'))
    for i, action in enumerate([76, 80]):
        code += b'\xbe' + struct.pack('<I', 0x6E4198 + 29 * action)
        code += b'\xbf' + struct.pack('<I', TABLE + 29 * i) + b'\xb9' + struct.pack('<I', 29) + bytes.fromhex('f3a4')
    code += bytes.fromhex('619d')
    helper(code)
    if c.get('control_action'):
        native(0x4966A0, ACTOR + 0x38D)
        native(0x496700, ACTOR, [c['control_action'], 0, 0, 0])
    if c.get('stop'):
        native(0x4966A0, ACTOR + 0x38D)
    for index, clock in enumerate(c['clocks']):
        code = bytearray(b'\xc7\x05' + struct.pack('<II', session + 4, clock))
        for off, value in [(0xF1, c['flags']), (0x3AD, c['status']), (0x3AE, c['count'])]:
            code += b'\xc6\x05' + struct.pack('<I', ACTOR + off) + bytes([value])
        if c.get('no_model'):
            code += b'\xc7\x05' + struct.pack('<II', ACTOR + 0xDC, 0)
        # The original dispatcher receives ECX=Creature; capture its return
        # before any snapshot helper, rather than mistaking helper EAX for it.
        code += b'\xb9' + struct.pack('<I', ACTOR) + b'\xb8' + struct.pack('<I', 0x496AA0) + bytes.fromhex('ffd0a3') + struct.pack('<I', RETURNS + 4 * index)
        helper(code)
        if c.get('ai', True):
            native(0x476FC0, old.old.CONTEXT)
        snap = 4 if index == 0 else 5
        calls.append(dict(address=hex(old.old.CONTROL + snap * 256), registers=dict(ECX='0'), stack_args=[]))
    if len(c['clocks']) == 1:
        calls.append(dict(address=hex(old.old.CONTROL + 5 * 256), registers=dict(ECX='0'), stack_args=[]))
    args['before_calls'] = calls
    args['allowed_external_IO_code'] += controls
    args['observe_reads'] += [[ACTOR, len(mem[ACTOR])]]
    args['observe_writes'] += [[RETURNS, 32], [TABLE, 58]]
    adapter = old.old.old.prior.readable
    mem[adapter.ADDRESS] = bytearray(adapter.adapter(adapter.allowed_ranges(EXE.read_bytes(), [region(a, b) for a, b in mem.items() if a != adapter.ADDRESS])))
    args['allowed_external_IO_code'] = [r for r in args['allowed_external_IO_code'] if r[0] != adapter.ADDRESS] + [[adapter.ADDRESS, len(mem[adapter.ADDRESS])]]
    args['memory'] = json.dumps(dict(regions=[region(a, b) for a, b in mem.items()]))
    return args, checks, offsets


def execute(c):
    args, checks, offsets = build(c)
    key = sha((json.dumps(args, sort_keys=True) + sha(Path(__file__).read_bytes())).encode())[:12]
    file = ROOT / 'ghidra' / f'phase189-crypt-completion-{c["name"]}-{key}.json'
    if file.exists():
        saved = json.loads(file.read_text(encoding='utf-8'))
        assert saved['input'] == args
    else:
        result = old.old.old.prior.old.base.cpu.run(EXE, args, list(checks.items()), p.f.OBS, ACTOR, len(checks[ACTOR]))
        saved = dict(case=c, input=args, result=result)
        file.write_text(json.dumps(saved, indent=2), encoding='utf-8')
    return args, checks, saved, file, offsets


def describe(c):
    args, checks, saved, file, offsets = execute(c)
    r = saved['result']; post = post_context(checks, r)
    snaps = []
    for i in range(6):
        data = post[LOG][i * STRIDE:(i + 1) * STRIDE]
        a, n, _ = offsets['actor']; actor = data[a:a+n]
        q, n, _ = offsets['room']; room = data[q:q+n]
        snaps.append(dict(i=i, state=u(actor, 0x25E), action=u(actor, 0x3A9), HP=u(actor, 0x40),
                          flags=actor[0xF1], status=actor[0x3AD], count=actor[0x3AE], registered=u(room, 0x53, 2)))
    out = dict(case=c, success=r['success'], error=r['emulation_error'], failures=r['protected_write_or_execution_failures'],
               snapshots=snaps, tables=post[TABLE].hex(), native_steps=r['instruction_count'], file=str(file.relative_to(ROOT)), tail=r['trace_tail'][-8:])
    print(json.dumps(out), flush=True)
    return out


def main():
    rows = [describe(c) for c in cases()]
    (ROOT / 'phase189-crypt-completion-discovery.json').write_text(json.dumps(dict(replays=rows), indent=2), encoding='utf-8')


if __name__ == '__main__':
    main()
