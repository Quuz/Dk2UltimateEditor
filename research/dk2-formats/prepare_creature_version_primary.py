"""Capture whole native functions and verify live initialized PE bytes later."""
from pathlib import Path
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import pe_read
import json,re
ROOT=Path(__file__).resolve().parent

def main():
    names=[('old', 'phase47-primary-DKII-local13.exe-565630-disassemble_function'), ('old', 'phase47-primary-DKII-local13.exe-565730-disassemble_function'), ('old', 'phase47-primary-DKII-local13.exe-565840-disassemble_function'), ('old', 'phase47-primary-DKII-local13.exe-488270-disassemble_function'), ('game', 'phase47-primary-DKII-DX.exe-56f330-disassemble_function'), ('game', 'phase47-primary-DKII-DX.exe-56f430-disassemble_function'), ('game', 'phase47-primary-DKII-DX.exe-56f540-disassemble_function'), ('game', 'phase47-primary-DKII-DX.exe-490240-disassemble_function')]
    tasks=[];manifest=[];seen=set();md=Cs(CS_ARCH_X86,CS_MODE_32)
    for who,name in names:
        p=ROOT/'ghidra'/(name+'.txt');rows=[int(s[:8],16) for s in p.read_text().splitlines() if re.match(r'^[0-9a-f]{8}:',s)];a=rows[0]
        if (who,a) in seen:continue
        seen.add((who,a));last=rows[-1];exe=(ROOT if who=='old' else ROOT.parent.parent)/('source-binaries/DKII-local13.exe' if who=='old' else 'Dungeon Keeper 2/DKII-DX.exe');data=exe.read_bytes();ins=next(md.disasm(pe_read(data,last,15),last));length=last+ins.size-a;program='DKII-local13.exe' if who=='old' else 'DKII-DX.exe';key='phase47-final-'+who+'-'+hex(a)[2:]
        tasks.extend([dict(id=key+'-assembly',tool='disassemble_function',args=dict(program=program,address=hex(a)),print_limit=20),dict(id=key+'-memory',tool='read_memory',args=dict(program=program,address=hex(a),length=length),print_limit=20)])
        manifest.append(dict(program=program,exe=str(exe),address=a,length=length,before=p.name,after=key+'-assembly.txt',memory=key+'-memory.txt'))
    (ROOT/'phase47-primary-queries.json').write_text(json.dumps(tasks,indent=2),encoding='utf8');(ROOT/'phase47-primary-manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf8')
if __name__=='__main__':main()
