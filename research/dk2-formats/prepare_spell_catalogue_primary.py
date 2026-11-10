"""Complete native parser/callback/loader and registration evidence for phase55."""
from pathlib import Path
import json,re
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent
def main():
    exe=ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe';raw=exe.read_bytes();md=Cs(CS_ARCH_X86,CS_MODE_32);rows=[];qs=[]
    files=[f'phase55-game-{a:x}-disassemble_function.txt' for a in [0x56A6D0,0x51FDE0,0x51FE00,0x50DFA0,0x51E0A0,0x541AA0,0x571040,0x570B60]]
    files+=['phase42-DKII-DX.exe-56a490-disassemble_function.txt','phase41-DKII-DX.exe-56a560-disassemble_function.txt','phase42-4f7860-disassemble_function.txt','phase52-DKII-DX.exe-6361a0-disassemble_function.txt']
    for file in files:
        aa=[int(l[:8],16) for l in (ROOT/'ghidra'/file).read_text().splitlines() if re.match(r'^[0-9a-f]{8}:',l)];a=aa[0];last=aa[-1];ins=next(md.disasm(pe_read(raw,last,15),last));n=last+ins.size-a;key=f'phase55-final-game-{a:x}'
        rows.append(dict(program='DKII-DX.exe',exe=str(exe),address=a,length=n,before=file,after=key+'-assembly.txt',memory=key+'-memory.txt'))
        qs.extend([dict(id=key+'-assembly',tool='disassemble_function',args=dict(program='DKII-DX.exe',address=hex(a)),print_limit=10),dict(id=key+'-memory',tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(a),length=n),print_limit=10)])
    (ROOT/'phase55-primary-manifest.json').write_text(json.dumps(rows,indent=2));(ROOT/'phase55-primary-queries.json').write_text(json.dumps(qs,indent=2));print('Complete native bodies:',len(rows))
if __name__=='__main__':main()
