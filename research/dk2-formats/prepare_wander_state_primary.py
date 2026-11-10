"""Re-read complete DX wander helpers and exact direction/jitter/source bytes."""
from pathlib import Path
import json,re
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent
exe=ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe';md=Cs(CS_ARCH_X86,CS_MODE_32);functions=[];blocks=[];queries=[]
for a,executed,before in [
 (0x480750,True,'phase75-DX-480750-assembly.txt'),
 (0x517490,True,'phase68-DKII-DX.exe-517490-assembly.txt'),
 (0x5558D0,True,'phase65-final-game-5558d0-assembly.txt'),
 (0x471890,True,'phase75-DX-471890-assembly.txt'),
 (0x4B35C0,True,'phase75-DX-4b35c0-assembly.txt'),
 (0x4808E0,False,'phase75-DX-4808e0-assembly.txt'),
 (0x480A00,False,'phase75-DX-480a00-assembly.txt'),
 (0x485AB0,False,'phase75-DX-485ab0-assembly.txt'),
 (0x485B80,False,'phase75-DX-485b80-assembly.txt'),
]:
    aa=[int(line[:8],16) for line in (ROOT/'ghidra'/before).read_text().splitlines() if re.match(r'^[0-9a-fA-F]{8}:',line)]
    last=next(md.disasm(pe_read(exe.read_bytes(),aa[-1],15),aa[-1]))
    assert aa[0]==a and (last.mnemonic=='ret' or (last.mnemonic=='jmp' and a<=int(last.op_str,16)<last.address+last.size)),(hex(a),last.mnemonic,last.op_str)
    n=last.address+last.size-a;key=f'phase75-final-DX-{a:x}'
    functions.append(dict(program='DKII-DX.exe',exe=str(exe),address=a,length=n,before=before,after=key+'-assembly.txt',memory=key+'-memory.txt',executed=executed))
    queries.extend([dict(id=key+'-assembly',tool='disassemble_function',args=dict(program='DKII-DX.exe',address=hex(a)),print_limit=5),dict(id=key+'-memory',tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(a),length=n),print_limit=5)])
source=pe_read(exe.read_bytes(),0x699F78,128).split(b'\0',1)[0]+b'\0'
for a,n,label in [(0x6BC350,32,'four-cell-offsets'),(0x698EC8,36,'nine-subcell-offsets'),(0x699F78,len(source),'native-StateUtils-source-path')]:
    key=f'phase75-final-DX-data-{a:x}'
    blocks.append(dict(program='DKII-DX.exe',exe=str(exe),address=a,length=n,memory=key+'-memory.txt',label=label))
    queries.append(dict(id=key+'-memory',tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(a),length=n),print_limit=5))
(ROOT/'phase75-primary-manifest.json').write_text(json.dumps(dict(functions=functions,blocks=blocks),indent=2),encoding='utf8')
(ROOT/'phase75-primary-queries.json').write_text(json.dumps(queries,indent=2),encoding='utf8')
print('9completePEfunctions,5executed;3exactdata ranges')
