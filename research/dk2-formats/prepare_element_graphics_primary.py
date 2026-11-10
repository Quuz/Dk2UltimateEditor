"""Read complete saved DX code extents and the constants used by phase 102."""
from pathlib import Path
import json
import re
from capstone import Cs, CS_ARCH_X86, CS_MODE_32
from build_effect_semantics import pe_read, EXE

ROOT = Path(__file__).resolve().parent
EXECUTED = {0x599C60, 0x59B880, 0x59C360, 0x59C720, 0x59C2A0,
            0x57CEB0, 0x57CD30, 0x57F1E0, 0x57FAF0,
            0x41C440, 0x44E7B0, 0x4363F0, 0x43D730, 0x571AE0}


def main():
    data = EXE.read_bytes()
    md = Cs(CS_ARCH_X86, CS_MODE_32)
    functions, blocks, queries = [], [], []
    for path in sorted((ROOT / 'ghidra').glob('phase102-*-disassemble_function.txt')):
        addresses = [int(s[:8], 16) for s in path.read_text(encoding='utf8').splitlines()
                     if re.match(r'^[0-9a-fA-F]{8}:', s)]
        start, last = min(addresses), max(addresses)
        tail = next(md.disasm(pe_read(data, last, 15), last))
        size = tail.address + tail.size - start
        key = f'phase102-primary-{start:x}'
        functions.append(dict(program='DKII-DX.exe', exe=str(EXE),
                              address=start, length=size, assembly=path.name,
                              memory=key + '.txt', executed=start in EXECUTED,
                              scope='Whole saved code extent; only explicitly selected fixture branches executed.'))
        queries.append(dict(id=key, tool='read_memory',
                            args=dict(program='DKII-DX.exe', address=hex(start), length=size),
                            print_limit=5))
    assert {r['address'] for r in functions if r['executed']} == EXECUTED
    for address, size, label in [(0x6704D0, 4, 'XY Q12 conversion'),
                                 (0x670504, 4, 'negative Z Q12 conversion'),
                                 (0x670520, 4, 'byte color normalization'),
                                 (0x66FC18, 4, 'constructor RGB scaling'),
                                 (0x6704B8, 4, 'angle pi constant'),
                                 (0x670548, 4, 'angle scaling'),
                                 (0x66FCCC, 40, 'original sprite vtable'),
                                 (0x66FCF4, 40, 'original quad vtable')]:
        key = f'phase102-block-{address:x}'
        blocks.append(dict(program='DKII-DX.exe', exe=str(EXE), address=address,
                           length=size, label=label, memory=key + '.txt'))
        queries.append(dict(id=key, tool='read_memory',
                            args=dict(program='DKII-DX.exe', address=hex(address), length=size),
                            print_limit=5))
    queries.append(dict(id='phase102-save-DX', tool='save_program',
                        args=dict(program='DKII-DX.exe'), print_limit=30))
    (ROOT / 'phase102-primary-manifest.json').write_text(
        json.dumps(dict(functions=functions, blocks=blocks), indent=2), encoding='utf8')
    (ROOT / 'phase102-primary-queries.json').write_text(json.dumps(queries, indent=2), encoding='utf8')
    print('Prepared', len(functions), 'code extents and', len(blocks), 'blocks')


if __name__ == '__main__':
    main()
