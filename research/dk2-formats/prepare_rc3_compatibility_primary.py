"""Capture complete functions for RC3 catalogue comparison and Creature paths."""
from pathlib import Path
import json,re
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import pe_read
from compare_rc3_converters import PRO,RC3
ROOT=Path(__file__).resolve().parent
def main():
    rows=[];md=Cs(CS_ARCH_X86,CS_MODE_32)
    for r in json.loads((ROOT/'phase50-converter-match-candidates.json').read_text()):
        rows.append(('DK2 Editor pro.exe',PRO,r['pro_assembly']))
        if len(r['rc3_candidates'])==1:rows.append(('DK2-Editor-rc3.exe',RC3,f'phase50-rc3-converter-{r["family"]}-disassemble_function.txt'))
    for r in json.loads((ROOT/'phase50-helper-pair-candidates.json').read_text()):
        for who,prog,exe in [('pro','DK2 Editor pro.exe',PRO),('rc3','DK2-Editor-rc3.exe',RC3)]:rows.append((prog,exe,f'phase50-helper-{who}-{r[who][2:]}-disassemble_function.txt'))
    for a in [0x419FF0,0x4199E0,0x41B5E0,0x4AD720,0x429270,0x4292B0,0x4B0BC0,0x426210,0x4BA280,0x4A58F0]:rows.append(('DK2-Editor-rc3.exe',RC3,f'phase50-rc3-{a:x}-disassemble_function.txt'))
    for r in json.loads((ROOT/'phase43-primary-manifest.json').read_text()):
        if r['program']=='DK2 Editor pro.exe' and r['address'] in [0x4A7F00,0x41EB80]:rows.append((r['program'],Path(r['exe']),r['before']))
    qs=[];manifest=[];seen=set()
    for program,exe,file in rows:
        ls=(ROOT/'ghidra'/file).read_text().splitlines();addresses=[int(l[:8],16) for l in ls if re.match(r'^[0-9a-f]{8}:',l)];a=addresses[0];last=addresses[-1]
        if (program,a) in seen:continue
        seen.add((program,a));ins=next(md.disasm(pe_read(exe.read_bytes(),last,15),last));n=last+ins.size-a;key=f'phase50-final-{program.split(".")[0].replace(" ","-")}-{a:x}'
        manifest.append(dict(program=program,exe=str(exe),address=a,length=n,before=file,after=key+'-assembly.txt',memory=key+'-memory.txt'))
        qs.extend([dict(id=key+'-assembly',tool='disassemble_function',args=dict(program=program,address=hex(a)),print_limit=10),dict(id=key+'-memory',tool='read_memory',args=dict(program=program,address=hex(a),length=n),print_limit=10)])
    (ROOT/'phase50-primary-manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf8');(ROOT/'phase50-primary-queries.json').write_text(json.dumps(qs,indent=2),encoding='utf8');print('Primaryfunctionranges',len(manifest))
if __name__=='__main__':main()
