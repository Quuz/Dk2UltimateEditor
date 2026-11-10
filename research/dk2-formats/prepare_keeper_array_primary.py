"""Re-read actual code and original World slots after keeper-array execution."""
from pathlib import Path
import json,re,sys
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent
EXECUTED=[0x4F7C70,0x504D60,0x56B990,0x56BAB0,0x6361A0,0x51FCD0,0x4F7CE0,0x50DD60,0x50DC80,0x50DCA0,0x50DD00,0x50DDD0,0x50DE00]
STATIC=[0x50DE50,0x511680,0x511760,0x51E2C0,0x55C020,0x55C700,0x55C7B0]
def main():
    q=[]
    if len(sys.argv)==1:
        for a in EXECUTED+STATIC:
            for label,tool in [('create','create_function'),('assembly','disassemble_function')]:
                args=dict(program='DKII-DX.exe',address=hex(a))
                if tool=='create_function':args['disassemble_first']=True
                q.append(dict(id=f'phase83-final-DX-{a:x}-{label}',tool=tool,args=args,print_limit=5))
        (ROOT/'phase83-final-code-queries.json').write_text(json.dumps(q,indent=2),encoding='utf8');return
    exe=ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe';data=exe.read_bytes();md=Cs(CS_ARCH_X86,CS_MODE_32);functions=[]
    for a in EXECUTED+STATIC:
        assembly=f'phase83-final-DX-{a:x}-assembly.txt';aa=[int(s[:8],16) for s in (ROOT/'ghidra'/assembly).read_text().splitlines() if re.match(r'^[0-9a-fA-F]{8}:',s)]
        assert aa and aa[0]==a;last=next(md.disasm(pe_read(data,aa[-1],15),aa[-1]));n=last.address+last.size-a;key=f'phase83-final-DX-{a:x}-memory'
        functions.append(dict(program='DKII-DX.exe',exe=str(exe),address=a,length=n,assembly=assembly,memory=key+'.txt',executed_path=a in EXECUTED))
        q.append(dict(id=key,tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(a),length=n),print_limit=5))
    a=0x66E3EC+0xFC;n=40;key=f'phase83-final-data-{a:x}';block=dict(program='DKII-DX.exe',exe=str(exe),address=a,length=n,memory=key+'.txt',label='Original World accessor vslots FC..120 inclusive')
    q.append(dict(id=key,tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(a),length=n),print_limit=5))
    (ROOT/'phase83-primary-manifest.json').write_text(json.dumps(dict(functions=functions,blocks=[block]),indent=2),encoding='utf8')
    (ROOT/'phase83-final-memory-queries.json').write_text(json.dumps(q,indent=2),encoding='utf8');print(len(functions),'code spans,1originalWorld-vtable range')
if __name__=='__main__':main()
