"""Bounded affine CFG search from exact shared-parameter base producers.

Candidates, not semantics or non-use proof. Ghidra instruction boundaries and
whole byte extents are primary; path flags are overapproximated. Calls, escapes,
unknown jumps and bounded queues remain explicit. x86 arithmetic is modulo32.
"""
from pathlib import Path
from collections import deque,Counter
import json,re,math,struct
from capstone import Cs,CS_ARCH_X86,CS_MODE_32,CS_AC_WRITE
from capstone.x86 import X86_OP_REG,X86_OP_MEM,X86_OP_IMM
from build_effect_semantics import EXE,pe_read
from native_memory_access import reads_memory
from emulate_editor_variables_writer import sha
ROOT=Path(__file__).resolve().parent
BASE=0x6F2550
FIELDS={29:(0x71,4),186:(0x41A,4)}
GROUP={'ax':'eax','al':'eax','ah':'eax','bx':'ebx','bl':'ebx','bh':'ebx','cx':'ecx','cl':'ecx','ch':'ecx','dx':'edx','dl':'edx','dh':'edx','si':'esi','di':'edi','bp':'ebp','sp':'esp'}
REGS=('eax','ebx','ecx','edx','esi','edi','ebp')
load=lambda n:json.loads((ROOT/n).read_text(encoding='utf8'))
dump=lambda n,x:(ROOT/n).write_text(json.dumps(x,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
s32=lambda x:(x&0x7fffffff)-(x&0x80000000)
const=lambda n:(0,s32(n),())
ptr=lambda n=0:(1,s32(n),())
unknown=lambda key,lo=0,hi=0xffffffff:(0,0,((key,1,lo,hi),))


def combine(a,b,mul=1):
    terms={k:(c,lo,hi) for k,c,lo,hi in a[2]}
    for k,c,lo,hi in b[2]:
        before=terms.get(k,(0,lo,hi));assert before[1:]==(lo,hi)
        terms[k]=(s32(before[0]+mul*c),lo,hi)
    return (s32(a[0]+mul*b[0]),s32(a[1]+mul*b[1]),tuple((k,c,lo,hi) for k,(c,lo,hi) in sorted(terms.items()) if c))


def times(a,n):return combine(const(0),a,n)


def possible(expr,size,field):
    """Exact one-symbol modular overlap; multi-symbol only necessary condition."""
    start,width=FIELDS[field];base,off,terms=expr
    if base!=1:return None
    if not terms:return dict(kind='exact',offset=off) if off<start+width and off+size>start else None
    answers=[]
    if len(terms)==1:
        key,k,lo,hi=terms[0];mod=1<<32;g=math.gcd(k,mod);step=mod//g
        for x in range(start-size+1,start+width):
            delta=(x-off)&0xffffffff
            if delta%g:continue
            first=((delta//g)*pow((k//g)%step,-1,step))%step if step!=1 else 0
            candidate=first+max(0,(lo-first+step-1)//step)*step
            if candidate<=hi:answers.append(dict(symbol=key,value=candidate,access_offset=x))
        return dict(kind='one_symbol_modular_candidate',solutions=answers[:16]) if answers else None
    g=math.gcd(1<<32,*[k for _,k,_,_ in terms])
    if not any(((x-off)&0xffffffff)%g==0 for x in range(start-size+1,start+width)):return None
    low=off+sum(min(k*lo,k*hi) for _,k,lo,hi in terms);high=off+sum(max(k*lo,k*hi) for _,k,lo,hi in terms)
    if -(1<<31)<=low<=high<(1<<31) and (high+size<=start or low>=start+width):return None
    return dict(kind='multi_symbol_necessary_condition_only',interval=[low,high])


def primary():
    blob=EXE.read_bytes();out=[];seen=set();md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True
    infofiles={ROOT/'ghidra'/(q['id']+'.txt') for name in ['phase200-global-alias-owner-queries.json','phase200-missing-owner-queries.json'] for q in load(name) if q['tool']=='get_function_by_address'}
    for f in sorted(infofiles):
        m=re.search(r'Body: ([0-9a-fA-F]+) - ([0-9a-fA-F]+)',f.read_text())
        if not m:continue
        a,b=[int(t,16) for t in m.groups()]
        if a in seen:continue
        seen.add(a);mp=f'ghidra/phase200-DX-function-{a:x}-memory.txt';ap=f'ghidra/phase200-DX-function-{a:x}-assembly.txt'
        j=load(mp);raw=bytes(j['data']);assert raw==pe_read(blob,a,b-a+1)
        starts=[int(x,16) for x in re.findall(r'^([0-9a-fA-F]{8}):',(ROOT/ap).read_text(),re.M)]
        code={pc:next(md.disasm(pe_read(blob,pc,15),pc,count=1)) for pc in starts}
        # WAIT+FNxxx can be printed as one Ghidra instruction, two Unicorn PCs.
        for pc,x in list(code.items()):
            if x.mnemonic=='wait':code[pc+x.size]=next(md.disasm(pe_read(blob,pc+x.size,15),pc+x.size,count=1))
        row=dict(address=a,length=b-a+1,info=str(f.relative_to(ROOT)),info_sha256=sha(f.read_bytes()),memory=mp,memory_sha256=sha((ROOT/mp).read_bytes()),assembly=ap,assembly_sha256=sha((ROOT/ap).read_bytes()),bytes_sha256=sha(raw),instructions=len(code))
        out.append((row,code))
    return out



def initial_facts(seed,row,ins):
    # Facts only from an uninterrupted unique-predecessor chain. A nearby
    # mask reached by another branch is deliberately not assumed to dominate.
    preds={a:[] for a in ins}
    for a,x in ins.items():
        if x.mnemonic.startswith('ret'):continue
        nxt=a+x.size
        if x.mnemonic.startswith('j'):
            targets=([x.operands[0].imm] if x.mnemonic=='jmp' else [nxt,x.operands[0].imm]) if x.operands[0].type==X86_OP_IMM else []
        else:targets=[nxt]
        for b in targets:
            if b in preds:preds[b].append(a)
    facts={};proofs=[]
    for name in REGS:
        if name==seed['register']:continue
        cur=seed['address'];chain=[];seen=set()
        while cur!=row['address'] and len(chain)<20 and len(preds.get(cur,[]))==1:
            pc=preds[cur][0]
            if pc in seen:break
            seen.add(pc);chain.append(pc);x=ins[pc];op=x.operands
            if x.mnemonic=='call' and name in ['eax','ecx','edx']:break
            _,written=x.regs_access();writes={GROUP.get(x.reg_name(r),x.reg_name(r)) for r in written}
            if name in writes:
                if len(op)==2 and op[0].type==X86_OP_REG and op[0].size==4 and x.reg_name(op[0].reg)==name:
                    if x.mnemonic=='and' and op[1].type==X86_OP_IMM:
                        facts[name]=unknown(f'{pc:x}:premask:{op[1].imm&0xffffffff:x}',0,op[1].imm&0xffffffff)
                    elif x.mnemonic=='movzx' and op[1].size<4:facts[name]=unknown(f'{pc:x}:prezero{8*op[1].size}',0,(1<<(8*op[1].size))-1)
                    elif x.mnemonic=='mov' and op[1].type==X86_OP_IMM:facts[name]=const(op[1].imm)
                    if name in facts:proofs.append(dict(register=name,value=facts[name],source=pc,unique_predecessor_chain=chain))
                break
            cur=pc
    return facts,proofs

def trace(seed,row,ins,limit=8000):
    first=seed['next'];states=0;seen=set();facts,proofs=initial_facts(seed,row,ins);facts[seed['register']]=ptr();todo=deque([(first,facts, {},0,[])])
    reads=[];candidates=[];forward=[];escapes=[];stops=[]
    def stop(pc,why):
        v=(pc,why)
        if v not in stops:stops.append(v)
    while todo and states<limit:
        a,regs,stack,sp,path=todo.popleft();key=(a,tuple(sorted(regs.items())),tuple(sorted(stack.items())),sp)
        if key in seen:continue
        seen.add(key);states+=1
        if a not in ins:stop(a,'target_outside_Ghidra_instruction_body');continue
        x=ins[a];op=x.operands;nxt=a+x.size;path=(path+[dict(address=a,instruction=x.mnemonic+' '+x.op_str)])[-40:]
        def reg(r):return GROUP.get(x.reg_name(r),x.reg_name(r))
        def rv(r):
            name=reg(r)
            return regs.get(name,unknown('input:'+name))
        def effective(o):
            v=const(o.mem.disp)
            if o.mem.base:v=combine(v,rv(o.mem.base))
            if o.mem.index:v=combine(v,times(rv(o.mem.index),o.mem.scale))
            return v
        def value(o):
            if o.type==X86_OP_IMM:return const(o.imm)
            if o.type==X86_OP_REG and o.size==4:return rv(o.reg)
            if o.type==X86_OP_MEM and reg(o.mem.base)=='esp' and not o.mem.index and o.size==4:return stack.get(sp+o.mem.disp,unknown(f'{a:x}:stack'))
            return unknown(f'{a:x}:load')
        for o in op:
            if o.type!=X86_OP_MEM or not reads_memory(x,o):continue
            expr=effective(o)
            if expr[0]!=1:continue
            r=dict(seed=seed['address'],function=row['address'],access=a,instruction=x.mnemonic+' '+x.op_str,bytes=o.size,expression=expr)
            reads.append(r)
            for typ in FIELDS:
                match=possible(expr,o.size,typ)
                if match:candidates.append(dict(r,type=typ,match=match,path=path))
        if x.mnemonic.startswith('ret'):continue
        if x.mnemonic.startswith('rep') or x.mnemonic in {'stosb','stosw','stosd','movsb','movsw','movsd','lodsb','lodsw','lodsd','scasb','scasw','scasd','cmpsb','cmpsw','cmpsd'}:
            stop(a,'implicit_string_or_repeat_not_expanded');continue
        if x.mnemonic=='call':
            pointers={}
            if regs.get('ecx',const(0))[0]:pointers['ecx']=regs['ecx']
            for off in range(0,32,4):
                v=stack.get(sp+off)
                if v and v[0]:pointers[f'stack+{off}']=v
            if pointers:forward.append(dict(seed=seed['address'],function=row['address'],call=a,target=x.op_str,pointers=pointers,path=path))
            for name in ['eax','ecx','edx']:regs[name]=unknown(f'{a:x}:return:{name}')
            # Callee cleanup/stack aliasing is unknown; retaining nonvolatile
            # register values follows the declared standard MSVC ABI assumption.
            stack={};sp=0
        elif x.mnemonic=='push':
            v=value(op[0]);sp-=4;stack[sp]=v
        elif x.mnemonic=='pop':
            v=stack.pop(sp,unknown(f'{a:x}:pop'));sp+=4
            if op[0].type==X86_OP_REG:regs[reg(op[0].reg)]=v
        elif x.mnemonic in ['mov','lea','movzx','movsx'] and len(op)==2:
            dst,src=op
            if x.mnemonic=='lea':v=effective(src)
            elif x.mnemonic in ['movzx','movsx'] and src.size<4:
                bits=8*src.size;lo,hi=(0,(1<<bits)-1) if x.mnemonic=='movzx' else (-(1<<(bits-1)),(1<<(bits-1))-1)
                v=unknown(f'{a:x}:{x.mnemonic}{bits}',lo,hi)
            else:v=value(src)
            if dst.type==X86_OP_REG:
                name=reg(dst.reg)
                if dst.size==4:regs[name]=v
                else:
                    if regs.get(name,const(0))[0]:stop(a,'partial_register_write_discards_pointer_identity')
                    regs[name]=unknown(f'{a:x}:partial:{name}')
            elif dst.type==X86_OP_MEM:
                if reg(dst.mem.base)=='esp' and not dst.mem.index:
                    off=sp+dst.mem.disp
                    for k in list(stack):
                        if k<off+dst.size and k+4>off:stack.pop(k)
                    if dst.size==4:stack[off]=v
                elif v[0]:escapes.append(dict(seed=seed['address'],function=row['address'],access=a,instruction=x.mnemonic+' '+x.op_str,value=v,path=path))
        elif x.mnemonic in ['xor','sub'] and len(op)==2 and op[0].type==op[1].type==X86_OP_REG and op[0].reg==op[1].reg and op[0].size==4:regs[reg(op[0].reg)]=const(0)
        elif x.mnemonic in ['add','sub'] and len(op)==2 and op[0].type==X86_OP_REG and op[0].size==4:
            name=reg(op[0].reg);mul=1 if x.mnemonic=='add' else -1
            if name=='esp' and op[1].type==X86_OP_IMM:sp+=mul*op[1].imm
            else:regs[name]=combine(rv(op[0].reg),value(op[1]),mul)
        elif x.mnemonic=='and' and len(op)==2 and op[0].type==X86_OP_REG and op[0].size==4 and op[1].type==X86_OP_IMM:
            name=reg(op[0].reg);before=rv(op[0].reg);mask=op[1].imm&0xffffffff
            if before[0]:stop(a,'pointer_bitmask_not_affine')
            if before[0]==0 and not before[2]:regs[name]=const(before[1]&mask)
            else:regs[name]=unknown(f'{a:x}:mask:{mask:x}',0,mask)
        elif x.mnemonic in ['inc','dec'] and op[0].type==X86_OP_REG and op[0].size==4:regs[reg(op[0].reg)]=combine(rv(op[0].reg),const(1 if x.mnemonic=='inc' else -1))
        elif x.mnemonic in ['shl','sal'] and op[0].type==X86_OP_REG and op[1].type==X86_OP_IMM:regs[reg(op[0].reg)]=times(rv(op[0].reg),1<<op[1].imm)
        elif x.mnemonic=='imul' and len(op) in [2,3] and op[0].type==X86_OP_REG and op[-1].type==X86_OP_IMM:
            source=op[0] if len(op)==2 else op[1];regs[reg(op[0].reg)]=times(value(source),op[-1].imm)
        else:
            read_regs,written=x.regs_access();read_names={reg(r) for r in read_regs}
            for r in written:
                name=reg(r)
                if name not in REGS:continue
                if regs.get(name,const(0))[0] and name in read_names:stop(a,'unsupported_instruction_discards_pointer_identity:'+x.mnemonic)
                regs[name]=unknown(f'{a:x}:write:{name}')
        if not any(v[0] for v in list(regs.values())+list(stack.values())):continue
        if x.mnemonic.startswith('j'):
            if op[0].type!=X86_OP_IMM:stop(a,'unresolved_indirect_jump');continue
            targets=[op[0].imm] if x.mnemonic=='jmp' else [op[0].imm,nxt]
        else:targets=[nxt]
        for target in targets:todo.append((target,dict(regs),dict(stack),sp,path))
    return dict(seed=seed['address'],function=row['address'],states=states,remaining_queue=len(todo),limit=limit,initial_facts_proofs=proofs,stops=[dict(address=a,reason=r) for a,r in stops]),reads,candidates,forward,escapes


def main():
    seeds=load('phase200-global-alias-seeds.json')['seeds'];funcs=primary();report=[];reads=[];hits=[];forward=[];escapes=[];coverage=[]
    for seed in seeds:
        owners=[(r,ins) for r,ins in funcs if seed['address'] in ins]
        assert len(owners)==1,(hex(seed['address']),len(owners))
        row,code=owners[0];x=code[seed['address']];assert x.bytes.hex()==seed['hex']
        r,rd,h,fw,es=trace(seed,row,code);report.append(r);reads+=rd;hits+=h;forward+=fw;escapes+=es
    def unique(rows,keys):
        d={}
        for r in rows:d[tuple(str(r[k]) for k in keys)]=r
        return list(d.values())
    reads=unique(reads,['seed','access','expression']);hits=unique(hits,['seed','access','type','expression']);forward=unique(forward,['seed','call','pointers']);escapes=unique(escapes,['seed','access','value'])
    out=dict(phase=200,status='bounded_static_candidates_not_semantics',profile=dict(file=str(EXE),sha256=sha(EXE.read_bytes())),primary_functions=[r for r,_ in funcs],seeds=seeds,reports=report,alias_reads=reads,field_overlap_candidates=hits,pointer_forwards=forward,pointer_escapes=escapes,
        counts=dict(seeds=len(seeds),functions=len(funcs),states=sum(r['states'] for r in report),bounded_scenes=sum(bool(r['remaining_queue']) for r in report),reads=len(reads),overlap_candidates=len(hits),exact_overlap_candidates=sum(h['match']['kind']=='exact' for h in hits),pointer_forwards=len(forward),pointer_escapes=len(escapes)),
        scope=__doc__,limits=['Not full interprocedural analysis. Pointer forwards and escapes remain leads.','Conditional flags not constrained; paths overapproximate feasible execution.','One-symbol modular address overlap is exact for its explicit domain; multi-symbol results are necessary conditions only.','Nonvolatile preservation assumes standard MSVC ABI; volatile registers and stack aliases are killed on unknown calls.','String/repeat instructions and unhandled pointer transforms/partial writes are explicit stops.','No global non-use conclusion and no new meanings for29/186.'])
    dump('phase200-global-alias-search.json',out);print(out['counts'])
    print('candidate accesses',[(hex(h['access']),h['type'],h['instruction'],h['match']['kind']) for h in hits[:30]])
    print('stop reasons',Counter(s['reason'] for r in report for s in r['stops']))


if __name__=='__main__':main()
