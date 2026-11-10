"""Capture complete executed functions;mark calibration bytes as fragments."""
from pathlib import Path
import json,re
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent
def main():
    exe=ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe';data=exe.read_bytes();md=Cs(CS_ARCH_X86,CS_MODE_32);funcs=[];blocks=[];queries=[]
    for a in [0x4611D0,0x517490]:
        before=f'phase64-primary-before-{a:x}.txt';aa=[int(l[:8],16) for l in (ROOT/'ghidra'/before).read_text().splitlines() if re.match(r'^[0-9a-fA-F]{8}:',l)];assert aa[0]==a;last=aa[-1];ins=next(md.disasm(pe_read(data,last,15),last));n=last+ins.size-a;key=f'phase64-final-game-{a:x}'
        funcs.append(dict(program='DKII-DX.exe',exe=str(exe),address=a,length=n,before=before,after=key+'-assembly.txt',memory=key+'-memory.txt'))
        queries.extend([dict(id=key+'-assembly',tool='disassemble_function',args=dict(program='DKII-DX.exe',address=hex(a)),print_limit=10),dict(id=key+'-memory',tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(a),length=n),print_limit=10)])
    policy=json.loads((ROOT/'memory-read-policy.json').read_text())
    for i,r in enumerate(policy['native_calibration']):
        program='DKII-DX.exe' if r['program']=='DX' else 'DKII-local13.exe';source=exe if r['program']=='DX' else ROOT/'source-binaries/DKII-local13.exe';a=int(r['address'],16);n=len(bytes.fromhex(r['hex']));key=f'phase64-final-calibration-{i}'
        blocks.append(dict(program=program,exe=str(source),address=a,length=n,memory=key+'-memory.txt',scope='Single native instruction fragment for read-candidate calibration;not fullfunctionexecution.'))
    a=0x6984B8;n=len(pe_read(data,a,180).split(b'\0',1)[0])+1;blocks.append(dict(program='DKII-DX.exe',exe=str(exe),address=a,length=n,memory='phase64-final-source-path-memory.txt',scope='Native diagnostic source path FightWithDrawState.cpp;source code itself not available.'))
    for r in blocks:queries.append(dict(id=r['memory'][:-4],tool='read_memory',args=dict(program=r['program'],address=hex(r['address']),length=r['length']),print_limit=10))
    (ROOT/'phase64-primary-manifest.json').write_text(json.dumps(dict(functions=funcs,blocks=blocks),indent=2),encoding='utf8');(ROOT/'phase64-primary-queries.json').write_text(json.dumps(queries,indent=2),encoding='utf8');print('Fullfunctions',[(hex(r['address']),r['length']) for r in funcs],'explicitranges',len(blocks))
if __name__=='__main__':main()
