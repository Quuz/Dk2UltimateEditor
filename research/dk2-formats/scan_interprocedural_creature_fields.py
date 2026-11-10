"""One-call adjusted-this-pointer leads; no object identity or global absence proof.

Compute bounded intraprocedural ECX-relative read/forward summaries over direct
CFG edges, then compose direct thiscall forwards with callee reads once. Does
not resolve stack argument pointers, dynamic indices, indirect calls or returns.
"""
from pathlib import Path
from collections import deque
import json,re
from capstone import Cs,CS_ARCH_X86,CS_MODE_32,CS_AC_READ,CS_AC_WRITE
from capstone.x86 import X86_OP_MEM,X86_OP_REG,X86_OP_IMM
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent
GROUP={'ax':'eax','al':'eax','ah':'eax','bx':'ebx','bl':'ebx','bh':'ebx','cx':'ecx','cl':'ecx','ch':'ecx','dx':'edx','dl':'edx','dh':'edx','si':'esi','di':'edi','bp':'ebp','sp':'esp'}
TARGETS=[('CreaturePickUpPriority',0x6EB,1),('CreatureAbilityJump',0x75C,4)]
def main():
    data=(ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe').read_bytes();md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True
    funcs={int(a,16):(name,int(n)) for a,name,n in re.findall(r'^global: va=([0-9A-F]+),name=([^,\n]+),size=(\d+)',(ROOT/'sources/DKII_EXE_v170.sgmap').read_text(),re.M) if 0x401000<=int(a,16)<0x640000 and int(n)<100000}
    summaries={}
    for start,(name,size) in funcs.items():
        try:ins={x.address:x for x in md.disasm(pe_read(data,start,size),start)}
        except (ValueError,AssertionError):continue
        todo=deque([(start,{'ecx':0},{},0)]);seen=set();reads={};calls={};stops=set();states=0
        while todo and states<2000:
            a,aliases,stack,sp=todo.popleft();key=(a,tuple(sorted(aliases.items())),tuple(sorted(stack.items())),sp)
            if key in seen:continue
            seen.add(key);states+=1
            if a not in ins:stops.add('outside span');continue
            x=ins[a];op=x.operands;nxt=a+x.size
            def reg(o):return x.reg_name(o.reg)
            def full(o):return GROUP.get(reg(o),reg(o))
            def value(o):
                if o.type==X86_OP_REG and o.size==4:return aliases.get(reg(o))
                if o.type==X86_OP_MEM and x.reg_name(o.mem.base)=='esp' and not o.mem.index and o.size==4:return stack.get(sp+o.mem.disp)
                return None
            for o in op:
                if o.type==X86_OP_MEM and o.access&CS_AC_READ and x.reg_name(o.mem.base) in aliases and not o.mem.index and x.mnemonic not in ['lea','call','jmp']:
                    off=aliases[x.reg_name(o.mem.base)]+o.mem.disp
                    if -4096<=off<=4096:reads[(a,off,o.size)]=dict(address=hex(a),offset=off,bytes=o.size,instruction=x.mnemonic+' '+x.op_str)
            if x.mnemonic.startswith('ret'):continue
            if x.mnemonic=='call':
                if 'ecx' in aliases and op[0].type==X86_OP_IMM:
                    dest=op[0].imm;delta=aliases['ecx'];calls[(a,dest,delta)]=dict(address=hex(a),target=dest,adjustment=delta)
                for r in ['eax','ecx','edx']:aliases.pop(r,None)
                stack={};sp=0
            elif x.mnemonic=='push':
                v=value(op[0]);sp-=4
                if v is not None:stack[sp]=v
                else:stack.pop(sp,None)
            elif x.mnemonic=='pop':
                v=stack.pop(sp,None);sp+=4;aliases.pop(full(op[0]),None)
                if v is not None and op[0].size==4:aliases[reg(op[0])]=v
            elif x.mnemonic in ['add','sub'] and len(op)==2 and op[0].type==X86_OP_REG and op[1].type==X86_OP_IMM:
                d=full(op[0]);delta=op[1].imm*(1 if x.mnemonic=='add' else -1)
                if d=='esp':sp+=delta
                elif d in aliases:
                    aliases[d]+=delta
                    if not -4096<=aliases[d]<=4096:aliases.pop(d)
            elif x.mnemonic in ['mov','lea'] and len(op)==2:
                v=value(op[1]) if x.mnemonic=='mov' else None
                if x.mnemonic=='lea' and op[1].type==X86_OP_MEM and x.reg_name(op[1].mem.base) in aliases and not op[1].mem.index:v=aliases[x.reg_name(op[1].mem.base)]+op[1].mem.disp
                if op[0].type==X86_OP_REG:
                    aliases.pop(full(op[0]),None)
                    if v is not None and op[0].size==4 and -4096<=v<=4096:aliases[reg(op[0])]=v
                elif op[0].type==X86_OP_MEM and x.reg_name(op[0].mem.base)=='esp' and not op[0].mem.index:
                    dest=sp+op[0].mem.disp
                    for k in list(stack):
                        if k<dest+op[0].size and k+4>dest:stack.pop(k)
                    if v is not None and op[0].size==4:stack[dest]=v
            else:
                for o in op:
                    if o.type==X86_OP_REG and o.access&CS_AC_WRITE:aliases.pop(full(o),None)
            if not aliases and not stack:continue
            if x.mnemonic.startswith('j'):
                if op[0].type!=X86_OP_IMM:stops.add('indirect jump');continue
                targets=[op[0].imm] if x.mnemonic=='jmp' else [op[0].imm,nxt]
            else:targets=[nxt]
            for target in targets:todo.append((target,dict(aliases),dict(stack),sp))
        summaries[start]=dict(name=name,states=states,unexplored=bool(todo),stops=sorted(stops),reads=list(reads.values()),calls=list(calls.values()))
    (ROOT/'phase57-this-read-summaries.json').write_text(json.dumps({hex(k):v for k,v in summaries.items()}))
    matches=[];forwards=0
    for start,s in summaries.items():
        for call in s['calls']:
            if call['target'] not in summaries:continue
            forwards+=1
            for r in summaries[call['target']]['reads']:
                off=call['adjustment']+r['offset'];hit=[n for n,o,w in TARGETS if off<o+w and off+r['bytes']>o]
                if hit:matches.append(dict(function=hex(start),name=s['name'],call=call['address'],target=hex(call['target']),target_name=summaries[call['target']]['name'],adjustment=call['adjustment'],read=r,effective_offset=off,matches=hit))
    out=dict(scope=__doc__,mapped_functions=len(summaries),states=sum(s['states'] for s in summaries.values()),composed_forwards=forwards,limited_functions=sum(s['unexplored'] for s in summaries.values()),candidates=matches)
    (ROOT/'phase57-interprocedural-creature-leads.json').write_text(json.dumps(out,indent=2));print(json.dumps(out,indent=2))
if __name__=='__main__':main()
