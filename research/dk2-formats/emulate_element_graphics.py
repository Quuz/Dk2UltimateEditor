"""Phase 102: original CRenderInfo dispatcher -> allocator -> constructor -> queue.

Prepared post-creation element from the accepted phase 81 snapshot; this is
not a new raw-file loader, creation, game tick, live renderer or GPU execution.
No game function/vtable replacement. Synthetic code is post-return observation.
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
from emulate_element_colors import region, pack32
from emulate_hero_parties import compare_code
import memory_readout

ROOT = Path(__file__).resolve().parent
NODE = 0x1000D2E0
DEF = 0x10007000
ENGINE = 0x10019000
HEAP = 0x10020000
QUEUE = 0x10021000
OUT = 0x10022000
OBSERVER = 0x10023000


def cases():
    base = dict(name='sprite-base', kind=2, resource_flags=1, palette=[0, 0])
    out = [base]
    for label, kind, flags in [('sprite1', 1, 1), ('sprite2', 2, 1),
                               ('sprite3', 3, 1), ('quad', 2, 0x101)]:
        for palette in ([0, 0], [255, 0], [0, 255], [255, 255]):
            if label == 'sprite2' and palette == [0, 0]:
                continue
            out.append(dict(name=f'{label}-palette-{palette[0]}-{palette[1]}',
                            kind=kind, resource_flags=flags, palette=palette))
    for name, changes in [
            ('sprite-rgb-control', dict(rgb=[0, 128, 255])),
            ('sprite-alpha0', dict(alpha=0)),
            ('sprite-alpha128', dict(alpha=128)),
            ('sprite-scale-control', dict(scale=[2048, 8192])),
            ('sprite-offset-control', dict(offset=[4096, -2048, 1024])),
            ('resource-disabled', dict(resource_flags=0)),
            ('instance-disabled', dict(instance_enabled=False)),
            ('gloom-hidden', dict(resource_flags=0x40001, gloom=0)),
            ('gloom-visible', dict(resource_flags=0x40001, gloom=1)),
            ('quad-rgb-alpha-control', dict(resource_flags=0x101,
                                            rgb=[255, 128, 0], alpha=128))]:
        out.append(dict(base, **dict(changes, name=name)))
    for name, changes in [
            ('source-sprite-base', dict(source_entry=True)),
            ('source-sprite-palette-both', dict(source_entry=True, palette=[255, 255])),
            ('source-quad-base', dict(source_entry=True, resource_flags=0x101)),
            ('source-suppressed', dict(source_entry=True, skip_graphics=1))]:
        out.append(dict(base, **dict(changes, name=name)))
    assert len(out) == 30 and len({c['name'] for c in out}) == 30
    return out


def put(buf, offset, value, size=4):
    buf[offset:offset + size] = value.to_bytes(size, 'little')


def build(case):
    node = bytearray((ROOT / 'ghidra/phase81-element-base.bin').read_bytes())
    assert len(node) == 287 and int.from_bytes(node[0x4E:0x52], 'little') == DEF
    if 'rgb' in case:
        node[0x6E:0x71] = bytes(case['rgb'])
    if 'alpha' in case:
        node[0x71] = case['alpha']
    if 'scale' in case:
        struct.pack_into('<2I', node, 0x2A, *case['scale'])
    if 'offset' in case:
        struct.pack_into('<3h', node, 0x77, *case['offset'])
    if not case.get('instance_enabled', True):
        node[0xD5] &= 0xFE
    node += bytes([0xA5]) * 64
    definition = bytearray(79 + 64)
    definition[79:] = bytes([0xA5]) * 64
    put(definition, 0, 4096)
    put(definition, 4, 8192)
    definition[0xA] = case['kind']
    put(definition, 0xB, case['resource_flags'])
    put(definition, 0xF, 0x12345678)
    definition[0x4D:0x4F] = bytes(case['palette'])
    engine = bytes(0x300)
    queue = bytes(0x80)
    heap = bytearray([0xA5]) * 192
    heap[:20] = bytes(20)
    put(heap, 12, 128)
    put(heap, 16, 28, 2)
    heap[19] = 1
    allocator = bytearray(0x300)
    put(allocator, 0xC, 28)
    put(allocator, 0xA0 + 4 * 28, HEAP)
    allocator_after = bytearray(allocator)
    queue_after = bytearray(queue)
    active = bool(case['resource_flags'] & 1 and case.get('instance_enabled', True)
                  and (not case['resource_flags'] & 0x40000 or case.get('gloom', 1))
                  and not case.get('skip_graphics', 0))
    if active:
        put(allocator_after, 0xA0 + 4 * 28, 0)
        put(queue_after, 0x30, HEAP + 20)
    fixed = [(NODE, node), (DEF, definition), (ENGINE, engine), (HEAP, heap),
             (QUEUE, queue), (OUT, bytes(80)), (0x765B00, allocator),
             (0x760B80, pack32(QUEUE)), (0x760B7C, struct.pack('<f', 0.5)),
             (0x759ADC, pack32(case.get('gloom', 1))), (0x759ACC, pack32(0)), (0x795704, pack32(0)),
             (0, bytes(4))]
    mem = [region(a, b) for a, b in fixed]
    checks = [(NODE, node), (DEF, definition), (ENGINE, engine),
              (HEAP + 128, heap[128:]), (QUEUE, queue_after),
              (0x765B00, allocator_after), (0x760B80, pack32(QUEUE)),
              (0x760B7C, struct.pack('<f', 0.5)), (0, bytes(4))]
    observer = b'\xA3' + pack32(OUT) + b'\xBD' + pack32(1)
    at = 0x11000000
    for address, expected in checks:
        mem.append(region(at, expected))
        observer += compare_code(address, at, len(expected)) + bytes.fromhex('21c5')
        at += (len(expected) + 15) & ~15
    if case.get('source_entry'):
        observer += b'\x8B\x1D' + pack32(OUT)  # expose native EAX after comparisons
    observer += memory_readout.code(HEAP) + b'\x68' + pack32(0xDEADBEEF) + b'\xC3'
    mem.append(region(OBSERVER, observer))
    stack = bytearray([0xA5]) * 8192
    if case.get('source_entry'):
        struct.pack_into('<5I', stack, 4096, OBSERVER, 0, 0, NODE,
                         case.get('skip_graphics', 0))
    else:
        struct.pack_into('<7I', stack, 4096, OBSERVER, NODE + 0x26,
                         NODE + 0xD6, NODE + 0xD6, NODE + 0xE2, NODE + 0xE2, 0)
    mem.append(region(0x7FFEF000, stack))
    spans = sorted((int(r['address'], 16), len(bytes.fromhex(r['hex']))) for r in mem)
    assert all(a + n <= b for (a, n), (b, _) in zip(spans, spans[1:]))
    args = dict(program='DKII-DX.exe', address='0x599c60' if case.get('source_entry') else '0x59b880', max_steps=100000,
                registers=json.dumps(dict(ECX=hex(ENGINE), DF='0')),
                memory=json.dumps(dict(regions=mem)),
                return_registers='EAX,EBX,ECX,EDX,ESI,EDI,ESP,EBP,EIP,' + memory_readout.REGISTERS)
    return args


def expected_heap(case):
    """Independent projection of the executed sprite/quad constructor stores.

Single products use exact input floats and fit Python double precision before
their explicit binary32 stores. No GPU/pixel rounding is inferred from this.
"""
    from build_effect_semantics import pe_read
    exe = (ROOT.parent.parent / 'Dungeon Keeper 2/DKII-DX.exe').read_bytes()
    constant = lambda a: struct.unpack('<f', pe_read(exe, a, 4))[0]
    f32 = lambda x: struct.unpack('<f', struct.pack('<f', x))[0]
    heap = bytearray([0xA5]) * 128
    heap[:20] = bytes(20)
    put(heap, 12, 128)
    put(heap, 16, 28, 2)
    heap[19] = 1
    active = bool(case['resource_flags'] & 1 and case.get('instance_enabled', True)
                  and (not case['resource_flags'] & 0x40000 or case.get('gloom', 1))
                  and not case.get('skip_graphics', 0))
    if not active:
        return bytes(heap)
    heap[18] = 1
    quad = bool(case['resource_flags'] & 0x100)
    obj = 20
    fields = {0: 0x66FCF4 if quad else 0x66FCCC, 4: 0}
    floats = {}
    scale = case.get('scale', [4096, 4096])
    angle = f32(123 * constant(0x6704B8) * constant(0x670548))
    rgb = [f32(f32(c * constant(0x670520)) * constant(0x66FC18))
           for c in case.get('rgb', [10, 20, 30])]
    alpha = f32(case.get('alpha', 255) * constant(0x670520))
    offsets = case.get('offset', [0, 0, 0])
    pos = [f32((p + d) * constant(a))
           for p, d, a in zip([43008, 43008, 8192], offsets,
                              [0x6704D0, 0x6704D0, 0x670504])]
    if quad:
        fields.update({0x28: 0x12345678, 0x34: 0x10203040})
        floats.update({8: scale[0] / 4096, 0xC: angle, 0x2C: 0, 0x30: alpha})
        floats.update(zip([0x10, 0x14, 0x18], pos))
        floats.update(zip([0x1C, 0x20, 0x24], rgb))
    else:
        fields.update({0x44: 0x12345678, 0x4C: 0x10203040})
        floats.update({8: scale[0] / 4096, 0xC: 2 * scale[1] / 4096,
                       0x10: angle, 0x14: 0, 0x18: 0, 0x40: alpha, 0x48: 0})
        floats.update(zip([0x1C, 0x20, 0x24], pos))
        floats.update(zip([0x34, 0x38, 0x3C], rgb))
    for offset, value in fields.items():
        put(heap, obj + offset, value)
    for offset, value in floats.items():
        struct.pack_into('<f', heap, obj + offset, value)
    return bytes(heap)


def verify(case, result):
    assert result['success'] and result['hit_return'], (case['name'], result)
    assert int(result['registers']['EBP'], 16) == 1, case['name']
    assert int(result['registers']['ECX'], 16) == 0, case['name']
    if case.get('source_entry'):
        assert int(result['registers']['EBX'], 16) == 1, case['name']
    actual = memory_readout.decode(result['registers'])
    expected = expected_heap(case)
    assert actual == expected, (case['name'], [(i, a, b) for i, (a, b) in
                                               enumerate(zip(actual, expected)) if a != b])
    return actual


async def main():
    params = StdioServerParameters(
        command=str(ROOT.parent / 'ghidra-mcp-runtime/venv/Scripts/python.exe'),
        args=[str(ROOT.parent / 'ghidra-mcp/bridge_mcp_ghidra.py'), '--transport', 'stdio'],
        env={**os.environ, 'GHIDRA_MCP_URL': 'http://127.0.0.1:8089'})
    selected = cases()
    if len(sys.argv) > 1:
        selected = [c for c in selected if c['name'] == sys.argv[1]]
        assert selected
    rows = []
    async with stdio_client(params) as (rd, wr):
        async with ClientSession(rd, wr) as session:
            await session.initialize()
            for case in selected:
                args = build(case)
                tag = hashlib.sha256(json.dumps(args, sort_keys=True).encode()).hexdigest()[:10]
                path = ROOT / 'ghidra' / f'phase102-gfx-{case["name"]}-{tag}.json'
                if path.exists():
                    saved = json.loads(path.read_text(encoding='utf8'))
                    assert saved['input'] == args and saved['case'] == case
                else:
                    response = await session.call_tool('emulate_function', args)
                    payload = '\n'.join(x.text for x in response.content if x.type == 'text')
                    try:
                        result = json.loads(payload)
                    except ValueError:
                        path.with_suffix('.error.txt').write_text(payload, encoding='utf8')
                        raise
                    saved = dict(case=case, input=args, response_is_error=bool(response.isError),
                                 result=result.get('data', result))
                    path.write_text(json.dumps(saved, indent=2), encoding='utf8')
                result = saved['result']
                actual = verify(case, result)
                rows.append(dict(case=case['name'], file=path.name,
                                 sha256=hashlib.sha256(path.read_bytes()).hexdigest(),
                                 heap_sha256=hashlib.sha256(actual).hexdigest()))
                print(case['name'], 'verified', flush=True)
    if len(selected) == len(cases()):
        (ROOT / 'phase102-element-graphics-emulations.json').write_text(
            json.dumps(dict(scope=__doc__, replays=rows), indent=2), encoding='utf8')


if __name__ == '__main__':
    asyncio.run(main())
