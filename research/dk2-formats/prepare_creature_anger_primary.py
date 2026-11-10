"""Final native bodies/ranges for anger logic, bounded field paths and rejected leads."""
from pathlib import Path
import json,re,struct
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent
def main():
    exe=ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe';raw=exe.read_bytes();md=Cs(CS_ARCH_X86,CS_MODE_32);rows=[];qs=[]
    addresses=[0x486E00,0x486E20,0x486ED0,0x486F80,0x487030,0x4870E0,0x4871E0,0x4871B0,0x478B80,0x4872A0,0x4BCB60,0x506BF0,0x418080,0x4C2C30,0x487210,0x4C2530,0x518AD0,0x4B5A10]
    files=[f'phase57-game-{a:x}-disassemble_function.txt' for a in addresses]+['game-creature-type-assembly.txt','phase41-49ef90-disassemble_function.txt']
    for file in files:
        aa=[int(l[:8],16) for l in (ROOT/'ghidra'/file).read_text().splitlines() if re.match(r'^[0-9a-f]{8}:',l)];a=aa[0];last=aa[-1];ins=next(md.disasm(pe_read(raw,last,15),last));n=last+ins.size-a;key=f'phase57-final-game-{a:x}'
        rows.append(dict(program='DKII-DX.exe',exe=str(exe),address=a,length=n,before=file,after=key+'-assembly.txt',memory=key+'-memory.txt'))
        qs.extend([dict(id=key+'-assembly',tool='disassemble_function',args=dict(program='DKII-DX.exe',address=hex(a)),print_limit=10),dict(id=key+'-memory',tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(a),length=n),print_limit=10)])
    blocks=[];locator=struct.unpack('<I',pe_read(raw,0x66D998,4))[0];td=struct.unpack_from('<I',pe_read(raw,locator,20),12)[0]
    for a,n,role in [(0x69A104,12,'Anger diagnostic caption'),(0x69A0F0,20,'UnhappyAt diagnostic field'),(0x69A0E0,16,'State diagnostic field'),(0x66D998,12,'CPlayer RTTI pointer and8-byte vtable'),(locator,20,'CPlayer RTTI locator'),(td,64,'CPlayer native type descriptor'),(0x5198C4,159,'World action jump/index tables,type44→519704')]:
        key=f'phase57-final-data-{a:x}';blocks.append(dict(address=a,length=n,exe=str(exe),role=role,memory=key+'-memory.txt'));qs.append(dict(id=key+'-memory',tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(a),length=n),print_limit=10))
    (ROOT/'phase57-primary-manifest.json').write_text(json.dumps(dict(functions=rows,blocks=blocks),indent=2));(ROOT/'phase57-primary-queries.json').write_text(json.dumps(qs,indent=2));print('Native function bodies:',len(rows),'data/RTTI ranges:',len(blocks))
if __name__=='__main__':main()
