"""New overlap/address-exposure inventory for the placed-creature radius.

Includes wide reads crossing the byte, LEA and selected offset immediates,
unlike phase47's exact-displacement inventory. Candidates remain untyped.
Also inventories DWORD writes to StateUtils+AF to trace its separate radius.
"""
from pathlib import Path
import json,hashlib,re,bisect
from capstone import Cs,CS_ARCH_X86,CS_MODE_32,CS_AC_WRITE
from capstone.x86 import X86_OP_MEM,X86_OP_IMM
from compare_rc3_converters import code_region
from native_memory_access import reads_memory
ROOT=Path(__file__).resolve().parent
mapping=(ROOT/'sources/DKII_EXE_v170.sgmap').read_text(encoding='utf8')
funcs=sorted((int(a,16),name,int(n)) for a,name,n in re.findall(r'^global: va=([0-9A-F]+),name=([^,\n]+),size=(\d+)',mapping,re.M));starts=[a for a,_,_ in funcs]
rows=[]
for label,exe,target in [('DX',ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe',0x418),('old',ROOT/'source-binaries/DKII-local13.exe',0x417)]:
    data=exe.read_bytes();base,code=code_region(data);md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;md.skipdata=True;exposure=[];writers=[];count=0
    for x in md.disasm(code,base):
        if not x.id:continue
        count+=1
        for o in x.operands:
            if o.type==X86_OP_MEM:
                hit=x.mnemonic!='lea' and o.mem.disp<=target<o.mem.disp+o.size
                relevant=o.mem.base and (hit or (x.mnemonic=='lea' and o.mem.disp==target) or (o.mem.disp==0xAF and o.size==4 and o.access&CS_AC_WRITE))
            else:relevant=o.type==X86_OP_IMM and o.imm==target and x.mnemonic in ['add','sub','mov','push']
            if not relevant:continue
            common=dict(address=x.address,instruction=x.mnemonic+' '+x.op_str)
            if label=='DX':
                i=bisect.bisect_right(starts,x.address)-1
                if i>=0 and funcs[i][0]<=x.address<funcs[i][0]+funcs[i][2]:common['mapped_hint']=dict(address=funcs[i][0],name=funcs[i][1],bytes=funcs[i][2])
            if o.type==X86_OP_MEM and o.mem.base:
                hit=x.mnemonic!='lea' and o.mem.disp<=target<o.mem.disp+o.size
                if (x.mnemonic=='lea' and o.mem.disp==target) or hit:
                    exposure.append(dict(common,kind='memory',displacement=o.mem.disp,bytes=o.size,index=x.reg_name(o.mem.index),base=x.reg_name(o.mem.base),read=reads_memory(x,o),write=bool(o.access&CS_AC_WRITE)))
                if o.mem.disp==0xAF and o.size==4 and o.access&CS_AC_WRITE:writers.append(dict(common,base=x.reg_name(o.mem.base),index=x.reg_name(o.mem.index)))
            elif o.type==X86_OP_IMM and o.imm==target and x.mnemonic in ['add','sub','mov','push']:
                exposure.append(dict(common,kind='immediate'))
    out=dict(profile=label,exe=str(exe),sha256=hashlib.sha256(data).hexdigest(),target=target,decoded_instructions=count,candidates=exposure,context_AF_writers=writers)
    rows.append(out);print(label,'exposure',len(exposure),'DWORD_AF_writers',len(writers))
    for r in exposure:
        if r.get('read') or r['kind']=='immediate' or r['instruction'].startswith('lea '):print(hex(r['address']),r['instruction'],r.get('mapped_hint'))
    if label=='DX':
        for r in writers:print('AFwriter',hex(r['address']),r['instruction'],r.get('mapped_hint'))
(ROOT/'phase75-wander-exposure.json').write_text(json.dumps(dict(scope=__doc__,profiles=rows),indent=2),encoding='utf8')
