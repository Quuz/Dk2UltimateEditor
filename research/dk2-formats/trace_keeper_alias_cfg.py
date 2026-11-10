"""Bounded intraprocedural alias leads from candidate Keeper definition getters.

Explores direct conditional branches and MOV/LEA/constant-add aliases, stack
spills between calls. Unknown calls kill volatile registers and stack aliases.
Virtual110 is a candidate seed, NOT a proof of Keeper definition identity.
Limits, missing branch targets and unknown indirect jumps are reported.
"""
from pathlib import Path
from collections import deque
import json,re
from capstone import Cs,CS_ARCH_X86,CS_MODE_32,CS_AC_READ,CS_AC_WRITE
from capstone.x86 import X86_OP_MEM,X86_OP_REG,X86_OP_IMM
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent
GROUP={'ax':'eax','al':'eax','ah':'eax','bx':'ebx','bl':'ebx','bh':'ebx','cx':'ecx','cl':'ecx','ch':'ecx','dx':'edx','dl':'edx','dh':'edx','si':'esi','di':'edi','bp':'ebp','sp':'esp'}
def main():
    data=(ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe').read_bytes();md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True
    funcs={int(a,16):(name,int(size)) for a,name,size in re.findall(r'^global: va=([0-9A-F]+),name=([^,\n]+),size=(\d+)',(ROOT/'sources/DKII_EXE_v170.sgmap').read_text(),re.M)}
    reports=[];reads=[];forward=[]
    seeds=json.loads((ROOT/'phase45-keeper-getter-alias-leads.json').read_text())['rows']
    for seed in seeds:
        start=int(seed['function'],16);name,size=funcs[start];ins={i.address:i for i in md.disasm(pe_read(data,start,size),start)};source=int(seed['call'],16);first=ins[source].address+ins[source].size
        todo=deque([(first,{'eax':0},{},0,[])]);seen=set();stops=[];states=0
        while todo and states<4000:
            a,aliases,stack,sp,path=todo.popleft();state=(a,tuple(sorted(aliases.items())),tuple(sorted(stack.items())),sp)
            if state in seen:continue
            seen.add(state);states+=1
            if a not in ins:stops.append(dict(address=hex(a),reason='Outside mapped instruction span'));continue
            x=ins[a];op=x.operands;nxt=a+x.size;path=(path+[f'{a:08x}: {x.mnemonic} {x.op_str}'])[-100:]
            def reg(o):return x.reg_name(o.reg)
            def full(o):return GROUP.get(reg(o),reg(o))
            def value(o):
                if o.type==X86_OP_REG and o.size==4:return aliases.get(reg(o))
                if o.type==X86_OP_MEM and x.reg_name(o.mem.base)=='esp' and not o.mem.index and o.size==4:return stack.get(sp+o.mem.disp)
                return None
            for o in op:
                if o.type==X86_OP_MEM and o.access&CS_AC_READ and x.reg_name(o.mem.base) in aliases and not o.mem.index and x.mnemonic not in ['lea','call','jmp']:
                    offset=aliases[x.reg_name(o.mem.base)]+o.mem.disp
                    if offset<0x3E and offset+o.size>0x3A:reads.append(dict(function=seed['function'],name=name,seed=seed['call'],access=hex(a),offset=offset,bytes=o.size,path=path))
            if x.mnemonic.startswith('ret'):continue
            if x.mnemonic=='call':
                pointers={f'arg+{off}':stack[sp+off] for off in range(0,32,4) if sp+off in stack}
                if 'ecx' in aliases:pointers['ecx']=aliases['ecx']
                if pointers:forward.append(dict(function=seed['function'],name=name,seed=seed['call'],call=hex(a),target=x.op_str,pointers=pointers,path=path))
                identity=aliases.get('ecx') if x.op_str=='0x40d640' else None
                for r in ['eax','ecx','edx']:aliases.pop(r,None)
                if identity is not None:aliases['eax']=identity
                stack={};sp=0
            elif x.mnemonic=='push':
                v=value(op[0]);sp-=4
                if v is not None:stack[sp]=v
                else:stack.pop(sp,None)
            elif x.mnemonic=='pop':
                v=stack.pop(sp,None);sp+=4;aliases.pop(full(op[0]),None)
                if v is not None and op[0].size==4:aliases[reg(op[0])]=v
            elif x.mnemonic in ['add','sub'] and op[0].type==X86_OP_REG and op[1].type==X86_OP_IMM:
                d=full(op[0]);delta=op[1].imm*(1 if x.mnemonic=='add' else -1)
                if d=='esp':sp+=delta
                elif d in aliases:aliases[d]+=delta
            elif x.mnemonic in ['mov','lea'] and len(op)==2:
                v=value(op[1]) if x.mnemonic=='mov' else None
                if x.mnemonic=='lea' and op[1].type==X86_OP_MEM and x.reg_name(op[1].mem.base) in aliases and not op[1].mem.index:v=aliases[x.reg_name(op[1].mem.base)]+op[1].mem.disp
                if op[0].type==X86_OP_REG:
                    aliases.pop(full(op[0]),None)
                    if v is not None and op[0].size==4:aliases[reg(op[0])]=v
                elif op[0].type==X86_OP_MEM and x.reg_name(op[0].mem.base)=='esp' and not op[0].mem.index:
                    dest=sp+op[0].mem.disp
                    for k in list(stack):
                        if k<dest+op[0].size and k+4>dest:stack.pop(k)
                    if v is not None and op[0].size==4:stack[dest]=v
            else:
                for o in op:
                    if o.type==X86_OP_REG and o.access&CS_AC_WRITE:aliases.pop(full(o),None)
            if not aliases and not stack:continue
            if len(path)>=100:stops.append(dict(address=hex(a),reason='Trace display capped100; analysis state exploration continues'))
            if x.mnemonic.startswith('j'):
                if op[0].type!=X86_OP_IMM:stops.append(dict(address=hex(a),reason='Indirect jump not resolved'));continue
                targets=[op[0].imm] if x.mnemonic=='jmp' else [op[0].imm,nxt]
            else:targets=[nxt]
            for target in targets:todo.append((target,dict(aliases),dict(stack),sp,path))
        reports.append(dict(function=seed['function'],seed=seed['call'],states=states,remaining_queue=len(todo),stops=stops))
    def unique(rows):
        out=[];seen=set()
        for r in rows:
            key=(r['seed'],r.get('access',r.get('call')),str(r.get('pointers',r.get('offset'))))
            if key not in seen:out.append(r);seen.add(key)
        return out
    result=dict(scope=__doc__,seeds=len(seeds),reports=reports,reads=unique(reads),forwarded=unique(forward))
    (ROOT/'phase53-keeper-cfg-alias-leads.json').write_text(json.dumps(result,indent=2),encoding='utf8');print('Seeds',len(seeds),'states',sum(r['states'] for r in reports),'C8 overlap reads',len(result['reads']),'pointer forwards',len(result['forwarded']))
    for r in result['reads']:print('READ',r['function'],r['name'],r['access'],r['offset'])
    for r in result['forwarded']:print('FORWARD',r['function'],r['target'],r['pointers'])
if __name__=='__main__':main()
