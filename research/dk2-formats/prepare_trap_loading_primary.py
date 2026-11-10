"""Attest complete loading functions and native CEEffectElement identity data."""
from pathlib import Path
import json,re,struct
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent

def main():
 rows=[];blocks=[];qs=[];md=Cs(CS_ARCH_X86,CS_MODE_32)
 old=ROOT/'source-binaries/DKII-local13.exe';dx=ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe'
 entries=[('DKII-DX.exe',dx,f) for f in ['phase61-DKII-DX.exe-570b60-disassemble_function.txt','phase59-game-570d10-disassemble_function.txt','phase59-game-4aa440-disassemble_function.txt','phase52-DKII-DX.exe-6361a0-disassemble_function.txt','game-catalogue-traps-parser-assembly.txt']]
 entries += [('DKII-local13.exe',old,f) for f in ['phase61-DKII-local13.exe-566e60-disassemble_function.txt','phase61-old-567010-disassemble_function.txt','phase61-old-4a1440-disassemble_function.txt','phase52-DKII-local13.exe-627cd0-disassemble_function.txt','phase61-DKII-local13.exe-42f080-disassemble_function.txt','phase61-DKII-local13.exe-42f0f0-disassemble_function.txt']]
 for program,exe,file in entries:
  data=exe.read_bytes();aa=[int(l[:8],16) for l in (ROOT/'ghidra'/file).read_text().splitlines() if re.match(r'^[0-9a-fA-F]{8}:',l)];start=aa[0];last=aa[-1];end=next(md.disasm(pe_read(data,last,15),last));n=last+end.size-start;key=f'phase61-final-{program}-{start:x}'
  rows.append(dict(program=program,exe=str(exe),address=start,length=n,before=file,after=key+'-assembly.txt',memory=key+'-memory.txt'))
  qs.extend([dict(id=key+'-assembly',tool='disassemble_function',args=dict(program=program,address=hex(start)),print_limit=10),dict(id=key+'-memory',tool='read_memory',args=dict(program=program,address=hex(start),length=n),print_limit=10)])
 for program,exe,label,a,n in [('DKII-DX.exe',dx,'sound-prefix',0x68ED34,8),('DKII-local13.exe',old,'sound-prefix',0x67FCCC,8),('DKII-local13.exe',old,'effectelement-vtable',0x65DB98,28),('DKII-local13.exe',old,'effectelement-locator',0x66F750,20),('DKII-local13.exe',old,'effectelement-type',0x688138,29)]:
  key=f'phase61-final-{program}-{label}';blocks.append(dict(program=program,exe=str(exe),address=a,length=n,memory=key+'-memory.txt',label=label));qs.append(dict(id=key+'-memory',tool='read_memory',args=dict(program=program,address=hex(a),length=n),print_limit=10))
 (ROOT/'phase61-primary-manifest.json').write_text(json.dumps(dict(functions=rows,blocks=blocks),indent=2));(ROOT/'phase61-primary-queries.json').write_text(json.dumps(qs,indent=2));print(len(rows),'complete bodies',len(blocks),'data blocks')
if __name__=='__main__':main()
