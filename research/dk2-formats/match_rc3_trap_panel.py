"""Full-body masked matching for Trap panel/model paths; candidates only."""
import json,re
from pathlib import Path
from capstone import Cs,CS_ARCH_X86,CS_MODE_32,CS_GRP_CALL,CS_GRP_JUMP
from capstone.x86 import X86_OP_MEM,X86_OP_IMM
from compare_rc3_converters import ROOT,PRO,RC3,code_region
from build_effect_semantics import pe_read

def main():
 a=PRO.read_bytes();b=RC3.read_bytes();origin,code=code_region(b);md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;out=[];qs=[]
 for name,start in [('ddx',0x470740),('fill',0x471930),('commit',0x471E00),('model',0x4A1770),('writer',0x4A8F40)]:
  file=f'phase60-pro-{start:x}-disassemble_function.txt';aa=[int(l[:8],16) for l in (ROOT/'ghidra'/file).read_text().splitlines() if re.match(r'^[0-9a-f]{8}:',l)];last=aa[-1];end=next(md.disasm(pe_read(a,last,15),last));n=last+end.size-start;raw=pe_read(a,start,n);mask=bytearray([255])*n
  for ins in md.disasm(raw,start):
   i=ins.address-start
   for op in ins.operands:
    if op.type==X86_OP_IMM:
     target=op.imm&0xFFFFFFFF;branch=(ins.group(CS_GRP_CALL) or ins.group(CS_GRP_JUMP)) and not start<=target<start+n
     if branch or (0x400000<=target<0x580000 and not ins.group(CS_GRP_JUMP)):mask[i+ins.imm_offset:i+ins.imm_offset+ins.imm_size]=bytes(ins.imm_size)
    elif op.type==X86_OP_MEM and not op.mem.base and not op.mem.index and 0x400000<=op.mem.disp<0x580000:mask[i+ins.disp_offset:i+ins.disp_offset+ins.disp_size]=bytes(ins.disp_size)
  spans=[];s=0
  while s<n:
   if not mask[s]:s+=1;continue
   e=s
   while e<n and mask[e]:e+=1
   spans.append((s,e));s=e
  s,e=max(spans,key=lambda t:t[1]-t[0]);needle=raw[s:e];pos=code.find(needle);matches=[]
  while pos!=-1:
   j=pos-s
   if j>=0 and j+n<=len(code) and all(not m or x==y for x,y,m in zip(raw,code[j:j+n],mask)):matches.append(origin+j)
   pos=code.find(needle,pos+1)
  print(name,hex(start),n,'RC3',list(map(hex,matches)))
  out.append(dict(name=name,pro=start,bytes=n,rc3_candidates=matches,mask_hex=mask.hex(),pro_assembly=file))
  for rc in matches:
   for t in ['create_function','disassemble_function','force_decompile']:
    ar=dict(program='DK2-Editor-rc3.exe',address=hex(rc))
    if t=='create_function':ar['disassemble_first']=True
    qs.append(dict(id=f'phase60-rc3-{name}-{t}',tool=t,args=ar,print_limit=20))
 (ROOT/'phase60-trap-matches.json').write_text(json.dumps(out,indent=2));(ROOT/'phase60-rc3-trap-queries.json').write_text(json.dumps(qs,indent=2))
if __name__=='__main__':main()
