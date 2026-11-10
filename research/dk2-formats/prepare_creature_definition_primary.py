"""Attest original Creature ctor, raw conversion, full loader and array helpers."""
from pathlib import Path
import json,re,sys
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent
FUNCTIONS=[0x491DC0,0x492320,0x494650,0x568820,0x568E30,0x504D60,0x635EC0,0x6361A0,
 0x491D50,0x491D60,0x491D80,0x491D90,0x491DB0,0x56A120,0x56A140,0x56A0D0,0x56A0F0]

def main():
    q=[]
    if len(sys.argv)==1:
        for a in FUNCTIONS:
            for label,tool in [('create','create_function'),('assembly','disassemble_function')]:
                args=dict(program='DKII-DX.exe',address=hex(a))
                if tool=='create_function':args['disassemble_first']=True
                q.append(dict(id=f'phase85-final-DX-{a:x}-{label}',tool=tool,args=args,print_limit=5))
        (ROOT/'phase85-final-code-queries.json').write_text(json.dumps(q,indent=2),encoding='utf8');return
    exe=ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe';data=exe.read_bytes();md=Cs(CS_ARCH_X86,CS_MODE_32);functions=[];blocks=[]
    for a in FUNCTIONS:
        assembly=f'phase85-final-DX-{a:x}-assembly.txt'
        aa=[int(s[:8],16) for s in (ROOT/'ghidra'/assembly).read_text().splitlines() if re.match(r'^[0-9a-fA-F]{8}:',s)]
        assert aa and aa[0]==a
        last=next(md.disasm(pe_read(data,aa[-1],15),aa[-1]));n=last.address+last.size-a;key=f'phase85-final-DX-{a:x}-memory'
        functions.append(dict(program='DKII-DX.exe',exe=str(exe),address=a,length=n,assembly=assembly,memory=key+'.txt',scope='Whole function bytes;not an instruction-coverage trace'))
        q.append(dict(id=key,tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(a),length=n),print_limit=5))
    a=0x66E3EC;n=0x400;key=f'phase85-final-data-{a:x}'
    blocks.append(dict(program='DKII-DX.exe',exe=str(exe),address=a,length=n,memory=key+'.txt',label='Original World vtable;fixtures retain original lookup implementations'))
    q.append(dict(id=key,tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(a),length=n),print_limit=5))
    (ROOT/'phase85-primary-manifest.json').write_text(json.dumps(dict(functions=functions,blocks=blocks),indent=2),encoding='utf8')
    (ROOT/'phase85-final-memory-queries.json').write_text(json.dumps(q,indent=2),encoding='utf8')
    print(len(functions),'whole native code spans;',len(blocks),'original vtable range')
if __name__=='__main__':main()
