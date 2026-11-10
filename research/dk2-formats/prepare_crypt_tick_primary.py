"""Primary code for complete Creature tick and its two Crypt modifier branches."""
from pathlib import Path
import json
import re
from capstone import Cs, CS_ARCH_X86, CS_MODE_32
from build_effect_semantics import EXE, pe_read

ROOT = Path(__file__).resolve().parent


def main():
    previous = {}
    for name in ['phase91-primary-manifest.json', 'phase94-primary-manifest.json', 'phase96-primary-manifest.json',
                 'phase98-primary-manifest.json', 'phase99-primary-manifest.json', 'phase105-primary-manifest.json', 'phase106-primary-manifest.json']:
        previous.update({r['address']: r for r in json.loads((ROOT/name).read_text(encoding='utf8'))['functions']
                         if r.get('program', 'DKII-DX.exe') == 'DKII-DX.exe'})
    current = {0x48D000: 'phase107-tick', 0x48D010: 'phase107-tick', 0x48E160: 'phase107-tick',
               0x485880: 'phase107-tick', 0x59C140: 'phase107-tick', 0x594E10: 'phase107-tick',
               0x575D70: 'phase107-tick', 0x496E00: 'phase107-work'}
    wanted = [0x488930, 0x48D000, 0x48D010, 0x4D5A40, 0x4D4760, 0x4D16B0, 0x4D27D0,
              0x47BBE0, 0x4798F0, 0x4950C0, 0x476FC0, 0x48E160, 0x485880,
              0x59C140, 0x594E10, 0x575D70, 0x4D8E60, 0x4D9570, 0x4E52F0, 0x496E00]
    data, md = EXE.read_bytes(), Cs(CS_ARCH_X86, CS_MODE_32)
    queries, functions = [], []
    for a in wanted:
        if a in current:
            assembly = f'{current[a]}-{a:x}-disassemble_function.txt'
            aa = [int(s[:8], 16) for s in (ROOT/'ghidra'/assembly).read_text(encoding='utf8').splitlines() if re.match(r'^[0-9a-fA-F]{8}:', s)]
            assert min(aa) == a
            last = max(aa)
            tail = next(md.disasm(pe_read(data, last, 15), last))
            size = tail.address+tail.size-a
        else:
            row = previous[a]
            assembly, size = row['assembly'], row['length']
        key = f'phase107-primary-{a:x}'
        functions.append(dict(address=a, length=size, assembly=assembly, memory=key+'.txt',
                              executed=a != 0x496E00, scope='Whole extent;executed paths orstatic only,not allbranchcoverage.'))
        queries.append(dict(id=key, tool='read_memory', args=dict(program='DKII-DX.exe', address=hex(a), length=size), print_limit=5))
    blocks = []
    for a, n, label in [(0x66D524, 0x130, 'Creature-vtable'), (0x66E3EC, 0x3A0, 'World-vtable'),
                         (0x670574, 0x200, 'original-CPCEngineInterface-vtable'),
                         (0x4D9274, 180, 'Crypt-remove-switch'), (0x4D9328, 180, 'Crypt-add-switch'),
                         (0x69D734, 28, 'native-WorkEnhancement-label')]:
        key = f'phase107-block-{a:x}'
        blocks.append(dict(address=a, length=n, label=label, memory=key+'.txt'))
        queries.append(dict(id=key, tool='read_memory', args=dict(program='DKII-DX.exe', address=hex(a), length=n), print_limit=5))
    queries.append(dict(id='phase107-save-DX', tool='save_program', args=dict(program='DKII-DX.exe'), print_limit=30))
    (ROOT/'phase107-primary-manifest.json').write_text(json.dumps(dict(functions=functions, blocks=blocks), indent=2), encoding='utf8')
    (ROOT/'phase107-primary-queries.json').write_text(json.dumps(queries, indent=2), encoding='utf8')
    print(len(functions), 'whole code extents;', len(blocks), 'blocks')


if __name__ == '__main__':
    main()
