"""Exact supplied-DX Object state registry, from original initializer instructions."""
import hashlib
import json
import struct
from pathlib import Path
from capstone import Cs, CS_ARCH_X86, CS_MODE_32
from capstone.x86 import X86_OP_REG, X86_OP_IMM, X86_OP_MEM
from build_effect_semantics import EXE, pe_read
from emulate_element_colors import region
import native_unicorn_fieldtrace as cpu

ROOT = Path(__file__).resolve().parent
BASE, CALLBACK_BASE, COUNT, SIZE = 0x6E4B38, 0x6E4B3C, 59, 1180
INITIALIZER, INITIALIZER_SIZE, STOP = 0x49E600, 2241, 0x10060000


def projected_table():
    # The first ID DWORD is not written; its normal PE/BSS initial value is zero.
    out, registers, written = bytearray(SIZE), {}, set()
    md = Cs(CS_ARCH_X86, CS_MODE_32)
    md.detail = True
    instructions = list(md.disasm(pe_read(EXE.read_bytes(), INITIALIZER, INITIALIZER_SIZE), INITIALIZER))
    assert instructions[-1].address == 0x49EEC0 and instructions[-1].mnemonic == 'ret'
    def value(op):
        if op.type == X86_OP_IMM:
            return op.imm & 0xFFFFFFFF
        assert op.type == X86_OP_REG and op.reg in registers
        return registers[op.reg]
    for ins in instructions:
        ops = ins.operands
        if ins.mnemonic in ['push', 'pop', 'ret']:
            continue
        if ins.mnemonic == 'xor':
            assert ops[0].type == ops[1].type == X86_OP_REG and ops[0].reg == ops[1].reg
            registers[ops[0].reg] = 0
        elif ins.mnemonic == 'or':
            assert ops[0].type == X86_OP_REG
            rhs = value(ops[1])
            registers[ops[0].reg] = 0xFFFFFFFF if rhs == 0xFFFFFFFF else value(ops[0]) | rhs
        else:
            assert ins.mnemonic == 'mov'
            if ops[0].type == X86_OP_REG:
                registers[ops[0].reg] = value(ops[1])
            else:
                op = ops[0]
                assert op.type == X86_OP_MEM and not op.mem.base and not op.mem.index and op.size == 4
                offset = op.mem.disp - BASE
                assert 4 <= offset <= SIZE - 4
                struct.pack_into('<I', out, offset, value(ops[1]))
                written.update(range(offset, offset + 4))
    assert written == set(range(4, SIZE))
    rows = [struct.unpack_from('<5I', out, i * 20) for i in range(COUNT)]
    assert [r[0] for r in rows] == list(range(COUNT))
    assert all(r[2:4] == (0, 0) for r in rows)
    assert rows[0] == (0, 0, 0, 0, 0xFFFFFFFF)
    assert all(r[4] == 0 for r in rows[1:])
    return bytes(out)


def initialized_table():
    expected = projected_table()
    stack = bytearray([0xA5]) * 16384
    struct.pack_into('<I', stack, 8192, STOP)
    initial = bytes(SIZE) + bytes([0xA5]) * 32
    args = dict(program='DKII-DX.exe', address=hex(INITIALIZER), max_steps=10000,
                registers=json.dumps(dict(ECX='0')),
                memory=json.dumps(dict(regions=[region(BASE, initial), region(0x7FFEE000, stack)])))
    backend = hashlib.sha256((ROOT/'native_unicorn_fieldtrace.py').read_bytes()).hexdigest()
    path = ROOT/'ghidra/phase110-object-state-initializer-v2.json'
    if path.exists():
        saved = json.loads(path.read_text(encoding='utf8'))
        assert saved['input'] == args and saved['backend_sha256'] == backend
    else:
        result = cpu.run(EXE, args, [(BASE, expected + bytes([0xA5]) * 32)], STOP, BASE, SIZE + 32)
        saved = dict(input=args, backend_sha256=backend, result=result)
        path.write_text(json.dumps(saved, indent=2), encoding='utf8')
    result = saved['result']
    assert result['success'] and result['hit_return'] and result['instruction_count'] == 360
    assert not result['context_mismatches'] and not result['protected_write_or_execution_failures']
    assert bytes.fromhex(result['captured_hex']) == expected + bytes([0xA5]) * 32
    return expected


if __name__ == '__main__':
    b = initialized_table()
    print(json.dumps(dict(record_base=hex(BASE), callback_base=hex(CALLBACK_BASE), count=COUNT,
                          bytes=len(b), table_sha256=hashlib.sha256(b).hexdigest())))
