"""Producer-anchored Object/Trap definition leads, not automatic type proofs.

Roots: loads of entity+12D/F7, exact world lookup calls, or matching world vslot.
Root structure identity must still be checked in primary code. Bounded CFG,
constant aliases and stack spills, plus one cached direct-thiscall composition.
No numeric loop bounds, dynamic indices, arbitrary stack-pointee calls or full
interprocedural return analysis. Search absence cannot establish global non-use.
"""
from pathlib import Path
from collections import deque,Counter
import hashlib,json,re,struct
from capstone import Cs,CS_ARCH_X86,CS_MODE_32,CS_AC_READ,CS_AC_WRITE
from capstone.x86 import X86_OP_MEM,X86_OP_REG,X86_OP_IMM
from build_effect_semantics import pe_read
from scan_interprocedural_creature_fields import GROUP
ROOT=Path(__file__).resolve().parent
TARGETS={'Objects':[(0x106,2,'unknown34C'),(0x104,2,'controlHealth')],
         'Traps':[(0x98,2,'unknown204'),(0x96,2,'controlManaUsage')]}

def main():
 data=(ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe').read_bytes();md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True
 funcs=re.findall(r'^global: va=([0-9A-F]+),name=([^,\n]+),size=(\d+)',(ROOT/'sources/DKII_EXE_v170.sgmap').read_text(),re.M)
 summaries=json.loads((ROOT/'phase57-this-read-summaries.json').read_text())
 getters={0x50DB20:'Objects',0x50D8C0:'Traps'};vt=struct.unpack('<230I',pe_read(data,0x66E3EC,920));slots={vt.index(a)*4:f for a,f in getters.items()}
 roots=[];hits=[];states_total=0;limited=0;seenhits=set();scanned=0
 for ah,name,ns in funcs:
  a=int(ah,16);n=int(ns)
  if not 0x401000<=a<0x640000 or n>100000:continue
  try:ii=list(md.disasm(pe_read(data,a,n),a))
  except (ValueError,AssertionError):continue
  ins={i.address:i for i in ii};scanned+=1
  for seed in ii:
   oo=seed.operands;family=dest=None;how=''
   if seed.mnemonic=='mov' and len(oo)==2 and oo[0].type==X86_OP_REG and oo[0].size==4 and oo[1].type==X86_OP_MEM and oo[1].mem.base and not oo[1].mem.index and oo[1].mem.disp in [0x12D,0xF7]:
    family={0x12D:'Objects',0xF7:'Traps'}[oo[1].mem.disp];dest=seed.reg_name(oo[0].reg);how='entity-field-load'
   elif seed.mnemonic=='call':
    if oo[0].type==X86_OP_IMM and oo[0].imm in getters:family=getters[oo[0].imm];how='exact-lookup-return'
    elif oo[0].type==X86_OP_MEM and oo[0].mem.disp in slots:family=slots[oo[0].mem.disp];how='vslot-return-hint'
    if family:dest='eax'
   if not family:continue
   root=dict(function=hex(a),name=name,seed=hex(seed.address),instruction=seed.mnemonic+' '+seed.op_str,family=family,producer=how);roots.append(root)
   todo=deque([(seed.address+seed.size,{dest:0},{},0,[])]);visited=set();states=0
   def accept(x,offset,size,trace,callee=None,call=None):
    for field,width,label in TARGETS[family]:
     if offset>=field+width or offset+size<=field:continue
     key=(a,seed.address,x.address,offset,label,callee,call)
     if key in seenhits:continue
     seenhits.add(key);hits.append({**root,'address':hex(x.address),'offset':offset,'bytes':size,'field':label,'text':x.mnemonic+' '+x.op_str,'trace':trace[-16:],'callee':callee,'call':call})
   while todo and states<1500:
    pc,aliases,stack,sp,trace=todo.popleft();key=(pc,tuple(sorted(aliases.items())),tuple(sorted(stack.items())),sp)
    if key in visited or pc not in ins:continue
    visited.add(key);states+=1;x=ins[pc];op=x.operands;nxt=pc+x.size
    def reg(o):return x.reg_name(o.reg)
    def full(o):return GROUP.get(reg(o),reg(o))
    def val(o):
     if o.type==X86_OP_REG and o.size==4:return aliases.get(reg(o))
     if o.type==X86_OP_MEM and x.reg_name(o.mem.base)=='esp' and not o.mem.index and o.size==4:return stack.get(sp+o.mem.disp)
     return None
    for o in op:
     if o.type==X86_OP_MEM and o.access&CS_AC_READ and x.reg_name(o.mem.base) in aliases and not o.mem.index and x.mnemonic not in ['lea','call','jmp']:
      accept(x,aliases[x.reg_name(o.mem.base)]+o.mem.disp,o.size,trace)
    if x.mnemonic.startswith('ret'):continue
    if x.mnemonic=='call':
     if 'ecx' in aliases and op[0].type==X86_OP_IMM:
      callee=hex(op[0].imm)
      for r in summaries.get(callee,{}).get('reads',[]):
       pseudo=next(md.disasm(pe_read(data,int(r['address'],16),15),int(r['address'],16)))
       accept(pseudo,aliases['ecx']+r['offset'],r['bytes'],trace,callee,hex(x.address))
     for r in ['eax','ecx','edx']:aliases.pop(r,None)
     stack={};sp=0
    elif x.mnemonic=='push':
     v=val(op[0]);sp-=4
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
      if not -512<=aliases[d]<=512:aliases.pop(d)
    elif x.mnemonic in ['mov','lea'] and len(op)==2:
     v=val(op[1]) if x.mnemonic=='mov' else None
     if x.mnemonic=='lea' and op[1].type==X86_OP_MEM and x.reg_name(op[1].mem.base) in aliases and not op[1].mem.index:v=aliases[x.reg_name(op[1].mem.base)]+op[1].mem.disp
     if op[0].type==X86_OP_REG:
      aliases.pop(full(op[0]),None)
      if v is not None and op[0].size==4 and -512<=v<=512:aliases[reg(op[0])]=v
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
   states_total+=states;limited+=bool(todo)
 out=dict(scope=__doc__,program_sha256=hashlib.sha256(data).hexdigest(),mapped_spans=scanned,world_vslots={hex(k):v for k,v in slots.items()},producer_roots=roots,states=states_total,limited_roots=limited,candidates=hits)
 (ROOT/'phase59-typed-definition-leads.json').write_text(json.dumps(out,indent=2))
 print('roots',len(roots),'states',states_total,'limited',limited,'hits',len(hits),'slots',slots)
 for h in hits:print(h['family'],h['field'],h['function'],h['seed'],h['address'],h['text'],'callee',h['callee'])
if __name__=='__main__':main()
