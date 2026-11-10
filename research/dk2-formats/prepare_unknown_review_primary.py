from pathlib import Path
import json,re
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent;EXE=ROOT/'source-binaries/DKII-local13.exe'
def main():
    review=json.loads((ROOT/'phase100-object-boundary-review.json').read_text(encoding='utf8'));md=Cs(CS_ARCH_X86,CS_MODE_32);data=EXE.read_bytes();rows=[];blocks=[];q=[]
    for a in sorted({r['function'] for r in review['reviews']}):
        file=f'phase100-old-context-{a:x}-disassemble_function.txt';aa=[int(x[:8],16) for x in (ROOT/'ghidra'/file).read_text(encoding='utf8').splitlines() if re.match('^[0-9a-fA-F]{8}:',x)]
        assert aa[0]==a;last=next(md.disasm(pe_read(data,aa[-1],15),aa[-1]));n=last.address+last.size-a;key=f'phase100-primary-old-{a:x}'
        rows.append(dict(program='DKII-local13.exe',exe=str(EXE),address=a,length=n,assembly=file,memory=key+'.txt',scope='Originalwholecodeextent;4979B0 includesseparatelyrecoveredfive switcharms;no nativeexecutioncoverage.'))
        q.append(dict(id=key,tool='read_memory',args=dict(program='DKII-local13.exe',address=hex(a),length=n),print_limit=5))
    for a,n,label in [(0x497e58,20,'originalfiveway-switch-table')]:
        key=f'phase100-data-{a:x}';blocks.append(dict(program='DKII-local13.exe',exe=str(EXE),address=a,length=n,label=label,memory=key+'.txt'))
        q.append(dict(id=key,tool='read_memory',args=dict(program='DKII-local13.exe',address=hex(a),length=n),print_limit=5))
    q.append(dict(id='phase100-save-old-final',tool='save_program',args=dict(program='DKII-local13.exe'),print_limit=10))
    (ROOT/'phase100-primary-manifest.json').write_text(json.dumps(dict(functions=rows,blocks=blocks),indent=2),encoding='utf8')
    (ROOT/'phase100-primary-queries.json').write_text(json.dumps(q,indent=2),encoding='utf8');print(len(rows),'fullspans',len(blocks),'table')
if __name__=='__main__':main()
