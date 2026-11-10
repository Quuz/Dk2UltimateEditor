"""Collect the original code/tables used by phase105 full melee compositions."""
from pathlib import Path
import json
import re
from capstone import Cs, CS_ARCH_X86, CS_MODE_32
from build_effect_semantics import EXE, pe_read

ROOT = Path(__file__).resolve().parent


def main():
    old = {}
    for file in ['phase94-primary-manifest.json', 'phase96-primary-manifest.json', 'phase99-primary-manifest.json', 'phase104-primary-manifest.json']:
        old.update({r['address']: r for r in json.loads((ROOT/file).read_text(encoding='utf8'))['functions']})
    wanted = [0x51DC70, 0x51DC40, 0x48DEA0, 0x43D750, 0x441990, 0x4363F0,
              0x498E30, 0x494D00, 0x59D380, 0x59D260, 0x4B5D40, 0x4B5D50,
              0x4E0460, 0x4998A0, 0x65FB54, 0x628BC0, 0x4B2CE0, 0x517490,
              0x48F2E0, 0x517480, 0x527150, 0x4B5DE0, 0x4B5C30,
              0x4D8E60, 0x4D9570, 0x4E52F0]
    data = EXE.read_bytes()
    md = Cs(CS_ARCH_X86, CS_MODE_32)
    queries, functions = [], []
    for a in wanted:
        recent = f'phase105-attack-{a:x}-disassemble_function.txt'
        if (ROOT/'ghidra'/recent).exists():
            addresses = [int(s[:8], 16) for s in (ROOT/'ghidra'/recent).read_text(encoding='utf8').splitlines()
                         if re.match(r'^[0-9a-fA-F]{8}:', s)]
            assert min(addresses) == a
            last = max(addresses)
            tail = next(md.disasm(pe_read(data, last, 15), last))
            size, assembly = tail.address+tail.size-a, recent
        else:
            row = old[a]
            size, assembly = row['length'], row['assembly']
        key = f'phase105-primary-{a:x}'
        functions.append(dict(program='DKII-DX.exe', exe=str(EXE), address=a, length=size,
                              assembly=assembly, memory=key+'.txt', executed=True,
                              scope='Complete saved code span;native paths executed,not all-branch coverage.'))
        queries.append(dict(id=key, tool='read_memory',
                            args=dict(program='DKII-DX.exe', address=hex(a), length=size), print_limit=5))
    blocks = []
    for address, length, label in [(0x65FC94, 514, 'integer-angle-table'),
                                  (0x69DA38, 149, 'impact-material-table-through-sentinel'),
                                  (0x66D524, 0x130, 'original-CCreature-vtable'),
                                  (0x66D654, 0x80, 'original-CObject-used-vslots'),
                                  (0x66E3EC, 0x3A0, 'original-World-used-vslots'),
                                  (0x51D740, 960, 'Variables-switch'),
                                  (0x4D9274, 180, 'Crypt-remove-switch'),
                                  (0x4D9328, 180, 'Crypt-add-switch')]:
        key = f'phase105-block-{address:x}'
        blocks.append(dict(address=address, length=length, label=label, memory=key+'.txt'))
        queries.append(dict(id=key, tool='read_memory',
                            args=dict(program='DKII-DX.exe', address=hex(address), length=length), print_limit=5))
    queries.append(dict(id='phase105-save-DX', tool='save_program', args=dict(program='DKII-DX.exe'), print_limit=30))
    (ROOT/'phase105-primary-manifest.json').write_text(json.dumps(dict(functions=functions, blocks=blocks), indent=2), encoding='utf8')
    (ROOT/'phase105-primary-queries.json').write_text(json.dumps(queries, indent=2), encoding='utf8')
    print(len(functions), 'whole code spans;', len(blocks), 'blocks')


if __name__ == '__main__':
    main()
