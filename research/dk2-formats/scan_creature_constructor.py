"""Locate real CCreature vtable stores in the provided PE; hints need review."""
from pathlib import Path
import json,bisect
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from capstone.x86 import X86_OP_MEM,X86_OP_IMM
from compare_rc3_converters import code_region
ROOT=Path(__file__).resolve().parent
data=(ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe').read_bytes();base,code=code_region(data)
md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;md.skipdata=True
rows=[];starts=[];insns=[]
for x in md.disasm(code,base):
    if not x.id:continue
    row=dict(address=x.address,instruction=x.mnemonic+' '+x.op_str);insns.append(row)
    if x.mnemonic=='call' and x.operands[0].type==X86_OP_IMM:starts.append(x.operands[0].imm)
    if any(o.type==X86_OP_IMM and o.imm==0x4b5e10 for o in x.operands):rows.append(dict(row))
    if x.mnemonic=='mov' and len(x.operands)>1 and x.operands[0].type==X86_OP_MEM and x.operands[1].type==X86_OP_IMM and x.operands[1].imm==0x66d524:rows.append(dict(row))
starts=sorted(set(starts))
for row in rows:
    row['preceding_call_target_hint']=starts[bisect.bisect_right(starts,row['address'])-1]
    row['context']=[r for r in insns if row['address']-140<=r['address']<=row['address']+75]
(ROOT/'phase90-creature-constructor-leads.json').write_text(json.dumps(rows,indent=2),encoding='utf8')
for row in rows:
    print(hex(row['address']),row['instruction'],'hint',hex(row['preceding_call_target_hint']))
    if not row['instruction'].startswith('mov dword ptr [esi]'):print('\n'.join(hex(r['address'])+' '+r['instruction'] for r in row['context']))
print('\n'.join(hex(r['address'])+' '+r['instruction'] for r in insns if 0x4b5dd0<=r['address']<0x4b5e70))
