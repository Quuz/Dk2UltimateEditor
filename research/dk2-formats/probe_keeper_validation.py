"""Phase152: primary evidence for original Keeper pre-cast validation.

Callback roots below are actual pointers stored by 5174A0/517580. No guessed
interior-function entries are created. Initial queries are read-only.
"""
from pathlib import Path
import json,struct
from build_effect_semantics import EXE,pe_read
ROOT=Path(__file__).resolve().parent
ROOTS=[0x4BB8A0,0x4BBA40,0x4BBBC0,0x4BB010,0x4C1740,0x517490,
       0x5174A0,0x517580,0x5176B0,0x517730,0x5177B0,
       0x517800,0x517930,0x517990,0x5179D0,0x517AB0,
       0x517B80,0x517CB0,0x517DE0,0x517F10,0x517F60]
def main():
    blob=EXE.read_bytes();rows=[]
    for slot in [0x10C,0x110,0x1D4,0x20C]:
        a=struct.unpack('<I',pe_read(blob,0x66E3EC+slot,4))[0]
        print(f'World {slot:x} -> {a:06x}')
        if a not in ROOTS: ROOTS.append(a)
    for a in ROOTS:
        for tool,suffix in [('get_function_by_address','owner'),('disassemble_function','assembly'),('decompile_function','decompile')]:
            rows.append(dict(id=f'phase152-probe-{a:06x}-{suffix}',tool=tool,args=dict(program='DKII-DX.exe',address=hex(a)),print_limit=0))
    (ROOT/'phase152-keeper-validation-probe-queries.json').write_text(json.dumps(rows,indent=2),encoding='utf8')
    print(len(rows),'read-only queries')
if __name__=='__main__':main()
