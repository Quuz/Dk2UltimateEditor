"""Independent Ghidra CPU controls: 3 native type functions and 3 matched damage paths."""
import asyncio, hashlib, json, os, struct
from pathlib import Path
from mcp import ClientSession, StdioServerParameters
from mcp.client.stdio import stdio_client
import emulate_editor_combat_categories as editor
import emulate_matched_categories as game
from emulate_hero_parties import compare_code
from emulate_element_colors import region, pack32

ROOT = Path(__file__).resolve().parent
EDITOR_CONTROLS = [('resistance_lookup', 8, 0), ('shot_lookup', 6, 0), ('setter', 8, 2)]
GAME_CONTROLS = ['matched-hit-res100', 'matched-area-res100', 'matched-range-res100']


def editor_input(kind, value, slot):
    case = editor.build(kind, value, slot)
    obs, shadow = 0x13000000, 0x14000000
    regions = list(case['memory'])
    stack = bytearray(8192)
    struct.pack_into('<' + 'I' * (len(case['stack']) + 1), stack, 4096, obs, *case['stack'])
    regions.append(region(0x7FFEF000, stack))
    driver = bytearray.fromhex('bd01000000bb00000000')
    # EAX must be observed before compare_code clobbers it.
    if case['expected_EAX'] is not None:
        driver += b'\x3d' + pack32(case['expected_EAX']) + bytes.fromhex('7405bbff000000')
    for index, r in enumerate(case['expected']):
        address, blob = int(r['address'], 16), bytes.fromhex(r['hex'])
        regions.append(region(shadow, blob))
        driver += compare_code(address, shadow, len(blob)) + bytes.fromhex('85c07505bb') + pack32(index + 1) + bytes.fromhex('21c5')
        shadow += (len(blob) + 255) // 256 * 256
    driver += bytes.fromhex('68efbeaddec3')
    regions.append(region(obs, driver))
    args = dict(program='DK2 Editor pro.exe', address=hex(case['entry']),
        registers=json.dumps(dict(ECX=hex(case['ecx']), ESP='0x7fff0000', DF='0')),
        memory=json.dumps(dict(regions=regions)), max_steps=200000,
        return_registers='EAX,EBP,EBX,ECX,EIP,ESP')
    return args, len(case['expected']), sum(len(bytes.fromhex(r['hex'])) for r in case['expected'])


def game_input(name):
    case = next(c for c in game.cases() if c['name'] == name)
    args, _ = game.native.probe.build(case)
    wanted = game.expected(case)
    calls, first = args['before_calls'], args['before_calls'][0]
    assert not first['stack_args']
    driver = bytearray()
    for call in calls[1:] + [dict(address=args['address'], registers=json.loads(args['registers']), stack_args=[])]:
        for value in reversed(call['stack_args']):
            driver += b'\x68' + pack32(value)
        driver += b'\xb9' + pack32(int(call['registers']['ECX'], 0)) + b'\xb8' + pack32(int(call['address'], 0)) + bytes.fromhex('ffd0')
    driver += bytearray.fromhex('bd01000000bb00000000')
    regions = json.loads(args['memory'])['regions']
    stack = next(r for r in regions if int(r['address'], 16) == 0x7FFEE000)
    blob = bytearray.fromhex(stack['hex'])
    struct.pack_into('<I', blob, 8192, game.native.probe.f.OBS)
    stack['hex'] = blob.hex()
    shadow = 0x11000000
    for index, (address, blob) in enumerate(wanted.items()):
        regions.append(region(shadow, blob))
        driver += compare_code(address, shadow, len(blob)) + bytes.fromhex('85c07505bb') + pack32(index + 1) + bytes.fromhex('21c5')
        shadow += (len(blob) + 255) // 256 * 256
    driver += bytes.fromhex('68efbeaddec3')
    regions.append(region(game.native.probe.f.OBS, driver))
    for key in ['before_calls', 'observe_reads', 'observe_writes', 'allowed_external_IO_code']:
        args.pop(key, None)
    args.update(address=first['address'], registers=json.dumps(dict(first['registers'], DF='0')),
        memory=json.dumps(dict(regions=regions)), max_steps=1000000,
        return_registers='EAX,EBP,EBX,ECX,EIP,ESP')
    return args, len(wanted), sum(map(len, wanted.values()))


async def run_group(port, prepared):
    params = StdioServerParameters(command=str(ROOT.parent / 'ghidra-mcp-runtime/venv/Scripts/python.exe'),
        args=[str(ROOT.parent / 'ghidra-mcp/bridge_mcp_ghidra.py'), '--transport', 'stdio'],
        env={**os.environ, 'GHIDRA_MCP_URL': f'http://127.0.0.1:{port}'})
    rows = []
    async with stdio_client(params) as (rd, wr):
        async with ClientSession(rd, wr) as session:
            await session.initialize()
            for name, (args, count, size) in prepared:
                tag = hashlib.sha256(json.dumps(args, sort_keys=True).encode()).hexdigest()[:12]
                path = ROOT / 'ghidra' / f'phase125-combat-Ghidra-{name}-{tag}.json'
                if path.exists():
                    saved = json.loads(path.read_text(encoding='utf8'))
                    assert saved['input'] == args
                else:
                    response = await session.call_tool('emulate_function', args)
                    raw = json.loads('\n'.join(c.text for c in response.content if c.type == 'text'))
                    saved = dict(input=args, result=raw.get('data', raw), tool_is_error=response.isError)
                    path.write_text(json.dumps(saved, indent=2), encoding='utf8')
                result = saved['result']
                assert not saved['tool_is_error'] and result['success'] and result['hit_return'], (name, result)
                assert all(int(result['registers'][key], 16) == value for key, value in dict(EAX=1, EBP=1, EBX=0, ECX=0).items()), (name, result)
                rows.append(dict(case=name, port=port, file=str(path.relative_to(ROOT)),
                    sha256=hashlib.sha256(path.read_bytes()).hexdigest(), checked_regions=count, checked_bytes=size))
                print(name, 'Ghidra passed', count, size, flush=True)
    return rows


async def main():
    prepared_editor = [(f'{k}-{v}-{s}', editor_input(k, v, s)) for k, v, s in EDITOR_CONTROLS]
    prepared_game = [(name, game_input(name)) for name in GAME_CONTROLS]
    rows = await run_group(8090, prepared_editor)
    rows += await run_group(8089, prepared_game)
    (ROOT / 'phase125-combat-type-crosschecks.json').write_text(json.dumps(dict(scope=__doc__, replays=rows), indent=2), encoding='utf8')


if __name__ == '__main__':
    asyncio.run(main())
