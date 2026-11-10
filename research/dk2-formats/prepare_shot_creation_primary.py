"""Recapture all native bodies executed by phase54, outside fixture overlays."""
from pathlib import Path
import json,re
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent
def main():
    exe=ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe';raw=exe.read_bytes();md=Cs(CS_ARCH_X86,CS_MODE_32);rows=[];qs=[]
    files=[f'phase53-game-{a:x}-disassemble_function.txt' for a in [0x4BBF90,0x4BC290,0x4B7CD0,0x40D640]]
    files += [f'phase54-game-{a:x}-disassemble_function.txt' for a in [0x4B7D60,0x4AAC90]]
    files += [f'phase54-helper-{a:x}-disassemble_function.txt' for a in [0x402AE0,0x40CFD0,0x4AAF60,0x4B49D0,0x4B5560]]
    for file in files:
        aa=[int(l[:8],16) for l in (ROOT/'ghidra'/file).read_text().splitlines() if re.match(r'^[0-9a-f]{8}:',l)];a=aa[0];last=aa[-1];ins=next(md.disasm(pe_read(raw,last,15),last));n=last+ins.size-a;key=f'phase54-final-game-{a:x}'
        rows.append(dict(program='DKII-DX.exe',exe=str(exe),address=a,length=n,before=file,after=key+'-assembly.txt',memory=key+'-memory.txt'))
        qs.extend([dict(id=key+'-assembly',tool='disassemble_function',args=dict(program='DKII-DX.exe',address=hex(a)),print_limit=10),dict(id=key+'-memory',tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(a),length=n),print_limit=10)])
    (ROOT/'phase54-primary-manifest.json').write_text(json.dumps(rows,indent=2));(ROOT/'phase54-primary-queries.json').write_text(json.dumps(qs,indent=2));print('Primary bodies:',len(rows))
if __name__=='__main__':main()
