"""Ghidra factory -> original fullmana tick -> complete context observer."""
from pathlib import Path
import asyncio
import hashlib
import json
import os
from mcp import ClientSession, StdioServerParameters
from mcp.client.stdio import stdio_client
import emulate_factory_mana as native
from emulate_element_colors import region, pack32
from emulate_hero_parties import compare_code

ROOT = Path(__file__).resolve().parent


def build(name):
    c = next(c for c in native.cases() if c['name'] == name)
    args, expected, wanted, _ = native.build(c)
    regions = json.loads(args['memory'])['regions']
    address = native.f.OBS
    driver = bytearray(b'\xB9'+pack32(native.f.PLAYER))
    driver += b'\xE8'+pack32(0x4C10E0-(address+len(driver)+5))
    driver += b'\xBD'+pack32(1)+b'\xBB'+pack32(0)
    shadow = 0x11000000
    for i, (a, b) in enumerate(wanted.items()):
        regions.append(region(shadow, b))
        driver += compare_code(a, shadow, len(b))+bytes.fromhex('85c07505bb')+pack32(i+1)+bytes.fromhex('21c5')
        shadow += ((len(b)+255)//256)*256
    driver += b'\x68'+pack32(0xDEADBEEF)+b'\xC3'
    regions.append(region(address, driver))
    for key in ['before_calls', 'observe_reads', 'allowed_external_IO_code']:
        args.pop(key, None)
    args.update(address='0x4b7a90', registers=json.dumps(dict(ECX=hex(native.f.WORLD+8), DF='0')),
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
            for name in ['base', 'object-mana100']:
                args, expected = build(name)
                path = ROOT/'ghidra'/f'phase109-factory-mana-crosscheck-{name}.json'
                if path.exists():
                    value = json.loads(path.read_text(encoding='utf8'))
                    assert value['input'] == args and value['expected'] == expected
                else:
                    response = await session.call_tool('emulate_function', args)
                    result = json.loads('\n'.join(x.text for x in response.content if x.type == 'text'))
                    value = dict(input=args, expected=expected, result=result.get('data', result))
                    path.write_text(json.dumps(value, indent=2), encoding='utf8')
                r = value['result']
                assert r['success'] and r['hit_return'] and all(int(r['registers'][k], 16) == v for k, v in dict(EAX=1, EBP=1, EBX=0, ECX=0).items()), (name, r)
                rows.append(dict(case=name, file=path.name, sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
                print(name, 'Ghidra fullcomposition verified', flush=True)
    (ROOT/'phase109-factory-mana-crosschecks.json').write_text(json.dumps(dict(scope=__doc__, replays=rows), indent=2), encoding='utf8')


if __name__ == '__main__':
    asyncio.run(main())
