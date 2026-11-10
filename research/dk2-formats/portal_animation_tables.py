"""Independent projection of native4950C0; compared to its execution, not patched."""
from pathlib import Path
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from capstone.x86 import X86_OP_REG,X86_OP_MEM,X86_OP_IMM
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent
BASE=0x6e4198;SIZE=81*29
def initialized_animations():
    data=(ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe').read_bytes();md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True
    regs={};aliases={};stack=[];table=bytearray([0xa5])*SIZE;written=set()
    for p,s in [('eax','ax'),('ebx','bx'),('ecx','cx'),('edx','dx'),('esi','si'),('edi','di'),('ebp','bp')]:
        aliases[s]=(p,0,16)
        if s in ['ax','bx','cx','dx']:aliases[s[0]+'l']=(p,0,8);aliases[s[0]+'h']=(p,8,8)
    def read(name):
        if name in aliases:p,shift,n=aliases[name];return regs[p]>>shift&((1<<n)-1)
        return regs[name]
    for x in md.disasm(pe_read(data,0x4950c0,0x49669d-0x4950c0),0x4950c0):
        o=x.operands
        def value(v):
            if v.type==X86_OP_IMM:return v.imm&0xffffffff
            assert v.type==X86_OP_REG;return read(x.reg_name(v.reg))
        if x.mnemonic=='mov':
            d,s=o;v=value(s)
            if d.type==X86_OP_REG:regs[x.reg_name(d.reg)]=v
            else:
                assert d.type==X86_OP_MEM and not d.mem.base and not d.mem.index;off=d.mem.disp-BASE;assert 0<=off<=SIZE-d.size
                table[off:off+d.size]=(v&((1<<(8*d.size))-1)).to_bytes(d.size,'little');written.update(range(off,off+d.size))
        elif x.mnemonic=='xor':assert o[0].reg==o[1].reg;regs[x.reg_name(o[0].reg)]=0
        elif x.mnemonic=='or':
            n=x.reg_name(o[0].reg);v=value(o[1]);assert n in regs or v==0xffffffff;regs[n]=regs.get(n,0)|v
        elif x.mnemonic=='push':stack.append(regs.get(x.reg_name(o[0].reg),0))
        elif x.mnemonic=='pop':regs[x.reg_name(o[0].reg)]=stack.pop()
        else:assert x.mnemonic=='ret' and x.address==0x49669c and not stack
    assert written==set(range(SIZE))
    return bytes(table)
