"""Read-only wander target/radius/query observations; frozen factorytrace unchanged.

No imported OS API stubs, instruction substitutions or lazy zero-memory mapping.
Code/write hooks only observe or reject execution. Stop at the prepared caller
return address before observation code; inspect memory from the host. Input PE
is never executed by Windows.
"""
from pathlib import Path
import hashlib
import json
import struct
import sys

ROOT = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT / 'dependencies/unicorn-2.1.4'))
import unicorn
from unicorn import Uc, UcError, UC_ARCH_X86, UC_MODE_32, UC_HOOK_CODE, UC_HOOK_MEM_WRITE, UC_HOOK_MEM_READ
from unicorn import x86_const as xc
from audit_targeted_spell import executable_ranges


def sha(data):
    return hashlib.sha256(data).hexdigest()


def pe_image(data):
    pe = struct.unpack_from('<I', data, 60)[0]
    count = struct.unpack_from('<H', data, pe+6)[0]
    optional = pe+24
    optional_size = struct.unpack_from('<H', data, pe+20)[0]
    base = struct.unpack_from('<I', data, optional+28)[0]
    size, headers = struct.unpack_from('<II', data, optional+56)
    image = bytearray(size)
    image[:headers] = data[:headers]
    for i in range(count):
        row = optional+optional_size+40*i
        rva, raw_size, raw_offset = struct.unpack_from('<III', data, row+12)
        image[rva:rva+raw_size] = data[raw_offset:raw_offset+raw_size]
    return base, image


def run(exe, args, checks, stop, capture_address, capture_size):
    data = Path(exe).read_bytes()
    base, image = pe_image(data)
    cpu = Uc(UC_ARCH_X86, UC_MODE_32)
    pages = set()

    def ensure(address, size):
        for page in range(address & ~4095, (address+size+4095) & ~4095, 4096):
            if page not in pages:
                cpu.mem_map(page, 4096)
                pages.add(page)

    ensure(base, len(image))
    cpu.mem_write(base, bytes(image))
    regions = [(int(r['address'], 16), bytes.fromhex(r['hex']))
               for r in json.loads(args['memory'])['regions']]
    code_ranges = executable_ranges(data)
    # Original graphics vtables are data, and are protected independently.
    protected = code_ranges + [(0x66FCCC, 0xC0), (0x66D524, 0x130), (0x66D654, 0x80), (0x66E3EC, 0x3A0), (0x670574, 0x200)]
    for address, blob in regions:
        assert all(address+len(blob) <= p or address >= p+n for p, n in protected)
        ensure(address, len(blob))
        cpu.mem_write(address, blob)
    constants = {name: getattr(xc, 'UC_X86_REG_'+name) for name in
                 ['EAX', 'EBX', 'ECX', 'EDX', 'ESI', 'EDI', 'ESP', 'EBP', 'EIP', 'EFLAGS']}
    for name in ['EAX', 'EBX', 'ECX', 'EDX', 'ESI', 'EDI']:
        cpu.reg_write(constants[name], 0)
    cpu.reg_write(xc.UC_X86_REG_ESP, 0x7FFF0000)
    cpu.reg_write(xc.UC_X86_REG_EBP, 0x7FFF0000)
    cpu.reg_write(xc.UC_X86_REG_EFLAGS, 2)
    cpu.reg_write(xc.UC_X86_REG_FPCW, 0x37F)
    cpu.reg_write(xc.UC_X86_REG_MXCSR, 0x1F80)
    for name, value in json.loads(args['registers']).items():
        if name == 'DF':
            assert int(value, 0) == 0
        else:
            cpu.reg_write(constants[name], int(value, 0))
    visited, write_failure = {}, []
    trace_tail = []
    observed_writes = []
    observed_reads = []
    wander_calls = []
    active_wander = None
    wander_rng_calls = []

    def on_code(uc, address, size, _):
        nonlocal active_wander
        if address == 0x480A00:
            assert active_wander is None
            top = uc.reg_read(xc.UC_X86_REG_ESP)
            target, radius = struct.unpack('<2I', bytes(uc.mem_read(top+4,8)))
            active_wander = dict(radius=radius&255,target_pointer=target,bounds_queries=[])
        if address == 0x509090 and active_wander is not None:
            top = uc.reg_read(xc.UC_X86_REG_ESP)
            caller = int.from_bytes(bytes(uc.mem_read(top,4)),'little')
            if 0x480A00 <= caller < 0x480EC4:
                active_wander['bounds_queries'].append(list(struct.unpack('<2i',bytes(uc.mem_read(top+4,8)))))
        if address in [0x480E92,0x480E9E]:
            assert active_wander is not None
            returned = uc.reg_read(xc.UC_X86_REG_EAX)
            active_wander.update(returned=returned,target=list(struct.unpack('<3I',bytes(uc.mem_read(active_wander['target_pointer'],12)))) if returned else None)
            wander_calls.append(active_wander)
            active_wander = None
        if address == 0x5558D0:
            top = uc.reg_read(xc.UC_X86_REG_ESP)
            bound, pointer = struct.unpack('<2I',bytes(uc.mem_read(top+4,8)))
            world = int.from_bytes(bytes(uc.mem_read(0x6E5050,4)),'little')
            if pointer == world+0x15A0:
                wander_rng_calls.append(dict(bound=bound,seed_pointer=pointer))
        visited[address] = visited.get(address, 0)+1
        trace_tail.append(address)
        del trace_tail[:-24]
        if not any(p <= address < p+n for p, n in code_ranges+args.get('allowed_external_IO_code', [])):
            write_failure.append(dict(reason='execution outside original PE code', address=address))
            uc.emu_stop()

    def on_write(uc, access, address, size, value, _):
        if any(address+size > p and address < p+n for p, n in args.get('observe_writes', [])):
            observed_writes.append(dict(pc=hex(uc.reg_read(xc.UC_X86_REG_EIP)), address=hex(address), size=size, value=value))
        if any(address+size > p and address < p+n for p, n in protected):
            write_failure.append(dict(reason='write to protected original code/vtable', address=address, size=size))
            uc.emu_stop()

    def on_read(uc, access, address, size, value, _):
        if any(address+size > p and address < p+n for p, n in args.get('observe_reads', [])):
            observed_reads.append(dict(pc=hex(uc.reg_read(xc.UC_X86_REG_EIP)), address=hex(address), size=size,
                                       value_hex=bytes(uc.mem_read(address, size)).hex()))

    cpu.hook_add(UC_HOOK_MEM_READ, on_read)
    cpu.hook_add(UC_HOOK_CODE, on_code)
    cpu.hook_add(UC_HOOK_MEM_WRITE, on_write)
    error = None
    before_calls = []
    try:
        for call in args.get('before_calls', []):
            for name in ['EAX', 'EBX', 'ECX', 'EDX', 'ESI', 'EDI']:
                cpu.reg_write(constants[name], 0)
            cpu.reg_write(xc.UC_X86_REG_ESP, 0x7FFF0000)
            cpu.reg_write(xc.UC_X86_REG_EBP, 0x7FFF0000)
            cpu.reg_write(xc.UC_X86_REG_EFLAGS, 2)
            for name, value in call.get('registers', {}).items():
                cpu.reg_write(constants[name], int(value, 0))
            stack_words = [stop]+call.get('stack_args', [])
            cpu.mem_write(0x7FFF0000, struct.pack('<'+'I'*len(stack_words), *stack_words))
            cpu.emu_start(int(call['address'], 16), stop, timeout=5_000_000,
                          count=args.get('max_steps', 100000))
            ok = cpu.reg_read(xc.UC_X86_REG_EIP) == stop and not write_failure
            before_calls.append(dict(address=call['address'], returned=ok))
            if not ok:
                break
        if all(c['returned'] for c in before_calls):
            for name in ['EAX', 'EBX', 'ECX', 'EDX', 'ESI', 'EDI']:
                cpu.reg_write(constants[name], 0)
            cpu.reg_write(xc.UC_X86_REG_ESP, 0x7FFF0000)
            cpu.reg_write(xc.UC_X86_REG_EBP, 0x7FFF0000)
            cpu.reg_write(xc.UC_X86_REG_EFLAGS, 2)
            cpu.mem_write(0x7FFF0000, struct.pack('<I', stop))
            for name, value in json.loads(args['registers']).items():
                if name != 'DF':
                    cpu.reg_write(constants[name], int(value, 0))
            cpu.emu_start(int(args['address'], 16), stop, timeout=5_000_000,
                          count=args.get('max_steps', 100000))
    except UcError as exc:
        error = str(exc)
    registers = {name: hex(cpu.reg_read(register)) for name, register in constants.items()}
    mismatches = []
    for address, expected in checks:
        actual = bytes(cpu.mem_read(address, len(expected)))
        if actual != bytes(expected):
            mismatches.append(dict(address=hex(address), length=len(expected),
                                   differences=[dict(offset=i, actual=a, expected=b)
                                                for i, (a, b) in enumerate(zip(actual, expected)) if a != b]))
    for address, size in protected:
        assert bytes(cpu.mem_read(address, size)) == bytes(image[address-base:address-base+size])
    try:
        diagnostic_stack = bytes(cpu.mem_read(cpu.reg_read(xc.UC_X86_REG_ESP), 128)).hex()
    except UcError:
        diagnostic_stack = 'unmapped at ESP='+hex(cpu.reg_read(xc.UC_X86_REG_ESP))
    result = dict(backend='Unicorn', backend_version=unicorn.__version__,
                  source_exe_sha256=sha(data), entry_address=args['address'],
                  success=error is None and not write_failure and cpu.reg_read(xc.UC_X86_REG_EIP) == stop,
                  hit_return=cpu.reg_read(xc.UC_X86_REG_EIP) == stop,
                  emulation_error=error, registers=registers,
                  context_mismatches=mismatches, protected_write_or_execution_failures=write_failure,
                  instruction_count=sum(visited.values()), visited_instructions={hex(k): v for k, v in sorted(visited.items())},
                  capture_address=hex(capture_address), capture_size=capture_size,
                  captured_hex=bytes(cpu.mem_read(capture_address, capture_size)).hex(),
                  trace_tail=[hex(a) for a in trace_tail], before_calls=before_calls, observed_writes=observed_writes, observed_reads=observed_reads,
                  stack_at_stop_hex=diagnostic_stack,
                  allowed_external_IO_code=args.get('allowed_external_IO_code', []),
                  wander_target_calls=wander_calls,unfinished_wander_target=active_wander,wander_rng_calls=wander_rng_calls,function_or_vtable_substitutions=0, missing_memory_mapping_policy='fatal; no lazy mapping')
    return result
