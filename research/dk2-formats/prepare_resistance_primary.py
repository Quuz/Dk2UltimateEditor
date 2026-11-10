"""Original loading/combat and all twenty direct displacement-lead contexts."""
import json
import re
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import ROOT,EXE,pe_read


def main():
    known={}
    for name in ['phase85-primary-manifest.json','phase104-primary-manifest.json','phase105-primary-manifest.json','phase111-primary-manifest.json']:
        v=json.loads((ROOT/name).read_text(encoding='utf8'))
        for r in v['functions']:
            if r.get('program','DKII-DX.exe')=='DKII-DX.exe':known[r['address']]=r
    executed=[0x491DC0,0x568820,0x568E30,0x492320,0x569D70,0x569D90,0x569DB0,
              0x51DC70,0x51DC40,0x498E30,0x4B2CE0,0x494D00,0x509580,0x50E490,0x4430D0]
    static=[0x492230,0x51F3B0,0x55C020,0x55DF40,0x5A8980,0x5A8B20,0x64B43E,0x64B51E,0x65C120]
    new={a:f'phase113-resist-{a:x}-disassemble_function.txt' for a in [0x569D70,0x569D90,0x569DB0]}
    new.update({a:f'phase113-resist-other-{a:x}-disassemble_function.txt' for a in static if a!=0x51F3B0})
    data=EXE.read_bytes();md=Cs(CS_ARCH_X86,CS_MODE_32);q=[];rows=[]
    for a in sorted(executed+static):
        if a in new:
            assembly=new[a];aa=[int(s[:8],16) for s in (ROOT/'ghidra'/assembly).read_text(encoding='utf8').splitlines() if re.match(r'^[0-9a-fA-F]{8}:',s)]
            assert min(aa)==a;last=max(aa);tail=next(md.disasm(pe_read(data,last,15),last));size=tail.address+tail.size-a
        else:
            r=known[a];assembly=r.get('assembly',r.get('after',r.get('before')));size=r['length']
        key=f'phase113-primary-{a:x}';rows.append(dict(address=a,length=size,assembly=assembly,memory=key+'.txt',executed=a in executed))
        q.append(dict(id=key,tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(a),length=size),print_limit=4))
    blocks=[]
    for a,n,label in [(0x66D524,0x130,'original Creature vtable'),(0x670574+0x90,4,'original null effect provider')]:
        key=f'phase113-block-{a:x}';blocks.append(dict(address=a,length=n,label=label,memory=key+'.txt'))
        q.append(dict(id=key,tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(a),length=n),print_limit=4))
    (ROOT/'phase113-primary-manifest.json').write_text(json.dumps(dict(functions=rows,blocks=blocks),indent=2),encoding='utf8')
    (ROOT/'phase113-primary-queries.json').write_text(json.dumps(q,indent=2),encoding='utf8')
    print(len(rows),'whole ranges;',len(executed),'with executed paths;',len(blocks),'blocks')


if __name__=='__main__':main()
