"""Capture full old functions and explicitly delimited RTTI/code fragments."""
from pathlib import Path
import json,re
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent
def main():
 exe=ROOT/'source-binaries/DKII-local13.exe';data=exe.read_bytes();md=Cs(CS_ARCH_X86,CS_MODE_32);rows=[];blocks=[];qs=[]
 funcs=[0x502300,0x502560,0x5020A0,0x49E770,0x49BF10,0x492370,0x4ADF80,0x4ADE90,0x4ADD10,0x4A94B0,0x4A89B0,0x4AB590,0x58ACA0,0x4ED3B0,0x4B22D0,0x512D00,0x513BC0,0x494E80,0x563920]
 for a in funcs:
  file=f'phase62-old-{a:x}-disassemble_function.txt';aa=[int(l[:8],16) for l in (ROOT/'ghidra'/file).read_text().splitlines() if re.match(r'^[0-9a-fA-F]{8}:',l)];assert aa[0]==a;last=aa[-1];end=next(md.disasm(pe_read(data,last,15),last));n=last+end.size-a;key=f'phase62-final-old-{a:x}'
  assert n<10000
  rows.append(dict(program='DKII-local13.exe',exe=str(exe),address=a,length=n,before=file,after=key+'-assembly.txt',memory=key+'-memory.txt'));qs.extend([dict(id=key+'-assembly',tool='disassemble_function',args=dict(program='DKII-local13.exe',address=hex(a)),print_limit=10),dict(id=key+'-memory',tool='read_memory',args=dict(program='DKII-local13.exe',address=hex(a),length=n),print_limit=10)])
 defs=[('world-vtable',0x65F31C,920,'initialized data'),('trap-factory-fragment',0x4AD54E,40,'code fragment;allocation/base paths not fully executed'),('door-factory-fragment',0x4AD3C3,42,'code fragment;allocation/base paths not fully executed'),('object-factory-fragment',0x4AD6D1,40,'code fragment;allocation/base paths not fully executed'),('computer-enable-fragment',0x4B22B0,25,'code fragment with tail jump;Ghidra inferred disjoint body not treated as one contiguous nativefunction'),('computer-label-fragment',0x4B4341,27,'code fragment in diagnostic writer;not a complete function'),('computer-label',0x691BB4,len(b'    ComputerPlayerOn %d\n\0'),'native diagnostic label')]
 for r in json.loads((ROOT/'phase62-old-structure-rtti-leads.json').read_text()):
  defs.extend([(r['name']+'-vtable',r['vtable']-4,36,'RTTI locator pointer and first8 virtualmethods'),(r['name']+'-locator',r['locator'],20,'complete object locator'),(r['name']+'-type',r['type_descriptor'],8+len(('.?AV'+r['name']+'@@\0').encode()),'native type descriptor')])
 for label,a,n,scope in defs:
  key='phase62-final-data-'+label;blocks.append(dict(program='DKII-local13.exe',exe=str(exe),address=a,length=n,memory=key+'-memory.txt',label=label,scope=scope));qs.append(dict(id=key+'-memory',tool='read_memory',args=dict(program='DKII-local13.exe',address=hex(a),length=n),print_limit=10))
 (ROOT/'phase62-primary-manifest.json').write_text(json.dumps(dict(functions=rows,blocks=blocks),indent=2));(ROOT/'phase62-primary-queries.json').write_text(json.dumps(qs,indent=2));print('Fullfunctions',len(rows),'explicitdata/fragments',len(blocks))
if __name__=='__main__':main()
