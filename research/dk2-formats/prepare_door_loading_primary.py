"""Read complete Door loading bodies again after native emulations."""
from pathlib import Path
import json
import re
from capstone import Cs, CS_ARCH_X86, CS_MODE_32
from build_effect_semantics import pe_read

ROOT = Path(__file__).resolve().parent


def main():
    dx = ROOT.parent.parent / 'Dungeon Keeper 2/DKII-DX.exe'
    old = ROOT / 'source-binaries/DKII-local13.exe'
    entries = [
        ('DKII-DX.exe', dx, 'phase67-DX-56a920-disassemble_function.txt'),
        ('DKII-DX.exe', dx, 'phase67-DX-56aa40-disassemble_function.txt'),
        ('DKII-DX.exe', dx, 'phase67-DX-4a7100-disassemble_function.txt'),
        ('DKII-DX.exe', dx, 'phase52-DKII-DX.exe-6361a0-disassemble_function.txt'),
        ('DKII-local13.exe', old, 'phase67-old-560c60-disassemble_function.txt'),
        ('DKII-local13.exe', old, 'phase67-old-560d80-disassemble_function.txt'),
        ('DKII-local13.exe', old, 'phase67-old-49df70-disassemble_function.txt'),
        ('DKII-local13.exe', old, 'phase52-DKII-local13.exe-627cd0-disassemble_function.txt'),
    ]
    rows, blocks, queries = [], [], []
    md = Cs(CS_ARCH_X86, CS_MODE_32)
    for program, exe, file in entries:
        addresses = [int(line[:8], 16) for line in (ROOT / 'ghidra' / file).read_text().splitlines() if re.match(r'^[0-9a-fA-F]{8}:', line)]
        start, last = addresses[0], addresses[-1]
        final = next(md.disasm(pe_read(exe.read_bytes(), last, 15), last))
        assert final.mnemonic.startswith('ret'), (file, final.mnemonic)
        n = last + final.size - start
        key = f'phase67-final-{program}-{start:x}'
        rows.append(dict(program=program, exe=str(exe), address=start, length=n, before=file,
                         after=key+'-assembly.txt', memory=key+'-memory.txt'))
        queries.extend([
            dict(id=key+'-assembly', tool='disassemble_function', args=dict(program=program, address=hex(start)), print_limit=5),
            dict(id=key+'-memory', tool='read_memory', args=dict(program=program, address=hex(start), length=n), print_limit=5),
        ])
    for program, exe, address in [('DKII-DX.exe', dx, 0x68ED34), ('DKII-local13.exe', old, 0x67FCCC)]:
        key = f'phase67-final-{program}-sound-prefix'
        blocks.append(dict(program=program, exe=str(exe), address=address, length=8, memory=key+'-memory.txt', label='sound-prefix'))
        queries.append(dict(id=key+'-memory', tool='read_memory', args=dict(program=program, address=hex(address), length=8), print_limit=5))
    (ROOT / 'phase67-primary-manifest.json').write_text(json.dumps(dict(functions=rows, blocks=blocks), indent=2), encoding='utf8')
    (ROOT / 'phase67-primary-queries.json').write_text(json.dumps(queries, indent=2), encoding='utf8')
    print(len(rows), 'complete functions;', len(blocks), 'data ranges')


if __name__ == '__main__':
    main()
