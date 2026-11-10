"""Prepare current native byte/disassembly comparisons for phase42."""
from pathlib import Path
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
import json,re
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent
def main():
    names=[('game','phase42-DKII-DX.exe-56a490-disassemble_function'),('editor','phase42-DK2 Editor pro.exe-42a000-disassemble_function'),('game','phase41-DKII-DX.exe-56a560-disassemble_function'),('editor','phase41-DK2 Editor pro.exe-42a0d0-disassemble_function'),('game','phase42-4f7860-disassemble_function'),('editor','phase41-model-disassemble_function'),('editor','phase41-spell-4a9730-disassemble_function'),('editor','phase42-442c40-disassemble_function'),('editor','phase42-442e80-disassemble_function'),('editor','phase42-443010-disassemble_function'),('editor','phase42-4439a0-disassemble_function'),('editor','phase42-443cf0-disassemble_function'),('editor','phase42-443c20-disassemble_function'),('editor','phase42-443c50-disassemble_function'),('editor','phase42-443370-disassemble_function'),('editor','phase42-443550-disassemble_function'),('editor','phase41-DK2 Editor pro.exe-41d760-disassemble_function'),('editor','phase41-spell-41d6f0-disassemble_function')]
    tasks=[];manifest=[];md=Cs(CS_ARCH_X86,CS_MODE_32)
    for who,name in names:
        p=ROOT/'ghidra'/(name+'.txt');rows=[int(s[:8],16) for s in p.read_text().splitlines() if re.match(r'^[0-9a-f]{8}:',s)];a=rows[0];last=rows[-1];exe=ROOT.parent.parent/('Dungeon Keeper 2/DKII-DX.exe' if who=='game' else 'Dungeon Keeper 2/Data/editor/DK2 Editor pro.exe');data=exe.read_bytes();ins=next(md.disasm(pe_read(data,last,15),last));length=last+ins.size-a;program='DKII-DX.exe' if who=='game' else 'DK2 Editor pro.exe';key='phase42-final-'+who+'-'+hex(a)[2:]
        tasks.extend([dict(id=key+'-assembly',tool='disassemble_function',args=dict(program=program,address=hex(a)),print_limit=30),dict(id=key+'-memory',tool='read_memory',args=dict(program=program,address=hex(a),length=length),print_limit=30)])
        manifest.append(dict(program=program,exe=str(exe),address=a,length=length,before=p.name,after=key+'-assembly.txt',memory=key+'-memory.txt'))
    (ROOT/'phase42-primary-queries.json').write_text(json.dumps(tasks,indent=2),encoding='utf8');(ROOT/'phase42-primary-manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf8')
if __name__=='__main__':main()
