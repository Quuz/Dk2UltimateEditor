"""Primary PE operand inventory around the established World recipe array."""
from pathlib import Path
import json,hashlib,bisect
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from capstone.x86 import X86_OP_MEM,X86_OP_IMM
from compare_rc3_converters import code_region
ROOT=Path(__file__).resolve().parent

def main():
    path=Path('D:/Games/DK2/Dungeon Keeper 2/DKII-DX.exe')
    data=path.read_bytes();base,code=code_region(data)
    md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;md.skipdata=True
    leads=[];neighborhood=[];calls=[];starts=set();all_ins=[]
    for x in md.disasm(code,base):
        if not x.id:continue
        row=dict(address=hex(x.address),bytes=x.bytes.hex(),instruction=x.mnemonic+' '+x.op_str)
        all_ins.append((x.address,row))
        if x.mnemonic=='call' and x.operands[0].type==X86_OP_IMM:
            starts.add(x.operands[0].imm)
        for o in x.operands:
            if o.type==X86_OP_MEM and (0x2e70<=o.mem.disp<0x2e92 or o.mem.disp==0x731b):
                leads.append(dict(row,base=x.reg_name(o.mem.base),index=x.reg_name(o.mem.index),displacement=hex(o.mem.disp)))
        if 0x50f7e0<=x.address<0x50fb70:neighborhood.append(row)
        if x.mnemonic=='call' and x.operands[0].type==X86_OP_IMM and 0x50f7e0<=x.operands[0].imm<0x50fb70:
            calls.append(dict(row,target=hex(x.operands[0].imm)))
    starts=sorted(starts)
    for lead in leads:
        pc=int(lead['address'],16);i=bisect.bisect_right(starts,pc)-1
        if i>=0:lead['nearest_preceding_call_target_hint']=hex(starts[i])
    report=dict(exe=str(path),sha256=hashlib.sha256(data).hexdigest(),leads=leads,calls=calls,
                warning='Displacement matches alone do not prove World or recipe identity.')
    (ROOT/'phase88-sacrifice-leads.json').write_text(json.dumps(report,indent=2),encoding='utf8')
    (ROOT/'phase88-sacrifice-neighborhood.txt').write_text('\n'.join(r['address']+': '+r['instruction'] for r in neighborhood),encoding='utf8')
    print(json.dumps(report,indent=2))
    for center in [0x472a28,0x49f6b6,0x513406]:
        print('\nContext',hex(center))
        print('\n'.join(r['address']+': '+r['instruction'] for pc,r in all_ins if center-240<=pc<center+130))

if __name__=='__main__':main()
