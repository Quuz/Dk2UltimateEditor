"""Reuse attested common helpers; read newly examined stat/caller/copy extents."""
from pathlib import Path
import json
import re
from capstone import Cs, CS_ARCH_X86, CS_MODE_32
from build_effect_semantics import EXE, pe_read

ROOT = Path(__file__).resolve().parent


def main():
    wanted = {0x51DC70, 0x51DC40, 0x51CD90, 0x48DEA0, 0x43D750, 0x441990, 0x4363F0}
    previous = json.loads((ROOT/'phase96-primary-manifest.json').read_text(encoding='utf8'))
    functions = [dict(r, program='DKII-DX.exe', executed=True) for r in previous['functions'] if r['address'] in wanted]
    assert len(functions) == 7
    md = Cs(CS_ARCH_X86, CS_MODE_32)
    data = EXE.read_bytes()
    queries = []
    for row in functions:
        key = f'phase104-primary-{row["address"]:x}'
        row['memory'] = key+'.txt'
        queries.append(dict(id=key, tool='read_memory',
                            args=dict(program='DKII-DX.exe', address=hex(row['address']), length=row['length']), print_limit=5))
    for address, file in [(0x494D00, 'phase104-stat-494d00-disassemble_function.txt'),
                           (0x494EF0, 'phase104-stat-494ef0-disassemble_function.txt'),
                           (0x51F3B0, 'phase104-stat-51f3b0-disassemble_function.txt'),
                           (0x498E30, 'phase104-damage-498e30-disassemble_function.txt')]:
        lines = (ROOT/'ghidra'/file).read_text(encoding='utf8').splitlines()
        addresses = [int(s[:8], 16) for s in lines if re.match(r'^[0-9a-fA-F]{8}:', s)]
        last = max(addresses)
        tail = next(md.disasm(pe_read(data, last, 15), last))
        size = tail.address+tail.size-address
        key = f'phase104-primary-{address:x}'
        functions.append(dict(program='DKII-DX.exe', exe=str(EXE), address=address,
                              length=size, assembly=file, memory=key+'.txt', executed=address == 0x494D00,
                              scope='Whole saved code extent;494D00 executed,other three static only. Not all-branch coverage.'))
        if not (ROOT/'ghidra'/(key+'.txt')).exists():
            queries.append(dict(id=key, tool='read_memory',
                                args=dict(program='DKII-DX.exe', address=hex(address), length=size), print_limit=5))
    old = json.loads((ROOT/'phase101-primary-manifest.json').read_text(encoding='utf8'))
    blocks = [r for r in old['blocks'] if r['program'] == 'DKII-DX.exe' and r['address'] == 0x51D740]
    assert len(blocks) == 1
    blocks[0] = dict(blocks[0], memory='phase104-block-51d740.txt')
    queries.append(dict(id='phase104-block-51d740', tool='read_memory',
                        args=dict(program='DKII-DX.exe', address='0x51d740', length=blocks[0]['length']), print_limit=5))
    queries.append(dict(id='phase104-save-DX', tool='save_program', args=dict(program='DKII-DX.exe'), print_limit=30))
    (ROOT/'phase104-primary-manifest.json').write_text(json.dumps(dict(functions=functions, blocks=blocks), indent=2), encoding='utf8')
    (ROOT/'phase104-primary-queries.json').write_text(json.dumps(queries, indent=2), encoding='utf8')
    print('Prepared11 code extents,8 with executed paths;one original switch table')


if __name__ == '__main__':
    main()
