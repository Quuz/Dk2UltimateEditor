from pathlib import Path
import json,re
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent;EXE=ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe'
def main():
    old=json.loads((ROOT/'phase91-primary-manifest.json').read_text());mapping={r['address']:r['assembly'] for r in old['functions']}
    for p in (ROOT/'ghidra').glob('phase92-possession-*-disassemble_function.txt'):
        lines=p.read_text().splitlines()
        if lines and re.match('^[0-9a-fA-F]{8}:',lines[0]):mapping[int(lines[0][:8],16)]=p.name
    q=[];functions=[];md=Cs(CS_ARCH_X86,CS_MODE_32);data=EXE.read_bytes()
    for a,file in sorted(mapping.items()):
        aa=[int(s[:8],16) for s in (ROOT/'ghidra'/file).read_text().splitlines() if re.match('^[0-9a-fA-F]{8}:',s)];last=next(md.disasm(pe_read(data,aa[-1],15),aa[-1]));assert aa[0]==a
        n=last.address+last.size-a;key=f'phase92-primary-{a:x}';functions.append(dict(exe=str(EXE),address=a,length=n,assembly=file,memory=key+'.txt',scope='Whole native span, including static unexecuted branches; not branch coverage'))
        if not (ROOT/'ghidra'/(key+'.txt')).exists():q.append(dict(id=key,tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(a),length=n),print_limit=5))
    blocks=[]
    for r in old['blocks']:
        key='phase92-data-'+r['label'];blocks.append(dict(r,memory=key+'.txt'))
        if not (ROOT/'ghidra'/(key+'.txt')).exists():q.append(dict(id=key,tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(r['address']),length=r['length']),print_limit=5))
    q.append(dict(id='phase92-save',tool='save_program',args=dict(program='DKII-DX.exe'),print_limit=5))
    (ROOT/'phase92-primary-manifest.json').write_text(json.dumps(dict(functions=functions,blocks=blocks),indent=2),encoding='utf8')
    (ROOT/'phase92-primary-queries.json').write_text(json.dumps(q,indent=2),encoding='utf8');print(len(functions),'native spans',len(blocks),'blocks')
if __name__=='__main__':main()
