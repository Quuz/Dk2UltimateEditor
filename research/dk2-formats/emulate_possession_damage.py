"""Raw224/stat4 +65/level +optional225 -> possession setter -> native494D00.

Prepared post-factory actor/definition from accepted phase90/92 fixtures.
No original function/vtable patch. Full actor,world,variables,raw buffers and
guards compared; no actual attack, targetHP change or new actor creation.
"""
from pathlib import Path
import asyncio
import hashlib
import json
import os
import struct
import sys
from mcp import ClientSession, StdioServerParameters
from mcp.client.stdio import stdio_client
import emulate_possession_speed as sp
import emulate_creature_factory as cf
import emulate_object_factory as f
from emulate_element_colors import region, pack32
from emulate_hero_parties import compare_code
from native_possession_recharge import u32, s32, tz, qmul, qdiv
from native_possession_damage import project

ROOT = Path(__file__).resolve().parent
RAW65 = 0x10300000
RAW225 = 0x10310000


def cases():
    base = dict(percent=60, possessed=True, secondary=False, base_damage=20,
                level=1, coefficient=125, scalars=[6, 6, 6])
    variants = [
        ('possessed60', {}), ('normal60', dict(possessed=False)),
        ('secondary60', dict(possessed=False, secondary=True)), ('both60', dict(secondary=True)),
        ('possessed0', dict(percent=0)), ('secondary0', dict(possessed=False, secondary=True, percent=0)),
        ('possessed100', dict(percent=100)), ('secondary100', dict(possessed=False, secondary=True, percent=100)),
        ('possessed200', dict(percent=200)), ('minus1', dict(percent=0xFFFFFFFF)),
        ('minimum16', dict(percent=0x8000)), ('maximum16', dict(percent=0x7FFF)),
        ('high-bits60', dict(percent=0xABCD003C)),
        ('level2-125', dict(level=2)), ('level3-150', dict(level=3, coefficient=150)),
        ('level2-small', dict(level=2, base_damage=1)),
        ('level2-negative', dict(level=2, base_damage=200, coefficient=0xFFFF)),
        ('product-wrap', dict(base_damage=65535, percent=0x7FFF)),
        ('pit150', dict(possessed=False, pit_percent=150)),
        ('pit-high-bits150', dict(possessed=False, pit_percent=0xABCD0096)),
        ('pit256', dict(possessed=False, pit_percent=256)),
        ('possessed-pit', dict(pit_percent=150)),
        ('secondary-pit', dict(possessed=False, secondary=True, pit_percent=150)),
        ('scalar-zero', dict(scalars=[0, 0, 0])),
        ('scalar-high', dict(scalars=[30000, 30000, 30000])),
        ('current-records-level2', dict(level=2, source_current=True)),
        ('current-records-pit-level2', dict(level=2, possessed=False, pit_percent=100, source_current=True))]
    return [dict(base, **changes, name=name) for name, changes in variants]


def outcome(case):
    return project(case['base_damage'], case['percent'], level=case['level'],
                   coefficient_bits=case['coefficient'], possessed=case['possessed'],
                   secondary=case['secondary'], pit_active='pit_percent' in case,
                   pit225_bits=case.get('pit_percent', 0))


def build(case):
    source = dict(sp.cases()[0], raw_index=4, percent=case['percent'],
                  possessed=case['possessed'], other_flag=case['secondary'], scalars=case['scalars'])
    args, old = sp.build(source)
    allmem = {int(r['address'], 16): bytearray.fromhex(r['hex'])
              for r in json.loads(args['memory'])['regions']}
    checks, shadow = {}, 0x11000000
    for row in old['checks']:
        checks[int(row['address'], 16)] = allmem[shadow]
        shadow += ((row['bytes']+255)//256)*256
    memory = {a: b for a, b in allmem.items()
              if a < 0x11000000 and a not in [f.OBS, sp.DRIVER, 0x7FFEE000]}
    for buffers in [memory, checks]:
        f.put(buffers[cf.DEFINITION], 0x5C4, case['base_damage'], 2)
        f.put(buffers[cf.ACTOR], 0x403, case['level'], 1)
        if 'pit_percent' in case:
            flags = int.from_bytes(buffers[cf.ACTOR][0x3E6:0x3EA], 'little')
            f.put(buffers[cf.ACTOR], 0x3E6, flags | 0x8000)
    raw224 = struct.pack('<4I', 224, 4, case['percent'], 0x11223344)
    raw65 = struct.pack('<4I', 65, 4, case['coefficient'], case['level'])
    if case.get('source_current'):
        doc = json.loads((ROOT/'decoded/GlobalVariables.kwd.json').read_text(encoding='utf8'))
        data = Path(doc['source']).read_bytes()
        assert hashlib.sha256(data).hexdigest() == doc['sha256']
        raw = []
        for words in [[224, 4, 60, 10], [65, 4, 125, 2]]:
            row = next(r for r in doc['sections'][0]['records'] if r['words_u32'] == words)
            blob = bytes.fromhex(row['raw_hex'])
            assert data[row['offset']:row['offset']+16] == blob
            raw.append(blob)
        raw224, raw65 = raw
    memory[sp.RAW] = bytearray(bytes(16)+raw224)
    checks[sp.RAW] = bytearray(memory[sp.RAW])
    memory[RAW65] = bytearray(bytes(16)+raw65+bytes([0xA5])*64)
    checks[RAW65] = bytearray(memory[RAW65])
    f.put(checks[sp.VARS], 0x117+2*case['level'], case['coefficient'], 2)
    if 'pit_percent' in case:
        raw225 = struct.pack('<4I', 225, case['pit_percent'], 1, 2)
        if case.get('source_current'):
            row = next(r for r in doc['sections'][0]['records'] if r['words_u32'] == [225, 100, 0, 0])
            raw225 = bytes.fromhex(row['raw_hex'])
            assert data[row['offset']:row['offset']+16] == raw225
        memory[RAW225] = bytearray(bytes(16)+raw225+bytes([0xA5])*64)
        checks[RAW225] = bytearray(memory[RAW225])
        f.put(checks[sp.VARS], 0x482, case['pit_percent'], 1)
    e = outcome(case)
    memory[f.OUT] = bytearray(16)+bytearray([0xA5])*16
    checks[f.OUT] = bytearray(struct.pack('<4I', 1, e['result_bits'], 1, int('pit_percent' in case)))+bytearray([0xA5])*16
    driver = bytearray(b'\xA3'+pack32(f.OUT))
    def emit(blob):
        driver.extend(blob)
    def call(address):
        emit(b'\xE8'+pack32(address-(sp.DRIVER+len(driver)+5)))
    emit(b'\x68'+pack32(RAW65))
    call(0x51DC40)
    emit(b'\xA3'+pack32(f.OUT+8))
    if 'pit_percent' in case:
        emit(b'\x68'+pack32(RAW225)+b'\xB9'+pack32(f.WORLD))
        call(0x51CD90)
        emit(b'\xA3'+pack32(f.OUT+12))
    if case['possessed']:
        emit(b'\x68'+pack32(1)+b'\xB9'+pack32(cf.ACTOR))
        call(0x48DEA0)
    emit(b'\x68'+pack32(cf.ACTOR)+b'\xB9'+pack32(cf.DEFINITION))
    call(0x494D00)
    emit(b'\xA3'+pack32(f.OUT+4)+b'\x68'+pack32(f.OBS)+b'\xC3')
    memory[sp.DRIVER] = driver
    stack = bytearray([0xA5])*16384
    struct.pack_into('<2I', stack, 8192, sp.DRIVER, sp.RAW)
    memory[0x7FFEE000] = stack
    observer = bytearray(b'\xBD'+pack32(1)+b'\xBB'+pack32(0))
    regions = [region(a, b) for a, b in memory.items()]
    shadow = 0x11000000
    for i, (address, expected) in enumerate(checks.items()):
        regions.append(region(shadow, expected))
        observer += compare_code(address, shadow, len(expected))+bytes.fromhex('85c07505bb')+pack32(i+1)+bytes.fromhex('21c5')
        shadow += ((len(expected)+255)//256)*256
    observer += bytes.fromhex('8b15')+pack32(f.OUT+4)+b'\x68'+pack32(0xDEADBEEF)+b'\xC3'
    regions.append(region(f.OBS, observer))
    spans = sorted((int(r['address'], 16), len(bytes.fromhex(r['hex']))) for r in regions)
    assert all(a+n <= b for (a, n), (b, _) in zip(spans, spans[1:]))
    args.update(address='0x51dc70', memory=json.dumps(dict(regions=regions)),
                return_registers='EAX,EBP,EBX,ECX,EDX,EIP,ESP')
    return args, dict(outcome=e, raw224=raw224.hex(), raw65=raw65.hex(),
                      checks=[dict(address=hex(a), bytes=len(b)) for a, b in checks.items()])


async def main():
    selected = cases()
    if len(sys.argv) > 1:
        selected = [c for c in selected if c['name'] == sys.argv[1]]
        assert selected
    params = StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),
        args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'), '--transport', 'stdio'],
        env={**os.environ, 'GHIDRA_MCP_URL': 'http://127.0.0.1:8089'})
    rows = []
    async with stdio_client(params) as (rd, wr):
        async with ClientSession(rd, wr) as session:
            await session.initialize()
            for case in selected:
                args, expected = build(case)
                tag = hashlib.sha256(json.dumps(args, sort_keys=True).encode()).hexdigest()[:10]
                path = ROOT/'ghidra'/f'phase104-damage-{case["name"]}-{tag}.json'
                if path.exists():
                    value = json.loads(path.read_text(encoding='utf8'))
                    assert value['input'] == args and value['expected'] == expected
                else:
                    response = await session.call_tool('emulate_function', args)
                    payload = '\n'.join(x.text for x in response.content if x.type == 'text')
                    try:
                        result = json.loads(payload)
                    except ValueError:
                        path.with_suffix('.error.txt').write_text(payload, encoding='utf8')
                        raise
                    value = dict(case=case, input=args, expected=expected, result=result.get('data', result))
                    path.write_text(json.dumps(value, indent=2), encoding='utf8')
                result = value['result']
                assert result['success'] and result['hit_return'], (case['name'], result)
                assert all(int(result['registers'][k], 16) == v for k, v in
                           dict(EAX=1, EBP=1, EBX=0, ECX=0, EDX=expected['outcome']['result_bits']).items()), (case['name'], result)
                rows.append(dict(case=case['name'], file=path.name, sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
                print(case['name'], expected['outcome'], 'verified', flush=True)
    if len(selected) == len(cases()):
        (ROOT/'phase104-possession-damage-emulations.json').write_text(
            json.dumps(dict(scope=__doc__, replays=rows), indent=2), encoding='utf8')


if __name__ == '__main__':
    asyncio.run(main())
