"""Fresh PE ranges for enclosing CMap placement/ownership/mana and original import."""
import json
import re
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import ROOT,EXE,pe_read


def main():
    known={}
    for name in ['phase89-primary-manifest.json','phase99-primary-manifest.json','phase111-primary-manifest.json']:
        for r in json.loads((ROOT/name).read_text(encoding='utf8'))['functions']:known[r['address']]=r
    wanted=[0x49E600,0x56D4F0,0x56D620,0x49DD60,0x4B7A90,0x49B1C0,0x49EF60,0x49EF80,
            0x56EB60,0x56ED20,0x4F66E0,0x4559D0,0x455B90,0x4BE070,0x4C10E0,0x4C17A0,
            0x44FC40,0x4D1930,0x4C4600,0x4B2AD0,0x4537E0,0x50DB20,0x50DC10,0x50CF80]
    new={0x4C4600:'phase112-event-4c4600-disassemble_function.txt',
         0x4B2AD0:'phase112-map-4b2ad0-disassemble_function.txt',0x4537E0:'phase112-map-4537e0-disassemble_function.txt'}
    data=EXE.read_bytes();md=Cs(CS_ARCH_X86,CS_MODE_32);rows=[];q=[]
    for a in sorted(wanted):
        if a in new:
            assembly=new[a];aa=[int(s[:8],16) for s in (ROOT/'ghidra'/assembly).read_text(encoding='utf8').splitlines() if re.match(r'^[0-9a-fA-F]{8}:',s)]
            assert min(aa)==a;last=max(aa);tail=next(md.disasm(pe_read(data,last,15),last));size=tail.address+tail.size-a
        else:
            r=known[a];assembly=r.get('assembly',r.get('after',r.get('before')));size=r['length']
        key=f'phase112-primary-{a:x}'
        rows.append(dict(address=a,length=size,assembly=assembly,memory=key+'.txt',executed=True,
                         scope='Whole original extent; selected paths executed, not all branches.'))
        q.append(dict(id=key,tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(a),length=size),print_limit=4))
    blocks=[]
    for a,n,label in [(0x66C1B8,4,'unchanged IsBadReadPtr IAT target'),(0x66D654,0x30,'Object vtable including HP0 request'),
                      (0x66E3EC,0x3A0,'World original vtable'),(0x6BC350,32,'neighbor XY offsets')]:
        key=f'phase112-block-{a:x}';blocks.append(dict(address=a,length=n,label=label,memory=key+'.txt'))
        q.append(dict(id=key,tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(a),length=n),print_limit=4))
    q.append(dict(id='phase112-save-DX',tool='save_program',args=dict(program='DKII-DX.exe'),print_limit=4))
    (ROOT/'phase112-primary-manifest.json').write_text(json.dumps(dict(functions=rows,blocks=blocks),indent=2),encoding='utf8')
    (ROOT/'phase112-primary-queries.json').write_text(json.dumps(q,indent=2),encoding='utf8')
    print(len(rows),'whole functions;',len(blocks),'blocks')


if __name__=='__main__':main()
