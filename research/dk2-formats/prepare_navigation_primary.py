from pathlib import Path
import json,re
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent;EXE=ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe'

def main():
    old=json.loads((ROOT/'phase96-primary-manifest.json').read_text(encoding='utf8'));functions={r['address']:r for r in old['functions']};q=[]
    md=Cs(CS_ARCH_X86,CS_MODE_32);data=EXE.read_bytes()
    for a in [0x48d010,0x4d5a40,0x4d4f60,0x65fae0,0x4d4760,0x4d5c00]:
        file=f'phase97-motion-{a:x}-disassemble_function.txt';aa=[int(x[:8],16) for x in (ROOT/'ghidra'/file).read_text(encoding='utf8').splitlines() if re.match('^[0-9a-fA-F]{8}:',x)]
        assert aa[0]==a;last=next(md.disasm(pe_read(data,aa[-1],15),aa[-1]));n=last.address+last.size-a;key=f'phase97-primary-{a:x}'
        functions[a]=dict(exe=str(EXE),address=a,length=n,assembly=file,memory=key+'.txt',scope='Completeoriginalspan;static/unexecutedbranchesincluded,notcoverageclaim')
        if not (ROOT/'ghidra'/(key+'.txt')).exists():q.append(dict(id=key,tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(a),length=n),print_limit=5))
    blocks=list(old['blocks'])
    for a,n,label in [(0x65fb14,64,'integer-sqrt-seeds'),(0x6a178c,len(pe_read(data,0x6a178c,80).split(b'\0')[0])+1,'nav-max-speed-format')]:
        key='phase97-data-'+label;blocks.append(dict(exe=str(EXE),address=a,length=n,label=label,memory=key+'.txt'))
        if not (ROOT/'ghidra'/(key+'.txt')).exists():q.append(dict(id=key,tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(a),length=n),print_limit=5))
    (ROOT/'phase97-primary-manifest.json').write_text(json.dumps(dict(functions=[functions[a] for a in sorted(functions)],blocks=blocks),indent=2),encoding='utf8')
    q.append(dict(id='phase97-save',tool='save_program',args=dict(program='DKII-DX.exe'),print_limit=5))
    (ROOT/'phase97-primary-queries.json').write_text(json.dumps(q,indent=2),encoding='utf8');print(len(functions),'spans',len(blocks),'blocks;',len(q),'newqueries')

if __name__=='__main__':main()
