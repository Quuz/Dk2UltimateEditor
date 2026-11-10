# Phase133 observation-only derivative of frozen native_unicorn_factorytrace.py.
# Additional hooks read original HP arguments and AoE entry state. No register,
# memory, instruction or vtable substitution; frozen source stays unchanged.
"""Observation-only derivative for original area damage; frozen backends unchanged.

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
    hp_requests, aoe_entries, call_edges = [], [], set()
    line_calls, lattice_captures, frame_entries = [], [], []
    lattice_pointer = None
    deform_pointer = None
    deform_captures = []
    terrain_sample_pointer = None
    terrain_samples = []
    shape_calls = []
    shape_stages = []
    derived_sample_calls = []
    previous_pc, previous_size = None, 0
    trace_tail = []
    observed_writes = []
    observed_reads = []

    def on_code(uc, address, size, _):
        nonlocal previous_pc, previous_size, lattice_pointer, deform_pointer, terrain_sample_pointer
        if previous_pc is not None:
            ins = bytes(uc.mem_read(previous_pc, previous_size))
            if ins[0] == 0xE8 or (len(ins)>1 and ins[0] == 0xFF and ((ins[1]>>3)&7) == 2):
                call_edges.add((previous_pc,address))
        previous_pc, previous_size = address, size
        if address == 0x59F3C0:
            sp = uc.reg_read(xc.UC_X86_REG_ESP)
            lattice_pointer = struct.unpack('<I',bytes(uc.mem_read(sp+4,4)))[0]
        if address == 0x59F4A3:
            assert lattice_pointer is not None
            lattice_captures.append(dict(address=lattice_pointer,hex=bytes(uc.mem_read(lattice_pointer,324)).hex()))
        if address == 0x59DD30:
            sp = uc.reg_read(xc.UC_X86_REG_ESP)
            words = struct.unpack('<10I',bytes(uc.mem_read(sp+4,40)))
            line_calls.append(dict(arguments_u32=list(words),arguments_float=list(struct.unpack('<10f',bytes(uc.mem_read(sp+4,40))))))
        if address == 0x59F4B0:
            sp = uc.reg_read(xc.UC_X86_REG_ESP)
            output, occupancy, samples = struct.unpack('<3I',bytes(uc.mem_read(sp+4,12)))
            shape_calls.append(dict(output=output,occupancy=occupancy,samples=samples,
                input_grid_hex=bytes(uc.mem_read(output,324)).hex(),
                occupancy_hex=bytes(uc.mem_read(occupancy,64)).hex(),
                samples_hex=bytes(uc.mem_read(samples,540)).hex()))
        if address == 0x59E480:
            sp = uc.reg_read(xc.UC_X86_REG_ESP)
            output = struct.unpack('<I',bytes(uc.mem_read(sp+16,4)))[0]
            derived_sample_calls.append(dict(output=output,before_hex=bytes(uc.mem_read(output,540)).hex()))
        if address == 0x5A0895:
            derived_sample_calls[-1]['after_hex']=bytes(uc.mem_read(derived_sample_calls[-1]['output'],540)).hex()
        if address == 0x5A08C4:
            shape_calls[-1]['output_grid_hex']=bytes(uc.mem_read(shape_calls[-1]['output'],324)).hex()
        if address == 0x59F6BC:
            sp = uc.reg_read(xc.UC_X86_REG_ESP)
            shape_stages.append(dict(stage='temporary_vectors',hex=bytes(uc.mem_read(sp+0x14,324)).hex()))
        if address == 0x59ED80:
            sp = uc.reg_read(xc.UC_X86_REG_ESP)
            deform_pointer = struct.unpack('<I',bytes(uc.mem_read(sp+4,4)))[0]
        if address == 0x59E210:
            sp = uc.reg_read(xc.UC_X86_REG_ESP)
            terrain_sample_pointer = struct.unpack('<I',bytes(uc.mem_read(sp+16,4)))[0]
            terrain_samples.append(dict(address=terrain_sample_pointer,before_hex=bytes(uc.mem_read(terrain_sample_pointer,540)).hex()))
        if address == 0x59E4AC:
            assert terrain_sample_pointer is not None
            terrain_samples[-1]['hex']=bytes(uc.mem_read(terrain_sample_pointer,540)).hex()
        if address == 0x5A094A:
            assert deform_pointer is not None
            deform_captures.append(dict(address=lattice_pointer,hex=bytes(uc.mem_read(lattice_pointer,324)).hex(),
                deformation_delta_hex=bytes(uc.mem_read(deform_pointer,324)).hex()))
        if address == 0x5A01A0:
            sp = uc.reg_read(xc.UC_X86_REG_ESP)
            render,position,last = struct.unpack('<3I',bytes(uc.mem_read(sp+4,12)))
            frame_entries.append(dict(render=render,position=list(struct.unpack('<3f',bytes(uc.mem_read(position,12)))),last_argument=last,
                phase_before_hex=bytes(uc.mem_read(0x795844,8)).hex()))
        if address == 0x4B2CE0:
            target = uc.reg_read(xc.UC_X86_REG_ECX)
            sp = uc.reg_read(xc.UC_X86_REG_ESP)
            delta, owner, mode = struct.unpack('<3I', bytes(uc.mem_read(sp+4, 12)))
            hp_requests.append(dict(target=target, delta=delta if delta<0x80000000 else delta-0x100000000,
                owner_u32=owner, owner_u16=owner & 65535, mode=mode,
                caller_return=hex(struct.unpack('<I', bytes(uc.mem_read(sp, 4)))[0]),
                HP_before=struct.unpack('<i', bytes(uc.mem_read(target+0x40, 4)))[0]))
        if address in [0x4AC3C0,0x4AC5A0,0x4AC6D0,0x4AC940,0x4ACAF0]:
            node = uc.reg_read(xc.UC_X86_REG_ECX)
            aoe_entries.append(dict(entry=hex(address), instance=node,
                data1=struct.unpack('<I', bytes(uc.mem_read(node+0x13D,4)))[0],
                data2=struct.unpack('<I', bytes(uc.mem_read(node+0x141,4)))[0],
                health=struct.unpack('<i', bytes(uc.mem_read(node+0x40,4)))[0]))
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
                  hp_requests=hp_requests, aoe_entries=aoe_entries, observed_call_edges=sorted(call_edges),
                  line_calls=line_calls, lattice_captures=lattice_captures, frame_entries=frame_entries, deform_captures=deform_captures, terrain_samples=terrain_samples, shape_calls=shape_calls, shape_stages=shape_stages, derived_sample_calls=derived_sample_calls,
                  stack_at_stop_hex=diagnostic_stack,
                  allowed_external_IO_code=args.get('allowed_external_IO_code', []),
                  function_or_vtable_substitutions=0, missing_memory_mapping_policy='fatal; no lazy mapping')
    return result
