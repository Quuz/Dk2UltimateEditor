"""Recapture complete AP condition functions and native dispatch/vtable entries."""
from pathlib import Path
import json,re
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent
def main():
    exe=ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe';raw=exe.read_bytes();md=Cs(CS_ARCH_X86,CS_MODE_32);rows=[];qs=[]
    files=[f'phase56-game-{a:x}-disassemble_function.txt' for a in [0x4B0D70,0x4B0620,0x4B06D0,0x4B07E0,0x4B0960,0x4B0A00,0x508290,0x519980,0x4B05E0,0x50FAE0]]
    files += [f'phase52-game-{a:x}-disassemble_function.txt' for a in [0x4B0A80,0x4B0CC0]]
    for file in files:
        aa=[int(l[:8],16) for l in (ROOT/'ghidra'/file).read_text().splitlines() if re.match(r'^[0-9a-f]{8}:',l)];a=aa[0];last=aa[-1];ins=next(md.disasm(pe_read(raw,last,15),last));n=last+ins.size-a;key=f'phase56-final-game-{a:x}'
        rows.append(dict(program='DKII-DX.exe',exe=str(exe),address=a,length=n,before=file,after=key+'-assembly.txt',memory=key+'-memory.txt'))
        qs.extend([dict(id=key+'-assembly',tool='disassemble_function',args=dict(program='DKII-DX.exe',address=hex(a)),print_limit=10),dict(id=key+'-memory',tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(a),length=n),print_limit=10)])
    blocks=[]
    for a,n,role in [(0x4B0EDC,64,'AP dispatch jump/index tables'),(0x519A04,24,'Signed comparison jump table'),(0x66D7B8,88,'AP RTTI locator pointer and84-byte vtable'),(0x66E3EC+0x2C8,4,'CWorld virtual2C8 getter pointer')]:
        key=f'phase56-final-data-{a:x}';blocks.append(dict(address=a,length=n,exe=str(exe),role=role,memory=key+'-memory.txt'));qs.append(dict(id=key+'-memory',tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(a),length=n),print_limit=10))
    (ROOT/'phase56-primary-manifest.json').write_text(json.dumps(dict(functions=rows,blocks=blocks),indent=2));(ROOT/'phase56-primary-queries.json').write_text(json.dumps(qs,indent=2));print('Complete native bodies:',len(rows),'data ranges:',len(blocks))
if __name__=='__main__':main()
