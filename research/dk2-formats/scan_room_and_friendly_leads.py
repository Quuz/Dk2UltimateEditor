"""Displacement/mask leads only; receiver types and bit provenance unproved."""
from pathlib import Path
import json,hashlib
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from capstone.x86 import X86_OP_MEM,X86_OP_IMM
from compare_rc3_converters import code_region
ROOT=Path(__file__).resolve().parent

def main():
    data=(ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe').read_bytes();base,code=code_region(data)
    md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;md.skipdata=True;recent=[];pending=[];counters=[];flags=[]
    for ins in md.disasm(code,base):
        if not ins.id:continue
        row=dict(address=hex(ins.address),instruction=ins.mnemonic+' '+ins.op_str,bytes=ins.bytes.hex(),
          bit8_mask_candidate=ins.mnemonic in ['or','and','xor','bts','btr'] and any(op.type==X86_OP_IMM and (op.imm&0xffffffff) in [8,0xf7,0xfffffff7,3] for op in ins.operands))
        for p in pending:
            if ins.address-int(p['address'],16)<90:p['after'].append(row)
        recent.append(row);recent=recent[-18:]
        slots=sorted(set(op.mem.disp for op in ins.operands if op.type==X86_OP_MEM and op.mem.base))
        if any(0x108a<=slot<=0x109b for slot in slots):
            p=dict(row,displacements=slots,before=list(recent),after=[]);counters.append(p);pending.append(p)
        if 0x3ea in slots:
            p=dict(row,displacements=slots,before=list(recent),after=[]);flags.append(p);pending.append(p)
        pending=[p for p in pending if ins.address-int(p['address'],16)<90]
    out=dict(executable_sha256=hashlib.sha256(data).hexdigest(),counter_leads=counters,flag3ea_leads=flags,
      status='Raw operand candidates only. Counter offsets can belong to other receiver types; nearby bit masks can modify unrelated registers/objects. No new effect label or writer proof.')
    (ROOT/'phase95-room-friendly-leads.json').write_text(json.dumps(out,indent=2),encoding='utf8')
    selected=[p for p in flags if any(r['bit8_mask_candidate'] for r in p['before']+p['after'])]
    print('Counter leads',len(counters),'flag leads',len(flags),'near-mask leads',len(selected))
    print([(p['address'],p['instruction']) for p in selected])

if __name__=='__main__':main()
