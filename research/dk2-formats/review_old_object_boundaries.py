"""Verify anchored candidate boundaries; bounded original-this CFG origin proof."""
from pathlib import Path
from collections import deque
import json,re,hashlib
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from capstone.x86 import X86_OP_REG,X86_OP_MEM,X86_OP_IMM
from build_effect_semantics import pe_read
from scan_interprocedural_creature_fields import GROUP
ROOT=Path(__file__).resolve().parent

def main():
    source=json.loads((ROOT/'phase100-old-object-boundary-contexts.json').read_text(encoding='utf8'));exe=ROOT/'source-binaries/DKII-local13.exe';data=exe.read_bytes();md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True
    out=[];recovery=None
    for row in source:
        a=int(row['candidate']['address'],16);f=row['nearest_calltarget_hint'];assembly=f'phase100-old-context-{f:x}-disassemble_function.txt'
        lines=(ROOT/'ghidra'/assembly).read_text(encoding='utf8').splitlines();addresses=[int(x[:8],16) for x in lines if re.match('^[0-9a-fA-F]{8}:',x)]
        assert addresses[0]==f
        if f==0x4979b0:
            # Ghidra omitted switch arms. The native CMP EAX,4/JA at497A15
            # bounds the one indexed jump; recover all five original targets.
            import struct
            targets=list(struct.unpack('<5I',pe_read(data,0x497e58,20)))
            queue=deque([f,*targets]);decoded={};stops=[]
            while queue:
                pc=queue.popleft()
                if pc in decoded:continue
                assert f<=pc<=0x497e54,(hex(pc),targets)
                x=next(md.disasm(pe_read(data,pc,15),pc));decoded[pc]=x
                if x.mnemonic.startswith('ret'):continue
                if x.mnemonic.startswith('j'):
                    if x.operands[0].type==X86_OP_IMM:queue.append(x.operands[0].imm)
                    else:
                        assert pc==0x497a1e
                        queue.extend(targets)
                    if x.mnemonic!='jmp':queue.append(pc+x.size)
                else:queue.append(pc+x.size)
            added=sorted(set(decoded)-set(addresses));addresses=sorted(decoded)
            recovery=dict(function=f,table=0x497e58,targets=targets,bound='CMP EAX,4;JA497E24 beforeJMP[EAX*4+497E58]',
              recovered_instructions=[dict(address=a,text=decoded[a].mnemonic+' '+decoded[a].op_str,hex=decoded[a].bytes.hex()) for a in added],
              unresolved_stops=stops,scope='All five switch arms recovered from PE;full extent includes recovered code rather than metadata omission.')
        canonical=next(md.disasm(pe_read(data,row['canonical_address'],15),row['canonical_address']));assert canonical.bytes.hex()==row['canonical_bytes'] and canonical.address in addresses
        assert canonical.address<=a<canonical.address+canonical.size and a not in addresses
        kind='prefixed-WORD104-excludes106' if canonical.mnemonic=='mov' and canonical.operands[0].size==2 else 'starts-in-JMP-displacement' if canonical.mnemonic=='jmp' else 'starts-in-immediate-zero'
        if kind=='prefixed-WORD104-excludes106':assert canonical.bytes[0]==0x66 and canonical.operands[1].mem.disp==0x104 and canonical.operands[1].size==2
        if kind=='starts-in-immediate-zero':assert a==0x5642b3 and canonical.mnemonic=='mov' and canonical.operands[0].type==X86_OP_MEM and canonical.operands[1].type==X86_OP_IMM and canonical.operands[1].imm==0
        out.append(dict(candidate=row['candidate'],function=f,assembly=assembly,canonical_address=canonical.address,canonical_bytes=canonical.bytes.hex(),canonical_text=canonical.mnemonic+' '+canonical.op_str,classification=kind))
    f=0x4a8c00;addresses=[int(x[:8],16) for x in (ROOT/'ghidra'/f'phase100-old-context-{f:x}-disassemble_function.txt').read_text(encoding='utf8').splitlines() if re.match('^[0-9a-fA-F]{8}:',x)]
    instructions={a:next(md.disasm(pe_read(data,a,15),a)) for a in addresses};todo=deque([(f,{'ecx':0})]);seen=set();reads=[];stops=[]
    while todo:
        pc,aliases=todo.popleft();key=(pc,tuple(sorted(aliases.items())))
        if key in seen:continue
        assert len(seen)<100000,'Boundedproofstatecap';seen.add(key)
        if pc not in instructions:stops.append(dict(pc=hex(pc),reason='outside Ghidra functionbody'));continue
        x=instructions[pc];ops=x.operands;nxt=pc+x.size
        if pc==0x4a9005:reads.append(dict(address=pc,aliases=dict(aliases),text=x.mnemonic+' '+x.op_str))
        def reg(o):return x.reg_name(o.reg)
        new=None;dest=None
        if ops and ops[0].type==X86_OP_REG:
            dest=GROUP.get(reg(ops[0]),reg(ops[0]))
            if x.mnemonic=='mov' and len(ops)==2 and ops[0].size==ops[1].size==4 and ops[1].type==X86_OP_REG:new=aliases.get(reg(ops[1]))
            elif x.mnemonic=='lea' and len(ops)==2 and ops[1].type==X86_OP_MEM and not ops[1].mem.index:
                b=x.reg_name(ops[1].mem.base)
                if b in aliases:new=aliases[b]+ops[1].mem.disp
            elif x.mnemonic in ['add','sub'] and len(ops)==2 and ops[1].type==X86_OP_IMM and dest in aliases:new=aliases[dest]+ops[1].imm*(1 if x.mnemonic=='add' else -1)
        try:_,written=x.regs_access()
        except Exception:written=[]
        for r in written:
            name=x.reg_name(r);aliases.pop(GROUP.get(name,name),None)
        if new is not None and dest and dest!='esp':aliases[dest]=new
        if x.mnemonic=='call':
            for r in ['eax','ecx','edx']:aliases.pop(r,None)
        if x.mnemonic.startswith('ret'):continue
        if x.mnemonic.startswith('j'):
            if not ops or ops[0].type!=X86_OP_IMM:stops.append(dict(pc=hex(pc),reason='indirect jump'));continue
            targets=[ops[0].imm] if x.mnemonic=='jmp' else [ops[0].imm,nxt]
        else:targets=[nxt]
        for target in targets:todo.append((target,dict(aliases)))
    # The original simple walker deliberately lacks stack reloads and therefore
    # cannot prove this at4A9005. Prove the actual local spill/restore explicitly.
    assert reads
    assert not stops,stops
    def reachable(start,exclude=None):
        todo=[start];seen=set()
        while todo:
            a=todo.pop()
            if a==exclude or a in seen:continue
            seen.add(a);x=instructions[a]
            if x.mnemonic.startswith('ret'):continue
            if x.mnemonic.startswith('j'):
                assert x.operands[0].type==X86_OP_IMM;todo.append(x.operands[0].imm)
                if x.mnemonic!='jmp':todo.append(a+x.size)
            else:todo.append(a+x.size)
        return seen
    assert 0x4a9005 not in reachable(f,0x4a8f51)
    # There are only two local incoming conditional branches to the read;
    # neither path writes ESI after its saved-this reload.
    incoming=[a for a,x in instructions.items() if x.mnemonic.startswith('j') and x.operands[0].type==X86_OP_IMM and x.operands[0].imm==0x4a9005]
    assert incoming==[0x4a8fb6,0x4a8fdd]
    assert instructions[0x4a8c1b].mnemonic+' '+instructions[0x4a8c1b].op_str=='mov esi, ecx'
    assert instructions[0x4a8c22].op_str=='dword ptr [esp + 0x10], esi'
    assert instructions[0x4a8f51].op_str=='esi, dword ptr [esp + 0x10]'
    for a in range(0x4a8f51+instructions[0x4a8f51].size,0x4a8fdd+instructions[0x4a8fdd].size):
        if a not in instructions:continue
        _,writes=instructions[a].regs_access();assert 'esi' not in [GROUP.get(instructions[a].reg_name(r),instructions[a].reg_name(r)) for r in writes]
    pcode=json.loads((ROOT/'ghidra/phase100-old107-backward.txt').read_text(encoding='utf8'))
    assert pcode['terminated']=='reached function input' and pcode['anchor']['variable']=='ECX'
    proof=dict(function=f,read=0x4a9005,cfg_states=len(seen),unresolved_stops=stops,original_walker_could_not_resolve_stack=reads,
      saved_this_store=0x4a8c22,restore=0x4a8f51,restore_dominates_read=True,incoming_conditional_branches=incoming,
      pcode_evidence='phase100-old107-backward.txt',
      result='RealDWORD107 belongs to the original ECX/this receiver: storeat4A8C22,reloadat4A8F51 dominatesread,noESIwrite before eitherincomingbranch. Decompilerparam_1 maps toECX input. FixedframeESP/ABI and unaliasedlocalspill are explicit assumptions,not recursivecallee proof.',
      limit='No globalnonuse or allotherunaligned/indirect reads excluded. Original receiver member is not a new typedObjectdefinition consumer;no new gameplay name for rawObjects34C.')
    result=dict(executable=str(exe),sha256=hashlib.sha256(data).hexdigest(),reviews=out,switch_recovery=recovery,neighbouring_actual_read=proof,source='phase70 novel_address_candidates; no new broad operand scan,emulation or rawpatch')
    (ROOT/'phase100-object-boundary-review.json').write_text(json.dumps(result,indent=2),encoding='utf8');print('Reviewed',len(out),'anchored candidates;',len(seen),'CFG states;',len(reads),'actual read states preserve this')
if __name__=='__main__':main()
