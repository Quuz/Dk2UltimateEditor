"""Attest complete native RC3 functions for the job-overlap compatibility rule."""
from pathlib import Path
import json,re,sys
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent
ADDRESSES=[0x4199E0,0x419FF0,0x41B180,0x41B1F0,0x428BD0,0x428BE0,0x428C00,0x428C10,0x4AD720,0x4B0B30]
def main():
    q=[];rows=[];exe=ROOT/'source-binaries/DK2-Editor-rc3.exe';md=Cs(CS_ARCH_X86,CS_MODE_32)
    for a in ADDRESSES:
        if len(sys.argv)==1:
            for label,tool in [('create','create_function'),('assembly','disassemble_function')]:
                args=dict(program='DK2-Editor-rc3.exe',address=hex(a))
                if tool=='create_function':args['disassemble_first']=True
                q.append(dict(id=f'phase87-final-RC3-{a:x}-{label}',tool=tool,args=args,print_limit=5))
        else:
            assembly=f'phase87-final-RC3-{a:x}-assembly.txt';ls=(ROOT/'ghidra'/assembly).read_text().splitlines();aa=[int(s[:8],16) for s in ls if re.match(r'^[0-9a-fA-F]{8}:',s)];assert aa and aa[0]==a
            last=next(md.disasm(pe_read(exe.read_bytes(),aa[-1],15),aa[-1]));n=last.address+last.size-a;key=f'phase87-final-RC3-{a:x}-memory'
            rows.append(dict(program='DK2-Editor-rc3.exe',exe=str(exe),address=a,length=n,assembly=assembly,memory=key+'.txt',clock_adapter_only=a==0x4B0B30,scope='Whole function bytes,not an instruction coverage trace'))
            q.append(dict(id=key,tool='read_memory',args=dict(program='DK2-Editor-rc3.exe',address=hex(a),length=n),print_limit=5))
    if len(sys.argv)>1:(ROOT/'phase87-primary-manifest.json').write_text(json.dumps(rows,indent=2),encoding='utf8')
    filename='phase87-final-'+('code' if len(sys.argv)==1 else 'memory')+'-queries.json';(ROOT/filename).write_text(json.dumps(q,indent=2),encoding='utf8');print(filename,len(q),'queries')
if __name__=='__main__':main()
