"""Independent straight-line projection of native4798F0, checked by emulation.

This is prepared expected data, not a replacement for the native initializer.
"""
from pathlib import Path
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from capstone.x86 import X86_OP_IMM,X86_OP_MEM,X86_OP_REG
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent
BASE=0x699688;SIZE=0x800
def initialized_categories():
    data=(ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe').read_bytes()
    table=bytearray(pe_read(data,BASE,SIZE));regs={};stack=[];written=set();md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True
    for x in md.disasm(pe_read(data,0x4798f0,0x47a77f-0x4798f0),0x4798f0):
        ops=x.operands
        def value(o):
            if o.type==X86_OP_IMM:return o.imm&0xffffffff
            assert o.type==X86_OP_REG;return regs[x.reg_name(o.reg)]
        if x.mnemonic=='mov':
            dst,src=ops;v=value(src)
            if dst.type==X86_OP_REG:regs[x.reg_name(dst.reg)]=v
            else:
                assert dst.type==X86_OP_MEM and not dst.mem.base and not dst.mem.index
                o=dst.mem.disp-BASE;assert 0<=o<=SIZE-dst.size
                table[o:o+dst.size]=v.to_bytes(dst.size,'little');written.update(range(o,o+dst.size))
        elif x.mnemonic=='xor':assert ops[0].reg==ops[1].reg;regs[x.reg_name(ops[0].reg)]=0
        elif x.mnemonic=='or':
            name=x.reg_name(ops[0].reg);v=value(ops[1]);assert name in regs or v==0xffffffff
            regs[name]=regs.get(name,0)|v
        elif x.mnemonic=='push':stack.append(regs.get(x.reg_name(ops[0].reg),0))
        elif x.mnemonic=='pop':regs[x.reg_name(ops[0].reg)]=stack.pop()
        else:assert x.mnemonic=='ret' and x.address==0x47a77e and not stack
    assert int.from_bytes(table[12*64+4:12*64+8],'little')==0x47b8c0
    return bytes(table),len(written)
