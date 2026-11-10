"""PE boundary inspection for the previously untyped manual-trap caller."""
from pathlib import Path
import json
from capstone import Cs, CS_ARCH_X86, CS_MODE_32
from build_effect_semantics import pe_read

ROOT = Path(__file__).resolve().parent


def main():
    saved = json.loads((ROOT/'ghidra/phase69-DX-513700-memory.txt').read_text())
    md = Cs(CS_ARCH_X86, CS_MODE_32)
    b = bytes(saved['data'])
    a = int(saved['address'],16)
    exe = (ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe').read_bytes()
    assert b == pe_read(exe,a,len(b))
    text = '\n'.join(f'{x.address:08x}: {x.mnemonic} {x.op_str}' for x in md.disasm(b,a))
    (ROOT/'ghidra/phase69-DX-513600-PE-inspection.txt').write_text(text,encoding='utf8')
    print(text)


if __name__=='__main__':
    main()
