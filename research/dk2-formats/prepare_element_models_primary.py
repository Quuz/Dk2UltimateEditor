"""Attest every code extent reached by new model/cross-engine cases; reuse old bytes."""
from pathlib import Path
import json
import re
from capstone import Cs, CS_ARCH_X86, CS_MODE_32
from build_effect_semantics import EXE, pe_read

ROOT = Path(__file__).resolve().parent


def main():
    visited = set()
    runs = json.loads((ROOT/'phase103-model-emulations.json').read_text(encoding='utf8'))['replays']
    for row in runs:
        result = json.loads((ROOT/'ghidra'/row['file']).read_text(encoding='utf8'))['result']
        visited.update(int(a, 16) for a in result['visited_instructions'])
    for row in json.loads((ROOT/'phase103-cross-engine-validation.json').read_text(encoding='utf8'))['replays']:
        result = json.loads((ROOT/row['Unicorn']['file']).read_text(encoding='utf8'))['result']
        visited.update(int(a, 16) for a in result['visited_instructions'])
    old = json.loads((ROOT/'phase102-primary-manifest.json').read_text(encoding='utf8'))
    prior_functions = {r['address']: r for r in old['functions']}
    md = Cs(CS_ARCH_X86, CS_MODE_32)
    data = EXE.read_bytes()
    functions, queries, found = [], [], set()
    paths = sorted((ROOT/'ghidra').glob('phase10[23]-*-disassemble_function.txt'))
    for path in paths:
        addresses = [int(s[:8], 16) for s in path.read_text(encoding='utf8').splitlines()
                     if re.match(r'^[0-9a-fA-F]{8}:', s)]
        start, last = min(addresses), max(addresses)
        if start in found or (start not in visited and start not in (0x5A01A0, 0x588190)):
            continue
        found.add(start)
        tail = next(md.disasm(pe_read(data, last, 15), last))
        length = tail.address + tail.size - start
        if start in prior_functions:
            row = dict(prior_functions[start], executed=start in visited)
            assert row['length'] == length
        else:
            key = f'phase103-primary-{start:x}'
            row = dict(program='DKII-DX.exe', exe=str(EXE), address=start, length=length,
                       assembly=path.name, memory=key+'.txt', executed=start in visited,
                       scope='Whole saved extent; visited PCs recorded. No all-branch coverage claim.')
            queries.append(dict(id=key, tool='read_memory',
                                args=dict(program='DKII-DX.exe', address=hex(start), length=length), print_limit=5))
        functions.append(row)
    missing = [a for a in sorted(visited) if not any(r['address'] <= a < r['address']+r['length'] for r in functions)]
    assert not missing, [hex(a) for a in missing[:30]]
    blocks = list(old['blocks'])
    for address, size, label in [(0x66FC00, 8, 'frame cycle1.0 double'),
                                 (0x66FC24, 8, 'frame0/1 clamp float constants'),
                                 (0x66FC3C, 8, 'frame integer conversion constants'),
                                 (0x66FC98, 8, 'animation frame wrap double'),
                                 (0x66FD1C, 40, 'dynamic model vtable'),
                                 (0x66FD64, 40, 'animated model vtable')]:
        key = f'phase103-block-{address:x}'
        blocks.append(dict(program='DKII-DX.exe', exe=str(EXE), address=address,
                           length=size, label=label, memory=key+'.txt'))
        queries.append(dict(id=key, tool='read_memory',
                            args=dict(program='DKII-DX.exe', address=hex(address), length=size), print_limit=5))
    queries.append(dict(id='phase103-save-DX', tool='save_program', args=dict(program='DKII-DX.exe'), print_limit=30))
    (ROOT/'phase103-primary-manifest.json').write_text(json.dumps(dict(functions=functions, blocks=blocks), indent=2), encoding='utf8')
    (ROOT/'phase103-primary-queries.json').write_text(json.dumps(queries, indent=2), encoding='utf8')
    print(json.dumps(dict(whole_code_extents=len(functions), executed=sum(r['executed'] for r in functions),
                         blocks=len(blocks), new_queries=len(queries), unique_visited_instructions=len(visited))))


if __name__ == '__main__':
    main()
