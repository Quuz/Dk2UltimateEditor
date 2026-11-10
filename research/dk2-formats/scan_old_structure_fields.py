"""Old executable displacement leads only; no pointer identity or absence proof.

Linear decoding of PE .text may miss code after data or produce false code.
Only memory operands overlapping nominated fields are retained; every lead
needs Ghidra ownership, instruction-boundary and structure-type verification.
"""
from pathlib import Path
import json,hashlib
from capstone import Cs,CS_ARCH_X86,CS_MODE_32,CS_AC_READ
from capstone.x86 import X86_OP_MEM
from compare_rc3_converters import code_region
ROOT=Path(__file__).resolve().parent
FIELDS=[('TrapUnknownRuntime',0x98,2),('ObjectUnknownRuntime',0x106,2),('TrapIntermediate',0x2AC,2),('ObjectRaw',0x34C,2)]

def main():
 exe=ROOT/'source-binaries/DKII-local13.exe';data=exe.read_bytes();base,code=code_region(data);md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;md.skipdata=True;rows=[];count=0
 for i in md.disasm(code,base):
  if not i.id:continue
  count+=1
  for o in i.operands:
   if o.type!=X86_OP_MEM or not o.mem.base or o.mem.index or not o.access&CS_AC_READ or i.mnemonic in ['lea','call','jmp']:continue
   # Byte accesses are included for larger fields; purpose and type remain open.
   names=[name for name,off,n in FIELDS if o.mem.disp<off+n and o.mem.disp+o.size>off]
   if names:rows.append(dict(address=hex(i.address),hex=i.bytes.hex(),text=i.mnemonic+' '+i.op_str,base=i.reg_name(o.mem.base),displacement=o.mem.disp,bytes=o.size,targets=names))
 out=dict(scope=__doc__,exe=str(exe),sha256=hashlib.sha256(data).hexdigest(),decoded_instructions=count,candidates=rows)
 (ROOT/'phase61-old-structure-field-leads.json').write_text(json.dumps(out,indent=2),encoding='utf8')
 narrow=[r for r in rows if r['bytes']==2]
 print('decoded',count,'all',len(rows),'WORD',len(narrow))
 for r in narrow:print(r['address'],r['targets'],r['text'])
 for r in rows:
  if 'TrapIntermediate' in r['targets']:print('mid',r['address'],r['text'])
 qs=[dict(id='phase61-load-old',tool='load_program',args=dict(file=str(exe)),print_limit=180)]
 for r in narrow:
  a=int(r['address'],16);qs.append(dict(id=f'phase61-old-owner-{a:x}',tool='get_function_by_address',args=dict(program='DKII-local13.exe',address=hex(a)),print_limit=80))
 (ROOT/'phase61-old-owner-queries.json').write_text(json.dumps(qs,indent=2))
if __name__=='__main__':main()
