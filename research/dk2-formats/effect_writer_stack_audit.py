"""Bounded CFG stack-offset audit of original Effects writer instructions.

Tracks direct ESP-relative writes, PUSHes and callee stack cleanup. Indirect
buffer writes/helpers are identified separately; this is not a general x86
alias/procedure verifier. Known-name precondition is CString length<=32.
"""
from pathlib import Path
from capstone import Cs,CS_ARCH_X86,CS_MODE_32,CS_AC_WRITE
from capstone.x86 import X86_OP_MEM,X86_OP_IMM,X86_OP_REG,X86_REG_ESP
from build_effect_semantics import pe_read

def inspect():
    data=(Path(__file__).parent.parent.parent/'Dungeon Keeper 2/Data/editor/DK2 Editor pro.exe').read_bytes()
    md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True
    instructions={i.address:i for i in md.disasm(pe_read(data,0x4AA040,0x363),0x4AA040)}
    todo=[(0x4AA040,0)];seen=set();writes=[];calls=[];returns=[]
    while todo:
        address,sp=todo.pop()
        if (address,sp) in seen:continue
        seen.add((address,sp));i=instructions[address];name=i.mnemonic;nxt=i.address+i.size
        for o in i.operands:
            if o.type==X86_OP_MEM and o.access&CS_AC_WRITE and o.mem.base==X86_REG_ESP:
                assert not o.mem.index
                writes.append(dict(address=i.address,entry_relative_start=sp+o.mem.disp,bytes=o.size,instruction=i.mnemonic+' '+i.op_str))
        if name=='push':
            sp-=4;writes.append(dict(address=i.address,entry_relative_start=sp,bytes=4,instruction=i.mnemonic+' '+i.op_str))
        elif name=='pop':sp+=4
        elif name in ['sub','add'] and i.operands[0].type==X86_OP_REG and i.operands[0].reg==X86_REG_ESP:
            assert i.operands[1].type==X86_OP_IMM;sp+=i.operands[1].imm*(1 if name=='add' else -1)
        elif name=='call':
            op=i.operands[0]
            if op.type==X86_OP_MEM:assert op.mem.disp==0x38;cleanup=8;kind='file write'
            else:
                target=op.imm;assert target in [0x4AB320,0x4ACDA8,0x41FEC0,0x41CE60,0x4AB3B0]
                cleanup={0x4AB320:4,0x4AB3B0:12}.get(target,0);kind=hex(target)
            calls.append(dict(address=i.address,stack_before=sp,cleanup=cleanup,target=kind));sp+=cleanup
        elif name=='ret':returns.append(dict(address=i.address,entry_relative_sp=sp));assert sp==0;continue
        if name=='jmp':todo.append((i.operands[0].imm,sp))
        elif name.startswith('j'):todo.extend([(i.operands[0].imm,sp),(nxt,sp)])
        else:todo.append((nxt,sp))
    # After original prologue+loop pushes, canonical frameSP=entrySP-144.
    # raw starts at frame+4C; rawF4 = entrySP-4, width2.
    tail_start=-4;tail_end=-2
    touching=[r for r in writes if r['entry_relative_start']<tail_end and r['entry_relative_start']+r['bytes']>tail_start]
    assert not touching and len(returns)==2
    return dict(function=0x4AA040,visited_instruction_states=len(seen),record_start_entry_sp=-0xF8,
        record_bytes=246,unassigned_tail_entry_sp=[tail_start,tail_end],direct_esp_write_or_push_hits=touching,
        writes=writes,calls=calls,returns=returns,
        scope='Direct ESP writes/PUSHes in full writer CFG. Record-array copies, resource/light/name helpers require separate instruction evidence and ordinary CString length<=32 precondition. File callback is assumed read-only on supplied buffer, as captured in native fixtures.')
if __name__=='__main__':
    import json
    value=inspect();print(json.dumps({k:v for k,v in value.items() if k not in ['writes','calls']},indent=2))
