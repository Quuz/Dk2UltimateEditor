"""Read critical original Object tick,debug labels and virtual method bindings."""
from pathlib import Path
import json
import re
import struct
from capstone import Cs, CS_ARCH_X86, CS_MODE_32
from build_effect_semantics import EXE, pe_read

ROOT = Path(__file__).resolve().parent


def main():
    old = {}
    for name in ['phase94-primary-manifest.json', 'phase105-primary-manifest.json', 'phase107-primary-manifest.json']:
        old.update({r['address']: r for r in json.loads((ROOT/name).read_text(encoding='utf8'))['functions']})
    current = {0x49BD20: 'phase108-object', 0x49EED0: 'phase108-object', 0x4B3AD0: 'phase108-object',
               0x49D310: 'phase108-object', 0x4B7250: 'phase108-object', 0x4B2030: 'phase107-tick'}
    data, md = EXE.read_bytes(), Cs(CS_ARCH_X86, CS_MODE_32)
    slot74 = struct.unpack('<I', pe_read(data, 0x66D654+0x74, 4))[0]
    wanted = set(current)|{0x4B2CE0, slot74}
    rows, queries = [], []
    for a in sorted(wanted):
        if a in current:
            assembly = f'{current[a]}-{a:x}-disassemble_function.txt'
            aa = [int(s[:8], 16) for s in (ROOT/'ghidra'/assembly).read_text(encoding='utf8').splitlines() if re.match(r'^[0-9a-fA-F]{8}:', s)]
            assert min(aa) == a
            last = max(aa)
            tail = next(md.disasm(pe_read(data, last, 15), last))
            size = tail.address+tail.size-a
        else:
            row = old[a]
            assembly, size = row['assembly'], row['length']
        key = f'phase108-primary-{a:x}'
        rows.append(dict(address=a, length=size, assembly=assembly, memory=key+'.txt', executed=a != 0x49D310))
        queries.append(dict(id=key, tool='read_memory', args=dict(program='DKII-DX.exe', address=hex(a), length=size), print_limit=5))
    blocks = []
    for a, n, label in [(0x66D654, 0x100, 'original-Object-vtable'),
                         (0x69D714, 16, 'native-RoomId-label'), (0x69D5DC, 16, 'native-InHand-label')]:
        key = f'phase108-block-{a:x}'
        blocks.append(dict(address=a, length=n, label=label, memory=key+'.txt'))
        queries.append(dict(id=key, tool='read_memory', args=dict(program='DKII-DX.exe', address=hex(a), length=n), print_limit=5))
    queries.append(dict(id='phase108-save-DX', tool='save_program', args=dict(program='DKII-DX.exe'), print_limit=30))
    (ROOT/'phase108-primary-manifest.json').write_text(json.dumps(dict(functions=rows, blocks=blocks), indent=2), encoding='utf8')
    (ROOT/'phase108-primary-queries.json').write_text(json.dumps(queries, indent=2), encoding='utf8')
    print('slot74', hex(slot74), ';functions', len(rows), ';blocks', len(blocks))


if __name__ == '__main__':
    main()
