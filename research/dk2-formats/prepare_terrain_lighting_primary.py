"""Capture whole native functions and verify live initialized PE bytes later."""
from pathlib import Path
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import pe_read
import json,re
ROOT=Path(__file__).resolve().parent

def main():
    names=[('editor', 'phase48-colour-get-420350-disassemble_function'), ('editor', 'phase48-colour-get-4203a0-disassemble_function'), ('editor', 'phase48-colour-get-4203f0-disassemble_function'), ('editor', 'phase48-colour-get-420470-disassemble_function'), ('editor', 'phase48-colour-get-420490-disassemble_function'), ('editor', 'phase48-colour-get-4204b0-disassemble_function'), ('editor', 'phase48-colour-420320-disassemble_function'), ('editor', 'phase48-colour-420370-disassemble_function'), ('editor', 'phase48-colour-4203c0-disassemble_function'), ('editor', 'phase48-colour-425640-disassemble_function'), ('editor', 'phase48-writer-4254c0-disassemble_function'), ('game', 'phase48-game-terrain-disassemble_function')]
    names += [('editor', f'phase48-ui-{a:x}-disassemble_function') for a in [0x46D3B0,0x46DEB0,0x46E410,0x49F5E0,0x4A7690]]
    tasks=[];manifest=[];seen=set();md=Cs(CS_ARCH_X86,CS_MODE_32)
    for who,name in names:
        p=ROOT/'ghidra'/(name+'.txt');rows=[int(s[:8],16) for s in p.read_text().splitlines() if re.match(r'^[0-9a-f]{8}:',s)];a=rows[0]
        if (who,a) in seen:continue
        seen.add((who,a));last=rows[-1];exe=ROOT.parent.parent/('Dungeon Keeper 2/DKII-DX.exe' if who=='game' else 'Dungeon Keeper 2/Data/editor/DK2 Editor pro.exe');data=exe.read_bytes();ins=next(md.disasm(pe_read(data,last,15),last));length=last+ins.size-a;program='DKII-DX.exe' if who=='game' else 'DK2 Editor pro.exe';key='phase48-final-'+who+'-'+hex(a)[2:]
        tasks.extend([dict(id=key+'-assembly',tool='disassemble_function',args=dict(program=program,address=hex(a)),print_limit=20),dict(id=key+'-memory',tool='read_memory',args=dict(program=program,address=hex(a),length=length),print_limit=20)])
        manifest.append(dict(program=program,exe=str(exe),address=a,length=length,before=p.name,after=key+'-assembly.txt',memory=key+'-memory.txt'))
    (ROOT/'phase48-primary-queries.json').write_text(json.dumps(tasks,indent=2),encoding='utf8');(ROOT/'phase48-primary-manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf8')
if __name__=='__main__':main()
