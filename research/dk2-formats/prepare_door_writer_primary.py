"""Reattest ten executed editorbodies,constants and old AP lookup identity."""
from pathlib import Path
import json,re
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from capstone.x86 import X86_OP_MEM
from build_effect_semantics import pe_read
from native_door_writer import PROFILES
ROOT=Path(__file__).resolve().parent
def main():
    md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;functions=[];blocks=[];queries=[]
    for row in json.loads((ROOT/'phase66-door-writer-profiles.json').read_text()):
        profile=row['profile'];f=PROFILES[profile];exe=ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/DK2 Editor pro.exe' if profile=='Pro' else ROOT/'source-binaries/DK2-Editor-rc3.exe';data=exe.read_bytes()
        for a in [row['writer'],*row['calls'][1:]]:
            before=f'phase66-{profile}-{a:x}-disassemble_function.txt';aa=[int(l[:8],16) for l in (ROOT/'ghidra'/before).read_text().splitlines() if re.match(r'^[0-9a-fA-F]{8}:',l)];assert aa[0]==a;last=aa[-1];end=next(md.disasm(pe_read(data,last,15),last));n=last+end.size-a;key=f'phase66-final-{profile}-{a:x}'
            functions.append(dict(profile=profile,program=f['program'],exe=str(exe),address=a,length=n,before=before,after=key+'-assembly.txt',memory=key+'-memory.txt',scope='Full executed nativefunction;date/filefunctions areproviders instead.'))
            queries.extend([dict(id=key+'-assembly',tool='disassemble_function',args=dict(program=f['program'],address=hex(a)),print_limit=8),dict(id=key+'-memory',tool='read_memory',args=dict(program=f['program'],address=hex(a),length=n),print_limit=8)])
        ins=list(md.disasm(pe_read(data,f['writer'],834),f['writer']));fmul=next(i for i in ins if i.mnemonic=='fmul');a=fmul.operands[0].mem.disp;key=f'phase66-final-data-{profile}-scale';blocks.append(dict(program=f['program'],exe=str(exe),address=a,length=8,memory=key+'-memory.txt',scope='Nativebinary64scale4096 usedbywriter.'))
    oldexe=ROOT/'source-binaries/DKII-local13.exe'
    for name,a,n,scope in [('old-APgetter',0x504140,46,'Completegetterbytesforstaticidentity;notexecuted inphase66.'),('old-APvslot',0x65F31C+0x2C8,4,'CWorldvslot2C8pointertoold504140.')]:
        key='phase66-final-data-'+name;blocks.append(dict(program='DKII-local13.exe',exe=str(oldexe),address=a,length=n,memory=key+'-memory.txt',scope=scope))
    for r in blocks:queries.append(dict(id=r['memory'][:-4],tool='read_memory',args=dict(program=r['program'],address=hex(r['address']),length=r['length']),print_limit=8))
    (ROOT/'phase66-primary-manifest.json').write_text(json.dumps(dict(functions=functions,blocks=blocks),indent=2),encoding='utf8');(ROOT/'phase66-primary-queries.json').write_text(json.dumps(queries,indent=2),encoding='utf8');print('Executedfunctions',len(functions),'constant/staticranges',len(blocks))
if __name__=='__main__':main()
