"""Final complete function/RTTI reads for exact old/current crosschecks."""
from pathlib import Path
import json,re
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent
EXES={'DKII-DX.exe':ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe','DKII-local13.exe':ROOT/'source-binaries/DKII-local13.exe'}
def main():
    selected=[]
    for program,addresses in [('DKII-DX.exe',[0x4F7860,0x56A490,0x56A560,0x6361A0,0x4B0460,0x4B0550,0x51DC20,0x401200,0x51AA90]),('DKII-local13.exe',[0x5607D0,0x5608A0,0x627CD0,0x4A70A0,0x511940,0x4EB320,0x50E860,0x4AEA80,0x4AE070])]:
        for a in addresses:selected.append((program,f'phase52-{program}-{a:x}-disassemble_function.txt'))
    for a in [0x4EC440,0x4A6FB0,0x4A7680,0x4A7700,0x4A6F60]:selected.append(('DKII-local13.exe',f'phase52-old-{a:x}-disassemble_function.txt'))
    for a in [0x4B0B90]:selected.append(('DKII-DX.exe',f'phase52-game-{a:x}-disassemble_function.txt'))
    selected.extend([('DKII-DX.exe','phase33-setter-4b0b10-assembly.txt'),('DKII-DX.exe','phase33-AP-init-assembly.txt')])
    qs=[];manifest=[];md=Cs(CS_ARCH_X86,CS_MODE_32)
    for program,file in selected:
        aa=[int(l[:8],16) for l in (ROOT/'ghidra'/file).read_text().splitlines() if re.match(r'^[0-9a-f]{8}:',l)];a=aa[0];last=aa[-1];ins=next(md.disasm(pe_read(EXES[program].read_bytes(),last,15),last));n=last+ins.size-a;key=f'phase52-final-{program}-{a:x}'
        manifest.append(dict(program=program,exe=str(EXES[program]),address=a,length=n,before=file,after=key+'-assembly.txt',memory=key+'-memory.txt'))
        qs.extend([dict(id=key+'-assembly',tool='disassemble_function',args=dict(program=program,address=hex(a)),print_limit=10),dict(id=key+'-memory',tool='read_memory',args=dict(program=program,address=hex(a),length=n),print_limit=10)])
    labels=[]
    for r in json.loads((ROOT/'phase52-ap-rtti-vtables.json').read_text()):
        program='DKII-DX.exe' if r['profile']=='dx' else 'DKII-local13.exe'
        for key,a,n in [('type_descriptor',r['type_descriptor'],27),('complete_object_locator',r['complete_object_locator'],20),('vtable_with_locator_pointer',r['vtable']-4,88)]:
            name=f"phase52-label-{r['profile']}-{key}"
            labels.append(dict(program=program,exe=r['file'],address=a,length=n,kind=key,memory=name+'.txt'))
            qs.append(dict(id=name,tool='read_memory',args=dict(program=program,address=hex(a),length=n),print_limit=10))
    (ROOT/'phase52-primary-manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf8');(ROOT/'phase52-label-manifest.json').write_text(json.dumps(labels,indent=2),encoding='utf8');(ROOT/'phase52-primary-queries.json').write_text(json.dumps(qs,indent=2),encoding='utf8');print(len(manifest),'functions,',len(labels),'RTTI ranges')
if __name__=='__main__':main()
