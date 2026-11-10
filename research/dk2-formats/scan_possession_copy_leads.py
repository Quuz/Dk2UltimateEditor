"""Widen pointer/immediate inventory near loaded possession globals.

This retains candidates only. No global usage or absence claim follows from a
direct scan: indexed reads and copied structures still need dataflow tracing.
"""
from pathlib import Path
import json,hashlib
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from capstone.x86 import X86_OP_IMM,X86_OP_MEM
from compare_rc3_converters import code_region
ROOT=Path(__file__).resolve().parent
data=(ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe').read_bytes();base,code=code_region(data)
md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;md.skipdata=True;rows=[];recent=[];pending=[]
for x in md.disasm(code,base):
    if not x.id:continue
    row=dict(address=hex(x.address),bytes=x.bytes.hex(),instruction=x.mnemonic+' '+x.op_str)
    for r in pending:
        if x.address-int(r['address'],16)<90:r['after'].append(row)
    recent.append(row);recent=recent[-14:]
    matches=[]
    for o in x.operands:
        if o.type==X86_OP_IMM and 0x6f28c0<=o.imm<0x6f2910:matches.append('Immediate pointer/value near scalar storage')
        if o.type==X86_OP_MEM and 0x6f28c0<=o.mem.disp<0x6f2910:matches.append('Absolute/indexed displacement near scalar storage')
    if matches:r=dict(row,kinds=matches,before=list(recent),after=[]);rows.append(r);pending.append(r)
    pending=[r for r in pending if x.address-int(r['address'],16)<90]
out=dict(executable_sha256=hashlib.sha256(data).hexdigest(),leads=rows,status='Candidates only; not gameplay proof; preserve earlier direct-operand negative limits')
(ROOT/'phase92-possession-copy-leads.json').write_text(json.dumps(out,indent=2),encoding='utf8')
print(len(rows),'near-storage pointer/operand leads saved for dataflow review')
