"""Capture scoped primary function bytes after memory-only emulations."""
from pathlib import Path
import json,re
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent
def main():
    rows=[]
    pro=ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/DK2 Editor pro.exe';rc3=ROOT/'source-binaries/DK2-Editor-rc3.exe'
    for a in [0x49F550,0x41CB70,0x4826F0,0x4AB210,0x47D5F0,0x47CF90]:
        prefix='phase49-pro-more' if a==0x41CB70 else 'phase49-pro';rows.append(('DK2 Editor pro.exe',pro,f'{prefix}-{a:x}-disassemble_function.txt'))
    for a in [0x4B0A20,0x488030,0x437790,0x437D50,0x437F80,0x427250,0x427270,0x427290,0x4272B0,0x4272D0,0x4272F0,0x427310,0x4B9850]:rows.append(('DK2-Editor-rc3.exe',rc3,f'phase49-rc3-{a:x}-disassemble_function.txt'))
    for r in json.loads((ROOT/'phase40-primary-manifest.json').read_text(encoding='utf8')):
        if r['address'] in [0x431220,0x42FD70,0x42FE20,0x42C730,0x5215F0,0x449A30]:rows.append((r['program'],Path(r['exe']),r['before']))
    md=Cs(CS_ARCH_X86,CS_MODE_32);manifest=[];qs=[]
    for program,exe,file in rows:
        lines=(ROOT/'ghidra'/file).read_text().splitlines();addresses=[int(l[:8],16) for l in lines if re.match(r'^[0-9a-f]{8}:',l)];start=addresses[0];last=addresses[-1];end=next(md.disasm(pe_read(exe.read_bytes(),last,15),last));n=last+end.size-start;key=f'phase49-final-{program.split(".")[0].replace(" ","-")}-{start:x}'
        qs.extend([dict(id=key+'-assembly',tool='disassemble_function',args=dict(program=program,address=hex(start)),print_limit=10),dict(id=key+'-memory',tool='read_memory',args=dict(program=program,address=hex(start),length=n),print_limit=10)]);manifest.append(dict(program=program,exe=str(exe),address=start,length=n,before=file,after=key+'-assembly.txt',memory=key+'-memory.txt'))
    assert len(manifest)==25,len(manifest)
    (ROOT/'phase49-primary-manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf8');(ROOT/'phase49-primary-queries.json').write_text(json.dumps(qs,indent=2),encoding='utf8')
if __name__=='__main__':main()
