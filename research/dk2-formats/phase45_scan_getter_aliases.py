from pathlib import Path
import json,re,struct
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from capstone.x86 import X86_OP_MEM
from build_effect_semantics import pe_read
p=Path('../../Dungeon Keeper 2/DKII-DX.exe');b=p.read_bytes();md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True
mapping=Path('sources/DKII_EXE_v170.sgmap').read_text(encoding='utf8');rows=[]
for a,name,size in re.findall(r'^global: va=([0-9A-F]+),name=([^,\n]+),size=(\d+)',mapping,re.M):
 a=int(a,16);size=int(size)
 if not 0x401000<=a<0x640000:continue
 try:ins=list(md.disasm(pe_read(b,a,size),a))
 except (ValueError,AssertionError):continue
 for i,x in enumerate(ins):
  if x.mnemonic=='call' and (x.op_str=='0x50dd60' or any(o.type==X86_OP_MEM and o.mem.disp==0x110 for o in x.operands)):
   rows.append(dict(function=hex(a),name=name,call=hex(x.address),instructions=[f'{j.address:08x}: {j.mnemonic} {j.op_str}' for j in ins[max(0,i-5):i+16]]))
Path('phase45-keeper-getter-alias-leads.json').write_text(json.dumps(dict(scope='Candidate vcall110 may be unrelated type; verify ECX identity, vtable and def-use before inference',rows=rows),indent=2))
print('Alias leads',len(rows))
for r in rows:print(r['function'],r['name'],'\n','\n'.join(r['instructions'][5:12]))
