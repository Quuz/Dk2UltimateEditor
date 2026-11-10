"""Full original Terrain converter/model registration/writer using frozen native heap setup.

No new game-field names inferred. Full raw-to-model-to-file preservation for
unknown198/224 and16WORD1AE is checked, including unsigned high-bit boundaries.
"""
from pathlib import Path
import hashlib, json, struct
import emulate_room_model_chain as engine
import native_terrain_catalogue as api
from build_effect_semantics import pe_read

ROOT = Path(__file__).resolve().parent
SOURCE = ROOT.parent.parent / 'Dungeon Keeper 2/Data/editor/Terrain.kwd'


def sha(blob):
    return hashlib.sha256(blob).hexdigest()


def cases():
    records = [raw for section in engine.fmt.parse(SOURCE.read_bytes()) if section.signature == 0x6E for raw in section.records]
    return [dict(name='catalogue41', ids=[raw[0x1D6] for raw in records], unknown198=None, unknown224=None, pattern=False)] + [
        dict(name=f'boundary-{v:08x}', ids=[records[0][0x1D6]], unknown198=v & 0xFFFF, unknown224=v, pattern=True)
        for v in [0, 1, 0x7FFFFFFF, 0x80000000, 0xFFFFFFFF]]


def build(case):
    template = engine.build()
    memory = {int(row['address'], 16): bytearray.fromhex(row['hex']) for row in template['memory']}
    del memory[engine.RAW]
    del memory[engine.MID]
    records = [raw for section in engine.fmt.parse(SOURCE.read_bytes()) if section.signature == 0x6E
        for raw in section.records if raw[0x1D6] in case['ids']]
    assert len(records) == len(case['ids'])
    raws = []
    for index, raw in enumerate(records):
        raw = bytearray(raw)
        if case['unknown198'] is not None:
            struct.pack_into('<H', raw, 0x198, case['unknown198'])
            struct.pack_into('<I', raw, 0x224, case['unknown224'])
        if case['pattern']:
            struct.pack_into('<16H', raw, 0x1AE, *(0x8000 + i * 41 for i in range(16)))
        raws.append(bytes(raw))
        memory[engine.RAW + index * 0x1000] = raw + bytearray([165]) * 64
        memory[engine.MID + index * 0x1000] = bytearray(736 + 64)
    memory[0x54F960] = bytearray(16)
    del memory[0x54F998]
    # Terrain native writer takes its timestamp salt from the prior10bytes.
    memory[0x55092C] = memory.pop(0x550936)
    size = max(0x4000, (56 + 552 * len(raws) + 4095) & ~4095)
    memory[engine.writer.OUTPUT] = bytearray([165]) * size
    sink = bytes(memory[engine.writer.CODE]).replace(engine.p32(engine.writer.OUTPUT + 0x4000), engine.p32(engine.writer.OUTPUT + size))
    memory[engine.writer.CODE] = bytearray(sink)
    calls = []
    for index in range(len(raws)):
        calls += [dict(address=0x42ED90, ecx=engine.MID + index * 0x1000, stack=[engine.RAW + index * 0x1000]),
            dict(address=0x49F5E0, ecx=0, stack=[engine.MID + index * 0x1000])]
    calls += [dict(address=0x4A7690, ecx=0x54F550, stack=[engine.writer.FILE])]
    spans = sorted((a, len(b)) for a, b in memory.items())
    assert all(a + size <= b for (a, size), (b, _) in zip(spans, spans[1:]))
    return dict(raw_records_hex=[raw.hex() for raw in raws], memory=[engine.region(a, b) for a, b in sorted(memory.items())],
        providers=template['providers'], calls=calls)


def path_for(case, fixture):
    tag = sha(json.dumps(fixture, sort_keys=True).encode())[:12]
    return ROOT / 'ghidra' / f'phase128-Terrain-model-{case["name"]}-{tag}.json'


def verify(case, fixture, result):
    assert result['success'] and not result['failures'] and result['error'] is None
    assert result['source_exe_sha256'] == '989c45df779bccfc9a9e509b2ad0224a9bd269629f74792adc0320fa4b99fb09'
    assert result['code_substitutions'] == 0
    inputs = {int(row['address'], 16): bytes.fromhex(row['hex']) for row in fixture['memory']}
    output = {int(row['address'], 16): bytes.fromhex(row['hex']) for row in result['output']}
    raws = [bytes.fromhex(raw) for raw in fixture['raw_records_hex']]
    data = output[engine.writer.OUTPUT][:56 + 552 * len(raws)]
    sections = engine.fmt.parse(data)
    assert len(sections) == 1 and sections[0].signature == 0x6E
    differences = []
    for index, (raw, out) in enumerate(zip(raws, sections[0].records)):
        changes = [(hex(offset), x, y) for offset, (x, y) in enumerate(zip(raw, out)) if x != y]
        if changes:
            differences.append(dict(record=index, ID=raw[0x1D6], differences=changes))
    assert sections[0].records == raws, differences
    assert b''.join(section.pack() for section in sections) == data
    heap = output[engine.HEAP]
    vector, count, capacity, _ = struct.unpack('<4I', output[0x54F960])
    assert count == len(raws) and capacity >= count
    models = []
    for index, raw in enumerate(raws):
        ptr = struct.unpack_from('<I', heap, vector - engine.HEAP + 4 * index)[0]
        model = heap[ptr - engine.HEAP:ptr - engine.HEAP + 0x3D8]
        assert model[0x3D0:0x3D2] == raw[0x198:0x19A]
        assert struct.unpack_from('<d', model, 0x320)[0] == struct.unpack_from('<I', raw, 0x224)[0] / 4096
        assert model[0x328:0x348] == raw[0x1AE:0x1CE]
        assert output[engine.RAW + index * 0x1000] == inputs[engine.RAW + index * 0x1000]
        assert output[engine.MID + index * 0x1000][-64:] == bytes(64)
        models.append(dict(address=hex(ptr), sha256=sha(model)))
    assert output[engine.writer.OUTPUT][len(data):] == bytes([165]) * (len(output[engine.writer.OUTPUT]) - len(data))
    assert struct.unpack('<3I', output[engine.writer.META]) == (3 + len(raws), engine.writer.OUTPUT + len(data), 0)
    assert struct.unpack_from('<I', data, 8)[0] == len(data)
    assert struct.unpack_from('<I', data, 20)[0] == len(raws)
    assert struct.unpack_from('<I', data, 52)[0] == 552 * len(raws)
    for address, count in [(0x42ED90, len(raws)), (0x49F5E0, len(raws)), (0x4250D0, len(raws)), (0x4A7690, 1)]:
        assert result['visited_instructions'].get(hex(address)) == count
    import capstone
    md = capstone.Cs(capstone.CS_ARCH_X86, capstone.CS_MODE_32)
    md.detail = True
    blob = engine.EXE.read_bytes()
    for row in result['low_reads']:
        pc = int(row['pc'], 16)
        ins = next(md.disasm(pe_read(blob, pc, 15), pc))
        assert row['address'] == 0 and row['size'] == 4 and 0x64 in ins.prefix and 'fs:[0]' in ins.op_str
    assert not struct.unpack_from('<I', output[engine.ALLOC], 8)[0]
    return dict(records=len(raws), all_record_bytes_preserved=True,
        unknown198_preserved=True, unknown224_unsigned_div4096_model_roundtrip=True,
        array16_words_preserved=True, complete_output_bytes=len(data), native_array_capacity=capacity,
        selected_model_field_bytes_verified=len(models) * 42,
        model_bytes_fingerprinted=len(models) * 0x3D8, instructions=result['instruction_count'], models=models)


def main():
    rows = []
    for case in cases():
        fixture = build(case)
        path = path_for(case, fixture)
        if path.exists():
            saved = json.loads(path.read_text(encoding='utf8'))
            assert saved['case'] == case and saved['input'] == fixture
        else:
            saved = dict(case=case, input=fixture, result=engine.run(fixture))
            path.write_text(json.dumps(saved, indent=2), encoding='utf8')
        try:
            outcome = verify(case, fixture, saved['result'])
        except AssertionError:
            print('Failed', case['name'], saved['result']['success'], saved['result']['error'], saved['result']['registers'])
            raise
        rows.append(dict(case=case['name'], file=str(path.relative_to(ROOT)), sha256=sha(path.read_bytes()), outcome=outcome))
        print(case['name'], outcome['records'], 'records passed')
    (ROOT / 'phase128-terrain-model-emulations.json').write_text(json.dumps(dict(scope=__doc__, replays=rows), indent=2), encoding='utf8')


if __name__ == '__main__':
    main()
