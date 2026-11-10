"""Full native spans and original PE data for portal consumer evidence."""
from pathlib import Path
import json,re
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent;EXE=ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe'
def main():
    prior=json.loads((ROOT/'phase90-primary-manifest.json').read_text());mapping={r['address']:r['assembly'] for r in prior['functions']}
    for p in sorted((ROOT/'ghidra').glob('phase91-portal-*.txt')):
        if not p.name.endswith(('-assembly.txt','-disassemble_function.txt')):continue
        lines=p.read_text().splitlines()
        if lines and re.match('^[0-9a-fA-F]{8}:',lines[0]):mapping[int(lines[0][:8],16)]=p.name
    for a,n in [(0x459c20,'readiness-DX-459c20-assembly.txt'),(0x494650,'phase85-DX-494650-assembly.txt'),
                (0x4950c0,'phase91-animation-init-disassemble_function.txt'),(0x494860,'phase86-DX-494860-assembly.txt'),
                (0x4790a0,'phase90-category-4790a0-disassemble_function.txt')]:mapping[a]=n
    md=Cs(CS_ARCH_X86,CS_MODE_32);data=EXE.read_bytes();q=[];functions=[]
    for a,n in sorted(mapping.items()):
        aa=[int(x[:8],16) for x in (ROOT/'ghidra'/n).read_text().splitlines() if re.match('^[0-9a-fA-F]{8}:',x)];last=next(md.disasm(pe_read(data,aa[-1],15),aa[-1]));assert aa[0]==a
        length=last.address+last.size-a;key=f'phase91-primary-{a:x}'
        functions.append(dict(exe=str(EXE),address=a,length=length,assembly=n,memory=key+'.txt',scope='Complete stored native span; includes unexecuted branches and is not a branch-coverage claim'))
        if not (ROOT/'ghidra'/(key+'.txt')).exists():q.append(dict(id=key,tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(a),length=length),print_limit=5))
    blocks=[]
    for old in prior['blocks']:
        a=old['address'];key=f'phase91-data-{old["label"]}';blocks.append(dict(old,memory=key+'.txt'))
        if not (ROOT/'ghidra'/(key+'.txt')).exists():q.append(dict(id=key,tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(a),length=old['length']),print_limit=5))
    q.append(dict(id='phase91-save',tool='save_program',args=dict(program='DKII-DX.exe'),print_limit=5))
    (ROOT/'phase91-primary-manifest.json').write_text(json.dumps(dict(functions=functions,blocks=blocks),indent=2),encoding='utf8')
    (ROOT/'phase91-primary-queries.json').write_text(json.dumps(q,indent=2),encoding='utf8');print(len(functions),'functions',len(blocks),'blocks',len(q),'queries')
if __name__=='__main__':main()
