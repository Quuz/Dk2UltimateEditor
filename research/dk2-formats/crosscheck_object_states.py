"""Ghidra controls of original Object raw-loader/state/tick compositions."""
import asyncio
import hashlib
import json
import os
from pathlib import Path
from mcp import ClientSession, StdioServerParameters
from mcp.client.stdio import stdio_client
import emulate_object_states as native
from emulate_element_colors import region, pack32
from emulate_hero_parties import compare_code

ROOT = Path(__file__).resolve().parent
CONTROL_CASES = ['persistent16-two', 'zero-stub2-two', 'mana-vault-period']


def build(name):
    c = next(c for c in native.cases() if c['name'] == name)
    args, expected, _, wanted = native.build(c)
    regions = json.loads(args['memory'])['regions']
    # Initial original49E600 returns to this controlled caller. No gameplay stubs.
    driver = bytearray()
    calls = args['before_calls'][1:] + [dict(address=args['address'], registers=dict(ECX=hex(native.cf.ACTOR)), stack_args=[])]
    for call in calls:
        for value in reversed(call['stack_args']):
            driver += b'\x68' + pack32(value)
        driver += b'\xB9' + pack32(int(call['registers']['ECX'], 0))
        driver += b'\xB8' + pack32(int(call['address'], 0)) + bytes.fromhex('ffd0')
    driver += b'\xBD' + pack32(1) + b'\xBB' + pack32(0)
    shadow = 0x11000000
    for i, (a, b) in enumerate(wanted.items()):
        regions.append(region(shadow, b))
        driver += compare_code(a, shadow, len(b)) + bytes.fromhex('85c07505bb') + pack32(i + 1) + bytes.fromhex('21c5')
        shadow += ((len(b) + 255) // 256) * 256
    driver += b'\x68' + pack32(0xDEADBEEF) + b'\xC3'
    regions.append(region(native.f.OBS, driver))
    for key in ['before_calls', 'observe_reads', 'observe_writes']:
        args.pop(key, None)
    args.update(address='0x49e600', registers=json.dumps(dict(ECX='0', DF='0')),
                memory=json.dumps(dict(regions=regions)), return_registers='EAX,EBP,EBX,ECX,EIP,ESP')
    return args, expected


async def main():
    params = StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),
        args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'), '--transport', 'stdio'],
        env={**os.environ, 'GHIDRA_MCP_URL': 'http://127.0.0.1:8089'})
    rows = []
    async with stdio_client(params) as (rd, wr):
        async with ClientSession(rd, wr) as session:
            await session.initialize()
            for name in CONTROL_CASES:
                args, expected = build(name)
                path = ROOT/'ghidra'/f'phase110-state-Ghidra-control-{name}.json'
                if path.exists():
                    saved = json.loads(path.read_text(encoding='utf8'))
                    assert saved['input'] == args and saved['expected'] == expected
                else:
                    response = await session.call_tool('emulate_function', args)
                    result = json.loads('\n'.join(x.text for x in response.content if x.type == 'text'))
                    saved = dict(input=args, expected=expected, result=result.get('data', result), tool_is_error=response.isError)
                    path.write_text(json.dumps(saved, indent=2), encoding='utf8')
                r = saved['result']
                assert not saved['tool_is_error'] and r['success'] and r['hit_return']
                assert all(int(r['registers'][k], 16) == v for k, v in dict(EAX=1, EBP=1, EBX=0, ECX=0).items()), (name, r)
                rows.append(dict(case=name, file=path.name, sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
                print(name, 'Ghidra composition verified', flush=True)
    (ROOT/'phase110-object-state-crosschecks.json').write_text(
        json.dumps(dict(scope=__doc__, replays=rows), indent=2), encoding='utf8')


if __name__ == '__main__':
    asyncio.run(main())
