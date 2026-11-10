from pathlib import Path
import json,re
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent;EXE=ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe'

def main():
    old=json.loads((ROOT/'phase98-primary-manifest.json').read_text(encoding='utf8'));functions={r['address']:r for r in old['functions']};q=[]
    md=Cs(CS_ARCH_X86,CS_MODE_32);data=EXE.read_bytes()
    for p in sorted((ROOT/'ghidra').glob('phase99-*-disassemble_function.txt')):
        aa=[int(x[:8],16) for x in p.read_text(encoding='utf8').splitlines() if re.match('^[0-9a-fA-F]{8}:',x)]
        assert aa;a=aa[0];last=next(md.disasm(pe_read(data,aa[-1],15),aa[-1]));n=last.address+last.size-a;key=f'phase99-primary-{a:x}'
        functions[a]=dict(exe=str(EXE),address=a,length=n,assembly=p.name,memory=key+'.txt',scope='Wholeoriginalspan;static/unexecutedbranchesincluded,notcoverageclaim')
        if not (ROOT/'ghidra'/(key+'.txt')).exists():q.append(dict(id=key,tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(a),length=n),print_limit=5))
    blocks=list(old['blocks']);key='phase99-data-neighbour-directions';blocks.append(dict(exe=str(EXE),address=0x6bc350,length=32,label='original-CMap-neighbour-directions',memory=key+'.txt'))
    if not (ROOT/'ghidra'/(key+'.txt')).exists():q.append(dict(id=key,tool='read_memory',args=dict(program='DKII-DX.exe',address='0x6bc350',length=32),print_limit=5))
    (ROOT/'phase99-primary-manifest.json').write_text(json.dumps(dict(functions=[functions[a] for a in sorted(functions)],blocks=blocks),indent=2),encoding='utf8')
    q.append(dict(id='phase99-save',tool='save_program',args=dict(program='DKII-DX.exe'),print_limit=5));(ROOT/'phase99-primary-queries.json').write_text(json.dumps(q,indent=2),encoding='utf8')
    print(len(functions),'spans',len(blocks),'blocks;',len(q),'new queries')

if __name__=='__main__':main()
