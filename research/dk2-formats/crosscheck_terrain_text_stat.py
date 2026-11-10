"""Ghidra comparison of complete native Terrain model-chain outputs, including heap."""
import asyncio, hashlib, json, os, struct
from pathlib import Path
from mcp import ClientSession, StdioServerParameters
from mcp.client.stdio import stdio_client
import emulate_terrain_text_stat as native
from emulate_element_colors import region, pack32
from emulate_hero_parties import compare_code

ROOT = Path(__file__).resolve().parent
CONTROLS = ['three-half', 'minus-one', 'dash-skip', 'max-u32', 'minus-one-reload']


def build(name):
    case = next(row for row in native.cases() if row['name'] == name)
    fixture, saved, _ = native.execute(case)
    native.verify(case, fixture, saved['result'])
    regions = list(fixture['memory'])
    first = fixture['calls'][0]
    driver = bytearray()
    for call in fixture['calls'][1:]:
        for value in reversed(call['stack']):
            driver += b'\x68' + pack32(value)
        driver += b'\xb9' + pack32(call['ecx']) + b'\xb8' + pack32(call['address']) + bytes.fromhex('ffd0')
        # The callback is cdecl; the converter and writer clean their one arg.
        if call['address'] == 0x49F5E0:
            driver += bytes.fromhex('83c404')
    driver += bytes.fromhex('bd01000000bb00000000')
    stack = next(row for row in regions if int(row['address'], 16) == native.engine.STACK - 0x4000)
    data = bytearray.fromhex(stack['hex'])
    struct.pack_into('<2I', data, 0x4000, native.engine.STOP, *first['stack'])
    stack['hex'] = data.hex()
    shadow = 0x18000000
    # Stack addresses differ in this continuous driver, so padding/date scratch
    # bytes of the header must be compared semantically rather than importing a
    # repeated-root stack snapshot. The original output records are exact.
    wanted = []
    for row in saved['result']['output']:
        address, data = int(row['address'], 16), bytes.fromhex(row['hex'])
        if address == native.engine.writer.OUTPUT:
            for start, end in [(0, 42), (44, 47), (48, len(data))]:
                wanted.append((address + start, data[start:end]))
        elif address != native.engine.writer.META:
            wanted.append((address, data))
        else:
            wanted.append((address, data))
    for index, (address, data) in enumerate(wanted):
        regions.append(region(shadow, data))
        driver += compare_code(address, shadow, len(data)) + bytes.fromhex('85c07505bb') + pack32(index + 1) + bytes.fromhex('21c5')
        shadow += (len(data) + 255) // 256 * 256
    driver += bytes.fromhex('68efbeaddec3')
    regions.append(region(native.engine.STOP, driver))
    return dict(program='DK2 Editor pro.exe', address=hex(first['address']),
        registers=json.dumps(dict(ECX=hex(first['ecx']), ESP=hex(native.engine.STACK), DF='0')),
        memory=json.dumps(dict(regions=regions)), max_steps=5000000,
        return_registers='EAX,EBP,EBX,ECX,EIP,ESP'), len(wanted), sum(len(data) for _, data in wanted)


async def main():
    params = StdioServerParameters(command=str(ROOT.parent / 'ghidra-mcp-runtime/venv/Scripts/python.exe'),
        args=[str(ROOT.parent / 'ghidra-mcp/bridge_mcp_ghidra.py'), '--transport', 'stdio'],
        env={**os.environ, 'GHIDRA_MCP_URL': 'http://127.0.0.1:8089'})
    rows = []
    async with stdio_client(params) as (rd, wr):
        async with ClientSession(rd, wr) as session:
            await session.initialize()
            for name in CONTROLS:
                args, count, size = build(name)
                tag = hashlib.sha256(json.dumps(args, sort_keys=True).encode()).hexdigest()[:12]
                path = ROOT / 'ghidra' / f'phase165-Terrain-text-Ghidra-{name}-{tag}.json'
                if path.exists():
                    saved = json.loads(path.read_text(encoding='utf8'))
                    assert saved['input'] == args
                else:
                    response = await session.call_tool('emulate_function', args)
                    raw = json.loads('\n'.join(row.text for row in response.content if row.type == 'text'))
                    saved = dict(input=args, result=raw.get('data', raw), tool_is_error=response.isError)
                    path.write_text(json.dumps(saved, indent=2), encoding='utf8')
                result = saved['result']
                assert not saved['tool_is_error'] and result['success'] and result['hit_return'], (name, result)
                assert all(int(result['registers'][key], 16) == value for key, value in dict(EAX=1, EBP=1, EBX=0, ECX=0).items()), (name, result)
                rows.append(dict(case=name, file=str(path.relative_to(ROOT)), sha256=hashlib.sha256(path.read_bytes()).hexdigest(),
                    checked_regions=count, checked_bytes=size, unassigned_header_bytes_excluded=[42, 43, 47]))
                print(name, 'Ghidra passed', count, size, flush=True)
    (ROOT / 'phase165-Terrain-text-crosschecks.json').write_text(json.dumps(dict(scope=__doc__, replays=rows), indent=2), encoding='utf8')


if __name__ == '__main__':
    asyncio.run(main())
