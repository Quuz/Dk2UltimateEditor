"""Collect room-owned species counter consumers without repeating old PE reads."""
from pathlib import Path
import json,re
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent
EXE=ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe'

def main():
    old=json.loads((ROOT/'phase93-primary-manifest.json').read_text())
    functions={r['address']:r for r in old['functions']};q=[]
    addresses=[0x4d8e60,0x4d93e0,0x4d9410,0x4d9570,0x47af20,0x47b120,0x47b920,0x47b970,0x47b4a0,0x483790,0x4839e0]
    for a in addresses:
        stem=f'phase94-owner-{a:x}'
        if not (ROOT/'ghidra'/(stem+'-disassemble_function.txt')).exists():
            for tool in ['create_function','disassemble_function','force_decompile']:
                args=dict(program='DKII-DX.exe',address=hex(a))
                if tool=='create_function':args['disassemble_first']=True
                q.append(dict(id=stem+'-'+tool,tool=tool,args=args,print_limit=5))
    if q:
        (ROOT/'phase94-primary-queries.json').write_text(json.dumps(q,indent=2),encoding='utf8')
        print('Need function evidence:',len(q));return
    md=Cs(CS_ARCH_X86,CS_MODE_32);data=EXE.read_bytes()
    for a in addresses:
        file=f'phase94-owner-{a:x}-disassemble_function.txt'
        aa=[int(x[:8],16) for x in (ROOT/'ghidra'/file).read_text().splitlines() if re.match('^[0-9a-fA-F]{8}:',x)]
        assert aa[0]==a
        last=next(md.disasm(pe_read(data,aa[-1],15),aa[-1]));n=last.address+last.size-a
        key=f'phase94-primary-{a:x}'
        functions[a]=dict(exe=str(EXE),address=a,length=n,assembly=file,memory=key+'.txt',scope='Complete native span; unexecuted branches included, not a coverage claim')
        if not (ROOT/'ghidra'/(key+'.txt')).exists():q.append(dict(id=key,tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(a),length=n),print_limit=5))
    blocks=list(old['blocks'])
    source_length=len(pe_read(data,0x6a1904,220).split(b'\0')[0])+1
    for a,n,label in [(0x4d9274,180,'old-creature-counter-switch'),(0x4d9328,180,'new-creature-counter-switch'),(0x6a1904,source_length,'crypt-cpp-source-path')]:
        key='phase94-data-'+label
        blocks.append(dict(exe=str(EXE),address=a,length=n,label=label,memory=key+'.txt'))
        if not (ROOT/'ghidra'/(key+'.txt')).exists():q.append(dict(id=key,tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(a),length=n),print_limit=5))
    (ROOT/'phase94-primary-manifest.json').write_text(json.dumps(dict(functions=[functions[a] for a in sorted(functions)],blocks=blocks),indent=2),encoding='utf8')
    q.append(dict(id='phase94-save',tool='save_program',args=dict(program='DKII-DX.exe'),print_limit=5))
    (ROOT/'phase94-primary-queries.json').write_text(json.dumps(q,indent=2),encoding='utf8')
    print(len(functions),'spans',len(blocks),'blocks;',len(q),'new queries')

if __name__=='__main__':main()
