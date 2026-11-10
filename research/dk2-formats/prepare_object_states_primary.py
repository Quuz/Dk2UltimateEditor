"""Fresh whole-function and vtable bytes for Object state conclusions."""
import json
import re
from capstone import Cs, CS_ARCH_X86, CS_MODE_32
from build_effect_semantics import ROOT, EXE, pe_read


def main():
    old = {}
    source = json.loads((ROOT/'object-loading-semantics.json').read_text(encoding='utf8'))
    for row in source['primary_code_extents']:
        if row['program'] == 'DKII-DX.exe':
            old[row['address']] = dict(length=row['length'], assembly=row['after'])
    for row in json.loads((ROOT/'object-lifetime-semantics.json').read_text(encoding='utf8'))['primary']['functions']:
        old[row['address']] = row
    executed = [0x56D4F0, 0x56D620, 0x49DD60, 0x49E600, 0x49EED0, 0x49BD20,
                0x484940, 0x4B4C20, 0x4A3400, 0x4A3450, 0x509580, 0x50E490, 0x4430D0]
    static = [0x4B9250, 0x4B8640, 0x426070, 0x568E30, 0x4C1AE0, 0x4C1B20]
    data, md = EXE.read_bytes(), Cs(CS_ARCH_X86, CS_MODE_32)
    rows, queries = [], []
    for a in sorted(executed + static):
        if a in static:
            evidence = f'phase110-mana-{a:x}-get_function_by_address.txt'
            text = (ROOT/'ghidra'/evidence).read_text(encoding='utf8')
            m = re.search(r'Body: ([0-9a-fA-F]+) - ([0-9a-fA-F]+)', text)
            assert int(m[1], 16) == a
            length = int(m[2], 16) + 1 - a
        elif a in old:
            evidence, length = old[a]['assembly'], old[a]['length']
        else:
            evidence = f'phase110-state-{a:x}-disassemble_function.txt'
            addresses = [int(s[:8], 16) for s in (ROOT/'ghidra'/evidence).read_text(encoding='utf8').splitlines()
                         if re.match(r'^[0-9a-fA-F]{8}:', s)]
            assert min(addresses) == a
            last = max(addresses)
            tail = next(md.disasm(pe_read(data, last, 15), last))
            length = tail.address + tail.size - a
        key = f'phase110-primary-{a:x}'
        rows.append(dict(address=a, length=length, boundary_evidence=evidence, memory=key+'.txt',
                         executed=a in executed, scope='Whole function extent; selected paths only; static rows not emulated.'))
        queries.append(dict(id=key, tool='read_memory', args=dict(program='DKII-DX.exe', address=hex(a), length=length), print_limit=5))
    blocks = []
    for a, n, label in [(0x66E3EC+0x388, 8, 'original World effect bridge slots'),
                         (0x670574+0x90, 4, 'original CPCEngineInterface slot90')]:
        key = f'phase110-block-{a:x}'
        blocks.append(dict(address=a, length=n, label=label, memory=key+'.txt'))
        queries.append(dict(id=key, tool='read_memory', args=dict(program='DKII-DX.exe', address=hex(a), length=n), print_limit=5))
    queries.append(dict(id='phase110-save-DX', tool='save_program', args=dict(program='DKII-DX.exe'), print_limit=5))
    (ROOT/'phase110-primary-manifest.json').write_text(json.dumps(dict(functions=rows, blocks=blocks), indent=2), encoding='utf8')
    (ROOT/'phase110-primary-queries.json').write_text(json.dumps(queries, indent=2), encoding='utf8')
    print(len(rows), 'whole functions;', len(executed), 'executed;', len(blocks), 'vtable blocks')


if __name__ == '__main__':
    main()
