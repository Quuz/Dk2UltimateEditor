"""Re-read complete PE bodies, vtables, RTTI and DoorOpen native label."""
from pathlib import Path
import json
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent

def main():
 exe=ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe';raw=exe.read_bytes();md=Cs(CS_ARCH_X86,CS_MODE_32);rows=[];blocks=[];qs=[]
 aa=[0x5953D0,0x5955C0,0x5956E0,0x5958F0,0x4B5C10,0x40CFD0,0x570D10,0x56AA40,0x4AA440,0x4A7100,0x4A7940,0x4A5070,0x4B28D0,0x4B4A10,0x4A6F10,0x59B880,0x4B37D0]
 files=[f'phase59-game-{a:x}-disassemble_function.txt' for a in aa]+['phase52-DKII-DX.exe-6361a0-disassemble_function.txt']
 for file in files:
  rr=[int(l[:8],16) for l in (ROOT/'ghidra'/file).read_text().splitlines() if len(l)>9 and l[8]==':'];a=rr[0];last=rr[-1];ins=next(md.disasm(pe_read(raw,last,15),last));n=last+ins.size-a;key=f'phase59-final-game-{a:x}'
  rows.append(dict(program='DKII-DX.exe',exe=str(exe),address=a,length=n,before=file,after=key+'-assembly.txt',memory=key+'-memory.txt'))
  qs.extend([dict(id=key+'-assembly',tool='disassemble_function',args=dict(program='DKII-DX.exe',address=hex(a)),print_limit=10),dict(id=key+'-memory',tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(a),length=n),print_limit=10)])
 for label,a,n in [('world-vtable',0x66E3EC,920),('trap-vtable',0x66D750,100),('door-vtable',0x66D6E8,100),('trap-locator',0x67D760,20),('door-locator',0x67D708,20),('trap-type',0x69F130,20),('door-type',0x69F118,20),('DoorOpen-label',0x69E514,32)]:
  key='phase59-final-data-'+label;blocks.append(dict(program='DKII-DX.exe',exe=str(exe),address=a,length=n,memory=key+'-memory.txt',label=label));qs.append(dict(id=key+'-memory',tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(a),length=n),print_limit=10))
 (ROOT/'phase59-primary-manifest.json').write_text(json.dumps(dict(functions=rows,blocks=blocks),indent=2));(ROOT/'phase59-primary-queries.json').write_text(json.dumps(qs,indent=2));print('Complete bodies',len(rows),'data blocks',len(blocks))
if __name__=='__main__':main()
