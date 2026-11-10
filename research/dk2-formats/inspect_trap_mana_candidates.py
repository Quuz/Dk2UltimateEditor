"""Inspect exact PE vtable and candidate-method bytes, without assuming types."""
from pathlib import Path
import json
import struct
from capstone import Cs, CS_ARCH_X86, CS_MODE_32
from build_effect_semantics import pe_read

ROOT = Path(__file__).resolve().parent


def main():
    md = Cs(CS_ARCH_X86, CS_MODE_32)
    profiles = [
        ('old', ROOT/'source-binaries/DKII-local13.exe', 0x65E6D4,
         [(0x49EAF0, 0x2E0), (0x49F000, 0xA00)]),
        ('DX', ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe', 0x66D754,
         [(0x4A7CE0, 0x270), (0x4A80E0, 0x900)]),
    ]
    out = []
    for name, exe, vt, spans in profiles:
        data = exe.read_bytes()
        vtable = struct.unpack('<24I', pe_read(data, vt, 96))
        print(name, 'vtable', [hex(a) for a in vtable])
        for a, n in spans:
            text = '\n'.join(f'{x.address:08x}: {x.mnemonic} {x.op_str}' for x in md.disasm(pe_read(data, a, n), a))
            file = f'phase68-{name}-{a:x}-PE-inspection.txt'
            (ROOT/'ghidra'/file).write_text(text, encoding='utf8')
            calls = [f'{x.address:x}:{x.op_str}' for x in md.disasm(pe_read(data, a, n), a) if x.mnemonic == 'call']
            out.append(dict(profile=name, exe=str(exe), address=a, length=n, inspection=file, direct_and_indirect_calls=calls))
            print(name, hex(a), calls)
    (ROOT/'phase68-trap-mana-inspections.json').write_text(json.dumps(out, indent=2), encoding='utf8')
    queries = [dict(id='phase68-load-old-program', tool='load_program',
                    args=dict(file=str(ROOT/'source-binaries/DKII-local13.exe')), print_limit=120)]
    for program, addresses in [
        ('DKII-DX.exe', [0x4A7D10, 0x4A7D90, 0x4A7E70, 0x4A80E0, 0x4A8E70, 0x4A9B70, 0x4A84E0, 0x4A0930, 0x458F60, 0x513744, 0x4C1740]),
        ('DKII-local13.exe', [0x49EB20, 0x49EBA0, 0x49EDA0, 0x49F000, 0x49FED0, 0x4A09D0]),
    ]:
        for address in addresses:
            key = f'phase68-{program}-{address:x}'
            queries.extend([
                dict(id=key+'-owner', tool='get_function_by_address', args=dict(program=program, address=hex(address)), print_limit=180),
                dict(id=key+'-xrefs', tool='get_xrefs_to', args=dict(program=program, address=hex(address), limit=200), print_limit=280),
            ])
    (ROOT/'phase68-trap-mana-queries.json').write_text(json.dumps(queries, indent=2), encoding='utf8')


if __name__ == '__main__':
    main()
