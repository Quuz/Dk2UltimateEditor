"""Post-emulation attestation of full functions and explicitly typed RTTI data."""
from pathlib import Path
import json
import re
from capstone import Cs, CS_ARCH_X86, CS_MODE_32
from build_effect_semantics import pe_read

ROOT = Path(__file__).resolve().parent


def main():
    md = Cs(CS_ARCH_X86, CS_MODE_32)
    rows, blocks, queries = [], [], []
    profiles = [('DKII-DX.exe', ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe',
                 [0x4A7D10, 0x517490, 0x4C1740, 0x4A84E0, 0x4A8E70, 0x4A9B70, 0x4A80E0]),
                ('DKII-local13.exe', ROOT/'source-binaries/DKII-local13.exe', [0x49EB20, 0x50B3C0, 0x4B79F0])]
    executed = {0x4A7D10, 0x517490, 0x4C1740, 0x49EB20, 0x50B3C0, 0x4B79F0}
    for program, exe, addresses in profiles:
        for address in addresses:
            before = f'phase68-{program}-{address:x}-assembly.txt'
            aa = [int(line[:8],16) for line in (ROOT/'ghidra'/before).read_text().splitlines() if re.match(r'^[0-9a-fA-F]{8}:',line)]
            assert aa[0] == address
            end = next(md.disasm(pe_read(exe.read_bytes(),aa[-1],15),aa[-1]))
            assert end.mnemonic.startswith('ret'), (before,end.mnemonic)
            n = aa[-1]+end.size-address
            key = f'phase68-final-{program}-{address:x}'
            rows.append(dict(program=program,exe=str(exe),address=address,length=n,before=before,
                             after=key+'-assembly.txt',memory=key+'-memory.txt',executed=address in executed))
            queries.extend([
                dict(id=key+'-assembly',tool='disassemble_function',args=dict(program=program,address=hex(address)),print_limit=5),
                dict(id=key+'-memory',tool='read_memory',args=dict(program=program,address=hex(address),length=n),print_limit=5),
            ])
    for label in ['CTrap-vtable','CTrap-locator','CTrap-type']:
        oldfile = ROOT/'ghidra'/f'phase68-DX-{label}.txt'
        v = json.loads(oldfile.read_text())
        address,n = int(v['address'],16),v['length']
        key = f'phase68-final-DX-{label}'
        blocks.append(dict(program='DKII-DX.exe',exe=str(profiles[0][1]),address=address,length=n,
                           memory=key+'-memory.txt',label=label))
        queries.append(dict(id=key+'-memory',tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(address),length=n),print_limit=5))
    (ROOT/'phase68-primary-manifest.json').write_text(json.dumps(dict(functions=rows,blocks=blocks),indent=2),encoding='utf8')
    (ROOT/'phase68-primary-queries.json').write_text(json.dumps(queries,indent=2),encoding='utf8')
    print(len(rows),'complete functions;',sum(r['executed'] for r in rows),'executed;',len(blocks),'RTTI data ranges')


if __name__=='__main__':
    main()
