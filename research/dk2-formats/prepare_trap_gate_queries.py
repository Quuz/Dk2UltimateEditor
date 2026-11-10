"""Capture complete native trap readiness functions and typed caller chains."""
from pathlib import Path
import json
import struct
from build_effect_semantics import pe_read

ROOT = Path(__file__).resolve().parent


def main():
    queries = []
    profiles = [('DKII-DX.exe', [0x4A7D10, 0x517490, 0x4C1740, 0x4A84E0, 0x4A8E70, 0x4A9B70, 0x4A80E0]),
                ('DKII-local13.exe', [0x49EB20, 0x50B3C0, 0x4B79F0])]
    for program, addresses in profiles:
        for address in addresses:
            key = f'phase68-{program}-{address:x}'
            if program == 'DKII-local13.exe':
                queries.append(dict(id=key+'-create', tool='create_function', args=dict(program=program, address=hex(address), disassemble_first=True), print_limit=20))
            queries.extend([
                dict(id=key+'-assembly', tool='disassemble_function', args=dict(program=program, address=hex(address)), print_limit=20),
                dict(id=key+'-decompile', tool='force_decompile', args=dict(program=program, address=hex(address)), print_limit=20),
            ])
    data = (ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe').read_bytes()
    locator = struct.unpack('<I', pe_read(data, 0x66D750, 4))[0]
    td = struct.unpack('<5I', pe_read(data, locator, 20))[3]
    for label, address, n in [('CTrap-vtable', 0x66D750, 100), ('CTrap-locator', locator, 20), ('CTrap-type', td, 25)]:
        queries.append(dict(id='phase68-DX-'+label, tool='read_memory', args=dict(program='DKII-DX.exe', address=hex(address), length=n), print_limit=20))
    (ROOT/'phase68-trap-gate-queries.json').write_text(json.dumps(queries, indent=2), encoding='utf8')


if __name__ == '__main__':
    main()
