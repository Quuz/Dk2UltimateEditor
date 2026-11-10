"""Effect pointer search with numeric stack locals and scoped native call cleanup.

Original argument/return/entity hints retained; memory values remain unknown.
Six indirect callsites use original-vtable associations from earlier complete
native fixtures. Other indirect calls reset knowledge conservatively. No global
nonuse proof; unsupported accesses, context limits and ABI assumptions explicit.
"""
from pathlib import Path
from collections import deque,Counter
from functools import lru_cache
import json,hashlib,bisect
from capstone import Cs,CS_ARCH_X86,CS_MODE_32,CS_AC_READ,CS_AC_WRITE
from capstone.x86 import X86_OP_MEM,X86_OP_REG,X86_OP_IMM
from compare_rc3_converters import code_region
from scan_interprocedural_creature_fields import GROUP
from constant_branch_domain import step as constant_step,branch as constant_branch
from effect_call_abi import resolve as call_abi
from native_memory_access import reads_memory
ROOT=Path(__file__).resolve().parent
TARGETS={'Effects':[(0x4B,2,'Misc2'),(0x4D,2,'Misc3'),(0x49,2,'controlGenerateRandomness'),(0x5F,4,'controlFlags'),(0x79,2,'controlId')]}

def main():
 data=(ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe').read_bytes();base,code=code_region(data);md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;md.skipdata=True;getters={0x440BC0:'Effects'};slots={0x184:'Effects'};seeds=[];starts=set([base]);decoded=0
 for x in md.disasm(code,base):
  if not x.id:continue
  decoded+=1;op=x.operands;family=dest=None;how=''
  if x.mnemonic=='call' and op[0].type==X86_OP_IMM and base<=op[0].imm<base+len(code):starts.add(op[0].imm)
  if x.mnemonic=='mov' and len(op)==2 and op[0].type==X86_OP_REG and op[0].size==4 and op[1].type==X86_OP_MEM and op[1].mem.base and x.reg_name(op[1].mem.base)!='esp' and not op[1].mem.index and op[1].mem.disp==0x107:
   family='Effects';dest=x.reg_name(op[0].reg);how='entity-field-hint'
  elif x.mnemonic=='call':
   if op[0].type==X86_OP_IMM and op[0].imm in getters:family=getters[op[0].imm];how='exact-lookup-return'
   elif op[0].type==X86_OP_MEM and op[0].mem.disp in slots:family=slots[op[0].mem.disp];how='vslot-return-hint'
   if family:dest='eax'
  if family:seeds.append(dict(address=x.address,next=x.address+x.size,family=family,register=dest,producer=how,instruction=x.mnemonic+' '+x.op_str))
 @lru_cache(maxsize=60000)
 def ins(a):
  if not base<=a<base+len(code):return None
  x=next(md.disasm(code[a-base:a-base+15],a,count=1),None)
  return x if x and x.id else None
 def walk(entry,initial,limit=1500,span=None,initial_stack=None):
  todo=deque([(entry,dict(initial),dict(initial_stack or {}),0,{},[],{},None,{})]);visited=set();reads=[];calls=[];states=0
  while todo and states<limit:
   pc,aliases,stack,sp,frames,trace,constants,condition_flags,numeric_stack=todo.popleft();key=(pc,tuple(sorted(aliases.items())),tuple(sorted(stack.items())),sp,tuple(sorted(frames.items())),tuple(sorted(constants.items())),None if condition_flags is None else tuple(sorted(condition_flags.items())),tuple(sorted(numeric_stack.items())))
   if key in visited or (span and not span[0]<=pc<span[1]):continue
   x=ins(pc)
   if x is None:continue
   visited.add(key);states+=1;op=x.operands;nxt=pc+x.size
   def reg(o):return x.reg_name(o.reg)
   def full(o):return GROUP.get(reg(o),reg(o))
   def frame(o):
    if o.type==X86_OP_REG and o.size==4:return sp if reg(o)=='esp' else frames.get(reg(o))
    return None
   def stack_address(o):
    if o.type!=X86_OP_MEM or o.mem.index:return None
    base=x.reg_name(o.mem.base);origin=sp if base=='esp' else frames.get(base)
    return None if origin is None else origin+o.mem.disp
   def value(o):
    if o.type==X86_OP_REG and o.size==4:return aliases.get(reg(o))
    at=stack_address(o)
    if at is not None and o.size==4:return stack.get(at)
    return None
   def numeric_value(o):
    if o.type==X86_OP_IMM:return o.imm&0xFFFFFFFF
    if o.type==X86_OP_REG and o.size==4:return constants.get(reg(o))
    at=stack_address(o)
    if at is not None and o.size==4:return numeric_stack.get(at)
    return None
   src_number=numeric_value(op[1]) if len(op)>1 else None
   pushed_number=numeric_value(op[0]) if op else None
   popped_number=numeric_stack.get(sp)
   branch_result=constant_branch(x.mnemonic,condition_flags) if x.mnemonic.startswith('j') else None
   constants,condition_flags=constant_step(x,constants,condition_flags)
   if x.mnemonic=='mov' and len(op)==2:
    if op[0].type==X86_OP_REG and op[0].size==4 and src_number is not None:constants[reg(op[0])]=src_number
    at=stack_address(op[0])
    if at is not None:
     for k in list(numeric_stack):
      if k<at+op[0].size and k+4>at:numeric_stack.pop(k)
     if op[0].size==4 and src_number is not None:numeric_stack[at]=src_number
   elif x.mnemonic=='push':
    if pushed_number is None:numeric_stack.pop(sp-4,None)
    else:numeric_stack[sp-4]=pushed_number
   elif x.mnemonic=='pop':
    numeric_stack.pop(sp,None)
    if op[0].type==X86_OP_REG and op[0].size==4 and popped_number is not None:constants[reg(op[0])]=popped_number
   elif op and op[0].type==X86_OP_MEM and op[0].access&CS_AC_WRITE:
    at=stack_address(op[0])
    if at is not None:
     for k in list(numeric_stack):
      if k<at+op[0].size and k+4>at:numeric_stack.pop(k)
   for o in op:
    if reads_memory(x,o) and x.reg_name(o.mem.base) in aliases and not o.mem.index:
     reads.append(dict(address=hex(pc),offset=aliases[x.reg_name(o.mem.base)]+o.mem.disp,bytes=o.size,instruction=x.mnemonic+' '+x.op_str,trace=trace[-12:]))
   if x.mnemonic.startswith('ret'):continue
   if x.mnemonic=='call':
    registers={k:v for k,v in aliases.items() if k=='ecx'};arguments={pos+4:stack[sp+pos] for pos in range(0,32,4) if sp+pos in stack}
    if op[0].type==X86_OP_IMM and (registers or arguments):calls.append(dict(address=hex(pc),target=op[0].imm,registers=registers,arguments=arguments,trace=trace[-12:]))
    for r in ['eax','ecx','edx']:aliases.pop(r,None);frames.pop(r,None)
    abi=call_abi(pc,op[0].imm if op[0].type==X86_OP_IMM else None)
    if abi is None:
     stack={};sp=0;frames={};numeric_stack={}
    else:
     size=abi['argument_bytes']
     if size is None:stack={};numeric_stack={}
     else:
      stack={k:v for k,v in stack.items() if not(k<sp+size and k+4>sp)}
      numeric_stack={k:v for k,v in numeric_stack.items() if not(k<sp+size and k+4>sp)}
     sp+=abi['callee_pop']
   elif x.mnemonic=='push':
    v=value(op[0]);sp-=4
    if v is not None:stack[sp]=v
    else:stack.pop(sp,None)
   elif x.mnemonic=='pop':
    v=stack.pop(sp,None);sp+=4;aliases.pop(full(op[0]),None);frames.pop(full(op[0]),None)
    if v is not None and op[0].size==4:aliases[reg(op[0])]=v
   elif x.mnemonic in ['add','sub'] and len(op)==2 and op[0].type==X86_OP_REG and op[1].type==X86_OP_IMM:
    d=full(op[0]);delta=op[1].imm*(1 if x.mnemonic=='add' else -1)
    if d=='esp':sp+=delta
    else:
     if d in frames:frames[d]+=delta
     if d in aliases:
      aliases[d]+=delta
      if not -512<=aliases[d]<=4096:aliases.pop(d)
   elif x.mnemonic in ['mov','lea'] and len(op)==2:
    v=value(op[1]) if x.mnemonic=='mov' else None;fp=frame(op[1]) if x.mnemonic=='mov' else stack_address(op[1])
    if x.mnemonic=='lea' and op[1].type==X86_OP_MEM and x.reg_name(op[1].mem.base) in aliases and not op[1].mem.index:v=aliases[x.reg_name(op[1].mem.base)]+op[1].mem.disp
    if op[0].type==X86_OP_REG:
     d=full(op[0]);aliases.pop(d,None);frames.pop(d,None)
     if v is not None and op[0].size==4 and -512<=v<=4096:aliases[reg(op[0])]=v
     if fp is not None and op[0].size==4:
      if d=='esp':sp=fp
      else:frames[d]=fp
    elif stack_address(op[0]) is not None:
     d=stack_address(op[0])
     for k in list(stack):
      if k<d+op[0].size and k+4>d:stack.pop(k)
     if v is not None and op[0].size==4:stack[d]=v
   else:
    for o in op:
     if o.type==X86_OP_REG and o.access&CS_AC_WRITE:aliases.pop(full(o),None);frames.pop(full(o),None)
   if not aliases and not stack:continue
   if x.mnemonic.startswith('j'):
    if op[0].type!=X86_OP_IMM:continue
    targets=[op[0].imm] if branch_result is True else [nxt] if branch_result is False else [op[0].imm,nxt]
   else:targets=[nxt]
   for target in targets:todo.append((target,dict(aliases),dict(stack),sp,dict(frames),trace+[f'{pc:x}: {x.mnemonic} {x.op_str}'],dict(constants),None if condition_flags is None else dict(condition_flags),dict(numeric_stack)))
  return dict(reads=reads,calls=calls,states=states,limited=bool(todo))
 table=json.loads((ROOT/'effect-semantics.json').read_text(encoding='utf8'))['generation_dispatch']['entries']
 for e in table:
  if int(e['callback'],16):seeds.append(dict(address=int(e['callback'],16),next=int(e['callback'],16),family='Effects',register=None,producer='generation-callback-stack-argument',mode=e['value'],instruction='First native stack argument is current Effect definition'))
 starts=sorted(starts);summaries={};hits=[];statecount=limited=0;roots=[]
 for seed in seeds:
  result=walk(seed['next'],{} if seed['register'] is None else {seed['register']:0},initial_stack={4:0} if seed['register'] is None else None);statecount+=result['states'];limited+=result['limited'];hint=starts[max(0,bisect.bisect_right(starts,seed['address'])-1)];root={**seed,'address':hex(seed['address']),'next':hex(seed['next']),'function_hint':hex(hint),'states':result['states'],'limited':result['limited']};roots.append(root)
  reads=[dict(r,callee=None,call=None) for r in result['reads']]
  work=deque((c,[dict(address=c['address'],target=hex(c['target']),registers=c['registers'],arguments=c['arguments'])],1) for c in result['calls']);seen_context=set()
  while work and len(seen_context)<256:
   call,chain,depth=work.popleft();target=call['target'];key=(target,tuple(sorted(call['registers'].items())),tuple(sorted(call['arguments'].items())))
   if key in seen_context:continue
   seen_context.add(key)
   if key not in summaries:summaries[key]=walk(target,call['registers'],limit=1000,span=(target,target+8192),initial_stack=call['arguments'])
   summary=summaries[key]
   for r in summary['reads']:reads.append({**r,'callee':hex(target),'call':call['address'],'caller_trace':call['trace'],'argument_chain':chain})
   if depth<3:
    for child in summary['calls']:work.append((child,chain+[dict(address=child['address'],target=hex(child['target']),registers=child['registers'],arguments=child['arguments'])],depth+1))
  root['argument_contexts']=len(seen_context);root['limited_argument_contexts']=bool(work)
  seen=set()
  for r in reads:
   for field,width,label in TARGETS[seed['family']]:
    if r['offset']>=field+width or r['offset']+r['bytes']<=field:continue
    key=(r['address'],r['offset'],r['callee'],r['call'],label)
    if key in seen:continue
    seen.add(key);hits.append(dict(root=root,field=label,read=r))
 out=dict(scope=__doc__,program_sha256=hashlib.sha256(data).hexdigest(),decoded_instructions=decoded,producer_roots=roots,root_states=statecount,limited_roots=limited,callee_summaries=len(summaries),limited_callees=sum(x['limited'] for x in summaries.values()),candidates=hits,constant_domain_sha256=hashlib.sha256((ROOT/'constant_branch_domain.py').read_bytes()).hexdigest(),ABI_evidence_sha256=hashlib.sha256((ROOT/'phase158-effect-call-ABI.json').read_bytes()).hexdigest(),limited_contexts=[dict(target=hex(k[0]),registers=dict(k[1]),arguments=dict(k[2]),summary=v) for k,v in summaries.items() if v['limited']])
 (ROOT/'phase158-effect-bounded-argument-leads.json').write_text(json.dumps(out,indent=2));print('roots',len(roots),'states',statecount,'limited',limited,'summaries',len(summaries),'leads',len(hits),'argumentlimits',sum(r['limited_argument_contexts'] for r in roots))
 for h in [h for h in hits if not h['field'].startswith('control')][:35]:
  if not h['field'].startswith('control'):print(h['root']['family'],h['field'],h['root']['address'],h['read']['address'],h['read']['instruction'],'callee',h['read']['callee'])
if __name__=='__main__':main()
