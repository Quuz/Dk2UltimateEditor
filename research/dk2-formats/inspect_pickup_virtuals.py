"""Inspect actual class virtual entries;source-map names are only selection hints."""
from pathlib import Path
import json,struct
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent
data=(ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe').read_bytes();md=Cs(CS_ARCH_X86,CS_MODE_32)
out=[]
for name,vt in [('CCreature',0x66D524),('CObject',0x66D654)]:
    entries=struct.unpack('<37I',pe_read(data,vt,148))
    for i,a in enumerate(entries):
        first=list(md.disasm(pe_read(data,a,20),a))
        out.append(dict(class_hint=name,vtable=vt,slot=i*4,address=a,prefix=[f'{x.address:08x}: {x.mnemonic} {x.op_str}' for x in first[:4]]))
        print(name,hex(i*4),hex(a),'; '.join(f'{x.mnemonic} {x.op_str}' for x in first[:3]))
(ROOT/'phase73-pickup-virtual-prefixes.json').write_text(json.dumps(out,indent=2),encoding='utf8')
