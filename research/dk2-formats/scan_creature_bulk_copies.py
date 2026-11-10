"""Old Creature bulk-copy and copied-stack field candidates.

Extends phase64 exactproducer argument walker with forward REP MOVS ranges.
ECX count is recovered only from a last constant assignment without intervening
writes/calls. Stack aliases may expose later reads of copied fields. Direction
flag forward is an explicit candidate assumption,not emulated proof. Numeric
loop bounds,indexedreads,unknowncopycounts,destinationheapaliases andindirect
calls remain unresolved. Results need objectidentity and fullnativeverification.
"""
from pathlib import Path
from collections import deque,Counter
from functools import lru_cache
import json,hashlib,bisect,re
from capstone import Cs,CS_ARCH_X86,CS_MODE_32,CS_AC_READ,CS_AC_WRITE
from capstone.x86 import X86_OP_MEM,X86_OP_REG,X86_OP_IMM
from compare_rc3_converters import code_region
from scan_interprocedural_creature_fields import GROUP
from native_memory_access import reads_memory
ROOT=Path(__file__).resolve().parent
TARGETS={'Creatures':[(0x75C,4,'AbilityJump'),(0x6EB,1,'PickUpPriority'),(0x684,1,'controlSpellSwipe'),(0x686,4,'controlCreatureFlags'),(0x692,2,'controlHealth')]}

def constant_ecx(trace,decode):
    for line in reversed(trace[-32:]):
        a=int(line.split(':',1)[0],16);x=decode(a)
        if x is None:return None
        op=x.operands
        if x.mnemonic=='call':return None
        if x.mnemonic=='mov' and len(op)==2 and op[0].type==X86_OP_REG and x.reg_name(op[0].reg)=='ecx' and op[0].size==4:
            return op[1].imm if op[1].type==X86_OP_IMM and 0<=op[1].imm<=16384 else None
        if x.mnemonic=='xor' and x.op_str=='ecx, ecx':return 0
        _,written=x.regs_access()
        if any(GROUP.get(x.reg_name(r),x.reg_name(r))=='ecx' for r in written):return None
    return None

def main():
 data=(ROOT/'source-binaries/DKII-local13.exe').read_bytes();base,code=code_region(data);md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;md.skipdata=True;getters={0x501DE0:'Creatures',0x502DE0:'Creatures'};slots={0xC0:'Creatures',0x1C8:'Creatures'};seeds=[];starts=set([base]);decoded=0
 for x in md.disasm(code,base):
  if not x.id:continue
  decoded+=1;op=x.operands;family=dest=None;how=''
  if x.mnemonic=='call' and op[0].type==X86_OP_IMM and base<=op[0].imm<base+len(code):starts.add(op[0].imm)
  if x.mnemonic=='mov' and len(op)==2 and op[0].type==X86_OP_REG and op[0].size==4 and op[1].type==X86_OP_MEM and op[1].mem.base and x.reg_name(op[1].mem.base)!='esp' and not op[1].mem.index and op[1].mem.disp==0x36F:
   family='Creatures';dest=x.reg_name(op[0].reg);how='entity-field-hint'
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
  todo=deque([(entry,dict(initial),dict(initial_stack or {}),0,{},[],[])]);visited=set();reads=[];calls=[];copies=[];states=0
  while todo and states<limit:
   pc,aliases,stack,sp,frames,copied,trace=todo.popleft();key=(pc,tuple(sorted(aliases.items())),tuple(sorted(stack.items())),sp,tuple(sorted(frames.items())),tuple(copied))
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
   for o in op:
    if reads_memory(x,o) and x.reg_name(o.mem.base) in aliases and not o.mem.index:
     reads.append(dict(address=hex(pc),offset=aliases[x.reg_name(o.mem.base)]+o.mem.disp,bytes=o.size,instruction=x.mnemonic+' '+x.op_str,trace=trace[-12:]))
   for o in op:
    at=stack_address(o)
    if reads_memory(x,o) and at is not None:
     for dest,count,source in copied:
      if dest<=at and at+o.size<=dest+count:
       reads.append(dict(address=hex(pc),offset=source+at-dest,bytes=o.size,instruction=x.mnemonic+' '+x.op_str,via='copied-stack',trace=trace[-16:]))
   if x.mnemonic.startswith('ret'):continue
   if x.mnemonic=='call':
    registers={k:v for k,v in aliases.items() if k=='ecx'};arguments={pos+4:stack[sp+pos] for pos in range(0,32,4) if sp+pos in stack}
    if op[0].type==X86_OP_IMM and (registers or arguments):calls.append(dict(address=hex(pc),target=op[0].imm,registers=registers,arguments=arguments,trace=trace[-12:]))
    for r in ['eax','ecx','edx']:aliases.pop(r,None)
    stack={};sp=0;frames={}
   elif x.mnemonic in ['rep movsd','rep movsw','rep movsb','movsd','movsw','movsb'] and len(op)==2:
    count=constant_ecx(trace,ins) if x.mnemonic.startswith('rep ') else 1
    width=4 if x.mnemonic.endswith('movsd') else 2 if x.mnemonic.endswith('movsw') else 1
    size=None if count is None else count*width
    if 'esi' in aliases:
     source=aliases['esi'];dest=frames.get('edi')
     copies.append(dict(address=hex(pc),instruction=x.mnemonic+' '+x.op_str,source_offset=source,bytes=size,stack_destination=dest,trace=trace[-20:]))
     if size:
      reads.append(dict(address=hex(pc),offset=source,bytes=size,instruction=x.mnemonic+' '+x.op_str,via='REP-source-range',trace=trace[-20:]))
      if dest is not None:copied.append((dest,size,source))
    if size is not None:
     for r in ['esi','edi']:
      if r in aliases:aliases[r]+=size
      if r in frames:frames[r]+=size
    else:
     for r in ['esi','edi']:aliases.pop(r,None);frames.pop(r,None)
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
   if not aliases and not stack and not copied:continue
   if x.mnemonic.startswith('j'):
    if op[0].type!=X86_OP_IMM:continue
    targets=[op[0].imm] if x.mnemonic=='jmp' else [op[0].imm,nxt]
   else:targets=[nxt]
   for target in targets:todo.append((target,dict(aliases),dict(stack),sp,dict(frames),list(copied),trace+[f'{pc:x}: {x.mnemonic} {x.op_str}']))
  return dict(reads=reads,calls=calls,copies=copies,states=states,limited=bool(todo))
 starts=sorted(starts);summaries={};hits=[];bulk=[];statecount=limited=0;roots=[]
 for seed in seeds:
  result=walk(seed['next'],{seed['register']:0});statecount+=result['states'];limited+=result['limited'];hint=starts[max(0,bisect.bisect_right(starts,seed['address'])-1)];root={**seed,'address':hex(seed['address']),'next':hex(seed['next']),'function_hint':hex(hint),'states':result['states'],'limited':result['limited']};roots.append(root)
  reads=[dict(r,callee=None,call=None) for r in result['reads']]
  bulk.extend(dict(root=root,copy=c,callee=None) for c in result['copies'])
  work=deque((c,[dict(address=c['address'],target=hex(c['target']),registers=c['registers'],arguments=c['arguments'])],1) for c in result['calls']);seen_context=set()
  while work and len(seen_context)<256:
   call,chain,depth=work.popleft();target=call['target'];key=(target,tuple(sorted(call['registers'].items())),tuple(sorted(call['arguments'].items())))
   if key in seen_context:continue
   seen_context.add(key)
   if key not in summaries:summaries[key]=walk(target,call['registers'],limit=1000,span=(target,target+8192),initial_stack=call['arguments'])
   summary=summaries[key]
   bulk.extend(dict(root=root,copy=c,callee=hex(target),chain=chain) for c in summary['copies'])
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
 out=dict(scope=__doc__,program_sha256=hashlib.sha256(data).hexdigest(),decoded_instructions=decoded,producer_roots=roots,root_states=statecount,limited_roots=limited,callee_summaries=len(summaries),limited_callees=sum(x['limited'] for x in summaries.values()),candidates=hits,bulk_copies=bulk)
 (ROOT/'phase65-old-creature-bulk-copy-leads-v2.json').write_text(json.dumps(out,indent=2));print('bulk',len(bulk),'roots',len(roots),'states',statecount,'limited',limited,'summaries',len(summaries),'leads',len(hits),'argumentlimits',sum(r['limited_argument_contexts'] for r in roots))
 for h in hits:
  if not h['field'].startswith('control'):print(h['root']['family'],h['field'],h['root']['address'],h['read']['address'],h['read']['instruction'],'callee',h['read']['callee'])
if __name__=='__main__':main()
