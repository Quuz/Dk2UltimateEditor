"""Bounded bit-origin extraction leads after prior Creature686 pointer hints.

Straight-line register/partial-register move, logical mask and shift handling.
Stops at first branch/call; not a global nonuse or pointer identity proof.
"""
from pathlib import Path
import json
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from capstone.x86 import X86_OP_REG,X86_OP_MEM,X86_OP_IMM
from build_effect_semantics import EXE,pe_read
ROOT=Path(__file__).resolve().parent
TARGET={11,12,29}
def main():
    d=json.loads((ROOT/'phase85-creature-index-diagnostics.json').read_text());rows={x['read']['address']:x for x in d['candidates'] if x['field']=='controlCreatureFlags'}
    md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;blob=EXE.read_bytes();aliases={}
    for name in ['eax','ebx','ecx','edx','esi','edi','ebp','esp']:
        aliases[name]=(name,0,32);aliases[name[1:]]=(name,0,16)
        if name[1:] in ['ax','bx','cx','dx']:
            aliases[name[1]+'l']=(name,0,8);aliases[name[1]+'h']=(name,8,8)
    out=[]
    for pc,row in rows.items():
        ins=list(md.disasm(pe_read(blob,int(pc,16),192),int(pc,16)))[:40]
        if not ins or ins[0].mnemonic not in ['mov','movzx','movsx']:continue
        first=ins[0];o=first.operands
        if len(o)!=2 or o[0].type!=X86_OP_REG or o[1].type!=X86_OP_MEM:continue
        regs={};history=[];hits=[]
        def read(reg):
            name=md.reg_name(reg)
            if name not in aliases:return []
            parent,start,n=aliases[name];return regs.get(parent,[None]*32)[start:start+n]
        def write(reg,bits):
            name=md.reg_name(reg)
            if name not in aliases:return
            parent,start,n=aliases[name];v=regs.setdefault(parent,[None]*32);v[start:start+n]=(bits+[None]*n)[:n]
        start=(row['read']['offset']-0x686)*8
        bits=list(range(start,start+8*o[1].size))
        if first.mnemonic=='movsx':bits+=bits[-1:]*(32-len(bits))
        write(o[0].reg,bits)
        for i in ins[1:]:
            text=f'{i.address:x}: {i.mnemonic} {i.op_str}';history.append(text);ops=i.operands
            if i.mnemonic in ['call','ret','jmp'] or i.mnemonic.startswith('j'):break
            if not ops:continue
            dest=ops[0]
            if i.mnemonic in ['mov','movzx','movsx'] and dest.type==X86_OP_REG:
                bits=read(ops[1].reg) if ops[1].type==X86_OP_REG else []
                if i.mnemonic=='movsx' and bits:bits+=bits[-1:]*(32-len(bits))
                write(dest.reg,bits)
            elif i.mnemonic in ['shr','sar','shl','sal','and','or','xor'] and dest.type==X86_OP_REG and len(ops)==2:
                bits=read(dest.reg)
                if ops[1].type==X86_OP_IMM:
                    value=ops[1].imm
                    if i.mnemonic in ['shr','sar']:bits=(bits[value:]+([bits[-1]]*value if i.mnemonic=='sar' else [None]*value))[:len(bits)]
                    elif i.mnemonic in ['shl','sal']:bits=([None]*value+bits)[:len(bits)]
                    elif i.mnemonic=='and':bits=[b if value&(1<<j) else None for j,b in enumerate(bits)]
                    elif i.mnemonic=='or':bits=[None if value&(1<<j) else b for j,b in enumerate(bits)]
                    write(dest.reg,bits)
                elif i.mnemonic=='xor' and ops[1].type==X86_OP_REG and ops[1].reg==dest.reg:write(dest.reg,[])
                else:write(dest.reg,[])
                live={b for b in read(dest.reg) if b is not None}
                if live and live<=TARGET:hits.append(dict(address=hex(i.address),instruction=text,source_bits=sorted(live),expression_register=md.reg_name(dest.reg)))
            elif i.mnemonic=='test' and dest.type==X86_OP_REG and len(ops)==2 and ops[1].type==X86_OP_IMM:
                live={b for j,b in enumerate(read(dest.reg)) if b is not None and ops[1].imm&(1<<j)}
                if live&TARGET:hits.append(dict(address=hex(i.address),instruction=text,source_bits=sorted(live)))
            elif dest.type==X86_OP_REG and i.mnemonic not in ['cmp','test','push']:write(dest.reg,[])
        if hits:out.append(dict(read=pc,root=row['root'],read_instruction=row['read']['instruction'],hits=hits,context=history))
    value=dict(scope=__doc__,prior_unique_flag_reads=len(rows),leads=out)
    (ROOT/'phase148-flag-extract-leads.json').write_text(json.dumps(value,indent=2),encoding='utf8')
    for r in out:print(r['read'],[(h['address'],h['source_bits']) for h in r['hits']])
if __name__=='__main__':main()
