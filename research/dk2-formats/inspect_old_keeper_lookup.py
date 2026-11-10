"""Read exact old world vslots and candidate native boundaries."""
from pathlib import Path
import json
import struct
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import pe_read

ROOT=Path(__file__).resolve().parent


def main():
    data=(ROOT/'source-binaries/DKII-local13.exe').read_bytes();md=Cs(CS_ARCH_X86,CS_MODE_32)
    getter=struct.unpack('<I',pe_read(data,0x65F31C+0x110,4))[0]
    a=getter
    text='\n'.join(f'{x.address:08x}: {x.mnemonic} {x.op_str}' for x in md.disasm(pe_read(data,a-16,240),a-16))
    print('Old worldvslot110',hex(getter));print(text)
    (ROOT/'ghidra/phase72-old-keeper-slot-inspection.txt').write_text(text,encoding='utf8')
    proof=dict(world_vtable=0x65F31C,slot=0x110,target=getter,
        scope='ExactPE vtable entry,not a guessedretailversion. Native getter body and returned definition identity require separate proof.')
    (ROOT/'phase72-old-keeper-slot.json').write_text(json.dumps(proof,indent=2),encoding='utf8')


if __name__=='__main__':main()
