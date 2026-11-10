"""Recapture all phase51 RC3 complete function bodies after emulation."""
from pathlib import Path
import json,re
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import pe_read
from compare_rc3_converters import PRO,RC3
ROOT=Path(__file__).resolve().parent
def main():
    rows=[('DK2-Editor-rc3.exe',RC3,p.name) for p in sorted((ROOT/'ghidra').glob('phase51-rc3-*-disassemble_function.txt'))]
    for r in json.loads((ROOT/'phase34-primary-manifest.json').read_text()):
        if r['address'] in [0x4323A0,0x432530,0x432850]:rows.append(('DK2 Editor pro.exe',PRO,r['before']))
    md=Cs(CS_ARCH_X86,CS_MODE_32);manifest=[];qs=[]
    for program,exe,file in rows:
        addresses=[int(l[:8],16) for l in (ROOT/'ghidra'/file).read_text().splitlines() if re.match(r'^[0-9a-f]{8}:',l)]
        a=addresses[0];last=addresses[-1];ins=next(md.disasm(pe_read(exe.read_bytes(),last,15),last));n=last+ins.size-a
        if program=='DK2-Editor-rc3.exe':assert a==int(file.split('-')[2],16),file
        key=f'phase51-final-{program.split(".")[0].replace(" ","-")}-{a:x}'
        manifest.append(dict(program=program,exe=str(exe),address=a,length=n,before=file,after=key+'-assembly.txt',memory=key+'-memory.txt'))
        qs.extend([dict(id=key+'-assembly',tool='disassemble_function',args=dict(program=program,address=hex(a)),print_limit=10),dict(id=key+'-memory',tool='read_memory',args=dict(program=program,address=hex(a),length=n),print_limit=10)])
    (ROOT/'phase51-primary-manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf8')
    (ROOT/'phase51-primary-queries.json').write_text(json.dumps(qs,indent=2),encoding='utf8');print('Primary function ranges',len(manifest))
if __name__=='__main__':main()
