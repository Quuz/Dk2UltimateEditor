"""Primary operand leads for ownerBYTE1095, not an assigned gameplay label."""
from pathlib import Path
import json,hashlib
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from capstone.x86 import X86_OP_MEM
from compare_rc3_converters import code_region
ROOT=Path(__file__).resolve().parent
data=(ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe').read_bytes();base,code=code_region(data)
md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;md.skipdata=True;rows=[];recent=[];pending=[]
for x in md.disasm(code,base):
    if not x.id:continue
    row=dict(address=hex(x.address),instruction=x.mnemonic+' '+x.op_str,bytes=x.bytes.hex())
    for r in pending:
        if x.address-int(r['address'],16)<75:r['after'].append(row)
    recent.append(row);recent=recent[-14:]
    if any(o.type==X86_OP_MEM and o.mem.base and o.mem.disp==0x1095 for o in x.operands):r=dict(row,before=list(recent),after=[]);rows.append(r);pending.append(r)
    pending=[r for r in pending if x.address-int(r['address'],16)<75]
out=dict(executable_sha256=hashlib.sha256(data).hexdigest(),leads=rows,status='Raw displacement candidates;4B2E95 owner read proven,other owners/writers still need receiver dataflow. No friendly/possession count label assigned.')
(ROOT/'phase94-owner-health-modifier-leads.json').write_text(json.dumps(out,indent=2),encoding='utf8')
print([(r['address'],r['instruction']) for r in rows])
