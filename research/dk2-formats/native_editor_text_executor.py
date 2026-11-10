"""Observation-only derivative of frozen phase127editorCPU: capture/audit lowpage writes and full lowpage output.

Originalnative execution, providers, code/readonly guards unchanged. No old
module modified and no instruction/register/vtable substitution introduced.
"""
import hashlib,json,pefile,struct,sys
from emulate_room_model_chain import ROOT,EXE,STACK,STOP,p32,region,writer
from audit_targeted_spell import executable_ranges

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
    low_writes = []

    def code(uc, address, size, _):
        visits[hex(address)] = visits.get(hex(address), 0) + 1
        if not any(a <= address < a + n for a, n in ranges + case['providers']):
            failures.append(['execution_outside_original_PE_or_explicit_provider', hex(address)])
            uc.emu_stop()

    def written(uc, access, address, size, value, _):
        if address < 4096:
            low_writes.append(dict(address=address,size=size,value=value,pc=hex(uc.reg_read(xc.UC_X86_REG_EIP))))
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
        for row in case['memory'] if int(row['address'], 16) not in [STACK - 0x4000]]
    return dict(success=not error and not failures and len(calls) == len(case['calls']) and all(c['returned'] for c in calls),
        error=error, failures=failures, low_reads=low_reads, low_writes=low_writes, calls=calls, registers=regs,
        instruction_count=sum(visits.values()), visited_instructions=visits, output=outputs,
        source_exe_sha256=hashlib.sha256(blob).hexdigest(), code_substitutions=0,
        callback_models=callback_models, before_writer_unassigned_byte=locals().get('before_writer_unassigned_byte'),
        external_provider_scope='CFile sink, five prior clock/TLS/error providers and HeapAlloc/HeapFree/lstrlen/interlocked/critical-section IAT adapters only.')
