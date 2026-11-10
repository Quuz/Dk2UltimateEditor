"""Bounded full-CFG prefix audit, through first C9 payload write.

Direct stack writes/PUSHes only. Bounded date/Good serializer and readonly file
callback contracts are separate; this is not a general interprocedural alias proof.
"""
from pathlib import Path
import re
from capstone import Cs,CS_ARCH_X86,CS_MODE_32,CS_AC_WRITE
from capstone.x86 import X86_OP_MEM,X86_OP_IMM,X86_OP_REG,X86_REG_ESP
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent

def cleanups():
    result={}
    for p in (ROOT/'ghidra').glob('phase32-prefix-callee-*.txt'):
        vals={int(x,0) if x else 0 for x in re.findall(r'^\w{8}: RET(?: (0x[0-9a-f]+))?\s*$',p.read_text(),re.M)}
        assert len(vals)==1,(p,vals);result[int(p.stem.split('-')[-1],16)]=vals.pop()
    assert len(result)==28
    return result

def inspect():
    data=(ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/DK2 Editor pro.exe').read_bytes();md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True
    ins={i.address:i for i in md.disasm(pe_read(data,0x4947C0,0x84C),0x4947C0)};todo=[(0x4947C0,0)];seen=set();writes=[];calls=[];ends=[];cleanup=cleanups()
    while todo:
        address,sp=todo.pop()
        if (address,sp) in seen:continue
        seen.add((address,sp))
        if address in [0x495008,0x49500C]:ends.append(sp);continue
        i=ins[address];name=i.mnemonic;nxt=address+i.size
        for o in i.operands:
            if o.type==X86_OP_MEM and o.access&CS_AC_WRITE and o.mem.base==X86_REG_ESP:
                assert not o.mem.index;writes.append(dict(address=hex(address),start=sp+o.mem.disp,bytes=o.size,text=name+' '+i.op_str))
        if name=='push':sp-=4;writes.append(dict(address=hex(address),start=sp,bytes=4,text=name+' '+i.op_str))
        elif name=='pop':sp+=4
        elif name in ['sub','add'] and i.operands[0].type==X86_OP_REG and i.operands[0].reg==X86_REG_ESP:
            assert i.operands[1].type==X86_OP_IMM;sp+=i.operands[1].imm*(1 if name=='add' else -1)
        elif name=='call':
            op=i.operands[0]
            if op.type==X86_OP_MEM:assert op.mem.disp in [4,0x14,0x18,0x28,0x34,0x38];n=8 if op.mem.disp==0x38 else 0
            else:n=cleanup[op.imm]
            calls.append(dict(address=hex(address),stack_before=sp,cleanup=n,target=i.op_str));sp+=n
        assert name!='ret'
        if name=='jmp':todo.append((i.operands[0].imm,sp))
        elif name.startswith('j'):todo.extend([(i.operands[0].imm,sp),(nxt,sp)])
        else:todo.append((nxt,sp))
    assert ends and all(sp==-0x3A8 for sp in ends)
    intervals=[[-0x22C,-0x20C],[-0x209,-0x201]]
    hits=[w for w in writes if any(w['start']<b and w['start']+w['bytes']>a for a,b in intervals)];assert not hits
    return dict(function=hex(0x4947C0),stop=hex(0x495008),instruction_states=len(seen),record_start_entry_sp=-0x22C,untouched_intervals_entry_sp=intervals,direct_write_hits=hits,writes=writes,calls=calls,
                scope=__doc__,helper_contracts=['Date4AB320 writes only supplied small header destinations, outside C9 scratch.', 'Good4952F0 writes32 bytes at supplied member slot, starting raw2B; previous standalone Good buffer ends immediately before party name.', 'Other getter calls receive model/count/index inputs, not a C9 name/reserved destination.', 'File virtual38 reads supplied buffer without modifying it.'])
if __name__=='__main__':
    import json
    r=inspect();print(json.dumps({k:v for k,v in r.items() if k not in ['writes','calls']},indent=2))
