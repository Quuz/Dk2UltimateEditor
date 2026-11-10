"""Native call-site leads for stateC1; no lifecycle meaning assigned by scanning."""
from pathlib import Path
import json,hashlib
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from capstone.x86 import X86_OP_IMM
from compare_rc3_converters import code_region
ROOT=Path(__file__).resolve().parent
data=(ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe').read_bytes();base,code=code_region(data)
md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;md.skipdata=True;rows=[];recent=[];todo=[]
for x in md.disasm(code,base):
    if not x.id:continue
    r=dict(address=hex(x.address),instruction=x.mnemonic+' '+x.op_str,bytes=x.bytes.hex())
    for row in todo:
        if x.address-int(row['address'],16)<100:row['after'].append(r)
    recent.append(r);recent=recent[-24:]
    if x.mnemonic=='push' and x.operands[0].type==X86_OP_IMM and x.operands[0].imm==193:
        row=dict(r,before=list(recent),after=[]);rows.append(row);todo.append(row)
    todo=[r for r in todo if x.address-int(r['address'],16)<100]
(ROOT/'phase91-state193-immediate-leads.json').write_text(json.dumps(dict(exe_sha256=hashlib.sha256(data).hexdigest(),leads=rows),indent=2),encoding='utf8')
for row in rows:
    print(row['address'],[(r['address'],r['instruction']) for r in row['after'][:9]])
