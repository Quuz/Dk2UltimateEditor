"""Discovery: full original Room converter, allocated model callback and writer.

Only Win32 IAT providers and output sink are adapters. Native malloc/CFixedAlloc,
CString/resource parsing, constructors, registration and serializer execute.
CRT heap and empty CString pools are explicitly prepared; bootstrap is not run.
"""
from pathlib import Path
import hashlib, json, struct, sys
import pefile
import dk2_formats as fmt
import emulate_room_writer as writer
from audit_targeted_spell import executable_ranges

ROOT = Path(__file__).resolve().parent
EXE = writer.EXE
RAW, MID, HEAP, ALLOC, EXTRA = [0x17000000 + i * 0x100000 for i in range(5)]
HEAP_SIZE = 0x40000
STACK, STOP = writer.STACK, writer.STOP


def p32(value):
    return struct.pack('<I', value & 0xFFFFFFFF)


def region(address, blob):
    return dict(address=hex(address), hex=bytes(blob).hex())


def providers():
    # HeapAlloc(hHeap, flags, size): explicit bounded bump allocator. Log every
    # requested size/result, retain native malloc and CString allocator bodies.
    malloc = bytearray.fromhex('8b44240c8b0d') + p32(ALLOC)
    malloc += bytes.fromhex('8d140181fa') + p32(HEAP_SIZE) + bytes.fromhex('771f')
    malloc += bytes.fromhex('8915') + p32(ALLOC) + bytes.fromhex('81c1') + p32(HEAP)
    malloc += bytes.fromhex('8b15') + p32(ALLOC + 4)
    malloc += bytes.fromhex('8904d5') + p32(ALLOC + 0x100)
    malloc += bytes.fromhex('890cd5') + p32(ALLOC + 0x104)
    malloc += bytes.fromhex('ff05') + p32(ALLOC + 4)
    malloc += bytes.fromhex('8bc1c20c00')
    # The branch displacement above is patched to a bounded failure return.
    fail = len(malloc)
    malloc += bytes.fromhex('c705') + p32(ALLOC + 8) + p32(1) + bytes.fromhex('33c0c20c00')
    # Find the single short JA placeholder by exact opcode near the size check.
    at = malloc.index(bytes.fromhex('771f'))
    malloc[at + 1] = fail - at - 2
    strlen = bytes.fromhex('8b54240433c0803c0200740340ebf7c20400')
    dec = bytes.fromhex('8b5424048b02ff0a48c20400')
    inc = bytes.fromhex('8b5424048b02ff0240c20400')
    return {EXTRA: bytes(malloc), EXTRA + 0x100: strlen,
        EXTRA + 0x200: bytes.fromhex('c20400'), EXTRA + 0x300: dec,
        EXTRA + 0x400: inc, EXTRA + 0x500: bytes.fromhex('b801000000c20c00')}


def build(resource=None, record_ids=None):
    catalogue = [raw for section in fmt.parse((ROOT.parent.parent / 'Dungeon Keeper 2/Data/editor/Rooms.kwd').read_bytes())
        if section.signature == 0x78 for raw in section.records]
    selected = [raw for raw in catalogue if record_ids is None and raw[0x392] == 1
        or record_ids is not None and raw[0x392] in record_ids]
    assert selected and (record_ids is None or len(selected) == len(record_ids))
    raws = []
    for raw in selected:
        if resource is not None:
            assert len(resource) == 84
            raw = raw[:0x1C4] + resource + raw[0x218:]
        raws.append(raw)
    raw = raws[0]
    base = writer.build(slots=(1,), seed=165)
    memory = {int(row['address'], 16): bytearray.fromhex(row['hex']) for row in base['memory']}
    # Converter sets all presence fields; starting zero supplies defined opaque
    # padding but is not claimed to simulate the original parser initializer.
    for index, raw in enumerate(raws):
        memory[RAW + index * 0x1000] = bytearray(raw) + bytearray([165]) * 64
        memory[MID + index * 0x1000] = bytearray(1267 + 64)
    memory[HEAP] = bytearray([165]) * HEAP_SIZE
    memory[ALLOC] = bytearray(0x1000)
    memory[0] = bytearray(4096)  # FS:[0] exception-chain cell only.
    for address, blob in providers().items():
        memory[address] = bytearray(blob)
    # Original CRT _sbh_threshold=0 chooses the normal Windows heap path for
    # positive allocations. Heap handle is opaque and only seen by the provider.
    memory[0x53B578] = bytearray(p32(0))
    memory[0x5559FC] = bytearray(p32(0x1234))
    # Four explicitly initialized CFixedAlloc pools; node sizes accommodate
    # native CString headers and capacity. The original pool code allocates,
    # links and recycles these nodes; arbitrary valid block count4 is fixture.
    for address, capacity in [(0x552370, 64), (0x552348, 128), (0x552320, 256), (0x5522F8, 512)]:
        pool = bytearray(40)
        struct.pack_into('<2I', pool, 0, (capacity + 13 + 3) & ~3, 4)
        memory[address] = pool
    # The callback grows this native global CArray; writer uses the same global
    # catalogue context, not a host-substituted model pointer or registration.
    memory[0x54F998] = bytearray(16)
    for address, target in [(0x504258, EXTRA), (0x5041F4, EXTRA + 0x100),
            (0x504278, EXTRA + 0x200), (0x504274, EXTRA + 0x200),
            (0x504310, EXTRA + 0x300), (0x504328, EXTRA + 0x400),
            (0x504254, EXTRA + 0x500)]:
        memory[address] = bytearray(p32(target))
    output_size = max(0x4000, (56 + 1055 * len(raws) + 4095) & ~4095)
    if output_size > 0x4000:
        memory[writer.OUTPUT] = bytearray([165]) * output_size
        # Same explicit CFile sink as phase122, with its output bound enlarged
        # for the full catalogue; original native writer remains unchanged.
        sink = bytearray(memory[writer.CODE])
        old_bound, new_bound = p32(writer.OUTPUT + 0x4000), p32(writer.OUTPUT + output_size)
        assert sink.count(old_bound) == 1
        memory[writer.CODE] = bytearray(sink.replace(old_bound, new_bound))
        base['allowed_external_IO_code'][0][1] = len(sink)
    spans = sorted((address, len(data)) for address, data in memory.items())
    assert all(address + size <= next_address for (address, size), (next_address, _) in zip(spans, spans[1:])), spans
    calls = []
    for index in range(len(raws)):
        calls += [dict(address=0x42DF90, ecx=MID + index * 0x1000, stack=[RAW + index * 0x1000]),
            dict(address=0x49FC30, ecx=0, stack=[MID + index * 0x1000])]
    calls.append(dict(address=0x4A7AC0, ecx=0x54F550, stack=[writer.FILE]))
    return dict(raw_hex=raws[0].hex(), raw_records_hex=[r.hex() for r in raws],
        output_size=output_size, memory=[region(a, b) for a, b in sorted(memory.items())],
        providers=base['allowed_external_IO_code'] + [[a, len(b)] for a, b in providers().items()],
        calls=calls)


def run(case):
    sys.path.insert(0, str(ROOT / 'dependencies/unicorn-2.1.4'))
    import unicorn
    from unicorn import x86_const as xc
    blob = EXE.read_bytes()
    pe = pefile.PE(data=blob)
    image = pe.get_memory_mapped_image()
    cpu = unicorn.Uc(unicorn.UC_ARCH_X86, unicorn.UC_MODE_32)
    pages = set()

    def write(address, data):
        for page in range(address & ~4095, (address + len(data) + 4095) & ~4095, 4096):
            if page not in pages:
                cpu.mem_map(page, 4096)
                pages.add(page)
        cpu.mem_write(address, bytes(data))

    write(pe.OPTIONAL_HEADER.ImageBase, image)
    for row in case['memory']:
        write(int(row['address'], 16), bytes.fromhex(row['hex']))
    ranges = executable_ranges(blob)
    readonly = [(0x400000 + section.VirtualAddress, section.Misc_VirtualSize)
        for section in pe.sections if section.Characteristics & 0x40000000 and not section.Characteristics & 0x80000000]
    visits, failures, low_reads, calls, callback_models = {}, [], [], [], []

    def code(uc, address, size, _):
        visits[hex(address)] = visits.get(hex(address), 0) + 1
        if not any(a <= address < a + n for a, n in ranges + case['providers']):
            failures.append(['execution_outside_original_PE_or_explicit_provider', hex(address)])
            uc.emu_stop()

    def written(uc, access, address, size, value, _):
        if any(address + size > a and address < a + n for a, n in ranges + readonly):
            failures.append(['write_original_code_or_readonly', hex(address)])
            uc.emu_stop()

    def read(uc, access, address, size, value, _):
        if address < 4096:
            low_reads.append(dict(address=address, size=size, pc=hex(uc.reg_read(xc.UC_X86_REG_EIP))))

    def get(address, size):
        return bytes(cpu.mem_read(address, size))

    cpu.hook_add(unicorn.UC_HOOK_CODE, code)
    cpu.hook_add(unicorn.UC_HOOK_MEM_WRITE, written)
    cpu.hook_add(unicorn.UC_HOOK_MEM_READ, read)
    error = None
    try:
        for call in case['calls']:
            for name in ['EAX', 'EBX', 'ECX', 'EDX', 'ESI', 'EDI']:
                cpu.reg_write(getattr(xc, 'UC_X86_REG_' + name), 0)
            cpu.reg_write(xc.UC_X86_REG_ESP, STACK)
            cpu.reg_write(xc.UC_X86_REG_EBP, STACK)
            cpu.reg_write(xc.UC_X86_REG_EFLAGS, 2)
            cpu.reg_write(xc.UC_X86_REG_FPCW, 0x37F)
            cpu.reg_write(xc.UC_X86_REG_ECX, call['ecx'])
            cpu.mem_write(STACK, b''.join(p32(value) for value in [STOP] + call['stack']))
            if call['address'] == 0x4A7AC0:
                before_writer_unassigned_byte = get(STACK - 0x420 + 0x36F, 1)[0]
            cpu.emu_start(call['address'], STOP, timeout=10_000_000, count=1000000)
            ok = cpu.reg_read(xc.UC_X86_REG_EIP) == STOP and not failures
            calls.append(dict(address=hex(call['address']), returned=ok))
            if ok and call['address'] == 0x49FC30:
                array, count = struct.unpack('<2I', get(0x54F998, 8))
                pointer = struct.unpack('<I', get(array + (count - 1) * 4, 4))[0]
                callback_models.append(dict(address=hex(pointer), hex=get(pointer, 0x548).hex()))
            if not ok:
                break
    except unicorn.UcError as exc:
        error = str(exc)
    regs = {name: hex(cpu.reg_read(getattr(xc, 'UC_X86_REG_' + name))) for name in ['EAX', 'EIP', 'ESP', 'ECX']}
    outputs = [region(int(row['address'], 16), cpu.mem_read(int(row['address'], 16), len(bytes.fromhex(row['hex']))))
        for row in case['memory'] if int(row['address'], 16) not in [0, STACK - 0x4000]]
    return dict(success=not error and not failures and len(calls) == len(case['calls']) and all(c['returned'] for c in calls),
        error=error, failures=failures, low_reads=low_reads, calls=calls, registers=regs,
        instruction_count=sum(visits.values()), visited_instructions=visits, output=outputs,
        source_exe_sha256=hashlib.sha256(blob).hexdigest(), code_substitutions=0,
        callback_models=callback_models, before_writer_unassigned_byte=locals().get('before_writer_unassigned_byte'),
        external_provider_scope='CFile sink, five prior clock/TLS/error providers and HeapAlloc/HeapFree/lstrlen/interlocked/critical-section IAT adapters only.')


def cases():
    catalogue = [raw for section in fmt.parse((ROOT.parent.parent / 'Dungeon Keeper 2/Data/editor/Rooms.kwd').read_bytes())
        if section.signature == 0x78 for raw in section.records]
    rows = [dict(name='catalogue26', ids=[raw[0x392] for raw in catalogue], resource_hex=None, cached_index=None)]
    for name, text, kind, cached in [('named', b'UnknownSlot#9', 7, 9),
            ('type0', b'UnknownSlot#65537', 0, 1), ('negative', b'UnknownSlot#-1', 7, 65535),
            ('suffix', b'UnknownSlot#12junk', 7, 12)]:
        resource = bytearray((index * 37 + 11) & 255 for index in range(84))
        resource[:64] = text + b'\0' + bytes(63 - len(text))
        resource[80] = kind
        rows.append(dict(name=name, ids=[1], resource_hex=resource.hex(), cached_index=cached))
    return rows


def prepared(case):
    return build(None if case['resource_hex'] is None else bytes.fromhex(case['resource_hex']), case['ids'])


def result_path(case, prepared_case):
    tag = hashlib.sha256(json.dumps(prepared_case, sort_keys=True).encode()).hexdigest()[:12]
    return ROOT / 'ghidra' / f'phase127-Room-native-model-{case["name"]}-{tag}.json'


def verify(case, prepared_case, result):
    assert result['success'] and result['error'] is None and not result['failures']
    assert result['source_exe_sha256'] == '989c45df779bccfc9a9e509b2ad0224a9bd269629f74792adc0320fa4b99fb09'
    assert result['code_substitutions'] == 0
    output = {int(row['address'], 16): bytes.fromhex(row['hex']) for row in result['output']}
    inputs = {int(row['address'], 16): bytes.fromhex(row['hex']) for row in prepared_case['memory']}
    raws = [bytes.fromhex(value) for value in prepared_case['raw_records_hex']]
    assert len(result['calls']) == 2 * len(raws) + 1 and all(row['returned'] for row in result['calls'])
    md = __import__('capstone').Cs(__import__('capstone').CS_ARCH_X86, __import__('capstone').CS_MODE_32)
    md.detail = True
    image = pefile.PE(data=EXE.read_bytes()).get_memory_mapped_image()
    for row in result['low_reads']:
        address = int(row['pc'], 16)
        ins = next(md.disasm(image[address - 0x400000:address - 0x400000 + 15], address))
        assert row['address'] == 0 and row['size'] == 4 and 0x64 in ins.prefix and 'fs:[0]' in ins.op_str
    vector, count, capacity, growth = struct.unpack('<4I', output[0x54F998])
    assert count == len(raws) and capacity >= count and growth == 0
    used, allocations, failed = struct.unpack_from('<3I', output[ALLOC])
    assert not failed and used <= HEAP_SIZE and allocations < (0x1000 - 0x100) // 8
    heap, position = output[HEAP], 0
    for index in range(allocations):
        size, address = struct.unpack_from('<2I', output[ALLOC], 0x100 + 8 * index)
        assert address == HEAP + position and size > 0 and size % 16 == 0
        position += size
    assert position == used and heap[used:] == bytes([165]) * (HEAP_SIZE - used)
    models = []
    for index, raw in enumerate(raws):
        model_address = struct.unpack_from('<I', heap, vector - HEAP + 4 * index)[0]
        model = heap[model_address - HEAP:model_address - HEAP + 0x548]
        assert len(model) == 0x548 and model[:4] == p32(0x50B5A8)
        for raw_offset, model_offset in writer.api.RESOURCES:
            assert model[model_offset:model_offset + 84] == raw[raw_offset:raw_offset + 84]
        if case['cached_index'] is not None:
            assert int.from_bytes(model[0x2D8:0x2DA], 'little') == case['cached_index']
        assert result['callback_models'][index] == dict(address=hex(model_address), hex=model.hex())
        models.append(dict(address=hex(model_address), sha256=hashlib.sha256(model).hexdigest()))
        assert output[RAW + index * 0x1000] == inputs[RAW + index * 0x1000]
        assert output[MID + index * 0x1000][-64:] == inputs[MID + index * 0x1000][-64:]
    sink = output[writer.OUTPUT]
    data = sink[:56 + 1055 * len(raws)]
    sections = fmt.parse(data)
    assert len(sections) == 1 and sections[0].signature == 0x78 and sections[0].records == raws
    assert b''.join(section.pack() for section in sections) == data
    assert sink[len(data):] == bytes([165]) * (len(sink) - len(data))
    assert struct.unpack_from('<I', data, 8)[0] == len(data)
    assert struct.unpack_from('<I', data, 20)[0] == len(raws)
    assert struct.unpack_from('<I', data, 52)[0] == 1055 * len(raws)
    assert struct.unpack('<3I', output[writer.META]) == (3 + len(raws), writer.OUTPUT + len(data), 0)
    for key in [writer.M1, writer.M2, writer.CTX, writer.ARRAY, writer.FILE, writer.FVT, writer.PTD]:
        assert output[key] == inputs[key]
    native_entries = result['visited_instructions']
    for address, expected_count in [(0x42DF90, len(raws)), (0x49FC30, len(raws)),
            (0x424780, len(raws)), (0x4A7AC0, 1), (0x4AB320, 1)]:
        assert native_entries.get(hex(address)) == expected_count
    assert native_entries.get('0x41bb90', 0) == 11 * len(raws)
    assert native_entries.get('0x41c0b0', 0) == 11 * len(raws)
    return dict(records=len(raws), output_records_byte_identical=True,
        complete_output_bytes=len(data), resource_blocks_checked=11 * len(raws),
        model_bytes_checked=0x548 * len(raws), allocations=allocations, allocated_bytes=used,
        native_array_count=count, native_array_capacity=capacity,
        unknown_resource_index_cache=case['cached_index'], models=models,
        unassigned_byte_before_writer=result['before_writer_unassigned_byte'],
        instructions=result['instruction_count'], low_page_reads_only_FS0=True)


def accepted_main():
    rows = []
    for case in cases():
        prepared_case = prepared(case)
        path = result_path(case, prepared_case)
        if path.exists():
            saved = json.loads(path.read_text(encoding='utf8'))
            assert saved['case'] == case and saved['input'] == prepared_case
        else:
            saved = dict(case=case, input=prepared_case, result=run(prepared_case))
            path.write_text(json.dumps(saved, indent=2), encoding='utf8')
        outcome = verify(case, prepared_case, saved['result'])
        rows.append(dict(case=case['name'], file=str(path.relative_to(ROOT)),
            sha256=hashlib.sha256(path.read_bytes()).hexdigest(), outcome=outcome))
        print(case['name'], 'accepted', outcome['records'], outcome['instructions'], flush=True)
    (ROOT / 'phase127-Room-model-chain-emulations.json').write_text(json.dumps(dict(scope=__doc__, replays=rows,
        original_full_MFC_GUI_executed=False, original_parser_initializer_executed=False), indent=2), encoding='utf8')


def main():
    case = build()
    result = run(case)
    tag = hashlib.sha256(json.dumps(case, sort_keys=True).encode()).hexdigest()[:12]
    path = ROOT / 'ghidra' / f'phase127-Room-model-discovery-{tag}.json'
    path.write_text(json.dumps(dict(input=case, result=result), indent=2), encoding='utf8')
    print('Saved', path.name)
    print({key: value for key, value in result.items() if key not in ['output', 'visited_instructions', 'low_reads']})
    print('last visits', list(result['visited_instructions'])[-15:])


if __name__ == '__main__':
    accepted_main() if '--accepted' in sys.argv else main()
