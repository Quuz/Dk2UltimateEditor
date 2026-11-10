"""Collect exact original Threat/Crypt code and diagnostic consumer boundaries."""
from pathlib import Path
import json
import re
from capstone import Cs, CS_ARCH_X86, CS_MODE_32
from build_effect_semantics import EXE, pe_read

ROOT = Path(__file__).resolve().parent


def main():
    old = {r['address']: r for r in json.loads((ROOT/'phase94-primary-manifest.json').read_text(encoding='utf8'))['functions']}
    current = {0x45E4F0: 'phase106-crypt', 0x45C5D0: 'phase106-crypt',
               0x4C5F50: 'phase106-threat', 0x4512F0: 'phase106-threat',
               0x488930: 'phase106-crypt', 0x4BCB60: 'phase106-crypt', 0x4BCF10: 'phase106-crypt'}
    executed = {0x51DC40, 0x45E4F0, 0x45C5D0, 0x48F350, 0x4C5F50, 0x4512F0, 0x4D8E60, 0x4D9570, 0x4E52F0}
    wanted = executed | set(current)
    data, md = EXE.read_bytes(), Cs(CS_ARCH_X86, CS_MODE_32)
    functions, queries = [], []
    for a in sorted(wanted):
        if a in current:
            assembly = f'{current[a]}-{a:x}-disassemble_function.txt'
            addresses = [int(s[:8], 16) for s in (ROOT/'ghidra'/assembly).read_text(encoding='utf8').splitlines()
                         if re.match(r'^[0-9a-fA-F]{8}:', s)]
            assert min(addresses) == a
            last = max(addresses)
            tail = next(md.disasm(pe_read(data, last, 15), last))
            size = tail.address+tail.size-a
        else:
            assembly, size = old[a]['assembly'], old[a]['length']
        key = f'phase106-primary-{a:x}'
        functions.append(dict(program='DKII-DX.exe', address=a, length=size, assembly=assembly,
                              memory=key+'.txt', executed=a in executed,
                              scope='Whole saved code span;executedpaths vsstatic-only distinguished,not all-branch coverage.'))
        queries.append(dict(id=key, tool='read_memory', args=dict(program='DKII-DX.exe', address=hex(a), length=size), print_limit=5))
    blocks = []
    for a, size, label in [(0x4D9274, 180, 'Crypt-removal-switch'), (0x4D9328, 180, 'Crypt-addition-switch'),
                           (0x66D524, 0x130, 'original-Creature-vtable'), (0x66E3EC, 0x3A0, 'original-World-vtable'),
                           (0x69F578, 30, 'CryptPowersPeopleActive-diagnostic-label')]:
        key = f'phase106-block-{a:x}'
        blocks.append(dict(address=a, length=size, label=label, memory=key+'.txt'))
        queries.append(dict(id=key, tool='read_memory', args=dict(program='DKII-DX.exe', address=hex(a), length=size), print_limit=5))
    queries.append(dict(id='phase106-save-DX', tool='save_program', args=dict(program='DKII-DX.exe'), print_limit=30))
    (ROOT/'phase106-primary-manifest.json').write_text(json.dumps(dict(functions=functions, blocks=blocks), indent=2), encoding='utf8')
    (ROOT/'phase106-primary-queries.json').write_text(json.dumps(queries, indent=2), encoding='utf8')
    print(len(functions), 'wholefunctions;', sum(r['executed'] for r in functions), 'withnativepaths;', len(blocks), 'blocks')


if __name__ == '__main__':
    main()
