"""Full-function live byte/disassembly manifest; no executable writes."""
from pathlib import Path
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import pe_read
import json,re
ROOT=Path(__file__).resolve().parent
def main():
    names=[]
    for p in sorted((ROOT/'ghidra').glob('phase43-helper-*.txt')):names.append(('editor',p.stem))
    for p in sorted((ROOT/'ghidra').glob('phase43-raw-callee-*.txt')):names.append(('editor',p.stem))
    names.extend([('editor','phase43-DK2 Editor pro.exe-'+hex(a)[2:]+'-disassemble_function') for a in [0x4A7F00,0x4287C0,0x4281B0,0x41D7E0,0x4AD490]])
    names.extend([('game','phase43-DKII-DX.exe-'+hex(a)[2:]+'-disassemble_function') for a in [0x568E30,0x568820]])
    names.extend([('game','phase43-parser-DKII-DX.exe-disassemble_function'),('editor','phase43-parser-DK2 Editor pro.exe-disassemble_function'),('editor','phase41-DK2 Editor pro.exe-4acda8-disassemble_function'),('editor','phase41-strncpy-editor'),('game','phase41-strncpy-game'),('editor','phase41-spell-4ab3b0-disassemble_function'),('editor','phase41-spell-4ab320-disassemble_function')])
    tasks=[];manifest=[];seen=set();md=Cs(CS_ARCH_X86,CS_MODE_32)
    for who,name in names:
        p=ROOT/'ghidra'/(name+'.txt');rows=[int(s[:8],16) for s in p.read_text().splitlines() if re.match(r'^[0-9a-f]{8}:',s)];a=rows[0]
        if (who,a) in seen:continue
        seen.add((who,a));last=rows[-1];exe=ROOT.parent.parent/('Dungeon Keeper 2/DKII-DX.exe' if who=='game' else 'Dungeon Keeper 2/Data/editor/DK2 Editor pro.exe');data=exe.read_bytes();ins=next(md.disasm(pe_read(data,last,15),last));length=last+ins.size-a;program='DKII-DX.exe' if who=='game' else 'DK2 Editor pro.exe';key='phase43-final-'+who+'-'+hex(a)[2:]
        tasks.extend([dict(id=key+'-assembly',tool='disassemble_function',args=dict(program=program,address=hex(a)),print_limit=20),dict(id=key+'-memory',tool='read_memory',args=dict(program=program,address=hex(a),length=length),print_limit=20)])
        manifest.append(dict(program=program,exe=str(exe),address=a,length=length,before=p.name,after=key+'-assembly.txt',memory=key+'-memory.txt'))
    (ROOT/'phase43-primary-queries.json').write_text(json.dumps(tasks,indent=2),encoding='utf8');(ROOT/'phase43-primary-manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf8')
if __name__=='__main__':main()
