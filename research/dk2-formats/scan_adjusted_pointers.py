"""Short, straight-line adjusted-pointer candidates; no structure identity proof.

Seeds LEA/ADD/SUB constant pointer adjustments in mapped native function spans.
Tracks register aliases for up to40 instructions on the fall-through path. Calls
kill EAX/ECX/EDX; indirect accesses, merges and stack reloads are not resolved.
Every candidate still needs primary disassembly and object identity verification.
"""
from pathlib import Path
import hashlib,json,re,struct
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from capstone.x86 import X86_OP_MEM,X86_OP_REG,X86_OP_IMM
from build_effect_semantics import pe_read

ROOT=Path(__file__).resolve().parent
TARGETS=[('Terrain198',0x183,2),('Terrain224',0x1B5,4),('Traps204',0x98,2),('Objects34c',0x106,2)]
REGS={'eax','ebx','ecx','edx','esi','edi','ebp','esp'}

def main():
    exe=ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe';data=exe.read_bytes()
    mapping=(ROOT/'sources/DKII_EXE_v170.sgmap').read_text(encoding='utf8')
    funcs=re.findall(r'^global: va=([0-9A-F]+),name=([^,\n]+),size=(\d+)',mapping,re.M)
    md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;md.skipdata=True
    results=[];seen=set();scanned=0;seeds=0
    def reg(ins,op):return ins.reg_name(op.reg)
    for address,name,size in funcs:
        address=int(address,16);size=int(size)
        if not 0x401000<=address<0x653000 or size>100000:continue
        try:code=pe_read(data,address,size)
        except (ValueError,AssertionError):continue
        insns=list(md.disasm(code,address));scanned+=1
        for i,seed in enumerate(insns):
            if not seed.id:continue
            ops=seed.operands;adjustment=None
            if seed.mnemonic=='lea' and len(ops)==2 and ops[0].type==X86_OP_REG and ops[1].type==X86_OP_MEM:
                mem=ops[1].mem
                if mem.base and not mem.index and 0<mem.disp<0x200:adjustment=mem.disp
            elif seed.mnemonic in ['add','sub'] and len(ops)==2 and ops[0].type==X86_OP_REG and ops[1].type==X86_OP_IMM:
                adjustment=ops[1].imm*(1 if seed.mnemonic=='add' else -1)
                if not 0<adjustment<0x200:adjustment=None
            if adjustment is None or reg(seed,ops[0]) not in REGS-{'esp','ebp'}:continue
            seeds+=1;aliases={reg(seed,ops[0]):adjustment};trace=[f'{seed.address:08x}: {seed.mnemonic} {seed.op_str}']
            for ins in insns[i+1:i+41]:
                if not ins.id:break
                ops=ins.operands
                for op in ops:
                    if op.type!=X86_OP_MEM or not op.mem.base or op.mem.index:continue
                    base=ins.reg_name(op.mem.base)
                    if base not in aliases:continue
                    effective=aliases[base]+op.mem.disp
                    if ins.mnemonic in ['lea','call','jmp'] or not op.access&1:continue
                    matches=[n for n,o,w in TARGETS if effective<o+w and effective+op.size>o]
                    key=(seed.address,ins.address,effective)
                    if matches and key not in seen:
                        seen.add(key);results.append(dict(function=hex(address),name=name,seed=hex(seed.address),
                            instruction=hex(ins.address),text=ins.mnemonic+' '+ins.op_str,effective_offset=hex(effective),
                            adjusted_register=base,adjustment=aliases[base],memory_bytes=op.size,targets=matches,
                            trace=trace+[f'{ins.address:08x}: {ins.mnemonic} {ins.op_str}']))
                new=None;dest=None
                if ops and ops[0].type==X86_OP_REG:
                    dest=reg(ins,ops[0])
                    if ins.mnemonic=='mov' and len(ops)==2 and ops[1].type==X86_OP_REG:new=aliases.get(reg(ins,ops[1]))
                    elif ins.mnemonic=='lea' and len(ops)==2 and ops[1].type==X86_OP_MEM:
                        mem=ops[1].mem;base=ins.reg_name(mem.base)
                        if not mem.index and base in aliases:new=aliases[base]+mem.disp
                    elif ins.mnemonic in ['add','sub'] and len(ops)==2 and ops[1].type==X86_OP_IMM and dest in aliases:
                        new=aliases[dest]+ops[1].imm*(1 if ins.mnemonic=='add' else -1)
                try:_,written=ins.regs_access()
                except Exception:written=[]
                for written_reg in written:
                    written_name=ins.reg_name(written_reg)
                    # Partial writes also invalidate tracked full-register pointers.
                    group={'ax':'eax','al':'eax','ah':'eax','bx':'ebx','bl':'ebx','bh':'ebx',
                        'cx':'ecx','cl':'ecx','ch':'ecx','dx':'edx','dl':'edx','dh':'edx','si':'esi','di':'edi','bp':'ebp','sp':'esp'}
                    aliases.pop(group.get(written_name,written_name),None)
                if new is not None:aliases[dest]=new
                if ins.mnemonic=='call':
                    for r in ['eax','ecx','edx']:aliases.pop(r,None)
                trace.append(f'{ins.address:08x}: {ins.mnemonic} {ins.op_str}')
                if not aliases or ins.mnemonic in ['ret','retf','jmp','int3']:break
    output=dict(program_sha256=hashlib.sha256(data).hexdigest(),scope=__doc__,mapped_spans_scanned=scanned,seeds=seeds,candidates=results)
    (ROOT/'phase23-adjusted-pointer-candidates.json').write_text(json.dumps(output,indent=2),encoding='utf8')
    print('Mapped spans:',scanned,'; seeds:',seeds,'; candidates:',len(results))
    for row in results:print(row['targets'],row['function'],row['name'],row['instruction'],row['text'],'effective',row['effective_offset'])

if __name__=='__main__':main()
