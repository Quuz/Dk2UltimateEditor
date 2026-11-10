"""Inspect candidate checksum loop and scalar-loader boundary in exact old PE."""
from pathlib import Path
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent
data=(ROOT/'source-binaries/DKII-local13.exe').read_bytes();md=Cs(CS_ARCH_X86,CS_MODE_32)
text='\n'.join(f'{x.address:08x}: {x.mnemonic} {x.op_str}' for x in md.disasm(pe_read(data,0x4EC8B0,512),0x4EC8B0))
(ROOT/'ghidra/phase72-old-loader-PE-inspection.txt').write_text(text,encoding='utf8')
print(text)
