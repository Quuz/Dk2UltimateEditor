"""Capture post-emulation native code and distinguish executed/static evidence."""
from pathlib import Path
import json,re,sys
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent
EXECUTED=[0x4352E0,0x554B30,0x435E70,0x4468E0,0x4B3610,0x41F900,
 0x440A70,0x4409A0,0x4412C0,0x440EE0,0x435F20,0x446C70,0x4435E0,
 0x5558D0,0x434E50,0x40CFD0,0x4363E0,0x40D640,0x4B37D0,0x4B41E0]
STATIC=[0x43AAD0,0x4366A0]

def main():
    q=[]
    if len(sys.argv)==1:
        for a in EXECUTED+STATIC:
            for label,tool in [('create','create_function'),('assembly','disassemble_function')]:
                args=dict(program='DKII-DX.exe',address=hex(a))
                if tool=='create_function':args['disassemble_first']=True
                q.append(dict(id=f'phase81-final-DX-{a:x}-{label}',tool=tool,args=args,print_limit=5))
        (ROOT/'phase81-final-code-queries.json').write_text(json.dumps(q,indent=2),encoding='utf8');return
    exe=ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe';data=exe.read_bytes();md=Cs(CS_ARCH_X86,CS_MODE_32);functions=[];blocks=[]
    for a in EXECUTED+STATIC:
        assembly=f'phase81-final-DX-{a:x}-assembly.txt'
        addresses=[int(line[:8],16) for line in (ROOT/'ghidra'/assembly).read_text().splitlines() if re.match(r'^[0-9a-fA-F]{8}:',line)]
        assert addresses and addresses[0]==a
        last=next(md.disasm(pe_read(data,addresses[-1],15),addresses[-1]))
        n=last.address+last.size-a;key=f'phase81-final-DX-{a:x}-memory'
        functions.append(dict(program='DKII-DX.exe',exe=str(exe),address=a,length=n,assembly=assembly,memory=key+'.txt',executed=a in EXECUTED,
          scope='Complete function span; only fixture-selected paths executed.' if a in EXECUTED else 'Static provenance/loader evidence only; not executed by phase81.'))
        q.append(dict(id=key,tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(a),length=n),print_limit=5))
    for a,n,label in [(0x66CC94+0x80,4,'CBridge visibility forwarder'),(0x66CC94+0x158,8,'CBridge EffectElement exists/get'),
                       (0x66CBDC,0x20,'Original EffectElement vtable prefix')]:
        key=f'phase81-final-data-{a:x}'
        blocks.append(dict(program='DKII-DX.exe',exe=str(exe),address=a,length=n,memory=key+'.txt',label=label))
        q.append(dict(id=key,tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(a),length=n),print_limit=5))
    (ROOT/'phase81-primary-manifest.json').write_text(json.dumps(dict(functions=functions,blocks=blocks),indent=2),encoding='utf8')
    (ROOT/'phase81-final-memory-queries.json').write_text(json.dumps(q,indent=2),encoding='utf8')
    print(len(EXECUTED),'executed-path function spans;',len(STATIC),'static;',len(blocks),'file-backed vtable ranges')

if __name__=='__main__':main()
