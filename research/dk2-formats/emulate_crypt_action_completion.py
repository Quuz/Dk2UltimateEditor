"""Strict phase189 continuation and animation-end controls on original code."""
from pathlib import Path
import json
from capstone import Cs, CS_ARCH_X86, CS_MODE_32
import probe_crypt_action_completion as probe
from emulate_resistance import post_context

ROOT = Path(__file__).resolve().parent
p, EXE, sha, u = probe.p, probe.EXE, probe.sha, probe.u


def cases():
    return probe.cases() + [
        dict(name='80-elapsed-200', clocks=[200, 205], flags=6, status=3, count=0),
        dict(name='80-clock-wrap-dispatch-only', clocks=[0xFFFFFFFF, 0, 4], flags=6, status=3, count=0, ai=False),
    ]


def execute(c):
    args, checks, saved, file, _ = probe.execute(c)
    return args, checks, saved, file


def snapshots(c, post):
    offsets = probe.build(c)[2]
    rows = []
    for i in range(6):
        data = post[probe.LOG][i*probe.STRIDE:(i+1)*probe.STRIDE]
        assert u(data, 0) == i
        end = max(o+n for o, n, _ in offsets.values())
        assert data[end:] == bytes([165]) * (probe.STRIDE-end)
        rows.append({name: data[o:o+n] for name, (o, n, _) in offsets.items()})
    return rows


def verify(c, args, checks, r):
    assert r['success'] and r['hit_return'] and not r['emulation_error'], (c['name'], r['emulation_error'], r['trace_tail'])
    assert not r['protected_write_or_execution_failures'] and r['function_or_vtable_substitutions'] == 0
    assert len(r['before_calls']) == len(args['before_calls']) and all(x['returned'] for x in r['before_calls'])
    md = Cs(CS_ARCH_X86, CS_MODE_32); md.detail = True; blob = EXE.read_bytes()
    for x in r['observed_reads'] + r['observed_writes']:
        if int(x['address'], 16) >= 4096:
            continue
        ins = next(md.disasm(p.pe_read(blob, int(x['pc'], 16), 15), int(x['pc'], 16)))
        assert int(x['address'], 16) == 0 and x['size'] == 4 and 0x64 in ins.prefix and 'fs:[0]' in ins.op_str, (c['name'], x)
    post = post_context(checks, r); rows = snapshots(c, post)
    assert u(post[probe.old.old.old.prior.readable.COUNTER], 4) == 0
    assert post[probe.old.old.old.UNRELATED] == checks[probe.old.old.old.UNRELATED]
    tables = post[probe.TABLE]
    assert tables.hex() == '000000000000000000000000ffffffff4c0000001c0300000000000001a09c4900000000000000000000000000500000000500000000d7000001'
    # Values are observed after original4950C0. In particular byte+1B is the
    # dispatcher duration; byte+14 is an animation selector, not a timeout.
    assert u(tables, 29) == 0x499CA0 and u(tables, 29+4) == 0 and u(tables, 29+21) == 0 and tables[29+27] == 0
    assert u(tables, 0) == 0 and u(tables, 21) == 3 and tables[20] == 28
    returns = [u(post[probe.RETURNS], 4*i) for i in range(len(c['clocks']))]
    assert returns == [1] * len(c['clocks']) and post[probe.RETURNS][4*len(returns):] == bytes([165])*(32-4*len(returns))
    assert [u(row['actor'], 0x25E) for row in rows[:4]] == [274, 6, 275, 275]
    assert [u(row['actor'], 0x3A9) for row in rows[:4]] == [0, 0, 80, 80]
    assert [u(row['actor'], 0x40) for row in rows[:4]] == [900, 900, 900, 905]
    assert u(rows[3]['room'], 0x53, 2) == 101
    control = c.get('control_action') or c.get('stop')
    expected_action = 76 if c['name'] == '76-animation-in-progress' else 0 if control else 80
    expected_hp = 905 if control else 910 if c['name'] == '80-boundary' else 915
    # This diagnostic additionally exercises AI across a clock wrap; its
    # separate exit is preserved rather than silently forcing membership.
    exits = c['name'] == '80-clock-wrap'
    for row in rows[4:]:
        assert u(row['actor'], 0x3A9) == expected_action
    assert u(rows[5]['actor'], 0x40) == expected_hp, (c['name'], u(rows[5]['actor'], 0x40), expected_hp)
    assert u(rows[5]['recipient'], 0x40) == expected_hp
    assert u(rows[5]['room'], 0x53, 2) == (0 if exits else 101), (c['name'], u(rows[5]['room'], 0x53, 2))
    assert u(rows[5]['actor'], 0x3B9, 2) == (0 if exits else 300)
    assert u(rows[5]['actor'], 0x25E) == (1 if exits else 275), (c['name'], u(rows[5]['actor'], 0x25E))
    for row in rows:
        for key in ['actor', 'recipient']:
            assert u(row[key], 0x12) == 0 and u(row[key], 0x40) > 0
    for tag in [100, 101, 102]:
        assert post[0x6ED540][tag] == 1
    owner = u(checks[probe.ACTOR], 0x2A)
    counts = [0]*18
    if not exits: counts[0] = 1
    assert list(post[owner][0x108A:0x109C]) == counts
    assert not any(int(x['address'], 16) == 0x476D30 and x['stack_args'] == [1, 0] for x in args['before_calls'])
    for address in [p.target.RAW, probe.old.old.old.prior.RAWS, probe.old.old.old.VARIABLE, 0x66D524, 0x66D654, 0x66D754, 0x66D86C, 0x66E3EC, 0x670574]:
        assert post[address] == checks[address], (c['name'], hex(address))
    visits = lambda address: r['visited_instructions'].get(hex(address), 0)
    # The new controls change status/clock only, and start/stop actions solely
    # through the real API. Initial Job28/registration/action80 are native.
    assert visits(0x478800) >= 1 and visits(0x483AC0) >= 1 and visits(0x483870) >= 1
    summary = dict(final_state=u(rows[5]['actor'], 0x25E), final_action=expected_action, HP=expected_hp,
                   final_registered=u(rows[5]['room'], 0x53, 2), dispatcher_returns=returns,
                   both_Creatures_alive=True, actor_live_registry=True, source_and_vtables_preserved=True,
                   original_AI_dispatches=visits(0x476FC0), original_action_dispatches=visits(0x496AA0),
                   Crypt_callbacks=visits(0x499CA0), native_steps=r['instruction_count'],
                   snapshots=[dict(i=i, state=u(row['actor'], 0x25E), action=u(row['actor'], 0x3A9), HP=u(row['actor'], 0x40),
                                   registered=u(row['room'], 0x53, 2)) for i, row in enumerate(rows)])
    return post, summary


def main():
    rows = []
    for c in cases():
        args, checks, saved, file = execute(c)
        _, out = verify(c, args, checks, saved['result'])
        rows.append(dict(case=c, file=str(file.relative_to(ROOT)), sha256=sha(file.read_bytes()), outcome=out))
        print(c['name'], out['final_state'], out['final_action'], out['HP'], flush=True)
    (ROOT/'phase189-crypt-completion-emulations.json').write_text(json.dumps(dict(scope=__doc__, replays=rows), indent=2), encoding='utf-8')


if __name__ == '__main__':
    main()
