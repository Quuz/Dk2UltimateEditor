"""Attest complete native spans and untouched class tables used in phase90."""
from pathlib import Path
import json,re
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent;EXE=ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe'
def main():
    prior=json.loads((ROOT/'phase89-primary-manifest.json').read_text());mapping={r['address']:r['assembly'] for r in prior['functions']}
    for prefix in ['phase89-creation','phase89-sacrifice','phase90-creature','phase90-pool','phase90-owner','phase90-temple']:
        for p in sorted((ROOT/'ghidra').glob(prefix+'-*.txt')):
            if not any(p.name.endswith(s) for s in ['-assembly.txt','-disassemble_function.txt']):continue
            lines=p.read_text().splitlines()
            if lines and re.match('^[0-9a-fA-F]{8}:',lines[0]):mapping[int(lines[0][:8],16)]=p.name
    for a,n in [(0x47bbe0,'phase78-DX-47bbe0-assembly.txt')]:mapping[a]=n
    for a in [0x4872a0,0x50d4d0,0x50e3a0]:mapping[a]=f'phase89-object-{a:x}-assembly.txt'
    required=[0x476d30,0x441990,0x4363f0,0x4c6ae0,0x4c4600]
    q=[];functions=[];data=EXE.read_bytes();md=Cs(CS_ARCH_X86,CS_MODE_32)
    for a in required:
        p=ROOT/'ghidra'/f'phase90-primary-extra-{a:x}-assembly.txt'
        if p.exists():mapping[a]=p.name
        elif a not in mapping:
            for t,suffix in [('create_function','create'),('disassemble_function','assembly'),('force_decompile','decompile')]:
                args=dict(program='DKII-DX.exe',address=hex(a))
                if t=='create_function':args['disassemble_first']=True
                q.append(dict(id=f'phase90-primary-extra-{a:x}-{suffix}',tool=t,args=args,print_limit=5))
    if q:
        (ROOT/'phase90-primary-queries.json').write_text(json.dumps(q,indent=2),encoding='utf8');print('First pass',len(q));return
    for a,file in sorted(mapping.items()):
        aa=[int(s[:8],16) for s in (ROOT/'ghidra'/file).read_text().splitlines() if re.match('^[0-9a-fA-F]{8}:',s)]
        last=next(md.disasm(pe_read(data,aa[-1],15),aa[-1]));assert aa[0]==a;n=last.address+last.size-a;key=f'phase90-primary-{a:x}'
        functions.append(dict(exe=str(EXE),address=a,length=n,assembly=file,memory=key+'.txt',scope='Whole byte span; static includes unexecuted branches, not a trace/coverage claim'))
        if not (ROOT/'ghidra'/(key+'.txt')).exists():q.append(dict(id=key,tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(a),length=n),print_limit=5))
    blocks=[]
    for a,n,label in [(0x66e3ec,0x400,'World-vtable'),(0x66d520,0x104,'Creature-vtable-with-locator'),(0x66d5bc,0x20,'navigation-vtable'),
                      (0x66d404,0x20,'AI-context-vtable'),(0x66c1b8,4,'IsBadReadPtr-IAT'),(0x699688,0x800,'AI-category-initial-data')]:
        key='phase90-data-'+label;blocks.append(dict(exe=str(EXE),address=a,length=n,memory=key+'.txt',label=label))
        if not (ROOT/'ghidra'/(key+'.txt')).exists():q.append(dict(id=key,tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(a),length=n),print_limit=5))
    q.append(dict(id='phase90-save',tool='save_program',args=dict(program='DKII-DX.exe'),print_limit=5))
    (ROOT/'phase90-primary-manifest.json').write_text(json.dumps(dict(functions=functions,blocks=blocks),indent=2),encoding='utf8')
    (ROOT/'phase90-primary-queries.json').write_text(json.dumps(q,indent=2),encoding='utf8');print(len(functions),'whole functions;',len(blocks),'blocks;',len(q),'queries')
if __name__=='__main__':main()
