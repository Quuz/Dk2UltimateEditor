"""Re-read complete native Sight chain and dispatch/type registration evidence."""
from pathlib import Path
import json,re,struct
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent
exe=ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe';data=exe.read_bytes();md=Cs(CS_ARCH_X86,CS_MODE_32);functions=[];blocks=[];queries=[]
for a,before,executed in [
 (0x4AF0E0,'phase77-DX-4af0e0-assembly.txt',True),(0x508C60,'phase77-DX-508c60-assembly.txt',True),
 (0x517480,'phase45-final-game-517480-assembly.txt',True),(0x527150,'phase77-DX-527150-assembly.txt',True),
 (0x43B1E0,'phase77-DX-43b1e0-assembly.txt',True),(0x448580,'phase77-DX-448580-assembly.txt',True),
 (0x441510,'phase77-DX-441510-assembly.txt',True),(0x440F50,'phase77-DX-440f50-assembly.txt',True),
 (0x43AAD0,'phase77-DX-43aad0-assembly.txt',False),(0x43A740,'phase77-DX-43a740-assembly.txt',False),
 (0x4AD1B0,'phase77-DX-4ad1b0-assembly.txt',False),
]:
    aa=[int(line[:8],16) for line in (ROOT/'ghidra'/before).read_text().splitlines() if re.match(r'^[0-9a-fA-F]{8}:',line)]
    last=next(md.disasm(pe_read(data,aa[-1],15),aa[-1]));assert aa[0]==a and (last.mnemonic=='ret' or (a==0x440F50 and last.mnemonic=='jmp'))
    n=last.address+last.size-a;key=f'phase77-final-DX-{a:x}'
    functions.append(dict(program='DKII-DX.exe',exe=str(exe),address=a,length=n,before=before,after=key+'-assembly.txt',memory=key+'-memory.txt',executed=executed))
    queries.extend([dict(id=key+'-assembly',tool='disassemble_function',args=dict(program='DKII-DX.exe',address=hex(a)),print_limit=5),dict(id=key+'-memory',tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(a),length=n),print_limit=5)])
bridge_rows=[]
while True:
    row=struct.unpack('<II',pe_read(data,0x66D0D8+8*len(bridge_rows),8));bridge_rows.append(row)
    if row==(0,0):break
assert dict(bridge_rows)[13]==0x448580 and len(bridge_rows)==22
for a,n,label in [(0x66D0D8,8*len(bridge_rows),'Bridge-command-pairs-terminated'),(0x69E9F8,17,'Shot-process25-runtime-table-zero-in-saved-image;4AD1B0-registers'),
 (0x66CC94+0x28,4,'Bridge-vslot28-dispatch'),(0x66CC94+0x90,4,'Bridge-vslot90-actual-effect-provider-not-executed'),
 (0x66CC94+0xBC,4,'Bridge-vslotBC-forwarder'),(0x66E3EC+0x264,4,'World-local-owner-getter'),(0x66E3EC+0x110,4,'World-Keeper-lookup-offered-not-used-by-native-chain')]:
    key=f'phase77-final-DX-data-{a:x}';blocks.append(dict(program='DKII-DX.exe',exe=str(exe),address=a,length=n,memory=key+'-memory.txt',label=label))
    queries.append(dict(id=key+'-memory',tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(a),length=n),print_limit=5))
(ROOT/'phase77-primary-manifest.json').write_text(json.dumps(dict(functions=functions,blocks=blocks),indent=2),encoding='utf8')
(ROOT/'phase77-primary-queries.json').write_text(json.dumps(queries,indent=2),encoding='utf8')
print('11wholePEfunctions(8executed),7exactdata ranges')
