"""Prepare repeat disassembly and live-PE comparisons, without byte edits."""
from pathlib import Path
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
import json,re
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent

def main():
    names=[('editor','phase41-spell-4a9730-disassemble_function'),('editor','phase41-spell-41ce60-disassemble_function'),('editor','phase41-spell-41d6f0-disassemble_function'),('editor','phase41-spell-4ab320-disassemble_function'),('editor','phase41-spell-4ab3b0-disassemble_function'),('editor','phase41-DK2 Editor pro.exe-42a0d0-disassemble_function'),('editor','phase41-DK2 Editor pro.exe-41d760-disassemble_function'),('editor','phase41-DK2 Editor pro.exe-4acda8-disassemble_function'),('editor','phase41-model-disassemble_function'),('editor','phase41-strncpy-editor'),('game','phase41-DKII-DX.exe-56a560-disassemble_function'),('game','phase41-strncpy-game')]
    tasks=[];manifest=[];md=Cs(CS_ARCH_X86,CS_MODE_32)
    for who,name in names:
        p=ROOT/'ghidra'/(name+'.txt');rows=[int(s[:8],16) for s in p.read_text().splitlines() if re.match(r'^[0-9a-f]{8}:',s)];a=rows[0];last=rows[-1];exe=ROOT.parent.parent/('Dungeon Keeper 2/DKII-DX.exe' if who=='game' else 'Dungeon Keeper 2/Data/editor/DK2 Editor pro.exe');data=exe.read_bytes();ins=next(md.disasm(pe_read(data,last,15),last));length=last+ins.size-a;program='DKII-DX.exe' if who=='game' else 'DK2 Editor pro.exe';key='phase41-final-'+who+'-'+hex(a)[2:]
        tasks.extend([dict(id=key+'-assembly',tool='disassemble_function',args=dict(program=program,address=hex(a)),print_limit=40),dict(id=key+'-memory',tool='read_memory',args=dict(program=program,address=hex(a),length=length),print_limit=40)])
        manifest.append(dict(program=program,exe=str(exe),address=a,length=length,before=p.name,after=key+'-assembly.txt',memory=key+'-memory.txt',scope='Repeated saved assembly plus live initialized program bytes compared with original PE. strncpy initial snapshots were captured after first emulations; no earlier snapshot claimed.'))
    (ROOT/'phase41-primary-queries.json').write_text(json.dumps(tasks,indent=2),encoding='utf8');(ROOT/'phase41-primary-manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf8')
if __name__=='__main__':main()
