"""Find address-taking/indexed operations omitted by read-only field searches.

All exact displacement matches are untyped leads. Includes writes and LEA,
which do not themselves establish semantic reads;no global non-use proof.
"""
from pathlib import Path
import json,hashlib
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from capstone.x86 import X86_OP_MEM,X86_OP_IMM
from compare_rc3_converters import code_region
ROOT=Path(__file__).resolve().parent
TARGETS={0x6EB:'PickUpPriority',0x75C:'AbilityJump'}
profiles=[('DX',ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe'),('old',ROOT/'source-binaries/DKII-local13.exe')]
allrows=[]
for name,exe in profiles:
    data=exe.read_bytes();base,code=code_region(data);md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;md.skipdata=True;rows=[]
    for x in md.disasm(code,base):
        if not x.id:continue
        for o in x.operands:
            if o.type==X86_OP_MEM and o.mem.disp in TARGETS and o.mem.base:
                rows.append(dict(address=x.address,field=TARGETS[o.mem.disp],kind='memory',instruction=x.mnemonic+' '+x.op_str,bytes=o.size,access=o.access,index=x.reg_name(o.mem.index),base=x.reg_name(o.mem.base)))
            elif o.type==X86_OP_IMM and o.imm in TARGETS and x.mnemonic in ['add','sub','mov','push']:
                rows.append(dict(address=x.address,field=TARGETS[o.imm],kind='immediate',instruction=x.mnemonic+' '+x.op_str))
    print(name,'candidates',len(rows))
    for r in rows:
        if r['kind']=='immediate' or r.get('index') or r['instruction'].startswith('lea'):print(hex(r['address']),r['instruction'],r['field'])
    allrows.append(dict(profile=name,exe=str(exe),sha256=hashlib.sha256(data).hexdigest(),candidates=rows))
(ROOT/'phase73-creature-pointer-exposure.json').write_text(json.dumps(dict(scope=__doc__,profiles=allrows),indent=2),encoding='utf8')
