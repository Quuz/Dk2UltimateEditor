"""Inspect canonical PE instruction boundaries of remaining anchored candidates."""
from pathlib import Path
import json,bisect
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from capstone.x86 import X86_OP_IMM
from compare_rc3_converters import code_region
ROOT=Path(__file__).resolve().parent
def main():
    d=json.loads((ROOT/'phase70-old-object-field-candidates-v2.json').read_text(encoding='utf8'));wanted=[r for r in d['novel_address_candidates'] if not r.get('index') and r['address']!='0x42f0f1']
    data=Path(d['exe']).read_bytes();base,code=code_region(data);md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;md.skipdata=True
    ins=[x for x in md.disasm(code,base) if x.id];starts=[x.address for x in ins];targets=sorted({x.operands[0].imm for x in ins if x.mnemonic=='call' and x.operands[0].type==X86_OP_IMM})
    out=[]
    for r in wanted:
        a=int(r['address'],16);i=bisect.bisect_right(starts,a)-1;x=ins[i];context=[f'{v.address:08x}: {v.mnemonic} {v.op_str}' for v in ins[max(i-10,0):i+5]]
        row=dict(candidate=r,linear_enclosing_instruction=context[10] if i>=10 else '',canonical_address=x.address,canonical_bytes=x.bytes.hex(),canonical_text=x.mnemonic+' '+x.op_str,
          nearest_calltarget_hint=targets[bisect.bisect_right(targets,a)-1],context=context,scope='Linear disassembly boundaries are leads;Ghidra function/CFG and PE bytes still required.')
        out.append(row);print(r['address'],'canonical',hex(x.address),x.mnemonic,x.op_str,'hint',hex(row['nearest_calltarget_hint']));print('\n'.join(context))
    (ROOT/'phase100-old-object-boundary-contexts.json').write_text(json.dumps(out,indent=2),encoding='utf8')
if __name__=='__main__':main()
