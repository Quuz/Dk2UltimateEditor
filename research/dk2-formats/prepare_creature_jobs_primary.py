"""Read back original code for job overlap and indexed-pointer lead contexts."""
from pathlib import Path
import json,re,sys
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent
INDEX=[0x494380,0x494480,0x4944D0,0x494550,0x4945D0,0x494700,0x494720,0x494860,0x494880,0x494A00,0x48BE50,0x496700,0x4C4600]
HELPERS=[0x569C40,0x569C60,0x569C80,0x569DD0,0x569DF0,0x569E10,0x569E30,0x569E50,0x569E70,0x569E90,0x569EB0,0x569ED0,0x569EF0,0x569F10,0x569F30,0x569F50,0x569F70,0x569F90,0x569FB0,0x569FD0]
SPEC=[('DX','DKII-DX.exe','Dungeon Keeper 2/DKII-DX.exe',sorted(set(INDEX+HELPERS+[0x4E8DA0,0x568820,0x568E30,0x492320,0x491DC0]))),
 ('Pro','DK2 Editor pro.exe','Dungeon Keeper 2/Data/editor/DK2 Editor pro.exe',[0x4281B0,0x4287C0,0x429940,0x4299B0,0x41E5A0,0x41E5B0,0x41E5D0,0x41E5E0,0x4A7F00,0x4AB320])]

def main():
    q=[];manifest=dict(functions=[],blocks=[]);md=Cs(CS_ARCH_X86,CS_MODE_32)
    for label,program,path,addresses in SPEC:
        exe=ROOT.parent.parent/path
        for a in addresses:
            if len(sys.argv)==1:
                for name,tool in [('create','create_function'),('assembly','disassemble_function')]:
                    args=dict(program=program,address=hex(a))
                    if tool=='create_function':args['disassemble_first']=True
                    q.append(dict(id=f'phase86-final-{label}-{a:x}-{name}',tool=tool,args=args,print_limit=5))
                continue
            assembly=f'phase86-final-{label}-{a:x}-assembly.txt';lines=(ROOT/'ghidra'/assembly).read_text().splitlines();aa=[int(s[:8],16) for s in lines if re.match(r'^[0-9a-fA-F]{8}:',s)]
            assert aa and aa[0]==a;last=next(md.disasm(pe_read(exe.read_bytes(),aa[-1],15),aa[-1]));n=last.address+last.size-a;key=f'phase86-final-{label}-{a:x}-memory'
            manifest['functions'].append(dict(program=program,exe=str(exe),address=a,length=n,assembly=assembly,memory=key+'.txt',scope='Whole function attestation;not a branch-coverage trace',clock_adapter_only=label=='Pro' and a==0x4AB320))
            q.append(dict(id=key,tool='read_memory',args=dict(program=program,address=hex(a),length=n),print_limit=5))
    if len(sys.argv)>1:
        a=0x66E3EC;n=0x400;key='phase86-final-DX-world-vtable'
        manifest['blocks'].append(dict(program='DKII-DX.exe',exe=str(ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe'),address=a,length=n,memory=key+'.txt',label='Original World vtable'))
        q.append(dict(id=key,tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(a),length=n),print_limit=5))
        (ROOT/'phase86-primary-manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf8')
    out='phase86-final-'+('code' if len(sys.argv)==1 else 'memory')+'-queries.json';(ROOT/out).write_text(json.dumps(q,indent=2),encoding='utf8');print(out,len(q),'queries')
if __name__=='__main__':main()
