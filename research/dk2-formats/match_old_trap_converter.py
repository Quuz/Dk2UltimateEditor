"""Candidate full converter equivalence with only external CALL operands masked."""
from pathlib import Path
import json
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import pe_read
from compare_rc3_converters import code_region
ROOT=Path(__file__).resolve().parent

def main():
 dx=ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe';old=ROOT/'source-binaries/DKII-local13.exe';a=dx.read_bytes();b=old.read_bytes();start=0x570D10;n=811;raw=pe_read(a,start,n);mask=bytearray([255])*n;md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;calls=[]
 for i in md.disasm(raw,start):
  if i.mnemonic=='call':
   assert i.size==5 and i.operands[0].imm==0x6361A0
   off=i.address-start+1;mask[off:off+4]=bytes(4);calls.append(off)
 spans=[];s=0
 while s<n:
  if not mask[s]:s+=1;continue
  e=s
  while e<n and mask[e]:e+=1
  spans.append((s,e));s=e
 s,e=max(spans,key=lambda t:t[1]-t[0]);base,code=code_region(b);pos=code.find(raw[s:e]);matches=[]
 while pos!=-1:
  p=pos-s
  if p>=0 and p+n<=len(code) and all(not m or x==y for x,y,m in zip(raw,code[p:p+n],mask)):matches.append(base+p)
  pos=code.find(raw[s:e],pos+1)
 assert len(matches)==1;oldstart=matches[0];other=pe_read(b,oldstart,n)
 relocs=[]
 for off in calls:
  dest=oldstart+off+4+int.from_bytes(other[off:off+4],'little',signed=True);assert dest==0x627CD0;relocs.append(dict(offset=off,dx_target=0x6361A0,old_target=dest))
 # Matching complete native body and preceding RET/NOP boundary, not a fixed
 # padding length: linker alignment varies between these builds.
 prefix=pe_read(b,oldstart-12,12)
 assert any(prefix.endswith(b'\xC3'+b'\x90'*k) for k in range(1,12)),(hex(oldstart),prefix.hex())
 out=dict(dx=start,old=oldstart,bytes=n,mask_hex=mask.hex(),calls=relocs,scope=__doc__);(ROOT/'phase61-trap-converter-match.json').write_text(json.dumps(out,indent=2));qs=[]
 for tool in ['create_function','disassemble_function','force_decompile']:
  ar=dict(program='DKII-local13.exe',address=hex(oldstart))
  if tool=='create_function':ar['disassemble_first']=True
  qs.append(dict(id=f'phase61-old-{oldstart:x}-{tool}',tool=tool,args=ar,print_limit=140))
 (ROOT/'phase61-old-trap-converter-queries.json').write_text(json.dumps(qs,indent=2));print('Trap full converter',hex(start),'old',hex(oldstart),'bytes',n,'two relocated calls',relocs)
if __name__=='__main__':main()
