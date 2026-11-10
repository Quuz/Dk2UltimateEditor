"""Old Creature-definition producer roots and direct-thiscall read leads; type still needs proof.

Roots are known catalogue lookup returns, same-vslot hints, and old entity field
36F hints. World ordinary/raw Creature lookups can return distinct definition rows. Follow constant aliases and
ESP spills; compose one direct ECX-relative callee read summary. Calls discard
volatile aliases and stack knowledge. Dynamic indices/indirect call returns,
numeric loop bounds and arbitrary argument-pointee semantics are unresolved.
No match is not a proof of global non-use. Linear seed discovery may miss code.
"""
from pathlib import Path
from collections import deque,Counter
from functools import lru_cache
import json,hashlib,bisect
from capstone import Cs,CS_ARCH_X86,CS_MODE_32,CS_AC_READ,CS_AC_WRITE
from capstone.x86 import X86_OP_MEM,X86_OP_REG,X86_OP_IMM
from compare_rc3_converters import code_region
from scan_interprocedural_creature_fields import GROUP
ROOT=Path(__file__).resolve().parent
TARGETS={'Creatures':[(0x75C,4,'AbilityJump'),(0x6EB,1,'PickUpPriority'),(0x684,1,'controlSpellSwipe'),(0x686,4,'controlCreatureFlags'),(0x692,2,'controlHealth')]}

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
 def walk(entry,initial,limit=1500,span=None):
  todo=deque([(entry,dict(initial),{},0,[])]);visited=set();reads=[];calls=[];states=0
  while todo and states<limit:
   pc,aliases,stack,sp,trace=todo.popleft();key=(pc,tuple(sorted(aliases.items())),tuple(sorted(stack.items())),sp)
   if key in visited or (span and not span[0]<=pc<span[1]):continue
   x=ins(pc)
   if x is None:continue
   visited.add(key);states+=1;op=x.operands;nxt=pc+x.size
   def reg(o):return x.reg_name(o.reg)
   def full(o):return GROUP.get(reg(o),reg(o))
   def value(o):
    if o.type==X86_OP_REG and o.size==4:return aliases.get(reg(o))
    if o.type==X86_OP_MEM and x.reg_name(o.mem.base)=='esp' and not o.mem.index and o.size==4:return stack.get(sp+o.mem.disp)
    return None
   for o in op:
    if o.type==X86_OP_MEM and o.access&CS_AC_READ and x.reg_name(o.mem.base) in aliases and not o.mem.index and x.mnemonic not in ['lea','call','jmp']:
     reads.append(dict(address=hex(pc),offset=aliases[x.reg_name(o.mem.base)]+o.mem.disp,bytes=o.size,instruction=x.mnemonic+' '+x.op_str,trace=trace[-12:]))
   if x.mnemonic.startswith('ret'):continue
   if x.mnemonic=='call':
    if 'ecx' in aliases and op[0].type==X86_OP_IMM:calls.append(dict(address=hex(pc),target=op[0].imm,adjustment=aliases['ecx'],trace=trace[-12:]))
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
     if not -512<=aliases[d]<=4096:aliases.pop(d)
   elif x.mnemonic in ['mov','lea'] and len(op)==2:
    v=value(op[1]) if x.mnemonic=='mov' else None
    if x.mnemonic=='lea' and op[1].type==X86_OP_MEM and x.reg_name(op[1].mem.base) in aliases and not op[1].mem.index:v=aliases[x.reg_name(op[1].mem.base)]+op[1].mem.disp
    if op[0].type==X86_OP_REG:
     aliases.pop(full(op[0]),None)
     if v is not None and op[0].size==4 and -512<=v<=4096:aliases[reg(op[0])]=v
    elif op[0].type==X86_OP_MEM and x.reg_name(op[0].mem.base)=='esp' and not op[0].mem.index:
     d=sp+op[0].mem.disp
     for k in list(stack):
      if k<d+op[0].size and k+4>d:stack.pop(k)
     if v is not None and op[0].size==4:stack[d]=v
   else:
    for o in op:
     if o.type==X86_OP_REG and o.access&CS_AC_WRITE:aliases.pop(full(o),None)
   if not aliases and not stack:continue
   if x.mnemonic.startswith('j'):
    if op[0].type!=X86_OP_IMM:continue
    targets=[op[0].imm] if x.mnemonic=='jmp' else [op[0].imm,nxt]
   else:targets=[nxt]
   for target in targets:todo.append((target,dict(aliases),dict(stack),sp,trace+[f'{pc:x}: {x.mnemonic} {x.op_str}']))
  return dict(reads=reads,calls=calls,states=states,limited=bool(todo))
 starts=sorted(starts);summaries={};hits=[];statecount=limited=0;roots=[]
 for seed in seeds:
  result=walk(seed['next'],{seed['register']:0});statecount+=result['states'];limited+=result['limited'];hint=starts[max(0,bisect.bisect_right(starts,seed['address'])-1)];root={**seed,'address':hex(seed['address']),'next':hex(seed['next']),'function_hint':hex(hint),'states':result['states'],'limited':result['limited']};roots.append(root)
  reads=[dict(r,callee=None,call=None) for r in result['reads']]
  for call in result['calls']:
   target=call['target']
   if target not in summaries:summaries[target]=walk(target,{'ecx':0},limit=1000,span=(target,target+8192))
   for r in summaries[target]['reads']:reads.append({**r,'offset':r['offset']+call['adjustment'],'callee':hex(target),'call':call['address'],'caller_trace':call['trace']})
  seen=set()
  for r in reads:
   for field,width,label in TARGETS[seed['family']]:
    if r['offset']>=field+width or r['offset']+r['bytes']<=field:continue
    key=(r['address'],r['offset'],r['callee'],r['call'],label)
    if key in seen:continue
    seen.add(key);hits.append(dict(root=root,field=label,read=r))
 out=dict(scope=__doc__,program_sha256=hashlib.sha256(data).hexdigest(),decoded_instructions=decoded,producer_roots=roots,root_states=statecount,limited_roots=limited,callee_summaries=len(summaries),limited_callees=sum(x['limited'] for x in summaries.values()),candidates=hits)
 (ROOT/'phase63-old-creature-thiscall-leads.json').write_text(json.dumps(out,indent=2));print('roots',len(roots),'states',statecount,'limited',limited,'summaries',len(summaries),'leads',len(hits))
 for h in hits:print(h['root']['family'],h['field'],h['root']['address'],h['read']['address'],h['read']['instruction'],'callee',h['read']['callee'])
if __name__=='__main__':main()
