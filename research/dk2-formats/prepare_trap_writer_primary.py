"""Attest complete editor bodies after fullwriter/DDX/commit emulations."""
from pathlib import Path
import json
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import pe_read
from native_trap_writer import PROFILES
ROOT=Path(__file__).resolve().parent

def main():
 md=Cs(CS_ARCH_X86,CS_MODE_32);rows=[];blocks=[];qs=[]
 for label,f in PROFILES.items():
  pg=f['program'];tag=label.lower();exe=ROOT/'source-binaries/DK2-Editor-rc3.exe' if label=='RC3' else ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/DK2 Editor pro.exe';raw=exe.read_bytes()
  files=[f'phase60-pro-{a:x}-disassemble_function.txt' for a in [0x470740,0x471930,0x471E00,0x4A1770,0x4A8F40]] if label=='Pro' else [f'phase60-rc3-{n}-disassemble_function.txt' for n in ['ddx','fill','commit','model','writer']]
  helpers=[0x426000,0x425EA0,0x425EB0,0x41CE60,0x4AB3B0,0x4ACDA8] if label=='Pro' else [0x430470,0x430310,0x430320,0x426210,0x4B0BC0,0x4B9850]
  files += [f'phase60-{tag}-{a:x}-disassemble_function.txt' for a in helpers]
  for file in files:
   aa=[int(l[:8],16) for l in (ROOT/'ghidra'/file).read_text().splitlines() if len(l)>9 and l[8]==':'];a=aa[0];last=aa[-1];ins=next(md.disasm(pe_read(raw,last,15),last));n=last+ins.size-a;key=f'phase60-final-{tag}-{a:x}'
   rows.append(dict(program=pg,exe=str(exe),address=a,length=n,before=file,after=key+'-assembly.txt',memory=key+'-memory.txt'))
   qs.extend([dict(id=key+'-assembly',tool='disassemble_function',args=dict(program=pg,address=hex(a)),print_limit=10),dict(id=key+'-memory',tool='read_memory',args=dict(program=pg,address=hex(a),length=n),print_limit=10)])
  a=0x50AC58 if label=='Pro' else 0x51BCC0;n=8;key=f'phase60-final-{tag}-scale';blocks.append(dict(program=pg,exe=str(exe),address=a,length=n,memory=key+'-memory.txt'));qs.append(dict(id=key+'-memory',tool='read_memory',args=dict(program=pg,address=hex(a),length=n),print_limit=10))
 (ROOT/'phase60-primary-manifest.json').write_text(json.dumps(dict(functions=rows,blocks=blocks),indent=2));(ROOT/'phase60-primary-queries.json').write_text(json.dumps(qs,indent=2));print('Complete bodies',len(rows),'data',len(blocks))
if __name__=='__main__':main()
