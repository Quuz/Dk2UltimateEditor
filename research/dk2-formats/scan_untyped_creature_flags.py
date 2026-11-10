"""New untyped overlapping686..689 operands, compared with old pointer-hint reads.

Linear executable-section decode with skipdata; operands alone do not prove a
Creature pointer. Small suffixes are leads, never global absence/meaning claims.
"""
from pathlib import Path
import json
import pefile
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from capstone.x86 import X86_OP_MEM,X86_OP_IMM
from build_effect_semantics import EXE,pe_read
ROOT=Path(__file__).resolve().parent
def main():
    d=json.loads((ROOT/'phase85-creature-index-diagnostics.json').read_text());known={x['read']['address'] for x in d['candidates'] if x['field']=='controlCreatureFlags'}
    blob=EXE.read_bytes();pe=pefile.PE(data=blob);md=Cs(CS_ARCH_X86,CS_MODE_32);md.skipdata=True;md.detail=True;out=[];count=0
    for s in pe.sections:
        if not s.Characteristics&0x20000000:continue
        for ins in md.disasm(s.get_data(),pe.OPTIONAL_HEADER.ImageBase+s.VirtualAddress):
            count+=1
            if not ins.id or '0x68' not in ins.op_str:continue
            for op in ins.operands:
                if op.type!=X86_OP_MEM or not op.mem.base:continue
                off=op.mem.disp
                if not(off<0x68A and off+op.size>0x686):continue
                rawbit=(off-0x686)*8;imm=next((o.imm for o in ins.operands if o.type==X86_OP_IMM),None)
                mask=(imm<<rawbit) if imm is not None and rawbit>=0 else None
                new=hex(ins.address) not in known
                interesting=mask is not None and mask&0x1800 and ins.mnemonic in ['test','and','or','xor']
                if new or interesting:
                    suffix=list(md.disasm(pe_read(blob,ins.address,96),ins.address))[:14]
                    out.append(dict(address=hex(ins.address),instruction=f'{ins.mnemonic} {ins.op_str}',displacement=off,bytes=op.size,index=ins.reg_name(op.mem.index),
                        normalized_immediate_mask=mask,new_operand_site=new,unknown11_12_mask=interesting,context=[f'{i.address:x}: {i.mnemonic} {i.op_str}' for i in suffix]));break
    value=dict(scope=__doc__,program_sha256=__import__('hashlib').sha256(blob).hexdigest(),decoded_instructions=count,prior_hint_read_sites=len(known),leads=out)
    (ROOT/'phase148-untyped-flag-leads.json').write_text(json.dumps(value,indent=2),encoding='utf8')
    print('decoded',count,'new/masked sites',len(out))
    for r in out[:35]:print(r['address'],r['instruction'],'unknownmask',r['unknown11_12_mask'])
if __name__=='__main__':main()
