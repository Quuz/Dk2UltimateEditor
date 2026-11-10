"""Final complete native function reads after Keeper cast emulations."""
from pathlib import Path
import json,re
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent
def main():
    exe=ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe';md=Cs(CS_ARCH_X86,CS_MODE_32);rows=[];qs=[]
    files=[f'phase53-game-{a:x}-disassemble_function.txt' for a in [0x4BBF90,0x4BC290,0x4B7CD0]]+['phase45-getter-50dd60-disassemble_function.txt','phase44-game-4f7ce0-disassemble_function.txt']
    for file in files:
        aa=[int(l[:8],16) for l in (ROOT/'ghidra'/file).read_text().splitlines() if re.match(r'^[0-9a-f]{8}:',l)];a=aa[0];last=aa[-1];ins=next(md.disasm(pe_read(exe.read_bytes(),last,15),last));n=last+ins.size-a;key=f'phase53-final-game-{a:x}'
        rows.append(dict(program='DKII-DX.exe',exe=str(exe),address=a,length=n,before=file,after=key+'-assembly.txt',memory=key+'-memory.txt'))
        qs.extend([dict(id=key+'-assembly',tool='disassemble_function',args=dict(program='DKII-DX.exe',address=hex(a)),print_limit=10),dict(id=key+'-memory',tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(a),length=n),print_limit=10)])
    (ROOT/'phase53-primary-manifest.json').write_text(json.dumps(rows,indent=2),encoding='utf8');(ROOT/'phase53-primary-queries.json').write_text(json.dumps(qs,indent=2),encoding='utf8');print('Primary functions',len(rows))
if __name__=='__main__':main()
