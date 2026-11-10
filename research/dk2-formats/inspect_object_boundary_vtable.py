from pathlib import Path
import struct
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent
data=(ROOT/'source-binaries/DKII-local13.exe').read_bytes()
for i,a in enumerate(struct.unpack('<37I',pe_read(data,0x65E5D4,148))):
    print(hex(i*4),hex(a))
