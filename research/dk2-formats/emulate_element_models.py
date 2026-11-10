"""Phase 103: prepared element -> native model builder/constructor/queue.

Loaded resource-cache node and frame metadata are explicit typed fixtures,
not decoded model geometry or a live asset load. No original code/vtable
replacement; model pointer payload is not claimed drawable by the GPU.
"""
from pathlib import Path
import asyncio
import hashlib
import json
import math
import os
import struct
import sys
from mcp import ClientSession, StdioServerParameters
from mcp.client.stdio import stdio_client
from emulate_element_colors import region, pack32
from emulate_hero_parties import compare_code
import memory_readout_ymm as capture

ROOT = Path(__file__).resolve().parent
NODE = 0x1000D2E0
DEF = 0x10007000
ENGINE = 0x10019000
HEAP = 0x10020000
QUEUE = 0x10021000
OUT = 0x10022000
OBSERVER = 0x10023000
TABLE = 0x10024000
CACHE = 0x10025000
METADATA = 0x10026000


def cases():
    out = []
    for kind in [4, 5, 6]:
        for palette in ([0, 0], [255, 0], [0, 255], [255, 255]):
            out.append(dict(name=f'model{kind}-palette-{palette[0]}-{palette[1]}',
                            kind=kind, palette=palette))
    for name, kind, changes in [
            ('model5-rgb-alpha', 5, dict(rgb=[0, 128, 255], alpha=128)),
            ('model5-scale', 5, dict(scale=[2048, 8192])),
            ('model4-scale', 4, dict(scale=[2048, 8192])),
            ('model6-scale', 6, dict(scale=[2048, 8192])),
            ('model5-resource-bit200', 5, dict(resource_flags=0x201)),
            ('model4-resource-bit200', 4, dict(resource_flags=0x201)),
            ('model6-resource-bit200', 6, dict(resource_flags=0x201)),
            ('model6-frame', 6, dict(frame_q12=[0, 4096])),
            ('model6-frame-count5', 6, dict(frame_q12=[0, 4096], frames=5)),
            ('model6-rgb-alpha', 6, dict(rgb=[255, 128, 0], alpha=128))]:
        out.append(dict(name=name, kind=kind, palette=[0, 0], **changes))
    assert len(out) == 22
    return out


def put(buf, offset, value, size=4):
    buf[offset:offset + size] = value.to_bytes(size, 'little')


def build(case, include_checks=False):
    node = bytearray((ROOT / 'ghidra/phase81-element-base.bin').read_bytes())
    assert len(node) == 287 and int.from_bytes(node[0xF4:0xF8], 'little') == 1
    node[0x6E:0x71] = bytes(case.get('rgb', [10, 20, 30]))
    node[0x71] = case.get('alpha', 255)
    struct.pack_into('<2I', node, 0x2A, *case.get('scale', [4096, 4096]))
    struct.pack_into('<2I', node, 0xB6, *case.get('frame_q12', [0, 0]))
    node += bytes([0xA5]) * 64
    definition = bytearray(143)
    definition[79:] = bytes([0xA5]) * 64
    definition[0xA] = case['kind']
    put(definition, 0xB, case.get('resource_flags', 1))
    put(definition, 0xF, 1)  # valid index into explicitly prepared 12-byte resource table
    definition[0x4D:0x4F] = bytes(case['palette'])
    engine = bytes(0x300)
    queue = bytes(0x80)
    table = bytearray(24 + 64)
    put(table, 16, CACHE)
    cache = bytearray(24 + 64)
    put(cache, 0, METADATA)
    put(cache, 12, 1)
    metadata = bytearray(32 + 64)
    put(metadata, 8, case.get('frames', 3))
    metadata[0x1C] = 1
    heap = bytearray([0xA5]) * 256
    heap[:20] = bytes(20)
    put(heap, 12, 192)
    put(heap, 16, 30, 2)
    heap[19] = 1
    allocator = bytearray(0x300)
    put(allocator, 0xC, 30)
    put(allocator, 0xA0 + 4 * 30, HEAP)
    allocator_after = bytearray(allocator)
    put(allocator_after, 0xA0 + 4 * 30, 0)
    queue_after = bytearray(queue)
    put(queue_after, 0x30, HEAP + 20)
    fixed = [(NODE, node), (DEF, definition), (ENGINE, engine), (HEAP, heap),
             (QUEUE, queue), (OUT, bytes(80)), (TABLE, table), (CACHE, cache),
             (METADATA, metadata), (0x765B00, allocator),
             (0x760B80, pack32(QUEUE)), (0x760B7C, struct.pack('<f', 0.5)),
             (0x759ADC, pack32(1)), (0x759ACC, pack32(0)), (0x795704, pack32(0)),
             (0x766A6C, pack32(TABLE)), (0x766A70, pack32(CACHE)),
             (0x766658, pack32(CACHE)), (0x766654, pack32(1)), (0, bytes(4))]
    mem = [region(a, b) for a, b in fixed]
    checks = [(NODE, node), (DEF, definition), (ENGINE, engine),
              (HEAP + 192, heap[192:]), (QUEUE, queue_after),
              (TABLE, table), (CACHE, cache), (METADATA, metadata),
              (0x765B00, allocator_after), (0x760B80, pack32(QUEUE)),
              (0x766A6C, pack32(TABLE)), (0x766A70, pack32(CACHE)),
              (0x766658, pack32(CACHE)), (0x766654, pack32(0 if case['kind'] == 6 else 1)),
              (0x760B7C, struct.pack('<f', 0.5)), (0, bytes(4))]
    observer = b'\xA3' + pack32(OUT) + b'\xBD' + pack32(1)
    at = 0x11000000
    for address, expected in checks:
        mem.append(region(at, expected))
        observer += compare_code(address, at, len(expected)) + bytes.fromhex('21c5')
        at += (len(expected) + 15) & ~15
    observer += b'\x8B\x1D' + pack32(OUT) + capture.code(HEAP)
    observer += b'\x68' + pack32(0xDEADBEEF) + b'\xC3'
    mem.append(region(OBSERVER, observer))
    stack = bytearray([0xA5]) * 8192
    struct.pack_into('<5I', stack, 4096, OBSERVER, 0, 0, NODE, 0)
    mem.append(region(0x7FFEF000, stack))
    spans = sorted((int(r['address'], 16), len(bytes.fromhex(r['hex']))) for r in mem)
    assert all(a + n <= b for (a, n), (b, _) in zip(spans, spans[1:]))
    args = dict(program='DKII-DX.exe', address='0x599c60', max_steps=100000,
                registers=json.dumps(dict(ECX=hex(ENGINE), DF='0')),
                memory=json.dumps(dict(regions=mem)),
                return_registers='EAX,EBX,ECX,EDX,ESI,EDI,ESP,EBP,EIP,' + capture.REGISTERS)
    return (args, checks) if include_checks else args


async def main():
    params = StdioServerParameters(
        command=str(ROOT.parent / 'ghidra-mcp-runtime/venv/Scripts/python.exe'),
        args=[str(ROOT.parent / 'ghidra-mcp/bridge_mcp_ghidra.py'), '--transport', 'stdio'],
        env={**os.environ, 'GHIDRA_MCP_URL': 'http://127.0.0.1:8089'})
    selected = cases()
    if len(sys.argv) > 1:
        selected = [c for c in selected if c['name'] == sys.argv[1]]
        assert selected
    async with stdio_client(params) as (rd, wr):
        async with ClientSession(rd, wr) as session:
            await session.initialize()
            for case in selected:
                args = build(case)
                tag = hashlib.sha256(json.dumps(args, sort_keys=True).encode()).hexdigest()[:10]
                path = ROOT / 'ghidra' / f'phase103-model-{case["name"]}-{tag}.json'
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
                regs = result.get('registers', {})
                print(json.dumps(dict(case=case['name'], file=path.name,
                                      success=result.get('success'), hit_return=result.get('hit_return'),
                                      error=result.get('error'), EBP=regs.get('EBP'),
                                      EBX=regs.get('EBX'), ECX=regs.get('ECX'),
                                      heap_hex=capture.decode(regs).hex() if 'YMM0' in regs else None)), flush=True)


if __name__ == '__main__':
    if len(sys.argv) < 2 or sys.argv[1] != '--ghidra-probe':
        raise SystemExit('Ghidra fcos is unsupported. Use run_element_models_unicorn.py; diagnostic probe only with --ghidra-probe CASE.')
    sys.argv.pop(1)
    asyncio.run(main())
