"""Derive the straight-line native AI table initializer for independent checking.

This projection is compared with execution of the original initializer in Ghidra.
It does not execute any callbacks stored in the table.
"""
from pathlib import Path
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from capstone.x86 import X86_OP_REG,X86_OP_IMM,X86_OP_MEM
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent
BASE=0x6DA8B0
SIZE=281*25

def initialized_states():
    data=(ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe').read_bytes()
    code=pe_read(data,0x47BBE0,18957);md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True
    registers={r:0 for r in ['eax','ebx','ecx','edx','esi','edi','ebp']}
    aliases={}
    for parent,short in [('eax','ax'),('ebx','bx'),('ecx','cx'),('edx','dx'),('esi','si'),('edi','di'),('ebp','bp')]:
        aliases[short]=(parent,0,16)
        if short in {'ax','bx','cx','dx'}:
            aliases[short[0]+'l']=(parent,0,8);aliases[short[0]+'h']=(parent,8,8)
    def read_reg(name):
        if name in registers:return registers[name]
        p,shift,bits=aliases[name];return registers[p]>>shift&((1<<bits)-1)
    table=bytearray([0xA5])*SIZE;written=set();writers={};stack=[];instructions=0
    for ins in md.disasm(code,0x47BBE0):
        instructions+=1;ops=ins.operands
        def value(op):
            if op.type==X86_OP_IMM:return op.imm
            assert op.type==X86_OP_REG;return read_reg(ins.reg_name(op.reg))
        if ins.mnemonic=='mov':
            dst,src=ops;v=value(src)
            if dst.type==X86_OP_REG:
                name=ins.reg_name(dst.reg);assert name in registers;registers[name]=v&0xFFFFFFFF
            else:
                assert dst.type==X86_OP_MEM and not dst.mem.base and not dst.mem.index
                offset=dst.mem.disp-BASE;assert 0<=offset<=SIZE-dst.size
                table[offset:offset+dst.size]=(v&((1<<(8*dst.size))-1)).to_bytes(dst.size,'little')
                for i in range(dst.size):written.add(offset+i);writers[offset+i]=ins.address
        elif ins.mnemonic=='xor':
            assert ops[0].reg==ops[1].reg;registers[ins.reg_name(ops[0].reg)]=0
        elif ins.mnemonic=='or':
            name=ins.reg_name(ops[0].reg);registers[name]=(registers[name]|value(ops[1]))&0xFFFFFFFF
        elif ins.mnemonic=='push':stack.append(value(ops[0]))
        elif ins.mnemonic=='pop':registers[ins.reg_name(ops[0].reg)]=stack.pop()
        elif ins.mnemonic=='ret':assert ins.address==0x4805EC and not stack
        else:raise AssertionError((hex(ins.address),ins.mnemonic,ins.op_str))
    assert written==set(range(SIZE)) and instructions==3113
    rows=[dict(state=i,category=table[25*i+23],category_writer=hex(writers[25*i+23]),
               callback=int.from_bytes(table[25*i:25*i+4],'little')) for i in range(281)]
    return bytes(table),rows
