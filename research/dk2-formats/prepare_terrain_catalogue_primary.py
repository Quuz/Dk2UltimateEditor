"""Attest complete native Terrain bodies after the emulations."""
from pathlib import Path
import json
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent

def main():
 exe=ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe';raw=exe.read_bytes();md=Cs(CS_ARCH_X86,CS_MODE_32);rows=[];qs=[]
 files=[f'phase58-game-{a:x}-disassemble_function.txt' for a in [0x56EB60,0x56ED20,0x56F090,0x4F6570,0x4F66E0,0x51EB30,0x51EB50,0x508DD0,0x504D60]]
 files+=['phase52-DKII-DX.exe-6361a0-disassemble_function.txt','phase55-game-51e0a0-disassemble_function.txt']
 for file in files:
  aa=[int(l[:8],16) for l in (ROOT/'ghidra'/file).read_text().splitlines() if len(l)>9 and l[8]==':'];a=aa[0];last=aa[-1];ins=next(md.disasm(pe_read(raw,last,15),last));n=last+ins.size-a;key=f'phase58-final-game-{a:x}'
  rows.append(dict(program='DKII-DX.exe',exe=str(exe),address=a,length=n,before=file,after=key+'-assembly.txt',memory=key+'-memory.txt'))
  qs.extend([dict(id=key+'-assembly',tool='disassemble_function',args=dict(program='DKII-DX.exe',address=hex(a)),print_limit=10),dict(id=key+'-memory',tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(a),length=n),print_limit=10)])
 (ROOT/'phase58-primary-manifest.json').write_text(json.dumps(rows,indent=2));(ROOT/'phase58-primary-queries.json').write_text(json.dumps(qs,indent=2));print('Complete native bodies:',len(rows))
if __name__=='__main__':main()
