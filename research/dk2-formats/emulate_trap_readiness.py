"""Full readiness, clock and mana helpers in two EXEs; no native code patched."""
from pathlib import Path
import asyncio
import hashlib
import json
import os
import struct
from mcp import ClientSession, StdioServerParameters
from mcp.client.stdio import stdio_client
from native_trap_readiness import *
from emulate_element_colors import region, pack32
from emulate_hero_parties import compare_code

ROOT = Path(__file__).resolve().parent


def cases():
    variants = [
        ('insufficient', dict(mana_cost=1, mana=0)),
        ('zero-cost', dict(mana_cost=0, mana=0x80000000)),
        ('equal-mana', dict(mana_cost=1, mana=1)),
        ('above-mana', dict(mana_cost=2, mana=1)),
        ('negative-cost', dict(mana_cost=0xFFFFFFFF, mana=0)),
        ('minimum-cost', dict(mana_cost=0x80000000, mana=0)),
        ('negative-mana', dict(mana_cost=0x7FFFFFFF, mana=0x80000000)),
        ('state-one', dict(state_word=1)),
        ('state-high', dict(state_word=65535)),
        ('flag-off', dict(definition_flags=0, mana_cost=0x7FFFFFFF, mana=0)),
        ('exempt-owner', dict(owner=255, exempt_owner=255, mana_cost=0x7FFFFFFF, mana=0)),
        ('instance-flag', dict(instance_flags=0x20)),
        ('future', dict(deadline=101, tick=100)),
        ('deadline-equal', dict(deadline=100, tick=100)),
        ('deadline-high', dict(deadline=0xFFFFFFFF, tick=0)),
        ('tick-high', dict(deadline=0, tick=0xFFFFFFFF)),
        ('early-null', dict(instance_flags=0x20, null_pointers=True)),
    ]
    variants += [(f'unknown-{n}', dict(unknown=n)) for n in [0, 1, 32768, 65535]]
    out = []
    for sha in PROFILES:
        for label, values in variants:
            c = dict(name=f'{sha[:3]}-{label}', sha=sha, instance_flags=0, deadline=100, tick=100,
                     definition_flags=0x100, owner=3, exempt_owner=255, mana_cost=10, mana=20, state_word=0, unknown=0x1234)
            c.update(values)
            out.append(c)
    return out


def build_case(c):
    p = PROFILES[c['sha']]
    actor = bytearray((i*31+7)&255 for i in range(512))
    definition = bytearray((i*37+13)&255 for i in range(209))
    owner = bytearray((i*41+19)&255 for i in range(512))
    null = c.get('null_pointers', False)
    actor[p['instance_flags']] = c['instance_flags']
    struct.pack_into('<I', actor, p['deadline'], c['deadline'])
    struct.pack_into('<I', actor, p['definition'], 0 if null else 0x10001000)
    struct.pack_into('<H', actor, p['state_word'], c['state_word'])
    struct.pack_into('<H', actor, 0x28, c['owner'])
    struct.pack_into('<I', actor, 0x2A, 0 if null else 0x10002000)
    struct.pack_into('<I', definition, 0x88, c['definition_flags'])
    struct.pack_into('<H', definition, 0x98, c['unknown'])
    struct.pack_into('<I', definition, 0x9A, c['mana_cost'])
    struct.pack_into('<I', owner, 0x82, c['mana'])
    clock = pack32(0) + pack32(0x10003100)
    frame = pack32(0) + pack32(c['tick'])
    values = {k:c[k] for k in ['instance_flags', 'deadline', 'tick', 'definition_flags', 'owner', 'exempt_owner', 'mana_cost', 'mana', 'state_word']}
    expected = return_code(**values)
    # Save native EAX before the comparison observer overwrites it.
    observer = b'\xA3'+pack32(0x1000B000)+b'\xBD'+pack32(1)
    mem = []
    for actual, previous in [(0x10000000, actor), (0x10001000, definition), (0x10002000, owner), (0x10003000, clock), (0x10003100, frame), (0x1000B000, pack32(expected))]:
        wanted = actual+0x10000
        mem += [region(actual, bytes(4) if actual == 0x1000B000 else previous), region(wanted, previous)]
        observer += compare_code(actual, wanted, len(previous)) + bytes.fromhex('21c5')
    observer += b'\x68'+pack32(0xDEADBEEF)+b'\xC3'
    mem += [region(0x1000C000, observer), region(p['clock_global'], pack32(0 if null else 0x10003000)),
            region(p['exempt_owner'], struct.pack('<H', c['exempt_owner'])), region(0x7FFF0000, pack32(0x1000C000))]
    spans = sorted((int(r['address'],16), len(bytes.fromhex(r['hex']))) for r in mem)
    assert all(a+n <= b for (a,n),(b,_) in zip(spans,spans[1:]))
    args = dict(program=p['program'], address=hex(p['entry']), max_steps=8000,
                registers=json.dumps(dict(ECX='0x10000000')), memory=json.dumps(dict(regions=mem)),
                return_registers='EAX,ECX,EBP,EIP,ESP')
    return args, dict(EAX=1, ECX=0, EBP=1), expected


async def main():
    params = StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),
        args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],
        env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    rows = []
    async with stdio_client(params) as (rd, wr):
        async with ClientSession(rd, wr) as session:
            await session.initialize()
            for c in cases():
                args, want, native = build_case(c)
                path = ROOT/'ghidra'/f'phase68-emulation-trap-readiness-{c["name"]}.json'
                if path.exists():
                    saved = json.loads(path.read_text(encoding='utf8'))
                    assert saved['input'] == args and saved['expected_native_return'] == native
                else:
                    response = await session.call_tool('emulate_function', args)
                    r = json.loads('\n'.join(x.text for x in response.content if x.type=='text'))
                    if response.isError or 'error' in r:
                        raise RuntimeError(str(r))
                    saved = dict(case=c, input=args, expected_registers=want, expected_native_return=native, result=r.get('data',r))
                    path.write_text(json.dumps(saved,indent=2),encoding='utf8')
                r = saved['result']
                assert r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in want.items()), (c['name'],r,native)
                rows.append(dict(file=path.name,sha256=hashlib.sha256(path.read_bytes()).hexdigest(),case=c['name']))
                print(c['name'],'verified',flush=True)
    (ROOT/'phase68-trap-readiness-emulations.json').write_text(json.dumps(dict(cases=rows,scope=__doc__),indent=2),encoding='utf8')


if __name__=='__main__':
    asyncio.run(main())
