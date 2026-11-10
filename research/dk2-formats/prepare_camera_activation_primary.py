"""Reattest all seven complete executed native bodies and dispatch tables."""
from pathlib import Path
import json,re
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent

def main():
    old=json.loads((ROOT/'phase40-primary-manifest.json').read_text())
    exe=ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe';data=exe.read_bytes();md=Cs(CS_ARCH_X86,CS_MODE_32)
    rows=[];queries=[]
    for addr in [0x44ACE0,0x44BD30,0x44BD70,0x572CD0,0x56F850,0x56F900,0x449A30]:
        before=f'phase63-game-{addr:06X}-disassemble_function.txt' if addr in [0x44ACE0,0x44BD30,0x44BD70,0x572CD0] else next(r['before'] for r in old if r['address']==addr and r['program']=='DKII-DX.exe')
        lines=(ROOT/'ghidra'/before).read_text().splitlines();aa=[int(l[:8],16) for l in lines if re.match(r'^[0-9a-fA-F]{8}:',l)];assert aa[0]==addr
        last=aa[-1];ins=next(md.disasm(pe_read(data,last,15),last));length=last+ins.size-addr
        key=f'phase63-final-camera-{addr:x}'
        rows.append(dict(program='DKII-DX.exe',exe=str(exe),address=addr,length=length,before=before,after=key+'-assembly.txt',memory=key+'-memory.txt'))
        queries.extend([dict(id=key+'-assembly',tool='disassemble_function',args=dict(program='DKII-DX.exe',address=hex(addr)),print_limit=12),dict(id=key+'-memory',tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(addr),length=length),print_limit=12)])
    blocks=[dict(program='DKII-DX.exe',exe=str(exe),address=0x44BB8C,length=0x44BD2E-0x44BB8C,memory='phase63-final-camera-dispatch-tables-memory.txt',scope='Initialized jump/index tables for full44ACE0;runtime orientation table75A311 is an explicit provider fixture,not initialized PE data.')]
    for r in blocks:queries.append(dict(id=r['memory'][:-4],tool='read_memory',args=dict(program=r['program'],address=hex(r['address']),length=r['length']),print_limit=12))
    (ROOT/'phase63-primary-manifest.json').write_text(json.dumps(dict(functions=rows,blocks=blocks),indent=2),encoding='utf8')
    (ROOT/'phase63-primary-queries.json').write_text(json.dumps(queries,indent=2),encoding='utf8')
    print('Fullnativefunctions',[(hex(r['address']),r['length']) for r in rows])
if __name__=='__main__':main()
