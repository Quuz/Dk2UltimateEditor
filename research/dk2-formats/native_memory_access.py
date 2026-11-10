"""Read-candidate classification tolerant of missing Capstone TEST metadata.

Data-read search policy only,not object identity or x86 emulator semantics.
Control-transfer/address-only operands are deliberately separate from fields.
"""
from capstone import CS_AC_READ
from capstone.x86 import X86_OP_MEM

def memory_read_reason(ins,operand):
    if operand.type!=X86_OP_MEM or ins.mnemonic in {'lea','call','jmp'}:return None
    if operand.access&CS_AC_READ:return 'capstone_read'
    if ins.mnemonic in {'test','cmp'}:return 'opcode_read_fallback'
    return None

def reads_memory(ins,operand):return memory_read_reason(ins,operand) is not None
