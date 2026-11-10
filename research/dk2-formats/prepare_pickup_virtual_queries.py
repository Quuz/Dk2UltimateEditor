"""Verify native entry boundaries and capture raw vslot predicate instructions."""
from pathlib import Path
import json,struct
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent
data=(ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe').read_bytes();md=Cs(CS_ARCH_X86,CS_MODE_32);qs=[];blocks=[]
for a in [0x49D130,0x628BC0,0x50DB20]:
    previous=pe_read(data,a-16,16)
    assert previous[-1] in [0x90,0xCC,0xC3] or previous[-3]==0xC2,(hex(a),previous.hex())
    print(hex(a),'; '.join(f'{x.address:x}:{x.mnemonic} {x.op_str}' for x in md.disasm(pe_read(data,a,32),a)))
    qs.append(dict(id=f'phase73-DX-{a:x}-create',tool='create_function',args=dict(program='DKII-DX.exe',address=hex(a),disassemble_first=True),print_limit=8))
for name,vt in [('CCreature',0x66D524),('CObject',0x66D654)]:
    locator=struct.unpack('<I',pe_read(data,vt-4,4))[0];td=struct.unpack('<5I',pe_read(data,locator,20))[3]
    for label,a,n in [(name+'-vtable',vt-4,152),(name+'-locator',locator,20),(name+'-type',td,8+len(('.?AV'+name+'@@\0').encode()))]:
        qs.append(dict(id='phase73-DX-'+label,tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(a),length=n),print_limit=8))
        blocks.append(dict(label=label,address=a,length=n))
(ROOT/'phase73-virtual-create-queries.json').write_text(json.dumps(qs,indent=2),encoding='utf8')
(ROOT/'phase73-virtual-rtti-ranges.json').write_text(json.dumps(blocks,indent=2),encoding='utf8')
