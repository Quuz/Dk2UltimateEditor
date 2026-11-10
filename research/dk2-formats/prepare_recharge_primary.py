"""Collect exact native recharge consumer, readiness/progress and raw loader."""
from pathlib import Path
import json,re
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent;EXE=ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe'

def main():
    old=json.loads((ROOT/'phase95-primary-manifest.json').read_text());functions={r['address']:r for r in old['functions']};q=[]
    addresses=[0x48c800,0x48c8b0,0x45cb00,0x43d750,0x441990,0x4363f0,0x4943f0,0x50dfa0,0x56a490,0x56a560,0x4f7860]
    for a in addresses:
        stem=f'phase96-stat31-{a:x}'
        if not (ROOT/'ghidra'/(stem+'-disassemble_function.txt')).exists():
            for tool in ['create_function','disassemble_function','force_decompile']:
                args=dict(program='DKII-DX.exe',address=hex(a))
                if tool=='create_function':args['disassemble_first']=True
                q.append(dict(id=stem+'-'+tool,tool=tool,args=args,print_limit=5))
    if q:
        (ROOT/'phase96-primary-queries.json').write_text(json.dumps(q,indent=2),encoding='utf8');print('Need functions:',len(q));return
    md=Cs(CS_ARCH_X86,CS_MODE_32);data=EXE.read_bytes()
    for a in addresses:
        file=f'phase96-stat31-{a:x}-disassemble_function.txt';aa=[int(x[:8],16) for x in (ROOT/'ghidra'/file).read_text().splitlines() if re.match('^[0-9a-fA-F]{8}:',x)]
        assert aa[0]==a;last=next(md.disasm(pe_read(data,aa[-1],15),aa[-1]));n=last.address+last.size-a;key=f'phase96-primary-{a:x}'
        functions[a]=dict(exe=str(EXE),address=a,length=n,assembly=file,memory=key+'.txt',scope='Whole native span; static includes unexecuted branches,not execution coverage')
        if not (ROOT/'ghidra'/(key+'.txt')).exists():q.append(dict(id=key,tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(a),length=n),print_limit=5))
    (ROOT/'phase96-primary-manifest.json').write_text(json.dumps(dict(functions=[functions[a] for a in sorted(functions)],blocks=old['blocks']),indent=2),encoding='utf8')
    q.append(dict(id='phase96-save',tool='save_program',args=dict(program='DKII-DX.exe'),print_limit=5))
    (ROOT/'phase96-primary-queries.json').write_text(json.dumps(q,indent=2),encoding='utf8');print(len(functions),'spans',len(old['blocks']),'blocks;',len(q),'new queries')

if __name__=='__main__':main()
