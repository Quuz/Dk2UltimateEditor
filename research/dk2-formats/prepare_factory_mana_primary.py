"""Primary factory/registration and complete original Player mana functions."""
from pathlib import Path
import json
import re
from capstone import Cs, CS_ARCH_X86, CS_MODE_32
from build_effect_semantics import EXE, pe_read

ROOT = Path(__file__).resolve().parent


def main():
    old = {}
    for name in ['phase94-primary-manifest.json', 'phase108-primary-manifest.json']:
        old.update({r['address']: r for r in json.loads((ROOT/name).read_text(encoding='utf8'))['functions']})
    current = {a: 'phase109-mana' for a in [0x4C10E0, 0x4C1770, 0x4C17A0, 0x4C0090, 0x4BB0D0]}
    wanted = set(current)|{0x4B7A90}
    data, md = EXE.read_bytes(), Cs(CS_ARCH_X86, CS_MODE_32)
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
            r = old[a]
            assembly, size = r['assembly'], r['length']
        key = f'phase109-primary-{a:x}'
        rows.append(dict(address=a, length=size, assembly=assembly, memory=key+'.txt',
                         executed=a not in [0x4C1770, 0x4BB0D0], scope='Whole extent,not allbranchcoverage;static-only rows explicit.'))
        queries.append(dict(id=key, tool='read_memory', args=dict(program='DKII-DX.exe', address=hex(a), length=size), print_limit=5))
    queries.append(dict(id='phase109-save-DX', tool='save_program', args=dict(program='DKII-DX.exe'), print_limit=30))
    (ROOT/'phase109-primary-manifest.json').write_text(json.dumps(dict(functions=rows), indent=2), encoding='utf8')
    (ROOT/'phase109-primary-queries.json').write_text(json.dumps(queries, indent=2), encoding='utf8')
    print(len(rows), 'whole code spans')


if __name__ == '__main__':
    main()
