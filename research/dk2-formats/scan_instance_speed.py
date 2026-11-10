"""Operand leads forinstanceWORD11C andDWORD206;not receiver/type proof."""
from pathlib import Path
import json,hashlib
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from capstone.x86 import X86_OP_MEM
from compare_rc3_converters import code_region
ROOT=Path(__file__).resolve().parent

def main():
    data=(ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe').read_bytes();base,code=code_region(data)
    md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;md.skipdata=True;recent=[];pending=[];leads=[]
    for ins in md.disasm(code,base):
        if not ins.id:continue
        row=dict(address=hex(ins.address),instruction=ins.mnemonic+' '+ins.op_str,bytes=ins.bytes.hex())
        for p in pending:
            if ins.address-int(p['address'],16)<90:p['after'].append(row)
        recent.append(row);recent=recent[-15:]
        offsets=sorted(set(op.mem.disp for op in ins.operands if op.type==X86_OP_MEM and op.mem.base and op.mem.disp in [0x11c,0x206,0xe2]))
        if offsets:
            p=dict(row,offsets=offsets,before=list(recent),after=[]);leads.append(p);pending.append(p)
        pending=[p for p in pending if ins.address-int(p['address'],16)<90]
    (ROOT/'phase97-instance-speed-leads.json').write_text(json.dumps(dict(executable_sha256=hashlib.sha256(data).hexdigest(),leads=leads,
      scope='Raw displacement candidates only;many canbelongtootherreceiverlayouts. 48D000/48D010Creaturevslots provenfromoriginalvtable separately.'),indent=2),encoding='utf8')
    print('Leads',len(leads));print([(p['address'],p['instruction']) for p in leads if 0xe2 in p['offsets'] and 0x4d0000<=int(p['address'],16)<0x4e0000])

if __name__=='__main__':main()
